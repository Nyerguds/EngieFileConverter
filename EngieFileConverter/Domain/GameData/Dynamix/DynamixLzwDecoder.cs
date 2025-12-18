using System;
using Nyerguds.Util;

namespace Nyerguds.GameData.Dynamix
{
    /// <summary>
    /// The Dynamix LZW decompression class.
    /// </summary>
    public class DynamixLzwDecoder
    {
        // Current code's string
        private Byte[] codeCur = new Byte[256];
        // Length of the current code
        private Int32 codeLen;
        // Amount of bits in the current code
        private Int32 codeSize;
        // cache chunks; 8 times the code size. Unsure what uses this except the reset.
        private Int32 cacheBits;

        // The "strings" of the dictionary table
        private Byte[][] dictTableStr;
        // lengths of the "strings" in the dictionary table
        private Byte[] dictTableLen;

        // Current dictionary size
        private Int32 dictSize;
        // Current dictionary maximum before the codeSize needs to be increased.
        private Int32 dictMax;
        // True if no more codes can be added.
        private Boolean dictFull;

        private void LzwReset()
        {
            this.dictTableStr = new Byte[0x4000][];
            this.dictTableLen = new Byte[0x4000];
            //for (Int32 i = 256; i < this.dictTableStr.Length; i++)
            //    dictTableStr[i] = new Byte[100];
            for (Int32 lcv = 0; lcv < 256; lcv++)
            {
                this.dictTableLen[lcv] = 1;
                this.dictTableStr[lcv] = new Byte[] {(Byte)lcv};
            }
            // 00-FF = ASCII
            // 100 = reset
            this.dictSize = 0x101;
            this.dictMax = 0x200;
            this.dictFull = false;
            // start = 9 bit codes
            this.codeSize = 9;
            this.codeLen = 0;
            // 9-12 byte cache chunks
            this.cacheBits = 0;
        }

        public void LzwDecode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Byte[] bufferOut)
        {
            Int32 inPtr = startOffset ?? 0;
            Int32 bitIndex = inPtr * 8;
            Int32 inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length;
            Int32 outPtr = 0;
            this.LzwReset();
            this.cacheBits = 0;
            while (outPtr < bufferOut.Length)
            {
                // get next code
                //Int32 code = GetBitsRight(this.codeSize, buffer, inPtrEnd, ref inPtr);
                Int32 code = ArrayUtils.ReadBitsFromByteArray(buffer, ref bitIndex, this.codeSize, inPtrEnd);

                if (code == -1)
                    return;
                // refresh data cache
                this.cacheBits += this.codeSize;
                if (this.cacheBits >= this.codeSize * 8)
                    this.cacheBits -= this.codeSize * 8;
                // reset: used when the codes are full and new codes are needed?
                if (code == 0x100)
                {
                    // Dynamix: dump data cache
                    if (this.cacheBits > 0)
                    {
                        Int32 ignoreBits = this.codeSize * 8 - this.cacheBits;
                        ArrayUtils.ReadBitsFromByteArray(buffer, ref bitIndex, ignoreBits, inPtrEnd);
                    }
                    this.LzwReset();
                    continue;
                }
                // special case: expand for new entry
                if (code >= this.dictSize && !this.dictFull)
                {
                    this.codeCur[this.codeLen++] = this.codeCur[0];
                    // write output - future expanded string
                    for (UInt32 codelen = 0; codelen < this.codeLen; codelen++)
                    //for (lastCodeValue = 0; lastCodeValue < this.codeLen; lastCodeValue++)
                        bufferOut[outPtr++] = this.codeCur[codelen];
                }
                else
                {
                    // write output
                    Int32 len = this.dictTableLen[code];
                    for (UInt32 codelen = 0; codelen < len; codelen++)
                    //for (lastCodeValue = 0; lastCodeValue < len; lastCodeValue++)
                        bufferOut[outPtr++] = this.dictTableStr[code][codelen];
                    // expand current string
                    this.codeCur[this.codeLen++] = this.dictTableStr[code][0];
                }
                if (this.codeLen < 2)
                    continue;
                // add to dictionary (2+ bytes only)
                if (!this.dictFull)
                {
                    Int32 lastCodeValue;
                    // check full condition
                    if (this.dictSize == this.dictMax && this.codeSize == 12)
                    {
                        this.dictFull = true;
                        lastCodeValue = this.dictSize;
                    }
                    else
                    {
                        lastCodeValue = this.dictSize++;
                        this.cacheBits = 0;
                    }
                    // expand dictionary (adaptive LZW)
                    if (this.dictSize == this.dictMax && this.codeSize < 12)
                    {
                        this.dictMax *= 2;
                        this.codeSize++;
                    }
                    // add new entry
                    this.dictTableStr[lastCodeValue]= new Byte[this.codeLen];
                    for (UInt32 codelen = 0; codelen < this.codeLen; codelen++)
                        this.dictTableStr[lastCodeValue][codelen] = this.codeCur[codelen];
                    this.dictTableLen[lastCodeValue] = (Byte)this.codeLen;
                }
                // reset to current code.
                for (UInt32 codelen = 0; codelen < this.dictTableLen[code]; codelen++)
                //for (lastCodeValue = 0; lastCodeValue < this.dictTableLen[code]; lastCodeValue++)
                    this.codeCur[codelen] = this.dictTableStr[code][codelen];
                this.codeLen = this.dictTableLen[code];
            }
        }
    }
}