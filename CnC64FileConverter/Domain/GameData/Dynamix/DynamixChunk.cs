using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace Nyerguds.GameData.Dynamix
{
    public class DynamixChunk
    {
        public String Identifier { get; private set; }
        public Int32 Address { get; private set; }
        public Boolean IsContainer { get; set; }
        public Byte[] Data
        {
            get { return this.m_data; }
            set
            {
                Byte[] dataCopy = new Byte[value.Length];
                Array.Copy(value, 0, dataCopy, 0, value.Length);
                this.m_data = dataCopy;
            }
        }
        public Int32 Length { get { return Data.Length + 8; } }
        public Int32 DataLength { get { return Data.Length; } }

        private Byte[] m_data;

        /// <summary>
        /// Creates a chunk.
        /// </summary>
        /// <param name="identifier">Chunk identifier.</param>
        public DynamixChunk(String identifier)
        {
            if (identifier.Length != 3 || Encoding.UTF8.GetBytes(identifier).Length != 3)
                throw new ArgumentException("Identifier must be a 3 ASCII characters!", "identifier");
            this.Identifier = identifier;
        }

        /// <summary>
        /// Creates a chunk with data in it.
        /// </summary>
        /// <param name="identifier">Chunk identifier.</param>
        /// <param name="data">Data to copy into the chunk data.</param>
        public DynamixChunk(String identifier, Byte[] data)
            : this(identifier)
        {
            this.Data = data;
        }

        /// <summary>
        /// Creates a chunk with a 5-byte compression header. This compression header is written at the start of the data array.
        /// </summary>
        /// <param name="identifier">Chunk identifier.</param>
        /// <param name="compressionType">Compression type.</param>
        /// <param name="uncompressedSize">Size of the uncompressed data.</param>
        /// <param name="data">Compressed data to copy into the chunk data.</param>
        public DynamixChunk(String identifier, Byte compressionType, UInt32 uncompressedSize, Byte[] data)
            : this(identifier)
        {
            Byte[] fullData = new Byte[data.Length + 5];
            fullData[0] = compressionType;
            ArrayUtils.WriteIntToByteArray(fullData, 1, 4, true, uncompressedSize);
            Array.Copy(data, 0, fullData, 5, data.Length);
            this.m_data = fullData;
        }

        /// <summary>
        /// Returns the full chunk as byte array.
        /// </summary>
        /// <returns>The full chunk as byte array.</returns>
        public Byte[] WriteChunk()
        {
            Byte[] data = new Byte[Length];
            this.WriteChunk(data, 0);
            return data;
        }

        /// <summary>
        /// Writes this chunk into a target array.
        /// </summary>
        /// <param name="target">Target array</param>
        /// <param name="offset">Offset in the target array</param>
        /// <returns>The offset right behind the written data in the target array.</returns>
        public Int32 WriteChunk(Byte[] target, Int32 offset)
        {
            Array.Copy(Encoding.ASCII.GetBytes(Identifier + ":"), 0, target, offset, 4);
            offset += 4;
            ArrayUtils.WriteIntToByteArray(target, offset, 4, true, (UInt32)(DataLength));
            offset += 4;
            if (IsContainer)
                target[offset - 1] |= 0x80;
            Array.Copy(Data, 0, target, offset, DataLength);
            return offset + DataLength;
        }

        /// <summary>
        /// Builds a chunk container which has other chunks as its data. The other chunk
        /// objects are not preserved in this operation; they are just saved as bytes.
        /// </summary>
        /// <param name="chunkName">Chunk name</param>
        /// <param name="contents">Chunk contents</param>
        /// <returns>The new chunk</returns>
        public static DynamixChunk BuildChunk(String chunkName, params DynamixChunk[] contents)
        {
            DynamixChunk mainChunk = new DynamixChunk(chunkName);
            Int32 fullDataSize = contents.Sum(x => x.Length);
            Byte[] fullData = new Byte[fullDataSize];
            Int32 offset = 0;
            foreach (DynamixChunk chunk in contents)
                offset = chunk.WriteChunk(fullData, offset);
            mainChunk.m_data = fullData;
            mainChunk.IsContainer = true;
            return mainChunk;
        }

        /// <summary>
        /// Finds and reads a chunk from data.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="chunkName">The chunk to find.</param>
        /// <returns>The chunk as DynamixChunk object.</returns>
        public static DynamixChunk ReadChunk(Byte[] data, String chunkName)
        {
            Int32 address = FindChunk(data, chunkName);
            if (address == -1)
                return null;
            DynamixChunk dc = new DynamixChunk(chunkName);
            dc.Address = address;
            dc.IsContainer = (data[7] & 0x80) != 0;
            dc.Data = GetChunkData(data, dc.Address);
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
                throw new ArgumentException("Chunk name must be 3 ASCII characters!", "chunkName");
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
                if (chunkLength < 0)
                    return -1;
                offset += 8 + chunkLength;
                if (offset < 0)
                    return -1;
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