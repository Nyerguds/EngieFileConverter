using System;
using System.Collections.Generic;
using Nyerguds.Util.GameData;

namespace Nyerguds.GameData.Agos
{
    // This one is too messed up to convert to a RleImplementation class.
    public class AgosCompression: RleImplementation<AgosCompression>
    {

        public static Byte[] DecodeImage(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 height, Int32 stride)
        {
            AgosCompression rle = new AgosCompression();
            Int32 byteLength = stride * height;
            Byte[] outBuffer = new Byte[byteLength];
            if (rle.RleDecodeData(buffer, startOffset, endOffset, outBuffer, true) == -1)
                return null;
            // outBuffer is now the image, with its columns stored as rows.
            Byte[] outBuffer2 = new Byte[byteLength];
            // Post-processing: Exchange rows and columns.
            for (Int32 i = 0; i < byteLength; i++)
                outBuffer2[i % height * stride + i / height] = outBuffer[i];
            // outBuffer2 is now the correct image.
            return outBuffer2;

        }

        public static Byte[] EncodeImage(Byte[] buffer, Int32 stride)
        {
            AgosCompression rle = new AgosCompression();
            Int32 byteLength = buffer.Length;
            Int32 height = byteLength / stride;
            // Should not happen, but you never know...
            if (byteLength > height * stride)
                height++;
            Byte[] buffer2 = new Byte[byteLength];
            // Pre-processing: Exchange rows and columns.
            for (Int32 i = 0; i < byteLength; i++)
                buffer2[i] = buffer[i % height * stride + i / height];
            // buffer2 is now the image, with its columns stored as rows.
            return rle.RleEncodeData(buffer2);
        }
        #region tweaked overrides

        /// <summary>Maximum amount of repeating bytes that can be stored in one code.</summary>
        public override UInt32 MaxRepeatValue { get { return 0x80; } }
        /// <summary>Maximum amount of copied bytes that can be stored in one code.</summary>
        public override UInt32 MaxCopyValue { get { return 0x7F; } }

        /// <summary>
        /// Reads a code, determines the repeat / skip command and the amount of bytes to to repeat/skip,
        /// and advances the read pointer to the location behind the read code.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="inPtr">Input pointer.</param>
        /// <param name="bufferEnd">End of buffer.</param>
        /// <param name="IsRepeat">Returns true for repeat code, false for copy code.</param>
        /// <param name="amount">Returns the amount to copy or repeat.</param>
        /// <returns>True if the read succeeded, false if it failed.</returns>
        protected override Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, UInt32 bufferEnd, out Boolean IsRepeat, out UInt32 amount)
        {
            Byte code = buffer[inPtr++];
            IsRepeat = (code & 0x80) == 0;
            if (IsRepeat)
                amount = (UInt32)(code + 1);
            else
                amount = (UInt32)(0x100 - code);
            return true;
        }

        /// <summary>
        /// Writes the copy/skip code to be put before the actual byte(s) to repeat/skip,
        /// and advances the write pointer to the location behind the written code.
        /// </summary>
        /// <param name="bufferOut">Output buffer to write to.</param>
        /// <param name="outPtr">Pointer for the output buffer.</param>
        /// <param name="forRepeat">True if this is a repeat code, false if this is a copy code.</param>
        /// <param name="amount">Amount to write into the repeat or copy code.</param>
        /// <returns>True if the write succeeded, false if it failed.</returns>
        protected override Boolean WriteCode(Byte[] bufferOut, ref UInt32 outPtr, Boolean forRepeat, UInt32 amount)
        {
            if (bufferOut.Length <= outPtr)
                return false;
            if (forRepeat)
                bufferOut[outPtr++] = (Byte)(amount - 1);
            else
                bufferOut[outPtr++] = (Byte)(0x100 - amount);
            return true;
        }
        #endregion

    }
}