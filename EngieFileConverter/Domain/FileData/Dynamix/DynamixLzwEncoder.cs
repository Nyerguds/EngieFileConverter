using System;

namespace Nyerguds.FileData.Dynamix
{
    /// <summary>
    /// LZW compression class. Currently untested.
    /// </summary>
    public class DynamixLzwEncoder
    {

        private byte[] _codeCur = new byte[256];
        private int _codeSize;
        private int _codeLen;

        private struct DictTableEntry
        {
            public byte[] Str; // Byte[256];
            public byte Len;
        }

        private DictTableEntry[] dict_table = new DictTableEntry[0x4000];

        private uint _dictSize;
        private uint _dictMax;
        private bool _dictFull;

        private uint FindLzwCode(byte[] data_in, int start, int dataEnd)
        {
            // 1-byte code
            uint findLast = data_in[start];
            uint findLen = 2;
            while (start + (findLen - 1) < dataEnd)
            {
                uint lcv;
                // AGI LZW uses SS:SP stack - limit code size
                if (findLen >= 16) break;
                bool hit = false;
                for (lcv = 0x102; lcv < this._dictSize; ++lcv)
                {
                    // check lengths
                    if (dict_table[lcv].Len != findLen)
                        continue;
                    // compare strings
                    int lcv2;
                    for (lcv2 = 0; lcv2 < dict_table[lcv].Len; ++lcv2)
                        if (dict_table[lcv].Str[lcv2] != data_in[start + lcv2])
                            break;
                    // match! no extra entry needed
                    if (lcv2 == dict_table[lcv].Len && lcv2 > 0)
                    {
                        hit = true;
                        break;
                    }
                }
                // expand search
                if (hit)
                {
                    findLast = lcv;
                    findLen++;
                    continue;
                }
                break;
            }
            return findLast;
        }

        private void LzwReset()
        {
            this.dict_table = new DictTableEntry[0x4000];
            for (int lcv = 0; lcv < 256; ++lcv)
            {
                DictTableEntry dte = new DictTableEntry();
                dte.Len = 1;
                dte.Str = new byte[256];
                dte.Str[0] = (byte) lcv;
                dict_table[lcv] = dte;
            }
            // 00-FF = ASCII
            // 100 = reset
            // 101 = stop
            this._dictSize = 0x102;
            this._dictMax = 0x200;
            this._dictFull = false;
            // start = 9 bit codes
            this._codeSize = 9;
            this._codeLen = 0;
        }

        public byte[] LzwEncode(byte[] data, int dataStart, int dataEnd, bool prefixSize)
        {
            LzwBuffer outBuffer = new LzwBuffer();
            if (dataStart < 0)
                dataStart = 0;
            if (dataEnd < 0)
                dataEnd = data.Length;
            // ------------------------------------------
            // ------------------------------------------
            int lcv = dataStart;
            while (lcv < dataEnd)
            {
                // send reset
                if (lcv == 0)
                {
                    this.LzwReset();
                    outBuffer.PackBitsRight(this._codeSize, 0x100);
                }
                // send reset - LZW stack too large
                if (this._codeLen >= 255)
                {
                    this.LzwReset();
                    outBuffer.PackBitsRight(this._codeSize, 0x100);
                }
                uint new_code = this.FindLzwCode(data, lcv, dataEnd);
                outBuffer.PackBitsRight(this._codeSize, new_code);
                // expand string
                this._codeCur[this._codeLen++] = dict_table[new_code].Str[0];
                lcv += dict_table[new_code].Len;
                // -----------------------------------------------
                // -----------------------------------------------
                // add to dictionary: 2+ bytes only
                if (this._codeLen >= 2)
                {
                    uint lcv1;
                    if (this._dictFull == false)
                    {
                        // check full condition
                        if (this._dictSize == this._dictMax && this._codeSize == 12)
                        {
                            this._dictFull = true;
                            lcv1 = this._dictSize;
                        }
                        else
                            lcv1 = this._dictSize++;
                        // expand dictionary (adaptive LZW)
                        if (this._dictSize == this._dictMax && this._codeSize < 12)
                        {
                            this._dictMax *= 2;
                            this._codeSize++;
                        }
                        // add new entry
                        for (uint lcv2 = 0; lcv2 < this._codeLen; lcv2++)
                        {
                            dict_table[lcv1].Str[lcv2] = this._codeCur[lcv2];
                            dict_table[lcv1].Len++;
                        }
                    }
                    // reset running code!
                    for (lcv1 = 0; lcv1 < dict_table[new_code].Len; lcv1++)
                        this._codeCur[lcv1] = dict_table[new_code].Str[lcv1];
                    this._codeLen = dict_table[new_code].Len;
                    // send reset code
                    if (this._dictSize == this._dictMax && this._codeSize == 12)
                    {
                        outBuffer.PackBitsRight(this._codeSize, 0x100);
                        this.LzwReset();
                    }
                }
            }
            outBuffer.PackBitsRight(this._codeSize, 0x101);
            outBuffer.FlushBitsRight();
            // ------------------------------------------
            // ------------------------------------------
            return outBuffer.GetBuffer(prefixSize, dataEnd - dataStart);
        }

        private class LzwBuffer
        {
            private byte[] buffer;
            private uint curVal;
            private int curBits;
            private uint curSize;

            public LzwBuffer()
            {
                this.ResetFileBits();
            }

            private void ResetFileBits()
            {
                this.buffer = new byte[16 * 0x100000];
                this.curVal = 0;
                this.curBits = 0;
                this.curSize = 0;
            }

            public byte[] GetBuffer(bool addSize, int origSize)
            {
                byte[] outBuf = new byte[addSize ? this.curSize + 4 : this.curSize];
                if (addSize)
                {
                    outBuf[0] = (byte) ((origSize >> 00) & 0xFF);
                    outBuf[1] = (byte) ((origSize >> 08) & 0xFF);
                    outBuf[2] = (byte) ((origSize >> 16) & 0xFF);
                    outBuf[3] = (byte) ((origSize >> 24) & 0xFF);
                }
                Array.Copy(this.buffer, 0, outBuf, addSize ? 4 : 0, this.curSize);
                return outBuf;
            }

            public void PackBitsLeft(int bits, uint val)
            {
                while (bits-- != 0)
                {
                    if (curBits == 8)
                    {
                        curBits = 0;
                        this.buffer[this.curSize++] = (byte) (curVal & 0xff);
                    }
                    curVal <<= 1;
                    curVal |= ((val >> bits) & 1);
                    curBits++;
                }
            }

            public void PackBitsRight(int bits, uint val)
            {
                while (bits-- != 0)
                {
                    if (curBits == 8)
                    {
                        curBits = 0;
                        this.buffer[this.curSize++] = (byte) (curVal & 0xff);
                    }
                    curVal >>= 1;
                    curVal |= ((val & 1) << 7);
                    val >>= 1;
                    curBits++;
                }
            }

            public void FlushBitsLeft()
            {
                while (curBits > 0)
                {
                    if (curBits == 8)
                    {
                        curBits = 0;
                        this.buffer[this.curSize++] = (byte) (curVal & 0xff);
                        break;
                    }
                    curVal <<= 1;
                    curVal |= 0;
                    curBits++;
                }
            }

            public void FlushBitsRight()
            {
                while (curBits > 0)
                {
                    if (curBits == 8)
                    {
                        curBits = 0;
                        this.buffer[this.curSize++] = (byte) (curVal & 0xff);
                        break;
                    }
                    curVal >>= 1;
                    curVal |= 0;
                    curBits++;
                }
            }
        }
    }
}