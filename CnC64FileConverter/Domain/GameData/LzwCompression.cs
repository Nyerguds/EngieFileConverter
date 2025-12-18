using System;
using System.IO;
using System.Text;

namespace Nyerguds.Util.GameData
{
    /// <summary>
    /// LZW-based compressor/decompressor - basic algorithm used as described on Mark Nelson's website http://marknelson.us
    /// Based on the C# translation by Github user pevillarreal
    /// https://github.com/pevillarreal/LzwCompressor
    /// The code was adapted to fix the 7-bit filter caused by using an ASCII stream reader.
    /// This version uses byte arrays as input and output, and uses an enum to switch to different supported bit lengths.
    /// </summary>
    public class LzwCompression
    {
        private Int32 BITS; // maximum bits allowed to read
        private Int32 HASHING_SHIFT; // hash bit to use with the hasing algorithm to find correct index
        private Int32 MAX_VALUE; // max value allowed based on max bits
        private Int32 MAX_CODE; // max code possible
        private Int32 TABLE_SIZE; // must be bigger than the maximum allowed by maxbits and prime

        private Int32[] code_value; // code table
        private Int32[] prefix_code; // prefix table
        private Int32[] append_character; // character table

        private UInt64 input_bit_buffer; // bit buffer to temporarily store bytes read from the files
        private Int32 input_bit_count; // counter for knowing how many bits are in the bit buffer

        public LzwCompression(LzwSize bitSize)
        {
            switch (bitSize)
            {
                case LzwSize.Size12Bit:
                case LzwSize.Size13Bit:
                case LzwSize.Size14Bit:
                    BITS = (Int32)bitSize;
                    break;
                default:
                    throw new ArgumentException("Unsupported bit size!", "bitSize");
            }
            HASHING_SHIFT = BITS - 8; // hash bit to use with the hasing algorithm to find correct index
            MAX_VALUE = (1 << BITS) - 1; // max value allowed based on max bits
            MAX_CODE = MAX_VALUE - 1; // max code possible
            // TABLE_SIZE must be bigger than the maximum allowed by maxbits and prime
            switch (bitSize)
            {
                case LzwSize.Size12Bit:
                    TABLE_SIZE = 5021;
                    break;
                case LzwSize.Size13Bit:
                    TABLE_SIZE = 9029;
                    break;
                case LzwSize.Size14Bit:
                    TABLE_SIZE = 18041;
                    break;
            }
            code_value = new Int32[TABLE_SIZE]; // code table
            prefix_code = new Int32[TABLE_SIZE]; // prefix table
            append_character = new Int32[TABLE_SIZE]; // character table
        }

        private void Initialize() // used to blank  out bit buffer incase this class is called to comprss and decompress from the same instance
        {
            input_bit_buffer = 0;
            input_bit_count = 0;
        }

        public Byte[] Compress(Byte[] inputBuffer)
        {
            Byte[] outputBuffer;
            using (MemoryStream inStream = new MemoryStream(inputBuffer))
            using (MemoryStream outStream = new MemoryStream())
            {
                try
                {
                    Initialize();
                    Int32 next_code = 256;
                    Int32 character;
                    for (Int32 i = 0; i < TABLE_SIZE; i++) // blank out table
                        code_value[i] = -1;
                    Int32 string_code = inStream.ReadByte();
                    while ((character = inStream.ReadByte()) != -1) // read until we reach end of file
                    {
                        Int32 index = this.FindMatch(string_code, character);
                        if (code_value[index] != -1) // set string if we have something at that index
                            string_code = code_value[index];
                        else // insert new entry
                        {
                            if (next_code <= MAX_CODE) // otherwise we insert into the tables
                            {
                                code_value[index] = next_code++; // insert and increment next code to use
                                prefix_code[index] = string_code;
                                append_character[index] = (Byte)character;
                            }
                            this.OutputCode(outStream, string_code); // output the data in the string
                            string_code = character;
                        }
                    }
                    this.OutputCode(outStream, string_code); // output last code
                    this.OutputCode(outStream, MAX_VALUE); // output end of buffer
                    this.OutputCode(outStream, 0); // flush
                    outputBuffer = outStream.ToArray();
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return outputBuffer;
        }

        // hasing function, tries to find index of prefix+char, if not found returns -1 to signify space available
        private Int32 FindMatch(Int32 hash_prefix, Int32 hash_character)
        {
            Int32 index = (hash_character << this.HASHING_SHIFT) ^ hash_prefix;
            Int32 offset = (index == 0) ? 1 : this.TABLE_SIZE - index;
            while (true)
            {
                if (code_value[index] == -1)
                    return index;
                if (prefix_code[index] == hash_prefix && append_character[index] == hash_character)
                    return index;
                index -= offset;
                if (index < 0)
                    index += TABLE_SIZE;
            }
        }

        public Byte[] Decompress(Byte[] inputBuffer, Int32 startOffset, Int32 length)
        {
            Byte[] outputBuffer;
            using (MemoryStream inStream = new MemoryStream(inputBuffer))
            using (MemoryStream outStream = new MemoryStream())
            {
                try
                {
                    Initialize();
                    Int32 next_code = 256;
                    Byte[] decode_stack = new Byte[TABLE_SIZE];
                    inStream.Seek(startOffset, SeekOrigin.Begin);
                    Int32 old_code = this.input_code(inStream);
                    Byte character = (Byte)old_code;
                    outStream.WriteByte((Byte)old_code); // write first Byte since it is plain ascii
                    Int32 new_code = this.input_code(inStream);
                    while (new_code != MAX_VALUE) // read file all file
                    {
                        Int32 code;
                        Int32 iCounter;
                        if (new_code >= next_code)
                        {
                            // fix for prefix+chr+prefix+char+prefx special case
                            decode_stack[0] = character;
                            iCounter = 1;
                            code = old_code;
                        }
                        else
                        {
                            iCounter = 0;
                            code = new_code;
                        }
                        // decode_string
                        while (code > 255) // decode string by cycling back through the prefixes
                        {
                            // lstDecodeStack.Add((Byte)append_character[iCurrentCode]);
                            // iCurrentCode = prefix_code[iCurrentCode];
                            decode_stack[iCounter] = (Byte)append_character[code];
                            ++iCounter;
                            if (iCounter >= MAX_CODE)
                                throw new Exception("oh crap");
                            code = prefix_code[code];
                        }
                        decode_stack[iCounter] = (Byte)code;
                        character = decode_stack[iCounter]; // set last char used
                        while (iCounter >= 0) // write out decodestack
                        {
                            outStream.WriteByte(decode_stack[iCounter]);
                            --iCounter;
                        }
                        if (next_code <= MAX_CODE) // insert into tables
                        {
                            prefix_code[next_code] = old_code;
                            append_character[next_code] = character;
                            ++next_code;
                        }
                        old_code = new_code;
                        new_code = input_code(inStream);
                    }
                    outputBuffer = outStream.ToArray();
                }
                catch (EndOfStreamException)
                {
                    outputBuffer = outStream.ToArray();
                }
                catch (Exception)
                {
                    return null;
                }
            }
            if (outputBuffer.Length == length)
                return outputBuffer;
            Byte[] outputBuffer2 = new Byte[length];
            Array.Copy(outputBuffer, 0, outputBuffer2, 0, Math.Min(outputBuffer.Length, outputBuffer2.Length));
            return outputBuffer2;
        }

        private Int32 input_code(MemoryStream pReader)
        {
            while (input_bit_count <= 24) // fill up buffer
            {
                input_bit_buffer |= (UInt64)pReader.ReadByte() << (24 - input_bit_count); // insert Byte into buffer
                input_bit_count += 8; // increment counter
            }
            UInt32 return_value = (UInt32)this.input_bit_buffer >> (32 - this.BITS);
            input_bit_buffer <<= BITS; // remove it from buffer
            input_bit_count -= BITS; // decrement bit counter
            Int32 temp = (Int32)return_value;
            return temp;
        }

        private void OutputCode(MemoryStream output, Int32 code)
        {
            input_bit_buffer |= (UInt64)code << (32 - BITS - input_bit_count); // make space and insert new code in buffer
            input_bit_count += BITS; // increment bit counter
            while (input_bit_count >= 8) // write all the bytes we can
            {
                output.WriteByte((Byte)((input_bit_buffer >> 24) & 255)); // write Byte from bit buffer
                input_bit_buffer <<= 8; // remove written Byte from buffer
                input_bit_count -= 8; // decrement counter
            }
        }
    }

    public enum LzwSize
    {
        Size12Bit = 12,
        Size13Bit = 13,
        Size14Bit = 14,
    }


}