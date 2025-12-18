using System;
using System.Collections.Generic;

namespace Nyerguds.GameData.KotB
{
    public class EACompression
    {

        public static Int32 RleDecode(Byte[] buffer, Int32? startOffset, Int32? endOffset, Byte[] bufferOut)
        {
            Int32 inPtr = startOffset ?? 0;
            Int32 inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length;
            Int32 outPtr = 0;

            // RLE implementation:
            // highest bit not set = followed by range of repeating bytes
            // highest bit set = followed by range of non-repeating bytes
            // In both cases, the "code" specifies the amount of bytes; either to write, or to skip.

            while (inPtr < inPtrEnd)
            {
                // get next code
                Int32 code = buffer[inPtr++];
                Int32 run = code & 0x7f;
                if (run == 0) // Illegal command.
                    return -1;
                // Repeat
                if ((code & 0x80) == 0)
                {
                    if (inPtr >= inPtrEnd)
                        return outPtr; 
                    Int32 rle = buffer[inPtr++];
                    for (UInt32 lcv = 0; lcv < run; lcv++)
                    {
                        if (outPtr >= bufferOut.Length)
                            return outPtr;
                        bufferOut[outPtr++] = (Byte)rle;
                    }
                }
                // Copy
                else
                {
                    for (UInt32 lcv = 0; lcv < run; lcv++)
                    {
                        if (inPtr >= inPtrEnd)
                            return outPtr;
                        Int32 data = buffer[inPtr++];
                        if (outPtr >= bufferOut.Length)
                            return outPtr;
                        bufferOut[outPtr++] = (Byte)data;
                    }
                }
            }
            return outPtr;
        }

        private static Byte[] ExpandBuffer(Byte[] source, Int32 expand)
        {
            Int32 len = source.Length;
            Byte[] expanded = new Byte[len + expand];
            Array.Copy(source, expanded, len);
            return expanded;
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static Byte[] RleEncode(Byte[] buffer)
        {
            return RleEncode(buffer, 3);
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
                if (repeatDetected || HasRepeatingAhead(buffer, len, inPtr, minimumRepeating))
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
                            if (!HasRepeatingAhead(buffer, len, inPtr, minimumRepeating))
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