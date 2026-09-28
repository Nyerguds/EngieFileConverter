using System;
using System.IO;

namespace Nyerguds.FileData.Westwood
{
    /// <summary>
    /// Class for the transparency-collapsing RLE methods used in Dune II and Tiberian Sun.
    /// </summary>
    public class WestwoodRleZero
    {

        public static byte[] DecompressRleZeroTs(byte[] fileData, ref int offset, int frameWidth, int frameHeight)
        {
            byte[] finalImage = new byte[frameWidth * frameHeight];
            int datalen = fileData.Length;
            int outLineOffset = 0;
            for (int y = 0; y < frameHeight; ++y)
            {
                int outOffset = outLineOffset;
                int nextLineOffset = outLineOffset + frameWidth;
                if (offset + 2 >= datalen)
                    throw new ArgumentException("Not enough lines in RLE-Zero data.", "fileData");
                // Compose little-endian UInt16 from 2 bytes
                int lineLen = fileData[offset] | (fileData[offset + 1] << 8);
                int end = offset + lineLen;
                if (lineLen < 2 || end > datalen)
                    throw new ArgumentException("Bad value in RLE-Zero line header.", "fileData");
                // Skip header
                offset += 2;
                bool readZero = false;
                for (; offset < end; ++offset)
                {
                    if (outOffset >= nextLineOffset)
                        throw new ArgumentException("Bad line alignment in RLE-Zero data.", "fileData");
                    if (readZero)
                    {
                        // Zero has been read. Process 0-repeat.
                        readZero = false;
                        int zeroes = fileData[offset];
                        for (; zeroes > 0 && outOffset < nextLineOffset; zeroes--)
                            finalImage[outOffset++] = 0;
                    }
                    else if (fileData[offset] == 0)
                    {
                        // Rather than manually increasing the offset, just flag that
                        // "a 0 value has been read" so the next loop can read the repeat value.
                        readZero = true;
                    }
                    else
                    {
                        // Simply copy a value.
                        finalImage[outOffset++] = fileData[offset];
                    }
                }
                // If a data line ended on a 0, there's something wrong.
                if (readZero)
                    throw new ArgumentException("Incomplete zero-repeat command.", "fileData");
                outLineOffset = nextLineOffset;
            }
            return finalImage;
        }

        public static byte[] CompressRleZeroTs(byte[] imageData, int frameWidth, int frameHeight)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int inputLineOffset = 0;
                for (int y = 0; y < frameHeight; ++y)
                {
                    long lineStartOffs = ms.Position;
                    ms.Position = lineStartOffs + 2;
                    int inputOffset = inputLineOffset;
                    int nextLineOffset = inputOffset + frameWidth;
                    while (inputOffset < nextLineOffset)
                    {
                        byte b = imageData[inputOffset];
                        if (b == 0)
                        {
                            int startOffs = inputOffset;
                            int max = Math.Min(startOffs + 256, nextLineOffset);
                            for (; inputOffset < max && imageData[inputOffset] == 0; ++inputOffset) { }
                            ms.WriteByte(0);
                            int skip = inputOffset - startOffs;
                            ms.WriteByte((byte)(skip));
                        }
                        else
                        {
                            ms.WriteByte(b);
                            inputOffset++;
                        }
                    }
                    // Go back to start of the line data and fill in the length.
                    long lineEndOffs = ms.Position;
                    long len = lineEndOffs - lineStartOffs;
                    if (len > ushort.MaxValue)
                        throw new ArgumentException("Compressed line width is too large to store.", "imageData");
                    ms.Position = lineStartOffs;
                    ms.WriteByte((byte)(len & 0xFF));
                    ms.WriteByte((byte) ((len >> 8) & 0xFF));
                    ms.Position = lineEndOffs;
                    inputLineOffset = nextLineOffset;
                }
                return ms.ToArray();
            }
        }

        public static byte[] DecompressRleZeroD2(byte[] fileData, ref int offset, int frameWidth, int frameHeight)
        {
            int fullLength = frameWidth * frameHeight;
            byte[] finalImage = new byte[fullLength];
            int datalen = fileData.Length;
            int outLineOffset = 0;
            for (int y = 0; y < frameHeight; ++y)
            {
                int outOffset = outLineOffset;
                int nextLineOffset = outLineOffset + frameWidth;
                bool readZero = false;
                for (; offset < datalen; ++offset)
                {
                    if (outOffset >= nextLineOffset)
                        break;
                    if (readZero)
                    {
                        readZero = false;
                        int zeroes = fileData[offset];
                        for (; zeroes > 0 && outOffset < nextLineOffset; zeroes--)
                            finalImage[outOffset++] = 0;
                    }
                    else if (fileData[offset] == 0)
                    {
                        readZero = true;
                    }
                    else
                    {
                        finalImage[outOffset++] = fileData[offset];
                    }
                }
                outLineOffset = nextLineOffset;
            }
            return finalImage;
        }

        public static byte[] CompressRleZeroD2(byte[] imageData, int frameWidth, int frameHeight)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int inputLineOffset = 0;
                for (int y = 0; y < frameHeight; ++y)
                {
                    int inputOffset = inputLineOffset;
                    int nextLineOffset = inputOffset + frameWidth;
                    while (inputOffset < nextLineOffset)
                    {
                        byte b = imageData[inputOffset];
                        if (b == 0)
                        {
                            int startOffs = inputOffset;
                            int max = Math.Min(startOffs + 256, nextLineOffset);
                            for (; inputOffset < max && imageData[inputOffset] == 0; ++inputOffset) { }
                            ms.WriteByte(0);
                            int skip = inputOffset - startOffs;
                            ms.WriteByte((byte)(skip));
                        }
                        else
                        {
                            ms.WriteByte(b);
                            inputOffset++;
                        }
                    }
                    inputLineOffset = nextLineOffset;
                }
                return ms.ToArray();
            }
        }

    }
}
