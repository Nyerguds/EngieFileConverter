using System;

namespace Nyerguds.FileData.Mythos
{
    public static class MythosCompression
    {
        /// <summary>
        /// Decodes the Mythos Software flag-based RLE compression.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="startOffset">Start offset. Leave null to start at the start.</param>
        /// <param name="endOffset">End offset. Leave null to take the length of the buffer.</param>
        /// <param name="decompressedSize">Decompressed size. If given, the initial output buffer will be initialised to this.</param>
        /// <param name="abortOnError">Abort and return null whenever an error occurs. If a decompressedSize was given, it will also abort when exceeding it.</param>
        /// <returns>The decoded data, or null if decoding failed.</returns>
        public static Byte[] FlagRleDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 decompressedSize, Boolean abortOnError)
        {
            UInt32 offset = startOffset ?? 0;
            UInt32 end = (UInt32) buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            UInt32 origOutLength = decompressedSize != 0 ? (UInt32) decompressedSize : ((end - offset) * 4);
            UInt32 outLength = origOutLength;
            Byte[] output = new Byte[outLength];
            UInt32 writeOffset = 0;
            if (end - offset < 3)
                return abortOnError ? null : new Byte[0];
            // Skip size bytes
            offset += 2;
            // Get flag byte
            Byte flag = buffer[offset++];
            while (offset < end)
            {
                Byte val = buffer[offset++];
                if (val == flag)
                {
                    if (offset + 1 >= end)
                    {
                        if (abortOnError)
                            return null;
                        break;
                    }
                    Byte repeatVal = buffer[offset++];
                    Byte repeatNum = buffer[offset++];
                    if (outLength < writeOffset + repeatNum)
                    {
                        if (abortOnError && decompressedSize != 0)
                            return null;
                        output = ExpandBuffer(output, Math.Max(origOutLength, repeatNum));
                        outLength = (UInt32) output.LongLength;
                    }
                    for (; repeatNum > 0; repeatNum--)
                        output[writeOffset++] = repeatVal;
                }
                else
                {
                    if (outLength <= writeOffset)
                    {
                        if (abortOnError && decompressedSize != 0)
                            return null;
                        output = ExpandBuffer(output, origOutLength);
                        outLength = (UInt32) output.LongLength;
                    }
                    output[writeOffset++] = val;
                }
            }
            if (abortOnError && decompressedSize != 0 && decompressedSize != writeOffset)
                return null;
            if (writeOffset < output.Length)
            {
                Byte[] finalOut = new Byte[writeOffset];
                Array.Copy(output, 0, finalOut, 0, writeOffset);
                output = finalOut;
            }
            return output;
        }

        /// <summary>
        /// Encodes data to the Mythos Software flag-based RLE compression.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="flag">Byte to use as flag value.</param>
        /// <param name="lineWidth">Line width. If not zero, the compression will be aligned to fit into separate rows.</param>
        /// <param name="headerSize">Header size, to correctly put the full block length at the start.</param>
        /// <returns>The encoded data.</returns>
        public static Byte[] FlagRleEncode(Byte[] buffer, Byte flag, Int32 lineWidth, Int32 headerSize)
        {
            if (headerSize + 3 >= 0x10000)
                throw new ArgumentException("Header too big.", "headerSize");
            UInt32 outLen = (UInt32)(0x10000 - headerSize - 3);
            Byte[] bufferOut = new Byte[outLen];
            UInt32 len = (UInt32) buffer.Length;
            UInt32 inPtr = 0;
            UInt32 outPtr = 0;
            UInt32 rowWidth = (lineWidth == 0) ? len : (UInt32) lineWidth;
            UInt32 curLineEnd = rowWidth;
            while (inPtr < len)
            {
                if (outLen == outPtr)
                    throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                Byte cur = buffer[inPtr];
                // only one pixel required to write a repeat code if the value is the flag.
                UInt32 requiredRepeat = (UInt32) (cur == flag ? 1 : 3);
                UInt32 detectedRepeat;
                if ((curLineEnd - inPtr >= requiredRepeat) && (detectedRepeat = RepeatingAhead(buffer, len, inPtr, requiredRepeat)) == requiredRepeat)
                {
                    // Found more than 2 bytes (or a flag byte). Worth compressing. Apply run-length encoding.
                    UInt32 start = inPtr;
                    UInt32 end = Math.Min(inPtr + 0xFF, curLineEnd);
                    // Already checked these in the RepeatingAhead function.
                    inPtr += detectedRepeat;
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == cur; ++inPtr) { }
                    UInt32 repeat = inPtr - start;
                    // check buffer overflow
                    if (outLen <= outPtr + 3)
                        throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                    // write code
                    bufferOut[outPtr++] = flag;
                    // Add value to repeat
                    bufferOut[outPtr++] = cur;
                    // add amount of repeats.
                    bufferOut[outPtr++] = (Byte) repeat;
                }
                else
                {
                    bufferOut[outPtr++] = cur;
                    inPtr++;
                }
                if (inPtr == curLineEnd)
                    curLineEnd = inPtr + rowWidth;
            }
            Byte[] finalOut = new Byte[outPtr + 3];
            Array.Copy(bufferOut, 0, finalOut, 3, outPtr);
            outPtr += 3 + (UInt32) headerSize;
            if (outPtr > UInt16.MaxValue)
                throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
            // Store size in first two bytes.
            finalOut[0] = (Byte) (outPtr & 0xFF);
            finalOut[1] = (Byte) ((outPtr >> 8) & 0xFF);
            // Store flag value in third byte.
            finalOut[2] = flag;
            return finalOut;
        }

        /// <summary>
        /// Decodes the Mythos Software transparency-collapsing RLE compression.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="startOffset">Start offset. Leave null to start at the start.</param>
        /// <param name="endOffset">End offset. Leave null to take the length of the buffer.</param>
        /// <param name="decompressedSize">Decompressed size. If given, the initial output buffer will be initialised to this.</param>
        /// <param name="lineWidth">Byte length of one line of image data.</param>
        /// <param name="transparentIndex">Transparency value to collapse.</param>
        /// <param name="abortOnError">Abort and return null whenever an error occurs. If a decompressedSize was given, it will also abort when exceeding it.</param>
        /// <returns>The decoded data, or null if decoding failed.</returns>
        public static Byte[] CollapsedTransparencyDecode(Byte[] buffer, UInt32? startOffset, UInt32? endOffset, Int32 decompressedSize, Int32 lineWidth, Byte transparentIndex, Boolean abortOnError)
        {
            UInt32 offset = startOffset ?? 0;
            UInt32 end = (UInt32)buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            UInt32 origOutLength = decompressedSize != 0 ? (UInt32) decompressedSize : ((end - offset) * 4);
            UInt32 outLength = origOutLength;
            Byte[] output = new Byte[outLength];
            UInt32 writeOffset = 0;
            // Skip size bytes and unused flag byte
            offset += 3;
            UInt32 curLineEnd = (UInt32) lineWidth;
            while (offset < end)
            {
                // Handle fill part
                Byte fillSize = buffer[offset++];
                if (outLength < writeOffset + fillSize)
                {
                    if (abortOnError && decompressedSize != 0)
                        return null;
                    output = ExpandBuffer(output, origOutLength);
                    outLength = (UInt32) output.LongLength;
                }
                for (; fillSize > 0; fillSize--)
                    output[writeOffset++] = transparentIndex;
                // Handle copy part
                if (writeOffset >= curLineEnd)
                {
                    if (writeOffset != curLineEnd && abortOnError)
                        return null;
                    writeOffset = curLineEnd;
                    curLineEnd += (UInt32) lineWidth;
                    continue;
                }
                if (offset >= end) // also view as error? Dunno if the format does that.
                    break;
                Byte copySize = buffer[offset++];
                if (end < offset + copySize)
                {
                    if (abortOnError)
                        return null;
                    copySize = (Byte) (end - offset);
                }
                if (outLength < writeOffset + copySize)
                {
                    if (abortOnError && decompressedSize != 0)
                        return null;
                    output = ExpandBuffer(output, origOutLength);
                    outLength = (UInt32) output.LongLength;
                }
                Array.Copy(buffer, offset, output, writeOffset, copySize);
                offset += copySize;
                writeOffset += copySize;
                if (writeOffset >= curLineEnd)
                {
                    if (writeOffset != curLineEnd && abortOnError)
                        return null;
                    writeOffset = curLineEnd;
                    curLineEnd += (UInt32) lineWidth;
                }
            }
            if (abortOnError && decompressedSize != 0 && decompressedSize != writeOffset)
                return null;
            if (writeOffset < output.Length)
            {
                Byte[] finalOut = new Byte[writeOffset];
                Array.Copy(output, 0, finalOut, 0, writeOffset);
                output = finalOut;
            }
            return output;
        }

        /// <summary>
        /// Encodes data to the Mythos Software transparency-collapsing RLE compression.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="transparentIndex">Transparency value to collapse.</param>
        /// <param name="lineWidth">Line width.</param>
        /// <param name="headerSize">Header size, to correctly put the full block length at the start. Should normally be '8'.</param>
        /// <returns>The encoded data.</returns>
        public static Byte[] CollapsedTransparencyEncode(Byte[] buffer, Byte transparentIndex, Int32 lineWidth, Int32 headerSize)
        {
            if (headerSize + 3 >= 0x10000)
                throw new ArgumentException("Header too big.", "headerSize");
            UInt32 outLen = (UInt32)(0x10000 - headerSize - 3);
            Byte[] bufferOut = new Byte[outLen];
            UInt32 len = (UInt32) buffer.Length;
            UInt32 inPtr = 0;
            UInt32 outPtr = 0;
            UInt32 rowWidth = (UInt32) lineWidth;
            UInt32 curLineEnd = rowWidth;
            Boolean writingTransparency = true;
            while (inPtr < len)
            {
                if (outLen == outPtr)
                    throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                Byte cur = buffer[inPtr];
                Boolean isTrans = cur == transparentIndex;
                if (writingTransparency && isTrans)
                {
                    // Get repeat length. Limit to current line end.
                    UInt32 start = inPtr;
                    UInt32 end = Math.Min(inPtr + 0xFF, curLineEnd);
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == transparentIndex; ++inPtr) { }
                    // write repeat value
                    bufferOut[outPtr++] = (Byte) (inPtr - start);
                }
                else if (!writingTransparency && !isTrans)
                {
                    // Get copy length. Limit to current line end.
                    UInt32 start = inPtr;
                    UInt32 end = Math.Min(inPtr + 0xFF, curLineEnd);
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] != transparentIndex; ++inPtr) { }
                    // write repeat value
                    Byte copySize = (Byte) (inPtr - start);
                    bufferOut[outPtr++] = copySize;
                    // Boundary checking
                    if (outLen < outPtr + copySize)
                        throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                    // Write uncollapsed data
                    Array.Copy(buffer, start, bufferOut, outPtr, copySize);
                    outPtr += copySize;
                }
                else
                {
                    // Somehow writing transparent while in non-transparent mode or vice versa. Could happen
                    // if a line starts with non-transparent, or the amount of consecutive transparent pixels
                    // exceeds 255. Just set a 0 and continue without incrementing the read ptr.
                    bufferOut[outPtr++] = 0;
                }
                if (inPtr >= len)
                    break;
                if (inPtr == curLineEnd)
                {
                    // Reset to next row
                    curLineEnd = inPtr + rowWidth;
                    writingTransparency = true;
                }
                else
                {
                    // Switch between transparency and opaque data.
                    writingTransparency = !writingTransparency;
                }
            }
            Byte[] finalOut = new Byte[outPtr + 3];
            Array.Copy(bufferOut, 0, finalOut, 3, outPtr);
            outPtr += 3 + (UInt32) headerSize;
            if (outPtr > UInt16.MaxValue)
                throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
            // Store size in first two bytes.
            finalOut[0] = (Byte) (outPtr & 0xFF);
            finalOut[1] = (Byte) ((outPtr >> 8) & 0xFF);
            // Store (unused) flag value in third byte.
            finalOut[2] = 0xFE;
            return finalOut;
        }

        /// <summary>
        /// Checks if there are enough repeating bytes ahead.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="max">The maximum offset to read inside the buffer.</param>
        /// <param name="ptr">The current read offset inside the buffer.</param>
        /// <param name="minAmount">Minimum amount of repeating bytes to search for.</param>
        /// <returns>The amount of detected repeating bytes.</returns>
        private static UInt32 RepeatingAhead(Byte[] buffer, UInt32 max, UInt32 ptr, UInt32 minAmount)
        {
            Byte cur = buffer[ptr];
            for (UInt32 i = 1; i < minAmount; ++i)
                if (ptr + i >= max || buffer[ptr + i] != cur)
                    return i;
            return minAmount;
        }

        /// <summary>
        /// Expands the buffer by copying its contents into a new, larger byte array.
        /// </summary>
        /// <param name="buffer">Buffer to expand.</param>
        /// <param name="expandSize">amount of bytes to add to the buffer.</param>
        /// <returns>The expanded buffer.</returns>
        private static Byte[] ExpandBuffer(Byte[] buffer, UInt32 expandSize)
        {
            Byte[] newBuf = new Byte[buffer.Length + expandSize];
            Array.Copy(buffer, 0, newBuf, 0, buffer.Length);
            return newBuf;
        }
    }
}