using System;

namespace Nyerguds.Util
{
    /// <summary>
    /// Dynamix compression / decompression class. Offers functionality to decompress chunks using RLE or LZW decompression,
    /// and has functions to compress to RLE.
    /// </summary>
    public class DynamixCompression
    {
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
                    return RleDecode(buffer, startOffset, endOffset, decompressedSize);
                case 2:
                    return LzwDecode(buffer, startOffset, endOffset, decompressedSize);
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

        public static Byte[] RleDecode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Int32 decompressedSize)
        {
            Byte[] outputBuffer = new Byte[decompressedSize];
            RleDecode(buffer, startOffset, endOffset, outputBuffer);
            return outputBuffer;
        }

        public static void RleDecode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Byte[] bufferOut)
        {
            Int32 inPtr = startOffset ?? 0;
            Int32 inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length;
            Int32 outPtr = 0;

            // RLE implementation:
            // highest bit set = followed by range of repeating bytes
            // highest bit not set = followed by range of non-repeating bytes
            // In both cases, the "code" specifies the amount of bytes; either to write, or to skip.

            while (outPtr < bufferOut.Length && inPtr < inPtrEnd)
            {
                // get next code
                Int32 code = buffer[inPtr++];
                if (code == -1)
                    return;
                // RLE run
                if ((code & 0x80) != 0)
                {
                    if (inPtr >= inPtrEnd)
                        return;
                    Int32 run = code & 0x7f;
                    Int32 rle = buffer[inPtr++];
                    for (UInt32 lcv = 0; lcv < run; lcv++)
                    {
                        if (outPtr >= bufferOut.Length)
                            return;
                        bufferOut[outPtr++] = (Byte)rle;
                    }
                }
                // raw run
                else
                {
                    Int32 run = code & 0x7f;
                    for (UInt32 lcv = 0; lcv < run; lcv++)
                    {
                        if (inPtr >= inPtrEnd)
                            return;
                        Int32 data = buffer[inPtr++];
                        if (outPtr >= bufferOut.Length)
                            return;
                        bufferOut[outPtr++] = (Byte)data;
                    }
                }
            }
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
            return RleEncode(buffer, 3);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <param name="minimumRepeating">Minimum amount of repeating bytes before compression is applied.</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] RleEncode(Byte[] buffer, Int32 minimumRepeating)
        {
            // Technically, compressing a repetition of 2 is not useful: the compressed data
            // ends up the same size, but it adds an extra byte to the data that follows it.
            // But it is allowed for the sake of completion.
            if (minimumRepeating < 2)
                minimumRepeating = 2;
            Int32 inPtr = 0;
            Int32 outPtr = 0;
            // Ensure big enough buffer. Sanity check will be done afterwards.
            Byte[] bufferOut = new Byte[(buffer.Length * 3) / 2];

            // RLE implementation:
            // highest bit set = followed by range of repeating bytes
            // highest bit not set = followed by range of non-repeating bytes
            // In both cases, the "code" specifies the amount of bytes; either to write, or to skip.
            Int32 len = buffer.Length;
            Boolean repeatDetected = false;
            while (inPtr < len)
            {
                if (repeatDetected || HasRepeatingAhead(buffer, len, inPtr, minimumRepeating))
                {
                    repeatDetected = false;
                    // Found more than (minimumRepeating) bytes. Worth compressing. Apply run-length encoding.
                    Int32 start = inPtr;
                    Int32 end = Math.Min(inPtr + 0x7F, len);
                    Byte cur = buffer[inPtr];
                    // Already checked these
                    inPtr += minimumRepeating;
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == cur; inPtr++) { }
                    bufferOut[outPtr++] = (Byte)((inPtr - start) | 0x80);
                    bufferOut[outPtr++] = cur;
                }
                else
                {
                    while (!repeatDetected && inPtr < len)
                    {
                        Int32 start = inPtr;
                        Int32 end = Math.Min(inPtr + 0x7F, len);
                        for (; inPtr < end; inPtr++)
                        {
                            // detected bytes to compress after this one: abort.
                            if (!HasRepeatingAhead(buffer, len, inPtr, minimumRepeating))
                                continue;
                            repeatDetected = true;
                            break;
                        }
                        bufferOut[outPtr++] = (Byte)(inPtr - start);
                        for (Int32 i = start; i < inPtr; i++)
                            bufferOut[outPtr++] = buffer[i];
                    }
                }
            }
            Byte[] finalOut = new Byte[outPtr];
            Array.Copy(bufferOut, 0, finalOut, 0, outPtr);
            return finalOut;
        }

        public static Boolean HasRepeatingAhead(Byte[] buffer, Int32 max, Int32 ptr, Int32 minAmount)
        {
            if (ptr + minAmount - 1 >= max)
                return false;
            Byte cur = buffer[ptr];
            for (Int32 i = 1; i < minAmount; i++)
                if (buffer[ptr + i] != cur)
                    return false;
            return true;
        }

    }
}