using System;
using Nyerguds.FileData.Compression;

namespace Nyerguds.FileData.Agos
{

    /// <summary>
    /// AdventureSoft / HorrorSoft VGA format compression; a vertical RLE. Used by the AGOS engine.
    /// </summary>
    ///<remarks>Finally managed to convert this to an RleImplementation class.</remarks>
    public class AgosCompression: RleImplementation<AgosCompression>
    {

        public static byte[] DecodeImage(byte[] buffer, uint? startOffset, uint? endOffset, int height, int stride)
        {
            AgosCompression rle = new AgosCompression();
            int byteLength = stride * height;
            byte[] outBuffer = new byte[byteLength];
            if (rle.RleDecodeData(buffer, startOffset, endOffset, ref outBuffer, true) == -1)
                return null;
            // outBuffer is now the image, with its columns stored as rows.
            byte[] outBuffer2 = new byte[byteLength];
            // Post-processing: Exchange rows and columns.
            for (int i = 0; i < byteLength; ++i)
                outBuffer2[(i % height) * stride + (i / height)] = outBuffer[i];
            // outBuffer2 is now the correct image.
            return outBuffer2;
        }

        public static byte[] EncodeImage(byte[] buffer, int stride)
        {
            int byteLength = buffer.Length;
            int height = byteLength / stride;
            // Should not happen, but you never know...
            while (byteLength > height * stride)
                height++;
            byte[] buffer2 = new byte[byteLength];
            // Pre-processing: Exchange rows and columns.
            for (int i = 0; i < byteLength; ++i)
                buffer2[i] = buffer[i % height * stride + i / height];
            // buffer2 is now the image, with its columns stored as rows.
            // Perform actual compression.
            AgosCompression rle = new AgosCompression();
            return rle.RleEncodeData(buffer2);
        }

        #region tweaked overrides
        /// <summary>Maximum amount of repeating bytes that can be stored in one code.</summary>
        protected override uint MaxRepeatValue { get { return 0x80; } }
        /// <summary>Maximum amount of copied bytes that can be stored in one code.</summary>
        protected override uint MaxCopyValue { get { return 0x7F; } }

        /// <summary>
        /// Reads a code, determines the repeat / skip command and the amount of bytes to repeat/skip,
        /// and advances the read pointer to the location behind the read code.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="inPtr">Input pointer.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be read from.</param>
        /// <param name="isRepeat">Returns true for repeat code, false for copy code.</param>
        /// <param name="amount">Returns the amount to copy or repeat.</param>
        /// <returns>True if the read succeeded, false if it failed.</returns>
        protected override bool GetCode(byte[] buffer, ref uint inPtr, ref uint bufferEnd, out bool isRepeat, out uint amount)
        {
            if (inPtr >= bufferEnd)
            {
                isRepeat = false;
                amount = 0;
                return false;
            }
            byte code = buffer[inPtr++];
            isRepeat = (code & 0x80) == 0;
            amount = (uint)(isRepeat ? code + 1 : 0x100 - code);
            return true;
        }

        /// <summary>
        /// Writes the copy/skip code to be put before the actual byte(s) to repeat/skip,
        /// and advances the write pointer to the location behind the written code.
        /// </summary>
        /// <param name="bufferOut">Output buffer to write to.</param>
        /// <param name="outPtr">Pointer for the output buffer.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be written to.</param>
        /// <param name="forRepeat">True if this is a repeat code, false if this is a copy code.</param>
        /// <param name="amount">Amount to write into the repeat or copy code.</param>
        /// <returns>True if the write succeeded, false if it failed.</returns>
        protected override bool WriteCode(byte[] bufferOut, ref uint outPtr, uint bufferEnd, bool forRepeat, uint amount)
        {
            if (bufferOut.Length <= outPtr)
                return false;
            if (forRepeat)
                bufferOut[outPtr++] = (byte)(amount - 1);
            else
                bufferOut[outPtr++] = (byte)(0x100 - amount);
            return true;
        }
        #endregion

    }
}