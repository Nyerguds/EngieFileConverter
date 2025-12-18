
using System;

namespace Nyerguds.FileData.EmotionalPictures
{
    public static class PppCompression
    {

        public static Byte[] DecompressPppRle(Byte[] data)
        {
            Int32 len = data.Length;
            UInt32 expandSize = (UInt32)len * 3;
            Int32 uncompressedSize = data.Length * 3;
            Byte[] bufferOut = new Byte[uncompressedSize];
            Int32 ptr = 0;
            // Decompress flag-based RLE.
            // The flag is 0xFF. It is followed by one byte for the value to fill,
            // and then two bytes for the amount of repetitions.
            Int32 i;
            for (i = 0; i < len; i++)
            {
                Byte value = data[i];
                if (value != 0xFF)
                {
                    if (ptr >= bufferOut.Length)
                        bufferOut = ExpandBuffer(bufferOut, expandSize);
                    bufferOut[ptr++] = value;
                }
                else
                {
                    if (i + 3 >= len)
                        throw new ArgumentException("Data ends on incomplete repeat command!", "data");
                    value = data[++i];
                    Int32 repeat = data[++i] + (data[++i] << 8);
                    Int32 endPoint = repeat + ptr;
                    if (endPoint > bufferOut.Length)
                        bufferOut = ExpandBuffer(bufferOut, Math.Max(expandSize, (UInt32)repeat));
                    for (; ptr < endPoint; ptr++)
                        bufferOut[ptr] = value;
                }
            }
            if (ptr < bufferOut.Length)
            {
                Byte[] bufferSized = new Byte[ptr];
                Array.Copy(bufferOut, 0, bufferSized, 0, ptr);
                bufferOut = bufferSized;
            }
            return bufferOut;
        }

        private static Byte[] ExpandBuffer(Byte[] bufferOut, UInt32 expandSize)
        {
            Byte[] newBuf = new Byte[bufferOut.Length + expandSize];
            Array.Copy(bufferOut, 0, newBuf, 0, bufferOut.Length);
            return newBuf;
        }
        
        public static Byte[] CompressPppRle(Byte[] data)
        {
            Int32 len = data.Length;
            Int32 curBufLen = len;
            // Compressed data should never exceed original size since compression only triggers on sequences of 4 or more,
            // though it could in case of 0xFF bytes, since they always need to be encoded with a flag.
            Byte[] bufferOut = new Byte[curBufLen];
            Int32 ptr = 0;
            for (Int32 i = 0; i < len; i++)
            {
                Byte value = data[i];
                Int32 repeat = i;
                for (; repeat < len && data[repeat] == value; repeat++) { }
                repeat -= i;
                Boolean compress = repeat >= 4 || value == 0xFF;
                Int32 needed = ptr + (compress ? 3 : 0);
                if (curBufLen <= needed)
                {
                    // Expand buffer if needed.
                    Int32 newLen = Math.Max(curBufLen + len, needed);
                    Byte[] newCompressBuffer = new Byte[newLen];
                    Array.Copy(bufferOut, 0, newCompressBuffer, 0, curBufLen);
                    curBufLen = newLen;
                }
                if (compress)
                {
                    i += repeat - 1; // -1 because the loop itself obviously increments it
                    do
                    {
                        Int32 repeat16b = repeat > 0xFFFF ? 0xFFFF : repeat;
                        bufferOut[ptr++] = 0xFF;
                        bufferOut[ptr++] = value;
                        bufferOut[ptr++] = (Byte)repeat16b;
                        bufferOut[ptr++] = (Byte)(repeat16b >> 8);
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
            Byte[] bufferSized = new Byte[ptr];
            Array.Copy(bufferOut, 0, bufferSized, 0, ptr);
            return bufferSized;
        }
    }
}