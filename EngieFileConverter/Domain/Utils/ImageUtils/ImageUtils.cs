using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nyerguds.ImageManipulation
{
    public static class ImageUtils
    {

        public static ColorPalette GetColorPalette(Color[] colors, PixelFormat pf)
        {
            ColorPalette cp;
            using (Bitmap bm = new Bitmap(1, 1, pf))
                cp = bm.Palette;
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
            using (Bitmap bm = new Bitmap(ms))
                return CloneImage(bm);
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
                    using (Bitmap img2 = BuildImage(eightBitData, image.Width, image.Height, stride, PixelFormat.Format8bppIndexed, image.Palette.Entries, Color.Black))
                        img2.Save(ms, saveFormat);
                }
                else if (saveFormat.Equals(ImageFormat.Png))
                    BitmapHandler.GetPngImageData(image, 0, false);
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
                case 1:
                    return PixelFormat.Format1bppIndexed;
                case 4:
                    return PixelFormat.Format4bppIndexed;
                case 8:
                    return PixelFormat.Format8bppIndexed;
                default:
                    throw new NotSupportedException("Unsupported indexed pixel format '" + bpp + "'!");
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
                        newImageData[outputOffs] = (Byte) (Math.Min((c.R * 0.3) + (c.G * 0.59) + (c.B * 0.11), 255) / divvalue);
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

        /// <summary>
        /// Compares the data of two images of the same non-indexed pixel format, taking their respective strides into account.
        /// </summary>
        /// <param name="imageData1">Data of the first image.</param>
        /// <param name="stride1">Stride of the first image.</param>
        /// <param name="imageData2">Data of the second image.</param>
        /// <param name="stride2">Stride of the second image.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="pf">Pixel format of the images.</param>
        /// <returns>True if the content matches.</returns>
        public static Boolean CompareHiColorImages(Byte[] imageData1, Int32 stride1, Byte[] imageData2, Int32 stride2, Int32 width, Int32 height, PixelFormat pf)
        {
            Int32 byteSize = Image.GetPixelFormatSize(pf) / 8;
            Int32 index1 = 0;
            Int32 index2 = 0;
            for (Int32 y = 0; y < height; y++)
            {
                Int32 offset1 = index1;
                Int32 offset2 = index2;

                for (Int32 x = 0; x < width; x++)
                {
                    for (Int32 n = 0; n > byteSize; n++)
                        if (imageData1[offset1 + n] != imageData2[offset2 + n])
                            return false;
                    offset1 += byteSize;
                    offset2 += byteSize;
                }
                index1 += stride1;
                index2 += stride2;
            }
            return true;
        }

        /// <summary>
        /// MAtches the data from an 8-bit image to a different palette. This handles the full stride.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="stride">Image stride.</param>
        /// <param name="height">Image height.</param>
        /// <param name="sourcePalette">Palette of the source data.</param>
        /// <param name="targetPalette">Target palette to convert to.</param>
        /// <returns>The converted image data.</returns>
        public static Byte[] Match8BitDataToPalette(Byte[] imageData, Int32 stride, Int32 height, Color[] sourcePalette, Color[] targetPalette)
        {
            Byte[] newImageData = new Byte[stride * height];
            for (Int32 i = 0; i < imageData.Length; i++)
            {
                Int32 currentVal = imageData[i];
                Color c = currentVal < sourcePalette.Length ? sourcePalette[imageData[i]] : Color.Black;
                newImageData[i] = (Byte) ColorUtils.GetClosestPaletteIndexMatch(c, targetPalette, null);
            }
            return newImageData;
        }

        /// <summary>
        /// Converts an image to paletted format.
        /// </summary>
        /// <param name="originalImage">Original image</param>
        /// <param name="bpp">Desired bits per pixel for the paletted image (should be less than or equal to 8)</param>
        /// <param name="palette">The colour palette.</param>
        /// <returns>A bitmap of the desired colour depth matched to the given palette.</returns>
        public static Bitmap ConvertToPalette(Bitmap originalImage, Int32 bpp, Color[] palette)
        {
            PixelFormat pf = GetIndexedPixelFormat(bpp);
            Int32 stride;
            Byte[] imageData;
            if (originalImage.PixelFormat != PixelFormat.Format32bppArgb)
            {
                using (Bitmap bm32bpp = PaintOn32bpp(originalImage, Color.Black))
                    imageData = GetImageData(bm32bpp, out stride);
            }
            else
                imageData = GetImageData(originalImage, out stride);
            Byte[] palettedData = Convert32BitToPaletted(imageData, originalImage.Width, originalImage.Height, bpp, bpp == 1, palette, ref stride);
            return BuildImage(palettedData, originalImage.Width, originalImage.Height, stride, pf, palette, Color.Black);
        }

        /// <summary>
        /// Converts 32 bit per pixel image data to match a given colour palette, and returns it as array in the desired pixel format.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="bpp">Bits per pixel.</param>
        /// <param name="bigEndianBits">True to use big endian ordered data in the indexed array if <paramref name="bpp "/> is less than 8.</param>
        /// <param name="palette">Colour palette to match to.</param>
        /// <param name="stride">Stride. Will be adjusted by the function.</param>
        /// <returns></returns>
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
            Int32 firstTransIndex = transparentIndices.Count > 0 ? transparentIndices[0] : -1;
            for (Int32 y = 0; y < height; y++)
            {
                Int32 inputOffs = y * stride;
                Int32 outputOffs = y * width;
                for (Int32 x = 0; x < width; x++)
                {
                    Color c = Color.FromArgb(imageData[inputOffs + 3], imageData[inputOffs + 2], imageData[inputOffs + 1], imageData[inputOffs]);
                    if (firstTransIndex >= 0 && c.A < 128)
                        newImageData[outputOffs] = (Byte)firstTransIndex;
                    else
                        newImageData[outputOffs] = (Byte) ColorUtils.GetClosestPaletteIndexMatch(c, palette, transparentIndices);
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
        /// Gets the raw bytes from an image in its original pixel format.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <returns>The raw bytes of the image.</returns>
        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride)
        {
            return GetImageData(sourceImage, out stride, sourceImage.PixelFormat, false);
        }

        /// <summary>
        /// Gets the raw bytes from an image, in the given pixel format.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <param name="desiredPixelFormat">PixelFormat in which the data needs to be retrieved. Use <paramref name="sourceImage"/>.PixelFormat for no conversion.</param>
        /// <returns>The raw bytes of the image.</returns>
        /// <remarks>
        ///   Note that <paramref name="desiredPixelFormat"/> has limitations when it comes to indexed formats:
        ///   giving an indexed pixel format if the sourceImage is an indexed image with a lower bpp will throw an exception, since GDI+ does not support that,
        ///   and if you give an indexed pixel format and the source is non-indexed, the colours will be matched to the standard Windows palette for that format.
        /// </remarks>
        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride, PixelFormat desiredPixelFormat)
        {
            return GetImageData(sourceImage, out stride, desiredPixelFormat, false);
        }

        /// <summary>
        /// Gets the raw bytes from an image in its original pixel format.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <param name="collapseStride">Collapse the stride to the minimum required for the image data.</param>
        /// <returns>The raw bytes of the image.</returns>
        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride, Boolean collapseStride)
        {
            return GetImageData(sourceImage, out stride, sourceImage.PixelFormat, collapseStride);
        }
        /// <summary>
        /// Gets the raw bytes from an image, in the desired <see cref="System.Drawing.Imaging.PixelFormat">PixelFormat</see>.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <param name="desiredPixelFormat">PixelFormat in which the data needs to be retrieved. Use <paramref name="sourceImage"/>.PixelFormat for no conversion.</param>
        /// <param name="collapseStride">Collapse the stride to the minimum required for the image data.</param>
        /// <returns>The raw bytes of the image.</returns>
        /// <remarks>
        ///   Note that <paramref name="desiredPixelFormat"/> has limitations when it comes to indexed formats:
        ///   giving an indexed pixel format if the sourceImage is an indexed image with a lower bpp will throw an exception, since GDI+ does not support that,
        ///   and if you give an indexed pixel format and the source is non-indexed, the colours will be matched to the standard Windows palette for that format.
        /// </remarks>
        public static Byte[] GetImageData(Bitmap sourceImage, out Int32 stride, PixelFormat desiredPixelFormat, Boolean collapseStride)
        {
            if (sourceImage == null)
                throw new ArgumentNullException("sourceImage", "Source image is null!");
            PixelFormat sourcePf = sourceImage.PixelFormat;
            Int32 width = sourceImage.Width;
            Int32 height = sourceImage.Height;

            if (sourcePf != desiredPixelFormat && (sourcePf & PixelFormat.Indexed) != 0 && (desiredPixelFormat & PixelFormat.Indexed) != 0
                && Image.GetPixelFormatSize(sourcePf) > Image.GetPixelFormatSize(desiredPixelFormat))
                throw new ArgumentException("Cannot convert from a higher to a lower indexed pixel format! Use ConvertTo8Bit / ConvertFrom8Bit instead!", "desiredPixelFormat");
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, desiredPixelFormat);
            stride = sourceData.Stride;
            Byte[] data = new Byte[stride * height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            if (collapseStride)
                data = CollapseStride(data, width, height, Image.GetPixelFormatSize(desiredPixelFormat), ref stride, true);
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
            Int64 sourcePos = sourceData.Scan0.ToInt64();
            Int64 destPos = targetData.Scan0.ToInt64();
            // Copy line by line, skipping by stride but copying actual data width
            for (Int32 y = 0; y < h; y++)
            {
                Marshal.Copy(new IntPtr(sourcePos), imageData, 0, actualDataWidth);
                Marshal.Copy(imageData, 0, new IntPtr(destPos), actualDataWidth);
                sourcePos += origStride;
                destPos += targetStride;
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
        /// <param name="stride">Scanline length inside the data. If this is negative, the image is built from the bottom up (BMP format).</param>
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

        private static Color[] GeneratePalette(Color[] colors, Color def)
        {
            Color[] pal = new Color[0x100];
            Int32 minSize = Math.Min(colors.Length, 0x100);
            Array.Copy(colors, 0, pal, 0, minSize);
            for (Int32 i = minSize; i < 0x100; i++)
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

        /// <summary>
        /// Generates a grid image to put over a pixel-zoomed image. Created for the font editor.
        /// </summary>
        /// <param name="origWidth">Original width of the shown image</param>
        /// <param name="origHeight">Original height of the shown image</param>
        /// <param name="zoomFactor">Zoom factor of the shown image.</param>
        /// <param name="colors">Color palette</param>
        /// <param name="bgColor">Background color of the grid. Usually just 0.</param>
        /// <param name="gridcolor">Main color of the grid.</param>
        /// <param name="outLineColor">Outline color of the grid.</param>
        /// <returns>The grid image to overlay on the displayded image.</returns>
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
        /// <param name="imageData">Byte data of the image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="stride">Stride of the image.</param>
        /// <param name="copyArea">The area to copy.</param>
        /// <returns></returns>
        public static Byte[] CopyFrom8bpp(Byte[] imageData, Int32 width, Int32 height, Int32 stride, Rectangle copyArea)
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
                    copiedPicture[indexDest] = imageData[indexSource];
                }
            }
            return copiedPicture;
        }

        /// <summary>
        /// Pastes 8-bit data on an 8-bit image.
        /// </summary>
        /// <param name="destData">Byte data of the image that is pasted on.</param>
        /// <param name="destWidth">Width of the image that is pasted on.</param>
        /// <param name="destHeight">Height of the image that is pasted on.</param>
        /// <param name="destStride">Stride of the image that is pasted on.</param>
        /// <param name="pasteData">Byte data of the image to paste.</param>
        /// <param name="pasteWidth">Width of the image to paste.</param>
        /// <param name="pasteHeight">Height of the image to paste.</param>
        /// <param name="pasteStride">Stride of the image to paste.</param>
        /// <param name="targetPos">Position at which to paste the image.</param>
        /// <param name="transparencyGuide">Colour palette of the images, to determine which colours should be treated as transparent. Use null for no transparency.</param>
        /// <param name="modifyOrig">True to modify the original array rather than returning a copy.</param>
        /// <returns>A new Byte array with the combined data, and the same stride as the source image.</returns>
        public static Byte[] PasteOn8bpp(Byte[] destData, Int32 destWidth, Int32 destHeight, Int32 destStride,
            Byte[] pasteData, Int32 pasteWidth, Int32 pasteHeight, Int32 pasteStride,
            Rectangle targetPos, Boolean[] transparencyGuide, Boolean modifyOrig)
        {
            return PasteOn8bpp(destData, destWidth, destHeight, destStride, pasteData, pasteWidth, pasteHeight, pasteStride, targetPos, transparencyGuide, modifyOrig, null);
        }

        /// <summary>
        /// Pastes 8-bit data on an 8-bit image.
        /// </summary>
        /// <param name="destData">Byte data of the image that is pasted on.</param>
        /// <param name="destWidth">Width of the image that is pasted on.</param>
        /// <param name="destHeight">Height of the image that is pasted on.</param>
        /// <param name="destStride">Stride of the image that is pasted on.</param>
        /// <param name="pasteData">Byte data of the image to paste.</param>
        /// <param name="pasteWidth">Width of the image to paste.</param>
        /// <param name="pasteHeight">Height of the image to paste.</param>
        /// <param name="pasteStride">Stride of the image to paste.</param>
        /// <param name="targetPos">Position at which to paste the image.</param>
        /// <param name="transparencyGuide">Colour palette of the images, to determine which colours should be treated as transparent. Use null for no transparency.</param>
        /// <param name="modifyOrig">True to modify the original array rather than returning a copy.</param>
        /// <param name="transparencyMask">For transparency masking; true is transparent. If given, should have a size of transparencyMaskStride * pasteHeight.</param>
        /// <returns>A new Byte array with the combined data, and the same stride as the source image.</returns>
        public static Byte[] PasteOn8bpp(Byte[] destData, Int32 destWidth, Int32 destHeight, Int32 destStride,
            Byte[] pasteData, Int32 pasteWidth, Int32 pasteHeight, Int32 pasteStride,
            Rectangle targetPos, Boolean[] transparencyGuide, Boolean modifyOrig, Boolean[] transparencyMask)
        {
            if (targetPos.Width != pasteWidth || targetPos.Height != pasteHeight)
                pasteData = CopyFrom8bpp(pasteData, pasteWidth, pasteHeight, pasteStride, new Rectangle(0, 0, targetPos.Width, targetPos.Height));
            Byte[] finalFileData;
            if (modifyOrig)
            {
                finalFileData = destData;
            }
            else
            {
                finalFileData = new Byte[destData.Length];
                Array.Copy(destData, finalFileData, destData.Length);
            }
            Boolean[] isTransparent = new Boolean[256];
            if (transparencyGuide != null)
            {
                Int32 len = Math.Min(isTransparent.Length, transparencyGuide.Length);
                for (Int32 i = 0; i < len; i++)
                    isTransparent[i] = transparencyGuide[i];
            }
            Boolean transMaskGiven = transparencyMask != null && transparencyMask.Length == pasteWidth * pasteHeight;
            Int32 maxY = Math.Min(destHeight - targetPos.Y, targetPos.Height);
            Int32 maxX = Math.Min(destWidth - targetPos.X, targetPos.Width);
            for (Int32 y = 0; y < maxY; y++)
            {
                for (Int32 x = 0; x < maxX; x++)
                {
                    Int32 indexSource = y * pasteStride + x;
                    Int32 indexTrans = transMaskGiven ? y * pasteWidth + x : 0;
                    Byte data = pasteData[indexSource];
                    if (isTransparent[data] || (transMaskGiven && transparencyMask[indexTrans]))
                        continue;
                    Int32 indexDest = (targetPos.Y + y) * destStride + targetPos.X + x;
                    // This will always get a new index
                    finalFileData[indexDest] = data;
                }
            }
            return finalFileData;
        }

        /// <summary>
        /// Collapse stride to the minimum required, for any image type. Note that if the current stride is already the
        /// minimum, the data will still be copied to a new array, so the input array is never referenced as result.
        /// If you want to avoid this, use the overload with the "unsafe" parameter.
        /// </summary>
        /// <param name="data">Image data</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="bitsLength">Bits per pixel</param>
        /// <param name="stride">Stride of the image</param>
        /// <returns>The data, collapsed to the minimum stride.</returns>
        public static Byte[] CollapseStride(Byte[] data, Int32 width, Int32 height, Int32 bitsLength, ref Int32 stride)
        {
            return CollapseStride(data, width, height, bitsLength, ref stride, false);
        }

        /// <summary>
        /// Collapse stride to the minimum required, for any image type.
        /// </summary>
        /// <param name="data">Image data</param>
        /// <param name="width">Width of the image</param>
        /// <param name="height">Height of the image</param>
        /// <param name="bitsLength">Bits per pixel</param>
        /// <param name="stride">Stride of the image</param>
        /// <param name="unsafe">If true, and the minimum stride equals the given <paramref name="stride"/>, simply return the original reference to the <paramref name="data"/> array without making a copy.</param>
        /// <returns>The data, collapsed to the minimum stride.</returns>
        public static Byte[] CollapseStride(Byte[] data, Int32 width, Int32 height, Int32 bitsLength, ref Int32 stride, Boolean @unsafe)
        {
            Int32 newStride = GetMinimumStride(width, bitsLength);
            Byte[] newData = new Byte[newStride * height];
            if (newStride == stride)
            {
                if (@unsafe)
                    return data;
                Array.Copy(data, newData, data.Length);
                return newData;
            }
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
        /// The new stride at the end of the operation will always equal the width.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="bitsLength">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value, which will always equal the width.</param>
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
            // Actual conversion process.
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
                    data8bit[index8bit] = (Byte) ((fileData[indexXbit] >> shift) & bitmask);
                }
            }
            stride = newStride;
            return data8bit;
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
            // Actual conversion process.
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
                    dataXbit[indexXbit] |= (Byte) ((data8bit[index8bit] & bitmask) << shift);
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

        public static Bitmap[] GetFramesFromAnimatedGif(Image image)
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
        /// <returns>The trimmed image, if adjustBuffer is true. Otherwise, the original reference to the <paramref name="buffer"/> array.</returns>
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

        /// <summary>
        /// Detects the bounds to crop an image. This treats the image as 32bppARGB.
        /// </summary>
        /// <param name="image">ISource image.</param>
        /// <param name="blankPixel">Blank color to crop off.</param>
        /// <param name="borderSizePixels">Leave border of this many pixels around cropped area.</param>
        /// <param name="searchArea">Original area of the image to perform the algorithm on.</param>
        /// <returns></returns>
        public static Rectangle GetCropBounds(Bitmap image, Color blankPixel, Int32 borderSizePixels, Rectangle? searchArea)
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

        /// <summary>
        /// Reorders the bits inside a byte array to a new pixel format of equal length. Both formats are specified by a PixelFormatter object.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="stride">Image data stride.</param>
        /// <param name="inputFormat">Input pixel formatter.</param>
        /// <param name="outputFormat">Output pixel formatter.</param>
        public static void ReorderBits(Byte[] imageData, Int32 width, Int32 height, Int32 stride, PixelFormatter inputFormat, PixelFormatter outputFormat)
        {
            if (!inputFormat.BitsAmounts.SequenceEqual(outputFormat.BitsAmounts))
                throw new ArgumentException("Output format's bytes per pixel do not match input format!", "outputFormat");
            // This code relies on the fact that both formats have the same amount of bits per pixel,
            // meaning they can be written back to the same space.
            if (inputFormat.BitsAmounts.SequenceEqual(outputFormat.BitsAmounts))
            {
                // Actually has same bit amounts : simply reorder the raw data.
                for (Int32 y = 0; y < height; y++)
                {
                    Int32 offset = y * stride;
                    for (Int32 x = 0; x < width; x++)
                    {
                        UInt32[] argbValues = inputFormat.GetRawComponents(imageData, offset);
                        outputFormat.WriteRawComponents(imageData, offset, argbValues);
                        offset += 2;
                    }
                }

            }
            else
            {
                // Bits differ: convert through Color.
                for (Int32 y = 0; y < height; y++)
                {
                    Int32 offset = y * stride;
                    for (Int32 x = 0; x < width; x++)
                    {
                        Color col = inputFormat.GetColor(imageData, offset);
                        outputFormat.WriteColor(imageData, offset, col);
                        offset += 2;
                    }
                }
            }
        }
        
        public static Bitmap[] CutImageIntoFrames(Bitmap image, Int32 frameWidth, Int32 frameHeight, Int32 framesLimit)
        {
            PixelFormat pf = image.PixelFormat;
            Int32 bpp = Image.GetPixelFormatSize(pf);
            ColorPalette imPal = image.Palette;
            Color[] imPalette = bpp > 8 ? null : imPal.Entries;
            Int32 colorsInPal = imPalette == null ? 0 : imPalette.Length;
            Boolean incompletePalette = bpp <= 8 && colorsInPal < (1 << bpp);
            Int32 multiplier = bpp < 8 ? 1 : bpp / 8;

            Int32 fullWidth = image.Width;
            Int32 fullHeight = image.Height;
            Int32 framesX = fullWidth / frameWidth;
            Int32 framesY = fullHeight / frameHeight;
            Int32 nrOfFrames = Math.Min(framesLimit, framesX * framesY);
            Byte[] imageData = null;
            Int32 stride = fullWidth;
            Boolean indexed = bpp <= 8;
            imageData = ImageUtils.GetImageData(image, out stride);
            if (bpp < 8)
                imageData = ImageUtils.ConvertTo8Bit(imageData, fullWidth, fullHeight, 0, bpp, true, ref stride);

            Bitmap[] frames = new Bitmap[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; i++)
            {
                Int32 rectY = i / framesX;
                Int32 rectX = i % framesX;
                Rectangle section = new Rectangle(rectX * frameWidth * multiplier, rectY * frameHeight, frameWidth * multiplier, frameHeight);
                Bitmap frameImage;
                Byte[] frameData = ImageUtils.CopyFrom8bpp(imageData, fullWidth * multiplier, fullHeight, stride, section);
                Int32 frameStride = frameWidth * multiplier;
                if (bpp < 8)
                    frameData = ImageUtils.ConvertFrom8Bit(frameData, frameWidth, frameHeight, bpp, true, ref frameStride);
                frameImage = ImageUtils.BuildImage(frameData, frameWidth, frameHeight, frameStride, pf, imPalette, null);
                if (incompletePalette)
                    frameImage.Palette = imPal;
                frames[i] = frameImage;
            }
            return frames;
        }


        public static Bitmap BuildImageFromFrames(Bitmap[] images, Int32 framesWidth, Int32 framesHeight, Int32 framesPerLine)
        {
            PixelFormat highestPf = PixelFormat.Undefined;
            Int32 highestBpp = 0;
            Color[] palette = null;
            ColorPalette paletteRaw = null;
            foreach (Bitmap img in images)
            {
                if (img == null)
                    continue;
                framesWidth = Math.Max(img.Width, framesWidth);
                framesHeight = Math.Max(img.Height, framesHeight);
                PixelFormat curPf = img.PixelFormat;
                Int32 curBpp = Image.GetPixelFormatSize(curPf);
                if (curBpp > highestBpp)
                {
                    highestPf = curPf;
                    highestBpp = curBpp;
                }
                if (curBpp <= 8 && palette == null)
                {
                    paletteRaw = img.Palette;
                    palette = paletteRaw.Entries;
                }
            }
            Boolean useRaw = highestBpp <= 8 && palette.Length < (1 << highestBpp);
            Int32 frames = images.Length;
            Int32 lines = (frames + framesPerLine - 1) / framesPerLine;
            Int32 fullWidth = framesWidth * framesPerLine;
            Int32 fullHeight = framesHeight * lines;
            Bitmap bp;
            if (highestBpp > 8)
            {
                bp = new Bitmap(fullWidth, fullHeight, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bp))
                {
                    for (Int32 i = 0; i < frames; i++)
                    {
                        Bitmap cur = images[i];
                        if (cur == null)
                            continue;
                        Int32 rectY = i / framesPerLine;
                        Int32 rectX = i % framesPerLine;
                        Rectangle section = new Rectangle(rectX * framesWidth, rectY * framesHeight, cur.Width, cur.Height);
                        using (Bitmap tempImg = new Bitmap(cur))
                            g.DrawImage(tempImg, section);
                    }
                }
                if (highestBpp < 32 || highestPf != PixelFormat.Format32bppArgb)
                {
                    Int32 stride;
                    Byte[] imageData = ImageUtils.GetImageData(bp, out stride, highestPf);
                    bp.Dispose();
                    bp = BuildImage(imageData, fullWidth, fullHeight, stride, highestPf, null, null);
                }
            }
            else
            {
                Byte[] bpData = new Byte[fullWidth * fullHeight];
                for (Int32 i = 0; i < frames; i++)
                {
                    Bitmap cur = images[i];
                    if (cur == null)
                        continue;
                    Int32 curBpp = Image.GetPixelFormatSize(cur.PixelFormat);
                    Int32 rectY = i / framesPerLine;
                    Int32 rectX = i % framesPerLine;
                    Int32 frWidth = cur.Width;
                    Int32 frHeight = cur.Height;
                    Rectangle section = new Rectangle(rectX * framesWidth, rectY * framesHeight, frWidth, frHeight);
                    Int32 frStride;
                    Byte[] frameData = GetImageData(cur, out frStride);
                    if (curBpp < 8)
                        frameData = ConvertTo8Bit(frameData, frWidth, frHeight, 0, curBpp, true, ref frStride);
                    PasteOn8bpp(bpData, fullWidth, fullHeight, fullWidth, frameData, frWidth, frHeight, frStride, section, null, true);
                }
                Int32 stride = fullWidth;

                if (highestBpp < 8)
                    bpData = ConvertFrom8Bit(bpData, fullWidth, fullHeight, highestBpp, true, ref stride);
                bp = BuildImage(bpData, fullWidth, fullHeight, stride, highestPf, palette, null);
                if (useRaw)
                    bp.Palette = paletteRaw;
            }
            return bp;
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
                Int32 padding = Math.Abs(width - height) / 2;
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
    }
}
