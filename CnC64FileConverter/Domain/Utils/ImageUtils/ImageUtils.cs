using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Nyerguds.ImageManipulation
{
    public static class ImageUtils
    {
        private static Color[] ConvertToColors(Byte[] colorData, Bitmap sourceImage, Int32? depth)
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
                    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
                    Guid formatId = ImageFormat.Jpeg.Guid;
                    foreach (ImageCodecInfo codec in codecs)
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

        public static Int32 GetMinimumStride(Int32 width, Int32 bitsLength)
        {
            Int32 stride = bitsLength * width;
            return (stride / 8) + ((stride % 8) > 0 ? 1 : 0);
        }

        public static Bitmap ConvertToPalettedGrayscale(Bitmap image)
        {
            return ConvertToPalettedGrayscale(image, 8);
        }

        public static Bitmap ConvertToPalettedGrayscale(Bitmap image, Int32 bpp)
        {
            PixelFormat pf = PaletteUtils.GetPalettedFormat(bpp);
            if (image.PixelFormat == PixelFormat.Format4bppIndexed || image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                if (image.PixelFormat == pf && ColorUtils.HasGrayPalette(image))
                    return CloneImage(image);
            }
            if (image.PixelFormat != PixelFormat.Format32bppArgb)
                image = PaintOn32bpp(image, Color.Black);
            Int32 grayBpp = Image.GetPixelFormatSize(pf);
            Int32 stride;
            Byte[] imageData = GetImageData(image, out stride);
            imageData = Convert32bToGray(imageData, image.Width, image.Height, grayBpp, ref stride);
            return BuildImage(imageData, image.Width, image.Height, stride, pf, PaletteUtils.GenerateGrayPalette(grayBpp, false, false), null);
        }

        public static Bitmap PaintOn32bpp(Image image, Color? transparencyColor)
        {
            Bitmap bp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bp))
            {
                if (transparencyColor.HasValue)
                    using (System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(transparencyColor.Value))
                        gr.FillRectangle(myBrush, new Rectangle(0, 0, image.Width, image.Height));
                gr.DrawImage(image, new Rectangle(0, 0, bp.Width, bp.Height));
            }
            return bp;
        }

        public static Byte[] Convert32bToGray(Byte[] imageData, Int32 width, Int32 height, Int32 bpp, ref Int32 stride)
        {
            if (width * 4 > stride)
                throw new ArgumentException("Stride is smaller than one scan line!", "stride");
            Int32 divvalue = 256 / (1 << bpp);
            Byte[] newImageData = new Byte[width * height];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 inputOffs = y * stride + x * 4;
                    Int32 outputOffs = y * width + x;
                    Color c = PixelFormatter.Format32BitArgb.GetColor(imageData, inputOffs);
                    if (c.A < 128)
                        newImageData[outputOffs] = 0;
                    else
                        newImageData[outputOffs] = (Byte)((c.R + c.G + c.B) / (3 * divvalue));
                }
            }
            stride = width;
            if (bpp < 8)
                newImageData = ConvertFrom8Bit(newImageData, width, height, bpp, true, ref stride);
            return newImageData;
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

        public static Byte[] Convert32BitToPaletted(Byte[] imageData, Int32 width, Int32 height, Int32 bpp, Color[] palette, ref Int32 stride)
        {
            if (width * 4 > stride)
                throw new ArgumentException("Stride is smaller than one scan line!", "stride");
            Byte[] newImageData = new Byte[width * height];
            List<Int32> transparentIndices = new List<Int32>();
            for (Int32 i = 0; i < palette.Length; i++)
                if (palette[i].A == 0)
                    transparentIndices.Add(i);

            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 inputOffs = y * stride + x * 4;
                    Int32 outputOffs = y * width + x;
                    Color c = PixelFormatter.Format32BitArgb.GetColor(imageData, inputOffs);
                    if (c.A < 128)
                        newImageData[outputOffs] = 0;
                    else
                        newImageData[outputOffs] = (Byte)ColorUtils.GetClosestPaletteIndexMatch(c, palette, transparentIndices);
                }
            }
            stride = width;
            if (bpp < 8)
                newImageData = ConvertFrom8Bit(newImageData, width, height, bpp, true, ref stride);
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
        /// Clones an image object.
        /// Code taken from http://stackoverflow.com/a/3661892/ with some extra fixes.
        /// </summary>
        /// <param name="sourceImage">The image to clone</param>
        /// <returns>The cloned image</returns>
        public static Bitmap CloneImage(Bitmap sourceImage)
        {
            return CloneImage(sourceImage, null);
        }

        /// <summary>
        /// Clones an image object, cutting a piece out of the original image..
        /// Code taken from http://stackoverflow.com/a/3661892/ with some extra fixes.
        /// </summary>
        /// <param name="sourceImage">The image to clone</param>
        /// <param name="sourceRect">Piece to cut out of the original image.</param>
        /// <returns>The cloned image</returns>
        public static Bitmap CloneImage(Bitmap sourceImage, Rectangle? sourceRect)
        {
            Rectangle rect;
            if (sourceRect.HasValue)
                rect = sourceRect.Value;
            else
                rect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
            if (rect.X + rect.Width > rect.Width || rect.Y + rect.Height > sourceImage.Height)
                throw new InvalidOperationException("Cutout size for image is larger than image!");

            Bitmap targetImage = new Bitmap(rect.Width, rect.Height, sourceImage.PixelFormat);
            BitmapData sourceData = sourceImage.LockBits(rect, ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            BitmapData targetData = targetImage.LockBits(new Rectangle(0, 0, targetImage.Width, targetImage.Height), ImageLockMode.WriteOnly, targetImage.PixelFormat);
            CopyMemory(targetData.Scan0, sourceData.Scan0, sourceData.Stride * sourceData.Height, 1024, 1024);
            targetImage.UnlockBits(targetData);
            sourceImage.UnlockBits(sourceData);
            // For 8-bit images, restore the palette. This is not linking to a referenced
            // object in the original image; the getter creates a new object when called.
            if (sourceImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
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
        
        public static void CopyToMemory(IntPtr target, Byte[] bytes, Int32 startPos, Int32 length, Int32 origStride, Int32 targetStride)
        {
            Int32 sourcePos = startPos;
            IntPtr destPos = target;
            Int32 minStride = Math.Min(origStride, targetStride);
            while (length >= targetStride)
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

        public static void CopyMemory(IntPtr target, IntPtr source, Int32 length, Int32 origStride, Int32 targetStride)
        {
            IntPtr sourcePos = source;
            IntPtr destPos = target;
            Int32 minStride = Math.Min(origStride, targetStride);
            Byte[] imageData = new Byte[targetStride];

            while (length >= targetStride)
            {
                Marshal.Copy(sourcePos, imageData, 0, minStride);
                Marshal.Copy(imageData, 0, destPos, targetStride);
                length -= origStride;
                sourcePos = new IntPtr(sourcePos.ToInt64() + origStride);
                destPos = new IntPtr(destPos.ToInt64() + targetStride);
            }
            if (length > 0)
            {
                Marshal.Copy(sourcePos, imageData, 0, length);
                Marshal.Copy(imageData, 0, destPos, length);
            }
        }

        /// <summary>
        /// Checks if a given image contains transparency.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Boolean HasTransparency(Bitmap bitmap)
        {
            // not an alpha-capable color format.
            if ((bitmap.Flags & (Int32)ImageFlags.HasAlpha) == 0)
                return false;
            // Indexed formats. Special case because one index on their palette is configured as THE transparent color.
            Int32 colDepth = Image.GetPixelFormatSize(bitmap.PixelFormat);
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && colDepth <= 8)
            {
                ColorPalette pal = bitmap.Palette;
                // Find the transparent indexes on the palette.
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
                Int32 stride;
                Byte[] bytes = GetImageData(bitmap, out stride);
                bytes = ConvertTo8Bit(bytes, bitmap.Width, bitmap.Height, 0, colDepth, false, ref stride);
                foreach (Byte b in bytes)
                {
                    if (transCols.Contains(b))
                        return true;
                }
                return false;
            }
            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppPArgb)
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                Byte[] bytes = new Byte[bitmap.Height * data.Stride];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                bitmap.UnlockBits(data);
                for (Int32 p = 3; p < bytes.Length; p += 4)
                {
                    if (bytes[p] != 255)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Final "screw it all" method. This is pretty slow, but it won't ever be used, unless you
            // encounter some really esoteric types not handled above, like 16bppArgb1555 and 64bppArgb.
            for (Int32 i = 0; i < bitmap.Width; i++)
            {
                for (Int32 j = 0; j < bitmap.Height; j++)
                {
                    if (bitmap.GetPixel(i, j).A != 255)
                        return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Calculates the minimum amount of bytes needed to write one line of data of the given bits per pixel.
        /// </summary>
        /// <param name="width">Width of the image</param>
        /// <param name="bpp">Bits per pixel.</param>
        /// <returns>The minimum amount of bytes needed to write one line of data of the given bits per pixel.</returns>
        public static Int32 GetMinStride(Int32 width, Int32 bpp)
        {
            Int32 stride = bpp * width;
            stride = (stride / 8) + ((stride % 8) > 0 ? 1 : 0);
            return stride;
        }

        /// <summary>
        /// Copies a piece out of an 8-bit image.
        /// </summary>
        /// <param name="fileData">Byte data of the image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="stride">Stride of the image.</param>
        /// <param name="copyArea">The area to copy.</param>
        /// <returns></returns>
        public static Byte[] CopyFrom8bpp(Byte[] fileData, Int32 width, Int32 height, Int32 stride, out Int32 copyStride, Rectangle copyArea)
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
            copyStride = copyArea.Width;
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
                pasteFileData = CopyFrom8bpp(pasteFileData, pasteWidth, pasteHeight, pasteStride, out pasteStride, new Rectangle(0, 0, targetPos.Width, targetPos.Height));
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
        /// Converts given raw image data for a paletted image to 8-bit, so we have a simple one-byte-per-pixel format to work with.
        /// Stride is assumed to be the minimum needed to contain the data. Output stride will be the same as the width.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="oldBpp">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        public static Byte[] ConvertTo8Bit(Byte[] fileData, Int32 width, Int32 height, Int32 start, Int32 oldBpp, Boolean bigEndian)
        {
            Int32 stride = GetMinStride(width, oldBpp);
            return ConvertTo8Bit(fileData, width, height, start, oldBpp, bigEndian, ref stride);
        }

        /// <summary>
        /// Converts given raw image data for a paletted image to 8-bit, so we have a simple one-byte-per-pixel format to work with.
        /// The stride of the output will be exactly the same as the width.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="oldBpp">Amount of bits used by one pixel.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        public static Byte[] ConvertTo8Bit(Byte[] fileData, Int32 width, Int32 height, Int32 start, Int32 oldBpp, Boolean bigEndian, ref Int32 stride)
        {
            if (oldBpp != 1 && oldBpp != 2 && oldBpp != 4 && oldBpp != 8)
                throw new ArgumentOutOfRangeException("Cannot handle image data with " + oldBpp + "bits per pixel.", "oldBpp");
            if (stride < GetMinStride(width, oldBpp))
                throw new ArgumentException("Stride is too small for the given width!", "stride");
            // Full array
            Byte[] data8bit = new Byte[width * height];
            // Amount of runs that end up on the same pixel
            Int32 parts = 8 / oldBpp;
            // Amount of bytes to read per width
            Int32 newStride = width;
            // Bit mask for reducing read and shifted data to actual bits length
            Int32 bitmask = (1 << oldBpp) - 1;
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
                    Int32 shift = (x % parts) * oldBpp;
                    // Reversed for big-endian
                    if (bigEndian)
                        shift = 8 - shift - oldBpp;
                    // Get data and store it.
                    data8bit[index8bit] = (Byte)((fileData[indexXbit] >> shift) & bitmask);
                }
            }
            stride = newStride;
            return data8bit;
        }

        /// <summary>
        /// Converts given raw image data for a paletted 8-bit image to lower amount of bits per pixel.
        /// Input stride is assumed to be the same as the width. Output stride is the minimum needed to contain the data.
        /// </summary>
        /// <param name="data8bit">The eight bit per pixel image data</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="newBpp">The new amount of bits per pixel</param>
        /// <param name="bigEndian">True if the bits in the new image data are to be stored as big-endian.</param>
        /// <returns>The image data converted to the requested amount of bits per pixel.</returns>
        public static Byte[] ConvertFrom8Bit(Byte[] data8bit, Int32 width, Int32 height, Int32 newBpp, Boolean bigEndian)
        {
            Int32 stride = width;
            return ConvertFrom8Bit(data8bit, width, height, newBpp, bigEndian, ref stride);
        }

        /// <summary>
        /// Converts given raw image data for a paletted 8-bit image to lower amount of bits per pixel.
        /// </summary>
        /// <param name="data8bit">The eight bit per pixel image data</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="newBpp">The new amount of bits per pixel</param>
        /// <param name="bigEndian">True if the bits in the new image data are to be stored as big-endian.</param>
        /// <param name="stride">Stride used in the original image data. Will be adjusted to the new stride value.</param>
        /// <returns>The image data converted to the requested amount of bits per pixel.</returns>
        public static Byte[] ConvertFrom8Bit(Byte[] data8bit, Int32 width, Int32 height, Int32 newBpp, Boolean bigEndian, ref Int32 stride)
        {
            if (newBpp > 8)
                throw new ArgumentException("Cannot convert to bit format greater than 8!","newBpp");
            if (stride < width)
                throw new ArgumentException("Stride is too small for the given width!", "stride");
            if (data8bit.Length < stride * height)
                throw new ArgumentException("Data given data is too small to contain an 8-bit image of the given dimensions", "data8bit");
            Int32 parts = 8 / newBpp;
            // Amount of bytes to write per scanline
            Int32 newStride = GetMinStride(width, newBpp);
            // Bit mask for reducing original data to actual bits maximum.
            // Should not be needed if data is correct, but eh.
            Int32 bitmask = (1 << newBpp) - 1;
            Byte[] dataXbit = new Byte[newStride * height];
            // Actual conversion porcess.
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // Source. This will always get a new index
                    Int32 index8bit = y * stride + x;
                    // Target. This will hit the same byte multiple times
                    Int32 indexXbit = y * newStride + x / parts;
                    // Amount of bits to shift the data to get to the current pixel data
                    Int32 shift = (x % parts) * newBpp;
                    // Reversed for big-endian
                    if (bigEndian)
                        shift = 8 - shift - newBpp;
                    // Get data, reduce to max amount of bits, shift it and store it.
                    dataXbit[indexXbit] |= (Byte)((data8bit[index8bit] & bitmask) << shift);
                }
            }
            stride = newStride;
            return dataXbit;
        }

    }
}
