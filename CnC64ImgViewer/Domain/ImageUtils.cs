using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace ColorManipulation
{
    public static class ImageUtils
    {

        public static Color GetVisibleBorderColor(Color color)
        {
            float sat = color.GetSaturation();
            float bri = color.GetBrightness();
            if (color.GetSaturation() < .16)
            {
                // this color is grey
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
            if (!depth.HasValue)
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

        private static Byte[] ConvertToBytes(Color[] colorData, Bitmap sourceImage, Int32? depth)
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

            Byte[] newBytes = new Byte[colorData.Length * byteCount];
            for (Int32 i = 0; i < colorData.Length; i++)
            {
                Int32 pos = i * byteCount;
                Color clr = colorData[i];

                // Get start index of the specified pixel

                if (depth == 32) // For 32 bpp: get Red, Green, Blue and Alpha
                {
                    newBytes[pos] = clr.B;
                    newBytes[pos + 1] = clr.G;
                    newBytes[pos + 2] = clr.R;
                    newBytes[pos + 3] = clr.A;
                }
                else if (depth == 24) // For 24 bpp: get Red, Green and Blue
                {
                    newBytes[pos] = clr.B;
                    newBytes[pos + 1] = clr.G;
                    newBytes[pos + 2] = clr.R;
                }
            }
            return newBytes;
        }

        public static Int32 GetPixelFormatSize(Bitmap image)
        {
            return Image.GetPixelFormatSize(image.PixelFormat);
        }

        public static void SaveImage(Bitmap image, String filename)
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

            if (saveFormat == ImageFormat.Jpeg)
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
                image.Save(filename, jpegEncoder, encparams);
            }
            else
                image.Save(filename, saveFormat);
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
        public static Bitmap CloneImage(Bitmap sourceImage)
        {
            Bitmap targetImage = new Bitmap(sourceImage.Width, sourceImage.Height, sourceImage.PixelFormat);
            BitmapData sourceData = sourceImage.LockBits(
                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            BitmapData targetData = targetImage.LockBits(
                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                ImageLockMode.WriteOnly, targetImage.PixelFormat);

            CopyMemory(targetData.Scan0, sourceData.Scan0, sourceData.Stride * sourceData.Height, 1024, 1024);

            sourceImage.UnlockBits(sourceData);
            targetImage.UnlockBits(targetData);
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
        /// <param name="pixelFormat"></param>
        /// <param name="palette"></param>
        /// <returns>The new image</returns>
        public static Bitmap BuildImage(Byte[] sourceData, Int32 width, Int32 height, Int32 stride, PixelFormat pixelFormat, ColorPalette palette)
        {
            Bitmap newImage = new Bitmap(width, height, pixelFormat);
            BitmapData targetData = newImage.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, newImage.PixelFormat);

            CopyMemory(targetData.Scan0, sourceData, sourceData.Length, stride, targetData.Stride);
            newImage.UnlockBits(targetData);
            // For 8-bit images, set the palette.
            if ((pixelFormat == PixelFormat.Format8bppIndexed || pixelFormat == PixelFormat.Format4bppIndexed) && palette != null)
                newImage.Palette = palette;
            return newImage;
        }

        public static void CopyMemory(IntPtr target, Byte[] bytes, Int32 length, Int32 origStride, Int32 targetStride)
        {
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            CopyMemory(target, unmanagedPointer, length, origStride, targetStride);
            Marshal.FreeHGlobal(unmanagedPointer);
        }

        public static void CopyMemory(IntPtr target, IntPtr source, Int32 length, Int32 origStride, Int32 targetStride)
        {
            IntPtr sourcePos = source;
            IntPtr destPos = target;
            Int32 minStride = Math.Min(origStride, targetStride);
            Byte[] imageData = new Byte[targetStride];
            while (length > targetStride)
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
    }
}
