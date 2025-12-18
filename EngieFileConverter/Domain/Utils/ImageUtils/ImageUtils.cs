using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;

namespace Nyerguds.ImageManipulation
{
    public static class ImageUtils
    {

        /// <summary>
        /// Loads an image without locking the underlying file.
        /// </summary>
        /// <param name="path">Path of the image to load.</param>
        /// <returns>The image.</returns>
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
            return GetSavedImageData(image, saveFormat);
        }

        public static Byte[] GetSavedImageData(Bitmap image, ImageFormat saveFormat)
        {
            using (image = CloneImage(image))
            using (MemoryStream ms = new MemoryStream())
            {
                if (saveFormat.Equals(ImageFormat.Jpeg))
                {
                    // What a mess just to have non-crappy jpeg. Scratch that; jpeg is always crappy.
                    ImageCodecInfo jpegEncoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                    EncoderParameters encparams = new EncoderParameters(1);
                    encparams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                    image.Save(ms, jpegEncoder, encparams);
                }
                else if (saveFormat.Equals(ImageFormat.Gif) && image.PixelFormat == PixelFormat.Format4bppIndexed)
                {
                    // 4-bit gif images don't get converted right; they get dumped on the standard windows 256 colour palette. So we convert it manually before the save.
                    Int32 stride;
                    Byte[] fourBitData = GetImageData(image, out stride);
                    Byte[] eightBitData = ConvertTo8Bit(fourBitData, image.Width, image.Height, 0, 4, true, ref stride);
                    using (Bitmap img2 = BuildImage(eightBitData, image.Width, image.Height, stride, PixelFormat.Format8bppIndexed, image.Palette.Entries, Color.Black))
                        img2.Save(ms, saveFormat);
                }
                else if (saveFormat.Equals(ImageFormat.Png))
                    GetPngImageData(image, 0, false);
                else
                    image.Save(ms, saveFormat);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets the minimum stride required for containing an image of the given width and bits per pixel.
        /// </summary>
        /// <param name="width">Image width.</param>
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

        public static Bitmap PaintOn32bppTogether(Image image1, Image image2, Color? transparencyColor)
        {
            Bitmap bp = new Bitmap(image1.Width, image1.Height, PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bp))
            {
                if (transparencyColor.HasValue)
                    using (SolidBrush myBrush = new SolidBrush(transparencyColor.Value))
                        gr.FillRectangle(myBrush, new Rectangle(0, 0, image1.Width, image1.Height));
                gr.DrawImage(image1, new Rectangle(0, 0, image1.Width, image1.Height));
                gr.DrawImage(image2, new Rectangle(0, 0, image2.Width, image2.Height));
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
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 inputOffs = inputOffsetLine;
                Int32 outputOffs = outputOffsetLine;
                for (Int32 x = 0; x < width; ++x)
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
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset1 = index1;
                Int32 offset2 = index2;

                for (Int32 x = 0; x < width; ++x)
                {
                    for (Int32 n = 0; n > byteSize; ++n)
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
        /// <param name="sourcePalette">Palette of the source data.</param>
        /// <param name="targetPalette">Target palette to convert to.</param>
        /// <returns>The converted image data.</returns>
        public static Byte[] Match8BitDataToPalette(Byte[] imageData, Color[] sourcePalette, Color[] targetPalette)
        {
            Int32 imageDataLength = imageData.Length;
            Byte[] newImageData = new Byte[imageDataLength];
            for (Int32 i = 0; i < imageDataLength; ++i)
            {
                Int32 currentVal = imageData[i];
                Color c = currentVal < sourcePalette.Length ? sourcePalette[currentVal] : Color.Black;
                newImageData[i] = (Byte) ColorUtils.GetClosestPaletteIndexMatch(c, targetPalette, null);
            }
            return newImageData;
        }

        /// <summary>
        /// Converts an image to paletted format.
        /// </summary>
        /// <param name="originalImage">Original image.</param>
        /// <param name="bpp">Desired bits per pixel for the paletted image (should be less than or equal to 8).</param>
        /// <param name="palette">The color palette.</param>
        /// <returns>A bitmap of the desired color depth matched to the given palette.</returns>
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
            Byte[] palettedData = Convert32BitToPaletted(imageData, originalImage.Width, originalImage.Height, bpp, true, palette, ref stride);
            return BuildImage(palettedData, originalImage.Width, originalImage.Height, stride, pf, palette, Color.Black);
        }

        /// <summary>
        /// Converts 32 bit per pixel image data to match a given color palette, and returns it as array in the desired pixel format.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="bpp">Bits per pixel.</param>
        /// <param name="bigEndianBits">True to use big endian ordered data in the indexed array if <paramref name="bpp "/> is less than 8.</param>
        /// <param name="palette">Color palette to match to.</param>
        /// <param name="stride">Stride. Will be adjusted by the function.</param>
        /// <returns>The converted indexed data.</returns>
        public static Byte[] Convert32BitToPaletted(Byte[] imageData, Int32 width, Int32 height, Int32 bpp, Boolean bigEndianBits, Color[] palette, ref Int32 stride)
        {
            if (stride < width * 4)
                throw new ArgumentException("Stride is smaller than one pixel line!", "stride");
            Byte[] newImageData = new Byte[width * height];
            List<Int32> transparentIndices = new List<Int32>();
            Int32 maxLen = Math.Min(0x100, palette.Length);
            for (Int32 i = 0; i < maxLen; ++i)
                if (palette[i].A == 0)
                    transparentIndices.Add(i);
            Int32 firstTransIndex = transparentIndices.Count > 0 ? transparentIndices[0] : -1;
            // Mapping table. Takes more memory, but it's way faster, especially on sprites with clear backgrounds.
            Dictionary<UInt32, Byte> colorMapping = new Dictionary<UInt32, Byte>();
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 inputOffs = y * stride;
                Int32 outputOffs = y * width;
                for (Int32 x = 0; x < width; ++x)
                {
                    Color c = Color.FromArgb(imageData[inputOffs + 3], imageData[inputOffs + 2], imageData[inputOffs + 1], imageData[inputOffs]);
                    UInt32 colKey = (UInt32)c.ToArgb();
                    Byte outInd;
                    if (colorMapping.ContainsKey(colKey))
                    {
                        outInd = colorMapping[colKey];
                    }
                    else
                    {
                        if (firstTransIndex >= 0 && c.A < 128)
                            outInd = (Byte)firstTransIndex;
                        else
                            outInd = (Byte)ColorUtils.GetClosestPaletteIndexMatch(c, palette, transparentIndices);
                        colorMapping.Add(colKey, outInd);
                    }
                    newImageData[outputOffs] = outInd;
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
        /// <returns>The raw bytes of the image.</returns>
        public static Byte[] GetImageData(Bitmap sourceImage)
        {
            Int32 stride;
            return GetImageData(sourceImage, out stride, sourceImage.PixelFormat, true);
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
        /// Gets the raw bytes from an image in its original pixel format. This automatically
        /// collapses the stride, and returns the data using the minimum stride.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="desiredPixelFormat">PixelFormat in which the data needs to be retrieved. Use <paramref name="sourceImage"/>.PixelFormat for no conversion.</param>
        /// <returns>The raw bytes of the image.</returns>
        public static Byte[] GetImageData(Bitmap sourceImage, PixelFormat desiredPixelFormat)
        {
            Int32 stride;
            return GetImageData(sourceImage, out stride, desiredPixelFormat, true);
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
        ///   and if you give an indexed pixel format and the source is non-indexed, the colors will be matched to the standard Windows palette for that format.
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
        ///   and if you give an indexed pixel format and the source is non-indexed, the colors will be matched to the standard Windows palette for that format.
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
            for (Int32 y = 0; y < h; ++y)
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
        /// <param name="sourceData">Byte array of raw source data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="stride">Scanline length inside the data.</param>
        /// <param name="pixelFormat">Pixel format.</param>
        /// <param name="palette">Color palette.</param>
        /// <param name="defaultColor">Default color to fill in on the palette if the given colors don't fully fill it.</param>
        /// <returns>The new image.</returns>
        public static Bitmap BuildImage(Byte[] sourceData, Int32 width, Int32 height, Int32 stride, PixelFormat pixelFormat, Color[] palette, Color? defaultColor)
        {
            Bitmap newImage = new Bitmap(width, height, pixelFormat);
            BitmapData targetData = newImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, newImage.PixelFormat);
            Int32 newDataWidth = ((Image.GetPixelFormatSize(pixelFormat) * width) + 7) / 8;
            Int32 targetStride = targetData.Stride;
            Int64 scan0 = targetData.Scan0.ToInt64();
            for (Int32 y = 0; y < height; ++y)
                Marshal.Copy(sourceData, y * stride, new IntPtr(scan0 + y * targetStride), newDataWidth);
            newImage.UnlockBits(targetData);
            // For indexed images, set the palette.
            if ((pixelFormat & PixelFormat.Indexed) != 0 && (palette != null || defaultColor.HasValue))
            {
                if (palette == null)
                    palette = new Color[0];
                ColorPalette pal = newImage.Palette;
                Int32 palLen = pal.Entries.Length;
                Int32 paletteLength = palette.Length;
                for (Int32 i = 0; i < palLen; ++i)
                {
                    if (i < paletteLength)
                        pal.Entries[i] = palette[i];
                    else if (defaultColor.HasValue)
                        pal.Entries[i] = defaultColor.Value;
                    else
                        break;
                }
                // Palette property getter creates a copy, so the newly filled in palette
                // is not actually referenced in the image until you set it again explicitly.
                newImage.Palette = pal;
            }
            return newImage;
        }
        
        /// <summary>
        /// Checks if a given image contains transparency.
        /// </summary>
        /// <param name="bitmap">Input bitmap</param>
        /// <returns>True if pixels were found with an alpha value of less than 255.</returns>
		public static Boolean HasTransparency(Bitmap bitmap)
		{
		    // Not an alpha-capable color format. Note that GDI+ indexed images are alpha-capable on the palette.
		    if (((ImageFlags)bitmap.Flags & ImageFlags.HasAlpha) == 0)
		        return false;
		    // Indexed format, and no alpha colors in the images palette: immediate pass.
		    if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.A == 255))
		        return false;
		    // Get the byte data 'as 32-bit ARGB'. This offers a converted version of the image data without modifying the original image.
		    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		    Int32 len = bitmap.Height * data.Stride;
		    Byte[] bytes = new Byte[len];
		    Marshal.Copy(data.Scan0, bytes, 0, len);
		    bitmap.UnlockBits(data);
		    // Check the alpha bytes in the data. Since the data is little-endian, the actual byte order is [BB GG RR AA]
		    for (Int32 i = 3; i < len; i += 4)
		        if (bytes[i] != 255)
		            return true;
		    return false;
		}

        private static Color[] GeneratePalette(Color[] colors, Color def)
        {
            Color[] pal = new Color[0x100];
            Int32 minSize = Math.Min(colors.Length, 0x100);
            Array.Copy(colors, 0, pal, 0, minSize);
            for (Int32 i = minSize; i < 0x100; ++i)
                pal[i] = def;
            return pal;
        }

        public static Bitmap GenerateBlankImage(Int32 width, Int32 height, Color[] colors, Byte paintColor)
        {
            if (width == 0 || height == 0)
                return null;
            Color[] pal = GeneratePalette(colors, Color.Empty);
            Int32 blankLen = width * height;
            Byte[] blankArray = new Byte[blankLen];
            if (paintColor != 0)
                for (Int32 i = 0; i < blankLen; ++i)
                    blankArray[i] = paintColor;
            return BuildImage(blankArray, width, height, width, PixelFormat.Format8bppIndexed, pal, Color.Empty);
        }

        public static Bitmap GenerateCheckerboardImage(Int32 width, Int32 height, Color[] colors, Byte color1, Byte color2)
        {
            if (width == 0 || height == 0)
                return null;
            Byte[] patternArray = new Byte[width * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = width * y;
                for (Int32 x = 0; x < width; ++x)
                {
                    patternArray[offset] = (Byte)(((x + y) % 2 == 0) ? 1 : 0);
                    offset++;
                }
            }
            return BuildImage(patternArray, width, height, width, PixelFormat.Format8bppIndexed, colors, Color.Black);
        }

        /// <summary>
        /// Generates a grid image to put over a pixel-zoomed image. Created for the font editor.
        /// </summary>
        /// <param name="origWidth">Original width of the shown image.</param>
        /// <param name="origHeight">Original height of the shown image.</param>
        /// <param name="zoomFactor">Zoom factor of the shown image.</param>
        /// <param name="colors">Color palette.</param>
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
                for (Int32 i = 0; i < patternArray.Length; ++i)
                    patternArray[i] = bgColor;
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; ++x)
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
        /// <param name="source">The source image.</param>
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
            for (Int32 y = 0; y < height; ++y)
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
            for (Int32 y = 0; y < height; ++y)
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
            for (Int32 y = startY; y <= endY; ++y)
            {
                if (fill)
                {
                    for (Int32 x = startX; x <= endX; ++x)
                        dataArray[x + y * stride] = colorIndex;
                }
                else
                {
                    if (y == startY || y == endY)
                        for (Int32 x = startX; x <= endX; ++x)
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
            Byte[] newSource = ArrayUtils.CloneArray(source);
            Byte[] emptyRow = new Byte[stride];
            if (backColor != 0 && !wrap)
                for (Int32 i = 0; i < stride; ++i)
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
            Byte[] newSource = ArrayUtils.CloneArray(source);
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
        /// <returns>The requested piece of the image.</returns>
        public static Byte[] CopyFrom8bpp(Byte[] imageData, Int32 width, Int32 height, Int32 stride, Rectangle copyArea)
        {
            Byte[] copiedPicture = new Byte[copyArea.Width * copyArea.Height];
            Int32 maxY = Math.Min(height - copyArea.Y, copyArea.Height);
            Int32 maxX = Math.Min(width - copyArea.X, copyArea.Width);

            for (Int32 y = 0; y < maxY; ++y)
            {
                for (Int32 x = 0; x < maxX; ++x)
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
        /// <param name="palTransparencyMask">Boolean array determining which offsets on the color palette will be treated as transparent. Use null for no transparency.</param>
        /// <param name="modifyOrig">True to modify the original array rather than returning a copy.</param>
        /// <returns>A new Byte array with the combined data, and the same stride as the source image.</returns>
        public static Byte[] PasteOn8bpp(Byte[] destData, Int32 destWidth, Int32 destHeight, Int32 destStride,
            Byte[] pasteData, Int32 pasteWidth, Int32 pasteHeight, Int32 pasteStride,
            Rectangle targetPos, Boolean[] palTransparencyMask, Boolean modifyOrig)
        {
            return PasteOn8bpp(destData, destWidth, destHeight, destStride, pasteData, pasteWidth, pasteHeight, pasteStride, targetPos, palTransparencyMask, modifyOrig, null);
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
        /// <param name="palTransparencyMask">Boolean array determining which offsets on the color palette will be treated as transparent. Use null for no transparency.</param>
        /// <param name="modifyOrig">True to modify the original array rather than returning a copy.</param>
        /// <param name="transparencyMask">For image-based transparency masking rather than palette based. Values in the array set to true are treated as transparent.
        /// If given, should have a size of exactly <see cref="pasteWidth"/> * <see cref="pasteHeight"/>.</param>
        /// <returns>A new Byte array with the combined data, and the same stride as the source image.</returns>
        public static Byte[] PasteOn8bpp(Byte[] destData, Int32 destWidth, Int32 destHeight, Int32 destStride,
            Byte[] pasteData, Int32 pasteWidth, Int32 pasteHeight, Int32 pasteStride,
            Rectangle targetPos, Boolean[] palTransparencyMask, Boolean modifyOrig, Boolean[] transparencyMask)
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
            if (palTransparencyMask != null)
            {
                Int32 len = Math.Min(isTransparent.Length, palTransparencyMask.Length);
                for (Int32 i = 0; i < len; ++i)
                    isTransparent[i] = palTransparencyMask[i];
            }
            Boolean transMaskGiven = transparencyMask != null && transparencyMask.Length == pasteWidth * pasteHeight;
            Int32 maxY = Math.Min(destHeight - targetPos.Y, targetPos.Height);
            Int32 maxX = Math.Min(destWidth - targetPos.X, targetPos.Width);
            for (Int32 y = 0; y < maxY; ++y)
            {
                for (Int32 x = 0; x < maxX; ++x)
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
        /// <param name="data">Image data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="bitsLength">Bits per pixel.</param>
        /// <param name="stride">Stride of the image.</param>
        /// <returns>The data, collapsed to the minimum stride.</returns>
        public static Byte[] CollapseStride(Byte[] data, Int32 width, Int32 height, Int32 bitsLength, ref Int32 stride)
        {
            return CollapseStride(data, width, height, bitsLength, ref stride, false);
        }

        /// <summary>
        /// Collapse stride to the minimum required, for any image type.
        /// </summary>
        /// <param name="data">Image data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="bitsLength">Bits per pixel.</param>
        /// <param name="stride">Stride of the image.</param>
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
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 oldOffs = stride * y;
                Int32 offs = newStride * y;
                for (Int32 s = 0; s < newStride; ++s)
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
        /// <param name="imageData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="bitsLength">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        public static Byte[] ConvertTo8Bit(Byte[] imageData, Int32 width, Int32 height, Int32 start, Int32 bitsLength, Boolean bigEndian)
        {
            Int32 stride = GetMinimumStride(width, bitsLength);
            return ConvertTo8Bit(imageData, width, height, start, bitsLength, bigEndian, ref stride);
        }
        
        /// <summary>
        /// Converts given raw image data for a paletted image to 8-bit, so we have a simple one-byte-per-pixel format to work with.
        /// The new stride at the end of the operation will always equal the width.
        /// </summary>
        /// <param name="imageData">The image data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="bitsLength">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value, which will always equal the width.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        public static Byte[] ConvertTo8Bit(Byte[] imageData, Int32 width, Int32 height, Int32 start, Int32 bitsLength, Boolean bigEndian, ref Int32 stride)
        {
            if (bitsLength != 1 && bitsLength != 2 && bitsLength != 4 && bitsLength != 8)
                throw new ArgumentOutOfRangeException("Cannot handle image data with " + bitsLength + "bits per pixel.");
            // Full array
            Byte[] data8bit = new Byte[width * height];
            // Special case: data is already compact 8-bit. Simply clone it.
            if (bitsLength == 8 && width == stride && imageData.Length == data8bit.Length)
                return ArrayUtils.CloneArray(imageData);
            // Amount of pixels that are stored on the same byte
            Int32 parts = 8 / bitsLength;
            // Amount of bytes to write per line
            Int32 newStride = width;
            // Bit mask for reducing read and shifted data to actual bits length
            Int32 bitmask = (1 << bitsLength) - 1;
            Int32 size = stride * height;
            // File check, and getting actual data.
            if (start + size > imageData.Length)
                throw new IndexOutOfRangeException("Data exceeds array bounds!");
            // Actual conversion process.
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; ++x)
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
                    data8bit[index8bit] = (Byte) ((imageData[indexXbit] >> shift) & bitmask);
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
        /// <param name="data8bit">The eight bit per pixel image data.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="bitsLength">The new amount of bits per pixel.</param>
        /// <param name="bigEndian">True if the bits in the new image data are to be stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value.</param>
        /// <returns>The image data converted to the requested amount of bits per pixel.</returns>
        public static Byte[] ConvertFrom8Bit(Byte[] data8bit, Int32 width, Int32 height, Int32 bitsLength, Boolean bigEndian, ref Int32 stride)
        {
            Int32 parts = 8 / bitsLength;
            // Amount of bytes to write per line
            Int32 newStride = GetMinimumStride(width, bitsLength);
            // Bit mask for reducing original data to actual bits maximum.
            // Should not be needed if data is correct, but eh.
            Int32 bitmask = (1 << bitsLength) - 1;
            Byte[] dataXbit = new Byte[newStride * height];
            // Actual conversion process.
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; ++x)
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

        /// <summary>
        /// Converts indexed data from line-based 1-bit planar to chunky mode.
        /// </summary>
        /// <param name="dataPlanar">The data in planar format</param>
        /// <param name="start">Start offset of the image data in the dataPlanar parameter.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="planes">The number of planes to combine per pixel.</param>
        /// <param name="planeStride">Stride of one plane line.</param>
        /// <param name="planeBitRate">Bits per entry in the planes.</param>
        /// <param name="outputBpp">Bits per pixel for the output data. Should typically be at least as high as the amount of planes.</param>
        /// <param name="stride">Stride of the resulting data.</param>
        /// <returns></returns>
        public static Byte[] PlanarLinesToLinear(Byte[] dataPlanar, Int32 start, Int32 width, Int32 height, Int32 planes, Int32 planeStride, Int32 planeBitRate, Int32 outputBpp, out Int32 stride)
        {
            // Single line length for horizontally-composed image is
            // [numPlanes] "bit frames" with a stride equal to the given byte width.
            Int32 planeLinesStride = planeStride * planes;
            // Final 1-bit image is four times the image width. Convert to 1 byte per bit for editing convenience.
            Byte[] oneBitMulPlanesImage = ConvertTo8Bit(dataPlanar, width * planes, height, start, planeBitRate, true, ref planeLinesStride);
            // Array for 4-bit image where each byte is one pixel. Will be converted to true 4bpp later.
            Byte[] pixelImage = new Byte[width * height];
            // Combine the bits into the new array.
            Int32 lineOffset = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 realOffset = lineOffset;
                Int32 firstPlaneOffset = planeLinesStride * y;
                for (Int32 x = 0; x < width; ++x)
                {
                    Int32 offset = firstPlaneOffset;
                    // Take [X] bits by skipping [width] bytes for each next one.
                    Byte bits = 0;
                    for (Int32 p = 0; p < planes; ++p)
                    {
                        bits |= (Byte) (oneBitMulPlanesImage[offset + x] << p);
                        offset += width;
                    }
                    pixelImage[realOffset] = bits;
                    realOffset++;
                }
                lineOffset += width;
            }
            stride = width;
            if (outputBpp > 0 && outputBpp < 8)
                pixelImage = ConvertFrom8Bit(pixelImage, width, height, outputBpp, true, ref stride);
            return pixelImage;
        }

        /// <summary>
        /// Converts RGB or ARGB data from planar to linear.
        /// </summary>
        /// <param name="dataPlanar">The data in planar format</param>
        /// <param name="start">Start offset of the image data in the dataPlanar parameter.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="hasAlpha">True if the input has four planes instead of three, and should output ARGB data.</param>
        /// <param name="planeStride">Stride of one plane line.</param>
        /// <param name="stride">Stride of the resulting data.</param>
        /// <returns></returns>
        public static Byte[] GetPlanarRgb(Byte[] dataPlanar, Int32 start, Int32 width, Int32 height, Boolean hasAlpha, Int32 planeStride, out Int32 stride)
        {
            Int32 numPlanes = hasAlpha ? 4 : 3;
            Int32 inStride = planeStride * numPlanes;
            Int32 outStride = width * numPlanes;
            Byte[] newImageData = new Byte[outStride * height];
            Int32 offsetIn = start;
            Int32 offsetOutY = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offsetOutX = offsetOutY;
                for (Int32 x = 0; x < width; ++x)
                {
                    Int32 offsetOut = offsetOutX + numPlanes - 1;
                    Int32 planeOffs = 0;
                    for (Int32 p = 0; p < numPlanes; ++p)
                    {
                        newImageData[offsetOut] = dataPlanar[offsetIn + planeOffs + x];
                        planeOffs += planeStride;
                        offsetOut--;
                    }
                    offsetOutX += numPlanes;
                }
                offsetIn += inStride;
                offsetOutY += outStride;
            }
            stride = outStride;
            return newImageData;
        }

        public static Byte[] LinearToOnebitPlanarLines(Byte[] imageData, Int32 start, Int32 width, Int32 height, Int32 bpp, Int32 stride, Int32 outputPlanes, out Int32 planeStride)
        {
            // TODO sort this mess out and make it work for multiple planes.
            // Width has to be a multiple of 8.
            Int32 byteWidth = (width + 7) / 8;
            Int32 alignedWidth = byteWidth * 8;
            planeStride = alignedWidth;
            // Width is multiplied by 4. This forms quadruple-width rows to be filled with the bits from one row.
            Int32 eightBitWidth = alignedWidth * outputPlanes;
            Byte[] eightbitImage = ConvertTo8Bit(imageData, width, height, 0, bpp, true, ref stride);
            if (alignedWidth > width)
                eightbitImage = ChangeStride(eightbitImage, stride, height, alignedWidth, false, 0);
            Byte[] oneBitQuadImage = new Byte[eightBitWidth * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = alignedWidth * y;
                Int32 finalOffset = eightBitWidth * y;
                for (Int32 x = 0; x < alignedWidth; ++x)
                {
                    // Split up and write the 4 bits.
                    for (Int32 i = 0; i < 4; ++i)
                        oneBitQuadImage[finalOffset + width * i + x] = (Byte)((eightbitImage[offset + x] >> i) & 1);
                }
            }
            // Compact to 1bpp image
            return ConvertFrom8Bit(oneBitQuadImage, eightBitWidth, height, 1, true, ref eightBitWidth);
        }

        public static Byte[] Tile8BitData(Byte[][] tiles, Int32 tileWidth, Int32 tileHeight, Int32 tileStride, Int32 nrOftiles, Color[] palette, Int32 tilesX)
        {
            Int32 yDim = nrOftiles / tilesX + (nrOftiles % tilesX == 0 ? 0 : 1);
            // Build image, set in m_LoadedImage
            Int32 fullImageWidth = tilesX * tileWidth;
            Int32 fullImageHeight = yDim * tileHeight;
            Byte[] fullImageData = new Byte[fullImageWidth * fullImageHeight];
            Boolean[] transGuide = PaletteUtils.MakePalTransparencyMask(8, palette);
            for (Int32 y = 0; y < yDim; ++y)
            {
                for (Int32 x = 0; x < tilesX; ++x)
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
            for (Int32 i = 0; i < length; ++i)
            {
                image.SelectActiveFrame(FrameDimension.Time, i);
                using (Bitmap frame = new Bitmap(image))
                    images.Add(CloneImage(frame));
            }
            return images.ToArray();
        }

        /// <summary>Trims empty lines off the bottom of an image buffer.</summary>
        /// <param name="buffer">Image data buffer.</param>
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
        /// <param name="buffer">Image data buffer.</param>
        /// <param name="width">Image width (technically stride).</param>
        /// <param name="height">Image height. Will be adjusted by this function.</param>
        /// <param name="yOffset">Current Y-offset to increase.</param>
        /// <param name="alsoTrimBottom">Trim both top and bottom of the image.</param>
        /// <param name="valueToTrim">Value to trim.</param>
        /// <param name="maxOffset">Maximum value that Y can contain in the file format it'll be saved to. Leave 0 to ignore.</param>
        /// <param name="adjustBuffer">True to actually apply the change to the given buffer. False to only adjust the ref parameters.</param>
        /// <returns>The trimmed image, if adjustBuffer is true. Otherwise, the original reference to the <paramref name="buffer"/> array.</returns>
        public static Byte[] OptimizeYHeight(Byte[] buffer, Int32 width, ref Int32 height, ref Int32 yOffset, Boolean alsoTrimBottom, Int32 valueToTrim, Int32 maxOffset, Boolean adjustBuffer)
        {
            // nothing to optimize.
            if (height == 0)
                return adjustBuffer ? new Byte[0] : buffer;
            // Nothing to process
            if (width == 0)
            {
                height = 0;
                yOffset = 0;
                return adjustBuffer ? new Byte[0] : buffer;
            }
            Int32 trimYMax = maxOffset != 0 ? Math.Min(maxOffset - yOffset, height) : height;
            Int32 trimmedYTop;
            Int32 trimmedYBottom = height;
            Byte[] tempArray = new Byte[width];
            for (trimmedYTop = 0; trimmedYTop < trimYMax; ++trimmedYTop)
            {
                Array.Copy(buffer, width * trimmedYTop, tempArray, 0, width);
                if (!tempArray.All(x => x == valueToTrim))
                    break;
            }
            if (alsoTrimBottom)
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
                // Full height was trimmed; image is empty.
                height = 0;
                yOffset = 0;
                return adjustBuffer ? new Byte[0] : buffer;
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
                for (Int32 y = 0; y < height; ++y)
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
        /// <param name="alsoTrimRight">Trim both left and right side of the image.</param>
        /// <param name="valueToTrim">Value to trim.</param>
        /// <param name="maxOffset">Maximum value that Y can contain in the file format it'll be saved to. Leave 0 to ignore.</param>
        /// <param name="adjustBuffer">True to actually apply the change to the given buffer. False to only adjust the ref parameters.</param>
        /// <returns>The trimmed image, if adjustBuffer is true. Otherwise, the original reference to the <paramref name="buffer"/> array.</returns>
        public static Byte[] OptimizeXWidth(Byte[] buffer, ref Int32 width, Int32 height, ref Int32 xOffset, Boolean alsoTrimRight, Int32 valueToTrim, Int32 maxOffset, Boolean adjustBuffer)
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
            for (Int32 x = 0; x < trimXMax; ++x)
            {
                Boolean empty = true;
                for (Int32 y = 0; y < height; ++y)
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
            if (alsoTrimRight)
            {
                for (Int32 x = width - 1; x >= 0; x--)
                {
                    Boolean empty = true;
                    for (Int32 y = 0; y < height; ++y)
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
        /// <param name="origStride">Original stride.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="targetStride">Target stride.</param>
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
                for (Int32 i = 0; i < targetSize; ++i)
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
                for (Int32 i = stride * origHeight; i < newSize; ++i)
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
        /// Find the most common color in an image.
        /// </summary>
        /// <param name="image">Input image.</param>
        /// <returns>The most common color found in the image. If there are multiple with the same frequency, the first one that was encountered is returned.</returns>
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
            for (Int32 y = 0; y < height; ++y)
            {
                // Reset offset on every line, since stride is not guaranteed to always be width * pixel size.
                Int32 inputOffs = y * stride;
                //Final offset = y * line length in bytes + x * pixel length in bytes.
                //To avoid recalculating that offset each time we just increase it with the pixel size at the end of each x iteration.
                for (Int32 x = 0; x < width; ++x)
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
        /// <param name="image">Source image.</param>
        /// <param name="blankPixel">Blank color to crop off.</param>
        /// <param name="borderSizePixels">Leave border of this many pixels around cropped area.</param>
        /// <param name="searchArea">Original area of the image to perform the algorithm on.</param>
        /// <returns>The minimum bounds the image can have with the blank pixel value trimmed off from all sides.</returns>
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
            Boolean newImage = image.PixelFormat != PixelFormat.Format32bppArgb;
            Bitmap bm = null;
            try
            {
                bm = newImage ? new Bitmap(image) : image;
                BitmapData sourceData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
                stride = sourceData.Stride;
                data = new Byte[stride * bm.Height];
                Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
                bm.UnlockBits(sourceData);
            }
            finally
            {
                if (newImage && bm != null)
                    bm.Dispose();
            }
            // ============= Y =============
            // Top = first found row which contains data
            for (top = yStart; top < yEnd; ++top)
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
            for (left = xStart; left < xEnd; ++left)
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
        /// Cuts an image into frames and returns it as an array of <see cref="Bitmap"/> objects.
        /// </summary>
        /// <param name="image">Source image.</param>
        /// <param name="frameWidth">Width of the cut out frames.</param>
        /// <param name="frameHeight">Height of the cut out frames.</param>
        /// <param name="cropColor">Color to trim away for cropping frames, if the source is high-color.</param>
        /// <param name="cropIndex">Color index to trim away for cropping frames, if the source is indexed.</param>
        /// <param name="matchBpp">Bits per pixel for the palette to match. 0 for no palette matching.</param>
        /// <param name="matchPalette">Palette to match. Only used if <see cref="matchBpp"/> is not 0.</param>
        /// <param name="framesRangeMin">Index of the first frame to retrieve.</param>
        /// <param name="framesRangeMax">Index of the last frame to retrieve.</param>
        /// <returns>An array of <see cref="Bitmap"/> objects that contains the cut-out frames.</returns>
        public static Bitmap[] ImageToFrames(Bitmap image, Int32 frameWidth, Int32 frameHeight, Color? cropColor, Int32? cropIndex, Int32 matchBpp, Color[] matchPalette, Int32 framesRangeMin, Int32 framesRangeMax)
        {
            if (image == null)
                return new Bitmap[0];
            PixelFormat origPf = image.PixelFormat;
            ColorPalette origPalette = image.Palette;
            Int32 bpp = Image.GetPixelFormatSize(origPf);
            Boolean indexed = bpp <= 8;
            Boolean convertTo32 = !indexed && origPf != PixelFormat.Format32bppArgb;
            Int32 fullWidth = image.Width;
            Int32 fullHeight = image.Height;
            Int32 framesX = fullWidth / frameWidth;
            Int32 framesY = fullHeight / frameHeight;
            Int32 frames = framesX * framesY;
            Int32 min = Math.Min(Math.Max(0, Math.Min(framesRangeMin, framesRangeMax)), frames);
            Int32 max = Math.Min(Math.Max(0, Math.Max(framesRangeMin, framesRangeMax)), frames);
            Int32 nrOfFrames = max - min + 1;
            Bitmap[] bmpFrames = new Bitmap[nrOfFrames];
            Boolean doMatching = matchBpp > 0 && matchBpp <= 8 && matchPalette != null;
            Bitmap edImage = null;
            Byte[] edImageData = null;
            Byte[] remapTable = null;
            if (indexed)
            {
                Int32 edImageStride;
                edImageData = GetImageData(image, out edImageStride);
                edImageData = ConvertTo8Bit(edImageData, fullWidth, fullHeight, 0, bpp, true, ref edImageStride);
                if (doMatching)
                {
                    Color[] orig = origPalette.Entries;
                    Int32 palLen = orig.Length;
                    remapTable = new Byte[palLen];
                    for (Int32 i = 0; i < palLen; ++i)
                    {
                        if (i >= orig.Length)
                            break;
                        remapTable[i] = (Byte)ColorUtils.GetClosestPaletteIndexMatch(orig[i], matchPalette, null);
                    }
                }
            }
            else
            {
                edImage = convertTo32 ? new Bitmap(image) : image;
            }
            for (Int32 frameNr = min; frameNr <= max; ++frameNr)
            {
                Int32 rectY = frameNr / framesX;
                Int32 rectX = frameNr % framesX;
                Rectangle section = new Rectangle(rectX * frameWidth, rectY * frameHeight, frameWidth, frameHeight);
                Bitmap bmp;
                if (indexed)
                    bmp = GetCutFrameIndexed(edImageData, fullWidth, fullHeight, section, origPf, origPalette, cropIndex, matchBpp, remapTable, matchPalette);
                else
                    bmp = GetCutFrameHiCol(edImage, section, origPf, cropColor, matchBpp, matchPalette);
                bmpFrames[frameNr - min] = bmp;
            }
            if (convertTo32)
                edImage.Dispose();
            return bmpFrames;
        }

        private static Bitmap GetCutFrameIndexed(Byte[] edImageData, Int32 width, Int32 height, Rectangle section, PixelFormat origPf, ColorPalette origPalette, Int32? trimIndex, Int32 matchBpp, Byte[] remapTable, Color[] matchPalette)
        {
            Bitmap bmp = null;
            Int32 origBpp = Image.GetPixelFormatSize(origPf);
            Boolean doMatching = matchBpp > 0 && matchBpp <= 8 && remapTable != null;
            Byte[] frameImageData = CopyFrom8bpp(edImageData, width, height, width, section);
            Int32 frameWidth = section.Width;
            Int32 frameHeight = section.Height;
            Int32 stride = frameWidth;
            if (trimIndex.HasValue && trimIndex.Value >= 0 && trimIndex.Value < origPalette.Entries.Length)
            {
                Int32 xOffset = 0;
                Int32 yOffset = 0;
                frameImageData = OptimizeYHeight(frameImageData, frameWidth, ref frameHeight, ref yOffset, true, trimIndex.Value, 0, true);
                frameImageData = OptimizeXWidth(frameImageData, ref frameWidth, frameHeight, ref xOffset, true, trimIndex.Value, 0, true);
                stride = frameWidth;
            }
            if (frameWidth > 0 && frameHeight > 0)
            {
                Color[] palette = origPalette.Entries;
                if (doMatching)
                {
                    for (Int32 i = 0; i < frameImageData.Length; ++i)
                        frameImageData[i] = remapTable[frameImageData[i]];
                    origBpp = matchBpp;
                    palette = matchPalette;
                    origPf = GetIndexedPixelFormat(matchBpp);
                }
                if (origBpp < 8)
                    frameImageData = ConvertFrom8Bit(frameImageData, frameWidth, frameHeight, origBpp, true, ref stride);
                bmp = BuildImage(frameImageData, frameWidth, frameHeight, stride, origPf, palette, Color.Black);
                if (!doMatching && origPalette.Entries.Length < palette.Length)
                    bmp.Palette = origPalette;
            }
            return bmp;
        }

        private static Bitmap GetCutFrameHiCol(Bitmap edImage, Rectangle section, PixelFormat origPf, Color? trimColor, Int32 matchBpp, Color[] matchPalette)
        {
            Bitmap bmp = null;
            Boolean doMatching = matchBpp > 0 && matchBpp <= 8 && matchPalette != null;
            if (trimColor.HasValue)
            {
                section = GetCropBounds(edImage, trimColor.Value, 0, section);
            }
            if (section.Width > 0 && section.Height > 0)
            {
                bmp = new Bitmap(section.Width, section.Height);
                // This operation is not thread-safe! Do not feed images into this which are being used on the UI and might be repainted during this operation.
                using (Graphics g = Graphics.FromImage(bmp))
                    g.DrawImage(edImage, new Rectangle(0, 0, section.Width, section.Height), section, GraphicsUnit.Pixel);
                if (doMatching)
                {
                    Bitmap newBmp = ConvertToPalette(bmp, matchBpp, matchPalette);
                    bmp.Dispose();
                    bmp = newBmp;
                }
                else if (Image.GetPixelFormatSize(origPf) > 8 && origPf != PixelFormat.Format32bppArgb)
                {
                    Int32 stride;
                    Byte[] frameData = GetImageData(bmp, out stride, origPf);
                    Bitmap newBmp = BuildImage(frameData, section.Width, section.Height, stride, origPf, null, null);
                    bmp.Dispose();
                    bmp = newBmp;
                }
                // else: target is 32bpp already; don't convert&replace.
            }
            return bmp;
        }
        
        public static Bitmap BuildImageFromFrames(Bitmap[] images, Int32 framesWidth, Int32 framesHeight, Int32 framesPerLine, Byte backFillPalIndex, Color backFillColor)
        {
            PixelFormat highestPf = PixelFormat.Undefined;
            Int32 highestBpp = 0;
            Color[] palette = null;
            ColorPalette paletteRaw = null;
            Int32 imagesLength = images.Length;
            for (Int32 i = 0; i < imagesLength; ++i)
            {
                Bitmap img = images[i];
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
            Boolean useRaw = highestBpp <= 8 && palette != null && palette.Length < (1 << highestBpp);
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
                    // allows painting with transparency.
                    g.CompositingMode = CompositingMode.SourceCopy;
                    if (backFillColor.ToArgb() != 0)
                        using (SolidBrush myBrush = new SolidBrush(Color.FromArgb(255, backFillColor)))
                            g.FillRectangle(myBrush, new Rectangle(0, 0, fullWidth, fullHeight));
                    for (Int32 i = 0; i < frames; ++i)
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
                    Byte[] imageData = GetImageData(bp, out stride, highestPf);
                    bp.Dispose();
                    bp = BuildImage(imageData, fullWidth, fullHeight, stride, highestPf, null, null);
                }
            }
            else
            {
                Byte[] bpData = new Byte[fullWidth * fullHeight];
                if (backFillPalIndex > 0)
                    for (Int32 i = 0; i < bpData.Length; ++i)
                        bpData[i] = backFillPalIndex;
                for (Int32 i = 0; i < frames; ++i)
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
            Boolean isPng = PngHandler.IsPng(data);
            Int32 trnsOffset = isPng ? FindPngTransparencyChunk(data) : -1;
            Int32 trnsLength = -1;
            if (trnsOffset != -1)
            {
                // Get chunk
                trnsLength = PngHandler.GetPngChunkDataLength(data, trnsOffset);
                transparencyData = PngHandler.GetPngChunkData(data, trnsOffset, trnsLength);
                // filter out the palette alpha chunk, make new data array
                Byte[] data2 = new Byte[data.Length - (trnsLength + 12)];
                Array.Copy(data, 0, data2, 0, trnsOffset);
                Int32 trnsEnd = trnsOffset + trnsLength + 12;
                Array.Copy(data, trnsEnd, data2, trnsOffset, data.Length - trnsEnd);
                data = data2;
            }
            using (MemoryStream ms = new MemoryStream(data))
            using (Bitmap imageFromStream = new Bitmap(ms))
            {
                if (isPng)
                {
                    // Shortcut; reading data from where the header should be in a PNG.
                    Byte colDepth = data[24];
                    Byte colType = data[25];
                    if (colType == 0 && imageFromStream.PixelFormat == PixelFormat.Format32bppArgb)
                    {
                        // colType == 0 means the image is actually grayscale, not 32bpp. Restore original colour depth.
                        // This type supports no palette transparency mask afaik, so this operation should exclude the other one.
                        Int32 width = imageFromStream.Width;
                        Int32 height = imageFromStream.Height;
                        Int32 stride32;
                        Byte[] imgBytes32 = GetImageData(imageFromStream, out stride32);
                        return FixGrayScaleFrom32bpp(imgBytes32, width, height, stride32, colDepth);
                    }
                }
                if (imageFromStream.Palette.Entries.Length != 0 && transparencyData != null)
                {
                    ColorPalette pal = imageFromStream.Palette;
                    Int32 palLength = pal.Entries.Length;
                    for (Int32 i = 0; i < palLength && i < trnsLength; ++i)
                    {
                        Byte alpha = transparencyData[i];
                        if (alpha == 255)
                            continue;
                        Color col = pal.Entries[i];
                        pal.Entries[i] = Color.FromArgb(alpha, col.R, col.G, col.B);
                    }
                    imageFromStream.Palette = pal;
                }
                return CloneImage(imageFromStream);
            }
        }

        private static Int32 FindPngTransparencyChunk(Byte[] data)
        {
            Int32 hdrOffset = PngHandler.FindPngChunk(data, "IHDR");
            if (hdrOffset == -1)
                return -1;
            Byte[] header = PngHandler.GetPngChunkData(data, hdrOffset);
            // Check if type is paletted ('3')
            if (header.Length < 13 || header[9] != 3)
                return -1;
            // Check if it really contains a palette.
            Int32 plteOffset = PngHandler.FindPngChunk(data, "PLTE");
            if (plteOffset == -1)
                return -1;
            // Check if it contains a palette transparency chunk.
            return PngHandler.FindPngChunk(data, "tRNS");
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
                for (Int32 i = 0; i < palette.Length; ++i)
                    changedPal.Entries[i] = Color.FromArgb(0xFF, palette[i]);
                if (!noPalTrans)
                    for (Int32 i = 0; i < paletteLength; ++i)
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
            Int32 paletteDataLength = paletteLength * 3;
            Int32 plteOffset = PngHandler.FindPngChunk(data, "PLTE");
            if (plteOffset == -1)
                return data;
            Int32 plteLength = PngHandler.GetPngChunkDataLength(data, plteOffset) + 12;
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
            Int32 trnsOffset = PngHandler.FindPngChunk(data, "tRNS");
            if (trnsOffset != -1)
            {
                oldTrnsDataLength = PngHandler.GetPngChunkDataLength(data, trnsOffset);
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
            currentPosTrg = PngHandler.WritePngChunk(newData, currentPosTrg, "PLTE", paletteData);
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
                currentPosTrg = PngHandler.WritePngChunk(newData, currentPosTrg, "tRNS", transparencyData);
                currentPosSrc += oldTrnsDataLength;
            }
            Array.Copy(data, currentPosSrc, newData, currentPosTrg, data.Length - currentPosSrc);
            data = newData;
            return data;
        }

        /// <summary>
        /// Restores a grayscale image that is loaded as 32bpp back to its correct grayscale colour depth.
        /// This does not CONVERT the image to grayscale; it assumes the image is already grayscale
        /// and will use the blue channel as its value.
        /// </summary>
        /// <param name="imgBytes32">Image bytes in 32bppArgb format</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="stride32">Stride of the 32bpp data.</param>
        /// <param name="targetDepth">Colour depth to restore the image to.</param>
        /// <returns>The restored grayscale image.</returns>
        public static Bitmap FixGrayScaleFrom32bpp(Byte[] imgBytes32, Int32 width, Int32 height, Int32 stride32, Byte targetDepth)
        {
            // 2bpp is not supported in .Net, so upgrade it to 4.
            if (targetDepth == 2)
                targetDepth = 4;
            else if (targetDepth > 8)
                targetDepth = 8;
            Int32 stride8 = width;
            Byte[] imgBytes8 = new Byte[width * height];
            Int32 linePos32 = 0;
            Int32 linePos8 = 0;
            if (targetDepth == 8)
            {

                for (Int32 y = 0; y < height; ++y)
                {
                    Int32 pos32 = linePos32;
                    Int32 pos8 = linePos8;
                    for (Int32 x = 0; x < width; ++x)
                    {
                        imgBytes8[pos8] = imgBytes32[pos32]; // Blue factor. If grayscale, this is the value.
                        pos8++;
                        pos32 += 4;
                    }
                    linePos32 += stride32;
                    linePos8 += stride8;
                }
            }
            else
            {
                // Uses the principles of the PixelFormatter while avoiding heaviy operations.
                Int32 maxVal = ((1 << targetDepth) - 1);
                Double multiplier = maxVal / 255.0;
                for (Int32 y = 0; y < height; ++y)
                {
                    Int32 pos32 = linePos32;
                    Int32 pos8 = linePos8;
                    for (Int32 x = 0; x < width; ++x)
                    {
                        // Blue factor. If grayscale, this is the value.
                        UInt32 tempVal = (UInt32)Math.Min(maxVal, Math.Round(imgBytes32[pos32] * multiplier, MidpointRounding.AwayFromZero));
                        imgBytes8[pos8] = (Byte)tempVal;
                        pos8++;
                        pos32 += 4;
                    }
                    linePos32 += stride32;
                    linePos8 += stride8;
                }
                imgBytes8 = ConvertFrom8Bit(imgBytes8, width, height, targetDepth, true, ref stride8);
            }
            PixelFormat pf = GetIndexedPixelFormat(targetDepth);
            // Return the result right away. No clone is needed since this object is new.
            return BuildImage(imgBytes8, width, height, stride8, pf, PaletteUtils.GenerateGrayPalette(targetDepth, null, false), null);
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
            return GetPalette(colors, colors.Length);
        }

        /// <summary>
        /// Creates a custom-sized color palette by creating an empty png with a limited palette and extracting its palette.
        /// </summary>
        /// <param name="colors">The colors to convert into a palette.</param>
        /// <param name="amount">Amount of colors in the new palette.</param>
        /// <returns>A color palette containing the given colors.</returns>
        public static ColorPalette GetPalette(Color[] colors, Int32 amount)
        {
            // Silliest idea ever, but it works.
            const Int32 chunkExtraLen = 0x0C;
            Byte[] pngStart = PngHandler.GetPngIdentifier();
            Int32 pngStartLen = pngStart.Length;
            const Int32 lenHdr = 0x0D;
            Int32 lenPal = Math.Min(amount, 0x100) * 3;
            Byte[] blankPngIdat = PngHandler.GetBlankPngIdatContents();
            Int32 idatLen = blankPngIdat.Length;
            Int32 fullLen = pngStartLen + lenHdr + chunkExtraLen + lenPal + chunkExtraLen + idatLen + chunkExtraLen + chunkExtraLen;
            Int32 offset = 0;
            Byte[] emptyPng = new Byte[fullLen];
            Array.Copy(pngStart, 0, emptyPng, 0, pngStartLen);
            offset += pngStartLen;
            Byte[] header = new Byte[lenHdr];
            // UInt32 Width: 1
            header[3] = 1; // Big-endian, so last of the four bytes.
            // UInt32 Heigth: 1
            header[7] = 1; // Big-endian, so last of the four bytes.
            // Byte Color depth: 8
            header[8] = 8;
            // Byte Color type: paletted
            header[9] = 3;
            PngHandler.WritePngChunk(emptyPng, offset, "IHDR", header);
            offset += lenHdr + chunkExtraLen;
            // Don't even need to fill this in. We just need the size.
            // Easier to fill it in later and preserve alpha.
            Byte[] palette = new Byte[lenPal];
            PngHandler.WritePngChunk(emptyPng, offset, "PLTE", palette);
            offset += lenPal + chunkExtraLen;
            PngHandler.WritePngChunk(emptyPng, offset, "IDAT", blankPngIdat);
            offset += idatLen + chunkExtraLen;
            PngHandler.WritePngChunk(emptyPng, offset, "IEND", new Byte[0]);
            using (MemoryStream ms = new MemoryStream(emptyPng))
            using (Bitmap loadedImage = new Bitmap(ms))
            {
                ColorPalette pal = loadedImage.Palette;
                for (Int32 i = 0; i < pal.Entries.Length; ++i)
                    pal.Entries[i] = colors[i];
                return pal;
            }
        }

    }
}
