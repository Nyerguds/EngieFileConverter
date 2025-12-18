using System;
using System.IO;

namespace Nyerguds.FileData.Westwood
{
    /// <summary>
    /// Class for the transparency-collapsing RLE methods used in Dune II and Tiberian Sun.
    /// </summary>
    public class WestwoodRleZero
    {

        public static Byte[] DecompressRleZeroTs(Byte[] fileData, ref Int32 offset, Int32 frameWidth, Int32 frameHeight)
        {
            Byte[] finalImage = new Byte[frameWidth * frameHeight];
            Int32 datalen = fileData.Length;
            Int32 outLineOffset = 0;
            for (Int32 y = 0; y < frameHeight; ++y)
            {
                Int32 outOffset = outLineOffset;
                Int32 nextLineOffset = outLineOffset + frameWidth;
                if (offset + 2 >= datalen)
                    throw new ArgumentException("Not enough lines in RLE-Zero data.", "fileData");
                // Compose little-endian UInt16 from 2 bytes
                Int32 lineLen = fileData[offset] | (fileData[offset + 1] << 8);
                Int32 end = offset + lineLen;
                if (lineLen < 2 || end > datalen)
                    throw new ArgumentException("Bad value in RLE-Zero line header.", "fileData");
                // Skip header
                offset += 2;
                Boolean readZero = false;
                for (; offset < end; ++offset)
                {
                    if (outOffset >= nextLineOffset)
                        throw new ArgumentException("Bad line alignment in RLE-Zero data.", "fileData");
                    if (readZero)
                    {
                        // Zero has been read. Process 0-repeat.
                        readZero = false;
                        Int32 zeroes = fileData[offset];
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

        public static Byte[] CompressRleZeroTs(Byte[] imageData, Int32 frameWidth, Int32 frameHeight)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Int32 inputLineOffset = 0;
                for (Int32 y = 0; y < frameHeight; ++y)
                {
                    Int64 lineStartOffs = ms.Position;
                    ms.Position = lineStartOffs + 2;
                    Int32 inputOffset = inputLineOffset;
                    Int32 nextLineOffset = inputOffset + frameWidth;
                    while (inputOffset < nextLineOffset)
                    {
                        Byte b = imageData[inputOffset];
                        if (b == 0)
                        {
                            Int32 startOffs = inputOffset;
                            Int32 max = Math.Min(startOffs + 256, nextLineOffset);
                            for (; inputOffset < max && imageData[inputOffset] == 0; ++inputOffset) { }
                            ms.WriteByte(0);
                            Int32 skip = inputOffset - startOffs;
                            ms.WriteByte((Byte)(skip));
                        }
                        else
                        {
                            ms.WriteByte(b);
                            inputOffset++;
                        }
                    }
                    // Go back to start of the line data and fill in the length.
                    Int64 lineEndOffs = ms.Position;
                    Int64 len = lineEndOffs - lineStartOffs;
                    if (len > UInt16.MaxValue)
                        throw new ArgumentException("Compressed line width is too large to store.", "imageData");
                    ms.Position = lineStartOffs;
                    ms.WriteByte((Byte)(len & 0xFF));
                    ms.WriteByte((Byte) ((len >> 8) & 0xFF));
                    ms.Position = lineEndOffs;
                    inputLineOffset = nextLineOffset;
                }
                return ms.ToArray();
            }
        }

        public static Byte[] DecompressRleZeroD2(Byte[] fileData, ref Int32 offset, Int32 frameWidth, Int32 frameHeight)
        {
            Int32 fullLength = frameWidth * frameHeight;
            Byte[] finalImage = new Byte[fullLength];
            Int32 datalen = fileData.Length;
            Int32 outLineOffset = 0;
            for (Int32 y = 0; y < frameHeight; ++y)
            {
                Int32 outOffset = outLineOffset;
                Int32 nextLineOffset = outLineOffset + frameWidth;
                Boolean readZero = false;
                for (; offset < datalen; ++offset)
                {
                    if (outOffset >= nextLineOffset)
                        break;
                    if (readZero)
                    {
                        readZero = false;
                        Int32 zeroes = fileData[offset];
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

        public static Byte[] CompressRleZeroD2(Byte[] imageData, Int32 frameWidth, Int32 frameHeight)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Int32 inputLineOffset = 0;
                for (Int32 y = 0; y < frameHeight; ++y)
                {
                    Int32 inputOffset = inputLineOffset;
                    Int32 nextLineOffset = inputOffset + frameWidth;
                    while (inputOffset < nextLineOffset)
                    {
                        Byte b = imageData[inputOffset];
                        if (b == 0)
                        {
                            Int32 startOffs = inputOffset;
                            Int32 max = Math.Min(startOffs + 256, nextLineOffset);
                            for (; inputOffset < max && imageData[inputOffset] == 0; ++inputOffset) { }
                            ms.WriteByte(0);
                            Int32 skip = inputOffset - startOffs;
                            ms.WriteByte((Byte)(skip));
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
