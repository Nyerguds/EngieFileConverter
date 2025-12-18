using Nyerguds.Util;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Image loading toolset class which corrects the bug that prevents paletted PNG images with transparency from being loaded as paletted.
    /// </summary>
    public static class BitmapHandler
    {
        private static Byte[] PNG_IDENTIFIER = {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};
        private static Byte[] PNG_BLANK = {0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01};

        /// <summary>
        /// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
        /// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
        /// </summary>
        /// <param name="filename">Filename to load.</param>
        /// <returns>The loaded image.</returns>
        public static Bitmap LoadBitmap(String filename)
        {
            Byte[] data = File.ReadAllBytes(filename);
            return LoadBitmap(data);
        }

        /// <summary>
        /// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
        /// The theory on the png internals can be found at http://www.libpng.org/pub/png/book/chapter08.html
        /// </summary>
        /// <param name="data">File data to load.</param>
        /// <returns>The loaded image.</returns>
        public static Bitmap LoadBitmap(Byte[] data)
        {
            Byte[] transparencyData = null;
            Int32 trnsOffset = FindPngTransparencyChunk(data);
            if (trnsOffset != -1)
            {
                // Get chunk
                Int32 trnsLength = GetPngChunkDataLength(data, trnsOffset);
                transparencyData = GetPngChunkData(data, trnsOffset, trnsLength);
                Array.Copy(data, trnsOffset + 8, transparencyData, 0, trnsLength);
                // filter out the palette alpha chunk, make new data array
                Byte[] data2 = new Byte[data.Length - (trnsLength + 12)];
                Array.Copy(data, 0, data2, 0, trnsOffset);
                Int32 trnsEnd = trnsOffset + trnsLength + 12;
                Array.Copy(data, trnsEnd, data2, trnsOffset, data.Length - trnsEnd);
                data = data2;
            }
            using (MemoryStream ms = new MemoryStream(data))
            using (Bitmap loadedImage = new Bitmap(ms))
            {
                if (loadedImage.Palette.Entries.Length != 0 && transparencyData != null)
                {
                    ColorPalette pal = loadedImage.Palette;
                    for (Int32 i = 0; i < pal.Entries.Length; i++)
                    {
                        if (i >= transparencyData.Length)
                            break;
                        Color col = pal.Entries[i];
                        pal.Entries[i] = Color.FromArgb(transparencyData[i], col.R, col.G, col.B);
                    }
                    loadedImage.Palette = pal;
                }
                return ImageUtils.CloneImage(loadedImage);
            }
        }

        private static Int32 FindPngTransparencyChunk(Byte[] data)
        {
            if (data.Length <= PNG_IDENTIFIER.Length)
                return -1;
            // Check if the image is a PNG.
            Byte[] compareData = new Byte[PNG_IDENTIFIER.Length];
            Array.Copy(data, compareData, PNG_IDENTIFIER.Length);
            if (!PNG_IDENTIFIER.SequenceEqual(compareData))
                return -1;
            Int32 hdrOffset = FindPngChunk(data, "IHDR");
            if (hdrOffset == -1)
                return -1;
            Byte[] header = GetPngChunkData(data, hdrOffset);
            // Check if type is paletted ('3')
            if (header.Length < 13 || header[9] != 3)
                return -1;
            // Check if it really contains a palette.
            Int32 plteOffset = FindPngChunk(data, "PLTE");
            if (plteOffset == -1)
                return -1;
            // Check if it contains a palette transparency chunk.
            return FindPngChunk(data, "tRNS");
        }

        /// <summary>
        /// Saves as png, reducing the palette to the given length.
        /// </summary>
        /// <param name="image">Image to save.</param>
        /// <param name="filename">Target filename.</param>
        /// <param name="paletteLength">Actual length of the palette.</param>
        public static void SaveAsPng(Bitmap image, String filename, Int32 paletteLength)
        {
            Byte[] data = GetPngImageData(image, paletteLength, false);
            File.WriteAllBytes(filename, data);
        }

        /// <summary>
        /// Saves as png, reducing the palette to the given length.
        /// </summary>
        /// <param name="image">Image to save.</param>
        /// <param name="paletteLength">Actual length of the palette. Use 0 to ignore.</param>
        /// <param name="noPalTrans">Remove all palette transparency.</param>
        public static Byte[] GetPngImageData(Bitmap image, Int32 paletteLength, Boolean noPalTrans)
        {
            Boolean isPaletted = (image.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed;
            Int32 maxPalSize = 1 << Image.GetPixelFormatSize(image.PixelFormat);
            if (paletteLength == 0 && isPaletted)
                paletteLength = maxPalSize;
            else
                paletteLength = Math.Min(paletteLength, maxPalSize);
            Byte[] data;
            Color[] palette = null;
            ColorPalette changedPal = null;
            Byte[] transparencyData = new Byte[noPalTrans ? 0 : paletteLength];
            if ((image.PixelFormat & PixelFormat.Indexed) != 0 && image.Palette.Entries.Length > 0)
            {
                changedPal = image.Palette;
                palette = image.Palette.Entries;
                for (Int32 i = 0; i < palette.Length; i++)
                    changedPal.Entries[i] = Color.FromArgb(0xFF, palette[i]);
                if (!noPalTrans)
                    for (Int32 i = 0; i < paletteLength; i++)
                        transparencyData[i] = palette[i].A;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                Bitmap saveImage = changedPal == null ? image : ImageUtils.CloneImage(image);
                if (changedPal != null)
                    saveImage.Palette = changedPal;
                saveImage.Save(ms, ImageFormat.Png);
                data = ms.ToArray();
                if (changedPal != null)
                    saveImage.Dispose();
            }
            if (palette == null)
                return data;
            Int32 paletteDataLength = paletteLength*3;
            Int32 plteOffset = FindPngChunk(data, "PLTE");
            if (plteOffset == -1)
                return data;
            Int32 plteLength = GetPngChunkDataLength(data, plteOffset) + 12;
            Byte[] paletteData = new Byte[paletteDataLength];
            Array.Copy(data, plteOffset + 8, paletteData, 0, paletteDataLength);
            paletteDataLength += 12;

            Int32 actualTrnsDataLength = Enumerable.Range(1, transparencyData.Length).LastOrDefault(i => transparencyData[i - 1] != 255);
            if (actualTrnsDataLength < transparencyData.Length)
            {
                Byte[] transData = new Byte[actualTrnsDataLength];
                Array.Copy(transparencyData, transData, actualTrnsDataLength);
                transparencyData = transData;
            }
            if (actualTrnsDataLength != 0)
                actualTrnsDataLength += 12;
            Int32 oldTrnsDataLength = 0;
            
            // Check if it contains a palette transparency chunk. Don't think it ever will though.
            Int32 trnsOffset = FindPngChunk(data, "tRNS");
            if (trnsOffset != -1)
            {
                oldTrnsDataLength = GetPngChunkDataLength(data, trnsOffset);
                oldTrnsDataLength += 12;
            }
            Int32 newSize = data.Length - (plteLength - paletteDataLength) - (oldTrnsDataLength - actualTrnsDataLength);
            Byte[] newData = new Byte[newSize];
            Int32 currentPosTrg = 0;
            Int32 currentPosSrc = 0;
            Int32 writeLength;
            Array.Copy(data, currentPosSrc, newData, currentPosTrg, writeLength = plteOffset);
            currentPosSrc += plteOffset;
            currentPosTrg += writeLength;
            currentPosTrg = WritePngChunk(newData, currentPosTrg, "PLTE", paletteData);
            currentPosSrc += plteLength;
            if (actualTrnsDataLength > 0)
            {
                Int32 inbetweenData = trnsOffset - currentPosSrc;
                if (inbetweenData > 0)
                {
                    Array.Copy(data, currentPosSrc, newData, currentPosTrg, writeLength = inbetweenData);
                    currentPosSrc += writeLength;
                    currentPosTrg += writeLength;
                }
                currentPosTrg = WritePngChunk(newData, currentPosTrg, "tRNS", transparencyData);
                currentPosSrc += oldTrnsDataLength;
            }
            Array.Copy(data, currentPosSrc, newData, currentPosTrg, data.Length - currentPosSrc);
            data = newData;
            return data;
        }

        public static ColorPalette AdjustPalette(ColorPalette colors, Int32 size)
        {
            Color[] oldpal = colors.Entries;
            Color[] newPal = new Color[size];
            Array.Copy(oldpal, newPal, Math.Min(oldpal.Length, size));
            return GetPalette(newPal);
        }

        /// <summary>
        /// Creates a custom-sized color palette by creating an empty png with a limited palette and extracting its palette.
        /// </summary>
        /// <param name="colors">The colors to convert into a palette.</param>
        /// <returns>A color palette containing the given colors.</returns>
        public static ColorPalette GetPalette(Color[] colors)
        {
            // Silliest idea ever, but it works, lol.
            const Int32 chunkExtraLen = 0x0C;
            Int32 lenPng = PNG_IDENTIFIER.Length;
            const Int32 lenHdr = 0x0D;
            Int32 lenPal = Math.Min(colors.Length, 0x100)*3;
            Int32 lenData = PNG_BLANK.Length;
            Int32 fullLen = lenPng + lenHdr + chunkExtraLen + lenPal + chunkExtraLen + lenData + chunkExtraLen + chunkExtraLen;
            Int32 offset = 0;
            Byte[] emptyPng = new Byte[fullLen];
            Array.Copy(PNG_IDENTIFIER, 0, emptyPng, 0, PNG_IDENTIFIER.Length);
            offset += lenPng;
            Byte[] header = new Byte[lenHdr];
            // Width: 1
            ArrayUtils.WriteIntToByteArray(header, 0, 4, false, 1);
            // Heigth: 1
            ArrayUtils.WriteIntToByteArray(header, 4, 4, false, 1);
            // Color depth: 8
            ArrayUtils.WriteIntToByteArray(header, 8, 1, false, 8);
            // Color type: paletted
            ArrayUtils.WriteIntToByteArray(header, 9, 1, false, 3);
            WritePngChunk(emptyPng, offset, "IHDR", header);
            offset += lenHdr + chunkExtraLen;
            // Don't even need to fill this in. We just need the size.
            Byte[] palette = new Byte[lenPal];
            WritePngChunk(emptyPng, offset, "PLTE", palette);
            offset += lenPal + chunkExtraLen;
            WritePngChunk(emptyPng, offset, "IDAT", PNG_BLANK);
            offset += lenData + chunkExtraLen;
            WritePngChunk(emptyPng, offset, "IEND", new Byte[0]);
            using (MemoryStream ms = new MemoryStream(emptyPng))
            using (Bitmap loadedImage = new Bitmap(ms))
            {
                ColorPalette pal = loadedImage.Palette;
                for (Int32 i = 0; i < pal.Entries.Length; i++)
                    pal.Entries[i] = colors[i];
                return pal;
            }
        }

        /// <summary>
        /// Finds the start of a png chunk. This assumes the image is already identified as PNG.
        /// It does not go over the first 8 bytes, but starts at the start of the header chunk.
        /// </summary>
        /// <param name="data">The bytes of the png image.</param>
        /// <param name="chunkName">The name of the chunk to find.</param>
        /// <returns>The index of the start of the png chunk, or -1 if the chunk was not found.</returns>
        private static Int32 FindPngChunk(Byte[] data, String chunkName)
        {
            if (data == null)
                throw new ArgumentNullException("data", "No data given!");
            if (chunkName == null)
                throw new ArgumentNullException("chunkName", "No chunk name given!");
            // Using UTF-8 as extra check to make sure the name does not contain > 127 values.
            Byte[] chunkNamebytes = Encoding.UTF8.GetBytes(chunkName);
            if (chunkName.Length != 4 || chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk name must be 4 ASCII characters!", "chunkName");
            Int32 offset = PNG_IDENTIFIER.Length;
            Int32 end = data.Length;
            Byte[] testBytes = new Byte[4];
            // continue until either the end is reached, or there is not enough space behind it for reading a new header
            try
            {
                while (offset < end && offset + 8 < end)
                {
                    Array.Copy(data, offset + 4, testBytes, 0, 4);
                    // Alternative for more visual debugging:
                    //String currentChunk = Encoding.ASCII.GetString(testBytes);
                    //if (chunkName.Equals(currentChunk))
                    //    return offset;
                    if (chunkNamebytes.SequenceEqual(testBytes))
                        return offset;
                    Int32 chunkLength = GetPngChunkDataLength(data, offset);
                    // chunk size + chunk header + chunk checksum = 12 bytes.
                    offset += 12 + chunkLength;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return -1;
            }
            return -1;
        }

        /// <summary>
        /// Writes a png data chunk.
        /// </summary>
        /// <param name="target">Target array to write into.</param>
        /// <param name="offset">Offset in the array to write the data to.</param>
        /// <param name="chunkName">4-character chunk name.</param>
        /// <param name="chunkData">Data to write into the new chunk.</param>
        /// <returns>The new offset after writing the new chunk. Always equal to the offset plus the length of chunk data plus 12.</returns>
        private static Int32 WritePngChunk(Byte[] target, Int32 offset, String chunkName, Byte[] chunkData)
        {
            if (offset + chunkData.Length + 12 > target.Length)
                throw new ArgumentException("Data does not fit in target array!", "chunkData");
            if (chunkName.Length != 4)
                throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
            Byte[] chunkNamebytes = Encoding.ASCII.GetBytes(chunkName);
            if (chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk must be 4 bytes!", "chunkName");
            Int32 curLength;
            ArrayUtils.WriteIntToByteArray(target, offset, curLength = 4, false, (UInt32) chunkData.Length);
            offset += curLength;
            Int32 nameOffset = offset;
            Array.Copy(chunkNamebytes, 0, target, offset, curLength = 4);
            offset += curLength;
            Array.Copy(chunkData, 0, target, offset, curLength = chunkData.Length);
            offset += curLength;
            UInt32 crcval = Crc32.ComputeChecksum(target, nameOffset, chunkData.Length + 4);
            ArrayUtils.WriteIntToByteArray(target, offset, curLength = 4, false, crcval);
            offset += curLength;
            return offset;
        }

        private static Int32 GetPngChunkDataLength(Byte[] data, Int32 chunkOffset)
        {
            if (chunkOffset + 4 > data.Length)
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            // Don't want to use BitConverter; then you have to check platform endianness and all that mess.
            //Int32 length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
            Int32 length = (Int32) ArrayUtils.ReadIntFromByteArray(data, chunkOffset, 4, false);
            if (length < 0 || chunkOffset + 12 + length > data.Length || !PngChecksumMatches(data, chunkOffset, length))
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            return length;
        }

        private static Byte[] GetPngChunkData(Byte[] data, Int32 chunkOffset)
        {
            return GetPngChunkData(data, chunkOffset, -1);
        }

        private static Byte[] GetPngChunkData(Byte[] data, Int32 chunkOffset, Int32 chunkLength)
        {
            if (chunkLength < 0)
                chunkLength = GetPngChunkDataLength(data, chunkOffset);
            if (chunkLength == -1)
                return null;
            Byte[] chunkData = new Byte[chunkLength];
            Array.Copy(data, chunkOffset + 8, chunkData, 0, chunkLength);
            return chunkData;
        }

        private static Boolean PngChecksumMatches(Byte[] data, Int32 offset, Int32 chunkLength)
        {
            Byte[] checksum = new Byte[4];
            Array.Copy(data, offset + 8 + chunkLength, checksum, 0, 4);
            UInt32 readChecksum = (UInt32) ArrayUtils.ReadIntFromByteArray(checksum, 0, 4, false);
            UInt32 calculatedChecksum = Crc32.ComputeChecksum(data, offset + 4, chunkLength + 4);
            return readChecksum == calculatedChecksum;
        }

    }
}