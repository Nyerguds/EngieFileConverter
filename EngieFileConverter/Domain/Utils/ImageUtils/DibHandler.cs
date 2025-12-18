using System;
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
            Int32 stride;
            Byte[] bm32bData;
            using (Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null))
            {
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
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
            ArrayUtils.WriteStructToByteArray(hdr, fullImage, writeOffs);
            writeOffs += hdrSize;
            ArrayUtils.WriteStructToByteArray(bf, fullImage, writeOffs);
            writeOffs += bfSize;
            Array.Copy(bm32bData, 0, fullImage, writeOffs, bm32bData.Length);
            return fullImage;
        }

        /// <summary>
        /// Converts the image to Device Independent Bitmap format of version 5, of type BI_BITFIELDS.
        /// </summary>
        /// <param name="image">Image to convert to DIB.</param>
        /// <param name="addExtraBitfields">True to add extra bit fields part to the header (normally not done, but Chrome does it).</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static Byte[] ConvertToDib5(Image image, Boolean addExtraBitfields)
        {
            Int32 stride;
            Byte[] bm32bData;
            using(Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null))
            {
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                bm32bData = ImageUtils.GetImageData(bm32b, out stride, PixelFormat.Format32bppArgb);
            }
            BITMAPV5HEADER hdr = new BITMAPV5HEADER();
            Int32 hdrSize = Marshal.SizeOf(typeof(BITMAPV5HEADER));
            Int32 bfSize = Marshal.SizeOf(typeof(BITFIELDS));
            hdr.bV5Size = (UInt32)hdrSize;
            hdr.bV5Width = image.Width;
            hdr.bV5Height = image.Height;
            hdr.bV5Planes = 1;
            hdr.bV5BitCount = 32;
            hdr.bV5Compression = BITMAPCOMPRESSION.BI_BITFIELDS;
            hdr.bV5SizeImage = (UInt32)bm32bData.Length;
            hdr.bV5XPelsPerMeter = 0;
            hdr.bV5YPelsPerMeter = 0;
            hdr.bV5ClrUsed = 0;
            hdr.bV5ClrImportant = 0;
            hdr.bV5RedMask = 0x00FF0000;
            hdr.bV5GreenMask = 0x0000FF00;
            hdr.bV5BlueMask = 0x000000FF;
            hdr.bV5AlphaMask = 0xFF000000;
            hdr.bV5CSType = LogicalColorSpace.LCS_sRGB;
            hdr.bV5Intent = GamutMappingIntent.LCS_GM_GRAPHICS;
            Int32 fullSize = hdrSize + bm32bData.Length;
            if (addExtraBitfields)
                fullSize += bfSize;
            Byte[] fullImage = new Byte[fullSize];
            Int32 writeOffs = 0;
            ArrayUtils.WriteStructToByteArray(hdr, fullImage, writeOffs);
            writeOffs += hdrSize;
            if (addExtraBitfields)
            {
                BITFIELDS bf = new BITFIELDS();
                bf.bfRedMask = 0x00FF0000;
                bf.bfGreenMask = 0x0000FF00;
                bf.bfBlueMask = 0x000000FF;
                ArrayUtils.WriteStructToByteArray(bf, fullImage, writeOffs);
                writeOffs += bfSize;
            }
            Array.Copy(bm32bData, 0, fullImage, writeOffs, bm32bData.Length);
            return fullImage;
        }
        
        public static Bitmap ImageFromDib5(Byte[] dibBytes)
        {
            if (dibBytes == null || dibBytes.Length < 4)
                return null;
            try
            {
                Int32 headerSize = (Int32)ArrayUtils.ReadIntFromByteArray(dibBytes, 0, 4, true);
                // Only supporting 124-byte DIBV5 in this.
                // If it fails, try the other type ;)
                Int32 dibHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                Int32 dib5HeaderSize = Marshal.SizeOf(typeof(BITMAPV5HEADER));
                if (headerSize != dib5HeaderSize)
                {
                    if (headerSize == dibHeaderSize)
                        return ImageFromDib(dibBytes);
                    return null;
                }
                Byte[] header = new Byte[headerSize];
                Array.Copy(dibBytes, header, headerSize);
                BITMAPV5HEADER dibHdr = ArrayUtils.StructFromByteArray<BITMAPV5HEADER>(header);
                // Not dealing with non-standard formats
                if (dibHdr.bV5Planes != 1 || (dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_RGB && dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return null;
                Int32 imageIndex = headerSize;
                Int32 width = dibHdr.bV5Width;
                Int32 height = dibHdr.bV5Height;
                Int32 bitCount = dibHdr.bV5BitCount;
                PixelFormat pf = PixelFormat.Undefined;
                Int32 dataLen = dibBytes.Length - imageIndex;
                // Detect BI_BITFIELDS idiocy applied to DIB5. No, I'm not even kidding... Chrome does this.
                if (dibHdr.bV5Compression == BITMAPCOMPRESSION.BI_BITFIELDS
                    && dataLen - width * height * 4 == 12
                    && ArrayUtils.ReadIntFromByteArray(dibBytes, imageIndex + 0, 4, true) == 0x00FF0000
                    && ArrayUtils.ReadIntFromByteArray(dibBytes, imageIndex + 4, 4, true) == 0x0000FF00
                    && ArrayUtils.ReadIntFromByteArray(dibBytes, imageIndex + 8, 4, true) == 0x000000FF)
                {
                    // They even abuse DIB5 RGB as ARGB with added indices. The bitfields are already in the header, morons.
                    imageIndex += 12;
                    dataLen -= 12;
                    pf = PixelFormat.Format32bppArgb;
                }
                Byte[] image = new Byte[dataLen];
                Array.Copy(dibBytes, imageIndex, image, 0, image.Length);
                Int32 stride = ImageUtils.GetClassicStride(width, bitCount);
                if (pf == PixelFormat.Undefined)
                {
                    switch (bitCount)
                    {
                        case 32:
                            if (dibHdr.bV5Compression == BITMAPCOMPRESSION.BI_BITFIELDS)
                            {
                                pf = dibHdr.bV5AlphaMask == 0 ? PixelFormat.Format32bppRgb : PixelFormat.Format32bppArgb;
                                if (dibHdr.bV5RedMask != 0x00FF0000 || dibHdr.bV5GreenMask != 0x0000FF00 || dibHdr.bV5BlueMask != 0x000000FF)
                                {
                                    // Any kind of custom format can be handled here.
                                    PixelFormatter pixFormatter = new PixelFormatter(2, dibHdr.bV5RedMask, dibHdr.bV5GreenMask, dibHdr.bV5BlueMask, dibHdr.bV5AlphaMask, true);
                                    ImageUtils.ReorderBits(image, width, height, stride, PixelFormatter.Format32BitArgb, pixFormatter);
                                }
                            }
                            else
                                pf = PixelFormat.Format32bppRgb;
                            break;
                        case 24:
                            pf = PixelFormat.Format24bppRgb;
                            break;
                        case 16:
                            if (dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_BITFIELDS)
                            {
                                // Not sure what the default is...
                                pf = PixelFormat.Format16bppArgb1555;
                            }
                            else
                            {
                                if (dibHdr.bV5RedMask == 0x7C00 && dibHdr.bV5GreenMask == 0x03E0 && dibHdr.bV5BlueMask == 0x01F)
                                {
                                    if (dibHdr.bV5AlphaMask == 0x0000)
                                        pf = PixelFormat.Format16bppRgb555;
                                    else if (dibHdr.bV5AlphaMask == 0x8000)
                                        pf = PixelFormat.Format16bppArgb1555;
                                }
                                else if (dibHdr.bV5RedMask == 0xF800 && dibHdr.bV5GreenMask == 0x07E0 && dibHdr.bV5BlueMask == 0x01F)
                                {
                                    pf = PixelFormat.Format16bppRgb565;
                                }
                                else
                                {
                                    // Any kind of custom format can be handled here.
                                    pf = PixelFormat.Format16bppArgb1555;
                                    //UInt32 alphaMask = 0xFFFF & ~(dibHdr.bV5RedMask | dibHdr.bV5GreenMask | dibHdr.bV5BlueMask);
                                    PixelFormatter pixFormatter = new PixelFormatter(2, dibHdr.bV5RedMask, dibHdr.bV5GreenMask, dibHdr.bV5BlueMask, dibHdr.bV5AlphaMask, true);
                                    ImageUtils.ReorderBits(image, width, height, stride, PixelFormatter.Format16BitArgb1555, pixFormatter);
                                }
                            }
                            break;
                        default:
                            return null;
                    }
                }
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

        public static Bitmap ImageFromDib(Byte[] dibBytes) 
        {
            PixelFormat originalPixelFormat;
            return ImageFromDib(dibBytes, false, out originalPixelFormat);
        }

        public static Bitmap ImageFromDib(Byte[] dibBytes, Boolean detectIconFormat, out PixelFormat originalPixelFormat)
        {
            Byte[] imageData;
            Byte[] bitMask;
            Color[] palette;
            BITMAPINFOHEADER header;
            BITFIELDS bitfields;
            originalPixelFormat = PixelFormat.Undefined;
            if (!GetDataFromDib(dibBytes, detectIconFormat, out imageData, out bitfields, out bitMask, out palette, out header))
                return null;
            Int32 width = header.biWidth;
            Int32 height = header.biHeight;
            Int32 stride = ImageUtils.GetClassicStride(width, header.biBitCount);
            originalPixelFormat = GetPixelFormat(header);
            Bitmap bitmap = null;
            if (bitMask != null)
            {
                height /= 2;
                if (originalPixelFormat == PixelFormat.Format32bppRgb)
                {
                    // Icons support transparency when they are 32-bit
                    originalPixelFormat = PixelFormat.Format32bppArgb;
                }
                else
                {
                    Int32 maskStride = ImageUtils.GetClassicStride(width, 1);
                    Byte[] imageDataMask = ImageUtils.ConvertTo8Bit(bitMask, width, height, 0, 1, true, ref maskStride);
                    Byte[] imageData32;
                    using (Bitmap indexedBm = ImageUtils.BuildImage(imageData, width, height, stride, originalPixelFormat, palette, Color.Black))
                        imageData32 = ImageUtils.GetImageData(indexedBm, out stride, PixelFormat.Format32bppArgb);
                    Int32 inputOffsetLine = 0;
                    Int32 outputOffsetLine = 0;
                    for (Int32 y = 0; y < height; y++)
                    {
                        Int32 inputOffs = inputOffsetLine;
                        Int32 outputOffs = outputOffsetLine;
                        for (Int32 x = 0; x < width; x++)
                        {
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
            if (bitmap == null)
            {
                bitmap = ImageUtils.BuildImage(imageData, width, height, stride, originalPixelFormat, palette, Color.Black);
                // This is bmp; reverse image lines.
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
            }
            return bitmap;
        }

        private static PixelFormat GetPixelFormat(BITMAPINFOHEADER header)
        {
            PixelFormat fmt;
            switch (header.biBitCount)
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

        private Byte[] ApplyBitMask(Byte[] imageData, ref PixelFormat fmt, Byte[] bitMask, Int32 hdrWidth, Int32 hdrHeight, Color[] palette)
        {
            if (bitMask.Length <= 0)
                return null;
            Int32 height = hdrHeight/2;
            Int32 stride = ImageUtils.GetClassicStride(hdrWidth, Image.GetPixelFormatSize(fmt));
            Int32 maskStride = ImageUtils.GetClassicStride(hdrWidth, 1);
            Byte[] newImageData;
            using (Bitmap maskImage = ImageUtils.BuildImage(bitMask, hdrWidth, height, maskStride, PixelFormat.Format1bppIndexed, null, null))
            using (Bitmap actualImage = ImageUtils.BuildImage(imageData, hdrWidth, height, stride, fmt, palette, Color.Black))
            {
                Byte[] maskBytes = ImageUtils.GetImageData(maskImage, out maskStride, PixelFormat.Format8bppIndexed);
                newImageData = ImageUtils.GetImageData(actualImage, out stride, PixelFormat.Format32bppArgb);
                for (Int32 y = 0; y < height; y++)
                {
                    Int32 offs = y*stride;
                    Int32 maskOffs = y*maskStride;
                    for (Int32 x = 0; x < hdrWidth; x++)
                    {
                        Byte andVal = (Byte) (maskBytes[maskOffs] == 0 ? 0x00 : 0xFF);
                        newImageData[offs] = (Byte) (andVal ^ newImageData[offs + 0]);
                        newImageData[offs + 1] = (Byte) (andVal ^ newImageData[offs + 1]);
                        newImageData[offs + 2] = (Byte) (andVal ^ newImageData[offs + 2]);
                        newImageData[offs + 3] = (Byte) (andVal ^ 0xFF);
                        offs += 4;
                        maskOffs++;
                    }
                }
            }
            fmt = PixelFormat.Format32bppArgb;
            return newImageData;
        }

        private static void ApplyBitFields(Byte[] imageData, Int32 width, Int32 height, ref Int32 stride, Int32 bitCount, ref PixelFormat fmt, BITMAPCOMPRESSION compression, BITFIELDS bitfields)
        {
            if (compression == BITMAPCOMPRESSION.BI_BITFIELDS)
            {
                UInt32 redMask = bitfields.bfRedMask;
                UInt32 greenMask = bitfields.bfGreenMask;
                UInt32 blueMask = bitfields.bfBlueMask;
                // Fix for the undocumented use of 32bppARGB disguised as BI_BITFIELDS. Despite lacking an alpha bit field,
                // the alpha bytes are still filled in, without any header indication of alpha usage.
                // Pure 32-bit RGB: check if a switch to ARGB can be made by checking for non-zero alpha.
                // Admitted, this may give a mess if the alpha bits simply aren't cleared, but why the hell wouldn't it use 24bpp then?
                if (bitCount == 32 && redMask == 0xFF0000 && greenMask == 0x00FF00 && blueMask == 0x0000FF)
                {
                    // Stride is always a multiple of 4; no need to take it into account for 32bpp.
                    for (Int32 pix = 3; pix < imageData.Length; pix += 4)
                    {
                        // 0 can mean transparent, but can also mean the alpha isn't filled in, so only check for non-zero alpha,
                        // which would indicate there is actual data in the alpha bytes.
                        if (imageData[pix] == 0)
                            continue;
                        fmt = PixelFormat.Format32bppPArgb;
                        break;
                    }
                }
                if (fmt != PixelFormat.Format32bppPArgb && bitCount == 16 || bitCount == 32)
                {
                    // Reformat bytes.
                    UInt32 alphaMask = (UInt32)Math.Min(UInt32.MaxValue, 1 << bitCount - 1) & ~(redMask | greenMask | blueMask);
                    PixelFormatter pixFormatter = new PixelFormatter((Byte)(bitCount / 8), redMask, greenMask, blueMask, alphaMask, true);
                    PixelFormatter pfTarget;
                    if (bitCount == 16)
                    {
                        pfTarget = PixelFormatter.Format16BitArgb1555;
                        if (alphaMask != 0)
                            fmt = PixelFormat.Format16bppArgb1555;
                    }
                    else
                    {
                        pfTarget = PixelFormatter.Format32BitArgb;
                        if (alphaMask != 0)
                            fmt = PixelFormat.Format32bppArgb;
                    }
                    ImageUtils.ReorderBits(imageData, width, height, stride, pixFormatter, pfTarget);
                }
            }
        }

        public static Boolean GetDataFromDib(Byte[] dibBytes, Boolean detectIconFormat, out Byte[] imageData, out BITFIELDS bitFields, out Byte[] bitMask, out Color[] palette, out BITMAPINFOHEADER header)
        {
            imageData = null;
            bitMask = null;
            palette = null;
            header = new BITMAPINFOHEADER();
            bitFields = new BITFIELDS();
            if (dibBytes == null || dibBytes.Length < 4)
                return false;
            try
            {
                Int32 headerSize = (Int32)ArrayUtils.ReadIntFromByteArray(dibBytes, 0, 4, true);
                Int32 dibHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                Int32 bitFieldsSize = Marshal.SizeOf(typeof(BITFIELDS));
                if (dibHeaderSize != headerSize)
                    return false;
                header = ArrayUtils.ReadStructFromByteArray<BITMAPINFOHEADER>(dibBytes, 0);
                // Not dealing with non-standard formats
                if (header.biPlanes != 1 || (header.biCompression != BITMAPCOMPRESSION.BI_RGB && header.biCompression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return false;
                Int32 readIndex = headerSize;
                Int32 width = header.biWidth;
                Int32 height = header.biHeight;
                Int32 bitCount = header.biBitCount;
                if (dibBytes.Length < readIndex)
                    return false;
                if (header.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS)
                {
                    bitFields = ArrayUtils.ReadStructFromByteArray<BITFIELDS>(dibBytes, readIndex);
                    readIndex += bitFieldsSize;
                    if (dibBytes.Length < readIndex)
                        return false;
                }
                Int32 paletteLength = bitCount > 8 ? 0 : (Int32)header.biClrUsed;
                if (paletteLength == 0 && bitCount <= 8)
                    paletteLength = 1 << bitCount;
                palette = new Color[paletteLength];
                if (dibBytes.Length < readIndex + paletteLength * 4)
                    return false;
                if (paletteLength > 0)
                {
                    for (Int32 i = 0; i < paletteLength; i++)
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
                Int32 dataLen = stride * height;
                if (dibBytes.Length - readIndex < dataLen + maskSize)
                    return false;
                imageData = new Byte[dataLen];
                Array.Copy(dibBytes, readIndex, imageData, 0, dataLen);
                readIndex += dataLen;
                bitMask = new Byte[maskSize];
                if (maskSize > 0)
                    Array.Copy(dibBytes, readIndex, bitMask, 0, maskSize);
                readIndex += maskSize;
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
