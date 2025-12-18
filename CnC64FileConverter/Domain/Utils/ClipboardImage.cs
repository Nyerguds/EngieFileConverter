using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;

namespace Nyerguds.Util
{
    public class ClipboardImage
    {
        /// <summary>
        /// Retrieves an image from the given clipboard data object, in the order PNG, DIB, Bitmap, Image object.
        /// </summary>
        /// <param name="retrievedData">The clipboard data.</param>
        /// <returns>The extracted image, or null if no supported image type was found.</returns>
        public static Bitmap GetClipboardImage(DataObject retrievedData)
        {
            Bitmap clipboardimage = null;
            String[] formats = retrievedData.GetFormats();
            if (formats.Length == 0)
                return null;
            // Order: try PNG, move on to try 32-bit ARGB DIB, then technically-RGB DIB abused as ARGB, and finally the normal Bitmap and Image types.
            Boolean built = false;
            if (formats.Contains("PNG"))
            {
                Byte[] pngData = TryGetStreamDataFromClipboard(retrievedData, "PNG");
                if (pngData != null)
                {
                    clipboardimage = BitmapHandler.LoadBitmap(pngData);
                    // LoadBitmap clones the object.
                    if (clipboardimage != null) built = true;
                }
            }
            if (clipboardimage == null && formats.Contains("Format17"))
            {
                Byte[] dibdata = ClipboardImage.TryGetStreamDataFromClipboard(retrievedData, "Format17");
                clipboardimage = ClipboardImage.ImageFromClipboardDib5(dibdata);
                // ImageFromClipboardDib5 builds the image in local memory.
                if (clipboardimage != null) built = true;
            }
            if (clipboardimage == null && formats.Contains(DataFormats.Dib))
            {
                Byte[] dibdata = ClipboardImage.TryGetStreamDataFromClipboard(retrievedData, DataFormats.Dib);
                clipboardimage = ClipboardImage.ImageFromClipboardDib(dibdata);
                // ImageFromClipboardDib builds the image in local memory.
                if (clipboardimage != null) built = true;
            }
            if (clipboardimage == null && formats.Contains(DataFormats.Bitmap))
                clipboardimage = retrievedData.GetData(DataFormats.Bitmap) as Bitmap;
            if (clipboardimage == null && formats.Contains(typeof(Bitmap).FullName))
                clipboardimage = retrievedData.GetData(typeof(Bitmap)) as Bitmap;
            if (clipboardimage == null && formats.Contains(typeof (Image).FullName))
            {
                Image clipImage = retrievedData.GetData(typeof (Image)) as Image;
                if (clipImage != null)
                    clipboardimage = new Bitmap(clipImage);
            }
            // Clone to separate it from any backing sources
            if (clipboardimage != null && !built)
                clipboardimage = ImageUtils.CloneImage(clipboardimage);
            return clipboardimage;
        }

        /// <summary>
        /// Copies the given image to the clipboard as PNG, DIB and standard Bitmap format.
        /// </summary>
        /// <param name="image">Image to put on the clipboard.</param>
        /// <param name="imageNoTr">Optional specifically nontransparent version of the image to put on the clipboard.</param>
        /// <param name="data">Clipboard data object to put the image into. Might already contain other stuff. Leave null to create a new one.</param>
        public static void SetClipboardImage(Image image, Image imageNoTr, DataObject data)
        {
            if (data == null)
                data = new DataObject();
            if (imageNoTr == null)
                imageNoTr = image;
            using (MemoryStream pngMemStream = new MemoryStream())
            using (MemoryStream dib5MemStream = new MemoryStream())
            using (MemoryStream dibMemStream = new MemoryStream())
            {
                // As standard bitmap, without transparency support
                data.SetData(DataFormats.Bitmap, true, imageNoTr);
                // As PNG. Gimp will prefer this over the other two.
                Byte[] pngData = BitmapHandler.GetPngImageData(image, 0);
                pngMemStream.Write(pngData, 0, pngData.Length);
                data.SetData("PNG", false, pngMemStream);
                // As DIBv5. This supports transparency when using BITFIELDS.
                Byte[] dib5Data = ClipboardImage.ConvertToDib5(image);
                dib5MemStream.Write(dib5Data, 0, dib5Data.Length);
                data.SetData("Format17", false, dib5MemStream);
                // As DIB. This is (wrongly) accepted as ARGB by many applications.
                Byte[] dibData = ClipboardImage.ConvertToDib(image);
                dibMemStream.Write(dibData, 0, dibData.Length);
                data.SetData(DataFormats.Dib, false, dibMemStream);
                // The 'copy=true' argument means the MemoryStreams can be safely disposed after the operation.
                Clipboard.SetDataObject(data, true);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BITMAPINFOHEADER
        {
            public UInt32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public Int16 biPlanes;
            public Int16 biBitCount;
            public BITMAPCOMPRESSION biCompression;
            public UInt32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public UInt32 biClrUsed;
            public UInt32 biClrImportant;
        }

        // Length = 124 bytes.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BITMAPV5HEADER
        {
            /// <summary>0x00</summary>
            public UInt32 bV5Size;
            /// <summary>0x04</summary>
            public Int32 bV5Width;
            /// <summary>0x08</summary>
            public Int32 bV5Height;
            /// <summary>0x0C</summary>
            public UInt16 bV5Planes;
            /// <summary>0x0E</summary>
            public UInt16 bV5BitCount;
            /// <summary>0x10</summary>
            public BITMAPCOMPRESSION bV5Compression;
            /// <summary>0x14</summary>
            public UInt32 bV5SizeImage;
            /// <summary>0x18</summary>
            public Int32 bV5XPelsPerMeter;
            /// <summary>0x1C</summary>
            public Int32 bV5YPelsPerMeter;
            /// <summary>0x20</summary>
            public UInt32 bV5ClrUsed;
            /// <summary>0x24</summary>
            public UInt32 bV5ClrImportant;
            /// <summary>0x28</summary>
            public UInt32 bV5RedMask;
            /// <summary>0x2C</summary>
            public UInt32 bV5GreenMask;
            /// <summary>0x30</summary>
            public UInt32 bV5BlueMask;
            /// <summary>0x34</summary>
            public UInt32 bV5AlphaMask;
            /// <summary>0x38</summary>
            public LogicalColorSpace bV5CSType;
            /// <summary>0x3C</summary>
            public UInt32 bV5EndpointsCiexyzRedX;
            /// <summary>0x40</summary>
            public UInt32 bV5EndpointsCiexyzRedY;
            /// <summary>0x44</summary>
            public UInt32 bV5EndpointsCiexyzRedZ;
            /// <summary>0x48</summary>
            public UInt32 bV5EndpointsCiexyzGreenX;
            /// <summary>0x4C</summary>
            public UInt32 bV5EndpointsCiexyzGreenY;
            /// <summary>0x50</summary>
            public UInt32 bV5EndpointsCiexyzGreenZ;
            /// <summary>0x54</summary>
            public UInt32 bV5EndpointsCiexyzBlueX;
            /// <summary>0x58</summary>
            public UInt32 bV5EndpointsCiexyzBlueY;
            /// <summary>0x5C</summary>
            public UInt32 bV5EndpointsCiexyzBlueZ;
            /// <summary>0x60</summary>
            public UInt32 bV5GammaRed;
            /// <summary>0x64</summary>
            public UInt32 bV5GammaGreen;
            /// <summary>0x68</summary>
            public UInt32 bV5GammaBlue;
            /// <summary>0x6C</summary>
            public GamutMappingIntent bV5Intent;
            /// <summary>0x70</summary>
            public UInt32 bV5ProfileData;
            /// <summary>0x74</summary>
            public UInt32 bV5ProfileSize;
            /// <summary>0x78</summary>
            public UInt32 bV5Reserved;
        }

        public enum LogicalColorSpace: uint
        {
            LCS_CALIBRATED_RGB = 0x00000000,
            LCS_sRGB = 0x73524742, // litle-endian "sRGB"
            LCS_WINDOWS_COLOR_SPACE = 0x57696E20 // litle-endian "Win "
        }

        public enum GamutMappingIntent : uint
        {
            LCS_GM_BUSINESS = 0x00000001,
            LCS_GM_GRAPHICS = 0x00000002,
            LCS_GM_IMAGES = 0x00000004,
            LCS_GM_ABS_COLORIMETRIC = 0x00000008,
        }

        public enum BITMAPCOMPRESSION: int
        {
            BI_RGB = 0x0000,
            BI_RLE8 = 0x0001,
            BI_RLE4 = 0x0002,
            BI_BITFIELDS = 0x0003,
            BI_JPEG = 0x0004,
            BI_PNG = 0x0005,
            BI_CMYK = 0x000B,
            BI_CMYKRLE8 = 0x000C,
            BI_CMYKRLE4 = 0x000D
        }

        public static Byte[] TryGetStreamDataFromClipboard(DataObject retrievedData, String identifier)
        {
            if (!retrievedData.GetDataPresent(identifier))
                return null;
            // Get the dib header
            Object data = retrievedData.GetData(identifier);
            if (!(data is MemoryStream))
                return null;
            MemoryStream ms = retrievedData.GetData(identifier) as MemoryStream;
            if (ms == null)
                return null;
            return ms.ToArray();
        }

        /// <summary>
        /// Converts the image to Device Independent Bitmap format of version 5, of type BI_BITFIELDS.
        /// </summary>
        /// <param name="image">Image to convert to DIB</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static Byte[] ConvertToDib5(Image image)
        {
            Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null);
            // Bitmap format has its lines reversed.
            bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
            Int32 stride;
            Byte[] bm32bData = ImageUtils.GetImageData(bm32b, out stride);

            BITMAPV5HEADER hdr = new BITMAPV5HEADER();
            Int32 hdrSize = Marshal.SizeOf(typeof(BITMAPV5HEADER));
            hdr.bV5Size = (UInt32)hdrSize;
            hdr.bV5Width = bm32b.Width;
            hdr.bV5Height = bm32b.Height;
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
            Byte[] fullImage = new Byte[hdrSize + bm32bData.Length];
            Byte[] pibHeaderBytes = StructToByteArray(hdr);
            Array.Copy(pibHeaderBytes, 0, fullImage, 0, hdrSize);
            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize, bm32bData.Length);
            return fullImage;
        }


        /// <summary>
        /// Converts the image to Device Independent Bitmap format of type BI_BITFIELDS.
        /// This is (wrongly) accepted by many applications as containing transparency,
        /// so I'm abusing it for that.
        /// </summary>
        /// <param name="image">Image to convert to DIB</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static Byte[] ConvertToDib(Image image)
        {
            Bitmap bm32b = ImageUtils.PaintOn32bpp(image, null);
            // Bitmap format has its lines reversed.
            bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
            Int32 stride;
            Byte[] bm32bData = ImageUtils.GetImageData(bm32b, out stride);

            BITMAPINFOHEADER hdr = new BITMAPINFOHEADER();
            Int32 hdrSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            hdr.biSize = (UInt32)hdrSize;
            hdr.biWidth = bm32b.Width;
            hdr.biHeight = bm32b.Height;
            hdr.biPlanes = 1;
            hdr.biBitCount = 32;
            hdr.biCompression = BITMAPCOMPRESSION.BI_BITFIELDS;
            hdr.biSizeImage = (UInt32)bm32bData.Length;
            hdr.biXPelsPerMeter = 0;
            hdr.biYPelsPerMeter = 0;
            hdr.biClrUsed = 0;
            hdr.biClrImportant = 0;

            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            Byte[] pibHeaderBytes = StructToByteArray(hdr);
            Array.Copy(pibHeaderBytes, 0, fullImage, 0, hdrSize);
            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        public static Bitmap ImageFromClipboardDib5(Byte[] dibBytes)
        {
            if (dibBytes == null || dibBytes.Length < 4)
                return null;
            try
            {
                Int32 headerSize = (Int32)ArrayUtils.ReadIntFromByteArray(dibBytes, 0, 4, true);
                // Only supporting 124-byte DIBV5 in this.
                // If it fails, try the other type ;)
                if (headerSize == 40)
                    return ImageFromClipboardDib(dibBytes);
                if (headerSize != 0x7C)
                    return null;
                Byte[] header = new Byte[headerSize];
                Array.Copy(dibBytes, header, headerSize);
                BITMAPV5HEADER dibHdr = StructFromByteArray<BITMAPV5HEADER>(header);
                // Not dealing with non-standard formats
                if (dibHdr.bV5Planes != 1 || (dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_RGB && dibHdr.bV5Compression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return null;
                Int32 imageIndex = headerSize;
                Int32 width = dibHdr.bV5Width;
                Int32 height = dibHdr.bV5Height;
                Int32 bitCount = dibHdr.bV5BitCount;
                PixelFormat fmt = PixelFormat.Undefined;
                switch (bitCount)
                {
                    case 32:
                        if (dibHdr.bV5Compression == BITMAPCOMPRESSION.BI_BITFIELDS)
                        {
                            if (dibHdr.bV5RedMask == 0x00FF0000 && dibHdr.bV5GreenMask == 0x0000FF00 && dibHdr.bV5BlueMask == 0x000000FF)
                            {
                                if (dibHdr.bV5AlphaMask == 0x00000000)
                                    fmt = PixelFormat.Format32bppRgb;
                                else if (dibHdr.bV5AlphaMask == 0xFF000000)
                                    fmt = PixelFormat.Format32bppArgb;
                            }
                        }
                        else
                            fmt = PixelFormat.Format32bppRgb;
                        break;
                    case 24:
                        fmt = PixelFormat.Format24bppRgb;
                        break;
                    case 16:
                        if (dibHdr.bV5Compression == BITMAPCOMPRESSION.BI_BITFIELDS)
                        {
                            if (dibHdr.bV5RedMask == 0x7C00 && dibHdr.bV5GreenMask == 0x03E0 && dibHdr.bV5BlueMask == 0x01F)
                            {
                                if (dibHdr.bV5AlphaMask == 0x0000)
                                    fmt = PixelFormat.Format16bppRgb555;
                                else if (dibHdr.bV5AlphaMask == 0x8000)
                                    fmt = PixelFormat.Format16bppArgb1555;
                            }
                            else if (dibHdr.bV5RedMask == 0xF800 && dibHdr.bV5GreenMask == 0x07E0 && dibHdr.bV5BlueMask == 0x01F)
                                fmt = PixelFormat.Format16bppRgb565;
                        }
                        else
                            fmt = PixelFormat.Format16bppRgb555;
                        break;
                    default:
                        return null;
                }
                if (fmt == PixelFormat.Undefined)
                    return null;
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
                    fmt = PixelFormat.Format32bppArgb;
                }
                Byte[] image = new Byte[dataLen];
                Array.Copy(dibBytes, imageIndex, image, 0, image.Length);
                Int32 stride = ImageUtils.GetClassicStride(width, bitCount);
                Bitmap bitmap = ImageUtils.BuildImage(image, width, height, stride, fmt, null, null);
                // This is bmp; reverse image lines.
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap ImageFromClipboardDib(Byte[] dibBytes)
        {
            if (dibBytes == null || dibBytes.Length < 4)
                return null;
            try
            {
                Int32 headerSize = (Int32)ArrayUtils.ReadIntFromByteArray(dibBytes, 0, 4, true);
                if (headerSize == 0x7C)
                    return ImageFromClipboardDib5(dibBytes);
                if (headerSize != 40)
                    return null;
                Byte[] header = new Byte[40];
                Array.Copy(dibBytes, header, 40);
                BITMAPINFOHEADER dibHdr = StructFromByteArray<BITMAPINFOHEADER>(header);
                // Not dealing with non-standard formats
                if (dibHdr.biPlanes != 1 || (dibHdr.biCompression != BITMAPCOMPRESSION.BI_RGB && dibHdr.biCompression != BITMAPCOMPRESSION.BI_BITFIELDS))
                    return null;
                Int32 imageIndex = headerSize;
                Int32 width = dibHdr.biWidth;
                Int32 height = dibHdr.biHeight;
                Int32 bitCount = dibHdr.biBitCount;
                PixelFormat fmt;
                switch (bitCount)
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
                    default:
                        return null;
                }
                if (dibHdr.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS)
                    imageIndex += 12;
                if (dibBytes.Length < imageIndex)
                    return null;
                Byte[] image = new Byte[dibBytes.Length - imageIndex];
                Array.Copy(dibBytes, imageIndex, image, 0, image.Length);
                Int32 stride = ImageUtils.GetClassicStride(width, bitCount);
                if (dibHdr.biCompression == BITMAPCOMPRESSION.BI_BITFIELDS)
                {
                    UInt32 redMask = ArrayUtils.ReadIntFromByteArray(dibBytes, headerSize + 0, 4, true);
                    UInt32 greenMask = ArrayUtils.ReadIntFromByteArray(dibBytes, headerSize + 4, 4, true);
                    UInt32 blueMask = ArrayUtils.ReadIntFromByteArray(dibBytes, headerSize + 8, 4, true);
                    // Fix for the undocumented use of 32bppARGB disguised as BI_BITFIELDS. Despite lacking an alpha bit field,
                    // the alpha bytes are still filled in, without any header indication of alpha usage.
                    // Pure 32-bit RGB: check if a switch to ARGB can be made by checking for non-zero alpha.
                    // Admitted, this may give a mess if the alpha bits simply aren't cleared, but why the hell wouldn't it use 24bpp then?
                    if (bitCount == 32 && redMask == 0xFF0000 && greenMask == 0x00FF00 && blueMask == 0x0000FF)
                    {
                        // Stride is always a multiple of 4; no need to take it into account for 32bpp.
                        for (Int32 pix = 3; pix < image.Length; pix += 4)
                        {
                            // 0 can mean transparent, but can also mean the alpha isn't filled in, so only check for non-zero alpha,
                            // which would indicate there is actual data in the alpha bytes.
                            if (image[pix] == 0)
                                continue;
                            fmt = PixelFormat.Format32bppPArgb;
                            break;
                        }
                    }
                    if (fmt != PixelFormat.Format32bppPArgb)
                    {
                        // Reformat bytes.
                        PixelFormatter pf = new PixelFormatter((Byte)(bitCount / 8), redMask, greenMask, blueMask, 0);
                        PixelFormatter pf32Argb = PixelFormatter.Format32BitArgb;
                        Int32 strideArgb = ImageUtils.GetClassicStride(width, 32);
                        Byte[] imageArgb = new Byte[height * strideArgb];
                        Int32 srcbytesPerPixel = bitCount / 8;
                        for (Int32 y = 0; y < height; y++)
                        {
                            Int32 offs = y * stride;
                            Int32 offs32 = y * strideArgb;
                            for (Int32 x = 0; x < width; x++)
                            {
                                Color c = pf.GetColor(image, offs);
                                pf32Argb.WriteColor(imageArgb, offs32, c);
                                offs += srcbytesPerPixel;
                                offs32 += 4;
                            }
                        }
                        image = imageArgb;
                        fmt = PixelFormat.Format32bppArgb;
                        stride = strideArgb;
                    }
                }
                Bitmap bitmap = ImageUtils.BuildImage(image, width, height, stride, fmt, null, null);
                // This is bmp; reverse image lines.
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static T StructFromByteArray<T>(Byte[] bytes) where T : struct
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                Int32 size = Marshal.SizeOf(typeof (T));
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                Object obj = Marshal.PtrToStructure(ptr, typeof (T));
                return (T)obj;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        public static Byte[] StructToByteArray<T>(T obj) where T : struct
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                Int32 size = Marshal.SizeOf(typeof (T));
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                Byte[] bytes = new Byte[size];
                Marshal.Copy(ptr, bytes, 0, size);
                return bytes;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
