using System;

namespace Nyerguds.Util
{
    /// <summary>
    /// From http://www.sanity-free.org/12/crc32_implementation_in_csharp.html
    /// </summary>
    public class Crc32
    {
        private static readonly uint[] Table = FillTable();

        public static uint ComputeChecksum(byte[] bytes)
        {
            return ComputeChecksum(bytes, 0, bytes.Length);
        }

        public static uint ComputeChecksum(byte[] bytes, int start, int length)
        {
            uint crc = 0xFFFFFFFF;
            int end = start + length;
            for (int i = start; i < end; ++i)
            {
                byte index = (byte)((crc & 0xFF) ^ bytes[i]);
                crc = (crc >> 8) ^ Table[index];
            }
            return ~crc;
        }

        public static byte[] ComputeChecksumBytes(byte[] bytes)
        {
            return BitConverter.GetBytes(ComputeChecksum(bytes));
        }

        private static uint[] FillTable()
        {
            const uint poly = 0xEDB88320;
            uint[] fillTable = new uint[256];
            for (uint i = 0; i < fillTable.Length; ++i)
            {
                uint temp = i;
                for (int j = 8; j > 0; --j)
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
