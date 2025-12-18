using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Nyerguds.Util
{
    public static class ArrayUtils
    {

        public static T[][] SwapDimensions<T>(T[][] original)
        {
            Int32 origHeight = original.Length;
            if (origHeight == 0)
                return new T[0][];
            // Since this is for images, it is assumed that the array is a perfectly rectangular matrix
            Int32 origWidth = original[0].Length;

            T[][] swapped = new T[origWidth][];
            for (Int32 newHeight = 0; newHeight < origWidth; newHeight++)
            {
                swapped[newHeight] = new T[origHeight];
                for (Int32 newWidth = 0; newWidth < origHeight; newWidth++)
                    swapped[newHeight][newWidth] = original[newWidth][newHeight];
            }
            return swapped;
        }

        public static Boolean ArraysAreEqual<T>(T[] row1, T[] row2) where T : IEquatable<T>
        {
            // There's probably a Linq version of this though... Probably .All() or something.
            // But this is with simple arrays.
            if (row1 == null && row2 == null)
                return true;
            if (row1 == null || row2 == null)
                return false;
            if (row1.Length != row2.Length)
                return false;
            for (Int32 i = 0; i < row1.Length; i++)
                if (row1[i].Equals(row2[i]))
                    return false;
            return true;
        }

        public static void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt32 value)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte)(value >> (8 * index) & 0xFF);
            }
        }

        public static UInt32 ReadIntFromByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            UInt32 value = 0;
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                value += (UInt32)(data[offs] << (8 * index));
            }
            return value;
        }

        public static Int32 ReadBitsFromByteArray(Byte[] dataArr, ref Int32 bitIndex, Int32 codeLen, Int32 bufferInEnd)
        {
            Int32 intCode = 0;
            Int32 byteIndex = bitIndex / 8;
            Int32 ignoreBitsAtIndex = bitIndex % 8;
            Int32 bitsToReadAtIndex = Math.Min(codeLen, 8 - ignoreBitsAtIndex);
            Int32 totalUsedBits = 0;
            while (codeLen > 0)
            {
                if (byteIndex >= bufferInEnd)
                    return -1;

                Int32 toAdd = (dataArr[byteIndex] >> ignoreBitsAtIndex) & ((1 << bitsToReadAtIndex) - 1);
                intCode |= (toAdd << totalUsedBits);
                totalUsedBits += bitsToReadAtIndex;
                codeLen -= bitsToReadAtIndex;
                bitsToReadAtIndex = Math.Min(codeLen, 8);
                ignoreBitsAtIndex = 0;
                byteIndex++;
            }
            bitIndex += totalUsedBits;
            return intCode;
        }

        public static void WriteBitsToByteArray(Byte[] dataArr, Int32 bitIndex, Int32 codeLen, Int32 intCode)
        {
            Int32 byteIndex = bitIndex / 8;
            Int32 usedBitsAtIndex = bitIndex % 8;
            Int32 bitsToWriteAtIndex = Math.Min(codeLen, 8 - usedBitsAtIndex);
            while (codeLen > 0)
            {
                Int32 codeToWrite = (intCode & ((1 << bitsToWriteAtIndex) - 1)) << usedBitsAtIndex;
                intCode = intCode >> bitsToWriteAtIndex;
                dataArr[byteIndex] |= (Byte)codeToWrite;
                codeLen -= bitsToWriteAtIndex;
                bitsToWriteAtIndex = Math.Min(codeLen, 8);
                usedBitsAtIndex = 0;
                byteIndex++;
            }
        }

    }
}
