using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Nyerguds.ImageManipulation
{
    public static class ColorUtils
    {

        const String invalid = "This is not a valid six-bit palette file.";

        public static Color ColorFromUInt(UInt32 argb)
        {
            return Color.FromArgb((Byte)((argb >> 0x18) & 0xFF), (Byte)((argb >> 0x10) & 0xFF), (Byte)((argb >> 0x08) & 0xFF), (Byte)(argb & 0xFF));
        }

        public static Color GetVisibleBorderColor(Color color)
        {
            if (!(color.GetSaturation() < .16))
                return GetInvertedColor(color);
            // this color is gray
            return color.GetBrightness() < .5 ? Color.White : Color.Black;
        }

        public static Color GetInvertedColor(Color color)
        {
            return Color.FromArgb((Int32)(0x00FFFFFFu ^ (UInt32)color.ToArgb()));
        }

        public static Boolean HasGrayPalette(Bitmap image)
        {
            PixelFormat pf = image.PixelFormat;
            if (pf != PixelFormat.Format1bppIndexed && pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                return false;
            Int32 grayPfs = Math.Min(8, Image.GetPixelFormatSize(image.PixelFormat));
            Color[] grayPalette = PaletteUtils.GenerateGrayPalette(grayPfs, null, false);
            Color[] pal = image.Palette.Entries;
            if (pal.Length != grayPalette.Length)
                return false;
            for (Int32 i = 0; i < 256; i++)
            {
                Color palcol = pal[i];
                Color graycol = grayPalette[i];
                if (pal[i].A != 255 || palcol.R != graycol.R || palcol.G != graycol.G || palcol.B != graycol.B)
                    return false;
            }
            return true;
        }

        public static Byte[] GenerateInterlaceTable(Color[] colorPalette, List<Int32> exclIndSrc, Boolean dupOnExcluded, List<Int32> exclIndTrg)
        {
            if (colorPalette.Length > 0x100)
                return null;
            Color[] palette = new Color[0x100];
            colorPalette.CopyTo(palette, 0);

            Boolean[] excludedFrom = new Boolean[0x100];
            foreach (Byte index in exclIndSrc)
                excludedFrom[index] = true;
            Byte[] interlaceTable = new Byte[0x10000];
            for (Int32 y = 0; y < 0x100; y++)
            {
                for (Int32 x = y; x < 0x100; x++)
                {
                    Byte value;
                    Boolean equal = y == x;
                    Boolean exclX = excludedFrom[x];
                    Boolean exclY = excludedFrom[y];
                    if (equal)
                        value = (Byte)y;
                    else if (exclX || exclY)
                        value = dupOnExcluded ? (exclX ? (Byte)x : (Byte)y) : (Byte)0;
                    else
                        value = (Byte)GetClosestPaletteIndexMatch(GetAverageColor(colorPalette[x], colorPalette[y]), palette, exclIndTrg);
                    interlaceTable[y * 0x100 + x] = value;
                    if (!equal)
                        interlaceTable[x * 0x100 + y] = value;
                }
            }
            return interlaceTable;
        }

        public static Color GetAverageColor(Color col1, Color col2)
        {
            Byte averageR = (Byte)Math.Max(0, Math.Min(255, Math.Min(col1.R, col2.R) + Math.Abs(col1.R - col2.R) / 2));
            Byte averageG = (Byte)Math.Max(0, Math.Min(255, Math.Min(col1.G, col2.G) + Math.Abs(col1.G - col2.G) / 2));
            Byte averageB = (Byte)Math.Max(0, Math.Min(255, Math.Min(col1.B, col2.B) + Math.Abs(col1.B - col2.B) / 2));
            return Color.FromArgb(averageR, averageG, averageB);
        }

        public static Color[] GetEightBitColorPalette(ColorSixBit[] sixbitpalette)
        {
            Color[] eightbitpalette = new Color[sixbitpalette.Length];
            for (Int32 i = 0; i < sixbitpalette.Length; i++)
                eightbitpalette[i] = sixbitpalette[i].GetAsColor();
            return eightbitpalette;
        }

        public static ColorSixBit[] GetSixBitColorPalette(Color[] eightbitpalette)
        {
            ColorSixBit[] sixbitpalette = new ColorSixBit[eightbitpalette.Length];
            for (Int32 i = 0; i < eightbitpalette.Length; i++)
                sixbitpalette[i] = new ColorSixBit(eightbitpalette[i]);
            return sixbitpalette;
        }

        public static void WriteSixBitPaletteFile(Color[] palette, String palfilename)
        {
            ColorSixBit[] newpal = GetSixBitColorPalette(palette);
            WriteSixBitPaletteFile(newpal, palfilename);
        }

        public static void WriteSixBitPaletteFile(ColorSixBit[] palette, String palfilename)
        {
            Byte[] pal = GetSixBitPaletteData(palette);
            File.WriteAllBytes(palfilename, pal);
        }

        public static Byte[] GetSixBitPaletteData(ColorSixBit[] palette)
        {
            Byte[] pal = new Byte[768];
            Int32 end = Math.Min(768, palette.Length);
            for (Int32 i = 0; i < end; i++)
            {
                Int32 index = i * 3;
                pal[index] = palette[i].R;
                pal[index + 1] = palette[i].G;
                pal[index + 2] = palette[i].B;
            }
            return pal;
        }

        public static void WriteEightBitPaletteFile(Color[] palette, String palfilename, Boolean expandTo256)
        {
            Byte[] bytes = GetEightBitPaletteData(palette, expandTo256);
            File.WriteAllBytes(palfilename, bytes);
        }

        public static Byte[] GetEightBitPaletteData(Color[] palette, Boolean expandTo256)
        {
            Int32 end = expandTo256 ? 256 : Math.Min(256, palette.Length);
            Byte[] pal = new Byte[end * 3];
            for (Int32 i = 0; i < end; i++)
            {
                Int32 index = i * 3;
                Color col;
                if (i < palette.Length)
                    col = palette[i];
                else
                    col = Color.Black;
                pal[index] = col.R;
                pal[index + 1] = col.G;
                pal[index + 2] = col.B;
            }
            return pal;
        }

        public static ColorSixBit[] ReadSixBitPaletteFile(String palfilename)
        {
            Byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadSixBitPaletteFile(readBytes);
        }

        public static ColorSixBit[] ReadSixBitPaletteFile(Byte[] paletteData)
        {
            if (paletteData.Length != 768)
                throw new ArgumentException(invalid);
            return ReadSixBitPalette(paletteData, 0);
        }

        public static ColorSixBit[] ReadSixBitPalette(Byte[] paletteData, Int32 start)
        {
            if (paletteData.Length + start < 768)
                throw new ArgumentException(invalid);
            ColorSixBit[] pal = new ColorSixBit[256];
            try
            {
                for (Int32 i = 0; i < pal.Length; i++)
                {
                    Int32 index = i * 3;
                    pal[i] = new ColorSixBit(paletteData[index], paletteData[index + 1], paletteData[index + 2]);
                }
                return pal;
            }
            catch (ArgumentException e)
            {
                // ArgumentException means some of the values exceeded 63
                throw new NotSupportedException(invalid, e);
            }
        }

        public static Color[] ReadEightBitPaletteFile(String palfilename, Boolean readFull)
        {
            Byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadEightBitPalette(readBytes, readFull);
        }

        public static Color[] ReadEightBitPalette(Byte[] paletteData, Boolean readFull)
        {
            Int32 dataLength = paletteData.Length;
            if (dataLength % 3 != 0)
                throw new ArgumentException("This is not a valid palette file.");
            Color[] pal = new Color[readFull ? 256 : dataLength / 3];
            for (Int32 i = 0; i < pal.Length; i++)
            {
                Int32 index = i * 3;
                if (index + 2 > dataLength)
                    pal[i] = Color.Empty;
                else
                    pal[i] = Color.FromArgb(paletteData[index], paletteData[index + 1], paletteData[index + 2]);
            }
            return pal;
        }

        public static Int32 GetClosestPaletteIndexMatch(Color col, Color[] colorPalette, List<Int32> excludedindices)
        {
            Int32 colorMatch = 0;
            Int32 leastDistance = Int32.MaxValue;
            Int32 red = col.R;
            Int32 green = col.G;
            Int32 blue = col.B;
            for (Int32 i = 0; i < colorPalette.Length; i++)
            {
                if (excludedindices != null && excludedindices.Contains(i))
                    continue;
                Color paletteColor = colorPalette[i];
                Int32 redDistance = paletteColor.R - red;
                Int32 greenDistance = paletteColor.G - green;
                Int32 blueDistance = paletteColor.B - blue;
                Int32 distance = (redDistance * redDistance) + (greenDistance * greenDistance) + (blueDistance * blueDistance);
                if (distance >= leastDistance)
                    continue;
                colorMatch = i;
                leastDistance = distance;
                if (distance == 0)
                    return i;
            }
            return colorMatch;
        }

        public static Color GetGreyColor(Color color)
        {
            Byte grey = GetGreyValue(color.R, color.G, color.B);
            return Color.FromArgb(grey, grey, grey);
        }

        public static Byte GetGreyValue(Color color)
        {
            return GetGreyValue(color.R, color.G, color.B);
        }

        public static Byte GetGreyValue(Byte red, Byte green, Byte blue)
        {
            Double redFactor = 0.2126d * Math.Pow(red, 2.2d);
            Double grnFactor = 0.7152d * Math.Pow(green, 2.2d);
            Double bluFactor = 0.0722d * Math.Pow(blue, 2.2d);
            Double grey = Math.Pow(redFactor + grnFactor + bluFactor, 1d / 2.2);
            return (Byte)Math.Max(0, Math.Min(255, Math.Round(grey, MidpointRounding.AwayFromZero)));
        }

    }
}
