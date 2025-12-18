using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace Nyerguds.ImageManipulation
{
    public static class ImageUtils
    {
        public static Color[] ConvertToColors(Byte[] colorData, Bitmap sourceImage, Int32? depth)
        {
            Int32 colDepth;
            if (depth.HasValue)
                colDepth = depth.Value;
            else
                colDepth = Image.GetPixelFormatSize(sourceImage.PixelFormat);
            // Get color components count
            Int32 byteCount = colDepth / 8;
            if (depth != 32 && depth != 24)
                throw new NotSupportedException("Unsupported colour depth!");

            // colorData.Length / byteCount
            Color[] newColors = new Color[sourceImage.Width];
            for (Int32 i = 0; i < newColors.Length; i++)
            {
                Int32 pos = i * byteCount;
                Color clr = Color.Empty;

                // Get start index of the specified pixel

                if (depth == 32) // For 32 bpp: get Red, Green, Blue and Alpha
                {
                    Byte b = colorData[pos];
                    Byte g = colorData[pos + 1];
                    Byte r = colorData[pos + 2];
                    Byte a = colorData[pos + 3]; // a
                    clr = Color.FromArgb(a, r, g, b);
                }
                else if (depth == 24) // For 24 bpp: get Red, Green and Blue
                {
                    Byte b = colorData[pos];
                    Byte g = colorData[pos + 1];
                    Byte r = colorData[pos + 2];
                    clr = Color.FromArgb(r, g, b);
                }
                newColors[i] = clr;
            }
            return newColors;
        }

        public static Int32 GetPixelFormatSize(Bitmap image)
        {
            return Image.GetPixelFormatSize(image.PixelFormat);
        }

        public static ColorPalette GetColorPalette(Color[] colors, PixelFormat pf)
        {
            ColorPalette cp = new Bitmap(1, 1, pf).Palette;
            for (Int32 i = 0; i < colors.Length && i < cp.Entries.Length; i++)
                cp.Entries[i] = colors[i];
            return cp;
        }

        /// <summary>
        /// Loads an image without locking the underlying file.
        /// </summary>
        /// <param name="path">Path of the image to load</param>
        /// <returns>The image</returns>
        public static Bitmap LoadImageSafe(String path)
        {
            Byte[] fileData = File.ReadAllBytes(path);
            using (MemoryStream ms = new MemoryStream(fileData))
            {
                Bitmap bm = new Bitmap(ms);
                bm = new Bitmap(bm);
                ms.Close();
                return bm;
            }
        }

        public static void SaveImage(Bitmap image, String filename)
        {
            Byte[] imageBytes = GetSavedImageData(image, ref filename);
            File.WriteAllBytes(filename, imageBytes);
        }

        public static Byte[] GetSavedImageData(Bitmap image, ref String filename)
        {
            String ext = Path.GetExtension(filename);
            ImageFormat saveFormat = ImageFormat.Png;

            if (".bmp".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                saveFormat = ImageFormat.Bmp;
            else if (".gif".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                saveFormat = ImageFormat.Gif;
            else if (".jpg".Equals(ext, StringComparison.InvariantCultureIgnoreCase) || ".jpeg".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                saveFormat = ImageFormat.Jpeg;
            else if (!".png".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                filename += ".png";
            using (image = CloneImage(image))
            using (MemoryStream ms = new MemoryStream())
            {
                if (saveFormat.Equals(ImageFormat.Jpeg))
                {
                    // What a mess just to have non-crappy jpeg. Scratch that; jpeg is always crappy.
                    ImageCodecInfo jpegEncoder = null;
                    Guid formatId = ImageFormat.Jpeg.Guid;
                    foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
                    {
                        if (codec.FormatID == formatId)
                        {
                            jpegEncoder = codec;
                            break;
                        }
                    }
                    Encoder qualityEncoder = Encoder.Quality;
                    EncoderParameters encparams = new EncoderParameters(1);
                    encparams.Param[0] = new EncoderParameter(qualityEncoder, 100L);
                    image.Save(ms, jpegEncoder, encparams);
                }
                else if (saveFormat.Equals(ImageFormat.Gif) && image.PixelFormat == PixelFormat.Format4bppIndexed)
                {
                    // 4-bit images don't get converted right; they get dumped on the standard windows 256 colour palette. So we convert it manually before the save.
                    Int32 stride;
                    Byte[] fourBitData = GetImageData(image, out stride);
                    Byte[] eightBitData = ConvertTo8Bit(fourBitData, image.Width, image.Height, 0, 4, true, ref stride);
                    using(Bitmap img2 = BuildImage(eightBitData, image.Width, image.Height, stride, PixelFormat.Format8bppIndexed, image.Palette.Entries, Color.Black))
                        img2.Save(ms, saveFormat);
                }
                else if (saveFormat.Equals(ImageFormat.Png))
                    BitmapHandler.GetPngImageData(image, 0);
                else
                    image.Save(ms, saveFormat);
                // Clean up temp image.
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets the minimum stride required for containing an image of the given width and bits per pixel.
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="bitsLength">bits length of each pixel.</param>
        /// <returns>The minimum stride required for containing an image of the given width and bits per pixel.</returns>
        public static Int32 GetMinimumStride(Int32 width, Int32 bitsLength)
        {
            return ((bitsLength * width) + 7) / 8;
        }

        public static Int32 GetClassicStride(Int32 width, Int32 bitsLength)
        {
            return ((GetMinimumStride(width, bitsLength) + 3) / 4) * 4;
        }

        public static PixelFormat GetIndexedPixelFormat(Int32 bpp)
        {
            switch (bpp)
            {
                case 1: return PixelFormat.Format1bppIndexed;
                case 4: return PixelFormat.Format4bppIndexed;
                case 8: return PixelFormat.Format8bppIndexed;
                default: throw new NotSupportedException("Unsupported indexed pixel format '" + bpp + "'!");
            }
        }

        public static Bitmap ConvertToPalettedGrayscale(Bitmap image)
        {
            return ConvertToPalettedGrayscale(image, 8, false);
        }

        public static Bitmap ConvertToPalettedGrayscale(Bitmap image, Int32 bpp, Boolean bigEndianBits)
        {
            PixelFormat pf = GetIndexedPixelFormat(bpp);
            if (image.PixelFormat == pf && ColorUtils.HasGrayPalette(image))
                return CloneImage(image);
            if (image.PixelFormat != PixelFormat.Format32bppArgb)
                image = PaintOn32bpp(image, Color.Black);
            Int32 grayBpp = Image.GetPixelFormatSize(pf);
            Int32 stride;
            Byte[] imageData = GetImageData(image, out stride);
            imageData = Convert32bToGray(imageData, image.Width, image.Height, grayBpp, bigEndianBits, ref stride);
            return BuildImage(imageData, image.Width, image.Height, stride, pf, PaletteUtils.GenerateGrayPalette(grayBpp, null, false), null);
        }

        public static Bitmap PaintOn32bpp(Image image, Color? transparencyFillColor)
        {
            Bitmap bp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bp))
            {
                if (transparencyFillColor.HasValue)
                    using (SolidBrush myBrush = new SolidBrush(Color.FromArgb(255, transparencyFillColor.Value)))
                        gr.FillRectangle(myBrush, new Rectangle(0, 0, image.Width, image.Height));
                gr.DrawImage(image, new Rectangle(0, 0, bp.Width, bp.Height));
            }
            return bp;
        }

        public static Byte[] Convert32bToGray(Byte[] imageData, Int32 width, Int32 height, Int32 bpp, Boolean bigEndianBits, ref Int32 stride)
        {
            if (stride < width * 4)
                throw new ArgumentException("Stride is smaller than one pixel line!", "stride");
            Int32 divvalue = 256 / (1 << bpp);
            Byte[] newImageData = new Byte[width * height];
            Int32 inputOffsetLine = 0;
            Int32 outputOffsetLine = 0;
            for (Int32 y = 0; y < height; y++)
            {
                Int32 inputOffs = inputOffsetLine;
                Int32 outputOffs = outputOffsetLine;
                for (Int32 x = 0; x < width; x++)
                {
                    Color c = Color.FromArgb(imageData[inputOffs + 3], imageData[inputOffs + 2], imageData[inputOffs + 1], imageData[inputOffs]);
                    if (c.A < 128)
                        newImageData[outputOffs] = 0;
                    else
                        newImageData[outputOffs] = (Byte)(Math.Min((c.R * 0.3) + (c.G * 0.59) + (c.B * 0.11), 255) / divvalue);
                    inputOffs += 4;
                    outputOffs++;
                }
                inputOffsetLine += stride;
                outputOffsetLine += width;
            }
            stride = width;
            if (bpp < 8)
                newImageData = ConvertFrom8Bit(newImageData, width, height, bpp, bigEndianBits, ref stride);
            return newImageData;
        }


        public static Boolean CompareHiColorImages(Byte[] imageData1, Int32 stride1, Byte[] imageData2, Int32 stride2, Int32 width, Int32 height, PixelFormat pf)
        {
            Int32 byteSize = Image.GetPixelFormatSize(pf) / 8;
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 offset1 = y * stride1 + x * byteSize;
                    Int32 offset2 = y * stride2 + x * byteSize;
                    for (Int32 n = 0; n > byteSize; n++)
                        if (imageData1[offset1 + n] != imageData2[offset2 + n])
                            return false;
                }
            }
            return true;
        }

        public static Byte[] Match8BitDataToPalette(Byte[] imageData, Int32 width, Int32 height, Color[] sourcePalette, Color[] targetPalette)
        {
            Byte[] newImageData = new Byte[width * height];
            for (Int32 i = 0; i < imageData.Length; i++)
            {
                Int32 currentVal = imageData[i];
                Color c;
                if (currentVal < sourcePalette.Length)
                    c = sourcePalette[imageData[i]];
                else
                    c = Color.Black;
                newImageData[i] = (Byte)ColorUtils.GetClosestPaletteIndexMatch(c, targetPalette, null);
            }
            return newImageData;
        }

        public static Bitmap ConvertToPalette(Bitmap originalImage, Int32 bpp, Color[] palette)
        {
            PixelFormat pf = GetIndexedPixelFormat(bpp);
            Int32 stride;
            Byte[] imageData;
            if (originalImage.PixelFormat != PixelFormat.Format32bppArgb)
            {
                using (Bitmap bm32bpp = PaintOn32bpp(originalImage, Color.Black))
                    imageData = GetImageData(originalImage, out stride);
            }
            else
                imageData = GetImageData(originalImage, out stride);
            Byte[] palettedData = Convert32BitToPaletted(imageData, originalImage.Width, originalImage.Height, bpp, bpp == 1, palette, ref stride);
            return BuildImage(palettedData, originalImage.Width, originalImage.Height, stride, pf, palette, Color.Black);
        }

        public static Byte[] Convert32BitToPaletted(Byte[] imageData, Int32 width, Int32 height, Int32 bpp, Boolean bigEndianBits, Color[] palette, ref Int32 stride)
        {
            if (stride < width * 4)
                throw new ArgumentException("Stride is smaller than one pixel line!", "stride");
            Byte[] newImageData = new Byte[width * height];
            List<Int32> transparentIndices = new List<Int32>();
            Int32 maxLen = Math.Min(0x100, palette.Length);
            for (Int32 i = 0; i < maxLen; i++)
                if (palette[i].A == 0)
                    transparentIndices.Add(i);
            for (Int32 y = 0; y < height; y++)
            {
                Int32 inputOffs = y * stride;
                Int32 outputOffs = y * width;
                for (Int32 x = 0; x < width; x++)
                {
                    Color c = Color.FromArgb(imageData[inputOffs + 3], imageData[inputOffs + 2], imageData[inputOffs + 1], imageData[inputOffs]);
                    if (c.A < 128)
                        newImageData[outputOffs] = 0;
                    else
                        newImageData[outputOffs] = (Byte)ColorUtils.GetClosestPaletteIndexMatch(c, palette, transparentIndices);
                    inputOffs += 4;
                    outputOffs++;
                }
            }
            stride = width;
            if (bpp < 8)
                newImageData = ConvertFrom8Bit(newImageData, width, height, bpp, bigEndianBits, ref stride);
            return newImageData;
        }

        /// <summary>
        /// Gets the raw bytes from an image.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <returns>The raw bytes of the image.</returns>
        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride)
        {
            return GetImageData(sourceImage, out stride, sourceImage.PixelFormat);
        }

        /// <summary>
        /// Gets the raw bytes from an image.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <param name="desiredPixelFormat">PixelFormat in which the data needs to be retrieved.</param>
        /// <returns>The raw bytes of the image.</returns>
        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride, PixelFormat desiredPixelFormat)
        {
            if (sourceImage == null)
                throw new ArgumentNullException("sourceImage", "Source image is null!");
            if (((desiredPixelFormat & PixelFormat.Indexed) != 0) && ((sourceImage.PixelFormat & PixelFormat.Indexed) == 0))
                throw new ArgumentException("An RGB pixel format cannot be converted to an indexed pixel format.", "desiredPixelFormat");
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, desiredPixelFormat);
            stride = sourceData.Stride;
            Byte[] data = new Byte[stride * sourceImage.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        /// <summary>
        /// Clones an image object to free it from any backing resources.
        /// Code taken from http://stackoverflow.com/a/3661892/ with some extra fixes.
        /// </summary>
        /// <param name="sourceImage">The image to clone.</param>
        /// <returns>The cloned image.</returns>
        public static Bitmap CloneImage(Bitmap sourceImage)
        {
            Rectangle rect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
            Bitmap targetImage = new Bitmap(rect.Width, rect.Height, sourceImage.PixelFormat);
            targetImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            BitmapData sourceData = sourceImage.LockBits(rect, ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            BitmapData targetData = targetImage.LockBits(rect, ImageLockMode.WriteOnly, targetImage.PixelFormat);
            Int32 actualDataWidth = ((Image.GetPixelFormatSize(sourceImage.PixelFormat) * rect.Width) + 7) / 8;
            Int32 h = sourceImage.Height;
            Int32 origStride = sourceData.Stride;
            Int32 targetStride = targetData.Stride;
            Byte[] imageData = new Byte[actualDataWidth];
            IntPtr sourcePos = sourceData.Scan0;
            IntPtr destPos = targetData.Scan0;
            // Copy line by line, skipping by stride but copying actual data width
            for (Int32 y = 0; y < h; y++)
            {
                Marshal.Copy(sourcePos, imageData, 0, actualDataWidth);
                Marshal.Copy(imageData, 0, destPos, actualDataWidth);
                sourcePos = new IntPtr(sourcePos.ToInt64() + origStride);
                destPos = new IntPtr(destPos.ToInt64() + targetStride);
            }
            targetImage.UnlockBits(targetData);
            sourceImage.UnlockBits(sourceData);
            // For indexed images, restore the palette. This is not linking to a referenced
            // object in the original image; the getter of Palette creates a new object when called.
            if ((sourceImage.PixelFormat & PixelFormat.Indexed) != 0)
                targetImage.Palette = sourceImage.Palette;
            // Restore DPI settings
            targetImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            return targetImage;
        }

        /// <summary>
        /// Creates a bitmap based on data, width, height, stride and pixel format.
        /// </summary>
        /// <param name="sourceData">Byte array of raw source data</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="stride">Scanline length inside the data</param>
        /// <param name="pixelFormat">Pixel format</param>
        /// <param name="palette">Color palette</param>
        /// <param name="defaultColor">Default color to fill in on the palette if the given colors don't fully fill it.</param>
        /// <returns>The new image</returns>
        public static Bitmap BuildImage(Byte[] sourceData, Int32 width, Int32 height, Int32 stride, PixelFormat pixelFormat, Color[] palette, Color? defaultColor)
        {
            Bitmap newImage = new Bitmap(width, height, pixelFormat);
            BitmapData targetData = newImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, newImage.PixelFormat);
            Int32 newDataWidth = ((Image.GetPixelFormatSize(pixelFormat) * width) + 7) / 8;
            // Compensate for possible negative stride on BMP format.
            Boolean isFlipped = targetData.Stride < 0;
            Int32 targetStride = Math.Abs(targetData.Stride);
            Int64 scan0 = targetData.Scan0.ToInt64();
            for (Int32 y = 0; y < height; y++)
                Marshal.Copy(sourceData, y * stride, new IntPtr(scan0 + y * targetStride), newDataWidth);
            newImage.UnlockBits(targetData);
            // Fix negative stride on BMP format.
            if (isFlipped)
                newImage.RotateFlip(RotateFlipType.Rotate180FlipX);
            // For indexed images, set the palette.
            if ((pixelFormat & PixelFormat.Indexed) != 0 && palette != null)
            {
                ColorPalette pal = newImage.Palette;
                for (Int32 i = 0; i < pal.Entries.Length; i++)
                {
                    if (i < palette.Length)
                        pal.Entries[i] = palette[i];
                    else if (defaultColor.HasValue)
                        pal.Entries[i] = defaultColor.Value;
                    else
                        break;
                }
                newImage.Palette = pal;
            }
            return newImage;
        }

        public static Bitmap FromTwoDimIntArray(Int32[,] data)
        {
            Int32 width = data.GetLength(0);
            Int32 height = data.GetLength(1);
            Int32 byteIndex = 0;
            Byte[] dataBytes = new Byte[height * width * 4];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // UInt32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    UInt32 val = (UInt32)data[x, y];
                    // This code clears out everything but a specific part of the value
                    // and then shifts the remaining piece down to the lowest byte
                    dataBytes[byteIndex + 0] = (Byte)(val & 0x000000FF); // B
                    dataBytes[byteIndex + 1] = (Byte)((val & 0x0000FF00) >> 08); // G
                    dataBytes[byteIndex + 2] = (Byte)((val & 0x00FF0000) >> 16); // R
                    dataBytes[byteIndex + 3] = (Byte)((val & 0xFF000000) >> 24); // A
                    // More efficient than multiplying
                    byteIndex += 4;
                }
            }
            return BuildImage(dataBytes, width, height, width, PixelFormat.Format32bppArgb, null, null);
        }

        public static Bitmap FromTwoDimIntArrayGray(Int32[,] data)
        {
            Int32 width = data.GetLength(0);
            Int32 height = data.GetLength(1);
            Int32 byteIndex = 0;
            Byte[] dataBytes = new Byte[height * width * 4];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // Int32 0xAARRGGBB = Byte[] { BB, GG, RR, AA }
                    // This uses the lowest byte, which is the blue component.
                    dataBytes[byteIndex] = (Byte)((UInt32)data[x, y] & 0xFF);
                    // More efficient than multiplying
                    byteIndex++;
                }
            }
            Color[] palette = new Color[256];
            for (Int32 i = 0; i < palette.Length; i++)
                palette[i] = Color.FromArgb(i, i, i);
            return BuildImage(dataBytes, width, height, width, PixelFormat.Format8bppIndexed, palette, null);
        }

        /// <summary>
        /// Checks if a given image contains transparency.
        /// </summary>
        /// <param name="bitmap">Input bitmap</param>
        /// <returns>True if pixels were found with an alpha value of less than 255.</returns>
        public static Boolean HasTransparency(Bitmap bitmap)
        {
            // not an alpha-capable color format.
            if ((bitmap.Flags & (Int32)ImageFlags.HasAlpha) == 0)
                return false;
            Int32 colDepth = Image.GetPixelFormatSize(bitmap.PixelFormat);
            Int32 height = bitmap.Height;
            Int32 width = bitmap.Height;
            Int32 stride;
            // Indexed formats. Special case because the colours on the palette have the transparency.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && colDepth <= 8)
            {
                ColorPalette pal = bitmap.Palette;
                // Find the transparent indices on the palette.
                List<Int32> transCols = new List<Int32>();
                for (Int32 i = 0; i < pal.Entries.Length; i++)
                {
                    Color col = pal.Entries[i];
                    if (col.A != 255)
                        transCols.Add(i);
                }
                // none of the entries in the palette have transparency information.
                if (transCols.Count == 0)
                    return false;
                // Check pixels for existence of the transparent index.
                Byte[] bytes = GetImageData(bitmap, out stride);
                bytes = ConvertTo8Bit(bytes, width, height, 0, colDepth, bitmap.PixelFormat == PixelFormat.Format1bppIndexed, ref stride);
                foreach (Byte b in bytes)
                {
                    if (transCols.Contains(b))
                        return true;
                }
                return false;
            }
            Byte[] bytes32bit;
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                using (Bitmap bitmap2 = PaintOn32bpp(bitmap, null))
                    bytes32bit = GetImageData(bitmap2, out stride);
            }
            else
            {
                bytes32bit = GetImageData(bitmap, out stride);
            }
            for (Int32 y = 0; y < height; y++)
            {
                Int32 inputOffs = y * stride + 3;
                for (Int32 x = 0; x < width; x++)
                {
                    if (bytes32bit[inputOffs] != 255)
                        return true;
                    inputOffs += 4;
                }
            }
            return false;
        }

        private static Color[] GeneratePalette(Color[] colors, Color def)
        {
            Color[] pal = new Color[256];
            for (Int32 i = 0; i < pal.Length; i++)
                if (i < colors.Length)
                    pal[i] = colors[i];
                else
                    pal[i] = def;
            return pal;
        }

        public static Bitmap GenerateBlankImage(Int32 width, Int32 height, Color[] colors, Byte paintColor)
        {
            if (width == 0 || height == 0)
                return null;
            Color[] pal = GeneratePalette(colors, Color.Empty);
            Byte[] blankArray = new Byte[width * height];
            if (paintColor != 0)
                for (Int32 i = 0; i < blankArray.Length; i++)
                    blankArray[i] = paintColor;
            return BuildImage(blankArray, width, height, width, PixelFormat.Format8bppIndexed, pal, Color.Empty);
            }

        public static Bitmap GenerateCheckerboardImage(Int32 width, Int32 height, Color[] colors, Byte color1, Byte color2)
        {
            if (width == 0 || height == 0)
                return null;
            Byte[] patternArray = new Byte[width * height];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 offset = x + y * height;
                    patternArray[offset] = (((x + y) % 2 == 0) ? color1 : color2);
                }
            }
            return BuildImage(patternArray, width, height, width, PixelFormat.Format8bppIndexed, colors, Color.Empty);
        }

        public static Bitmap GenerateGridImage(Int32 origWidth, Int32 origHeight, Int32 zoomFactor, Color[] colors, Byte bgColor, Byte gridcolor, Byte outLineColor)
        {
            if (zoomFactor <= 0)
                throw new ArgumentOutOfRangeException("zoomFactor");
            Color[] pal = GeneratePalette(colors, Color.Empty);
            Int32 width1 = origWidth * zoomFactor;
            Int32 height1 = origHeight * zoomFactor;
            Int32 width = width1 + 1;
            Int32 height = height1 + 1;
            if (width == 0 || height == 0)
                return null;
            Byte[] patternArray = new Byte[width * height];
            if (bgColor != 0)
                for (Int32 i = 0; i < patternArray.Length; i++)
                    patternArray[i] = bgColor;
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 offset = x + y * width;
                    if (x == 0 || x == width1 || y == 0 || y == height1)
                        patternArray[offset] = outLineColor;
                    else if (x % zoomFactor == 0 || y % zoomFactor == 0)
                        patternArray[offset] = gridcolor;
                }
            }
            return BuildImage(patternArray, width, height, width, PixelFormat.Format8bppIndexed, pal, Color.Empty);
        }

        /// <summary>
        ///     Gets an 8-bit image's internal byte array for editing, executes a given function with that data, and writes the edited array back to the image afterwards.
        /// </summary>
        /// <param name="source">The source image</param>
        /// <param name="editDelegate">A delegate to edit the resulting byte array, with the byte array's stride as second argument.</param>
        public static void EditRawImageBytes(Bitmap source, Action<Byte[], Int32> editDelegate)
        {
            BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            // Compensate for possible negative stride on BMP format.
            Boolean isFlipped = sourceData.Stride < 0;
            Int32 sourceStride = Math.Abs(sourceData.Stride);
            Int32 height = source.Height;
            Int64 scan0 = sourceData.Scan0.ToInt64();
            Int32 dataWidth = ((Image.GetPixelFormatSize(source.PixelFormat) * source.Width) + 7) / 8;
            Byte[] picData = new Byte[dataWidth * sourceData.Height];
            for (Int32 y = 0; y < height; y++)
            {
                Int32 line = isFlipped ? height - 1 - y : y;
                Marshal.Copy(new IntPtr(scan0 + line * sourceStride), picData, y * dataWidth, dataWidth);
            }
            source.UnlockBits(sourceData);
            // =======================================
            // Call delegate function to perform the actual actions.
            editDelegate(picData, dataWidth);
            // =======================================
            BitmapData destData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.WriteOnly, source.PixelFormat);
            Int32 destStride = Math.Abs(destData.Stride);
            isFlipped = sourceData.Stride < 0;
            scan0 = destData.Scan0.ToInt64();
            for (Int32 y = 0; y < height; y++)
            {
                Int32 line = isFlipped ? height - 1 - y : y;
                Marshal.Copy(picData, y * dataWidth, new IntPtr(scan0 + line * destStride), dataWidth);
            }
            source.UnlockBits(destData);
        }

        public static void DrawRect8Bit(Bitmap source, Int32 startX, Int32 startY, Int32 endX, Int32 endY, Byte colorIndex, Boolean fill)
        {
            if (source.PixelFormat != PixelFormat.Format8bppIndexed)
                return;
            EditRawImageBytes(source, (arr, stride) => DrawRect8Bit(arr, source.Width, source.Height, stride, startX, startY, endX, endY, colorIndex, fill));
        }

        public static void DrawRect8Bit(Byte[] dataArray, Int32 width, Int32 height, Int32 stride, Int32 startX, Int32 startY, Int32 endX, Int32 endY, Byte colorIndex, Boolean fill)
        {
            // Switch incorrect start and end positions
            if (startX > endX)
            {
                Int32 tmp = startX;
                startX = endX;
                endX = tmp;
            }
            if (startY > endY)
            {
                Int32 tmp = startY;
                startY = endY;
                endY = tmp;
            }
            // Check if bounds are completely outside image
            if ((startX < 0 && endX < 0) || (startX >= width && endX >= width)
                || (startY < 0 && endY < 0) || (startY >= height && endX >= height))
                return;
            // Restrict bounds to image.
            Int32 maxw = width - 1;
            Int32 maxh = height - 1;
            startX = Math.Min(maxw, Math.Max(0, startX));
            endX = Math.Min(maxw, Math.Max(0, endX));
            startY = Math.Min(maxh, Math.Max(0, startY));
            endY = Math.Min(maxh, Math.Max(0, endY));
            for (Int32 y = startY; y <= endY; y++)
            {
                if (fill)
                {
                    for (Int32 x = startX; x <= endX; x++)
                        dataArray[x + y * stride] = colorIndex;
                }
                else
                {
                    if (y == startY || y == endY)
                        for (Int32 x = startX; x <= endX; x++)
                            dataArray[x + y * stride] = colorIndex;
                    else
                    {
                        dataArray[startX + y * stride] = colorIndex;
                        dataArray[endX + y * stride] = colorIndex;
                    }
                }
            }
        }

        public static void Shift8BitRowVert(Byte[] source, Int32 stride, Boolean up, Boolean wrap, Byte backColor)
        {
            Byte[] newSource = source.ToArray();
            Byte[] emptyRow = new Byte[stride];
            if (backColor != 0 && !wrap)
                for (Int32 i = 0; i < stride; i++)
                    emptyRow[i] = backColor;
            Int32 length = source.Length - stride;
            Int32 srcStart = up ? stride : 0;
            Int32 tarStart = up ? 0 : stride;
            if (wrap)
                Array.Copy(source, up ? 0 : length, emptyRow, 0, stride);
            Array.Copy(newSource, srcStart, source, tarStart, length);
            // clear shifted row
            Array.Copy(emptyRow, 0, source, up ? length : 0, stride);
        }

        public static void Shift8BitRowHor(Byte[] source, Int32 stride, Boolean left, Boolean wrap, Byte backColor)
        {
            Byte[] newSource = source.ToArray();
            Int32 length = stride - 1;
            Int32 srcStart = left ? 1 : 0;
            Int32 tarStart = left ? 0 : 1;
            for (Int32 i = 0; i < source.Length; i += stride)
            {
                Byte fill = (wrap ? newSource[i + (left ? 0 : length)] : backColor);
                Array.Copy(newSource, i + srcStart, source, i + tarStart, length);
                // clear shifted pixel
                source[i + length * srcStart] = fill;
            }
        }

        /// <summary>
        /// Copies a piece out of an 8-bit image. The stride of the output will always equal the width.
        /// </summary>
        /// <param name="fileData">Byte data of the image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="stride">Stride of the image.</param>
        /// <param name="copyArea">The area to copy.</param>
        /// <returns></returns>
        public static Byte[] CopyFrom8bpp(Byte[] fileData, Int32 width, Int32 height, Int32 stride, Rectangle copyArea)
        {
            Byte[] copiedPicture = new Byte[copyArea.Width * copyArea.Height];
            Int32 maxY = Math.Min(height - copyArea.Y, copyArea.Height);
            Int32 maxX = Math.Min(width - copyArea.X, copyArea.Width);

            for (Int32 y = 0; y < maxY; y++)
            {
                for (Int32 x = 0; x < maxX; x++)
                {
                    // This will hit the same byte multiple times
                    Int32 indexSource = (copyArea.Y + y) * stride + copyArea.X + x;
                    // This will always get a new index
                    Int32 indexDest = y * copyArea.Width + x;
                    copiedPicture[indexDest] = fileData[indexSource];
                }
            }
            return copiedPicture;
        }

        /// <summary>
        /// Pastes 8-bit data on an 8-bit image. Note that the source data
        /// is copied before the operation, so it is not modified.
        /// </summary>
        /// <param name="sourceFileData">Byte data of the image that is pasted on.</param>
        /// <param name="sourceWidth">Width of the image that is pasted on.</param>
        /// <param name="sourceHeight">Height of the image that is pasted on.</param>
        /// <param name="sourceStride">Stride of the image that is pasted on.</param>
        /// <param name="pasteFileData">Byte data of the image to paste.</param>
        /// <param name="pasteWidth">Width of the image to paste.</param>
        /// <param name="pasteHeight">Height of the image to paste.</param>
        /// <param name="pasteStride">Stride of the image to paste.</param>
        /// <param name="targetPos">Position at which to paste the image.</param>
        /// <param name="transparencyGuide">Colour palette of the images, to determine which colours should be treated as transparent. Use null for no transparency.</param>
        /// <param name="modifyOrig">True to modify the original array rather than returning a copy.</param>
        /// <returns>A new Byte array with the combined data, and the same stride as the source image.</returns>
        public static Byte[] PasteOn8bpp(Byte[] sourceFileData, Int32 sourceWidth, Int32 sourceHeight, Int32 sourceStride,
            Byte[] pasteFileData, Int32 pasteWidth, Int32 pasteHeight, Int32 pasteStride,
            Rectangle targetPos, Boolean[] transparencyGuide, Boolean modifyOrig)
        {
            if (targetPos.Width != pasteWidth || targetPos.Height != pasteHeight)
                pasteFileData = CopyFrom8bpp(pasteFileData, pasteWidth, pasteHeight, pasteStride, new Rectangle(0, 0, targetPos.Width, targetPos.Height));
            Byte[] finalFileData;
            if (modifyOrig)
            {
                finalFileData = sourceFileData;
            }
            else
            {
                finalFileData = new Byte[sourceFileData.Length];
                Array.Copy(sourceFileData, finalFileData, sourceFileData.Length);
            }
            Boolean[] isTransparent = new Boolean[256];
            if (transparencyGuide != null)
            {
                Int32 len = Math.Min(isTransparent.Length, transparencyGuide.Length);
                for (Int32 i = 0; i < len; i++)
                    isTransparent[i] = transparencyGuide[i];
            }
            Int32 maxY = Math.Min(sourceHeight - targetPos.Y, targetPos.Height);
            Int32 maxX = Math.Min(sourceWidth - targetPos.X, targetPos.Width);
            for (Int32 y = 0; y < maxY; y++)
            {
                for (Int32 x = 0; x < maxX; x++)
                {
                    Int32 indexSource = y * pasteStride + x;
                    Byte data = pasteFileData[indexSource];
                    if (!isTransparent[data])
                    {
                        Int32 indexDest = (targetPos.Y + y) * sourceStride + targetPos.X + x;
                        // This will always get a new index
                        finalFileData[indexDest] = data;
                    }
                }
            }
            return finalFileData;
        }

        /// <summary>
        /// Collapse stride to the minimum required, for any image type.
        /// </summary>
        /// <param name="data">Image data</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="bitsLength">Bits per pixel</param>
        /// <param name="stride">Stride of the image</param>
        /// <returns></returns>
        public static Byte[] CollapseStride(Byte[] data, Int32 width, Int32 height, Int32 bitsLength, ref Int32 stride)
        {
            Int32 newStride = GetMinimumStride(width, bitsLength);
            Byte[] newData = new Byte[newStride * height];
            if (newStride == stride)
                return data;
            for (Int32 y = 0; y < height; y++)
            {
                Int32 oldOffs = stride * y;
                Int32 offs = newStride * y;
                for (Int32 s = 0; s < newStride; s++)
                {
                    newData[offs] = data[oldOffs];
                    offs++;
                    oldOffs++;
                }
            }
            stride = newStride;
            return newData;
        }

        /// <summary>
        /// Converts given raw image data for a paletted image to 8-bit, so we have a simple one-byte-per-pixel format to work with.
        /// Stride is assumed to be the minimum needed to contain the data. Output stride will be the same as the width.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="bitsLength">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        public static Byte[] ConvertTo8Bit(Byte[] fileData, Int32 width, Int32 height, Int32 start, Int32 bitsLength, Boolean bigEndian)
        {
            Int32 stride = GetMinimumStride(width, bitsLength);
            return ConvertTo8Bit(fileData, width, height, start, bitsLength, bigEndian, ref stride);
        }

        /// <summary>
        /// Converts given raw image data for a paletted image to 8-bit, so we have a simple one-byte-per-pixel format to work with.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="bitsLength">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        public static Byte[] ConvertTo8Bit(Byte[] fileData, Int32 width, Int32 height, Int32 start, Int32 bitsLength, Boolean bigEndian, ref Int32 stride)
        {
            if (bitsLength != 1 && bitsLength != 2 && bitsLength != 4 && bitsLength != 8)
                throw new ArgumentOutOfRangeException("Cannot handle image data with " + bitsLength + "bits per pixel.");
            // Full array
            Byte[] data8bit = new Byte[width * height];
            // Amount of runs that end up on the same pixel
            Int32 parts = 8 / bitsLength;
            // Amount of bytes to read per width
            Int32 newStride = width;
            // Bit mask for reducing read and shifted data to actual bits length
            Int32 bitmask = (1 << bitsLength) - 1;
            Int32 size = stride * height;
            // File check, and getting actual data.
            if (start + size > fileData.Length)
                throw new IndexOutOfRangeException("Data exceeds array bounds!");
            // Actual conversion porcess.
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // This will hit the same byte multiple times
                    Int32 indexXbit = start + y * stride + x / parts;
                    // This will always get a new index
                    Int32 index8bit = y * newStride + x;
                    // Amount of bits to shift the data to get to the current pixel data
                    Int32 shift = (x % parts) * bitsLength;
                    // Reversed for big-endian
                    if (bigEndian)
                        shift = 8 - shift - bitsLength;
                    // Get data and store it.
                    data8bit[index8bit] = (Byte)((fileData[indexXbit] >> shift) & bitmask);
                }
            }
            stride = newStride;
            return data8bit;
        }

        /// <summary>
        /// Converts given raw image data for a paletted 8-bit image to lower amount of bits per pixel.
        /// Stride is assumed to be the same as the width. Output stride is the minimum needed to contain the data.
        /// </summary>
        /// <param name="data8bit">The eight bit per pixel image data</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="bitsLength">The new amount of bits per pixel</param>
        /// <param name="bigEndian">True if the bits in the new image data are to be stored as big-endian. One-bit images should generally be big-endian, while 4-bit ones should not be.</param>
        /// <returns>The image data converted to the requested amount of bits per pixel.</returns>
        public static Byte[] ConvertFrom8Bit(Byte[] data8bit, Int32 width, Int32 height, Int32 bitsLength, Boolean bigEndian)
        {
            Int32 stride = width;
            return ConvertFrom8Bit(data8bit, width, height, bitsLength, bigEndian, ref stride);
        }

        /// <summary>
        /// Converts given raw image data for a paletted 8-bit image to lower amount of bits per pixel.
        /// </summary>
        /// <param name="data8bit">The eight bit per pixel image data</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="bitsLength">The new amount of bits per pixel</param>
        /// <param name="bigEndian">True if the bits in the new image data are to be stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value.</param>
        /// <returns>The image data converted to the requested amount of bits per pixel.</returns>
        public static Byte[] ConvertFrom8Bit(Byte[] data8bit, Int32 width, Int32 height, Int32 bitsLength, Boolean bigEndian, ref Int32 stride)
        {
            Int32 parts = 8 / bitsLength;
            // Amount of bytes to write per width
            Int32 newStride = GetMinimumStride(width, bitsLength);
            // Bit mask for reducing original data to actual bits maximum.
            // Should not be needed if data is correct, but eh.
            Int32 bitmask = (1 << bitsLength) - 1;
            Byte[] dataXbit = new Byte[newStride * height];
            // Actual conversion porcess.
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // This will hit the same byte multiple times
                    Int32 indexXbit = y * newStride + x / parts;
                    // This will always get a new index
                    Int32 index8bit = y * stride + x;
                    // Amount of bits to shift the data to get to the current pixel data
                    Int32 shift = (x % parts) * bitsLength;
                    // Reversed for big-endian
                    if (bigEndian)
                        shift = 8 - shift - bitsLength;
                    // Get data, reduce to bit rate, shift it and store it.
                    dataXbit[indexXbit] |= (Byte)((data8bit[index8bit] & bitmask) << shift);
                }
            }
            stride = newStride;
            return dataXbit;
        }

        public static Byte[] Tile8BitData(Byte[][] tiles, Int32 tileWidth, Int32 tileHeight, Int32 tileStride, Int32 nrOftiles, Color[] palette, Int32 tilesX)
        {
            Int32 yDim = nrOftiles / tilesX + (nrOftiles % tilesX == 0 ? 0 : 1);

            // Build image, set in m_LoadedImage
            Int32 fullImageWidth = tilesX * tileWidth;
            Int32 fullImageHeight = yDim * tileHeight;
            Byte[] fullImageData = new Byte[fullImageWidth * fullImageHeight];
            Boolean[] transGuide = PaletteUtils.MakeTransparencyGuide(8, palette);
            for (Int32 y = 0; y < yDim; y++)
            {
                for (Int32 x = 0; x < tilesX; x++)
                {
                    Int32 index = y * tilesX + x;
                    if (index == nrOftiles)
                        break;
                    Byte[] curTile = tiles[index];
                    PasteOn8bpp(fullImageData, fullImageWidth, fullImageHeight, fullImageWidth,
                        curTile, tileWidth, tileHeight, tileStride,
                        new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight), transGuide, true);
                }
            }
            return fullImageData;
        }

        public static Bitmap Tile8BitImages(Byte[][] tiles, Int32 tileWidth, Int32 tileHeight, Int32 tileStride, Int32 nrOftiles, Color[] palette, Int32 tilesX)
        {
            Int32 yDim = nrOftiles / tilesX + (nrOftiles % tilesX == 0 ? 0 : 1);
            Int32 fullImageWidth = tilesX * tileWidth;
            Int32 fullImageHeight = yDim * tileHeight;
            Byte[] fullImageData = Tile8BitData(tiles, tileWidth, tileHeight, tileStride, nrOftiles, palette, tilesX);
            return BuildImage(fullImageData, fullImageWidth, fullImageHeight, fullImageWidth, PixelFormat.Format8bppIndexed, palette, Color.Empty);
        }

        public static Bitmap[] GetFramesFromAnimatedGIF(Image image)
        {
            List<Bitmap> images = new List<Bitmap>();
            Int32 length = image.GetFrameCount(FrameDimension.Time);
            for (Int32 i = 0; i < length; i++)
            {
                image.SelectActiveFrame(FrameDimension.Time, i);
                using (Bitmap frame = new Bitmap(image))
                    images.Add(CloneImage(frame));
            }
            return images.ToArray();
        }

        /// <summary>Trims empty lines off the bottom of an image buffer.</summary>
        /// <param name="buffer">Image data buffer</param>
        /// <param name="width">Image width (technically stride).</param>
        /// <param name="height">Image height. Will be adjusted by this function.</param>
        /// <param name="valueToTrim">Value to trim.</param>
        /// <returns>The trimmed image, if adjustBuffer is true.</returns>
        public static Byte[] TrimYHeight(Byte[] buffer, Int32 width, ref Int32 height, Int32 valueToTrim)
        {
            // nothing to optimize.
            if (height == 0)
                return new Byte[0];
            // Nothing to process
            if (width == 0)
            {
                height = 0;
                return new Byte[0];
            }
            Int32 newHeight;
            Byte[] tempArray = new Byte[width];
            for (newHeight = height; newHeight > 0; newHeight--)
            {
                Array.Copy(buffer, width * (newHeight - 1), tempArray, 0, width);
                if (tempArray.All(x => x == valueToTrim))
                    continue;
                break;
            }
            // Vertical reduce is a simple array copy.
            Byte[] buffer2 = new Byte[newHeight * width];
            Array.Copy(buffer, 0, buffer2, 0, newHeight * width);
            buffer = buffer2;
            height = newHeight;
            return buffer;
        }

        /// <summary>Calculate the amount an image can be cropped in Y-dimension, and adjust the given height and Y offset to compensate.</summary>
        /// <param name="buffer">Image data buffer</param>
        /// <param name="width">Image width (technically stride).</param>
        /// <param name="height">Image height. Will be adjusted by this function.</param>
        /// <param name="yOffset">Current Y-offset to increase.</param>
        /// <param name="AlsoTrimBottom">Trim both top and bottom of the image.</param>
        /// <param name="valueToTrim">Value to trim.</param>
        /// <param name="maxOffset">Maximum value that Y can contain in the file format it'll be saved to. Leave 0 to ignore.</param>
        /// <param name="adjustBuffer">True to actually apply the change to the given buffer. False to only adjust the ref parameters.</param>
        /// <returns>The trimmed image, if adjustBuffer is true.</returns>
        public static Byte[] OptimizeYHeight(Byte[] buffer, Int32 width, ref Int32 height, ref Int32 yOffset, Boolean AlsoTrimBottom, Int32 valueToTrim, Int32 maxOffset, Boolean adjustBuffer)
        {
            // nothing to optimize.
            if (height == 0)
                return new Byte[0];
            // Nothing to process
            if (width == 0)
            {
                height = 0;
                yOffset = 0;
                return new Byte[0];
            }
            Int32 trimYMax = maxOffset != 0 ? Math.Min(maxOffset - yOffset, height) : height;
            Int32 trimmedYTop;
            Int32 trimmedYBottom = height;
            Byte[] tempArray = new Byte[width];
            for (trimmedYTop = 0; trimmedYTop < trimYMax; trimmedYTop++)
            {
                Array.Copy(buffer, width * trimmedYTop, tempArray, 0, width);
                if (!tempArray.All(x => x == valueToTrim))
                    break;
            }
            if (AlsoTrimBottom)
            {
                for (trimmedYBottom = height; trimmedYBottom > trimmedYTop; trimmedYBottom--)
                {
                    Array.Copy(buffer, width * (trimmedYBottom - 1), tempArray, 0, width);
                    if (tempArray.All(x => x == valueToTrim))
                        continue;
                    break;
                }
            }
            Int32 newHeight = trimmedYBottom - trimmedYTop;

            if (trimmedYTop == height)
            {
                // Full width was trimmed; image is empty.
                height = 0;
                yOffset = 0;
                return new Byte[0];
            }
            if (adjustBuffer)
            {
                // Vertical reduce is a simple array copy.
                Byte[] buffer2 = new Byte[newHeight * width];
                Array.Copy(buffer, trimmedYTop * width, buffer2, 0, newHeight * width);
                buffer = buffer2;
            }
            height = newHeight;
            // Optimization: no need to keep Y if data is empty.
            if (height == 0)
                yOffset = 0;
            else
                yOffset += trimmedYTop;
            return buffer;
        }

        /// <summary>Trims empty columns off the right side of an image buffer.</summary>
        /// <param name="buffer">Image data buffer.</param>
        /// <param name="width">Image width (technically stride). Will be adjusted by this function.</param>
        /// <param name="height">Image height.</param>
        /// <param name="valueToTrim">Value to trim.</param>
        /// <returns>The trimmed image.</returns>
        public static Byte[] TrimXWidth(Byte[] buffer, ref Int32 width, Int32 height, Int32 valueToTrim)
        {
            // nothing to optimize.
            if (width == 0)
                return new Byte[0];
            // Nothing to process
            if (height == 0)
            {
                width = 0;
                return new Byte[0];
            }
            Int32 trimmedXRight = 0;
            for (Int32 x = width - 1; x >= 0; x--)
            {
                Boolean empty = true;
                for (Int32 y = 0; y < height; y++)
                {
                    if (buffer[y * width + x] != valueToTrim)
                    {
                        empty = false;
                        break;
                    }
                }
                if (!empty)
                    break;
                trimmedXRight++;
            }
            Int32 newWidth = width - trimmedXRight;
            buffer = CopyFrom8bpp(buffer, width, height, width, new Rectangle(0, 0, newWidth, height));
            width = newWidth;
            return buffer;
        }

        /// <summary>
        /// Crop the image in X-dimension and adjust the X offset to compensate.
        /// </summary>
        /// <param name="buffer">Image data buffer.</param>
        /// <param name="width">Image width (technically stride). Will be adjusted by this function.</param>
        /// <param name="height">Image height.</param>
        /// <param name="xOffset">Current X-offset to increase.</param>
        /// <param name="AlsoTrimRight">Trim both left and right side of the image.</param>
        /// <param name="valueToTrim">Value to trim.</param>
        /// <param name="maxOffset">Maximum value that Y can contain in the file format it'll be saved to. Leave 0 to ignore.</param>
        /// <param name="adjustBuffer">True to actually apply the change to the given buffer. False to only adjust the ref parameters.</param>
        /// <returns>The trimmed image, if adjustBuffer is true.</returns>
        public static Byte[] OptimizeXWidth(Byte[] buffer, ref Int32 width, Int32 height, ref Int32 xOffset, Boolean AlsoTrimRight, Int32 valueToTrim, Int32 maxOffset, Boolean adjustBuffer)
        {
            // nothing to optimize.
            if (width == 0)
                return adjustBuffer ? new Byte[0] : buffer;
            // Nothing to process
            if (height == 0)
            {
                width = 0;
                xOffset = 0;
                return adjustBuffer ? new Byte[0] : buffer;
            }
            Int32 trimXMax = maxOffset != 0 ? Math.Min(maxOffset - xOffset, width) : width;
            Int32 trimmedXLeft = 0;
            Int32 trimmedXRight = 0;
            for (Int32 x = 0; x < trimXMax; x++)
            {
                Boolean empty = true;
                for (Int32 y = 0; y < height; y++)
                {
                    if (buffer[y * width + x] != valueToTrim)
                    {
                        empty = false;
                        break;
                    }
                }
                if (!empty)
                    break;
                trimmedXLeft++;
            }
            if (trimmedXLeft == width)
            {
                width = 0;
                xOffset = 0;
                return new Byte[0];
            }
            if (AlsoTrimRight)
            {
                for (Int32 x = width - 1; x >= 0; x--)
                {
                    Boolean empty = true;
                    for (Int32 y = 0; y < height; y++)
                    {
                        if (buffer[y * width + x] != valueToTrim)
                        {
                            empty = false;
                            break;
                        }
                    }
                    if (!empty)
                        break;
                    trimmedXRight++;
                }
            }
            Int32 newWidth = width - trimmedXLeft - trimmedXRight;
            if (adjustBuffer)
                buffer = CopyFrom8bpp(buffer, width, height, width, new Rectangle(trimmedXLeft, 0, newWidth, height));
            width = newWidth;
            // Optimization: no need to keep Y if data is empty.
            if (width == 0)
                xOffset = 0;
            else
                xOffset += trimmedXLeft;
            return buffer;
        }

        /// <summary>
        /// Changes the stride of the given image data.
        /// </summary>
        /// <param name="buffer">Source byte array.</param>
        /// <param name="origStride">Original stride</param>
        /// <param name="height">Height of the image</param>
        /// <param name="targetStride">Target stride</param>
        /// <param name="fromLeft">True to add/remove bytes at the left side instead of the right.</param>
        /// <param name="fillValue">Byte value used to fill any added space.</param>
        /// <returns>The adjusted array, with the target stride.</returns>
        public static Byte[] ChangeStride(Byte[] buffer, Int32 origStride, Int32 height, Int32 targetStride, Boolean fromLeft, Byte fillValue)
        {
            Int32 sourcePos = 0;
            Int32 destPos = 0;
            Int32 minStride = Math.Min(origStride, targetStride);
            Int32 length = buffer.Length;
            Int32 targetSize = height * targetStride;
            Byte[] target = new Byte[targetSize];
            if (fillValue != 0)
                for (Int32 i = 0; i < targetSize; i++)
                    target[i] = fillValue;
            Int32 diff = origStride - targetStride;
            while (length >= origStride && length > 0)
            {
                Int32 sourcePos1 = sourcePos;
                Int32 destPos1 = destPos;
                if (fromLeft)
                {
                    if (diff > 0)
                        sourcePos1 += diff;
                    else
                        destPos1 -= diff;
                }
                Array.Copy(buffer, sourcePos1, target, destPos1, minStride);
                length -= origStride;
                sourcePos += origStride;
                destPos += targetStride;
            }
            if (length > 0)
                Array.Copy(buffer, sourcePos, target, destPos, length);
            return target;
        }

        /// <summary>
        /// Changes the height of the given image data.
        /// </summary>
        /// <param name="buffer">Source byte array.</param>
        /// <param name="stride">Stride of the image.</param>
        /// <param name="origHeight">Original height of the image.</param>
        /// <param name="targetHeight">Target height.</param>
        /// <param name="fromTop">True to add/remove bytes at the top instead of the bottom.</param>
        /// <param name="fillValue">Byte value used to fill any added space.</param>
        /// <returns>The adjusted array, with the target height.</returns>
        public static Byte[] ChangeHeight(Byte[] buffer, Int32 stride, Int32 origHeight, Int32 targetHeight, Boolean fromTop, Byte fillValue)
        {
            if (origHeight == targetHeight)
                return buffer;
            Int32 newSize = stride * targetHeight;
            Byte[] newData = new Byte[newSize];
            if (fillValue != 0)
                for (Int32 i = stride * origHeight; i < newSize; i++)
                    newData[i] = fillValue;
            Int32 readOffset = 0;
            Int32 writeOffset = 0;
            if (fromTop)
            {
                Int32 hdiff = targetHeight - origHeight;
                if (hdiff < 0)
                    readOffset = (-hdiff) * stride;
                else
                    writeOffset = hdiff * stride;
            }
            Array.Copy(buffer, readOffset, newData, writeOffset, Math.Min(buffer.Length, newData.Length));
            return newData;
        }

        /// <summary>
        /// Find the most common colour in an image.
        /// </summary>
        /// <param name="image">Input image</param>
        /// <returns>The most common colour found in the image. If there are multiple with the same frequency, the first one that was encountered is returned.</returns>
        public static Color FindMostCommonColor(Image image)
        {
            // Avoid unnecessary getter calls
            Int32 height = image.Height;
            Int32 width = image.Width;
            Int32 stride;
            Byte[] imageData;
            // Get image data, in 32bpp
            using (Bitmap bm = PaintOn32bpp(image, Color.Empty))
                imageData = GetImageData(bm, out stride);
            // Store colour frequencies in a dictionary.
            Dictionary<Color, Int32> colorFreq = new Dictionary<Color, Int32>();
            for (Int32 y = 0; y < height; y++)
            {
                // Reset offset on every line, since stride is not guaranteed to always be width * pixel size.
                Int32 inputOffs = y * stride;
                //Final offset = y * line length in bytes + x * pixel length in bytes.
                //To avoid recalculating that offset each time we just increase it with the pixel size at the end of each x iteration.
                for (Int32 x = 0; x < width; x++)
                {
                    //Get colour components out. "ARGB" is actually the order in the final integer which is read as little-endian, so the real order is BGRA.
                    Color col = Color.FromArgb(imageData[inputOffs + 3], imageData[inputOffs + 2], imageData[inputOffs + 1], imageData[inputOffs]);
                    // Only look at nontransparent pixels; cut off at 127.
                    if (col.A > 127)
                    {
                        // Save as pure colour without alpha
                        Color bareCol = Color.FromArgb(255, col);
                        if (!colorFreq.ContainsKey(bareCol))
                            colorFreq.Add(bareCol, 1);
                        else
                            colorFreq[bareCol]++;
                    }
                    // Increase the offset by the pixel width. For 32bpp ARGB, each pixel is 4 bytes.
                    inputOffs += 4;
                }
            }
            // Get the maximum value in the dictionary values
            Int32 max = colorFreq.Values.Max();
            // Get the first colour that matches that maximum.
            return colorFreq.FirstOrDefault(x => x.Value == max).Key;
        }
        
        public static Rectangle GetCropBounds(Bitmap image, Color blankPixel, Int32 borderSizePixels = 5, Rectangle? searchArea = null)
        {
            // Not too worried about the other boundaries; the "for" loops will exclude those anyway.
            Int32 yStart = searchArea.HasValue ? Math.Max(0, searchArea.Value.Y) : 0;
            Int32 yEnd = searchArea.HasValue ? Math.Min(image.Height, searchArea.Value.Y + searchArea.Value.Height) : image.Height;
            Int32 xStart = searchArea.HasValue ? Math.Max(0, searchArea.Value.X) : 0;
            Int32 xEnd = searchArea.HasValue ? Math.Min(image.Width, searchArea.Value.X + searchArea.Value.Width) : image.Width;
            // Values to calculate
            Int32 top;
            Int32 bottom;
            Int32 left;
            Int32 right;
            // Convert to 32bppARGB and get bytes and stride out.
            Byte[] data;
            Int32 stride;
            using (Bitmap bm = new Bitmap(image))
            {
                BitmapData sourceData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
                stride = sourceData.Stride;
                data = new Byte[stride * bm.Height];
                Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
                bm.UnlockBits(sourceData);
            }
            // ============= Y =============
            // Top = first found row which contains data
            for (top = yStart; top < yEnd; top++)
            {
                Int32 index = top * stride;
                if (!RowClear(data, index, 4, xStart, xEnd, blankPixel))
                    break;
            }
            // Sanity check: no data on image. Abort.
            if (top == yEnd)
                return new Rectangle(xStart, yStart, 0, 0);
            // Bottom = last found row which contains data
            for (bottom = yEnd - 1; bottom > top; bottom--)
            {
                Int32 index = bottom * stride;
                if (!RowClear(data, index, 4, xStart, xEnd, blankPixel))
                    break;
            }
            // Make bottom the first actually clear row.
            bottom++;
            // ============= X =============
            // Left = first found column which contains data
            for (left = xStart; left < xEnd; left++)
            {
                Int32 index = left * 4;
                if (!ColClear(data, index, stride, yStart, yEnd, blankPixel))
                    break;
            }
            // Right = last found row which contains data
            for (right = xEnd - 1; right > left; right--)
            {
                Int32 index = right * 4;
                if (!ColClear(data, index, stride, yStart, yEnd, blankPixel))
                    break;
            }
            // Make right the first actually clear column
            right++;
            // Calculate final rectangle values, including border.
            Int32 rectX = Math.Max(xStart, left - borderSizePixels);
            Int32 rectY = Math.Max(yStart, top - borderSizePixels);
            Int32 rectW = Math.Min(xEnd, right + borderSizePixels) - rectX;
            Int32 rectH = Math.Min(yEnd, bottom + borderSizePixels) - rectY;
            return new Rectangle(rectX, rectY, rectW, rectH);
        }

        public static Boolean RowClear(Byte[] data, Int32 index, Int32 pixelWidth, Int32 xStart, Int32 xEnd, Color blankPixel)
        {
            Boolean rowOk = true;
            Int32 start = index + pixelWidth * xStart;
            Int32 end = index + pixelWidth * xEnd;
            for (Int32 x = start; x < end; x += pixelWidth)
            {
                if (blankPixel.A != data[x + 3]) rowOk = false;
                else if (blankPixel.R != data[x + 2]) rowOk = false;
                else if (blankPixel.G != data[x + 1]) rowOk = false;
                else if (blankPixel.B != data[x + 0]) rowOk = false;
                if (!rowOk)
                    return false;
            }
            return true;
        }

        public static Boolean ColClear(Byte[] data, Int32 index, Int32 stride, Int32 yStart, Int32 yEnd, Color blankPixel)
        {
            Boolean colOk = true;
            Int32 start = index + stride * yStart;
            Int32 end = index + stride * yEnd;
            for (Int32 y = start; y < end; y += stride)
            {
                if (blankPixel.A != data[y + 3]) colOk = false;
                else if (blankPixel.R != data[y + 2]) colOk = false;
                else if (blankPixel.G != data[y + 1]) colOk = false;
                else if (blankPixel.B != data[y + 0]) colOk = false;
                if (!colOk)
                    return false;
            }
            return true;
        }

        public static void ReorderBits(Byte[] imageData, Int32 width, Int32 height, Int32 stride, PixelFormatter inputFormat, PixelFormatter outputFormat)
        {
            if (!inputFormat.BitsAmounts.SequenceEqual(outputFormat.BitsAmounts))
                throw new ArgumentException("Output format's bytes per pixel do not match input format!", "outputFormat");
            // This code relies on the fact that both formats have the same amount of bits per pixel,
            // meaning they can be written back to the same space.
            if (inputFormat.BitsAmounts.SequenceEqual(outputFormat.BitsAmounts))
            {
                // Actually has same bit amounts : simply reorder the data.
                for (Int32 y = 0; y < height; y++)
                {
                    Int32 offset = y*stride;
                    for (Int32 x = 0; x < width; x++)
                    {
                        UInt32[] rgbaValues = inputFormat.GetRawComponents(imageData, offset);
                        outputFormat.WriteRawComponents(imageData, offset, rgbaValues);
                        offset += 2;
                    }
                }

            }
            else
            {
                // Bits differ: convert through Color.
                for (Int32 y = 0; y < height; y++)
                {
                    Int32 offset = y*stride;
                    for (Int32 x = 0; x < width; x++)
                    {
                        Color col = inputFormat.GetColor(imageData, offset);
                        outputFormat.WriteColor(imageData, offset, col);
                        offset += 2;
                    }
                }
            }
        }

        public static Bitmap GrayImageFromCsv(String[] lines, Int32 startColumn, Int32 maxValue)
        {
            // maxValue cannot exceed 255
            maxValue = Math.Min(maxValue, 255);
            // Read lines; this gives us the data, and the height.
            //String[] lines = File.ReadAllLines(path);
            if (lines == null || lines.Length == 0)
                return null;
            Int32 bottom = lines.Length;
            // Trim any empty lines from the start and end.
            while (bottom > 0 && lines[bottom - 1].Trim().Length == 0)
                bottom--;
            if (bottom == 0)
                return null;
            Int32 top = 0;
            while (top < bottom && lines[top].Trim().Length == 0)
                top++;
            Int32 height = bottom - top;
            // This removes the top-bottom stuff; the new array is compact.
            String[][] values = new String[height][];
            for (Int32 i = top; i < bottom; i++)
                values[i - top] = lines[i].Split(',');
            // Find width: maximum csv line length minus the amount of columns to skip.
            Int32 width = values.Max(line => line.Length) - startColumn;
            if (width <= 0)
                return null;
            // Create the array. Since it's 8-bit, this is one byte per pixel.
            Byte[] imageArray = new Byte[width*height];
            // Parse all values into the array
            // Y = lines, X = csv values
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset = y*width;
                // Skip indices before "startColumn". Target offset starts from the start of the line anyway.
                for (Int32 x = startColumn; x < values[y].Length; x++)
                {
                    Int32 val;
                    // Don't know if Trim is needed here. Depends on the file.
                    if (Int32.TryParse(values[y][x].Trim(), out val))
                        imageArray[offset] = (Byte) Math.Max(0, Math.Min(val, maxValue));
                    offset++;
                }
            }
            // generate gray palette for the given range, by calculating the factor to multiply by.
            Double mulFactor = 255d / maxValue;
            Color[] palette = new Color[maxValue + 1];
            for (Int32 i = 0; i <= maxValue; i++)
            {
                // Away from zero rounding: 2.4 => 2 ; 2.5 => 3
                Byte v = (Byte)Math.Round(i * mulFactor, MidpointRounding.AwayFromZero);
                palette[i] = Color.FromArgb(v, v, v);
            }
            return BuildImage(imageArray, width, height, width, PixelFormat.Format8bppIndexed, palette, Color.White);
        }

        public static Bitmap GetGreyImage(Image img, Int32 width, Int32 height)
        {
            // get image data
            Bitmap b = new Bitmap(img, width, height);
            BitmapData sourceData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Int32 stride = sourceData.Stride;
            Byte[] data = new Byte[stride * b.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);            
            // iterate
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; x++)
                {
                    Byte colB = data[offset + 0]; // B
                    Byte colG = data[offset + 1]; // G
                    Byte colR = data[offset + 2]; // R
                    //Int32 ColA = data[offset + 3]; // A
                    Byte grayValue = ColorUtils.GetGreyValue(colR, colG, colB);
                    data[offset + 0] = grayValue; // B
                    data[offset + 1] = grayValue; // G
                    data[offset + 2] = grayValue; // R
                    data[offset + 3] = 0xFF; // A
                    offset += 4;
                }
            }
            Marshal.Copy(data, 0, sourceData.Scan0, data.Length);
            b.UnlockBits(sourceData);
            return b;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// Found on stackoverflow: https://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <param name="makeSquare">True to return a square image by centering on the largest dimension.</param>
        /// <param name="smooth">Use high-quality smooth resize.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, Int32 width, Int32 height, Boolean makeSquare, Boolean smooth)
        {
            Int32 padX = 0;
            Int32 padY = 0;
            Int32 imageWidth = width;
            Int32 imageHeight = height;
            if (makeSquare)
            {
                Int32 padding = Math.Abs(width - height)/2;
                Int32 max = Math.Max(width, height);
                if (width > height)
                    padY = padding;
                else
                    padX = padding;
                imageWidth = max;
                imageHeight = max;
            }
            Rectangle destRect = new Rectangle(padX, padY, width, height);
            Bitmap destImage = new Bitmap(imageWidth, imageHeight);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                if (smooth)
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                }
                else
                {
                    graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = SmoothingMode.None;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                }
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }
        
        public static Byte[] BayerToRgb2x2Orig(Byte[] arr, ref Int32 width, ref Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 actualWidth = width - 1;
            Int32 actualHeight = height - 1;
            Int32 actualStride = actualWidth*3;
            Byte[] result = new Byte[actualStride*actualHeight];
            for (Int32 y = 0; y < actualHeight; y++)
            {
                Int32 curPtr = y*stride;
                Int32 resPtr = y*actualStride;
                for (Int32 x = 0; x < actualWidth; x++)
                {
                    // Get correct colour components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1);
                    Boolean blueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte cornerCol1 = isGreen ? arr[curPtr + 1] : arr[curPtr];
                    Byte cornerCol2 = isGreen ? arr[curPtr + stride] : arr[curPtr + stride + 1];
                    Byte greenCol1 = isGreen ? arr[curPtr] : arr[curPtr + 1];
                    Byte greenCol2 = isGreen ? arr[curPtr + stride + 1] : arr[curPtr + stride];
                    Byte blueCol = blueRow ? cornerCol1 : cornerCol2;
                    Byte redCol = blueRow ? cornerCol2 : cornerCol1;
                    // 24bpp RGB is saved as [B, G, R].
                    // Blue
                    result[resPtr + 0] = blueCol;
                    // Green
                    result[resPtr + 1] = (Byte) ((greenCol1 + greenCol2)/2);
                    // Red
                    result[resPtr + 2] = redCol;
                    curPtr++;
                    resPtr+=3;
                }
            }
            height = actualHeight;
            width = actualWidth;
            stride = actualStride;
            return result;
        }

        public static Byte[] BayerToRgb2x2CopyExpand(Byte[] arr, Int32 width, Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 processWidth = width;
            Int32 processHeight = height;
            if (width > 1 && height > 1)
            {
                arr = ChangeStride(arr, stride, height, width + 1, false, 0);
                stride = width + 1;
                processWidth = width + 1;
                Byte[] lastColB = CopyFrom8bpp(arr, width, height, stride, new Rectangle(width - 2, 0, 1, height));
                PasteOn8bpp(arr, processWidth, height, stride, lastColB, 1, height, 1, new Rectangle(width, 0, 1, height), null, true);
                arr = ChangeHeight(arr, stride, height, height + 1, false, 0);
                processHeight = height + 1;
                Byte[] lastRowB = CopyFrom8bpp(arr, processWidth, processHeight, stride, new Rectangle(0, height - 2, processWidth, 1));
                PasteOn8bpp(arr, processWidth, processHeight, stride, lastRowB, processWidth, 1, processWidth, new Rectangle(0, height, processWidth, 1), null, true);
            }

            Int32 lastCol = processWidth;
            Int32 lastRow = processHeight;
            Int32 actualStride = width * 3;
            Byte[] result = new Byte[actualStride * height];
            for (Int32 y = 0; y < height; y++)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < width; x++)
                {
                    // Get correct colour components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colours and center are green.
                    Boolean isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte valGreen;
                    Byte valRed;
                    Byte valBlue;

                    Byte pxCol = arr[curPtr];
                    Byte? tpCol1 = null;
                    Byte? tpCol2 = null;
                    Byte? tpCol3 = null;
                    Byte? lfCol = null;
                    Byte? rtCol = x == lastCol ? (Byte?)null : arr[curPtr + 1];
                    Byte? btCol1 = null;
                    Byte? btCol2 = y == lastRow ? (Byte?)null : arr[curPtr + stride];
                    Byte? btCol3 = y == lastRow || x == lastCol ? (Byte?)null : arr[curPtr + stride + 1];

                    if (isGreen)
                    {
                        valGreen = GetAverageCol(tpCol1, tpCol3, btCol1, btCol3, pxCol);
                        Byte verVal = GetAverageCol(tpCol2, btCol2);
                        Byte horVal = GetAverageCol(lfCol, rtCol);
                        valRed = isBlueRow ? verVal : horVal;
                        valBlue = isBlueRow ? horVal : verVal;
                    }
                    else
                    {
                        valGreen = GetAverageCol(tpCol2, rtCol, btCol2, lfCol);
                        Byte cornerCol = GetAverageCol(tpCol1, tpCol3, btCol1, btCol3);
                        valRed = isBlueRow ? cornerCol : pxCol;
                        valBlue = isBlueRow ? pxCol : cornerCol;
                    }
                    result[resPtr + 0] = valBlue;
                    result[resPtr + 1] = valGreen;
                    result[resPtr + 2] = valRed;
                    curPtr++;
                    resPtr += 3;
                }
            }
            stride = actualStride;
            return result;
        }

        public static Byte[] BayerToRgb3x3(Byte[] arr, Int32 width, Int32 height, ref Int32 stride, Boolean greenFirst, Boolean blueRowFirst)
        {
            Int32 lastCol = width - 1;
            Int32 lastRow = height - 1;
            Int32 actualStride = width * 3;
            Byte[] result = new Byte[actualStride * height];
            for (Int32 y = 0; y < height; y++)
            {
                Int32 curPtr = y * stride;
                Int32 resPtr = y * actualStride;
                for (Int32 x = 0; x < width; x++)
                {
                    // Get correct colour components from sliding window
                    Boolean isGreen = (x + y) % 2 == (greenFirst ? 0 : 1); // all corner colours and center are green.
                    Boolean isBlueRow = y % 2 == (blueRowFirst ? 0 : 1);
                    Byte valGreen;
                    Byte valRed;
                    Byte valBlue;

                    Byte cntrCol = arr[curPtr];
                    Byte? tplfCol = y == 0 || x == 0       ? (Byte?)null : arr[curPtr - stride - 1];
                    Byte? tpcnCol = y == 0                 ? (Byte?)null : arr[curPtr - stride];
                    Byte? tprtCol = y == 0 || x == lastCol ? (Byte?)null : arr[curPtr - stride + 1];
                    Byte? cnlfCol = x == 0                  ? (Byte?)null : arr[curPtr - 1];
                    Byte? cnrtCol = x == lastCol            ? (Byte?)null : arr[curPtr + 1];
                    Byte? btlfCol = y == lastRow || x == 0 ? (Byte?)null : arr[curPtr + stride - 1];
                    Byte? btcnCol = y == lastRow           ? (Byte?)null : arr[curPtr + stride];
                    Byte? btrtCol = y == lastRow || x == lastCol ? (Byte?)null : arr[curPtr + stride + 1];

                    if (isGreen)
                    {
                        valGreen = GetAverageCol(tplfCol, tprtCol, btlfCol, btrtCol, cntrCol);
                        Byte verVal = GetAverageCol(tpcnCol, btcnCol);
                        Byte horVal = GetAverageCol(cnlfCol, cnrtCol);
                        valRed = isBlueRow ? verVal : horVal;
                        valBlue = isBlueRow ? horVal : verVal;
                    }
                    else
                    {
                        valGreen = GetAverageCol(tpcnCol, cnrtCol, btcnCol, cnlfCol);
                        Byte cornerCol = GetAverageCol(tplfCol, tprtCol, btlfCol, btrtCol);
                        valRed = isBlueRow ? cornerCol : cntrCol;
                        valBlue = isBlueRow ? cntrCol : cornerCol;                        
                    }
                    result[resPtr + 0] = valBlue;
                    result[resPtr + 1] = valGreen;
                    result[resPtr + 2] = valRed;
                    curPtr++;
                    resPtr += 3;
                }
            }
            stride = actualStride;
            return result;
        }

        private static Byte GetAverageCol(params Byte?[] cols)
        {
            Int32 colsCount = 0;
            foreach(Byte? col in cols)
                if (col.HasValue) colsCount++;
            Int32 avgVal = 0;
            foreach (Byte? col in cols)
                avgVal += col.GetValueOrDefault();
            return colsCount == 0 ? (Byte)0x80 : (Byte)(avgVal / colsCount);
        }


        /// <summary>
        ///     Exracts a channel as two-dimensional array
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="channelNr">0 = B, 1 = G, 2 = R</param>
        /// <returns></returns>
        public static Int32[,] GetChannel(Bitmap image, Int32 channelNr)
        {
            if (channelNr >= 3 || channelNr < 0)
                throw new IndexOutOfRangeException();
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] dataBytes = GetImageData(image, out stride, PixelFormat.Format24bppRgb);
            Int32[,] channel = new Int32[height, width];
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; x++)
                {
                    channel[y, x] = dataBytes[offset + channelNr];
                    offset += 3;
                }
            }
            return channel;
        }

        public static Int32[,] ReduceChannel(Int32[,] origChannel, Int32 lossfactor)
        {
            Int32 newHeight = origChannel.GetLength(0) / lossfactor;
            Int32 newWidth = origChannel.GetLength(1) / lossfactor;
            // to avoid rounding errors
            Int32 origHeight = newHeight * lossfactor;
            Int32 origWidth = newWidth *lossfactor;
            Int32[,] newChannel = new Int32[newHeight, newWidth];
            Int32 newX = 0;
            Int32 newY = 0;
            for (Int32 y = 1; y < origHeight; y += lossfactor)
            {
                newX = 0;
                for (Int32 x = 1; x < origWidth; x += lossfactor)
                {
                    newChannel[newY, newX] = origChannel[y, x];
                    newX++;
                }
                newY++;
            }
            return newChannel;
        }

        public static Bitmap CreateImageFromChannels(Int32[,] redChannel, Int32[,] greenChannel, Int32[,] blueChannel)
        {
            Int32 width = greenChannel.GetLength(1);
            Int32 height = greenChannel.GetLength(0);
            Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            Int32 stride = bmpData.Stride;
            // stride is the actual line width in bytes.
            Int32 bytes = stride * height;
            Byte[] PixelValues = new Byte[bytes];
            for (Int32 y = 0; y < height; y++)
            {
                // use stride to get the start offset of each line
                Int32 offset = y * stride;
                for (Int32 x = 0; x < width; x++)
                {
                    PixelValues[offset + 0] = (Byte)blueChannel[y, x];
                    PixelValues[offset + 1] = (Byte)greenChannel[y, x];
                    PixelValues[offset + 2] = (Byte)redChannel[y, x];
                    offset += 3;
                }
            }
            Marshal.Copy(PixelValues, 0, bmpData.Scan0, bytes);
            result.UnlockBits(bmpData);
            return result;
        }

        /// <summary>
        /// Detects darker or brighter spots on the image by brightness threshold, and returns their center points.
        /// Built for https://stackoverflow.com/q/50277978/395685 but never posted since it's a homework question.
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="detectDark">Detect dark spots. False to detect bright drops</param>
        /// <param name="brightnessThreshold">Brightness threshold needed to see a pixel as "bright"</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the returned lists only contain the edges of the blobs. This saves a lot of memory.</param>
        /// <returns>A list of points indicating the centers of all found spots.</returns>
        public static List<Point> FindPoints(Bitmap image, Boolean detectDark, Single brightnessThreshold, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            List<List<Point>> blobs = FindBlobs(image, detectDark, brightnessThreshold, mergeThreshold, getEdgesOnly);
            return blobs.Select(GetBlobCenter).ToList();
        }

        /// <summary>
        /// Detects darker or brighter spots on the image by brightness threshold, and returns their list of points.
        /// Built for https://stackoverflow.com/q/50277978/395685 but never posted since it's a homework question.
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="detectDark">Detect dark spots. False to detect bright drops</param>
        /// <param name="brightnessThreshold">Brightness threshold. Use -1 to attempt automatic levelling.</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the returned lists only contain the edges of the blobs. This saves a lot of memory.</param>
        /// <returns>A list of points indicating the centers of all found spots.</returns>
        public static List<List<Point>> FindBlobs(Bitmap image, Boolean detectDark, Single brightnessThreshold, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            Boolean detectVal = !detectDark;
            Int32 width = image.Width;
            Int32 height = image.Height;
            // Binarization: get 32-bit data
            Int32 stride;
            Byte[] data = GetImageData(image, out stride, PixelFormat.Format32bppArgb);
            // Binarization: get brightness
            Single[,] brightness = new Single[height, width];
            Int32 offset = 0;
            Byte groups = 255;
            for (Int32 y = 0; y < height; y++)
            {
                // use stride to get the start offset of each line
                Int32 usedOffset = offset;
                for (Int32 x = 0; x < width; x++)
                {
                    // get colour
                    Byte blu = data[usedOffset + 0];
                    Byte grn = data[usedOffset + 1];
                    Byte red = data[usedOffset + 2];
                    Color c = Color.FromArgb(red, grn, blu);
                    brightness[y, x] = c.GetBrightness();
                    usedOffset += 4;
                }
                offset += stride;
            }
            if (brightnessThreshold < 0)
            {
                Dictionary<Byte, Int32> historigram = new Dictionary<Byte, Int32>();
                for (Int32 y = 0; y < height; y++)
                {
                    for (Int32 x = 0; x < width; x++)
                    {
                        Byte val = (Byte) (brightness[y, x] * groups);
                        Int32 num;
                        historigram.TryGetValue(val, out num);
                        historigram[val] = num + 1;
                    }
                }
                List<KeyValuePair<Byte, Int32>> sortedHistorigram = historigram.OrderBy(x => x.Value).ToList();
                sortedHistorigram.Reverse();
                sortedHistorigram = sortedHistorigram.Take(groups * 9 / 10).ToList();
                Byte maxBrightness = sortedHistorigram.Max(x => x.Key);
                Byte minBrightness = sortedHistorigram.Min(x => x.Key);
                // [............m.............T.............M............]
                // still not very good... need to find some way to detect image highlights. Probably needs K-means clustering...
                brightnessThreshold = (minBrightness + (maxBrightness - minBrightness) * .5f) / groups;
            }
            // Binarization: convert to 1-byte-per-pixel array of 1/0 values based on a brightness threshold
            Boolean[,] dataBw = new Boolean[height, width];
            for (Int32 y = 0; y < height; y++)
                for (Int32 x = 0; x < width; x++)
                    dataBw[y, x] = brightness[y, x] > brightnessThreshold;

            // Detect blobs.
            // Coult technically simplify the required Func<> to remove the imgData and directly reference dataBw, but meh.
            Func<Boolean[,], Int32, Int32, Boolean> clearsThreshold = (imgData, yVal, xVal) => imgData[yVal, xVal] == detectVal;
            return FindBlobs(dataBw, width, height, clearsThreshold, true, mergeThreshold, getEdgesOnly);
        }

        /// <summary>
        /// Detects a list of all blobs in the image, and merges any with bounds that intersect with each other according to the 'mergeThreshold' parameter.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static List<List<Point>> FindBlobs<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            List<Boolean[,]> inBlobs = new List<Boolean[,]>();
            List<List<Point>> blobs = FindBlobs(data, width, height, clearsThreshold, allEightEdges, getEdgesOnly, out inBlobs);
            MergeBlobs(blobs, width, height, null, mergeThreshold);
            return blobs;
        }


        /// <summary>
        /// Detects a list of all blobs in the image, and merges any with bounds that intersect with each other according to the 'mergeThreshold' parameter.
        /// Returns the result as Boolean[,] array.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="mergeThreshold">The found spots are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static List<Boolean[,]> FindBlobsAsBooleans<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Int32 mergeThreshold, Boolean getEdgesOnly)
        {
            List<Boolean[,]> inBlobs = new List<Boolean[,]>();
            List<List<Point>> blobs = FindBlobs(data, width, height, clearsThreshold, allEightEdges, getEdgesOnly, out inBlobs);
            MergeBlobs(blobs, width, height, inBlobs, mergeThreshold);
            return inBlobs;
        }

        /// <summary>
        /// Detects a list of all blobs in the image.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        /// <param name="inBlobs">Output parameter for receiving the blobs as boolean[,] arrays.</param>
        public static List<List<Point>> FindBlobs<T>(T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, Boolean allEightEdges, Boolean getEdgesOnly, out List<Boolean[,]> inBlobs)
        {
            List<List<Point>> blobs = new List<List<Point>>();
            inBlobs = new List<Boolean[,]>();
            for (Int32 y = 0; y < height; y++)
                for (Int32 x = 0; x < width; x++)
                    BuildBlobsCollection(x, y, data, width, height, clearsThreshold, blobs, inBlobs, allEightEdges, getEdgesOnly);
            return blobs;
        }

        /// <summary>
        /// Merge any blobs that fall in each other's square bounds, to reduce the amount of stray pixels.
        /// Bounds are inflated by the amount of pixels specified in mergeThreshold.
        /// </summary>
        /// <param name="blobs">Collection of blobs</param>
        /// <param name="width">width of full image. Use -1 to detect from blob bounds.</param>
        /// <param name="height">Height of full image. Use -1 to detect from blob bounds.</param>
        /// <param name="inBlobs"></param>
        /// <param name="mergeThreshold">The found blobs are merged based on their square bounds. This is the amount of added pixels when checking these bounds. Use -1 to disable all merging.</param>
        /// <returns></returns>
        public static void MergeBlobs(List<List<Point>> blobs, Int32 width, Int32 height, List<Boolean[,]> inBlobs, Int32 mergeThreshold)
        {
            if (width == -1 || height == -1)
            {
                width = -1;
                height = -1;
                foreach (List<Point> blob in blobs)
                {
                    foreach (Point point in blob)
                    {
                        if (width < point.X)
                            width = point.X;
                        if (height < point.Y)
                            height = point.Y;
                    }
                }
                // because width and height are sizes, not highest ccordinates.
                width++;
                height++;
            }
            Boolean continueMerge = mergeThreshold >= 0;
            List<Rectangle> collBounds = new List<Rectangle>();
            List<Rectangle> collBoundsInfl = new List<Rectangle>();
            Rectangle imageBounds = new Rectangle(0, 0, width, height);
            if (continueMerge)
            {
                foreach (List<Point> coll in blobs)
                    collBounds.Add(GetBlobBounds(coll));
                foreach (Rectangle rect in collBounds)
                {
                    Rectangle r = Rectangle.Inflate(rect, mergeThreshold, mergeThreshold);
                    collBoundsInfl.Add(Rectangle.Intersect(imageBounds, r));
                }
            }
            while (continueMerge)
            {
                continueMerge = false;
                for (Int32 i = 0; i < blobs.Count; i++)
                {
                    List<Point> blob1 = blobs[i];
                    Boolean[,] inBlob1 = inBlobs == null ? null : inBlobs[i];
                    if (blob1.Count == 0)
                        continue;
                    Rectangle checkBounds = collBoundsInfl[i];
                    for (Int32 j = 0; j < blobs.Count; j++)
                    {
                        if (i == j)
                            continue;
                        List<Point> blob2 = blobs[j];
                        Boolean[,] inBlob2 = inBlobs == null ? null : inBlobs[j];
                        if (blob2.Count == 0)
                            continue;
                        Rectangle bounds2 = collBounds[j];
                        if (checkBounds.IntersectsWith(bounds2))
                        {
                            // should be safe without checks; there are already
                            // checks against duplicates in these collections.
                            continueMerge = true;
                            blob1.AddRange(blob2);
                            if (inBlobs != null)
                                foreach (Point p in blob2)
                                    inBlob1[p.Y, p.X] = true;
                            Rectangle rect1New = GetBlobBounds(blob1);
                            collBounds[i] = rect1New;
                            Rectangle rect1NewInfl = Rectangle.Inflate(rect1New, mergeThreshold, mergeThreshold);
                            collBoundsInfl[i] = Rectangle.Intersect(imageBounds, rect1NewInfl);
                            blob2.Clear();
                            // don't bother clearing inBlob2; it doesn't actually get referenced, and gets filtered out at the end.
                            collBounds[j] = new Rectangle(0, 0, 0, 0);
                        }
                    }
                }
            }
            // Filter out removed entries.
            Int32[] nonEmptyIndices = Enumerable.Range(0, blobs.Count).Where(i => blobs[i].Count > 0).ToArray();
            if (inBlobs != null)
            {
                List<Boolean[,]> trimmedInBlobs = new List<Boolean[,]>();
                foreach (Int32 i in nonEmptyIndices)
                    trimmedInBlobs.Add(inBlobs[i]);
                inBlobs.Clear();
                inBlobs.AddRange(trimmedInBlobs);
            }
            List<List<Point>> trimmedBlobs = new List<List<Point>>();
            foreach (Int32 i in nonEmptyIndices)
                trimmedBlobs.Add(blobs[i]);
            blobs.Clear();
            blobs.AddRange(trimmedBlobs);
        }

        public static Point GetBlobCenter(List<Point> blob)
        {
            if (blob.Count == 0)
                return Point.Empty;
            Rectangle bounds = GetBlobBounds(blob);
            return new Point(bounds.X + (bounds.Width - 1) / 2, bounds.Y + (bounds.Height - 1) / 2);
        }

        /// <summary>
        /// Builds a list of all points adjacent to the current point, and adds it to the list of point collections.
        /// If the point was already found in one of the collections, the function does nothing.
        /// Loop this over every pixel of an image to detect all blobs.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="pointX">X-coordinate of the current point.</param>
        /// <param name="pointY">Y-coordinate of the current point.</param>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean</param>
        /// <param name="blobs">List of point collections.</param>
        /// <param name="inBlobs">The list of point collections represented as boolean arrays, for very quick checks to see if a set of coordinates is in a collection.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="getEdgesOnly">True to make the lists in 'blobs' only contain the edge points of the blobs. The 'inBlobs' items will still have all points marked.</param>
        public static void BuildBlobsCollection<T>(Int32 pointX, Int32 pointY, T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, List<List<Point>> blobs, List<Boolean[,]> inBlobs, Boolean allEightEdges, Boolean getEdgesOnly)
        {
            // If the point does not equal the value to detect, abort.
            if (!clearsThreshold(data, pointY, pointX))
                return;
            // if the point is already in any of the collections, abort.
            foreach (Boolean[,] inCheckBlob in inBlobs)
                if (inCheckBlob[pointY, pointX])
                    return;
            List<Point> blob;
            // existence check optimisation in the form of a boolean grid that is kept synced with the points in the collection.
            Boolean[,] inBlob;
            BuildBlob(pointX, pointY, data, width, height, clearsThreshold, out blob, out inBlob, allEightEdges, getEdgesOnly);
            // should never happen; it starts off with the given pixel.
            if (blob.Count == 0)
                return;
            blobs.Add(blob);
            inBlobs.Add(inBlob);
        }

        /// <summary>
        /// Builds a list of all points adjacent to the current point, and adds it to the list of point collections.
        /// </summary>
        /// <typeparam name="T">Type of the list to detect equal neighbours in.</typeparam>
        /// <param name="pointX">X-coordinate of the current point.</param>
        /// <param name="pointY">Y-coordinate of the current point.</param>
        /// <param name="data">Image data array. It is processed as one pixel per coordinate.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="clearsThreshold">Function to check if the pixel at the given coordinates clears the threshold. Should be of the format (imgData, yVal, xVal) => Boolean</param>
        /// <param name="blob">Point collection that is returned.</param>
        /// <param name="inBlob">The point collection represented as boolean array, for very quick checks to see if a set of coordinates is in the collection.</param>
        /// <param name="allEightEdges">When scanning for pixels to add to the blob, scan all eight surrounding pixels rather than just top, left, bottom, right.</param>
        /// <param name="edgeOnly">True to make the lists in 'blob' only contain the edge points of the blob. The 'inBlob' array will still have all points marked.</param>
        public static void BuildBlob<T>(Int32 pointX, Int32 pointY, T data, Int32 width, Int32 height, Func<T, Int32, Int32, Boolean> clearsThreshold, out List<Point> blob, out Boolean[,] inBlob, Boolean allEightEdges, Boolean edgeOnly)
        {
            // set up return variables
            blob = new List<Point>();
            inBlob = new Boolean[height, width];

            // If the given point does not equal the value to detect, abort.
            if (!clearsThreshold(data, pointY, pointX))
                return;
            
            // setting up variables to use...
            List<Point> edgeCollection = new List<Point>();
            Int32 lastX = width - 1;
            Int32 lastY = height - 1;
            List<Point> newEdgeCollection = new List<Point>();
            Boolean[,] inNewEdgeCollection = new Boolean[height, width];

            // starting point
            edgeCollection.Add(new Point(pointX, pointY));

            // Start looking.
            while (edgeCollection.Count > 0)
            {
                if (!edgeOnly)
                    blob.AddRange(edgeCollection);
                foreach (Point p in edgeCollection)
                {
                    Int32 x = p.X;
                    Int32 y = p.Y;
                    inBlob[y, x] = true;
                    if (edgeOnly &&
                        (x == 0 || y == 0 || x == lastX || y == lastY
                        || !clearsThreshold(data, y - 1, x)
                        || !clearsThreshold(data, y, x - 1)
                        || !clearsThreshold(data, y, x + 1)
                        || !clearsThreshold(data, y + 1, x)))
                        blob.Add(p);
                }
                // Search all neighbouring pixels of the current neighbours list.
                foreach (Point ed in edgeCollection)
                {
                    // gets all 8 neighbouring pixels.
                    List<Point> neighbours = GetNeighbours(ed.X, ed.Y, lastX, lastY, allEightEdges);
                    foreach (Point p in neighbours)
                    {
                        Int32 x = p.X;
                        Int32 y = p.Y;
                        if (!inBlob[y, x] && !inNewEdgeCollection[y, x]
                            && clearsThreshold(data, y, x))
                        {
                            newEdgeCollection.Add(p);
                            inNewEdgeCollection[y, x] = true;
                        }
                    }
                }
                edgeCollection.Clear();
                edgeCollection.AddRange(newEdgeCollection);
                newEdgeCollection.Clear();
                Array.Clear(inNewEdgeCollection, 0, inNewEdgeCollection.Length);
            }
        }

        private static List<Point> GetNeighbours(Int32 x, Int32 y, Int32 lastX, Int32 lastY, Boolean allEight)
        {
            List<Point> neighbours = new List<Point>();
            if (allEight && x > 0 && y > 0)
                neighbours.Add(new Point(x - 1, y - 1));
            if (y > 0)
                neighbours.Add(new Point(x, y - 1));
            if (allEight && x < lastX && y > 0)
                neighbours.Add(new Point(x + 1, y - 1));
            if (x > 0)
                neighbours.Add(new Point(x - 1, y));
            if (x < lastX)
                neighbours.Add(new Point(x + 1, y));
            if (allEight && x > 0 && y < lastY)
                neighbours.Add(new Point(x - 1, y + 1));
            if (y < lastY)
                neighbours.Add(new Point(x, y + 1));
            if (allEight && x < lastX && y < lastY)
                neighbours.Add(new Point(x + 1, y + 1));
            return neighbours;
        }

        public static Rectangle GetBlobBounds(List<Point> blob)
        {
            if (blob.Count == 0)
                return new Rectangle(0, 0, 0, 0);
            Int32 minX = Int32.MaxValue;
            Int32 maxX = 0;
            Int32 minY = Int32.MaxValue;
            Int32 maxY = 0;
            foreach (Point p in blob)
            {
                minX = Math.Min(minX, p.X);
                maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        
        public static List<Point> GetBlobEdgePoints(List<Point> blob, Int32 imageWidth, Int32 imageHeight)
        {
            Boolean[,] pointInList = new Boolean[imageHeight, imageWidth];
            foreach (Point p in blob)
                pointInList[p.Y, p.X] = true;
            List<Point> edgePoints = new List<Point>();
            Int32 lastX = imageWidth - 1;
            Int32 lastY = imageHeight - 1;
            foreach (Point p in blob)
            {
                Int32 x = p.X;
                Int32 y = p.Y;
                // Image edge is obviously a blob edge too.
                if (x == 0 || y == 0 || x == lastX || y == lastY
                    || !pointInList[y - 1, x]
                    || !pointInList[y, x - 1]
                    || !pointInList[y, x + 1]
                    || !pointInList[y + 1, x])
                    edgePoints.Add(p);
            }
            return edgePoints;
        }
        
    }
}
