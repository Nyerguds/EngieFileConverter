using CnC64FileConverter.Domain.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Image loading toolset class which corrects the bug that prevents paletted PNG images with transparency from being loaded as paletted.
    /// </summary>
    public class BitmapHandler
    {
        private static Byte[] PNG_IDENTIFIER = {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};

        /// <summary>
        /// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
        /// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
        /// </summary>
        /// <param name="filename">Filename to load</param>
        /// <returns>The loaded image</returns>
        public static Bitmap LoadBitmap(String filename)
        {
            Byte[] data = File.ReadAllBytes(filename);
            return LoadBitmap(data);
        }

        /// <summary>
        /// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
        /// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
        /// </summary>
        /// <param name="filename">Filename to load</param>
        /// <param name="paletteLength">Palette length in the original image. The palette format of .net is not adjustable in size, so it'll be the max size. This value can be used to adjust that.</param>
        /// <returns>The loaded image</returns>
        public static Bitmap LoadBitmap(String filename, out Int32 paletteLength)
        {
            Byte[] data = File.ReadAllBytes(filename);
            return LoadBitmap(data, out paletteLength);
        }

        /// <summary>
        /// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
        /// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
        /// </summary>
        /// <param name="data">File data to load</param>
        /// <returns>The loaded image</returns>
        public static Bitmap LoadBitmap(Byte[] data)
        {
            Int32 colors;
            return LoadBitmap(data, out colors);
        }

        /// <summary>
        /// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
        /// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
        /// </summary>
        /// <param name="data">File data to load</param>
        /// <param name="paletteLength">Palette length in the original image. The palette format of .net is not adjustable in size, so it'll be the max size. This value can be used to adjust that.</param>
        /// <returns>The loaded image</returns>
        public static Bitmap LoadBitmap(Byte[] data, out Int32 paletteLength)
        {
            Bitmap loadedImage;
            if (data.Length > PNG_IDENTIFIER.Length)
            {
                // Check if the image is a PNG.
                Byte[] compareData = new Byte[PNG_IDENTIFIER.Length];
                Array.Copy(data, compareData, PNG_IDENTIFIER.Length);
                if (PNG_IDENTIFIER.SequenceEqual(compareData))
                {
                    Byte[] transparencyData = null;
                    // Check if it contains a palette.
                    // I'm sure it can be looked up in the header somehow, but meh.
                    Int32 plteOffset = FindChunk(data, "PLTE");
                    if (plteOffset != -1)
                    {
                        // Check if it contains a palette transparency chunk.
                        Int32 trnsOffset = FindChunk(data, "tRNS");
                        if (trnsOffset != -1)
                        {
                            // Get chunk
                            Int32 trnsLength = GetChunkDataLength(data, trnsOffset);
                            transparencyData = new Byte[trnsLength];
                            Array.Copy(data, trnsOffset + 8, transparencyData, 0, trnsLength);
                            // filter out the palette alpha chunk, make new data array
                            Byte[] data2 = new Byte[data.Length - (trnsLength + 12)];
                            Array.Copy(data, 0, data2, 0, trnsOffset);
                            Int32 trnsEnd = trnsOffset + trnsLength + 12;
                            Array.Copy(data, trnsEnd, data2, trnsOffset, data.Length - trnsEnd);
                            data = data2;
                        }
                    }
                    // Open a Stream and decode a PNG image
                    using (MemoryStream imageStreamSource = new MemoryStream(data))
                    {
                        PngBitmapDecoder decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        BitmapSource bitmapSource = decoder.Frames[0];
                        Int32 width = bitmapSource.PixelWidth;
                        Int32 height = bitmapSource.PixelHeight;
                        Int32 bpp = bitmapSource.Format.BitsPerPixel;
                        Int32 stride = Math.Max(width, GetStride(width, bitmapSource.Format.BitsPerPixel));
                        Byte[] pixel = new Byte[height * stride];
                        bitmapSource.CopyPixels(pixel, stride, 0);
                        WriteableBitmap myBitmap = new WriteableBitmap(width, height, 96, 96, bitmapSource.Format, bitmapSource.Palette);
                        myBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixel, stride, 0);
                        // Convert WPF BitmapSource to GDI+ Bitmap
                        Bitmap newBitmap = BitmapFromSource(myBitmap);
                        
                        System.Drawing.Color[] colpal = newBitmap.Palette.Entries;
                        Boolean hasTransparency = false;
                        if (colpal.Length != 0 && transparencyData != null)
                        {
                            for (Int32 i = 0; i < colpal.Length; i++)
                            {
                                if (i >= transparencyData.Length)
                                    break;
                                System.Drawing.Color col = colpal[i];
                                colpal[i] = System.Drawing.Color.FromArgb(transparencyData[i], col.R, col.G, col.B);
                                if (!hasTransparency)
                                    hasTransparency = transparencyData[i] == 0;
                            }
                        }
                        paletteLength = colpal.Length;
                        if (hasTransparency)
                        {
                            Byte[] imageData = ImageUtils.GetImageData(newBitmap, out stride);
                            newBitmap = ImageUtils.BuildImage(imageData, newBitmap.Width, newBitmap.Height, stride, newBitmap.PixelFormat, colpal, System.Drawing.Color.Empty);
                        }
                        return newBitmap;
                    }                    
                }
            }
            using (MemoryStream ms = new MemoryStream(data))
            {
                loadedImage = new Bitmap(ms);
                paletteLength = loadedImage.Palette.Entries.Length;
            }
            return loadedImage;
        }

        private static Int32 GetStride(Int32 width, Int32 bits)
        {
            Int32 stride = bits * width;
            return (stride / 8) + ((stride % 8) > 0 ? 1 : 0);
        }
        
        /// <summary>
        /// Saves as png, reducing the palette to the given length.
        /// </summary>
        /// <param name="image">Image to save</param>
        /// <param name="filename">Target filename</param>
        /// <param name="paletteLength">Actual length of the palette.</param>
        public static void SaveAsPng(Bitmap image, String filename, Int32 paletteLength)
        {
            String tempFileName = filename;
            if (!tempFileName.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                tempFileName += ".png";
            Byte[] data = ImageUtils.GetSavedImageData(image, ref tempFileName);
            Int32 cols = image.Palette.Entries.Length;
            if (cols > 0 && cols > paletteLength)
            {
                Int32 paletteDataLength = paletteLength * 3;
                Int32 plteOffset = FindChunk(data, "PLTE");
                if (plteOffset != -1)
                {
                    Int32 plteLength = GetChunkDataLength(data, plteOffset) + 12;
                    Byte[] paletteData = new Byte[paletteDataLength];
                    Array.Copy(data, plteOffset + 8, paletteData, 0, paletteDataLength);
                    paletteDataLength +=12;

                    Int32 transparencyDataLength = 0;
                    Int32 trnsLength = 0;
                    Byte[] transparencyData = null;

                    // Check if it contains a palette transparency chunk.
                    Int32 trnsOffset = FindChunk(data, "tRNS");
                    if (trnsOffset != -1)
                    {
                        trnsLength = GetChunkDataLength(data, trnsOffset);
                        transparencyDataLength = Math.Min(trnsLength, paletteLength);
                        transparencyData = new Byte[transparencyDataLength];
                        Array.Copy(data, trnsOffset + 8, transparencyData, 0, transparencyDataLength);
                        trnsLength+=12;
                        transparencyDataLength += 12;
                    }
                    Int32 newSize = data.Length - (plteLength - paletteDataLength) - (trnsLength - transparencyDataLength);
                    Byte[] newData = new Byte[newSize];
                    Int32 currentPosTrg = 0;
                    Int32 currentPosSrc = 0;
                    Int32 writeLength;
                    Array.Copy(data, currentPosSrc, newData, currentPosTrg, writeLength = plteOffset);
                    currentPosSrc += plteOffset;
                    currentPosTrg += writeLength;
                    currentPosTrg = WriteChunk(newData, currentPosTrg, "PLTE", paletteData);
                    currentPosSrc += plteLength;
                    if (trnsOffset != -1)
                    {
                        Int32 inbetweenData = trnsOffset - currentPosSrc;
                        if (inbetweenData > 0)
                        {
                            Array.Copy(data, currentPosSrc, newData, currentPosTrg, writeLength = inbetweenData);
                            currentPosSrc += writeLength;
                            currentPosTrg += writeLength;
                        }
                        currentPosTrg = WriteChunk(newData, currentPosTrg, "tRNS", transparencyData);
                        currentPosSrc += trnsLength;
                    }
                    Array.Copy(data, currentPosSrc, newData, currentPosTrg, data.Length - currentPosSrc);
                    data = newData;
                }
            }
            File.WriteAllBytes(filename, data);
        }

        private static System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                // from System.Media.BitmapImage to System.Drawing.Bitmap 
                BitmapEncoder enc = new BmpBitmapEncoder();
                BitmapFrame bmf = BitmapFrame.Create(bitmapsource);
                enc.Frames.Add(bmf);
                enc.Save(outStream);
                return new System.Drawing.Bitmap(outStream);
            }
        }

        /// <summary>
        /// Finds the start of a png chunk. This assumes the image is already identified as PNG.
        /// It does not go over the first 8 bytes, but starts at the start of the header chunk.
        /// </summary>
        /// <param name="data">The bytes of the png image</param>
        /// <param name="chunkName">The name of the chunk to find.</param>
        /// <returns>The index of the start of the png chunk, or -1 if the chunk was not found.</returns>
        private static Int32 FindChunk(Byte[] data, String chunkName)
        {
            if (chunkName.Length != 4 )
                throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
            Byte[] chunkNamebytes = Encoding.ASCII.GetBytes(chunkName);
            if (chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
            Int32 offset = PNG_IDENTIFIER.Length;
            Int32 end = data.Length;
            Byte[] testBytes = new Byte[4];
            // continue until either the end is reached, or there is not enough space behind it for reading a new header
            while (offset < end && offset + 8 < end)
            {
                Array.Copy(data, offset + 4, testBytes, 0, 4);
                // Alternative for more visual debugging:
                //String currentChunk = Encoding.ASCII.GetString(testBytes);
                //if (chunkName.Equals(currentChunk))
                //    return offset;
                if (chunkNamebytes.SequenceEqual(testBytes))
                    return offset;
                Int32 chunkLength = GetChunkDataLength(data, offset);
                // chunk size + chunk header + chunk checksum = 12 bytes.
                offset += 12 + chunkLength;
            }
            return -1;
        }

        private static Int32 WriteChunk(Byte[] target, Int32 offset, String chunkName, Byte[] chunkData)
        {
            if (offset + chunkData.Length + 12 > target.Length)
                throw new ArgumentException("Data does not fit in target array!", "chunkData");
            if (chunkName.Length != 4)
                throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
            Byte[] chunkNamebytes = Encoding.ASCII.GetBytes(chunkName);
            if (chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
            Byte[] length = new Byte[4];
            length[0] = (Byte)((chunkData.Length >> 24) & 0xFF);
            length[1] = (Byte)((chunkData.Length >> 16) & 0xFF);
            length[2] = (Byte)((chunkData.Length >> 8) & 0xFF);
            length[3] = (Byte)(chunkData.Length & 0xFF);

            Int32 curLength;
            Array.Copy(length, 0, target, offset, curLength = 4);
            offset += curLength;
            Int32 nameOffset = offset;
            Array.Copy(chunkNamebytes, 0, target, offset, curLength = 4);
            offset += curLength;
            Array.Copy(chunkData, 0, target, offset, curLength = chunkData.Length);
            offset += curLength;

            UInt32 crcval = Crc32.ComputeChecksum(target, nameOffset, chunkData.Length + 4);
            Byte[] crc = new Byte[4];
            crc[0] = (Byte)((crcval >> 24) & 0xFF);
            crc[1] = (Byte)((crcval >> 16) & 0xFF);
            crc[2] = (Byte)((crcval >> 8) & 0xFF);
            crc[3] = (Byte)(crcval & 0xFF);
            Array.Copy(crc, 0, target, offset, curLength = 4);
            offset += curLength;
            return offset;
        }

        private static Int32 GetChunkDataLength(Byte[] data, Int32 offset)
        {
            if (offset + 4 > data.Length)
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            // Don't want to use BitConverter; then you have to check platform endianness and all that mess.
            Int32 length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
            if (length < 0)
                throw new IndexOutOfRangeException("Bad chunk size in png image.");
            return length;
        }
    }
}