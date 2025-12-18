using System;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace Nyerguds.ImageManipulation
{

    public static class PngHandler
    {
        /// <summary>An array containing the identifying bytes required at the start of a PNG image file.</summary>
        private static Byte[] PNG_IDENTIFIER = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        /// <summary>Returns an array containing the identifying bytes required at the start of a PNG image file.</summary>
        /// <returns>An array containing the identifying bytes required at the start of a PNG image file.</returns>
        public static Byte[] GetPngIdentifier() { return ArrayUtils.CloneArray(PNG_IDENTIFIER); }

        /// <summary>The contents of the IDAT chunk for a 1x1 8-bit indexed image with pixel value 0.</summary>
        private static Byte[] PNG_BLANK = { 0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01 };
        /// <summary>
        /// Returns the contents of the IDAT chunk for a 1x1 8-bit indexed image with pixel value 0.
        /// Used as dummy for generating custom-sized palettes.
        /// </summary>
        /// <returns>The contents of the IDAT chunk for a 1x1 8-bit indexed image with pixel value 0.</returns>
        public static Byte[] GetBlankPngIdatContents() { return ArrayUtils.CloneArray(PNG_BLANK); }

        /// <summary>
        /// Checks the start of a byte array to see if it matches the identifying bytes required at the start of a PNG image file.
        /// This will not do a full integrity check on chunk CRCs.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns>True if the start of the data matches the PNG identifier.</returns>
        public static Boolean IsPng(Byte[] data)
        {
            Int32 idLen = PNG_IDENTIFIER.Length;
            if (data.Length < PNG_IDENTIFIER.Length)
                return false;
            for (Int32 i = 0; i < idLen; ++i)
                if (data[i] != PNG_IDENTIFIER[i])
                    return false;
            return true;
        }

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
            if (!IsPng(data))
                throw new FormatException("Data does not contain a png header.");
            // Using UTF-8 as extra check to make sure the name does not contain > 127 values.
            Byte[] chunkNamebytes = Encoding.UTF8.GetBytes(chunkName);
            if (chunkName.Length != 4 || chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk name must be 4 ASCII characters!", "chunkName");
            Int32 offset = PNG_IDENTIFIER.Length;
            Int32 end = data.Length;
            // continue until either the end is reached, or there is not enough space behind it for reading a new chunk
            while (offset + 12 <= end)
            {
                Int32 nameStart = offset + 4;
                Boolean isMatch = true;
                for (Int32 i = 0; i < 4; ++i)
                {
                    if (chunkNamebytes[i] != data[nameStart + i])
                    {
                        isMatch = false;
                        break;
                    }
                }
                Int32 chunkLength = GetPngChunkDataLength(data, offset);
                if (isMatch)
                {
                    // For efficiency, only check checksum on found chunk.
                    if (!PngChecksumMatches(data, offset, chunkLength))
                        throw new FormatException(String.Format("Incorrect checksum on chunk data at {0}", offset));
                    return offset;
                }
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
            ArrayUtils.WriteInt32ToByteArrayBe(target, offset, chunkData.Length);
            offset += 4;
            Int32 nameOffset = offset;
            Array.Copy(chunkNamebytes, 0, target, offset, 4);
            offset += 4;
            Int32 curLength = chunkData.Length;
            Array.Copy(chunkData, 0, target, offset, curLength);
            offset += curLength;
            UInt32 crcval = Crc32.ComputeChecksum(target, nameOffset, chunkData.Length + 4);
            ArrayUtils.WriteInt32ToByteArrayBe(target, offset, crcval);
            offset += 4;
            return offset;
        }

        /// <summary>
        /// Returns the length of the data inside a given chunk. This value does not include the additional 12 bytes
        /// for the length value, the chunk ID and the checksum.
        /// </summary>
        /// <param name="data">The PNG file data</param>
        /// <param name="chunkOffset">Offset of the PNG chunk, as found by FindPngChunk.</param>
        /// <returns>The value read from the chunk's length block.</returns>
        public static Int32 GetPngChunkDataLength(Byte[] data, Int32 chunkOffset)
        {
            if (chunkOffset + 12 > data.Length)
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            // Don't want to use BitConverter; then you have to check platform endianness and all that mess.
            //Int32 length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
            Int32 length = ArrayUtils.ReadInt32FromByteArrayBe(data, chunkOffset);
            if (length < 0 || chunkOffset + 12 + length > data.Length)
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            return length;
        }

        /// <summary>
        /// Gets the data from inside a PNG chunk.
        /// </summary>
        /// <param name="data">The PNG file data</param>
        /// <param name="chunkOffset">Offset of the chunk.</param>
        /// <returns>The contents inside the chunk, without the chunk header or CRC footer.</returns>
        public static Byte[] GetPngChunkData(Byte[] data, Int32 chunkOffset)
        {
            return GetPngChunkData(data, chunkOffset, -1);
        }

        /// <summary>
        /// Gets the data from inside a PNG chunk. This assumes the length was already fetched.
        /// </summary>
        /// <param name="data">The PNG file data</param>
        /// <param name="chunkOffset">Offset of the chunk.</param>
        /// <param name="chunkLength">Length of the chunk, of -1 to auto-fetch using GetPngChunkDataLength.</param>
        /// <returns>The contents inside the chunk, without the chunk header or CRC footer.</returns>
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

        /// <summary>
        /// Checks whether the 4-byte CRC checksum at the end of the chunk matches the contents.
        /// </summary>
        /// <param name="data">The PNG file data</param>
        /// <param name="chunkOffset">Offset of the chunk.</param>
        /// <returns>True if the 4-byte CRC checksum at the end of the chunk matches the contents.</returns>
        public static Boolean PngChecksumMatches(Byte[] data, Int32 chunkOffset)
        {
            return PngChecksumMatches(data, chunkOffset, -1);
        }

        /// <summary>
        /// Checks whether the 4-byte CRC checksum at the end of the chunk matches the contents. This assumes the length was already fetched.
        /// </summary>
        /// <param name="data">The PNG file data</param>
        /// <param name="chunkOffset">Offset of the chunk.</param>
        /// <param name="chunkLength">Length of the chunk, of -1 to auto-fetch using GetPngChunkDataLength.</param>
        /// <returns>True if the 4-byte CRC checksum at the end of the chunk matches the contents.</returns>
        public static Boolean PngChecksumMatches(Byte[] data, Int32 chunkOffset, Int32 chunkLength)
        {
            if (chunkLength < 0)
                chunkLength = GetPngChunkDataLength(data, chunkOffset);
            if (chunkLength == -1)
                return false;
            Byte[] checksum = new Byte[4];
            Array.Copy(data, chunkOffset + 8 + chunkLength, checksum, 0, 4);
            UInt32 readChecksum = ArrayUtils.ReadUInt32FromByteArrayBe(checksum, 0);
            UInt32 calculatedChecksum = Crc32.ComputeChecksum(data, chunkOffset + 4, chunkLength + 4);
            return readChecksum == calculatedChecksum;
        }

    }
}