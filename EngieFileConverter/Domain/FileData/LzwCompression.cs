using System;
using System.IO;

namespace Nyerguds.FileData.Compression
{
    /// <summary>
    /// LZW-based compressor/decompressor - basic algorithm used as described on Mark Nelson's website:
    /// https://marknelson.us/posts/1989/10/01/lzw-data-compression.html
    /// Based on the C# translation by Pedro Villarreal:
    /// https://github.com/pevillarreal/LzwCompressor
    /// Pedro Villarreal's code is released under the MIT license:
    /// 
    /// ==============================================================================
    /// 
    /// MIT License
    ///
    /// Copyright (c) 2019 Pedro Villarreal
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a copy
    /// of this software and associated documentation files (the "Software"), to deal
    /// in the Software without restriction, including without limitation the rights
    /// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    /// copies of the Software, and to permit persons to whom the Software is
    /// furnished to do so, subject to the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be included in all
    /// copies or substantial portions of the Software.
    /// 
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    /// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    /// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    /// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    /// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    /// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    /// SOFTWARE.
    /// 
    /// ==============================================================================
    /// 
    /// The code was adapted by Maarten Meuris aka Nyerguds:
    /// -Fixes the 7-bit filter caused by using an ASCII stream reader (an issue later fixed on the original repository)
    /// -Uses byte arrays as input and output.
    /// -Added support for 12 and 13 bit, with an enum for the different supported bit lengths. Different values for
    ///  TABLE_SIZE taken from https://marknelson.us/assets/1989-10-01-lzw-data-compression/lzw_nelson.cpp
    /// </summary>
    public class LzwCompression
    {
        private int BITS; // maximum bits allowed to read
        private int HASHING_SHIFT; // hash bit to use with the hasing algorithm to find correct index
        private int MAX_VALUE; // max value allowed based on max bits
        private int MAX_CODE; // max code possible
        private int TABLE_SIZE; // must be bigger than the maximum allowed by maxbits and prime

        private int[] code_value; // code table
        private int[] prefix_code; // prefix table
        private int[] append_character; // character table

        private ulong input_bit_buffer; // bit buffer to temporarily store bytes read from the files
        private int input_bit_count; // counter for knowing how many bits are in the bit buffer

        public LzwCompression(LzwSize bitSize)
        {
            switch (bitSize)
            {
                case LzwSize.Size12Bit:
                case LzwSize.Size13Bit:
                case LzwSize.Size14Bit:
                    this.BITS = (int)bitSize;
                    break;
                default:
                    throw new ArgumentException("Unsupported bit size.", "bitSize");
            }
            this.HASHING_SHIFT = this.BITS - 8; // hash bit to use with the hasing algorithm to find correct index
            this.MAX_VALUE = (1 << this.BITS) - 1; // max value allowed based on max bits
            this.MAX_CODE = this.MAX_VALUE - 1; // max code possible
            // TABLE_SIZE must be bigger than the maximum allowed by maxbits, and prime
            switch (bitSize)
            {
                case LzwSize.Size12Bit:
                    this.TABLE_SIZE = 5021;
                    break;
                case LzwSize.Size13Bit:
                    this.TABLE_SIZE = 9029;
                    break;
                case LzwSize.Size14Bit:
                    this.TABLE_SIZE = 18041;
                    break;
            }
            this.code_value = new int[this.TABLE_SIZE]; // code table
            this.prefix_code = new int[this.TABLE_SIZE]; // prefix table
            this.append_character = new int[this.TABLE_SIZE]; // character table
        }

        /// <summary>
        /// Used to blank out the bit buffer, in case this class is called to compress and decompress from the same instance.
        /// </summary>
        private void Initialize()
        {
            this.input_bit_buffer = 0;
            this.input_bit_count = 0;
        }

        public byte[] Compress(byte[] inputBuffer)
        {
            byte[] outputBuffer;
            using (MemoryStream inStream = new MemoryStream(inputBuffer))
            using (MemoryStream outStream = new MemoryStream())
            {
                try
                {
                    this.Initialize();
                    int next_code = 256;
                    int character;
                    for (int i = 0; i < this.TABLE_SIZE; ++i) // blank out table
                        this.code_value[i] = -1;
                    int string_code = inStream.ReadByte();
                    while ((character = inStream.ReadByte()) != -1) // read until we reach end of file
                    {
                        int index = this.FindMatch(string_code, character);
                        if (this.code_value[index] != -1) // set string if we have something at that index
                            string_code = this.code_value[index];
                        else // insert new entry
                        {
                            if (next_code <= this.MAX_CODE) // otherwise we insert into the tables
                            {
                                this.code_value[index] = next_code++; // insert and increment next code to use
                                this.prefix_code[index] = string_code;
                                this.append_character[index] = (byte)character;
                            }
                            this.OutputCode(outStream, string_code); // output the data in the string
                            string_code = character;
                        }
                    }
                    this.OutputCode(outStream, string_code); // output last code
                    this.OutputCode(outStream, this.MAX_VALUE); // output end of buffer
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
        private int FindMatch(int hash_prefix, int hash_character)
        {
            int index = (hash_character << this.HASHING_SHIFT) ^ hash_prefix;
            int offset = (index == 0) ? 1 : this.TABLE_SIZE - index;
            while (true)
            {
                if (this.code_value[index] == -1)
                    return index;
                if (this.prefix_code[index] == hash_prefix && this.append_character[index] == hash_character)
                    return index;
                index -= offset;
                if (index < 0)
                    index += this.TABLE_SIZE;
            }
        }

        public byte[] Decompress(byte[] inputBuffer, int startOffset, int length)
        {
            byte[] outputBuffer;
            using (MemoryStream inStream = new MemoryStream(inputBuffer))
            using (MemoryStream outStream = new MemoryStream())
            {
                try
                {
                    this.Initialize();
                    int next_code = 256;
                    byte[] decode_stack = new byte[this.TABLE_SIZE];
                    inStream.Seek(startOffset, SeekOrigin.Begin);
                    int old_code = this.input_code(inStream);
                    byte character = (byte)old_code;
                    outStream.WriteByte((byte)old_code); // write first Byte since it is plain ascii
                    int new_code = this.input_code(inStream);
                    while (new_code != this.MAX_VALUE) // read file all file
                    {
                        int code;
                        int iCounter;
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
                            decode_stack[iCounter] = (byte) this.append_character[code];
                            ++iCounter;
                            if (iCounter >= this.MAX_CODE)
                                throw new IndexOutOfRangeException("Maximum code exceeded.");
                            code = this.prefix_code[code];
                        }
                        decode_stack[iCounter] = (byte)code;
                        character = decode_stack[iCounter]; // set last char used
                        while (iCounter >= 0) // write out decodestack
                        {
                            outStream.WriteByte(decode_stack[iCounter]);
                            --iCounter;
                        }
                        if (next_code <= this.MAX_CODE) // insert into tables
                        {
                            this.prefix_code[next_code] = old_code;
                            this.append_character[next_code] = character;
                            ++next_code;
                        }
                        old_code = new_code;
                        new_code = this.input_code(inStream);
                    }
                    outputBuffer = outStream.ToArray();
                }
                catch (EndOfStreamException)
                {
                    outputBuffer = outStream.ToArray();
                }
            }
            if (outputBuffer.Length == length || length == 0)
                return outputBuffer;
            byte[] outputBuffer2 = new byte[length];
            Array.Copy(outputBuffer, 0, outputBuffer2, 0, Math.Min(outputBuffer.Length, outputBuffer2.Length));
            return outputBuffer2;
        }

        private int input_code(MemoryStream pReader)
        {
            while (this.input_bit_count <= 24) // fill up buffer
            {
                this.input_bit_buffer |= (ulong)pReader.ReadByte() << (24 - this.input_bit_count); // insert Byte into buffer
                this.input_bit_count += 8; // increment counter
            }
            uint return_value = (uint)this.input_bit_buffer >> (32 - this.BITS);
            this.input_bit_buffer <<= this.BITS; // remove it from buffer
            this.input_bit_count -= this.BITS; // decrement bit counter
            int temp = (int)return_value;
            return temp;
        }

        private void OutputCode(MemoryStream output, int code)
        {
            this.input_bit_buffer |= (ulong)code << (32 - this.BITS - this.input_bit_count); // make space and insert new code in buffer
            this.input_bit_count += this.BITS; // increment bit counter
            while (this.input_bit_count >= 8) // write all the bytes we can
            {
                output.WriteByte((byte)((this.input_bit_buffer >> 24) & 255)); // write Byte from bit buffer
                this.input_bit_buffer <<= 8; // remove written Byte from buffer
                this.input_bit_count -= 8; // decrement counter
            }
        }
    }

    /// <summary>
    /// Bit lengths supported by the LZWCompression class.
    /// </summary>
    public enum LzwSize
    {
        Size12Bit = 12,
        Size13Bit = 13,
        Size14Bit = 14,
    }
}