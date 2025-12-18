using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace Nyerguds.ImageManipulation
{

    public static class PngHandler
    {
        private static Byte[] PNG_IDENTIFIER = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        public static Byte[] GetPngIdentifier() { return ArrayUtils.CloneArray(PNG_IDENTIFIER); }
        private static Byte[] PNG_BLANK = { 0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01 };
        public static Byte[] GetBlankPngIdatContents() { return ArrayUtils.CloneArray(PNG_BLANK); }

        /// <summary>
        /// Finds the start of a png chunk. This assumes the image is already identified as PNG.
        /// It does not go over the first 8 bytes, but starts at the start of the header chunk.
        /// </summary>
        /// <param name="data">The bytes of the png image.</param>
        /// <param name="chunkName">The name of the chunk to find.</param>
        /// <returns>The index of the start of the png chunk, or -1 if the chunk was not found.</returns>
        public static Int32 FindPngChunk(Byte[] data, String chunkName)
        {
            if (data == null)
                throw new ArgumentNullException("data", "No data given!");
            if (chunkName == null)
                throw new ArgumentNullException("chunkName", "No chunk name given!");
            // Using UTF-8 as extra check to make sure the name does not contain > 127 values.
            Byte[] chunkNamebytes = Encoding.UTF8.GetBytes(chunkName);
            if (chunkName.Length != 4 || chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk name must be 4 ASCII characters!", "chunkName");
            Int32 offset = PNG_IDENTIFIER.Length;
            Int32 end = data.Length;
            Byte[] testBytes = new Byte[4];
            // continue until either the end is reached, or there is not enough space behind it for reading a new chunk
            while (offset + 12 <= end)
            {
                Array.Copy(data, offset + 4, testBytes, 0, 4);
                if (chunkNamebytes.SequenceEqual(testBytes))
                    return offset;
                Int32 chunkLength = GetPngChunkDataLength(data, offset);
                // chunk size + chunk header + chunk checksum = 12 bytes.
                offset += 12 + chunkLength;
            }
            return -1;
        }

        /// <summary>
        /// Writes a png data chunk.
        /// </summary>
        /// <param name="target">Target array to write into.</param>
        /// <param name="offset">Offset in the array to write the data to.</param>
        /// <param name="chunkName">4-character chunk name.</param>
        /// <param name="chunkData">Data to write into the new chunk.</param>
        /// <returns>The new offset after writing the new chunk. Always equal to the offset plus the length of chunk data plus 12.</returns>
        public static Int32 WritePngChunk(Byte[] target, Int32 offset, String chunkName, Byte[] chunkData)
        {
            if (offset + chunkData.Length + 12 > target.Length)
                throw new ArgumentException("Data does not fit in target array!", "chunkData");
            if (chunkName.Length != 4)
                throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
            Byte[] chunkNamebytes = Encoding.ASCII.GetBytes(chunkName);
            if (chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk must be 4 bytes!", "chunkName");
            Int32 curLength;
            ArrayUtils.WriteIntToByteArray(target, offset, curLength = 4, false, (UInt32) chunkData.Length);
            offset += curLength;
            Int32 nameOffset = offset;
            Array.Copy(chunkNamebytes, 0, target, offset, curLength = 4);
            offset += curLength;
            Array.Copy(chunkData, 0, target, offset, curLength = chunkData.Length);
            offset += curLength;
            UInt32 crcval = Crc32.ComputeChecksum(target, nameOffset, chunkData.Length + 4);
            ArrayUtils.WriteIntToByteArray(target, offset, curLength = 4, false, crcval);
            offset += curLength;
            return offset;
        }

        public static Int32 GetPngChunkDataLength(Byte[] data, Int32 chunkOffset)
        {
            if (chunkOffset + 12 > data.Length)
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            // Don't want to use BitConverter; then you have to check platform endianness and all that mess.
            //Int32 length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
            Int32 length = (Int32) ArrayUtils.ReadIntFromByteArray(data, chunkOffset, 4, false);
            if (length < 0 || chunkOffset + 12 + length > data.Length || !PngChecksumMatches(data, chunkOffset, length))
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            return length;
        }

        public static Byte[] GetPngChunkData(Byte[] data, Int32 chunkOffset)
        {
            return GetPngChunkData(data, chunkOffset, -1);
        }

        public static Byte[] GetPngChunkData(Byte[] data, Int32 chunkOffset, Int32 chunkLength)
        {
            if (chunkLength < 0)
                chunkLength = GetPngChunkDataLength(data, chunkOffset);
            if (chunkLength == -1)
                return null;
            Byte[] chunkData = new Byte[chunkLength];
            Array.Copy(data, chunkOffset + 8, chunkData, 0, chunkLength);
            return chunkData;
        }

        public static Boolean PngChecksumMatches(Byte[] data, Int32 offset, Int32 chunkLength)
        {
            Byte[] checksum = new Byte[4];
            Array.Copy(data, offset + 8 + chunkLength, checksum, 0, 4);
            UInt32 readChecksum = (UInt32) ArrayUtils.ReadIntFromByteArray(checksum, 0, 4, false);
            UInt32 calculatedChecksum = Crc32.ComputeChecksum(data, offset + 4, chunkLength + 4);
            return readChecksum == calculatedChecksum;
        }

    }
}