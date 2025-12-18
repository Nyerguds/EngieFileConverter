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
        /// <param name="bufferEnd">End of buffer.</param>
        /// <param name="IsRepeat">Returns true for repeat code, false for copy code.</param>
        /// <param name="amount">Returns the amount to copy or repeat.</param>
        /// <returns>True if the read succeeded, false if it failed.</returns>
        protected override Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, UInt32 bufferEnd, out Boolean IsRepeat, out UInt32 amount)
        {
            Byte code = buffer[inPtr++];
            amount = (UInt32)(code & 0x7f);
            IsRepeat = (code & 0x80) == 0;
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
                bufferOut[outPtr++] = (Byte)(amount);
            else
                bufferOut[outPtr++] = (Byte)(amount | 0x80);
            return true;
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
            // But it is allowed for the sake of completeness.
            if (minimumRepeating < 2)
                minimumRepeating = 2;
            Int32 inPtr = 0;
            Int32 outPtr = 0;
            // Ensure big enough buffer. Sanity check will be done afterwards.
            Byte[] bufferOut = new Byte[(buffer.Length * 3) / 2];

            // RLE implementation:
            // highest bit not set = followed by range of repeating bytes
            // highest bit set = followed by range of non-repeating bytes
            // In both cases, the "code" specifies the amount of bytes; either to write, or to skip.
            Int32 len = buffer.Length;
            Boolean repeatDetected = false;
            while (inPtr < len)
            {
                if (repeatDetected || HasRepeatingAhead(buffer, len, inPtr, 2))
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
                    bufferOut[outPtr++] = (Byte)(inPtr - start);
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
                            if (!HasRepeatingAhead(buffer, len, inPtr, 3))
                                continue;
                            repeatDetected = true;
                            break;
                        }
                        bufferOut[outPtr++] = (Byte)((inPtr - start) | 0x80);
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