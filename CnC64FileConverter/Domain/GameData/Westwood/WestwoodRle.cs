using System;
using Nyerguds.Util.GameData;

namespace CnC64FileConverter.Domain.GameData.Westwood
{
    /// <summary>
    /// Westwood RLE implementation:
    /// highest bit set = followed by range of repeating bytes, but seen as (256-value)
    /// highest bit not set = followed by range of non-repeating bytes
    /// Value is 0: read 2 more bytes as Int16 to repeat.
    /// In all cases, the "code" specifies the amount of bytes; either to write, or to skip.
    /// </summary>
    public class WestwoodRle : RleImplementation<WestwoodRle>
    {
        public override UInt32 MaxRepeatValue { get { return UInt16.MaxValue; } }
        public override UInt32 MaxCopyValue { get { return 0x7F; } }

        protected Boolean m_SwapWords;

        public WestwoodRle()
        {
            this.m_SwapWords = false;
        }

        public WestwoodRle(Boolean swapWords)
        {
            this.m_SwapWords = swapWords;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode</param>
        /// <param name="startOffset">Start offset in buffer</param>
        /// <param name="endOffset">End offset in buffer</param>
        /// <param name="decompressedSize">The expected size of the decompressed data.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding them as big-endian.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static Byte[] RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 decompressedSize, Boolean swapWords, Boolean abortOnError)
        {
            WestwoodRle rle = new WestwoodRle(swapWords);
            return rle.RleDecodeData(buffer, startOffset, endOffset, decompressedSize, abortOnError);
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode</param>
        /// <param name="startOffset">Start offset in buffer</param>
        /// <param name="endOffset">End offset in buffer</param>
        /// <param name="bufferOut">Output array. Determines the maximum that can be decoded.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding them as big-endian.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return -1.</param>
        /// <returns>The amount of written bytes in bufferOut</returns>
        public static Int32 RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Byte[] bufferOut, Boolean swapWords, Boolean abortOnError)
        {
            WestwoodRle rle = new WestwoodRle(swapWords);
            return rle.RleDecodeData(buffer, startOffset, endOffset, bufferOut, abortOnError);
        }

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
            IsRepeat = ((code & 0x80) != 0 || code == 0);
            if (IsRepeat)
            {
                if (code == 0)
                {
                    // Westwood extension for 16-bit repeat values.
                    if (inPtr + 2 >= bufferEnd)
                    {
                        amount = 0;
                        return false;
                    }
                    amount = (UInt32)(m_SwapWords ? buffer[inPtr++] + (buffer[inPtr++] << 8) : (buffer[inPtr++] << 8) + buffer[inPtr++]);
                }
                else
                    amount = (UInt32)(0x100 - code);
            }
            else
            {
                amount = code;
            }
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
            {
                if (amount < 0x80)
                {
                    if (bufferEnd <= outPtr)
                        return false;
                    bufferOut[outPtr++] = (Byte)((0x100 - amount) | 0x80);
                }
                else
                {
                    if (bufferOut.Length <= outPtr + 2)
                        return false;
                    Byte lenHi = (Byte)((amount >> 8) & 0xFF);
                    Byte lenLo = (Byte)(amount & 0xFF);
                    bufferOut[outPtr++] = 0;
                    bufferOut[outPtr++] = m_SwapWords ? lenLo : lenHi;
                    bufferOut[outPtr++] = m_SwapWords ? lenHi : lenLo;
                }
            }
            else
            {
                bufferOut[outPtr++] = (Byte)(amount);
            }
            return true;
        }
    }
}