using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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
            image = CloneImage(image);
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
                    System.Drawing.Imaging.Encoder qualityEncoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters encparams = new EncoderParameters(1);
                    encparams.Param[0] = new EncoderParameter(qualityEncoder, 100L);
                    image.Save(ms, jpegEncoder, encparams);
                }
                else if (saveFormat.Equals(ImageFormat.Gif) && image.PixelFormat == PixelFormat.Format4bppIndexed)
                {
                    // 4-bit images don't get converted right; they get dumped on the standard windows 256 colour palette. So we convert it manually before the save.
                    Int32 stride;
                    Byte[] fourBitData = GetImageData(image, out stride);
                    // data returned from this always has the exact width as stride.
                    Byte[] eightBitData = ConvertTo8Bit(fourBitData, image.Width, image.Height, 0, 4, true, ref stride);
                    image = BuildImage(eightBitData, image.Width, image.Height, stride, PixelFormat.Format8bppIndexed, image.Palette.Entries, Color.Black);
                    image.Save(ms, saveFormat);
                }
                else if (saveFormat.Equals(ImageFormat.Png))
                    BitmapHandler.GetPngImageData(image, 0);
                else
                    image.Save(ms, saveFormat);
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

        public static Bitmap ConvertToPalettedGrayscale(Bitmap image)
        {
            return ConvertToPalettedGrayscale(image, 8, false);
        }

        public static Bitmap ConvertToPalettedGrayscale(Bitmap image, Int32 bpp, Boolean bigEndianBits)
        {
            PixelFormat pf = PaletteUtils.GetPalettedFormat(bpp);
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
                    using (System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(Color.FromArgb(255, transparencyFillColor.Value)))
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
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 inputOffs = y * stride + x * 4;
                    Int32 outputOffs = y * width + x;
                    Color c = Color.FromArgb(imageData[inputOffs + 3], imageData[inputOffs + 2], imageData[inputOffs + 1], imageData[inputOffs]);
                    if (c.A < 128)
                        newImageData[outputOffs] = 0;
                    else
                        newImageData[outputOffs] = (Byte)((c.R + c.G + c.B) / (3 * divvalue));
                }
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

        public static PixelFormat GetPixelFormat(Int32 bpp)
        {
            switch (bpp)
            {
                case 1: return PixelFormat.Format1bppIndexed;
                case 4: return PixelFormat.Format4bppIndexed;
                case 8: return PixelFormat.Format8bppIndexed;
                default: throw new NotSupportedException("Unsupported indexed pixel format '" + bpp + "'!");
            }
        }

        public static Bitmap ConvertToPalette(Bitmap originalImage, Int32 bpp, Color[] palette)
        {
            PixelFormat pf = GetPixelFormat(bpp);
            Int32 stride;
            if (originalImage.PixelFormat != PixelFormat.Format32bppArgb)
                originalImage = PaintOn32bpp(originalImage, Color.Black);
            Byte[] imageData = GetImageData(originalImage, out stride);
            Byte[] palettedData = Convert32BitToPaletted(imageData, originalImage.Width, originalImage.Height, bpp, bpp == 1, palette, ref stride);
            return BuildImage(palettedData, originalImage.Width, originalImage.Height, stride, pf, palette, Color.Black);
        }

        public static Byte[] Convert32BitToPaletted(Byte[] imageData, Int32 width, Int32 height, Int32 bpp, Boolean bigEndianBits, Color[] palette, ref Int32 stride)
        {
            if (stride < width * 4)
                throw new ArgumentException("Stride is smaller than one pixel line!", "stride");
            Byte[] newImageData = new Byte[width * height];
            List<Int32> transparentIndices = new List<Int32>();
            for (Int32 i = 0; i < palette.Length; i++)
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
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, sourceImage.PixelFormat);
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
        /// <param name="sourceImage">The image to clone</param>
        /// <returns>The cloned image</returns>
        public static Bitmap CloneImage(Bitmap sourceImage)
        {
            Rectangle rect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
            Bitmap targetImage = new Bitmap(rect.Width, rect.Height, sourceImage.PixelFormat);
            BitmapData sourceData = sourceImage.LockBits(rect, ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            BitmapData targetData = targetImage.LockBits(rect, ImageLockMode.WriteOnly, targetImage.PixelFormat);
            Int32 actualDataWidth = ((Image.GetPixelFormatSize(sourceImage.PixelFormat) * rect.Width) + 7) / 8;
            Int32 h = sourceImage.Height;
            Int32 origStride = sourceData.Stride;
            Int32 targetStride = targetData.Stride;
            Byte[] imageData = new Byte[actualDataWidth];
            IntPtr sourcePos = sourceData.Scan0;
            IntPtr destPos = targetData.Scan0;
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
            // object in the original image; the getter creates a new object when called.
            if ((sourceImage.PixelFormat & PixelFormat.Indexed) != 0)
                targetImage.Palette = sourceImage.Palette;
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
            CopyToMemory(targetData.Scan0, sourceData, 0, sourceData.Length, stride, targetData.Stride);
            newImage.UnlockBits(targetData);
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

        private static void CopyToMemory(IntPtr target, Byte[] bytes, Int32 startPos, Int32 length, Int32 origStride, Int32 targetStride)
        {
            Int32 sourcePos = startPos;
            IntPtr destPos = target;
            Int32 minStride = Math.Min(origStride, targetStride);
            while (length >= origStride)
            {
                Marshal.Copy(bytes, sourcePos, destPos, minStride);
                length -= origStride;
                sourcePos += origStride;
                destPos = new IntPtr(destPos.ToInt64() + targetStride);
            }
            if (length > 0)
            {
                Marshal.Copy(bytes, sourcePos, destPos, length);
            }
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
                for (int i = 0; i < pal.Entries.Length; i++)
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
            Color[] pal = GeneratePalette(colors, Color.Empty);
            Byte[] patternArray = new Byte[width * height];
            for (Int32 y = 0; y < width; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 offset = x + y * height;
                    patternArray[offset] = (Byte)(((x + y) % 2 == 0) ? 1 : 0);
                }
            }
            return BuildImage(patternArray, width, height, width, PixelFormat.Format8bppIndexed, pal, Color.Empty);
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
            // Could technically design this to edit the bytes directly instead of copying, but this way doesn't require (technically) unsafe code.
            Byte[] picData = new Byte[sourceData.Stride * sourceData.Height];
            Int32 sourceStride = sourceData.Stride;
            Marshal.Copy(sourceData.Scan0, picData, 0, picData.Length);
            source.UnlockBits(sourceData);
            // =======================================
            // Call delegate function to perform the actual actions.
            editDelegate(picData, sourceStride);
            // =======================================
            BitmapData destData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.WriteOnly, source.PixelFormat);
            CopyToMemory(destData.Scan0, picData, 0, picData.Length, sourceStride, destData.Stride);
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
            if (backColor != 0)
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
            Rectangle targetPos, Color[] transparencyGuide, Boolean modifyOrig)
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
                    isTransparent[i] = transparencyGuide[i].A < 128;
            }
            Int32 maxY = Math.Min(sourceHeight - targetPos.Y, targetPos.Height);
            Int32 maxX = Math.Min(sourceWidth - targetPos.X, targetPos.Width);
            for (Int32 y = 0; y < maxY; y++)
            {
                for (Int32 x = 0; x < maxX; x++)
                {
                    // This will hit the same byte multiple times
                    Int32 indexDest = (targetPos.Y + y) * sourceStride + targetPos.X + x;
                    // This will always get a new index
                    Int32 indexSource = y * targetPos.Width + x;
                    Byte data = pasteFileData[indexSource];
                    if (!isTransparent[data])
                        finalFileData[indexDest] = data;
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
        /// <param name="bigEndian">True if the bits in the new image data are to be stored as big-endian.</param>
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
            byte[] fullImageData = new Byte[fullImageWidth * fullImageHeight];
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
                        new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight), palette, true);
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
                return adjustBuffer? new Byte[0] : buffer;
            // Nothing to process
            if (width == 0)
            {
                height = 0;
                yOffset = 0;
                return adjustBuffer? new Byte[0] : buffer;
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
                return adjustBuffer? new Byte[0] : buffer;
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
                return adjustBuffer ? buffer : new Byte[0];
            // Nothing to process
            if (height == 0)
            {
                width = 0;
                xOffset = 0;
                return adjustBuffer ? buffer : new Byte[0];
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
            if(adjustBuffer)
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

    }
}
