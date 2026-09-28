using System;

namespace Nyerguds.FileData.EmotionalPictures
{
    public static class PppCompression
    {

        public static byte[] DecompressPppRle(byte[] data)
        {
            int len = data.Length;
            int uncompressedSize = len * 3;
            uint expandSize = (uint)uncompressedSize;
            byte[] bufferOut = new byte[uncompressedSize];
            int ptr = 0;
            // Decompress flag-based RLE.
            // The flag is 0xFF. It is followed by one byte for the value to fill,
            // and then two bytes for the amount of repetitions.
            int i;
            for (i = 0; i < len; ++i)
            {
                byte value = data[i];
                if (value != 0xFF)
                {
                    if (ptr >= bufferOut.Length)
                        bufferOut = ExpandBuffer(bufferOut, expandSize);
                    bufferOut[ptr++] = value;
                }
                else
                {
                    if (i + 3 >= len)
                        throw new ArgumentException("Data ends on incomplete repeat command.", "data");
                    value = data[++i];
                    int repeat = data[++i] + (data[++i] << 8);
                    int endPoint = repeat + ptr;
                    // if the repeat amount is more than the expand size, there's no point in just expanding with the repeat amount;
                    // the next written byte will need to expand it again then anyway. So expand to their sum instead.
                    if (endPoint > bufferOut.Length)
                        bufferOut = ExpandBuffer(bufferOut, repeat >= expandSize ? (uint)repeat + expandSize : expandSize);
                    for (; ptr < endPoint; ++ptr)
                        bufferOut[ptr] = value;
                }
            }
            if (ptr < bufferOut.Length)
            {
                byte[] bufferSized = new byte[ptr];
                Array.Copy(bufferOut, 0, bufferSized, 0, ptr);
                bufferOut = bufferSized;
            }
            return bufferOut;
        }

        private static byte[] ExpandBuffer(byte[] bufferOut, uint expandSize)
        {
            byte[] newBuf = new byte[bufferOut.Length + expandSize];
            Array.Copy(bufferOut, 0, newBuf, 0, bufferOut.Length);
            return newBuf;
        }

        public static byte[] CompressPppRle(byte[] data)
        {
            int len = data.Length;
            // Compressed data normally never exceeds original size, since compression only triggers on sequences of 4 or more.
            // However, since 0xFF bytes always need to be encoded with a flag, a sequence of the type FF XX FF XX FF etc... needs to be taken into account.
            // Formula for this worse case scenario: "(len + 1) / 2 * 4 + (len / 2)" or "(((len + 1) >> 1) << 2) + (len >> 1)"
            // for now, we'll just count on expanding with check instead.
            int curBufLen = len;

            byte[] bufferOut = new byte[curBufLen];
            int ptr = 0;
            for (int i = 0; i < len; ++i)
            {
                byte value = data[i];
                int repeat = i;
                for (; repeat < len && data[repeat] == value; ++repeat) { }
                repeat -= i;
                bool compress = repeat >= 4 || value == 0xFF;
                int needed = ptr + (compress ? 3 : 0);
                if (curBufLen <= needed)
                {
                    // Expand buffer if needed.
                    int newLen = Math.Max(curBufLen + len, needed);
                    byte[] newCompressBuffer = new byte[newLen];
                    Array.Copy(bufferOut, 0, newCompressBuffer, 0, curBufLen);
                    curBufLen = newLen;
                }
                if (compress)
                {
                    i += repeat - 1; // -1 because the loop itself obviously increments it
                    do
                    {
                        int repeat16b = repeat > 0xFFFF ? 0xFFFF : repeat;
                        bufferOut[ptr++] = 0xFF;
                        bufferOut[ptr++] = value;
                        bufferOut[ptr++] = (byte)repeat16b;
                        bufferOut[ptr++] = (byte)(repeat16b >> 8);
                        repeat -= repeat16b;
                        // Fix for compressing too-small leftover repeats
                        if (repeat < 4 && value != 0xFF)
                            for (; repeat > 0; repeat--)
                                bufferOut[ptr++] = value;
                    } while (repeat > 0);
                }
                else
                    bufferOut[ptr++] = value;
            }
            byte[] bufferSized = new byte[ptr];
            Array.Copy(bufferOut, 0, bufferSized, 0, ptr);
            return bufferSized;
        }
    }
}