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
        public static byte[] FlagRleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool abortOnError)
        {
            uint offset = startOffset ?? 0;
            uint end = (uint) buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            uint origOutLength = decompressedSize != 0 ? (uint) decompressedSize : ((end - offset) * 4);
            uint outLength = origOutLength;
            byte[] output = new byte[outLength];
            uint writeOffset = 0;
            if (end - offset < 3)
                return abortOnError ? null : new byte[0];
            // Skip size bytes
            offset += 2;
            // Get flag byte
            byte flag = buffer[offset++];
            while (offset < end)
            {
                byte val = buffer[offset++];
                if (val == flag)
                {
                    if (offset + 1 >= end)
                    {
                        if (abortOnError)
                            return null;
                        break;
                    }
                    byte repeatVal = buffer[offset++];
                    byte repeatNum = buffer[offset++];
                    if (outLength < writeOffset + repeatNum)
                    {
                        if (abortOnError && decompressedSize != 0)
                            return null;
                        output = ExpandBuffer(output, Math.Max(origOutLength, repeatNum));
                        outLength = (uint) output.LongLength;
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
                        outLength = (uint) output.LongLength;
                    }
                    output[writeOffset++] = val;
                }
            }
            if (abortOnError && decompressedSize != 0 && decompressedSize != writeOffset)
                return null;
            if (writeOffset < output.Length)
            {
                byte[] finalOut = new byte[writeOffset];
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
        public static byte[] FlagRleEncode(byte[] buffer, byte flag, int lineWidth, int headerSize)
        {
            if (headerSize + 3 >= 0x10000)
                throw new ArgumentException("Header too big.", "headerSize");
            uint outLen = (uint)(0x10000 - headerSize - 3);
            byte[] bufferOut = new byte[outLen];
            uint len = (uint) buffer.Length;
            uint inPtr = 0;
            uint outPtr = 0;
            uint rowWidth = (lineWidth == 0) ? len : (uint) lineWidth;
            uint curLineEnd = rowWidth;
            while (inPtr < len)
            {
                if (outLen == outPtr)
                    throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                byte cur = buffer[inPtr];
                // only one pixel required to write a repeat code if the value is the flag.
                uint requiredRepeat = (uint) (cur == flag ? 1 : 3);
                uint detectedRepeat;
                if ((curLineEnd - inPtr >= requiredRepeat) && (detectedRepeat = RepeatingAhead(buffer, len, inPtr, requiredRepeat)) == requiredRepeat)
                {
                    // Found more than 2 bytes (or a flag byte). Worth compressing. Apply run-length encoding.
                    uint start = inPtr;
                    uint end = Math.Min(inPtr + 0xFF, curLineEnd);
                    // Already checked these in the RepeatingAhead function.
                    inPtr += detectedRepeat;
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == cur; ++inPtr) { }
                    uint repeat = inPtr - start;
                    // check buffer overflow
                    if (outLen <= outPtr + 3)
                        throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                    // write code
                    bufferOut[outPtr++] = flag;
                    // Add value to repeat
                    bufferOut[outPtr++] = cur;
                    // add amount of repeats.
                    bufferOut[outPtr++] = (byte) repeat;
                }
                else
                {
                    bufferOut[outPtr++] = cur;
                    inPtr++;
                }
                if (inPtr == curLineEnd)
                    curLineEnd = inPtr + rowWidth;
            }
            byte[] finalOut = new byte[outPtr + 3];
            Array.Copy(bufferOut, 0, finalOut, 3, outPtr);
            outPtr += 3 + (uint) headerSize;
            if (outPtr > ushort.MaxValue)
                throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
            // Store size in first two bytes.
            finalOut[0] = (byte) (outPtr & 0xFF);
            finalOut[1] = (byte) ((outPtr >> 8) & 0xFF);
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
        public static byte[] CollapsedTransparencyDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, int lineWidth, byte transparentIndex, bool abortOnError)
        {
            uint offset = startOffset ?? 0;
            uint end = (uint)buffer.LongLength;
            if (endOffset.HasValue)
                end = Math.Min(endOffset.Value, end);
            uint origOutLength = decompressedSize != 0 ? (uint) decompressedSize : ((end - offset) * 4);
            uint outLength = origOutLength;
            byte[] output = new byte[outLength];
            uint writeOffset = 0;
            // Skip size bytes and unused flag byte
            offset += 3;
            uint curLineEnd = (uint) lineWidth;
            while (offset < end)
            {
                // Handle fill part
                byte fillSize = buffer[offset++];
                if (outLength < writeOffset + fillSize)
                {
                    if (abortOnError && decompressedSize != 0)
                        return null;
                    output = ExpandBuffer(output, origOutLength);
                    outLength = (uint) output.LongLength;
                }
                for (; fillSize > 0; fillSize--)
                    output[writeOffset++] = transparentIndex;
                // Handle copy part
                if (writeOffset >= curLineEnd)
                {
                    if (writeOffset != curLineEnd && abortOnError)
                        return null;
                    writeOffset = curLineEnd;
                    curLineEnd += (uint) lineWidth;
                    continue;
                }
                if (offset >= end) // also view as error? Dunno if the format does that.
                    break;
                byte copySize = buffer[offset++];
                if (end < offset + copySize)
                {
                    if (abortOnError)
                        return null;
                    copySize = (byte) (end - offset);
                }
                if (outLength < writeOffset + copySize)
                {
                    if (abortOnError && decompressedSize != 0)
                        return null;
                    output = ExpandBuffer(output, origOutLength);
                    outLength = (uint) output.LongLength;
                }
                Array.Copy(buffer, offset, output, writeOffset, copySize);
                offset += copySize;
                writeOffset += copySize;
                if (writeOffset >= curLineEnd)
                {
                    if (writeOffset != curLineEnd && abortOnError)
                        return null;
                    writeOffset = curLineEnd;
                    curLineEnd += (uint) lineWidth;
                }
            }
            if (abortOnError && decompressedSize != 0 && decompressedSize != writeOffset)
                return null;
            if (writeOffset < output.Length)
            {
                byte[] finalOut = new byte[writeOffset];
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
        public static byte[] CollapsedTransparencyEncode(byte[] buffer, byte transparentIndex, int lineWidth, int headerSize)
        {
            if (headerSize + 3 >= 0x10000)
                throw new ArgumentException("Header too big.", "headerSize");
            uint outLen = (uint)(0x10000 - headerSize - 3);
            byte[] bufferOut = new byte[outLen];
            uint len = (uint) buffer.Length;
            uint inPtr = 0;
            uint outPtr = 0;
            uint rowWidth = (uint) lineWidth;
            uint curLineEnd = rowWidth;
            bool writingTransparency = true;
            while (inPtr < len)
            {
                if (outLen == outPtr)
                    throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
                byte cur = buffer[inPtr];
                bool isTrans = cur == transparentIndex;
                if (writingTransparency && isTrans)
                {
                    // Get repeat length. Limit to current line end.
                    uint start = inPtr;
                    uint end = Math.Min(inPtr + 0xFF, curLineEnd);
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] == transparentIndex; ++inPtr) { }
                    // write repeat value
                    bufferOut[outPtr++] = (byte) (inPtr - start);
                }
                else if (!writingTransparency && !isTrans)
                {
                    // Get copy length. Limit to current line end.
                    uint start = inPtr;
                    uint end = Math.Min(inPtr + 0xFF, curLineEnd);
                    // Increase inptr to the last repeated.
                    for (; inPtr < end && buffer[inPtr] != transparentIndex; ++inPtr) { }
                    // write repeat value
                    byte copySize = (byte) (inPtr - start);
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
            byte[] finalOut = new byte[outPtr + 3];
            Array.Copy(bufferOut, 0, finalOut, 3, outPtr);
            outPtr += 3 + (uint) headerSize;
            if (outPtr > ushort.MaxValue)
                throw new ArgumentException("Compressed data is too big to be stored as Mythos compressed format.", "buffer");
            // Store size in first two bytes.
            finalOut[0] = (byte) (outPtr & 0xFF);
            finalOut[1] = (byte) ((outPtr >> 8) & 0xFF);
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
        private static uint RepeatingAhead(byte[] buffer, uint max, uint ptr, uint minAmount)
        {
            byte cur = buffer[ptr];
            for (uint i = 1; i < minAmount; ++i)
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
        private static byte[] ExpandBuffer(byte[] buffer, uint expandSize)
        {
            byte[] newBuf = new byte[buffer.Length + expandSize];
            Array.Copy(buffer, 0, newBuf, 0, buffer.Length);
            return newBuf;
        }
    }
}