using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Graphics2d;
using Nyerguds.Util;

namespace Nyerguds.ImageManipulation
{
    public class DibHandler
    {

        /// <summary>
        /// Converts the image to Device Independent Bitmap format of type BI_BITFIELDS.
        /// This is (wrongly) accepted by many applications as containing transparency,
        /// so I'm abusing it for that.
        /// </summary>
        /// <param name="image">Image to convert to DIB.</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static byte[] ConvertToDib(Image image)
        {
            byte[] bm32bData;
            using (Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null))
            {
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                int stride;
                bm32bData = ImageUtils.GetImageData(bm32b, out stride);
            }
            BITMAPINFOHEADER hdr = new BITMAPINFOHEADER();
            int hdrSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            int bfSize = Marshal.SizeOf(typeof(BITFIELDS));
            hdr.biSize = (uint)hdrSize;
            hdr.biWidth = image.Width;
            hdr.biHeight = image.Height;
            hdr.biPlanes = 1;
            hdr.biBitCount = 32;
            hdr.biCompression = BITMAPCOMPRESSION.BI_BITFIELDS;
            hdr.biSizeImage = (uint)bm32bData.Length;
            hdr.biXPelsPerMeter = 0;
            hdr.biYPelsPerMeter = 0;
            hdr.biClrUsed = 0;
            hdr.biClrImportant = 0;

            BITFIELDS bf = new BITFIELDS();
            bf.bfRedMask = 0x00FF0000;
            bf.bfGreenMask = 0x0000FF00;
            bf.bfBlueMask = 0x000000FF;

            byte[] fullImage = new byte[hdrSize + 12 + bm32bData.Length];
            int writeOffs = 0;
            ArrayUtils.WriteStructToByteArray(hdr, fullImage, writeOffs, Endianness.LittleEndian);
            writeOffs += hdrSize;
            ArrayUtils.WriteStructToByteArray(bf, fullImage, writeOffs, Endianness.LittleEndian);
            writeOffs += bfSize;
            Array.Copy(bm32bData, 0, fullImage, writeOffs, bm32bData.Length);
            return fullImage;
        }

        /// <summary>
        /// Converts the image to Device Independent Bitmap format of version 5, of type BI_BITFIELDS.
        /// </summary>
        /// <param name="image">Image to convert to DIB.</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static byte[] ConvertToDib5(Image image)
        {
            int stride;
            byte[] bm32bData;
            using (Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null))
            {
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                bm32bData = ImageUtils.GetImageData(bm32b, out stride, PixelFormat.Format32bppArgb);
            }
            BITMAPV5HEADER hdr = new BITMAPV5HEADER();
            int hdrSize = Marshal.SizeOf(typeof (BITMAPV5HEADER));
            int bfSize = Marshal.SizeOf(typeof (BITFIELDS));
            hdr.bV5Size = (uint) hdrSize;
            hdr.bV5Width = image.Width;
            hdr.bV5Height = image.Height;
            hdr.bV5Planes = 1;
            hdr.bV5BitCount = 32;
            hdr.bV5Compression = BITMAPCOMPRESSION.BI_BITFIELDS;
            hdr.bV5SizeImage = (uint) bm32bData.Length;
            hdr.bV5XPelsPerMeter = 0;
            hdr.bV5YPelsPerMeter = 0;
            hdr.bV5ClrUsed = 0;
            hdr.bV5ClrImportant = 0;
            hdr.bV5RedMask = 0x00FF0000;
            hdr.bV5GreenMask = 0x0000FF00;
            hdr.bV5BlueMask = 0x000000FF;
            hdr.bV5AlphaMask = 0xFF000000;
            hdr.bV5CSType = LogicalColorSpace.LCS_sRGB;
            hdr.bV5Intent = GamutMappingIntent.LCS_GM_IMAGES;
            int fullSize = hdrSize + bm32bData.Length + bfSize;
            byte[] fullImage = new byte[fullSize];
            int writeOffs = 0;
            ArrayUtils.WriteStructToByteArray(hdr, fullImage, writeOffs, Endianness.LittleEndian);
            writeOffs += hdrSize;
            BITFIELDS bf = new BITFIELDS();
            bf.bfRedMask = 0x00FF0000;
            bf.bfGreenMask = 0x0000FF00;
            bf.bfBlueMask = 0x000000FF;
            ArrayUtils.WriteStructToByteArray(bf, fullImage, writeOffs, Endianness.LittleEndian);
            writeOffs += bfSize;
            Array.Copy(bm32bData, 0, fullImage, writeOffs, bm32bData.Length);
            return fullImage;
        }

        public static Bitmap ImageFromDib5(byte[] dibBytes, int offset, int length, int dataOffset, bool forceAlpha)
        {
            // Specs:
            // https://docs.microsoft.com/en-us/windows/desktop/api/wingdi/ns-wingdi-bitmapv5header
            // https://docs.microsoft.com/en-gb/windows/desktop/api/wingdi/ns-wingdi-tagbitmapinfo

            if (dibBytes == null || dibBytes.Length - offset < 4)
                return null;
            try
            {
                int headerSize = ArrayUtils.ReadInt32FromByteArrayLe(dibBytes, offset);
                // Only supporting 124-byte DIBV5 in this.
                // If it fails, try the other type ;)
                int dibHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                int dib5HeaderSize = Marshal.SizeOf(typeof(BITMAPV5HEADER));
                if (headerSize != dib5HeaderSize)
                {
                    if (headerSize == dibHeaderSize)
                        return ImageFromDib(dibBytes, offset, length, dataOffset, forceAlpha);
                    return null;
                }
                BITMAPV5HEADER dibHdr = ArrayUtils.ReadStructFromByteArray<BITMAPV5HEADER>(dibBytes, offset, Endianness.LittleEndian);
                // Not dealing with non-standard formats
                if (dibHdr.bV5Planes != 1 || (dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_RGB && dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return null;
                int imageIndex = dataOffset != 0 ? dataOffset : headerSize;
                int width = dibHdr.bV5Width;
                int height = dibHdr.bV5Height;
                int bitCount = dibHdr.bV5BitCount;
                int dataLen = dibBytes.Length - imageIndex;
                if (dibHdr.bV5Compression == BITMAPCOMPRESSION.BI_BITFIELDS && bitCount == 32)
                {
                    // Dumb specs; bitfields are saved twice. I'm just skipping this useless copy.
                    // Apparently this is not done for 16-bit images?
                    imageIndex += 12;
                    dataLen -= 12;
                }
                byte[] image = new byte[dataLen];
                Array.Copy(dibBytes, imageIndex, image, 0, image.Length);
                PixelFormat pf;
                uint redMask = dibHdr.bV5RedMask;
                uint greenMask = dibHdr.bV5GreenMask;
                uint blueMask = dibHdr.bV5BlueMask;
                uint alphaMask = dibHdr.bV5AlphaMask;
                if (forceAlpha)
                {
                    if (redMask == 0 && greenMask == 0 && blueMask == 0)
                    {
                        // Not sure if this case ever happens in DIBv5, tbh.
                        redMask = PixelFormatter.Format32BitArgbLe.BitMasks[PixelFormatter.ColR];
                        greenMask = PixelFormatter.Format32BitArgbLe.BitMasks[PixelFormatter.ColG];
                        blueMask = PixelFormatter.Format32BitArgbLe.BitMasks[PixelFormatter.ColB];
                        alphaMask = PixelFormatter.Format32BitArgbLe.BitMasks[PixelFormatter.ColA];
                    }
                    else
                    {
                        // If alpha is forced, generate alpha bit mask from all bits not in the Red/Green/Blue masks
                        alphaMask = ~(dibHdr.bV5RedMask | dibHdr.bV5GreenMask | dibHdr.bV5BlueMask);
                    }
                }
                image = ApplyBitMask(image, out pf, width, height, bitCount, alphaMask, redMask, greenMask, blueMask);
                int stride = ImageUtils.GetClassicStride(width, bitCount);
                if (pf == PixelFormat.Undefined)
                    return null;
                Bitmap bitmap = ImageUtils.BuildImage(image, width, height, stride, pf, null, null);
                // This is bmp; reverse image lines.
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap ImageFromDib(byte[] dibBytes, int offset, int length, bool detectArgb)
        {
            PixelFormat originalPixelFormat;
            return ImageFromDib(dibBytes, offset, length, 0, false, detectArgb, out originalPixelFormat);
        }

        public static Bitmap ImageFromDib(byte[] dibBytes, int offset, int length, int dataOffset, bool detectArgb)
        {
            PixelFormat originalPixelFormat;
            return ImageFromDib(dibBytes, offset, length, dataOffset, false, detectArgb, out originalPixelFormat);
        }

        public static Bitmap ImageFromDib(byte[] dibBytes, int offset, int lengthOverride, int dataOffset, bool detectIconFormat, bool detectArgb, out PixelFormat originalPixelFormat)
        {
            byte[] imageData;
            byte[] bitMask;
            Color[] palette;
            BITMAPINFOHEADER header;
            BITFIELDS bitfields;
            originalPixelFormat = PixelFormat.Undefined;
            if (!GetDataFromDib(dibBytes, offset, lengthOverride, dataOffset, detectIconFormat, out imageData, out bitfields, out bitMask, out palette, out header))
                return null;
            int width = header.biWidth;
            int height = header.biHeight;
            int stride = ImageUtils.GetClassicStride(width, header.biBitCount);

            Bitmap bitmap = null;
            originalPixelFormat = GetPixelFormat(header.biBitCount);
            // Icon handling
            bool isIcon = bitMask != null && bitMask.Length > 0;
            if (isIcon)
            {
                height /= 2;
                if (originalPixelFormat != PixelFormat.Format32bppRgb)
                {
                    int maskStride = ImageUtils.GetClassicStride(width, 1);
                    bool is24Bit = originalPixelFormat == PixelFormat.Format24bppRgb;
                    byte[] imageDataMask = is24Bit ? bitMask : ImageUtils.ConvertTo8Bit(bitMask, width, height, 0, 1, true, ref maskStride);
                    // For indexed, 0 in mask means no transparency.
                    if (!is24Bit)
                        for (int i = 0; i < imageDataMask.Length; ++i)
                            imageDataMask[i] = (byte)(imageDataMask[i] == 0 ? 255 : 0);
                    byte[] imageData32;
                    using (Bitmap indexedBm = ImageUtils.BuildImage(imageData, width, height, stride, originalPixelFormat, palette, Color.Black))
                        imageData32 = ImageUtils.GetImageData(indexedBm, out stride, PixelFormat.Format32bppArgb);
                    int inputOffsetLine = 0;
                    int outputOffsetLine = 0;
                    for (int y = 0; y < height; ++y)
                    {
                        int inputOffs = inputOffsetLine;
                        int outputOffs = outputOffsetLine;
                        // Apply alpha from mask.
                        for (int x = 0; x < width; ++x)
                        {
                            imageData32[outputOffs + 3] = imageDataMask[inputOffs];
                            inputOffs++;
                            outputOffs += 4;
                        }
                        inputOffsetLine += maskStride;
                        outputOffsetLine += stride;
                    }
                    bitmap = ImageUtils.BuildImage(imageData32, width, height, stride, PixelFormat.Format32bppArgb, palette, Color.Black);
                    // This is bmp; reverse image lines.
                    bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                }
            }
            if (bitmap != null)
                return bitmap;
            if (isIcon && originalPixelFormat == PixelFormat.Format32bppRgb && header.biCompression == BITMAPCOMPRESSION.BI_RGB)
            {
                // Icons support alpha when they are 32-bit.
                originalPixelFormat = PixelFormat.Format32bppArgb;
            }
            else if (detectArgb && originalPixelFormat == PixelFormat.Format32bppRgb && header.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS)
            {
                uint alphaMask = 0;
                // force mask to the remainder.
                if (bitfields.bfRedMask != 0 && bitfields.bfGreenMask != 0 && bitfields.bfBlueMask != 0)
                    alphaMask = ~(bitfields.bfRedMask | bitfields.bfGreenMask | bitfields.bfBlueMask);
                imageData = ApplyBitMask(imageData, out originalPixelFormat, width, height, header.biBitCount, alphaMask, bitfields.bfRedMask, bitfields.bfGreenMask, bitfields.bfBlueMask);
            }
            bitmap = ImageUtils.BuildImage(imageData, width, height, stride, originalPixelFormat, palette, Color.Black);
            // This is bmp; reverse image lines.
            bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
            return bitmap;
        }

        private static PixelFormat GetPixelFormat(int bitcount)
        {
            PixelFormat fmt;
            switch (bitcount)
            {
                case 32:
                    fmt = PixelFormat.Format32bppRgb;
                    break;
                case 24:
                    fmt = PixelFormat.Format24bppRgb;
                    break;
                case 16:
                    fmt = PixelFormat.Format16bppRgb555;
                    break;
                case 8:
                    fmt = PixelFormat.Format8bppIndexed;
                    break;
                case 4:
                    fmt = PixelFormat.Format4bppIndexed;
                    break;
                case 1:
                    fmt = PixelFormat.Format1bppIndexed;
                    break;
                default:
                    return PixelFormat.Undefined;
            }
            return fmt;
        }

        private static byte[] ApplyBitMask(byte[] image, out PixelFormat pf, int width, int height, int bitCount, uint alphaMask, uint redMask, uint greenMask, uint blueMask)
        {
            int stride = ImageUtils.GetClassicStride(width, bitCount);
            switch (bitCount)
            {
                case 32:
                    pf = PixelFormat.Format32bppRgb;
                    // Default
                    if (redMask == 0xFF0000 && greenMask == 0xFF00 && blueMask == 0xFF)
                    {
                        if (alphaMask == 0xFF000000)
                            pf = PixelFormat.Format32bppArgb;
                    }
                    else
                    {
                        pf = alphaMask != 0 ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
                        // Any kind of custom format can be handled here.
                        PixelFormatter pixFormatter = new PixelFormatter(4, alphaMask, redMask, greenMask, blueMask, true);
                        PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format32BitArgbLe, pixFormatter);
                    }
                    break;
                case 24:
                    pf = PixelFormat.Format24bppRgb;
                    if (redMask != 0xFF0000 || greenMask != 0xFF00 || blueMask != 0xFF)
                    {                        
                        PixelFormatter pixFormatter = new PixelFormatter(3, 0, redMask, greenMask, blueMask, true);
                        PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format24BitRgbLe, pixFormatter);
                    }
                    break;
                case 16:
                    if (alphaMask == 0 && redMask == 0 && greenMask == 0 && blueMask == 0)
                    {
                        // Not sure what the default is... or if this is even allowed.
                        pf = PixelFormat.Format16bppArgb1555;
                    }
                    else
                    {
                        if (redMask == 0x7C00 && greenMask == 0x03E0 && blueMask == 0x01F)
                        {
                            if (alphaMask == 0x8000)
                                pf = PixelFormat.Format16bppArgb1555;
                            else
                                pf = PixelFormat.Format16bppRgb555;
                        }
                        else if (redMask == 0xF800 && greenMask == 0x07E0 && blueMask == 0x01F)
                        {
                            pf = PixelFormat.Format16bppRgb565;
                        }
                        else
                        {
                            // Any kind of custom format can be handled here.
                            //UInt32 alphaMask = 0xFFFF & ~(redMask | greenMask | blueMask);
                            PixelFormatter pixFormatter = new PixelFormatter(2, alphaMask, redMask, greenMask, blueMask, true);
                            ReadOnlyCollection<byte> bits = pixFormatter.BitsAmounts;
                            if (bits[PixelFormatter.ColA] == 1 && bits[PixelFormatter.ColR] == 5 && bits[PixelFormatter.ColG] == 5 && bits[PixelFormatter.ColB] == 5)
                            {
                                PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitArgb1555Le, pixFormatter);
                                pf = PixelFormat.Format16bppArgb1555;
                            }
                            else if (bits[PixelFormatter.ColA] == 0 && bits[PixelFormatter.ColR] == 5 && bits[PixelFormatter.ColG] == 5 && bits[PixelFormatter.ColB] == 5)
                            {
                                PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitRgb555Le, pixFormatter);
                                pf = PixelFormat.Format16bppRgb555;
                            }
                            else if (bits[PixelFormatter.ColA] == 0 && bits[PixelFormatter.ColR] == 5 && bits[PixelFormatter.ColG] == 6 && bits[PixelFormatter.ColB] == 5)
                            {
                                PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitRgb565Le, pixFormatter);
                                pf = PixelFormat.Format16bppRgb565;
                            }
                            else
                                pf = PixelFormat.Undefined;
                        }
                    }
                    break;
                default:
                    pf = GetPixelFormat(bitCount);
                    break;
            }
            return image;
        }


        public static bool GetDataFromDib(byte[] dibBytes, int offset, int length, int dataOffsetOverride, bool detectIconFormat, out byte[] imageData, out BITFIELDS bitFields, out byte[] bitMask, out Color[] palette, out BITMAPINFOHEADER header)
        {
            if (length == 0)
            {
                length = dibBytes.Length - offset;
            }
            uint readEnd = (uint)(length + offset);
            imageData = null;
            bitMask = null;
            palette = null;
            header = new BITMAPINFOHEADER();
            bitFields = new BITFIELDS();
            if (dibBytes == null || dibBytes.Length - offset < 4 || length < 4)
                return false;
            try
            {
                int headerSize = ArrayUtils.ReadInt32FromByteArrayLe(dibBytes, offset);
                int dibHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                int bitFieldsSize = Marshal.SizeOf(typeof(BITFIELDS));
                if (dibHeaderSize != headerSize)
                    return false;
                header = ArrayUtils.ReadStructFromByteArray<BITMAPINFOHEADER>(dibBytes, offset, Endianness.LittleEndian);
                // No support for dealing with multiplanar or compressed formats yet.
                if (header.biPlanes != 1 || (header.biCompression != BITMAPCOMPRESSION.BI_RGB && header.biCompression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return false;
                int readIndex = headerSize + offset;
                int width = header.biWidth;
                int height = header.biHeight;
                int bitCount = header.biBitCount;
                uint imageSize = header.biSizeImage;
                if (dibBytes.Length < readIndex || readEnd < readIndex)
                    return false;
                if (header.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS)
                {
                    bitFields = ArrayUtils.ReadStructFromByteArray<BITFIELDS>(dibBytes, readIndex, Endianness.LittleEndian);
                    readIndex += bitFieldsSize;
                    if (dibBytes.Length < readIndex || readEnd < readIndex)
                        return false;
                }
                int paletteLength = bitCount > 8 ? 0 : (int)header.biClrUsed;
                if (paletteLength == 0 && bitCount <= 8)
                    paletteLength = 1 << bitCount;
                palette = new Color[paletteLength];
                int palEnd = readIndex + paletteLength * 4;
                if (dibBytes.Length < palEnd || readEnd < palEnd)
                    return false;
                if (paletteLength > 0)
                {
                    for (int i = 0; i < paletteLength; ++i)
                    {
                        palette[i] = Color.FromArgb(dibBytes[readIndex + 2], dibBytes[readIndex + 1], dibBytes[readIndex]);
                        readIndex += 4;
                    }
                }
                if (imageSize == 0)
                {
                    // This seems to happen? Just take the length minus the current read offset; that should match.
                    imageSize = (uint)Math.Max(0, readEnd - readIndex);
                }
                int stride = ImageUtils.GetClassicStride(width, bitCount);
                int maskSize = 0;
                if (height % 2 == 0 && detectIconFormat)
                {
                    int actualReadSize = (int)readEnd - readIndex;
                    int halfHeight = height / 2;
                    // I think mask is always just single-bit OR.
                    int maskStride = ImageUtils.GetClassicStride(width, 1);
                    int maskSizeCheck = maskStride * halfHeight;
                    int imgSizeDiff = (int)imageSize - stride * halfHeight;
                    int sizeWithMask = (int)imageSize + maskSizeCheck;
                    if (imgSizeDiff == maskSizeCheck || (imgSizeDiff == 0 && actualReadSize == sizeWithMask))
                    {
                        height = halfHeight;
                        maskSize = maskSizeCheck;
                    }
                    else if (bitCount == 24)
                    {
                        // 8-bit 'mask' on 24bpp just containing alpha? Unsure if this exists.
                        maskStride = ImageUtils.GetClassicStride(width, 8);
                        maskSizeCheck = maskStride * halfHeight;
                        if (imgSizeDiff == maskSizeCheck || (imgSizeDiff == 0 && actualReadSize == sizeWithMask))
                        {
                            height = halfHeight;
                            maskSize = maskSizeCheck;
                        }
                    }
                }
                if (dataOffsetOverride != 0)
                    readIndex = dataOffsetOverride;
                int dataLen = stride * height;
                int fullLen = dataLen + maskSize;
                if (dibBytes.Length - readIndex < fullLen || readEnd - readIndex < fullLen)
                    return false;
                imageData = new byte[dataLen];
                Array.Copy(dibBytes, readIndex, imageData, 0, dataLen);
                readIndex += dataLen;
                // Icon stuff only.
                if (maskSize == 0)
                    return true;
                bitMask = new byte[maskSize];
                Array.Copy(dibBytes, readIndex, bitMask, 0, maskSize);
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
