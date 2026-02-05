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
        const string Invalid6bit = "This is not a valid six-bit palette file.";
        const string Invalid8bit = "This is not a valid eight-bit palette file.";

        public static Color ColorFromUInt(uint argb)
        {
            return Color.FromArgb((byte)((argb >> 0x18) & 0xFF), (byte)((argb >> 0x10) & 0xFF), (byte)((argb >> 0x08) & 0xFF), (byte)(argb & 0xFF));
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
            return Color.FromArgb(color.A, Color.FromArgb((int)(0x00FFFFFFu ^ (uint)color.ToArgb())));
        }

        public static bool HasGrayPalette(Bitmap image)
        {
            PixelFormat pf = image.PixelFormat;
            if (pf != PixelFormat.Format1bppIndexed && pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                return false;
            int grayPfs = Math.Min(8, Image.GetPixelFormatSize(image.PixelFormat));
            Color[] grayPalette = PaletteUtils.GenerateGrayPalette(grayPfs, null, false);
            Color[] pal = image.Palette.Entries;
            if (pal.Length != grayPalette.Length)
                return false;
            for (int i = 0; i < 256; ++i)
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
            byte averageR = (byte)Math.Max(0, Math.Min(255, Math.Min(col1.R, col2.R) + Math.Abs(col1.R - col2.R) / 2));
            byte averageG = (byte)Math.Max(0, Math.Min(255, Math.Min(col1.G, col2.G) + Math.Abs(col1.G - col2.G) / 2));
            byte averageB = (byte)Math.Max(0, Math.Min(255, Math.Min(col1.B, col2.B) + Math.Abs(col1.B - col2.B) / 2));
            return Color.FromArgb(averageR, averageG, averageB);
        }

        public static byte[] GetSixBitPaletteData(Color[] palette)
        {
            return GetSixBitPaletteData(palette, false);
        }

        public static byte[] GetSixBitPaletteData(Color[] palette, bool expandTo256)
        {
            int end = expandTo256 ? 256 : Math.Min(256, palette.Length);
            byte[] pal = new byte[end * 3];
            int writeIndex = 0;
            for (int i = 0; i < end; ++i)
            {
                Color col = i < palette.Length ? palette[i] : Color.Black;
                PixelFormatter.Format6BitVgaPal.WriteColor(pal, writeIndex, col);
                writeIndex += 3;
            }
            return pal;
        }

        public static void WriteSixBitPaletteFile(Color[] palette, String palfilename)
        {
            byte[] newpal = GetSixBitPaletteData(palette);
            File.WriteAllBytes(palfilename, newpal);
        }

        public static void WriteEightBitPaletteFile(Color[] palette, String palfilename, bool expandTo256)
        {
            byte[] bytes = GetEightBitPaletteData(palette, expandTo256);
            File.WriteAllBytes(palfilename, bytes);
        }

        public static byte[] GetEightBitPaletteData(Color[] palette)
        {
            return GetEightBitPaletteData(palette, false);
        }

        public static byte[] GetEightBitPaletteData(Color[] palette, bool expandTo256)
        {
            int end = expandTo256 ? 256 : Math.Min(256, palette.Length);
            byte[] pal = new byte[end * 3];
            int writeIndex = 0;
            for (int i = 0; i < end; ++i)
            {
                Color col = i < palette.Length ? palette[i] : Color.Black;
                PixelFormatter.Format8BitVgaPal.WriteColor(pal, writeIndex, col);
                writeIndex += 3;
            }
            return pal;
        }

        public static Color[] ReadSixBitPaletteFile(String palfilename, bool readFull)
        {
            byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadSixBitPaletteFile(readBytes, readFull);
        }

        public static Color[] ReadSixBitPaletteFile(byte[] paletteData, bool readFull)
        {
            int dataLength = paletteData.Length;
            if (dataLength % 3 != 0 || dataLength > 0x300)
                throw new ArgumentException(Invalid6bit);
            return ReadEightBitPalette(paletteData, 0, readFull ? 0x100 : dataLength / 3);
        }

        public static Color[] ReadSixBitPalette(byte[] paletteData)
        {
            return ReadSixBitPalette(paletteData, 0, 0x100);
        }

        public static Color[] ReadSixBitPalette(byte[] paletteData, int start)
        {
            return ReadSixBitPalette(paletteData, start, 0x100);
        }

        public static Color[] ReadSixBitPalette(byte[] paletteData, int start, bool autoSize)
        {
            return ReadSixBitPalette(paletteData, start, autoSize ? Math.Min(0x100, paletteData.Length / 3) : 0x100);
        }

        public static Color[] ReadSixBitPalette(byte[] paletteData, int start, int colors)
        {
            colors = Math.Min(0x100, Math.Max(0, colors));
            int fullLen = colors * 3;
            int end = start + fullLen;
            if (end > paletteData.Length)
                throw new ArgumentException(Invalid6bit);
            for (int i = start; i < end; ++i)
            {
                if (paletteData[i] > 0x3F)
                    throw new ArgumentException(Invalid6bit, "paletteData");
            }
            return PixelFormatter.Format6BitVgaPal.GetColorPalette(paletteData, start, colors);
        }

        public static Color[] ReadEightBitPaletteFile(string palfilename, bool readFull)
        {
            byte[] readBytes = File.ReadAllBytes(palfilename);
            return ReadEightBitPaletteFile(readBytes, readFull);
        }

        public static Color[] ReadEightBitPaletteFile(byte[] paletteData, bool readFull)
        {
            int dataLength = paletteData.Length;
            if (dataLength % 3 != 0 || dataLength > 0x300)
                throw new ArgumentException(Invalid8bit);
            return ReadEightBitPalette(paletteData, 0, readFull ? 0x100 : dataLength / 3);
        }

        public static Color[] ReadEightBitPalette(byte[] paletteData)
        {
            return ReadEightBitPalette(paletteData, 0, 0x100);
        }

        public static Color[] ReadEightBitPalette(byte[] paletteData, int start, bool autoSize)
        {
            return ReadEightBitPalette(paletteData, start, autoSize ? Math.Min(0x100, paletteData.Length / 3) : 0x100);
        }

        public static Color[] ReadEightBitPalette(byte[] paletteData, int colors)
        {
            return ReadEightBitPalette(paletteData, 0, colors);
        }

        public static Color[] ReadEightBitPalette(byte[] paletteData, int start, int colors)
        {
            colors = Math.Min(0x100, Math.Max(0, colors));
            int fullLen = colors * 3;
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
        public static int GetClosestPaletteIndexMatch(Color col, Color[] colorPalette, IEnumerable<int> excludedindices = null)
        {
            int palLength = colorPalette.Length;
            // Much more efficient than performing List.Contains() on every iteration.
            bool[] dontMatch = excludedindices == null ? null : new bool[palLength];
            if (excludedindices != null)
                foreach (int val in excludedindices)
                    if (val >= 0 && val < palLength)
                        dontMatch[val] = true;
            int colorMatch = 0;
            int leastDistance = int.MaxValue;
            int red = col.R;
            int green = col.G;
            int blue = col.B;
            for (int i = 0; i < palLength; ++i)
            {
                if (dontMatch != null && dontMatch[i])
                    continue;
                Color paletteColor = colorPalette[i];
                int redDistance = paletteColor.R - red;
                int greenDistance = paletteColor.G - green;
                int blueDistance = paletteColor.B - blue;
                // Technically, Pythagorean distance needs to have a root taken of the result, but this is not needed for just comparing them.
                int distance = (redDistance * redDistance) + (greenDistance * greenDistance) + (blueDistance * blueDistance);
                if (distance >= leastDistance)
                    continue;
                colorMatch = i;
                leastDistance = distance;
                if (distance == 0)
                    return i;
            }
            return colorMatch;
        }

        public static Color ColorFromHexString(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr))
                return Color.Empty;
            colorStr = colorStr.TrimStart('#').ToUpperInvariant();
            if (!Regex.IsMatch(colorStr, "[0-9A-F]+"))
                return Color.Empty;
            int len = colorStr.Length;
            if (len != 3 && len != 4 && len != 6 && len != 8)
                return Color.Empty;
            int red;
            int green;
            int blue;
            int alpha;
            if (len <= 4)
            {
                int startIndex = len == 3 ? 0 : 1;
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
                uint argb = UInt32.Parse(colorStr, NumberStyles.HexNumber);
                if (len == 6)
                    argb += 0xFF000000;
                return Color.FromArgb((int)argb);
            }
        }

        public static string HexStringFromColor(Color color, bool withAlpha)
        {
            uint colVal = (uint) color.ToArgb();
            if (!withAlpha)
                colVal = colVal & 0xFFFFFF;
            return "#" + colVal.ToString(withAlpha ? "X8" : "X6");
        }

    }
}
