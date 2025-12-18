using System;
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
        public static Byte[] RleEncodeJazz(Byte[] buffer)
        {
            if (buffer == null)
                return null;
            // Uses standard RLE implementation, but the final data byte is added in a specfic "stop" command.
            JazzRleCompression rle = new JazzRleCompression();
            UInt32 lastPos = (UInt32)buffer.Length - 1;
            Byte[] comprBuffer = rle.RleEncodeData(buffer, 0, lastPos);
            Int32 finalLen = comprBuffer.Length;
            // Length plus 2-byte header lpus final dummy copy-command containing last byte
            Byte[] finalBuff = new Byte[finalLen + 4];
            // Account for 0-code written at the end to signal the end of the compression.
            ArrayUtils.WriteIntToByteArray(finalBuff, 0, 2, true, (UInt64)(finalLen + 2));
            Array.Copy(comprBuffer, 0, finalBuff, 2, comprBuffer.Length);
            // no need to write the 00-code; it's the default fill value in a new array.
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
        public static Byte[] RleDecodeJazz(Byte[] buffer, UInt32? startOffset, Boolean hasHeader, UInt32? decompressedSize, Boolean abortOnError)
        {
            if (buffer == null)
                return null;
            UInt32 usableStartOffset = startOffset.GetValueOrDefault(0);
            UInt32? endOffset;
            if (hasHeader)
            {
                if (buffer.Length < 2)
                    return null; // Error.
                endOffset = (UInt32) ArrayUtils.ReadIntFromByteArray(buffer, (Int32) usableStartOffset, 2, true);
                // Add read value size and start
                usableStartOffset += 2;
                endOffset += usableStartOffset;
                if (endOffset > buffer.Length)
                {
                    if (abortOnError)
                        return null;
                    endOffset = (UInt32) buffer.Length;
                }
            }
            else
            {
                // If not set, this will rely on the compressed data ending on a 0-byte.
                // That should happen anyway, though.
                endOffset = null;
            }
            // Setting this to null forces auto-expand logic.
            Byte[] bufferOut = decompressedSize.HasValue ? new Byte[decompressedSize.Value] : null;
            JazzRleCompression rle = new JazzRleCompression();
            rle.AbortOnError = abortOnError;
            // Never set "abort on error" on this level; value 0 is a normal end.
            Int32 retSize = rle.RleDecodeData(buffer, usableStartOffset, endOffset, ref bufferOut, false);
            return retSize == -1 ? null : bufferOut;
        }

        protected Boolean AbortOnError { get; set; }

        protected override Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, ref UInt32 bufferEnd, out Boolean isRepeat, out UInt32 amount)
        {
            Boolean success = base.GetCode(buffer, ref inPtr, ref bufferEnd, out isRepeat, out amount);
            // Detect end of compression.
            if (success && amount == 0)
            {
                // Technically this should always be a "copy" command, but in reality it doesn't really matter. But it can be used as corruption check.
                if (isRepeat && this.AbortOnError)
                    return false;
                // Set to 1, and make sure decompression ends after reading the next 1 byte.
                amount = 1;
                bufferEnd = inPtr + 1;
            }
            return success;
        }

    }
}