using System;
using Nyerguds.Util;
using Nyerguds.Util.GameData;

namespace Nyerguds.GameData.Dynamix
{
    /// <summary>
    /// Dynamix compression / decompression class. Offers functionality to decompress chunks using RLE or LZW decompression,
    /// and has functions to compress to RLE.
    /// </summary>
    public class DynamixCompression
    {

        public static Byte[] EnrichFourBit(Byte[] vgaData, Byte[] binData)
        {
            Byte[] fullData = new Byte[vgaData.Length * 2];
            // ENRICHED 4-BIT IMAGE LOGIC
            // Basic principle: The data in the VGA chunk is already perfectly viewable as 4-bit image. The colour palettes
            // are designed so each block of 16 colours consists of different tints of the same colour. The 16-colour palette
            // for the VGA chunk alone can be constructed by taking a palette slice where each colour is 16 entries apart.

            // This VGA data [AB] gets "ennobled" to 8-bit by adding detail data [ab] from the BIN chunk, to get bytes [Aa Bb].
            for (Int32 i = 0; i < vgaData.Length; i++)
            {
                Int32 offs = i * 2;
                // This can be written much simpler, but I expanded it to clearly show each step.
                Byte vgaPix = vgaData[i]; // 0xAB
                Byte binPix = binData[i]; // 0xab
                Byte vgaPixHi = (Byte)((vgaPix & 0xF0) >> 4); // 0x0A
                Byte binPixHi = (Byte)((binPix & 0xF0) >> 4); // 0x0a
                Byte finalPixHi = (Byte)((vgaPixHi << 4) + binPixHi); // Aa
                Byte vgaPixLo = (Byte)(vgaPix & 0x0F); // 0x0B
                Byte binPixLo = (Byte)(binPix & 0x0F); // 0x0b
                Byte finalPixLo = (Byte)((vgaPixLo << 4) + binPixLo); // Bb
                // Final result: AB + ab == [Aa Bb]
                fullData[offs] = finalPixHi;
                fullData[offs + 1] = finalPixLo;
            }
            return fullData;
        }

        public static void SplitEightBit(Byte[] imageData, out Byte[] vgaData, out Byte[] binData)
        {
            vgaData = new Byte[imageData.Length / 2];
            binData = new Byte[imageData.Length / 2];
            for (Int32 i = 0; i < imageData.Length; i++)
            {
                Byte pixData = imageData[i];
                Int32 pixHi = pixData & 0xF0;
                Int32 pixLo = pixData & 0x0F;
                if (i % 2 == 0)
                    pixLo = pixLo << 4;
                else
                    pixHi = pixHi >> 4;
                Int32 pixOffs = i / 2;
                vgaData[pixOffs] |= (Byte)pixHi;
                binData[pixOffs] |= (Byte)pixLo;
            }

        }

        /// <summary>
        /// Decompresses Dynamix chunk data. The chunk data should start with the compression
        /// type byte, followed by a 32-bit integer specifying the uncompressed length.
        /// </summary>
        /// <param name="chunkData">Chunk data to decompress. </param>
        /// <returns>The uncompressed data.</returns>
        public static Byte[] DecodeChunk(Byte[] chunkData)
        {
            if (chunkData.Length < 5)
                throw new FileTypeLoadException("Chunk is too short to read compression header!");
            Byte compression = chunkData[0];
            Int32 uncompressedLength = (Int32)ArrayUtils.ReadIntFromByteArray(chunkData, 1, 4, true);
            return Decode(chunkData, 5, null, compression, uncompressedLength);
        }

        /// <summary>
        /// Decompresses Dynamix data.
        /// </summary>
        /// <param name="buffer">Buffer to decompress</param>
        /// <param name="startOffset">Start offset of the data in the buffer</param>
        /// <param name="endOffset">End offset of the data in the buffer</param>
        /// <param name="compression">Compression type: 0 for uncompressed, 1 for RLE, 2 for LZA</param>
        /// <param name="decompressedSize">Decompressed size.</param>
        /// <returns>The uncompressed data.</returns>
        public static Byte[] Decode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Int32 compression, Int32 decompressedSize)
        {
            Int32 start = startOffset ?? 0;
            Int32 end = endOffset ?? buffer.Length;
            if (end < start)
                throw new ArgumentException("End offset cannot be smaller than start offset!", "endOffset");
            if (start < 0 || start > buffer.Length)
                throw new ArgumentOutOfRangeException("startOffset");
            if (end < 0 || end > buffer.Length)
                throw new ArgumentOutOfRangeException("endOffset");
            switch (compression)
            {
                case 0:
                    Byte[] outBuff = new Byte[decompressedSize];
                    Int32 len = Math.Min(end - start, decompressedSize);
                    Array.Copy(buffer, start, outBuff, 0, len);
                    return outBuff;
                case 1:
                    return RleDecode(buffer, (UInt32)start, (UInt32)end, decompressedSize, true);
                case 2:
                    return LzwDecode(buffer, start, end, decompressedSize);
                default:
                    throw new ArgumentException("Unknown compression type: \"" + compression + "\".", "compression");
            }
        }

        public static Byte[] LzwDecode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Int32 decompressedSize)
        {
            DynamixLzwDecoder lzwDec = new DynamixLzwDecoder();
            Byte[] outputBuffer = new Byte[decompressedSize];
            lzwDec.LzwDecode(buffer, startOffset, endOffset, outputBuffer);
            return outputBuffer;
        }

        public static Byte[] RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 decompressedSize, Boolean abortOnError)
        {
            Byte[] outputBuffer = new Byte[decompressedSize];
            RleCompression rle = new RleCompression();
            rle.RleDecodeData(buffer, startOffset, endOffset, outputBuffer, abortOnError);
            return outputBuffer;
        }
        
        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] LzwEncode(Byte[] buffer)
        {
            DynamixLzwEncoder enc= new DynamixLzwEncoder();
            return enc.Compress(buffer);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] RleEncode(Byte[] buffer)
        {
            // Uses standard RLE implementation.
            RleCompression rle = new RleCompression();
            return rle.RleEncodeData(buffer);
        }
    }
}