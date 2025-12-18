using System;
using System.Collections.Generic;
using Nyerguds.Util.GameData;

namespace Nyerguds.GameData.KotB
{
    public class EACompression : RleImplementation<EACompression>
    {
        /// <summary>
        /// Reads a code, determines the repeat / skip command and the amount of bytes to to repeat/skip,
        /// and advances the read pointer to the location behind the read code.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="inPtr">Input pointer.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be read from.</param>
        /// <param name="IsRepeat">Returns true for repeat code, false for copy code.</param>
        /// <param name="amount">Returns the amount to copy or repeat.</param>
        /// <returns>True if the read succeeded, false if it failed.</returns>
        protected override Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, UInt32 bufferEnd, out Boolean IsRepeat, out UInt32 amount)
        {
            if (inPtr >= bufferEnd)
            {
                IsRepeat = false;
                amount = 0;
                return false;
            }
            Byte code = buffer[inPtr++];
            IsRepeat = (code & 0x80) == 0;
            amount = (UInt32)(code & 0x7f);
            return true;
        }

        /// <summary>
        /// Writes the copy/skip code to be put before the actual byte(s) to repeat/skip,
        /// and advances the write pointer to the location behind the written code.
        /// </summary>
        /// <param name="bufferOut">Output buffer to write to.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be written to.</param>
        /// <param name="outPtr">Pointer for the output buffer.</param>
        /// <param name="forRepeat">True if this is a repeat code, false if this is a copy code.</param>
        /// <param name="amount">Amount to write into the repeat or copy code.</param>
        /// <returns>True if the write succeeded, false if it failed.</returns>
        protected override Boolean WriteCode(Byte[] bufferOut, UInt32 bufferEnd, ref UInt32 outPtr, Boolean forRepeat, UInt32 amount)
        {
            if (outPtr >= bufferEnd)
                return false;
            if (forRepeat)
                bufferOut[outPtr++] = (Byte)(amount);
            else
                bufferOut[outPtr++] = (Byte)(amount | 0x80);
            return true;
        }

    }
}