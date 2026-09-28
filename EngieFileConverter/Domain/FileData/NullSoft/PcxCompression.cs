using System;
using System.Collections.Generic;
using System.IO;

namespace Nyerguds.FileData.NullSoft
{
    /// <summary>
    /// Run Length Encoding (RLE)
    /// In RLE compression, a series of repeated values is replaced by a count and a single value.
    /// But keep in mind. If I have a lot of single values (not a series of identical values), then
    /// this type of compression would double the size of my file. So rather than use RLE on a series
    /// of non identical values, just store the values with no count value before it.
    ///
    /// This makes a problem though. How do you tell if a value is a part of a 2 byte 'count-value'
    /// value, or is that value a direct individual (no count) value to by displayed? The solution
    /// is to make each value that is a count value (a value that you will take the next value and
    /// repeat it) have bits 6 and 7 set. If bits 6 and 7 are set, then clear them, and take the
    /// returned value as the count value. Then take the next value and repeat it count times. Note:
    /// Once you have the count value, the pixel value can have a value of 0-255 (bits 6 and 7 can be set).
    ///
    /// This makes another problem. How to display a single value of 192 or higher. First, you must
    /// make a count value of 1 (11000001b) then the display value of 192 (or higher) (makes a 2 byte
    /// entry).
    /// </summary>
    public static class PcxCompression
    {
        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int scanlineSize, int planes, int height, out uint offset)
        {
            bool zeroRepeatsFound;
            return RleDecode(buffer, startOffset, endOffset, scanlineSize, planes, height, out offset, out zeroRepeatsFound);
        }

        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int scanlineSize, int planes, int height, out uint offset, out bool zeroRepeatsFound)
        {
            zeroRepeatsFound = false;
            int outputSize = planes * scanlineSize * height;
            offset = startOffset ?? 0;
            uint end = (uint)buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            byte[] output = new byte[outputSize];
            int outputOffset = 0;
            while (offset < end && outputOffset < outputSize)
            {
                byte val = buffer[offset++];
                if ((val & 0xC0) == 0xC0)
                {
                    // Repeat
                    uint amount = (uint)(val & 0x3F);
                    if (offset >= end)
                        break;
                    if (amount == 0)
                    {
                        amount = 1;
                        val = 0xc0;
                        zeroRepeatsFound = true;
                    }
                    else
                        val = buffer[offset++];
                    if (outputOffset + amount > outputSize)
                        amount = (uint)(outputSize - outputOffset);
                    for (int i = 0; i < amount; ++i)
                        output[outputOffset++] = val;
                }
                else
                {
                    if (outputOffset >= outputSize)
                        break;
                    output[outputOffset++] = val;
                }
            }
            return output;
        }

        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int scanlineSize, int planes, int height, out uint offset, out byte[] hiddenMessage)
        {
            int outputSize = planes * scanlineSize * height;
            offset = startOffset ?? 0;
            uint end = (uint)buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            byte[] output = new byte[outputSize];
            int outputOffset = 0;
            List<byte> c0Bytes = new List<byte>();
            while (offset < end && outputOffset < outputSize)
            {
                byte val = buffer[offset++];
                if ((val & 0xC0) == 0xC0)
                {
                    // Repeat
                    uint amount = (uint) (val & 0x3F);
                    if (offset >= end)
                        break;
                    val = buffer[offset++];
                    if (amount == 0)
                        c0Bytes.Add(val);
                    if (outputOffset + amount > outputSize)
                        amount = (uint)(outputSize - outputOffset);
                    for (int i = 0; i < amount; ++i)
                        output[outputOffset++] = val;
                }
                else
                {
                    if (outputOffset >= outputSize)
                        break;
                    output[outputOffset++] = val;
                }
            }
            hiddenMessage = c0Bytes.ToArray();
            return output;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer">Image data buffer.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="stride">Stride of a full line in the image data. For planar data, this means one line of actual pixel data, of the combined planes.</param>
        /// <returns>The compressed data.</returns>
        public static byte[] RleEncode(byte[] buffer, int height, int stride)
        {
            uint end = (uint)buffer.Length;
            uint linePtr = 0;
            using (MemoryStream output = new MemoryStream())
            {
                for (int y = 0; y < height; ++y)
                {
                    uint inPtr = linePtr;
                    linePtr += (uint)stride;
                    while (inPtr < linePtr && inPtr < end)
                    {
                        byte val = buffer[inPtr];
                        uint start = inPtr;
                        // Increase inptr to the last repeated.
                        for (; inPtr < end && buffer[inPtr] == val; ++inPtr) { }
                        long len = inPtr - start;
                        if (len == 1 && val < 0xC0)
                            output.WriteByte(val);
                        else
                        {
                            while (len > 0)
                            {
                                output.WriteByte((byte)(Math.Min(0x3F, len) | 0xC0));
                                output.WriteByte(val);
                                len -= 0x3F;
                            }
                        }
                    }
                }
                return output.ToArray();
            }
        }

    }
}