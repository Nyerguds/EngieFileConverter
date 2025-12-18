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
                {
                    swapped[newHeight][newWidth] = original[newWidth][newHeight];
                }
            }
            return swapped;
        }

        public static Boolean ArraysAreEqual<T>(T[] row1, T[] row2) where T : IComparable<T>
        {
            if (row1 == null && row2 == null)
                return true;
            if (row1 == null || row2 == null)
                return false;
            if (row1.Length != row2.Length)
                return false;
            for (int i = 0; i < row1.Length; i++)
            {
                if (row1[i].CompareTo(row2[i]) != 0)
                    return false;
            }
            return true;
        }

    public static UInt32 GetIntFromByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian)
    {
        UInt32 value = 0;
        Int32 lastByte = bytes - 1;
        if (data.Length < startIndex + bytes)
            throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value from offset" + startIndex + ".");
        for (Int32 index = 0; index < bytes; index++)
        {
            Int32 offs = startIndex + (littleEndian ? lastByte - index : index);
            value |= (UInt32)(data[offs] << (index * 8));
        }
        return value;
    }

    public static void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt32 value)
    {
        Int32 lastByte = bytes - 1;
        if (data.Length < startIndex + bytes)
            throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset" + startIndex + ".");
        for (Int32 index = 0; index < bytes; index++)
        {
            Int32 offs = startIndex + (littleEndian ? lastByte - index : index);
            data[offs] = (Byte)(value >> (8 * index) & 0xFF);
        }
    }
        
        public static Int32 GetLEIntFromByteArray(Byte[] data, Int32 startIndex)
        {
            UInt32 val = ((UInt32)data[startIndex] << 24)
                 | ((UInt32)data[startIndex + 1] << 16)
                 | ((UInt32)data[startIndex + 2] << 8)
                 | data[startIndex + 3];
            return (Int32)val;
        }

        public static void SetLEIntInByteArray(Byte[] data, Int32 index, Int32 value)
        {
            UInt32 val = (UInt32)value;
            data[index] = (Byte)(val >> 24 & 0xFF);
            data[index + 1] = (Byte)(val >> 16 & 0xFF);
            data[index + 2] = (Byte)(val >> 8 & 0xFF);
            data[index + 3] = (Byte)(val & 0xFF);
        }

        public static Int16 GetLEShortFromByteArray(Byte[] data, Int32 startIndex)
        {
            return (Int16)Convert.ToUInt16((data[startIndex] << 8) | data[startIndex + 1]);
        }

        public static void SetLEShortInByteArray(Byte[] data, Int32 index, Int16 value)
        {
            UInt16 val = (UInt16)value;
            data[index] = (Byte)(val >> 8 & 0xFF);
            data[index + 1] = (Byte)(val & 0xFF);
        }

        public static Int32 GetBEIntFromByteArray(Byte[] data, Int32 startIndex)
        {
            UInt32 val = ((UInt32)data[startIndex + 3] << 24)
                         | ((UInt32)data[startIndex + 2] << 16)
                         | ((UInt32)data[startIndex + 1] << 8)
                         | data[startIndex];
            return (Int32)val;
        }

        public static void SetBEIntInByteArray(Byte[] data, Int32 index, Int32 value)
        {
            UInt32 val = (UInt32)value;
            data[index] = (Byte)(val & 0xFF);
            data[index + 1] = (Byte)(val >> 8 & 0xFF);
            data[index + 2] = (Byte)(val >> 16 & 0xFF);
            data[index + 3] = (Byte)(val >> 24 & 0xFF);
        }

        public static Int16 GetBEShortFromByteArray(Byte[] data, Int32 startIndex)
        {
            return (Int16)Convert.ToUInt16((data[startIndex + 1] << 8) | data[startIndex]);
        }

        public static void SetBEShortInByteArray(Byte[] data, Int32 index, Int16 value)
        {
            UInt16 val = (UInt16)value;
            data[index + 1] = (Byte)(val >> 8 & 0xFF);
            data[index] = (Byte)(val & 0xFF);
        }
    }
}
