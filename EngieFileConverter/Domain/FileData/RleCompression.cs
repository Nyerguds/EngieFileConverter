using System;

namespace Nyerguds.FileData.Compression
{
    #region Implementation

    /// <summary>
    /// Basic implementation of Run-Length Encoding with the highest bit set for the Repeat code.
    /// The used run length is always (code &amp; 0x7F).
    /// </summary>
    public class RleCompressionHighBitRepeat : RleImplementation<RleCompressionHighBitRepeat> { }

    /// <summary>
    /// Basic implementation of Run-Length Encoding with the highest bit set for the Copy code.
    /// The used run length is always (code &amp; 0x7F).
    /// </summary>
    /// <remarks>This uses the original GetCode/WriteCode functions but simply flips their "Repeat" boolean.</remarks>
    public class RleCompressionHighBitCopy : RleImplementation<RleCompressionHighBitCopy>
    {
        protected override bool GetCode(byte[] buffer, ref uint inPtr, ref uint bufferEnd, out bool isRepeat, out uint amount)
        {
            bool success = base.GetCode(buffer, ref inPtr, ref bufferEnd, out isRepeat, out amount);
            isRepeat = !isRepeat;
            return success;
        }

        protected override bool WriteCode(byte[] bufferOut, ref uint outPtr, uint bufferEnd, bool forRepeat, uint amount)
        {
            return base.WriteCode(bufferOut, ref outPtr, bufferEnd, !forRepeat, amount);
        }
    }
    #endregion

    #region Main RLE class

    /// <summary>
    /// Basic code-based (or token-based) run-length encoding algorithm.
    /// Written by Maarten Meuris, aka Nyerguds.
    /// This class allows easy overriding of the code to read and write codes, to
    /// allow flexibility in subclassing the system for different RLE implementations.
    /// </summary>
    /// <typeparam name="T">
    /// The implementing class. This trick allows access to the internal type and its constructor from static functions
    /// in the superclass, giving the subclasses access to static functions that still use the specific subclass behaviour.
    /// </typeparam>
    public abstract class RleImplementation<T> where T : RleImplementation<T>, new()
    {
        #region overridables to tweak in subclasses
        /// <summary>Maximum amount of repeating bytes that can be stored in one code.</summary>
        protected virtual uint MaxRepeatValue { get { return 0x7F; } }
        /// <summary>Maximum amount of copied bytes that can be stored in one code.</summary>
        protected virtual uint MaxCopyValue { get { return 0x7F; } }

        /// <summary>Worst case output buffer size for compressed content, calculated from input data size.</summary>
        protected virtual uint CompressionWorstCase(uint inputSize)
        {
            // Worst-case for this function is probably alternating blocks of 1 non-repeating and 3 repeating,
            // which would expand 4 bytes to 6; 3/2, or 150%. Just to be safe, the buffer is set to 7/4, or 175%.
            // This technically depends on the WriteCode implementation, but in general this should be okay.
            return inputSize * 7 / 4;
        }

        /// <summary>
        /// Reads a code, determines the repeat / copy command and the amount of bytes to repeat / copy,
        /// and advances the read pointer to the location behind the read code.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="inPtr">Input pointer.</param>
        /// <param name="bufferEnd">Exclusive end of buffer; first position that can no longer be read from. Writable because some compression types might want to force the process to end depending on the code.</param>
        /// <param name="isRepeat">Returns true for repeat code, false for copy code.</param>
        /// <param name="amount">Returns the amount to copy or repeat.</param>
        /// <returns>True if the read succeeded, false if it failed.</returns>
        protected virtual bool GetCode(byte[] buffer, ref uint inPtr, ref uint bufferEnd, out bool isRepeat, out uint amount)
        {
            if (inPtr >= bufferEnd)
            {
                isRepeat = false;
                amount = 0;
                return false;
            }
            byte code = buffer[inPtr++];
            isRepeat = (code & 0x80) != 0;
            amount = (uint)(code & 0x7f);
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
        protected virtual bool WriteCode(byte[] bufferOut, ref uint outPtr, uint bufferEnd, bool forRepeat, uint amount)
        {
            if (outPtr >= bufferEnd)
                return false;
            if (forRepeat)
                bufferOut[outPtr++] = (byte)(amount | 0x80);
            else
                bufferOut[outPtr++] = (byte)(amount);
            return true;
        }
        #endregion

        #region static functions
        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static byte[] RleDecode(byte[] buffer, bool abortOnError)
        {
            T rle = new T();
            byte[] bufferOut = null;
            rle.RleDecodeData(buffer, null, null, ref bufferOut, abortOnError);
            return bufferOut;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Start offset in buffer.</param>
        /// <param name="endOffset">End offset in buffer.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, bool abortOnError)
        {
            T rle = new T();
            byte[] bufferOut = null;
            rle.RleDecodeData(buffer, startOffset, endOffset, ref bufferOut, abortOnError);
            return bufferOut;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Start offset in buffer.</param>
        /// <param name="endOffset">End offset in buffer.</param>
        /// <param name="decompressedSize">The expected size of the decompressed data.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data.</returns>
        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool abortOnError)
        {
            T rle = new T();
            return rle.RleDecodeData(buffer, startOffset, endOffset, decompressedSize, abortOnError);
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Start offset in buffer.</param>
        /// <param name="endOffset">End offset in buffer.</param>
        /// <param name="bufferOut">Output array. Determines the maximum that can be decoded. If the given object is null it will be filled automatically.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>The amount of written bytes in bufferOut.</returns>
        public static int RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, ref byte[] bufferOut, bool abortOnError)
        {
            T rle = new T();
            return rle.RleDecodeData(buffer, startOffset, endOffset, ref bufferOut, abortOnError);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <returns>The run-length encoded data.</returns>
        public static byte[] RleEncode(byte[] buffer)
        {
            T rle = new T();
            return rle.RleEncodeData(buffer);
        }
        #endregion

        #region public functions
        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Inclusive start offset in buffer. Defaults to 0.</param>
        /// <param name="endOffset">Exclusive end offset in buffer. Defaults to the buffer length.</param>
        /// <param name="decompressedSize">The expected size of the decompressed data. Leave 0 to make an auto-expanding buffer.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return null.</param>
        /// <returns>A byte array of the given output size, filled with the decompressed data, or null if abortOnError is enabled and an empty command was found.</returns>
        public byte[] RleDecodeData(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool abortOnError)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            byte[] outputBuffer = decompressedSize <= 0 ? null : new byte[decompressedSize];
            int result = this.RleDecodeData(buffer, startOffset, endOffset, ref outputBuffer, abortOnError);
            if (result == -1)
                return null;
            return outputBuffer;
        }

        /// <summary>
        /// Decodes RLE-encoded data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="startOffset">Inclusive start offset in buffer. Defaults to 0.</param>
        /// <param name="endOffset">Exclusive end offset in buffer. Defaults to the buffer length.</param>
        /// <param name="bufferOut">Output array. Determines the maximum that can be decoded. Leave null to create a buffer automaically and expand it when needed.</param>
        /// <param name="abortOnError">If true, any found command with amount "0" in it will cause the process to abort and return -1.</param>
        /// <returns>The amount of written bytes in bufferOut.</returns>
        public int RleDecodeData(byte[] buffer, uint? startOffset, uint? endOffset, ref byte[] bufferOut, bool abortOnError)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            uint inPtr = startOffset ?? 0;
            uint inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, (uint)buffer.Length) : (uint)buffer.Length;

            uint outPtr = 0;
            bool autoExpand = bufferOut == null;
            uint bufLenOrig = inPtrEnd - inPtr;
            if (autoExpand)
                bufferOut = new byte[bufLenOrig * 4];
            uint maxOutLen = autoExpand? uint.MaxValue : (uint)bufferOut.Length;
            bool error = false;

            while (inPtr < inPtrEnd && outPtr < maxOutLen)
            {
                // get next code
                uint run;
                bool repeat;
                if (!this.GetCode(buffer, ref inPtr, ref inPtrEnd, out repeat, out run) || (run == 0 && abortOnError))
                {
                    error = true;
                    break;
                }
                //End ptr after run
                uint runEnd = Math.Min(outPtr + run, maxOutLen);
                if (autoExpand && runEnd > bufferOut.Length)
                    bufferOut = ExpandBuffer(bufferOut, Math.Max(bufLenOrig, runEnd));
                // Repeat run
                if (repeat)
                {
                    if (inPtr >= inPtrEnd)
                        break;
                    int repeatVal = buffer[inPtr++];
                    for (; outPtr < runEnd; ++outPtr)
                        bufferOut[outPtr] = (byte)repeatVal;
                    if (outPtr == maxOutLen)
                        break;
                }
                // Raw copy
                else
                {
                    bool abort = false;
                    for (; outPtr < runEnd; ++outPtr)
                    {
                        if (inPtr >= inPtrEnd)
                        {
                            abort = true;
                            break;
                        }
                        int data = buffer[inPtr++];
                        bufferOut[outPtr] = (byte)data;
                    }
                    if (abort)
                        break;
                    if (outPtr == maxOutLen)
                        break;
                }
            }
            if (error)
                return -1;
            if (autoExpand)
            {
                byte[] newBuf = new byte[outPtr];
                Array.Copy(bufferOut, 0, newBuf, 0, outPtr);
                bufferOut = newBuf;
            }
            return (int)outPtr;
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <remarks>
        /// This function achieves better compression than most other methods, because it only switches from a Copy command
        /// to a Repeat command if more than two repeating bytes are found (or if the maximum copy amount is reached).
        /// This avoids adding extra Copy command bytes after replacing two repeating bytes by a two-byte Repeat command.
        /// Written by Maarten Meuris, aka Nyerguds.
        /// </remarks>
        /// <param name="buffer">Input buffer.</param>
        /// <returns>The run-length encoded data.</returns>
        public byte[] RleEncodeData(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            return this.RleEncodeData(buffer, 0, (uint)buffer.Length);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <remarks>
        /// This function achieves better compression than other methods out there, because it only switches from a Copy
        /// command to a Repeat command if more than two repeating bytes are found (or if the maximum copy amount is reached).
        /// This avoids adding extra Copy command bytes after replacing two repeating bytes by a two-byte Repeat command.
        /// Written by Maarten Meuris, aka Nyerguds.
        /// </remarks>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="dataStart">Start of the data inside the buffer.</param>
        /// <param name="dataEnd">End of the data inside the buffer.</param>
        /// <returns>The run-length encoded data.</returns>
        public byte[] RleEncodeData(byte[] buffer, uint dataStart, uint dataEnd)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            uint dataLen = (uint)buffer.Length;
            uint inPtr = Math.Min(dataLen, dataStart);
            uint outPtr = 0;
            // Retrieve these in advance to avoid extra calls to getters.
            // These are made customizable because some implementations support larger codes. Technically
            // neither run-length 0 nor 1 are useful for repeat codes (0 should not exist, 1 is identical to copy),
            // so the values are often decremented to allow storing one or two more bytes.
            // Some implementations also use these values as indicators for reading a larger value to repeat or copy.
            uint maxRepeat = this.MaxRepeatValue;
            uint maxCopy = this.MaxCopyValue;
            uint len = Math.Min(dataLen, dataEnd);
            // This code does not do sanity checks, since some file formats can't disable their compression.
            uint bufLen = this.CompressionWorstCase(len);
            byte[] bufferOut = new byte[bufLen];
            uint detectedRepeat = 0;
            while (inPtr < len)
            {
                // Handle 2 cases: repeat was already detected, or a new repeat detect needs to be done.
                if (detectedRepeat >= 2 || (detectedRepeat = RepeatingAhead(buffer, len, inPtr, 2)) == 2)
                {
                    // Found more than 2 bytes. Worth compressing. Apply run-length encoding.
                    uint start = inPtr;
                    uint end = Math.Min(inPtr + maxRepeat, len);
                    byte cur = buffer[inPtr];
                    // Already checked these in the RepeatingAhead function.
                    inPtr += detectedRepeat;
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == cur; ++inPtr) { }
                    // WriteCode is split off into a function to allow overriding it in specific implementations.
                    // After the code is written, test if there is still a byte free to add the value to repeat. Shouldn't happen, but better be sure.
                    if (!this.WriteCode(bufferOut, ref outPtr, bufLen, true, (inPtr - start)) || outPtr + 1 >= bufLen)
                        break;
                    // Add value to repeat
                    bufferOut[outPtr++] = cur;
                    // Reset for next run
                    detectedRepeat = 0;
                }
                else
                {
                    bool abort = false;
                    // if detectedRepeat is not greater than 1 after writing a code,
                    // that means the maximum copy length was reached. Keep repeating
                    // until the copy is aborted for a repeat.
                    while (detectedRepeat == 1 && inPtr < len)
                    {
                        uint start = inPtr;
                        // Normal non-repeat detection logic.
                        uint end = Math.Min(inPtr + maxCopy, len);
                        uint maxend = inPtr + maxCopy;
                        inPtr += detectedRepeat;
                        while (inPtr < end)
                        {
                            // detected bytes to compress after this one: abort.
                            detectedRepeat = RepeatingAhead(buffer, len, inPtr, 3);
                            // Only switch to Repeat when finding three repeated bytes: if the data
                            // behind a repeat of two is non-repeating, it adds an extra Copy command.
                            if (detectedRepeat == 3)
                                break;
                            // Optimise: apply a 1-byte or 2-byte skip to ptr right away.
                            inPtr += detectedRepeat;
                            // A detected repeat of two could make it go beyond the maximum accepted number of
                            // stored bytes per code. This fixes that. These repeating bytes are always saved as
                            // Repeat code, since a new command needs to be added after ending this one anyway.
                            // If you'd use the copy max amount instead, the 2-repeat would be cut in two Copy
                            // commands, wasting one byte if another repeating range would start after it.
                            if (inPtr > maxend)
                            {
                                inPtr -= detectedRepeat;
                                break;
                            }
                        }
                        uint amount = inPtr - start;
                        if (amount == 0)
                        {
                            abort = true;
                            break;
                        }
                        // Need to reset this if the copy commands aborts for full size, so a last-detected
                        // repeat value of 2 at the end of a copy range isn't propagated to a new repeat command.
                        if (amount == maxCopy)
                            detectedRepeat = 0;
                        // WriteCode is split off into a function to allow overriding it in specific implementations.
                        // After the code is written, test if there is still space to add the values to copy. Shouldn't happen, but better be sure.
                        abort = !this.WriteCode(bufferOut, ref outPtr, bufLen, false, amount) || outPtr + amount >= bufLen;
                        if (abort)
                            break;
                        // Add values to copy
                        for (uint i = start; i < inPtr; ++i)
                            bufferOut[outPtr++] = buffer[i];
                    }
                    if (abort)
                        break;
                }
            }
            byte[] finalOut = new byte[outPtr];
            Array.Copy(bufferOut, 0, finalOut, 0, outPtr);
            return finalOut;
        }
        #endregion

        #region internal tools

        private static byte[] ExpandBuffer(byte[] bufferOut, uint expandSize)
        {
            byte[] newBuf = new byte[bufferOut.Length + expandSize];
            Array.Copy(bufferOut, 0, newBuf, 0, bufferOut.Length);
            return newBuf;
        }

        /// <summary>
        /// Checks if there are enough repeating bytes ahead.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="max">The maximum offset to read inside the buffer.</param>
        /// <param name="ptr">The current read offset inside the buffer.</param>
        /// <param name="minAmount">Minimum amount of repeating bytes to search for.</param>
        /// <returns>The amount of detected repeating bytes.</returns>
        protected static uint RepeatingAhead(byte[] buffer, uint max, uint ptr, uint minAmount)
        {
            byte cur = buffer[ptr];
            for (uint i = 1; i < minAmount; ++i)
                if (ptr + i >= max || buffer[ptr + i] != cur)
                    return i;
            return minAmount;
        }
        #endregion
    }

    #endregion
}