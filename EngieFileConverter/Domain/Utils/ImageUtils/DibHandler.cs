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
        public static Byte[] ConvertToDib(Image image)
        {
            Byte[] bm32bData;
            using (Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null))
            {
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                Int32 stride;
                bm32bData = ImageUtils.GetImageData(bm32b, out stride);
            }
            BITMAPINFOHEADER hdr = new BITMAPINFOHEADER();
            Int32 hdrSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            Int32 bfSize = Marshal.SizeOf(typeof(BITFIELDS));
            hdr.biSize = (UInt32)hdrSize;
            hdr.biWidth = image.Width;
            hdr.biHeight = image.Height;
            hdr.biPlanes = 1;
            hdr.biBitCount = 32;
            hdr.biCompression = BITMAPCOMPRESSION.BI_BITFIELDS;
            hdr.biSizeImage = (UInt32)bm32bData.Length;
            hdr.biXPelsPerMeter = 0;
            hdr.biYPelsPerMeter = 0;
            hdr.biClrUsed = 0;
            hdr.biClrImportant = 0;

            BITFIELDS bf = new BITFIELDS();
            bf.bfRedMask = 0x00FF0000;
            bf.bfGreenMask = 0x0000FF00;
            bf.bfBlueMask = 0x000000FF;

            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            Int32 writeOffs = 0;
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
        public static Byte[] ConvertToDib5(Image image)
        {
            Int32 stride;
            Byte[] bm32bData;
            using (Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null))
            {
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                bm32bData = ImageUtils.GetImageData(bm32b, out stride, PixelFormat.Format32bppArgb);
            }
            BITMAPV5HEADER hdr = new BITMAPV5HEADER();
            Int32 hdrSize = Marshal.SizeOf(typeof (BITMAPV5HEADER));
            Int32 bfSize = Marshal.SizeOf(typeof (BITFIELDS));
            hdr.bV5Size = (UInt32) hdrSize;
            hdr.bV5Width = image.Width;
            hdr.bV5Height = image.Height;
            hdr.bV5Planes = 1;
            hdr.bV5BitCount = 32;
            hdr.bV5Compression = BITMAPCOMPRESSION.BI_BITFIELDS;
            hdr.bV5SizeImage = (UInt32) bm32bData.Length;
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
            Int32 fullSize = hdrSize + bm32bData.Length + bfSize;
            Byte[] fullImage = new Byte[fullSize];
            Int32 writeOffs = 0;
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

        public static Bitmap ImageFromDib5(Byte[] dibBytes, Int32 offset, Int32 dataOffset, Boolean forceTransBf)
        {
            // Specs:
            // https://docs.microsoft.com/en-us/windows/desktop/api/wingdi/ns-wingdi-bitmapv5header
            // https://docs.microsoft.com/en-gb/windows/desktop/api/wingdi/ns-wingdi-tagbitmapinfo

            if (dibBytes == null || dibBytes.Length - offset < 4)
                return null;
            try
            {
                Int32 headerSize = ArrayUtils.ReadInt32FromByteArrayLe(dibBytes, offset);
                // Only supporting 124-byte DIBV5 in this.
                // If it fails, try the other type ;)
                Int32 dibHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                Int32 dib5HeaderSize = Marshal.SizeOf(typeof(BITMAPV5HEADER));
                if (headerSize != dib5HeaderSize)
                {
                    if (headerSize == dibHeaderSize)
                        return ImageFromDib(dibBytes, offset, dataOffset);
                    return null;
                }
                BITMAPV5HEADER dibHdr = ArrayUtils.ReadStructFromByteArray<BITMAPV5HEADER>(dibBytes, offset, Endianness.LittleEndian);
                // Not dealing with non-standard formats
                if (dibHdr.bV5Planes != 1 || (dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_RGB && dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return null;
                Int32 imageIndex = dataOffset != 0 ? dataOffset : headerSize;
                Int32 width = dibHdr.bV5Width;
                Int32 height = dibHdr.bV5Height;
                Int32 bitCount = dibHdr.bV5BitCount;
                Int32 dataLen = dibBytes.Length - imageIndex;
                if (dibHdr.bV5Compression == BITMAPCOMPRESSION.BI_BITFIELDS && bitCount == 32)
                {
                    // Dumb specs; bitfields are saved twice. I'm just skipping this useless copy.
                    // Apparently this is not done for 16-bit images?
                    imageIndex += 12;
                    dataLen -= 12;
                }
                Byte[] image = new Byte[dataLen];
                Array.Copy(dibBytes, imageIndex, image, 0, image.Length);
                PixelFormat pf;
                UInt32 redMask = dibHdr.bV5RedMask;
                UInt32 greenMask = dibHdr.bV5GreenMask;
                UInt32 blueMask = dibHdr.bV5BlueMask;
                UInt32 alphaMask = dibHdr.bV5AlphaMask;
                if (forceTransBf)
                {
                    if (redMask == 0 && greenMask == 0 && blueMask == 0)
                    {
                        // Not sure if this case ever happens in DIBv5, tbh.
                        redMask = PixelFormatter.Format32BitArgb.BitMasks[PixelFormatter.ColR];
                        greenMask = PixelFormatter.Format32BitArgb.BitMasks[PixelFormatter.ColG];
                        blueMask = PixelFormatter.Format32BitArgb.BitMasks[PixelFormatter.ColB];
                        alphaMask = PixelFormatter.Format32BitArgb.BitMasks[PixelFormatter.ColA];
                    }
                    else
                    {
                        // If alpha is forced, generate alpha bit mask from all bits not in the Red/Green/Blue masks
                        alphaMask = ~(dibHdr.bV5RedMask | dibHdr.bV5GreenMask | dibHdr.bV5BlueMask);
                    }
                }
                image = ApplyBitMask(image, out pf, width, height, bitCount, alphaMask, redMask, greenMask, blueMask);
                Int32 stride = ImageUtils.GetClassicStride(width, bitCount);
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

        public static Bitmap ImageFromDib(Byte[] dibBytes, Int32 offset)
        {
            PixelFormat originalPixelFormat;
            return ImageFromDib(dibBytes, offset, 0, false, out originalPixelFormat);
        }

        public static Bitmap ImageFromDib(Byte[] dibBytes, Int32 offset, Int32 dataOffset)
        {
            PixelFormat originalPixelFormat;
            return ImageFromDib(dibBytes, offset, dataOffset, false, out originalPixelFormat);
        }

        public static Bitmap ImageFromDib(Byte[] dibBytes, Int32 offset, Int32 dataOffset, Boolean detectIconFormat, out PixelFormat originalPixelFormat)
        {
            Byte[] imageData;
            Byte[] bitMask;
            Color[] palette;
            BITMAPINFOHEADER header;
            BITFIELDS bitfields;
            originalPixelFormat = PixelFormat.Undefined;
            if (!GetDataFromDib(dibBytes, offset, dataOffset, detectIconFormat, out imageData, out bitfields, out bitMask, out palette, out header))
                return null;
            Int32 width = header.biWidth;
            Int32 height = header.biHeight;
            Int32 stride = ImageUtils.GetClassicStride(width, header.biBitCount);
            
            Bitmap bitmap = null;
            // Icon handling
            Boolean isIcon = bitMask != null && bitMask.Length > 0;
            if (isIcon)
            {
                originalPixelFormat = GetPixelFormat(header.biBitCount);
                height /= 2;
                if (originalPixelFormat != PixelFormat.Format32bppRgb)
                {
                    Int32 maskStride = ImageUtils.GetClassicStride(width, 1);
                    Byte[] imageDataMask = ImageUtils.ConvertTo8Bit(bitMask, width, height, 0, 1, true, ref maskStride);
                    Byte[] imageData32;
                    using (Bitmap indexedBm = ImageUtils.BuildImage(imageData, width, height, stride, originalPixelFormat, palette, Color.Black))
                        imageData32 = ImageUtils.GetImageData(indexedBm, out stride, PixelFormat.Format32bppArgb);
                    Int32 inputOffsetLine = 0;
                    Int32 outputOffsetLine = 0;
                    for (Int32 y = 0; y < height; ++y)
                    {
                        Int32 inputOffs = inputOffsetLine;
                        Int32 outputOffs = outputOffsetLine;
                        // Apply alpha from mask.
                        for (Int32 x = 0; x < width; ++x)
                        {
                            // 0 in mask means no transparency.
                            imageData32[outputOffs + 3] = (Byte)(imageDataMask[inputOffs] == 0 ? 255 : 0);
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
            else
            {
                UInt32 alphaMask = 0;
                // If icon: force mask to the remainder.
                if (isIcon && header.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS && bitfields.bfRedMask != 0 && bitfields.bfGreenMask != 0 && bitfields.bfBlueMask != 0)
                    alphaMask = ~(bitfields.bfRedMask | bitfields.bfGreenMask | bitfields.bfBlueMask);
                imageData = ApplyBitMask(imageData, out originalPixelFormat, width, height, header.biBitCount, alphaMask, bitfields.bfRedMask, bitfields.bfGreenMask, bitfields.bfBlueMask);
            }
            bitmap = ImageUtils.BuildImage(imageData, width, height, stride, originalPixelFormat, palette, Color.Black);
            // This is bmp; reverse image lines.
            bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
            return bitmap;
        }

        private static PixelFormat GetPixelFormat(Int32 bitcount)
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

        private static Byte[] ApplyBitMask(Byte[] image, out PixelFormat pf, Int32 width, Int32 height, Int32 bitCount, UInt32 alphaMask, UInt32 redMask, UInt32 greenMask, UInt32 blueMask)
        {
            Int32 stride = ImageUtils.GetClassicStride(width, bitCount);
            switch (bitCount)
            {
                case 32:
                    if (alphaMask == 0 && redMask == 0 && greenMask == 0 && blueMask == 0)
                    {
                        pf = PixelFormat.Format32bppRgb;
                    }
                    else
                    {
                        pf = alphaMask != 0 ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
                        // Any kind of custom format can be handled here.
                        PixelFormatter pixFormatter = new PixelFormatter(4, alphaMask, redMask, greenMask, blueMask, true);
                        PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format32BitArgb, pixFormatter);
                    }
                    break;
                case 24:
                    pf = PixelFormat.Format24bppRgb;
                    if (redMask != 0 || greenMask != 0 || blueMask != 0)
                    {
                        PixelFormatter pixFormatter = new PixelFormatter(3, 0, redMask, greenMask, blueMask, true);
                        PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format24BitRgb, pixFormatter);
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
                            ReadOnlyCollection<Byte> bits = pixFormatter.BitsAmounts;
                            if (bits[PixelFormatter.ColA] == 1 && bits[PixelFormatter.ColR] == 5 && bits[PixelFormatter.ColG] == 5 && bits[PixelFormatter.ColB] == 5)
                            {
                                PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitArgb1555, pixFormatter);
                                pf = PixelFormat.Format16bppArgb1555;
                            }
                            else if (bits[PixelFormatter.ColA] == 0 && bits[PixelFormatter.ColR] == 5 && bits[PixelFormatter.ColG] == 5 && bits[PixelFormatter.ColB] == 5)
                            {
                                PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitRgb555, pixFormatter);
                                pf = PixelFormat.Format16bppRgb555;
                            }
                            else if (bits[PixelFormatter.ColA] == 0 && bits[PixelFormatter.ColR] == 5 && bits[PixelFormatter.ColG] == 6 && bits[PixelFormatter.ColB] == 5)
                            {
                                PixelFormatter.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitRgb565, pixFormatter);
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


        public static Boolean GetDataFromDib(Byte[] dibBytes, Int32 offset, Int32 dataOffsetOverride, Boolean detectIconFormat, out Byte[] imageData, out BITFIELDS bitFields, out Byte[] bitMask, out Color[] palette, out BITMAPINFOHEADER header)
        {
            imageData = null;
            bitMask = null;
            palette = null;
            header = new BITMAPINFOHEADER();
            bitFields = new BITFIELDS();
            if (dibBytes == null || dibBytes.Length - offset < 4)
                return false;
            try
            {
                Int32 headerSize = ArrayUtils.ReadInt32FromByteArrayLe(dibBytes, offset);
                Int32 dibHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                Int32 bitFieldsSize = Marshal.SizeOf(typeof(BITFIELDS));
                if (dibHeaderSize != headerSize)
                    return false;
                header = ArrayUtils.ReadStructFromByteArray<BITMAPINFOHEADER>(dibBytes, offset, Endianness.LittleEndian);
                // Not dealing with non-standard formats
                if (header.biPlanes != 1 || (header.biCompression != BITMAPCOMPRESSION.BI_RGB && header.biCompression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return false;
                Int32 readIndex = headerSize + offset;
                Int32 width = header.biWidth;
                Int32 height = header.biHeight;
                Int32 bitCount = header.biBitCount;
                if (dibBytes.Length < readIndex)
                    return false;
                if (header.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS)
                {
                    bitFields = ArrayUtils.ReadStructFromByteArray<BITFIELDS>(dibBytes, readIndex, Endianness.LittleEndian);
                    readIndex += bitFieldsSize;
                    if (dibBytes.Length < readIndex)
                        return false;
                }
                //else if (header.biCompression == BITMAPCOMPRESSION.BI_RGB)
                //{
                //    // encountered some format that pads 4 bytes here... not sure if standard.
                //    readIndex += 4;
                //}
                Int32 paletteLength = bitCount > 8 ? 0 : (Int32)header.biClrUsed;
                if (paletteLength == 0 && bitCount <= 8)
                    paletteLength = 1 << bitCount;
                palette = new Color[paletteLength];
                if (dibBytes.Length < readIndex + paletteLength * 4)
                    return false;
                if (paletteLength > 0)
                {
                    for (Int32 i = 0; i < paletteLength; ++i)
                    {
                        palette[i] = Color.FromArgb(dibBytes[readIndex + 2], dibBytes[readIndex + 1], dibBytes[readIndex]);
                        readIndex += 4;
                    }
                }
                Int32 stride = ImageUtils.GetClassicStride(width, bitCount);
                Int32 maskSize = 0;
                if (height % 2 == 0 && detectIconFormat)
                {
                    Int32 halfHeight = height / 2;
                    Int32 remainingSize = dibBytes.Length - readIndex;
                    Int32 maskStride = ImageUtils.GetClassicStride(width, 1);
                    if (remainingSize - stride * halfHeight == maskStride * halfHeight)
                    {
                        height = height / 2;
                        maskSize = maskStride * height;
                    }
                }
                if (dataOffsetOverride != 0)
                    readIndex = dataOffsetOverride;
                Int32 dataLen = stride * height;
                if (dibBytes.Length - readIndex < dataLen + maskSize)
                    return false;
                imageData = new Byte[dataLen];
                Array.Copy(dibBytes, readIndex, imageData, 0, dataLen);
                readIndex += dataLen;
                // Icon stuff only.
                if (maskSize == 0)
                    return true;
                bitMask = new Byte[maskSize];
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
