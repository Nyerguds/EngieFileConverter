using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace Nyerguds.ImageManipulation
{
    public static class ImageUtils
    {

        public static Color GetVisibleBorderColor(Color color)
        {
            float sat = color.GetSaturation();
            float bri = color.GetBrightness();
            if (color.GetSaturation() < .16)
            {
                // this color is gray
                if (color.GetBrightness() < .5)
                    return Color.White;
                else
                    return Color.Black;
            }
            else return GetInvertedColor(color);
        }

        public static Color GetInvertedColor(Color color)
        {
            return Color.FromArgb((Int32)(0x00FFFFFFu ^ (UInt32)color.ToArgb()));
        }

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
                    Byte[] eightBitData = ConvertTo8Bit(fourBitData, image.Width, image.Height, 0, 4, stride, true);
                    image = BuildImage(eightBitData, image.Width, image.Height, image.Width, PixelFormat.Format8bppIndexed, image.Palette.Entries, Color.Black);
                    image.Save(ms, saveFormat);
                }
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
        
        public static Bitmap ConvertToGrayscale(Bitmap image)
        {
            PixelFormat pf;
            if (image.PixelFormat == PixelFormat.Format4bppIndexed || image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                if (ColorUtils.HasGrayPalette(image))
                    return image;
                pf = image.PixelFormat;
            }
            else
                pf = PixelFormat.Format8bppIndexed;
            if (image.PixelFormat != PixelFormat.Format32bppArgb)
                image = PaintOn32bpp(image);
            Int32 grayBpp = Image.GetPixelFormatSize(pf);
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            imageData = ImageUtils.Convert32bToGray(imageData, image.Width, image.Height, grayBpp, ref stride);
            return BuildImage(imageData, image.Width, image.Height, stride, pf, ColorUtils.GenerateGrayPalette(grayBpp), null);
        }

        public static Bitmap PaintOn32bpp(Bitmap image)
        {
            Bitmap bp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bp))
                gr.DrawImage(image, new Rectangle(0, 0, bp.Width, bp.Height));
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
                    Color c = GetColorFrom32BitData(imageData, inputOffs, true);
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
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 inputOffs = y * stride + x * 4;
                    Int32 outputOffs = y * width + x;
                    Color c = GetColorFrom32BitData(imageData, inputOffs, true);
                    if (c.A < 128)
                        newImageData[outputOffs] = 0;
                    else
                        newImageData[outputOffs] = (Byte)ColorUtils.GetClosestPaletteIndexMatch(c, palette, null);
                }
            }
            stride = width;
            if (bpp < 8)
                newImageData = ConvertFrom8Bit(newImageData, width, height, bpp, true, ref stride);
            return newImageData;
        }


        /// <summary>
        /// Gets a colour from data
        /// </summary>
        /// <param name="data">The data array</param>
        /// <param name="offset">The offset of the color in the data array.</param>
        /// <param name="allowTransparency">False to force transaprency to 255</param>
        /// <returns>The color data at that position</returns>
        public static Color GetColorFrom32BitData(Byte[] data, Int32 offset, Boolean allowTransparency)
        {
            if (offset >= data.Length)
                return Color.Empty;
            //0x00334455 == 55 44 33 00 == R=55, G=44, B=33, A=00
            Int32 blue = data[offset + 0];
            Int32 green = data[offset + 1];
            Int32 red = data[offset + 2];
            Int32 alpha = allowTransparency ? data[offset + 3] : 255;
            return Color.FromArgb(alpha, red, green, blue);
        }

        public static void Write32BitColorToData(Color color, Byte[] data, Int32 offset, Boolean allowTransparency)
        {
            data[offset + 0] = (Byte)(allowTransparency ? color.A : 255);
            data[offset + 1] = (Byte)color.B;
            data[offset + 2] = (Byte)color.G;
            data[offset + 3] = (Byte)color.R;
        }

        /// <summary>
        /// Converts given raw image data for a paletted image to 8-bit, so we have a simple one-byte-per-pixel format to work with.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="start">Start offset of the image data in the fileData parameter.</param>
        /// <param name="bitsLength">Amount of bits used by one pixel.</param>
        /// <param name="stride">Stride used in the original image data.</param>
        /// <param name="bigEndian">True if the bits in the original image data are stored as big-endian.</param>
        /// <returns>The image data in a 1-byte-per-pixel format, with a stride exactly the same as the width.</returns>
        private static Byte[] ConvertTo8Bit(Byte[] fileData, Int32 width, Int32 height, Int32 start, Int32 bitsLength, Int32 stride, Boolean bigEndian)
        {
            if (bitsLength != 1 && bitsLength != 2 && bitsLength != 4 && bitsLength != 8)
                throw new IndexOutOfRangeException("Cannot handle image data with " + bitsLength + "bits per pixel.");
            // Full array
            Byte[] data8bit = new Byte[width * height];
            // Amount of runs that end up on the same pixel
            Int32 parts = 8 / bitsLength;
            // Bit mask for reducing read and shifted data to actual bits length
            Int32 bitmask = (1 << bitsLength) - 1;
            Int32 size = stride * height;
            // File check, and getting actual data.
            if (start + size > fileData.Length)
                throw new IndexOutOfRangeException(String.Format("Data exceeds file bounds!"));
            Byte[] curData = new Byte[size];
            Array.Copy(fileData, start, curData, 0, size);
            // Actual conversion porcess.
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // This will hit the same byte multiple times
                    Int32 indexXbit = y * stride + x / parts;
                    // This will always get a new index
                    Int32 index8bit = y * width + x;
                    // Amount of bits to shift the data to get to the current pixel data
                    Int32 shift = (x % parts) * bitsLength;
                    // Reversed for big-endian
                    if (bigEndian)
                        shift = 8 - shift - bitsLength;
                    // Get data and store it.
                    data8bit[index8bit] = (Byte)((curData[indexXbit] >> shift) & bitmask);
                }
            }
            return data8bit;
        }

        /// <summary>
        /// Loads an image without locking the underlying file.
        /// Code taken from http://stackoverflow.com/a/3661892/
        /// </summary>
        /// <param name="path">Path of the image to load</param>
        /// <returns>The image</returns>
        public static Bitmap LoadImageSafe(String path)
        {
            using (Bitmap sourceImage = (Bitmap)Image.FromFile(path))
            {
                return CloneImage(sourceImage);
            }
        }

        /// <summary>
        /// Clones an image object.
        /// Code taken from http://stackoverflow.com/a/3661892/ with some extra fixes.
        /// </summary>
        /// <param name="sourceImage">The image to clone</param>
        /// <returns>The cloned image</returns>
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
            return CloneImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);
        }

        /// <summary>
        /// Clones an image object.
        /// Code taken from http://stackoverflow.com/a/3661892/ with some extra fixes.
        /// </summary>
        /// <param name="sourceImage">The image to clone</param>
        /// <returns>The cloned image</returns>
        public static Bitmap CloneImage(Bitmap sourceImage, Int32 startX, Int32 startY, Int32 newWidth, Int32 newHeight)
        {
            if (startX + newWidth > sourceImage.Width || startY + newHeight > sourceImage.Height)
                throw new InvalidOperationException("Cutout size for image is larger than image!");

            Bitmap targetImage = new Bitmap(newWidth, newHeight, sourceImage.PixelFormat);
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(startX, startY, newWidth, newHeight), ImageLockMode.ReadOnly, sourceImage.PixelFormat);
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
            // For 8-bit images, set the palette.
            if ((pixelFormat == PixelFormat.Format8bppIndexed || pixelFormat == PixelFormat.Format4bppIndexed) && palette != null)
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
                Marshal.Copy(bytes, sourcePos, destPos, targetStride);
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


        public static Boolean HasTransparency(Bitmap bitmap)
        {
            // not an alpha-capable color format.
            if ((bitmap.Flags & (Int32)ImageFlags.HasAlpha) == 0)
                return false;
            // Indexed formats. Special case because one index on their palette is configured as THE transparent color.
            if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed || bitmap.PixelFormat == PixelFormat.Format4bppIndexed)
            {
                ColorPalette pal = bitmap.Palette;
                // Find the transparent indexea on the palette.
                List<Int32> transCols = new List<Int32>();
                for (int i = 0; i < pal.Entries.Length; i++)
                {
                    Color col = pal.Entries[i];
                    if (col.A != 255)
                    {
                        // Color palettes should only have one index acting as transparency. Not sure if there's a better way of getting it...
                        transCols.Add(i);
                        break;
                    }
                }
                // none of the entries in the palette have transparency information.
                if (transCols.Count == 0)
                    return false;
                // Check pixels for existence of the transparent index.
                Int32 colDepth = Image.GetPixelFormatSize(bitmap.PixelFormat);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                Int32 stride = data.Stride;
                Byte[] bytes = new Byte[bitmap.Height * stride];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                bitmap.UnlockBits(data);
                if (colDepth == 8)
                {
                    // Last line index.
                    Int32 lineMax = bitmap.Width - 1;
                    for (Int32 i = 0; i < bytes.Length; i++)
                    {
                        // Last position to process.
                        Int32 linepos = i % stride;
                        // Passed last image byte of the line. Abort and go on with loop.
                        if (linepos > lineMax)
                            continue;
                        Byte b = bytes[i];
                        if (transCols.Contains((Int32)b))
                            return true;
                    }
                }
                else if (colDepth == 4)
                {
                    // line size in bytes. 1-indexed for the moment.
                    Int32 lineMax = bitmap.Width / 2;
                    // Check if end of line ends on half a byte.
                    Boolean halfByte = bitmap.Width % 2 != 0;
                    // If it ends on half a byte, one more needs to be processed.
                    // We subtract in the other case instead, to make it 0-indexed right away.
                    if (!halfByte)
                        lineMax--;
                    for (Int32 i = 0; i < bytes.Length; i++)
                    {
                        // Last position to process.
                        Int32 linepos = i % stride;
                        // Passed last image byte of the line. Abort and go on with loop.
                        if (linepos > lineMax)
                            continue;
                        Byte b = bytes[i];
                        if (transCols.Contains((Int32)(b & 0x0F)))
                            return true;
                        if (halfByte && linepos == lineMax) // reached last byte of the line. If only half a byte to check on that, abort and go on with loop.
                            continue;
                        if (transCols.Contains((Int32)((b & 0xF0) >> 4)))
                            return true;
                    }
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

        public static Int32 GetMinStride(Int32 width, Int32 bitsLength)
        {
            // Amount of bytes to read per width
            Int32 stride = bitsLength * width;
            stride = (stride / 8) + ((stride % 8) > 0 ? 1 : 0);
            return stride;
        }

        public static Byte[] ConvertTo8Bit(Byte[] fileData, Int32 width, Int32 height, Int32 start, Int32 bitsLength, Boolean bigEndian, ref Int32 stride)
        {
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
                throw new Exception("Data exceeds array bounds!");
            // Actual conversion porcess.
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    // This will hit the same byte multiple times
                    Int32 indexXbit = start + y * stride + x / parts;
                    // This will always get a new index
                    Int32 index8bit = start + y * newStride + x;
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

        public static Byte[] ConvertFrom8Bit(Byte[] data8bit, Int32 width, Int32 height, Int32 bitsLength, Boolean bigEndian, ref Int32 stride)
        {
            Int32 parts = 8 / bitsLength;
            // Amount of bytes to write per width
            Int32 newStride = GetMinStride(width, bitsLength);
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
    }
}
