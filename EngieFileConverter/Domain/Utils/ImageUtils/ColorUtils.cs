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
        const String Invalid6bit = "This is not a valid six-bit palette file.";
        const String Invalid8bit = "This is not a valid eight-bit palette file.";

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

        public static Byte[] GetSixBitPaletteData(Color[] palette)
        {
            return GetSixBitPaletteData(palette, false);
        }

        public static Byte[] GetSixBitPaletteData(Color[] palette, Boolean expandTo256)
        {
            Int32 end = expandTo256 ? 256 : Math.Min(256, palette.Length);
            Byte[] pal = new Byte[end * 3];
            Int32 writeIndex = 0;
            for (Int32 i = 0; i < end; ++i)
            {
                Color col = i < palette.Length ? palette[i] : Color.Black;
                PixelFormatter.Format6BitVgaPal.WriteColor(pal, writeIndex, col);
                writeIndex += 3;
            }
            return pal;
        }

        public static void WriteSixBitPaletteFile(Color[] palette, String palfilename)
        {
            Byte[] newpal = GetSixBitPaletteData(palette);
            File.WriteAllBytes(palfilename, newpal);
        }

        public static void WriteEightBitPaletteFile(Color[] palette, String palfilename, Boolean expandTo256)
        {
            Byte[] bytes = GetEightBitPaletteData(palette, expandTo256);
            File.WriteAllBytes(palfilename, bytes);
        }

        public static Byte[] GetEightBitPaletteData(Color[] palette)
        {
            return GetEightBitPaletteData(palette, false);
        }

        public static Byte[] GetEightBitPaletteData(Color[] palette, Boolean expandTo256)
        {
            Int32 end = expandTo256 ? 256 : Math.Min(256, palette.Length);
            Byte[] pal = new Byte[end * 3];
            Int32 writeIndex = 0;
            for (Int32 i = 0; i < end; ++i)
            {
                Color col = i < palette.Length ? palette[i] : Color.Black;
                PixelFormatter.Format8BitVgaPal.WriteColor(pal, writeIndex, col);
                writeIndex += 3;
            }
            return pal;
        }

        public static Color[] ReadSixBitPaletteFile(String palfilename, Boolean readFull)
        {
            Byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadSixBitPaletteFile(readBytes, readFull);
        }

        public static Color[] ReadSixBitPaletteFile(Byte[] paletteData, Boolean readFull)
        {
            Int32 dataLength = paletteData.Length;
            if (dataLength % 3 != 0 || dataLength > 0x300)
                throw new ArgumentException(Invalid6bit);
            return ReadEightBitPalette(paletteData, 0, readFull ? 0x100 : dataLength / 3);
        }

        public static Color[] ReadSixBitPalette(Byte[] paletteData)
        {
            return ReadSixBitPalette(paletteData, 0, 0x100);
        }

        public static Color[] ReadSixBitPalette(Byte[] paletteData, Int32 start)
        {
            return ReadSixBitPalette(paletteData, start, 0x100);
        }

        public static Color[] ReadSixBitPalette(Byte[] paletteData, Int32 start, Boolean autoSize)
        {
            return ReadSixBitPalette(paletteData, start, autoSize ? Math.Min(0x100, paletteData.Length / 3) : 0x100);
        }

        public static Color[] ReadSixBitPalette(Byte[] paletteData, Int32 start, Int32 colors)
        {
            colors = Math.Min(0x100, Math.Max(0, colors));
            Int32 fullLen = colors * 3;
            if (start + fullLen > paletteData.Length)
                throw new ArgumentException(Invalid6bit);
            for (Int32 i = 0; i < fullLen; ++i)
            {
                if (paletteData[i] > 0x3F)
                    throw new ArgumentException(Invalid6bit, "paletteData");
            }
            return PixelFormatter.Format6BitVgaPal.GetColorPalette(paletteData, start, colors);
        }

        public static Color[] ReadEightBitPaletteFile(String palfilename, Boolean readFull)
        {
            Byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadEightBitPaletteFile(readBytes, readFull);
        }

        public static Color[] ReadEightBitPaletteFile(Byte[] paletteData, Boolean readFull)
        {
            Int32 dataLength = paletteData.Length;
            if (dataLength % 3 != 0 || dataLength > 0x300)
                throw new ArgumentException(Invalid8bit);
            return ReadEightBitPalette(paletteData, 0, readFull ? 0x100 : dataLength / 3);
        }

        public static Color[] ReadEightBitPalette(Byte[] paletteData)
        {
            return ReadEightBitPalette(paletteData, 0, 0x100);
        }

        public static Color[] ReadEightBitPalette(Byte[] paletteData, Int32 start, Boolean autoSize)
        {
            return ReadEightBitPalette(paletteData, start, autoSize ? Math.Min(0x100, paletteData.Length / 3) : 0x100);
        }

        public static Color[] ReadEightBitPalette(Byte[] paletteData, Int32 colors)
        {
            return ReadEightBitPalette(paletteData, 0, colors);
        }

        public static Color[] ReadEightBitPalette(Byte[] paletteData, Int32 start, Int32 colors)
        {
            colors = Math.Min(0x100, Math.Max(0, colors));
            Int32 fullLen = colors * 3;
            if (start + fullLen > paletteData.Length)
                throw new ArgumentException(Invalid8bit);
            return PixelFormatter.Format8BitVgaPal.GetColorPalette(paletteData, start, colors);
        }

        /// <summary>
        /// Uses Pythagorean distance in 3D color space to find the closest match to a given color on
        /// a given color palette, and returns the index on the palette at which that match was found.
        /// </summary>
        /// <param name="col">The color to find the closest match to</param>
        /// <param name="colorPalette">The palette of available colors to match</param>
        /// <param name="excludedindices">List of palette indices that are specifically excluded from the search.</param>
        /// <returns>The index on the palette of the color that is the closest to the given color.</returns>
        public static Int32 GetClosestPaletteIndexMatch(Color col, Color[] colorPalette, IEnumerable<Int32> excludedindices = null)
        {
            Int32 palLength = colorPalette.Length;
            // Much more efficient than performing List.Contains() on every iteration.
            Boolean[] dontMatch = excludedindices == null ? null : new Boolean[palLength];
            if (excludedindices != null)
                foreach (Int32 val in excludedindices)
                    if (val >= 0 && val < palLength)
                        dontMatch[val] = true;
            Int32 colorMatch = 0;
            Int32 leastDistance = Int32.MaxValue;
            Int32 red = col.R;
            Int32 green = col.G;
            Int32 blue = col.B;
            for (Int32 i = 0; i < palLength; ++i)
            {
                if (dontMatch != null && dontMatch[i])
                    continue;
                Color paletteColor = colorPalette[i];
                Int32 redDistance = paletteColor.R - red;
                Int32 greenDistance = paletteColor.G - green;
                Int32 blueDistance = paletteColor.B - blue;
                // Technically, Pythagorean distance needs to have a root taken of the result, but this is not needed for just comparing them.
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
