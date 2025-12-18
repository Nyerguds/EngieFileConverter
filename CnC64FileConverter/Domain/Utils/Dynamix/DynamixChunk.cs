using System;
using System.Linq;
using System.Text;

namespace Nyerguds.Util
{
    public class DynamixChunk
    {
        public String Identifier { get; private set; }
        public Int32 Address { get; private set; }

        public Int32 DataAddress
        {
            get
            {
                return Address + 8;
            }
        }

        public Int32 DataLength { get; private set; }
        public Byte[] Data { get; private set; }

        public static DynamixChunk GetChunk(Byte[] data, String chunkName, Boolean retrieveContents)
        {
            DynamixChunk dc = new DynamixChunk();
            dc.Address = FindChunk(data, chunkName);
            if (dc.Address == -1)
                return null;
            dc.Identifier = chunkName;
            dc.DataLength = GetChunkDataLength(data, dc.Address);
            dc.Data = retrieveContents ? GetChunkData(data, dc.Address) : null;
            return dc;
        }

        /// <summary>
        /// Finds the start of a chunk.
        /// It does not go over the first 8 bytes, but starts at the start of the header chunk.
        /// </summary>
        /// <param name="data">The bytes of the Dynamix file</param>
        /// <param name="chunkName">The name of the chunk to find.</param>
        /// <returns>The index of the start of the chunk, or -1 if the chunk was not found.</returns>
        public static Int32 FindChunk(Byte[] data, String chunkName)
        {
            if (data == null)
                throw new ArgumentNullException("data", "No data given!");
            if (chunkName == null)
                throw new ArgumentNullException("chunkName", "No chunk name given!");
            // Using UTF-8 as extra check to make sure the name does not contain > 127 values.
            Byte[] chunkNamebytes = Encoding.UTF8.GetBytes(chunkName + ":");
            if (chunkName.Length != 3 || chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk name must be 4 ASCII characters!", "chunkName");
            Int32 offset = 0;
            Int32 end = data.Length;
            Byte[] testBytes = new Byte[4];
            // continue until either the end is reached, or there is not enough space behind it for reading a new header
            while (offset < end && offset + 8 < end)
            {
                Array.Copy(data, offset, testBytes, 0, 4);
                if (chunkNamebytes.SequenceEqual(testBytes))
                    return offset;
                Int32 chunkLength = GetChunkDataLength(data, offset);
                offset += 8 + chunkLength;
            }
            return -1;
        }

        public static Int32 GetChunkDataLength(Byte[] data, Int32 offset)
        {
            if (offset + 8 > data.Length)
                throw new FileTypeLoadException("Bad chunk size in Dynamix image.");
            // Don't want to use BitConverter; then you have to check platform endianness and all that mess.
            //Int32 length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
            Int32 length = (Int32)ArrayUtils.ReadIntFromByteArray(data, offset + 4, 4, true);
            // Sometimes has a byte 80 there? Some flag I guess...
            length = (Int32)((UInt32)length & 0x7FFFFFFF);
            if (length < 0 || length + offset + 8 > data.Length)
                throw new FileTypeLoadException("Bad chunk size in Dynamix image.");
            return length;
        }

        public static Byte[] GetChunkData(Byte[] data, Int32 offset)
        {
            Int32 dataLength = GetChunkDataLength(data, offset);
            Byte[] returndata = new Byte[dataLength];
            Array.Copy(data, offset + 8, returndata, 0, dataLength);
            return returndata;
        }
    }
}