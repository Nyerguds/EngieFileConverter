// The Dynamix LZW decompression code was converted from the C++ code of
// VOGONS.org user tikalat's "midi tools v4". It contains no license, but I
// assume the fact he included the code implies that he doesn't mind it being
// useful to more people. The license on the attachment on the forum says
// "Fair use/fair dealing exception"; not really a license meant for posting
// your own code under.
// 
// https://www.vogons.org/viewtopic.php?p=273448#p273448

using System;
using Nyerguds.Util;

namespace Nyerguds.FileData.Dynamix
{
    /// <summary>
    /// The Dynamix LZW decompression class.
    /// https://www.vogons.org/viewtopic.php?p=273448#p273448
    /// </summary>
    public class DynamixLzwDecoder
    {
        // Current code's string
        private byte[] codeCur = new byte[256];
        // Length of the current code
        private int codeLen;
        // Amount of bits in the current code
        private int codeSize;
        // cache chunks; 8 times the code size. Unsure what uses this except the reset.
        private int cacheBits;

        // The "strings" of the dictionary table
        private byte[][] dictTableStr;
        // lengths of the "strings" in the dictionary table
        private byte[] dictTableLen;

        // Current dictionary size
        private int dictSize;
        // Current dictionary maximum before the codeSize needs to be increased.
        private int dictMax;
        // True if no more codes can be added.
        private bool dictFull;

        private void LzwReset()
        {
            this.dictTableStr = new byte[0x4000][];
            this.dictTableLen = new byte[0x4000];
            for (int lcv = 0; lcv < 256; ++lcv)
            {
                this.dictTableLen[lcv] = 1;
                this.dictTableStr[lcv] = new byte[] {(byte)lcv};
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

        public void LzwDecode(byte[] buffer, int? startOffset, int? endOffset, byte[] bufferOut)
        {
            int inPtr = startOffset ?? 0;
            int bitIndex = inPtr * 8;
            int inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length;
            int outPtr = 0;
            this.LzwReset();
            this.cacheBits = 0;
            while (outPtr < bufferOut.Length)
            {
                // get next code
                int code = ArrayUtils.ReadBitsFromByteArray(buffer, ref bitIndex, this.codeSize, inPtrEnd);

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
                        int ignoreBits = this.codeSize * 8 - this.cacheBits;
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
                    for (uint codelen = 0; codelen < this.codeLen; ++codelen)
                    //for (lastCodeValue = 0; lastCodeValue < this.codeLen; ++lastCodeValue)
                        bufferOut[outPtr++] = this.codeCur[codelen];
                }
                else
                {
                    // write output
                    int len = this.dictTableLen[code];
                    for (uint codelen = 0; codelen < len; ++codelen)
                    //for (lastCodeValue = 0; lastCodeValue < len; ++lastCodeValue)
                        bufferOut[outPtr++] = this.dictTableStr[code][codelen];
                    // expand current string
                    this.codeCur[this.codeLen++] = this.dictTableStr[code][0];
                }
                if (this.codeLen < 2)
                    continue;
                // add to dictionary (2+ bytes only)
                if (!this.dictFull)
                {
                    int lastCodeValue;
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
                    this.dictTableStr[lastCodeValue]= new byte[this.codeLen];
                    for (uint codelen = 0; codelen < this.codeLen; ++codelen)
                        this.dictTableStr[lastCodeValue][codelen] = this.codeCur[codelen];
                    this.dictTableLen[lastCodeValue] = (byte)this.codeLen;
                }
                // reset to current code.
                for (uint codelen = 0; codelen < this.dictTableLen[code]; ++codelen)
                //for (lastCodeValue = 0; lastCodeValue < this.dictTableLen[code]; ++lastCodeValue)
                    this.codeCur[codelen] = this.dictTableStr[code][codelen];
                this.codeLen = this.dictTableLen[code];
            }
        }
    }
}