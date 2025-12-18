using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.Utils
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

        public static Int32 GetBEIntFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex] << 24)
                 | (data[startIndex + 1] << 16)
                 | (data[startIndex + 2] << 8)
                 | data[startIndex + 3];
        }

        public static Int16 GetBEShortFromByteArray(byte[] data, int startIndex)
        {
            return Convert.ToInt16((data[startIndex] << 8) | data[startIndex + 1]);
        }

        public static Int32 GetLEIntFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex + 3] << 24)
                 | (data[startIndex + 2] << 16)
                 | (data[startIndex + 1] << 8)
                 | data[startIndex];
        }

        public static Int32 GetLEShortromByteArray(byte[] data, int startIndex)
        {
            return Convert.ToInt16((data[startIndex + 1] << 8) | data[startIndex]);
        }
    }
}
