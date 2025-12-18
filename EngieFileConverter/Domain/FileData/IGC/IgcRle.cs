using System;
using Nyerguds.FileData.Compression;

namespace Nyerguds.FileData.IGC
{
    public class IgcRle : RleImplementation<IgcRle>
    {

        #region overridables to tweak in subclasses
        /// <summary>Maximum amount of repeating bytes that can be stored in one code.</summary>
        public override UInt32 MaxRepeatValue { get { return 0x7F; } }
        /// <summary>Maximum amount of copied bytes that can be stored in one code.</summary>
        public override UInt32 MaxCopyValue { get { return 0x7F; } }

        /// <summary>
        /// Reads a code, determines the repeat / copy command and the amount of bytes to repeat / copy,
        /// and advances the read pointer to the location behind the read code.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="inPtr">Input pointer.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be read from.</param>
        /// <param name="isRepeat">Returns true for repeat code, false for copy code.</param>
        /// <param name="amount">Returns the amount to copy or repeat.</param>
        /// <returns>True if the read succeeded, false if it failed.</returns>
        protected override Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, UInt32 bufferEnd, out Boolean isRepeat, out UInt32 amount)
        {
            if (inPtr >= bufferEnd)
            {
                isRepeat = false;
                amount = 0;
                return false;
            }
            Byte code = buffer[inPtr++];
            isRepeat = (code & 0x80) != 0;
            if (isRepeat)
                amount = (UInt32) (code & 0x7F);
            else
                amount = code;
            return true;
        }

        /// <summary>
        /// Writes the repeat / copy code to be put before the actual byte(s) to repeat / copy,
        /// and advances the write pointer to the location behind the written code.
        /// </summary>
        /// <param name="bufferOut">Output buffer to write to.</param>
        /// <param name="outPtr">Pointer for the output buffer.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be written to.</param>
        /// <param name="forRepeat">True if this is a repeat code, false if this is a copy code.</param>
        /// <param name="amount">Amount to write into the repeat or copy code.</param>
        /// <returns>True if the write succeeded, false if it failed.</returns>
        protected override Boolean WriteCode(Byte[] bufferOut, ref UInt32 outPtr, UInt32 bufferEnd, Boolean forRepeat, UInt32 amount)
        {
            if (outPtr >= bufferEnd)
                return false;
            if (forRepeat)
                bufferOut[outPtr++] = (Byte) (amount - 1 | 0x80);
            else
                bufferOut[outPtr++] = (Byte) (amount - 1);
            return true;
        }
        #endregion
    }
}