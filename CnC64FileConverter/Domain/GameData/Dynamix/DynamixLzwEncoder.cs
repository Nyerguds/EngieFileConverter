using System;
using System.Collections.Generic;
using System.Linq;
using Nyerguds.Util;

namespace Nyerguds.GameData.Dynamix
{
    /// <summary>
    /// LZW compression class. Experimental.
    /// </summary>
    public class DynamixLzwEncoder
    {
        private List<Byte[]> dictKeys = new List<Byte[]>();
        private List<Int32> dictCodes = new List<Int32>();


        private Int32 GetCode(Byte[] sequence)
        {
            Int32 seqLen = sequence.Length;
            if (seqLen == 1)
                return sequence[0];
            // 256 is not used; it's the "reset" code.
            for (Int32 i = 257; i < this.dictKeys.Count; i++)
            {
                Byte[] check = this.dictKeys[i];
                if (seqLen != check.Length)
                    continue;
                Boolean noMatch = false;
                for (Int32 bi = 0; bi < check.Length; bi++)
                {
                    if (sequence[bi] == check[bi])
                        continue;
                    noMatch = true;
                    break;
                }
                if (noMatch)
                    continue;
                return i;
            }
            return -1;
        }

        private Boolean ContainsCode(Byte[] sequence)
        {
            return GetCode(sequence) != -1;
        }

        public DynamixLzwEncoder()
        {
            for (Int32 i = 0; i < 256; i++)
            {
                this.dictKeys.Add(new Byte[] { (Byte)i });
                this.dictCodes.Add(i);
            }
            // Reset code
            this.dictKeys.Add(null);
            this.dictCodes.Add(256);
        }

        public Byte[] Compress(Byte[] buffer)
        {
            Int32 codeLen = 9;
            Int32 bitIndex = 0;
            Int32 outbuffSize = buffer.Length * 2;
            Byte[] outbuff = new Byte[outbuffSize];
            Int32 addedSize = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                Byte b = buffer[i];
                // increase code length to amount of bits needed by intCode.
                ArrayUtils.WriteBitsToByteArray(outbuff, bitIndex, codeLen, b);
                bitIndex += codeLen;
                if (((i + addedSize + 1) % 24) != 23)
                    continue;
                ArrayUtils.WriteBitsToByteArray(outbuff, bitIndex, codeLen, 0x100);
                bitIndex += codeLen;
                addedSize++;
            }
            Int32 bufSize = (bitIndex + 7) / 8;
            Byte[] outbuf2 = new Byte[bufSize];
            Array.Copy(outbuff, outbuf2, bufSize);
            return outbuf2;
        }


        public Int32[] CompressToInts(Byte[] buffer)
        {
            Byte[] match = new Byte[0];
            Int32[] compressed = new Int32[(buffer.Length * 2) / 3];
            Int32 index = 0;
            foreach (Byte b in buffer)
            {
                Int32 oldLen = match.Length;
                Byte[] nextMatch = new Byte[oldLen + 1];
                nextMatch[oldLen] = b;
                if (this.ContainsCode(nextMatch))
                    match = nextMatch;
                else
                {
                    Int32 code = this.GetCode(match);
                    // Add current code to list
                    compressed[index++]= this.dictCodes[code];
                    // new sequence; add it to the dictionary
                    this.dictKeys.Add(nextMatch.ToArray());
                    this.dictCodes.Add(this.dictCodes.Count);
                    match = new Byte[] { b };
                }
            }
            // write remaining output if necessary
            if (match.Length > 0)
            {
                Int32 code = this.GetCode(match);
                compressed[index++] = this.dictCodes[code];
            }
            Int32[] finalCodes = new Int32[index];
            Array.Copy(compressed, 0, finalCodes, 0, index);
            return finalCodes;
        }
    }

}