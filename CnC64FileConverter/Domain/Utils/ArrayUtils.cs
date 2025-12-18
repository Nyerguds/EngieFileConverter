using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        
        /// <summary>
        /// Creates and returns a new array, containing the contents of all the given arrays, in the given order.
        /// </summary>
        /// <typeparam name="T">Type of the arrays</typeparam>
        /// <param name="arrays">Arrays to join together.</param>
        /// <returns>A new array containing the contents of all given arrays, joined together.</returns>
        public static T[] MergeArrays<T>(params T[][] arrays)
        {
            Int32 length = 0;
            foreach (T[] array in arrays)
            {
                if (array != null)
                    length += array.Length;
            }
            T[] result = new T[length];
            Int32 copyIndex = 0;
            foreach (T[] array in arrays)
            {
                if (array != null)
                {
                    array.CopyTo(result, copyIndex);
                    copyIndex += array.Length;
                }
            }
            return result;
        }

        public static void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt64 value)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte) (value >> (8*index) & 0xFF);
            }
        }

        public static UInt64 ReadIntFromByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            UInt64 value = 0;
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                value += (UInt64) (data[offs] << (8*index));
            }
            return value;
        }

        public static Int32 ReadBitsFromByteArray(Byte[] dataArr, ref Int32 bitIndex, Int32 codeLen, Int32 bufferInEnd)
        {
            Int32 intCode = 0;
            Int32 byteIndex = bitIndex/8;
            Int32 ignoreBitsAtIndex = bitIndex%8;
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
            Int32 byteIndex = bitIndex/8;
            Int32 usedBitsAtIndex = bitIndex%8;
            Int32 bitsToWriteAtIndex = Math.Min(codeLen, 8 - usedBitsAtIndex);
            while (codeLen > 0)
            {
                Int32 codeToWrite = (intCode & ((1 << bitsToWriteAtIndex) - 1)) << usedBitsAtIndex;
                intCode = intCode >> bitsToWriteAtIndex;
                dataArr[byteIndex] |= (Byte) codeToWrite;
                codeLen -= bitsToWriteAtIndex;
                bitsToWriteAtIndex = Math.Min(codeLen, 8);
                usedBitsAtIndex = 0;
                byteIndex++;
            }
        }

        public static T CloneStruct<T>(T obj) where T : struct
        {
            return StructFromByteArray<T>(StructToByteArray(obj));
        }

        public static T StructFromByteArray<T>(Byte[] bytes) where T : struct
        {
            return ReadStructFromByteArray<T>(bytes, 0);
        }

        public static Byte[] StructToByteArray<T>(T obj) where T : struct
        {
            Int32 size = Marshal.SizeOf(typeof (T));
            Byte[] target = new Byte[size];
            WriteStructToByteArray(obj, target, 0);
            return target;
        }

        public static T ReadStructFromByteArray<T>(Byte[] bytes, Int32 offset) where T : struct
        {
            Int32 size = Marshal.SizeOf(typeof (T));
            if (size + offset > bytes.Length)
                throw new IndexOutOfRangeException("Array is too small to get the requested struct!");
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, offset, ptr, size);
                Object obj = Marshal.PtrToStructure(ptr, typeof (T));
                return (T) obj;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public static void WriteStructToByteArray<T>(T obj, Byte[] target, Int32 index) where T : struct
        {
            Type tType = typeof (T);
            Int32 size = Marshal.SizeOf(tType);
            if (!BitConverter.IsLittleEndian)
            {
                Byte[] arr = GetStructBytes(obj, true);
                Array.Copy(arr, 0, target, index, arr.Length);

            }
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, target, index, size);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        private static Byte[] GetStructBytes<T>(T obj, Boolean littleEndian)
        {
            Type tType = typeof (T);
            if (!tType.IsValueType)
                return new Byte[0];
            if (tType.IsPrimitive)
                return GetValueTypeBytes((IConvertible)obj, littleEndian);

            PropertyInfo[] pi = tType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.MetadataToken).ToArray();
            Dictionary<Int32, Byte[]> allValuesDict = new Dictionary<Int32, Byte[]>();
            foreach (PropertyInfo info in pi)
            {
                Byte[] b = null;
                Type propertyType = info.PropertyType;
                if (tType.IsPrimitive)
                    b = GetValueTypeBytes((IConvertible)info.GetValue(obj, null), littleEndian);
                else if (propertyType.IsValueType)
                    b = GetStructBytes(info.GetValue(obj, null), littleEndian);
                allValuesDict.Add(info.MetadataToken, b);
            }
            FieldInfo[] fi = tType.GetFields(BindingFlags.Public | BindingFlags.NonPublic).OrderBy(f => f.MetadataToken).ToArray();
            foreach (FieldInfo info in fi)
            {
                Byte[] b = null;
                Type propertyType = info.FieldType;
                if (tType.IsPrimitive)
                    b = GetValueTypeBytes((IConvertible)info.GetValue(obj), littleEndian);
                else if (propertyType.IsValueType)
                    b = GetStructBytes(info.GetValue(obj), littleEndian);
                allValuesDict.Add(info.MetadataToken, b);
            }
            Byte[][] allValues = new Byte[allValuesDict.Count][];
            for (Int32 i = 0; i < fi.Length; i++)
            {
                FieldInfo info = fi[i];
                Type fieldType = info.FieldType;
                if (fieldType.IsValueType)
                    allValues[i] = GetValueTypeBytes((IConvertible) info.GetValue(obj), littleEndian);
                else
                    allValues[i] = GetStructBytes(info.GetValue(obj), littleEndian);
            }
            return MergeArrays(allValues);
        }

        private static Byte[] GetValueTypeBytes<T>(T obj, Boolean littleEndian) where T : IConvertible
        {
            Type tType = typeof (T);
            if (tType == typeof (SByte)
                || tType == typeof (UInt16)
                || tType == typeof (Int16)
                || tType == typeof (UInt32)
                || tType == typeof (Int32)
                || tType == typeof (UInt64)
                || tType == typeof (Int64))
            {
                Int32 len = Marshal.SizeOf(tType);
                Byte[] ret = new Byte[len];
                WriteIntToByteArray(ret, 0, len, littleEndian, ((IConvertible)obj).ToUInt64(null));
                return ret;
            }
            Boolean le = BitConverter.IsLittleEndian;
            if (tType == typeof (Single))
            {
                Byte[] sBytes = BitConverter.GetBytes(((IConvertible)obj).ToSingle(null));
                if (!le && littleEndian || !littleEndian)
                    Array.Reverse(sBytes);
                return sBytes;
            }
            if (tType == typeof (Double))
            {
                Byte[] dBytes = BitConverter.GetBytes(((IConvertible)obj).ToDouble(null));
                if (!le && littleEndian || !littleEndian)
                    Array.Reverse(dBytes);
                return dBytes;
            }
            if (tType == typeof (Boolean))
                return new Byte[] {(Byte) ((obj as Boolean?).GetValueOrDefault(false) ? 1 : 0)};
            return new Byte[0];
        }
    }
}
