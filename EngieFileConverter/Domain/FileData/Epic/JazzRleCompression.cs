using System;
using System.Text;
using Nyerguds.FileData.Compression;
using Nyerguds.Util;

namespace Nyerguds.FileData.Epic
{
    public class JazzRleCompression : RleImplementation<JazzRleCompression>
    {
        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <returns>The run-length encoded data, with size header.</returns>
        public static byte[] RleEncodeJazz(byte[] buffer)
        {
            if (buffer == null)
                return null;
            // Uses standard RLE implementation, but the final data byte is added in a specific "stop" command.
            JazzRleCompression rle = new JazzRleCompression();
            uint lastPos = (uint)buffer.Length - 1;
            byte[] comprBuffer = rle.RleEncodeData(buffer, 0, lastPos);
            int finalLen = comprBuffer.Length;
            // Length plus 2-byte header plus 2-byte final dummy copy-command containing the last byte.
            byte[] finalBuff = new byte[finalLen + 4];
            // Account for 0-code written at the end to signal the end of the compression.
            ArrayUtils.WriteIntToByteArray(finalBuff, 0, 2, true, (ulong)(finalLen + 2));
            Array.Copy(comprBuffer, 0, finalBuff, 2, finalLen);
            // no need to write the 00-code; it's the default clear value in a new array.
            finalBuff[finalLen + 3] = buffer[lastPos];
            return finalBuff;
        }
        /// <summary>
        /// Decompresses Jazz Jackrabbit RLE data.
        /// </summary>
        /// <param name="buffer">Buffer in which the data is stored.</param>
        /// <param name="startOffset">Offset in the data where the compressed block starts.</param>
        /// <param name="hasHeader">True if the first two bytes of the compressed block indicate the length of the compressed data.</param>
        /// <param name="decompressedSize">Decompressed size. Leave null if unknown.</param>
        /// <param name="abortOnError">Abort on error.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static byte[] RleDecodeJazz(byte[] buffer, uint? startOffset, bool hasHeader, uint? decompressedSize, bool abortOnError)
        {
            int actualCompressedSize;
            return RleDecodeJazz(buffer, startOffset, hasHeader, decompressedSize, abortOnError, out actualCompressedSize);
        }

        /// <summary>
        /// Decompresses Jazz Jackrabbit RLE data.
        /// </summary>
        /// <param name="buffer">Buffer in which the data is stored.</param>
        /// <param name="startOffset">Offset in the data where the compressed block starts.</param>
        /// <param name="hasHeader">True if the first two bytes of the compressed block indicate the length of the compressed data.</param>
        /// <param name="decompressedSize">Decompressed size. Leave null if unknown.</param>
        /// <param name="abortOnError">Abort on error.</param>
        /// <param name="actualCompressedSize">Actual read size before encountering a stop-command in the RLE data</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static byte[] RleDecodeJazz(byte[] buffer, uint? startOffset, bool hasHeader, uint? decompressedSize, bool abortOnError, out int actualCompressedSize)
        {
            actualCompressedSize = -1;
            if (buffer == null)
                return null;
            uint usableStartOffset = startOffset.GetValueOrDefault(0);
            int compressedSize = -1;
            uint? endOffset;
            if (hasHeader)
            {
                if (buffer.Length < 2)
                    return null; // Error.
                compressedSize = (int)ArrayUtils.ReadIntFromByteArray(buffer, (int)usableStartOffset, 2, true);
                // Add read value size and start
                usableStartOffset += 2;
                endOffset = (uint)compressedSize + usableStartOffset;
                if (endOffset > buffer.Length)
                {
                    if (abortOnError)
                        return null;
                    endOffset = (uint) buffer.Length;
                }
            }
            else
            {
                // If not set, this will rely on the compressed data ending on a 0-byte.
                // That should happen anyway, though.
                endOffset = null;
            }
            // Setting this to null forces auto-expand logic.
            byte[] bufferOut = decompressedSize.HasValue ? new byte[decompressedSize.Value] : null;
            JazzRleCompression rle = new JazzRleCompression();
            rle.AbortOnError = abortOnError;
            rle.LastEndPoint = -1;
            // Never set "abort on error" on this level; value 0 is a normal end.
            int retSize = rle.RleDecodeData(buffer, usableStartOffset, endOffset, ref bufferOut, false);
            actualCompressedSize = rle.LastEndPoint == -1 ? compressedSize : (rle.LastEndPoint - (int)usableStartOffset);
            return retSize == -1 ? null : bufferOut;
        }

        protected bool AbortOnError { get; set; }
        protected int LastEndPoint { get; set; }

        protected override bool GetCode(byte[] buffer, ref uint inPtr, ref uint bufferEnd, out bool isRepeat, out uint amount)
        {
            bool success = base.GetCode(buffer, ref inPtr, ref bufferEnd, out isRepeat, out amount);
            // Detect end of compression.
            if (success && amount == 0)
            {
                // Technically this should always be a "copy" command, but in reality it doesn't really matter. But it can be used as corruption check.
                if (isRepeat)
                    return !this.AbortOnError;
                // Set to 1, and make sure decompression ends after reading the next 1 byte.
                amount = 1;
                bufferEnd = inPtr + 1;
                this.LastEndPoint = (int) bufferEnd;
            }
            return success;
        }

    }
}