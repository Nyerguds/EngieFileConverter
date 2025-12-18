using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Nyerguds.GameData.Westwood;

namespace Nyerguds.ImageManipulation
{
    public static class ColorUtils
    {
        public static Color ColorFromUInt(UInt32 argb)
        {
            return Color.FromArgb((Byte)((argb & 0xff000000) >> 0x18), (Byte)((argb & 0xff0000) >> 0x10), (Byte)((argb & 0xff00) >> 0x08), (Byte)(argb & 0xff));
        }

        public static Color GetVisibleBorderColor(Color color)
        {
            float sat = color.GetSaturation();
            float bri = color.GetBrightness();
            if (color.GetSaturation() < .16)
            {
                // this color is gray
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

        public static Boolean HasGrayPalette(Bitmap image)
        {
            PixelFormat pf = image.PixelFormat;
            if (pf != PixelFormat.Format1bppIndexed && pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                return false;
            Int32 grayPfs = Math.Min(8, Image.GetPixelFormatSize(image.PixelFormat));
            Color[] grayPalette = PaletteUtils.GenerateGrayPalette(grayPfs, false, false);
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

        public static Color[] GetEightBitColorPalette(SixBitColor[] sixbitpalette)
        {
            Color[] eightbitpalette = new Color[sixbitpalette.Length];
            for (Int32 i = 0; i < sixbitpalette.Length; i++)
                eightbitpalette[i] = sixbitpalette[i].GetAsColor();
            return eightbitpalette;
        }

        public static SixBitColor[] GetSixBitColorPalette(Color[] eightbitpalette)
        {
            SixBitColor[] sixbitpalette = new SixBitColor[eightbitpalette.Length];
            for (Int32 i = 0; i < eightbitpalette.Length; i++)
                sixbitpalette[i] = new SixBitColor(eightbitpalette[i]);
            return sixbitpalette;
        }

        public static void WriteSixBitPaletteFile(Color[] palette, String palfilename)
        {
            SixBitColor[] newpal = GetSixBitColorPalette(palette);
            WriteSixBitPaletteFile(newpal, palfilename);
        }

        public static void WriteSixBitPaletteFile(SixBitColor[] palette, String palfilename)
        {
            Byte[] pal = GetSixBitPaletteData(palette);
            File.WriteAllBytes(palfilename, pal);
        }

        public static Byte[] GetSixBitPaletteData(SixBitColor[] palette)
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

        public static SixBitColor[] ReadSixBitPaletteFile(String palfilename)
        {
            Byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadSixBitPalette(readBytes);
        }

        public static SixBitColor[] ReadSixBitPalette(Byte[] paletteData)
        {
            const String invalid = "This is not a valid six-bit palette file.";
            if (paletteData.Length != 768)
                throw new ArgumentException(invalid);

            SixBitColor[] pal = new SixBitColor[256];
            try
            {
                for (Int32 i = 0; i < pal.Length; i++)
                {
                    Int32 index = i * 3;
                    pal[i] = new SixBitColor(paletteData[index], paletteData[index + 1], paletteData[index + 2]);
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
            if (dataLength %3 != 0)
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

        public static Int32 GetClosestPaletteIndexMatch(Color col, Color[] colorPalette, List<Int32> excludedindexes)
        {
            Int32 colorMatch = 0;
            Int32 leastDistance = Int32.MaxValue;
            Int32 red = col.R;
            Int32 green = col.G;
            Int32 blue = col.B;
            for (Int32 i = 0; i < colorPalette.Length; i++)
            {
                if (excludedindexes != null && excludedindexes.Contains(i))
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

    }
}
