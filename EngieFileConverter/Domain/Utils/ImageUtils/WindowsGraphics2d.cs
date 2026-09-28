using System;
using System.Runtime.InteropServices;

namespace Windows.Graphics2d
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public BITMAPCOMPRESSION biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITMAPV5HEADER
    {
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public ushort bV5Planes;
        public ushort bV5BitCount;
        public BITMAPCOMPRESSION bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
        public LogicalColorSpace bV5CSType;
        public uint bV5EndpointsCiexyzRedX;
        public uint bV5EndpointsCiexyzRedY;
        public uint bV5EndpointsCiexyzRedZ;
        public uint bV5EndpointsCiexyzGreenX;
        public uint bV5EndpointsCiexyzGreenY;
        public uint bV5EndpointsCiexyzGreenZ;
        public uint bV5EndpointsCiexyzBlueX;
        public uint bV5EndpointsCiexyzBlueY;
        public uint bV5EndpointsCiexyzBlueZ;
        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;
        public GamutMappingIntent bV5Intent;
        public uint bV5ProfileData;
        public uint bV5ProfileSize;
        public uint bV5Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITFIELDS
    {
        public uint bfRedMask;
        public uint bfGreenMask;
        public uint bfBlueMask;
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
        public ushort Reserved;
        public ushort Type;
        public ushort NumberOfImages;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ICONDIRENTRY
    {
        // 0 image width
        public byte Width;// is 0 for "256"
        // 1 image height
        public byte Height;
        // 2 number of colors
        public byte PaletteLength;
        // 3 reserved
        public byte Reserved;
        // 4-5 color planes
        public ushort ColorPlanes;
        // 6-7 bits per pixel
        public ushort BitsPerPixel;
        // 8-11 size of image data
        public uint ImageLength;
        // 12-15 offset of image data
        public uint ImageOffset;
    }
}
