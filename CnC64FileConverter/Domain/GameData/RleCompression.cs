using System;

namespace Nyerguds.Util.GameData
{
    /// <summary>
    /// Basic implementation of Run-Length Encoding.
    /// Highest bit set is the Repeat code, and the used run length is always (code & 0x7F).
    /// </summary>
    public class RleCompression: RleImplementation<RleCompression> { }

    /// <summary>
    /// Basic Run-Length Encoding algorithm. Written by Maarten Meuris, aka Nyerguds.
    /// This class allows easy overriding of the code to read and write codes, to
    /// allow flexibility in subclassing the system for different RLE implementations.
    /// </summary>
    /// <typeparam name="T">The implementing class. This trick allows access the internal type and its constructor from static functions in the superclass</typeparam>
    public abstract class RleImplementation<T> where T : RleImplementation<T>, new()
    {
        #region constants
        /// <summary>
        /// The standard value for the mimimum amount of repeating bytes is three.
        /// </summary>
        public const UInt32 STANDARD_REPEAT_DETECT = 3;

        #endregion

        #region overridables to tweak in subclasses
        /// <summary>Maximum amount of repeating bytes that can be stored in one code.</summary>
        public virtual UInt32 MaxRepeatValue { get { return 0x7F; } }
        /// <summary>Maximum amount of copied bytes that can be stored in one code.</summary>
        public virtual UInt32 MaxCopyValue { get { return 0x7F; } }

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
        protected virtual Boolean GetCode(Byte[] buffer, ref UInt32 inPtr, UInt32 bufferEnd, out Boolean IsRepeat, out UInt32 amount)
        {
            Byte code = buffer[inPtr++];
            amount = (UInt32)(code & 0x7f);
            IsRepeat = (code & 0x80) != 0;
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
        protected virtual Boolean WriteCode(Byte[] bufferOut, ref UInt32 outPtr, Boolean forRepeat, UInt32 amount)
        {
            if (bufferOut.Length <= outPtr)
                return false;
            if (forRepeat)
                bufferOut[outPtr++] = (Byte)(amount | 0x80);
            else
                bufferOut[outPtr++] = (Byte)(amount);
            return true;
        }
        #endregion

        #region static functions

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode</param>
        /// <param name="startOffset">Start offset in buffer</param>
        /// <param name="endOffset">End offset in buffer</param>
        /// <param name="decompressedSize">The expected size of the decompressed data.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static Byte[] RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 decompressedSize, Boolean abortOnError)
        {
            T rle = new T();
            return rle.RleDecodeData(buffer, startOffset, endOffset, decompressedSize, abortOnError);
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode</param>
        /// <param name="startOffset">Start offset in buffer</param>
        /// <param name="endOffset">End offset in buffer</param>
        /// <param name="bufferOut">Output array. Determines the maximum that can be decoded.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>The amount of written bytes in bufferOut</returns>
        public static Int32 RleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Byte[] bufferOut, Boolean abortOnError)
        {
            T rle = new T();
            return rle.RleDecodeData(buffer, startOffset, endOffset, bufferOut, abortOnError);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] RleEncode(Byte[] buffer)
        {
            T rle = new T();
            return rle.RleEncodeData(buffer);
        }
        #endregion

        #region public functions
        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode</param>
        /// <param name="startOffset">Start offset in buffer</param>
        /// <param name="endOffset">End offset in buffer</param>
        /// <param name="decompressedSize">The expected size of the decompressed data.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data, or null if abortOnError is enabled and an empty command was found.</returns>
        public Byte[] RleDecodeData(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 decompressedSize, Boolean abortOnError)
        {
            Byte[] outputBuffer = new Byte[decompressedSize];
            Int32 result = this.RleDecodeData(buffer, startOffset, endOffset, outputBuffer, abortOnError);
            if (result == -1)
                return null;
            return outputBuffer;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode</param>
        /// <param name="startOffset">Start offset in buffer</param>
        /// <param name="endOffset">End offset in buffer</param>
        /// <param name="bufferOut">Output array. Determines the maximum that can be decoded.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return -1.</param>
        /// <returns>The amount of written bytes in bufferOut</returns>
        public Int32 RleDecodeData(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Byte[] bufferOut, Boolean abortOnError)
        {
            UInt32 inPtr = startOffset ?? 0;
            UInt32 inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, (UInt32)buffer.Length) : (UInt32)buffer.Length;
            Int32 outPtr = 0;
            Int32 maxOutLen = bufferOut.Length;

            // RLE implementation:
            // highest bit set = followed by range of repeating bytes
            // highest bit not set = followed by range of non-repeating bytes
            // In both cases, the "code" specifies the amount of bytes; either to write, or to skip.

            while (outPtr < maxOutLen && inPtr < inPtrEnd)
            {
                // get next code
                UInt32 run;
                Boolean repeat;
                if (!this.GetCode(buffer, ref inPtr, inPtrEnd, out repeat, out run))
                    return -1;
                if (run == 0 && abortOnError)
                    return -1;
                //End ptr after run
                UInt32 runEnd = Math.Min((UInt32)(outPtr + run), (UInt32)maxOutLen);
                // Repeat run
                if (repeat)
                {
                    if (inPtr >= inPtrEnd)
                        return outPtr;
                    Int32 repeatVal = buffer[inPtr++];
                    for (; outPtr < runEnd; outPtr++)
                        bufferOut[outPtr] = (Byte)repeatVal;
                    if (outPtr == maxOutLen)
                        return outPtr;
                }
                // Raw copy
                else
                {
                    for (; outPtr < runEnd; outPtr++)
                    {
                        if (inPtr >= inPtrEnd)
                            return outPtr;
                        Int32 data = buffer[inPtr++];
                        bufferOut[outPtr] = (Byte)data;
                    }
                    if (outPtr == maxOutLen)
                        return outPtr;
                }
            }
            return outPtr;
        }
        
        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public Byte[] RleEncodeData(Byte[] buffer)
        {
            UInt32 inPtr = 0;
            UInt32 outPtr = 0;
            // Ensure big enough buffer. Sanity check will be done afterwards.
            Int32 bufLen = (buffer.Length * 3) / 2;
            Byte[] bufferOut = new Byte[bufLen];

            // Retrieve these in advance to avoid extra calls to getters.
            // These are made customizable because some implementations support larger codes. Technically
            // neither run-length 0 nor 1 are useful for repeat codes (0 should not exist, 1 is identical to copy),
            // so these two values could be used as indicators for reading a larger value to repeat or copy.
            // Some implementations also decrement the repeat code value to allow storing one or two more bytes.
            UInt32 maxRepeat = this.MaxRepeatValue;
            UInt32 maxCopy = this.MaxCopyValue;

            // Standard RLE implementation:
            // highest bit set = followed by range of repeating bytes
            // highest bit not set = followed by range of non-repeating bytes
            // In both cases, the "code" specifies the amount of bytes; either to write, or to skip.
            UInt32 len = (UInt32)buffer.Length;
            UInt32 detectedRepeat = 0;
            while (inPtr < len)
            {
                // Handle 2 cases: repeat was already detected, or a new repeat detect needs to be done.
                if (detectedRepeat >= 2 || (detectedRepeat = RepeatingAhead(buffer, len, inPtr, 2)) == 2)
                {
                    // Found more than (minimumRepeating) bytes. Worth compressing. Apply run-length encoding.
                    UInt32 start = inPtr;
                    UInt32 end = Math.Min(inPtr + maxRepeat, len);
                    Byte cur = buffer[inPtr];
                    // Already checked these in the HasRepeatingAhead function.
                    inPtr += detectedRepeat;
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == cur; inPtr++) { }
                    // WriteCode is split off into a function to allow overriding it in specific implementations.
                    if (!this.WriteCode(bufferOut, ref outPtr, true, (inPtr - start)) || outPtr + 1 >= bufLen)
                        break;
                    // Add value to repeat
                    bufferOut[outPtr++] = cur;
                    // Reset for next run
                    detectedRepeat = 0;
                }
                else
                {
                    Boolean abort = false;
                    while (detectedRepeat == 1 && inPtr < len)
                    {
                        UInt32 start = inPtr;
                        // Normal non-repeat detection logic.
                        UInt32 end = Math.Min(inPtr + maxCopy, len);
                        UInt32 maxend = inPtr + maxCopy;
                        inPtr += detectedRepeat;
                        while (inPtr < end)
                        {
                            // detected bytes to compress after this one: abort.
                            detectedRepeat = RepeatingAhead(buffer, len, inPtr, 3);
                            if (detectedRepeat < 3)
                            {
                                // Optimise: apply 2-byte skips to ptr right away.
                                inPtr += detectedRepeat;
                                // The detected repeat could make it go beyond the max accepted number of stored bytes per code.
                                // This fixes that.
                                if (inPtr >= maxend)
                                {
                                    inPtr -= detectedRepeat;
                                    //inPtr = maxend - 1;
                                    break;
                                }
                                continue;
                            }
                            break;
                        }
                        UInt32 amount = inPtr - start;
                        if (amount == 0)
                        {
                            abort = true;
                            break;
                        }
                        // WriteCode is split off into a function to allow overriding it in specific implementations.
                        abort = !this.WriteCode(bufferOut, ref outPtr, false, amount) || outPtr + amount >= bufLen;
                        if (abort)
                            break;
                        // Add values to copy
                        for (UInt32 i = start; i < inPtr; i++)
                            bufferOut[outPtr++] = buffer[i];
                    }
                    if (abort)
                        break;
                }
            }
            Byte[] finalOut = new Byte[outPtr];
            Array.Copy(bufferOut, 0, finalOut, 0, outPtr);
            return finalOut;
        }
        #endregion

        #region internal tools
        /// <summary>
        /// Checks if there are repeating bytes ahead.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="max">Maximum offset to read inside the buffer.</param>
        /// <param name="ptr">The current read offset inside the buffer.</param>
        /// <param name="minAmount">Minimum amount of repeating bytes before True is returned.</param>
        /// <returns>True if there are at least the requested amount of repeating bytes ahead.</returns>
        protected static UInt32 RepeatingAhead(Byte[] buffer, UInt32 max, UInt32 ptr, UInt32 minAmount)
        {
            Byte cur = buffer[ptr];
            for (UInt32 i = 1; i < minAmount; i++)
                if (ptr + i >= max || buffer[ptr + i] != cur)
                    return i;
            return minAmount;
        }
        #endregion

   }
}