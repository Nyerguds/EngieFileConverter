using System;
using System.Runtime.InteropServices;

namespace Windows.Graphics2d
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITMAPINFOHEADER
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITMAPV5HEADER
    {
        public UInt32 bV5Size;
        public Int32 bV5Width;
        public Int32 bV5Height;
        public UInt16 bV5Planes;
        public UInt16 bV5BitCount;
        public BITMAPCOMPRESSION bV5Compression;
        public UInt32 bV5SizeImage;
        public Int32 bV5XPelsPerMeter;
        public Int32 bV5YPelsPerMeter;
        public UInt32 bV5ClrUsed;
        public UInt32 bV5ClrImportant;
        public UInt32 bV5RedMask;
        public UInt32 bV5GreenMask;
        public UInt32 bV5BlueMask;
        public UInt32 bV5AlphaMask;
        public LogicalColorSpace bV5CSType;
        public UInt32 bV5EndpointsCiexyzRedX;
        public UInt32 bV5EndpointsCiexyzRedY;
        public UInt32 bV5EndpointsCiexyzRedZ;
        public UInt32 bV5EndpointsCiexyzGreenX;
        public UInt32 bV5EndpointsCiexyzGreenY;
        public UInt32 bV5EndpointsCiexyzGreenZ;
        public UInt32 bV5EndpointsCiexyzBlueX;
        public UInt32 bV5EndpointsCiexyzBlueY;
        public UInt32 bV5EndpointsCiexyzBlueZ;
        public UInt32 bV5GammaRed;
        public UInt32 bV5GammaGreen;
        public UInt32 bV5GammaBlue;
        public GamutMappingIntent bV5Intent;
        public UInt32 bV5ProfileData;
        public UInt32 bV5ProfileSize;
        public UInt32 bV5Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITFIELDS
    {
        public UInt32 bfRedMask;
        public UInt32 bfGreenMask;
        public UInt32 bfBlueMask;
    }

    public enum LogicalColorSpace : uint
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

    public enum BITMAPCOMPRESSION : int
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ICONDIR
    {
        public UInt16 Reserved;
        public UInt16 Type;
        public UInt16 NumberOfImages;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ICONDIRENTRY
    {
        // 0 image width
        public Byte Width;// is 0 for "256"
        // 1 image height
        public Byte Height;
        // 2 number of colors
        public Byte PaletteLength;
        // 3 reserved
        public Byte Reserved;
        // 4-5 color planes
        public UInt16 ColorPlanes;
        // 6-7 bits per pixel
        public UInt16 BitsPerPixel;
        // 8-11 size of image data
        public UInt32 ImageLength;
        // 12-15 offset of image data
        public UInt32 ImageOffset;
    }
}
