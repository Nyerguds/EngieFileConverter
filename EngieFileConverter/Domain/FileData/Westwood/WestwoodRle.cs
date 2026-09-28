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
        protected override uint MaxRepeatValue { get { return ushort.MaxValue; } }
        protected override uint MaxCopyValue { get { return 0x7F; } }

        protected bool m_SwapWordsLE;

        /// <summary>
        /// Initialises a new WestwoodRLE compression object, with the "swap words" option defaulting to false (PC format).
        /// </summary>
        public WestwoodRle()
        {
            this.m_SwapWordsLE = false;
        }

        /// <summary>
        /// Initialises a new WestwoodRLE compression object.
        /// </summary>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding
        /// and decoding them as little-endian. Note that on PC, these are normally handled as big-endian.</param>
        public WestwoodRle(bool swapWords)
        {
            this.m_SwapWordsLE = swapWords;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Start offset in buffer.</param>
        /// <param name="endOffset">End offset in buffer.</param>
        /// <param name="decompressedSize">The expected size of the decompressed data.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding
        /// and decoding them as little-endian. Note that on PC, these are normally handled as big-endian.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool swapWords, bool abortOnError)
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
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding
        /// and decoding them as little-endian. Note that on PC, these are normally handled as big-endian.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return -1.</param>
        /// <returns>The amount of written bytes in bufferOut.</returns>
        public static int RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, ref byte[] bufferOut, bool swapWords, bool abortOnError)
        {
            WestwoodRle rle = new WestwoodRle(swapWords);
            return rle.RleDecodeData(buffer, startOffset, endOffset, ref bufferOut, abortOnError);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="swapWords">Swaps the bytes of the long-repetition Int16 values, encoding
        /// and decoding them as little-endian. Note that on PC, these are normally handled as big-endian.</param>
        /// <returns>The run-length encoded data.</returns>
        public static byte[] RleEncode(byte[] buffer, bool swapWords)
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
        protected override bool GetCode(byte[] buffer, ref uint inPtr, ref uint bufferEnd, out bool isRepeat, out uint amount)
        {
            if (inPtr >= bufferEnd)
            {
                isRepeat = false;
                amount = 0;
                return false;
            }
            byte code = buffer[inPtr++];
            isRepeat = ((code & 0x80) != 0 || code == 0);
            if (!isRepeat)
                amount = code;
            else if (code != 0)
                amount = (uint)(0x100 - code);
            else
            {
                // Westwood extension for 16-bit repeat values.
                if (inPtr + 2 >= bufferEnd)
                {
                    amount = 0;
                    return false;
                }
                amount = (uint)(this.m_SwapWordsLE ? buffer[inPtr++] + (buffer[inPtr++] << 8) : (buffer[inPtr++] << 8) + buffer[inPtr++]);
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
        protected override bool WriteCode(byte[] bufferOut, ref uint outPtr, uint bufferEnd, bool forRepeat, uint amount)
        {
            if (outPtr >= bufferEnd)
                return false;
            if (forRepeat)
            {
                if (amount < 0x80)
                {
                    bufferOut[outPtr++] = (byte)((0x100 - amount) | 0x80);
                }
                else
                {
                    if (outPtr + 2 >= bufferEnd)
                        return false;
                    byte lenHi = (byte)((amount >> 8) & 0xFF);
                    byte lenLo = (byte)(amount & 0xFF);
                    bufferOut[outPtr++] = 0;
                    bufferOut[outPtr++] = this.m_SwapWordsLE ? lenLo : lenHi;
                    bufferOut[outPtr++] = this.m_SwapWordsLE ? lenHi : lenLo;
                }
            }
            else
            {
                bufferOut[outPtr++] = (byte)(amount);
            }
            return true;
        }
    }
}