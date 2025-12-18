using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nyerguds.Util
{
    /// <summary>
    /// From http://www.sanity-free.org/12/crc32_implementation_in_csharp.html
    /// </summary>
    public class Crc32
    {
        private static UInt32[] table = FillTable();

        public static UInt32 ComputeChecksum(Byte[] bytes)
        {
            return ComputeChecksum(bytes, 0, bytes.Length);
        }

        public static UInt32 ComputeChecksum(Byte[] bytes, Int32 start, Int32 length)
        {
            UInt32 crc = 0xFFFFFFFF;
            Int32 end = start + length;
            for (Int32 i = start; i < end; ++i)
            {
                Byte index = (Byte)(((crc) & 0xFF) ^ bytes[i]);
                crc = (crc >> 8) ^ table[index];
            }
            return ~crc;
        }

        public static Byte[] ComputeChecksumBytes(Byte[] bytes)
        {
            return BitConverter.GetBytes(ComputeChecksum(bytes));
        }

        private static UInt32[] FillTable()
        {
            const UInt32 poly = 0xEDB88320;
            UInt32[] fillTable = new UInt32[256];
            for (UInt32 i = 0; i < fillTable.Length; ++i)
            {
                UInt32 temp = i;
                for (Int32 j = 8; j > 0; --j)
                {
                    if ((temp & 1) == 1)
                        temp = ((temp >> 1) ^ poly);
                    else
                        temp >>= 1;
                }
                fillTable[i] = temp;
            }
            return fillTable;
        }
    }
}
