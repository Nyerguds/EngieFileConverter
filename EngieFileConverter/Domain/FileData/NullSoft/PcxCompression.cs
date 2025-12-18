using System;
using System.IO;

namespace Nyerguds.FileData.Compression.NullSoft
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
    public class PcxCompression
    {
        public static Byte[] RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 scanlineSize, Int32 planes, Int32 height, out UInt32 offset)
        {
            Int32 outputSize = planes * scanlineSize * height;
            offset = startOffset ?? 0;
            UInt32 end = (UInt32)buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            Byte[] output = new Byte[outputSize];
            Int32 outputOffset = 0;
            while (offset < end && outputOffset < outputSize)
            {
                Byte val = buffer[offset++];
                if ((val & 0xC0) == 0xC0)
                {
                    // Repeat
                    UInt32 amount = (UInt32) (val & 0x3F);
                    if (offset >= end)
                        break;
                    if (amount == 0)
                    {
                        amount = 1;
                        val = 0xc0;
                    }
                    else
                        val = buffer[offset++];
                    if (outputOffset + amount > outputSize)
                        amount = (UInt32)(outputSize - outputOffset);
                    for (Int32 i = 0; i < amount; ++i)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">Image data buffer.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="stride">Stride of a full line in the image data. For planar data, this means one line of actual pixel data, of the combined planes.</param>
        /// <returns>The compressed data.</returns>
        public static Byte[] RleEncode(Byte[] buffer, Int32 height, Int32 stride)
        {
            UInt32 end = (UInt32)buffer.Length;
            UInt32 linePtr = 0;
            using (MemoryStream output = new MemoryStream())
            {
                for (Int32 y = 0; y < height; ++y)
                {
                    UInt32 inPtr = linePtr;
                    linePtr += (UInt32)stride;
                    while (inPtr < linePtr && inPtr < end)
                    {
                        Byte val = buffer[inPtr];
                        UInt32 start = inPtr;
                        // Increase inptr to the last repeated.
                        for (; inPtr < end && buffer[inPtr] == val; ++inPtr) { }
                        Int64 len = inPtr - start;
                        if (len == 1 && val < 0xC0)
                            output.WriteByte(val);
                        else
                        {
                            while (len > 0)
                            {
                                output.WriteByte((Byte)(Math.Min(0x3F, len) | 0xC0));
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