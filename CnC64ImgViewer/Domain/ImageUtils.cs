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
    }
}
