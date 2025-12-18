using System;
using Nyerguds.FileData.Compression;

namespace Nyerguds.FileData.Westwood
{
    /// <summary>
    /// Westwood RLE implementation:
    /// highest code bit set = followed by a single byte to repeat. Amount is (0x100-Code)
    /// highest code bit not set = followed by range of non-repeating bytes. Amount to copy and skip is the code value.
    /// Code is 0 = read 2 more bytes to get an Int16 amount, and perform a repeat command on the byte following that.
    /// </summary>
    public class WestwoodRle : RleImplementation<WestwoodRle>
    {
        protected override UInt32 MaxRepeatValue { get { return UInt16.MaxValue; } }
        protected override UInt32 MaxCopyValue { get { return 0x7F; } }

        protected Boolean m_SwapWords;

        /// <summary>
        /// Initialises a new WestwoodRLE compression object, with the "swap words" option defaulting to true (PC format).
        /// </summary>
        public WestwoodRle()
        {
            this.m_SwapWords = true;
        }

        /// <summary>
        /// Initialises a new WestwoodRLE compression object.
        /// </summary>
        /// <param name="swapWords">True to use little-endian (PC format) for 16-bit values.</param>
        public WestwoodRle(Boolean swapWords)
        {
            this.m_SwapWords = swapWords;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Start offset in buffer.</param>
        /// <param name="endOffset">End offset in buffer.</param>
        /// <param name="decompressedSize">The expected size of the decompressed data.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, decoding them as little-endian.</param>
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
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Start offset in buffer.</param>
        /// <param name="endOffset">End offset in buffer.</param>
        /// <param name="bufferOut">Output array. Determines the maximum that can be decoded.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, decoding them as little-endian.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return -1.</param>
        /// <returns>The amount of written bytes in bufferOut.</returns>
        public static Int32 RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Byte[] bufferOut, Boolean swapWords, Boolean abortOnError)
        {
            WestwoodRle rle = new WestwoodRle(swapWords);
            return rle.RleDecodeData(buffer, startOffset, endOffset, ref bufferOut, abortOnError);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding them as little-endian.</param>
        /// <returns>The run-length encoded data.</returns>
        public static Byte[] RleEncode(Byte[] buffer, Boolean swapWords)
        {
            WestwoodRle rle = new WestwoodRle(swapWords);
            return rle.RleEncodeData(buffer);
        }

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
        protected override Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, ref UInt32 bufferEnd, out Boolean isRepeat, out UInt32 amount)
        {
            if (inPtr >= bufferEnd)
            {
                isRepeat = false;
                amount = 0;
                return false;
            }
            Byte code = buffer[inPtr++];
            isRepeat = ((code & 0x80) != 0 || code == 0);
            if (!isRepeat)
                amount = code;
            else if (code != 0)
                amount = (UInt32)(0x100 - code);
            else
            {
                // Westwood extension for 16-bit repeat values.
                if (inPtr + 2 >= bufferEnd)
                {
                    amount = 0;
                    return false;
                }
                amount = (UInt32)(this.m_SwapWords ? buffer[inPtr++] + (buffer[inPtr++] << 8) : (buffer[inPtr++] << 8) + buffer[inPtr++]);
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
        protected override Boolean WriteCode(Byte[] bufferOut, ref UInt32 outPtr, UInt32 bufferEnd, Boolean forRepeat, UInt32 amount)
        {
            if (outPtr >= bufferEnd)
                return false;
            if (forRepeat)
            {
                if (amount < 0x80)
                {
                    bufferOut[outPtr++] = (Byte)((0x100 - amount) | 0x80);
                }
                else
                {
                    if (outPtr + 2 >= bufferEnd)
                        return false;
                    Byte lenHi = (Byte)((amount >> 8) & 0xFF);
                    Byte lenLo = (Byte)(amount & 0xFF);
                    bufferOut[outPtr++] = 0;
                    bufferOut[outPtr++] = this.m_SwapWords ? lenLo : lenHi;
                    bufferOut[outPtr++] = this.m_SwapWords ? lenHi : lenLo;
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