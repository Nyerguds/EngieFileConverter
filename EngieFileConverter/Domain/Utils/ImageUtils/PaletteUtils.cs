using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.Util;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Class containing palette generating utilities.
    /// </summary>
    public static class PaletteUtils
    {
        public static ColorPalette GetColorPalette(Color[] colors, PixelFormat pf)
        {
            ColorPalette cp;
            using (Bitmap bm = new Bitmap(1, 1, pf))
                cp = bm.Palette;
            Int32 len = Math.Min(cp.Entries.Length, colors.Length);
            for (Int32 i = 0; i < len; ++i)
                cp.Entries[i] = colors[i];
            return cp;
        }

        /// <summary>
        /// The standard EGA/CGA palette.
        /// </summary>
        private static readonly Color[] EgaPalette =
        {
            Color.FromArgb(0x00, 0x00, 0x00), // black
            Color.FromArgb(0x00, 0x00, 0xAA), // blue
            Color.FromArgb(0x00, 0xAA, 0x00), // green
            Color.FromArgb(0x00, 0xAA, 0xAA), // cyan
            Color.FromArgb(0xAA, 0x00, 0x00), // red
            Color.FromArgb(0xAA, 0x00, 0xAA), // magenta
            Color.FromArgb(0xAA, 0x55, 0x00), // yellow / brown
            Color.FromArgb(0xAA, 0xAA, 0xAA), // white / light gray
            Color.FromArgb(0x55, 0x55, 0x55), // dark gray / bright black
            Color.FromArgb(0x55, 0x55, 0xFF), // bright blue
            Color.FromArgb(0x55, 0xFF, 0x55), // bright green
            Color.FromArgb(0x55, 0xFF, 0xFF), // bright cyan
            Color.FromArgb(0xFF, 0x55, 0x55), // bright red
            Color.FromArgb(0xFF, 0x55, 0xFF), // bright magenta
            Color.FromArgb(0xFF, 0xFF, 0x55), // bright yellow
            Color.FromArgb(0xFF, 0xFF, 0xFF), // bright white
        };

        private static readonly Byte[][] CgaPalettes =
        {
            new Byte[] {2, 4, 6}, // Mode 4, palette 0
            new Byte[] {3, 5, 7}, // Mode 4, palette 1
            new Byte[] {3, 4, 7}, // Mode 5 palette
        };

        public static Color[] GetEgaPalette()
        {
            return ArrayUtils.CloneArray(EgaPalette);
        }

        public static Color[] GetEgaPalette(Int32 bitsPerPixel)
        {
            Int32 colors = 1 << bitsPerPixel;
            if (colors > 16 || colors <= 0)
                throw new ArgumentException("EGA palette can not contain more than 16 colors!", "bitsPerPixel");
            Color[] pal = new Color[colors];
            Array.Copy(EgaPalette, 0, pal, 0, colors);
            return pal;
        }

        public static Color[] GetCgaPalette(Byte backgroundColor, Boolean colorBurst, Boolean palette, Boolean intensity, Int32 bitsPerPixel)
        {
            if (backgroundColor > 15)
                throw new ArgumentException("CGA palette values only go up to 15!", "backgroundColor");
            Color[] pal = new Color[1 << bitsPerPixel];
            pal[0] = EgaPalette[backgroundColor];
            if (bitsPerPixel == 1)
            {
                pal[1] = EgaPalette[0];
                return pal;
            }
            if (bitsPerPixel != 2)
                throw new ArgumentException("CGA palette can only be 1bpp or 2bpp!","bitsPerPixel");
            Int32 paletteNr = colorBurst ? (palette ? 1 : 0) : 2;
            Byte[] colors = CgaPalettes[paletteNr];
            Int32 intensityAdd = intensity ? 8 : 0;
            for (Int32 i = 0; i < 3; ++i)
            {
                Int32 cgacol = colors[i] | intensityAdd;
                pal[i + 1] = EgaPalette[cgacol];
            }
            return pal;
        }

        public static Boolean DetectCgaPalette(Color[] palEntries, out Byte backgroundColor, out Boolean colorBurst, out Boolean palette, out Boolean intensity)
        {
            Int32 colors = palEntries.Length;
            colorBurst = false;
            palette = false;
            intensity = false;
            backgroundColor = GetEgaIndex(palEntries[0]);
            if (backgroundColor == 0xFF)
            {
                backgroundColor = 0;
                return false;
            }
            if (colors == 2)
                return palEntries[1].R == 0 && palEntries[1].G == 0 && palEntries[1].B == 0;
            if (colors != 4)
                return false;
            for (Int32 opts = 0; opts < 6; ++opts)
            {
                Boolean palMatch = true;
                // Switched colorburst so it would come last.
                Color[] cgaPal = GetCgaPalette(backgroundColor, (opts & 4) == 0, (opts & 2) == 1, (opts & 1) == 1, 2);
                for (Int32 i = 1; i < 4; ++i)
                {
                    if (Color.FromArgb(palEntries[i].R, palEntries[i].G, palEntries[i].B) == cgaPal[i])
                        continue;
                    palMatch = false;
                    break;
                }
                if (!palMatch)
                    continue;
                colorBurst = (opts & 4) == 0;
                palette = (opts & 2) == 1;
                intensity = (opts & 1) == 1;
                return true;
            }
            return false;
        }

        public static Byte GetEgaIndex(Color col)
        {
            Color c = Color.FromArgb(col.R, col.G, col.B);
            for (Byte i = 0; i < 16; ++i)
            {
                if (EgaPalette[i].R != c.R || EgaPalette[i].G != c.G || EgaPalette[i].B != c.B)
                    continue;
                return i;
            }
            return 0xFF;
        }

        public static Boolean[] MakePalTransparencyMask(Int32 bpp, Int32[] transparentIndices)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Boolean[] transMask = new Boolean[palLen];
            Int32 transLen = transparentIndices.Length;
            for (Int32 i = 0; i < transLen; ++i)
            {
                Int32 b = transparentIndices[i];
                if (b < palLen)
                    transMask[b] = true;
            }
            return transMask;
        }

        public static Boolean[] MakePalTransparencyMask(Int32 bpp, Int32 transparentColor)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Boolean[] transMask = new Boolean[palLen];
            if (transparentColor < palLen)
                transMask[transparentColor] = true;
            return transMask;
        }

        public static Boolean[] MakePalTransparencyMask(Int32 bpp, Color[] palette)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Boolean[] transMask = new Boolean[palLen];
            if (palette == null)
                return transMask;
            Int32 len = Math.Min(palLen, palette.Length);
            for (Int32 i = 0; i < len; ++i)
                transMask[i] = palette[i].A < 128;
            return transMask;
        }

        private static Boolean[] PreparePalTransparencyMask(Boolean[] palTransparencyMask, Int32 targetPalLen)
        {
            Boolean[] newPalTransMask = new Boolean[targetPalLen];
            if (palTransparencyMask != null)
                Array.Copy(palTransparencyMask, 0, newPalTransMask, 0, Math.Min(palTransparencyMask.Length, targetPalLen));
            return newPalTransMask;
        }

        public static Color[] ApplyPalTransparencyMask(Color[] palette, Boolean[] palTransMask)
        {
            Int32 palLen = palette.Length;
            palTransMask = PreparePalTransparencyMask(palTransMask, palLen);
            for (Int32 i = 0; i < palLen; ++i)
                palette[i] = Color.FromArgb(palTransMask[i] ? 0x00 : 0xFF, palette[i]);
            return palette;
        }

        /// <summary>
        /// Creates a new palette with the full amount of color for the given bits per pixel value, and pours the given colors into it.
        /// </summary>
        /// <param name="sourcePalette">Source colors.</param>
        /// <param name="pixelFormat">Pixel format for which to generate the new palette.</param>
        /// <param name="palTransparencyMask">Array of booleans specifying which indices to make transparent.</param>
        /// <returns>The new palette.</returns>
        public static Color[] MakePalette(Color[] sourcePalette, PixelFormat pixelFormat, Boolean[] palTransparencyMask)
        {
            return MakePalette(sourcePalette, pixelFormat, palTransparencyMask, null);
        }

        /// <summary>
        /// Creates a new palette with the full amount of color for the given bits per pixel value, and pours the given colors into it.
        /// </summary>
        /// <param name="sourcePalette">Source colors.</param>
        /// <param name="pixelFormat">Pixel format for which to generate the new palette.</param>
        /// <param name="palTransparencyMask">Array of booleans specifying which indices to make transparent.</param>
        /// <param name="defaultColor">Default color if the source palette is smaller than the returned palette. If not filled in, leftover colors will be Color.Empty.</param>
        /// <returns>The new palette.</returns>
        public static Color[] MakePalette(Color[] sourcePalette, PixelFormat pixelFormat, Boolean[] palTransparencyMask, Color? defaultColor)
        {
            Int32 bpp = Image.GetPixelFormatSize(pixelFormat);
            return MakePalette(sourcePalette, bpp, palTransparencyMask, defaultColor);
        }

        /// <summary>
        /// Creates a new palette with the full amount of color for the given bits per pixel value, and pours the given colors into it.
        /// </summary>
        /// <param name="sourcePalette">Source colors.</param>
        /// <param name="bpp">Bits per pixel for which to generate the new palette.</param>
        /// <param name="palTransparencyMask">Array of booleans specifying which indices to make transparent.</param>
        /// <returns>The new palette.</returns>
        public static Color[] MakePalette(Color[] sourcePalette, Int32 bpp, Boolean[] palTransparencyMask)
        {
            return MakePalette(sourcePalette, bpp, palTransparencyMask, null);
        }

        /// <summary>
        /// Creates a new palette with the full amount of color for the given bits per pixel value, and pours the given colors into it.
        /// </summary>
        /// <param name="sourcePalette">Source colors.</param>
        /// <param name="bpp">Bits per pixel for which to generate the new palette.</param>
        /// <param name="palTransparencyMask">Array of booleans specifying which indices to make transparent.</param>
        /// <param name="defaultColor">Default color if the source palette is smaller than the returned palette. If not filled in, leftover colors will be Color.Empty.</param>
        /// <returns>The new palette.</returns>
        public static Color[] MakePalette(Color[] sourcePalette, Int32 bpp, Boolean[] palTransparencyMask, Color? defaultColor)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Color[] pal = new Color[palLen];
            palTransparencyMask = PreparePalTransparencyMask(palTransparencyMask, palLen);
            for (Int32 i = 0; i < palLen; ++i)
            {
                Color col;
                if (sourcePalette != null && i < sourcePalette.Length)
                    col = sourcePalette[i];
                else if (defaultColor.HasValue)
                    col = defaultColor.Value;
                else
                    col = Color.Empty;
                pal[i] = Color.FromArgb(palTransparencyMask[i] ? 0x00 : 0xFF, col);
            }
            return pal;
        }

        public static Color[] GenerateGrayPalette(Int32 bpp, Boolean[] palTransparencyMask, Boolean reverseGenerated)
        {
            Int32 palLen = 1 << bpp;
            Color[] pal = new Color[palLen];
            palTransparencyMask = PreparePalTransparencyMask(palTransparencyMask, palLen);
            // generate greyscale palette.
            Int32 steps = 255 / (palLen - 1);
            for (Int32 i = 0; i < palLen; ++i)
            {
                Double curval = reverseGenerated ? palLen - 1 - i : i;
                Byte grayval = (Byte)Math.Min(255, Math.Round(curval * steps, MidpointRounding.AwayFromZero));
                pal[i] = Color.FromArgb(palTransparencyMask == null ? 255 : palTransparencyMask[i] ? 0x00 : 0xFF, grayval, grayval, grayval);
            }
            return pal;
        }

        public static Color[] GenerateDefWindowsPalette(Int32 bpp, Boolean[] palTransparencyMask, Boolean reverseGenerated)
        {
            Color[] pal;
            using (Bitmap bm = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
                pal = bm.Palette.Entries;
            Int32 palLen = pal.Length;
            for (Int32 i = 0; i < palLen; ++i)
                if (pal[i].A < 0xFF)
                    pal[i] = Color.FromArgb(0xFF, pal[i]);
            // Cut down to requested size
            pal = MakePalette(pal, bpp, null, Color.Black);
            palLen = pal.Length;
            // Reverse after cutting since otherwise we won't get the default 16 color palette.
            if (reverseGenerated)
            {
                Color[] entries = pal.Reverse().ToArray();
                for (Int32 i = 0; i < palLen; ++i)
                    pal[i] = entries[i];
            }
            // Apply transparency and return
            return ApplyPalTransparencyMask(pal, palTransparencyMask);
        }

        public static Color[] GenerateDoubleRainbow(Int32 blackIndex, Boolean[] palTransparencyMask, Boolean reverseGenerated)
        {
            Color[] smallPal = GenerateRainbowPalette(4, blackIndex, null, reverseGenerated);
            Color[] bigPal = GenerateRainbowPalette(8, blackIndex, null, reverseGenerated);
            Array.Copy(smallPal, 0, bigPal, 0, smallPal.Length);
            return ApplyPalTransparencyMask(bigPal, palTransparencyMask);
        }

        public static Color[] GenerateRainbowPalette(Int32 bpp, Int32 blackIndex, Boolean[] palTransparencyMask, Boolean reverseGenerated)
        {
            return GenerateRainbowPalette(bpp, blackIndex, palTransparencyMask, reverseGenerated, 0, (Int32)ColorHSL.SCALE, false);
        }

        /// <summary>
        /// Generates a color palette of the given bits per pixel containing a hue rotation of the given range.
        /// </summary>
        /// <param name="bpp">Bits per pixel of the image the palette is for.</param>
        /// <param name="blackIndex">Index on the palette to replace with black.</param>
        /// <param name="palTransparencyMask">Array with booleans indicating which indices should become transparent.</param>
        /// <param name="reverseGenerated">Reverse the generated range. This happens after the generating, and before the operations on the first index/.</param>
        /// <param name="startHue">Start hue range. Value from 0 to 240.</param>
        /// <param name="endHue">End hue range. Value from 0 to 240. Must be higher then startHue.</param>
        /// <param name="inclusiveEnd">True to include the end hue in the palette. If you generate a full hue range, this can be set to False to avoid getting a duplicate red color on it.</param>
        /// <returns>The generated palette, as array of System.Drawing.Color objects.</returns>
        public static Color[] GenerateRainbowPalette(Int32 bpp, Int32 blackIndex, Boolean[] palTransparencyMask, Boolean reverseGenerated, Int32 startHue, Int32 endHue, Boolean inclusiveEnd)
        {
            Int32 colors = 1 << bpp;
            Color[] pal = new Color[colors];
            Double step = (Double)(endHue - startHue) / (inclusiveEnd ? colors - 1 : colors);
            Double start = startHue;
            Double satValue = ColorHSL.SCALE;
            Double lumValue = 0.5 * ColorHSL.SCALE;
            for (Int32 i = 0; i < colors; ++i)
            {
                Double curStep = start + step * i;
                pal[i] = new ColorHSL(curStep, satValue, lumValue);
            }
            if (reverseGenerated)
                pal = pal.Reverse().ToArray();
            if (blackIndex >= 0 && blackIndex < colors)
                pal[blackIndex] = Color.Black;
            // Apply transparency
            return ApplyPalTransparencyMask(pal, palTransparencyMask);
        }

        /// <summary>
        /// Palette compare. Replacement for SequenceEquals, since it's bloody slow, and with added support to ignore alpha.
        /// </summary>
        /// <param name="palette1"></param>
        /// <param name="palette2"></param>
        /// <param name="ignoreAlpha"></param>
        /// <returns></returns>
        internal static Boolean PalettesAreEqual(Color[] palette1, Color[] palette2, Boolean ignoreAlpha)
        {
            // Replacement for SequenceEquals, since it's bloody slow.
            Int32 pal1Length = palette1.Length;
            if (pal1Length != palette2.Length)
                return false;
            if (ignoreAlpha)
            {
                for (Int32 i = 0; i < pal1Length; ++i)
                {
                    if ((((UInt32)palette1[i].ToArgb()) & 0x00FFFFFF) == (((UInt32)palette2[i].ToArgb()) & 0x00FFFFFF))
                        continue;
                    return false;
                }
            }
            for (Int32 i = 0; i < pal1Length; ++i)
            {
                if (palette1[i].ToArgb() == palette2[i].ToArgb())
                    continue;
                return false;
            }
            return true;
        }
    }
}
