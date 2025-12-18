using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

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
            return Color.FromArgb(color.A, Color.FromArgb((Int32)(0x00FFFFFFu ^ (UInt32)color.ToArgb())));
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
            for (Int32 i = 0; i < 256; ++i)
            {
                Color palcol = pal[i];
                Color graycol = grayPalette[i];
                if (pal[i].A != 255 || palcol.R != graycol.R || palcol.G != graycol.G || palcol.B != graycol.B)
                    return false;
            }
            return true;
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
            for (Int32 i = 0; i < sixbitpalette.Length; ++i)
                eightbitpalette[i] = sixbitpalette[i];
            return eightbitpalette;
        }

        public static ColorSixBit[] GetSixBitColorPalette(Color[] eightbitpalette)
        {
            ColorSixBit[] sixbitpalette = new ColorSixBit[eightbitpalette.Length];
            for (Int32 i = 0; i < eightbitpalette.Length; ++i)
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
            for (Int32 i = 0; i < end; ++i)
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
            for (Int32 i = 0; i < end; ++i)
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
            return ReadSixBitPalette(readBytes);
        }

        public static Color[] ReadFromSixBitPaletteFile(String palfilename)
        {
            Byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadSixBitPaletteAsEightBit(readBytes, 0, 0x100);
        }

        public static ColorSixBit[] ReadSixBitPalette(Byte[] paletteData)
        {
            if (paletteData.Length != 0x300)
                throw new ArgumentException(invalid);
            return ReadSixBitPalette(paletteData, 0, 0x100);
        }

        public static ColorSixBit[] ReadSixBitPalette(Byte[] paletteData, Int32 start)
        {
            return ReadSixBitPalette(paletteData, start, 0x100);
        }

        public static ColorSixBit[] ReadSixBitPalette(Byte[] paletteData, Int32 start, Int32 colors)
        {
            if (paletteData.Length + start < colors * 3)
                throw new ArgumentException(invalid);
            ColorSixBit[] pal = new ColorSixBit[colors];
            try
            {
                for (Int32 i = 0; i < colors; ++i)
                {
                    Int32 index = start + i * 3;
                    pal[i] = new ColorSixBit(paletteData[index], paletteData[index + 1], paletteData[index + 2]);
                }
                return pal;
            }
            catch (ArgumentException e)
            {
                // ArgumentException means some of the values exceeded 63
                throw new ArgumentException(invalid, e);
            }
        }
        
        public static Color[] ReadSixBitPaletteAsEightBit(Byte[] paletteData)
        {
            return ReadSixBitPaletteAsEightBit(paletteData, 0, 0x100);
        }

        public static Color[] ReadSixBitPaletteAsEightBit(Byte[] paletteData, Int32 start)
        {
            return ReadSixBitPaletteAsEightBit(paletteData, start, 0x100);
        }

        public static Color[] ReadSixBitPaletteAsEightBit(Byte[] paletteData, Int32 start, Int32 colors)
        {
            if (paletteData.Length + start < colors * 3)
                throw new ArgumentException(invalid);
            Color[] pal = new Color[colors];
            try
            {
                for (Int32 i = 0; i < colors; ++i)
                {
                    Int32 index = start + i * 3;
                    pal[i] = new ColorSixBit(paletteData[index], paletteData[index + 1], paletteData[index + 2]).GetAsColor();
                }
                return pal;
            }
            catch (ArgumentException e)
            {
                // ArgumentException means some of the values exceeded 63
                throw new ArgumentException(invalid, e);
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
            return ReadEightBitPalette(paletteData, 0, readFull ? 0x100 : Math.Min(0x100, dataLength / 3));
        }

        public static Color[] ReadEightBitPalette(Byte[] data)
        {
            return ReadEightBitPalette(data, 0, 0x100);
        }

        public static Color[] ReadEightBitPalette(Byte[] data, Int32 index)
        {
            return ReadEightBitPalette(data, index, 0x100);
        }

        public static Color[] ReadEightBitPalette(Byte[] data, Int32 index, Int32 length)
        {
            length = Math.Min(0x100, Math.Max(0, length));
            Color[] pal = new Color[length];
            Int32 dataEnd = Math.Min(data.Length, index + length * 3);
            for (Int32 i = 0; i < length; ++i)
            {
                if (index + 2 > dataEnd)
                    pal[i] = Color.Empty;
                else
                    pal[i] = Color.FromArgb(data[index], data[index + 1], data[index + 2]);
                index += 3;
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
            for (Int32 i = 0; i < colorPalette.Length; ++i)
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

        public static Color ColorFromHexString(String colorStr)
        {
            if (String.IsNullOrEmpty(colorStr))
                return Color.Empty;
            colorStr = colorStr.TrimStart('#').ToUpperInvariant();
            if (!Regex.IsMatch(colorStr, "[0-9A-F]+"))
                return Color.Empty;
            Int32 len = colorStr.Length;
            if (len != 3 && len != 4 && len != 6 && len != 8)
                return Color.Empty;
            Int32 red;
            Int32 green;
            Int32 blue;
            Int32 alpha;
            if (len <= 4)
            {
                Int32 startIndex = len == 3 ? 0 : 1;
                red = Int32.Parse(colorStr.Substring(startIndex, startIndex + 1), NumberStyles.HexNumber);
                green = Int32.Parse(colorStr.Substring(startIndex + 1, startIndex + 2), NumberStyles.HexNumber);
                blue = Int32.Parse(colorStr.Substring(startIndex + 2, startIndex + 3), NumberStyles.HexNumber);
                alpha = len == 3 ? 0xF : Int32.Parse(colorStr.Substring(0, 1), NumberStyles.HexNumber);
                // double the digits
                red = red << 8 | red;
                green = green << 8 | green;
                blue = blue << 8 | blue;
                alpha = alpha << 8 | alpha;
                return Color.FromArgb(alpha, red, green, blue);
            }
            else
            {
                UInt32 argb = UInt32.Parse(colorStr, NumberStyles.HexNumber);
                if (len == 6)
                    argb += 0xFF000000;
                return Color.FromArgb((Int32)argb);
            }
        }

        public static String HexStringFromColor(Color color, Boolean withAlpha)
        {
            UInt32 colVal = (UInt32) color.ToArgb();
            if (!withAlpha)
                colVal = colVal & 0xFFFFFF;
            return "#" + colVal.ToString(withAlpha ? "X8" : "X6");
        }

    }
}
