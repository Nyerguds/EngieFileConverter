using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Nyerguds.ImageManipulation
{
    public static class PaletteUtils
    {
        public static Boolean[] MakeTransparencyGuide(Int32 bpp, Int32[] transparentIndices)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Boolean[] tranGuide = new Boolean[palLen];
            foreach (Int32 b in transparentIndices)
            {
                if (b < tranGuide.Length)
                    tranGuide[b] = true;
            }
            return tranGuide;
        }

        public static Boolean[] MakeTransparencyGuide(Int32 bpp, Int32 transparentColor)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Boolean[] tranGuide = new Boolean[palLen];
            if (transparentColor < tranGuide.Length)
                tranGuide[transparentColor] = true;
            return tranGuide;
        }

        public static Boolean[] MakeTransparencyGuide(Int32 bpp, Color[] palette)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Boolean[] tranGuide = new Boolean[palLen];
            if (palette != null)
            {
                Int32 len = Math.Min(palLen, palette.Length);
                for (Int32 i = 0; i < len; i++)
                    tranGuide[i] = palette[i].A < 128;
            }
            return tranGuide;
        }

        private static Boolean[] PrepareTransparencyGuide(Boolean[] transparencyGuide, Int32 targetPalLen)
        {
            Boolean[] newTransparencyGuide = new Boolean[targetPalLen];
            if (transparencyGuide != null)
                Array.Copy(transparencyGuide, 0, newTransparencyGuide, 0, Math.Min(transparencyGuide.Length, targetPalLen));
            return newTransparencyGuide;
        }

        public static Color[] ApplyTransparencyGuide(Color[] palette, Boolean[] transparencyGuide)
        {
            transparencyGuide = PrepareTransparencyGuide(transparencyGuide, palette.Length);
            for (Int32 i = 0; i < palette.Length; i++)
                palette[i] = Color.FromArgb(transparencyGuide[i] ? 0x00 : 0xFF, palette[i]);
            return palette;
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="pixelFormat">Pixel format for which to generate the new palette</param>
        /// <param name="transparencyGuide">Array of booleans specifying which indices to make transparent.</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, PixelFormat pixelFormat, Boolean[] transparencyGuide)
        {
            return MakePalette(sourcePalette, pixelFormat, transparencyGuide, null);
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="pixelFormat">Pixel format for which to generate the new palette</param>
        /// <param name="transparencyGuide">Array of booleans specifying which indices to make transparent.</param>
        /// <param name="defaultColor">Default colour if the source palette is smaller than the returned palette. If not filled in, leftover colors will be Color.Empty</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, PixelFormat pixelFormat, Boolean[] transparencyGuide, Color? defaultColor)
        {
            Int32 bpp = Image.GetPixelFormatSize(pixelFormat);
            return MakePalette(sourcePalette, bpp, transparencyGuide, defaultColor);
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="bpp">Bits per pixel for which to generate the new palette</param>
        /// <param name="transparencyGuide">Array of booleans specifying which indices to make transparent.</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, Int32 bpp, Boolean[] transparencyGuide)
        {
            return MakePalette(sourcePalette, bpp, transparencyGuide, null);
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="bpp">Bits per pixel for which to generate the new palette</param>
        /// <param name="transparencyGuide">Array of booleans specifying which indices to make transparent.</param>
        /// <param name="defaultColor">Default colour if the source palette is smaller than the returned palette. If not filled in, leftover colors will be Color.Empty</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, Int32 bpp, Boolean[] transparencyGuide, Color? defaultColor)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Color[] pal = new Color[palLen];
            transparencyGuide = PrepareTransparencyGuide(transparencyGuide, palLen);
            for (Int32 i = 0; i < palLen; i++)
            {
                Color col;
                if (sourcePalette != null && i < sourcePalette.Length)
                    col = sourcePalette[i];
                else if (defaultColor.HasValue)
                    col = defaultColor.Value;
                else
                    col = Color.Empty;
                pal[i] = Color.FromArgb(transparencyGuide[i] ? 0x00 : 0xFF, col);
            }
            return pal;
        }

        public static Color[] GenerateGrayPalette(Int32 bpp, Boolean[] transparencyGuide, Boolean reverseGenerated)
        {
            Int32 palLen = 1 << bpp;
            Color[] pal = new Color[palLen];
            transparencyGuide = PrepareTransparencyGuide(transparencyGuide, palLen);
            // generate greyscale palette.
            Int32 steps = 255 / (palLen - 1);
            for (Int32 i = 0; i < pal.Length; i++)
            {
                Double curval = reverseGenerated ? pal.Length - 1 - i : i;
                Byte grayval = (Byte)Math.Min(255, Math.Round(curval * steps, MidpointRounding.AwayFromZero));
                pal[i] = Color.FromArgb(transparencyGuide == null ? 255 : transparencyGuide[i] ? 0x00 : 0xFF, grayval, grayval, grayval);
            }
            return pal;
        }

        public static Color[] GenerateDefWindowsPalette(Int32 bpp, Boolean[] transparencyGuide, Boolean reverseGenerated)
        {
            Color[] pal;
            using (Bitmap bm = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
                pal = bm.Palette.Entries;
            for (Int32 i = 0; i < pal.Length; i++)
                if (pal[i].A < 0xFF)
                    pal[i] = Color.FromArgb(0xFF, pal[i]);
            // Cut down to requested size
            pal = MakePalette(pal, bpp, null, Color.Black);
            // Reverse after cutting since otherwise we won't get the default 16 color palette.
            if (reverseGenerated)
            {
                Color[] entries = pal.Reverse().ToArray();
                for (Int32 i = 0; i < pal.Length; i++)
                    pal[i] = entries[i];
            }
            // Apply transparency and return
            return ApplyTransparencyGuide(pal, transparencyGuide);
        }

        public static Color[] GenerateDoubleRainbow(Int32 blackIndex, Boolean[] transparencyGuide, Boolean reverseGenerated)
        {
            Color[] smallPal = GenerateRainbowPalette(4, blackIndex, null, reverseGenerated);
            Color[] bigPal = GenerateRainbowPalette(8, blackIndex, null, reverseGenerated);
            Array.Copy(smallPal, 0, bigPal, 0, smallPal.Length);
            return ApplyTransparencyGuide(bigPal, transparencyGuide);
        }

        public static Color[] GenerateRainbowPalette(Int32 bpp, Int32 blackIndex, Boolean[] transparencyGuide, Boolean reverseGenerated)
        {
            return GenerateRainbowPalette(bpp, blackIndex, transparencyGuide, reverseGenerated, 0, (Int32)ColorHSL.SCALE, false);
        }

        /// <summary>
        /// Generates a colour palette of the given bits per pixel containing a hue rotation of the given range.
        /// </summary>
        /// <param name="bpp">Bits per pixel of the image the palette is for.</param>
        /// <param name="blackIndex">Index on the palette to replace with black.</param>
        /// <param name="transparencyGuide">Array with booleans indicating which indices should become transparent.</param>
        /// <param name="reverseGenerated">Reverse the generated range. This happens after the generating, and before the operations on the first index/</param>
        /// <param name="startHue">Start hue range. Value from 0 to 240.</param>
        /// <param name="endHue">End hue range. Value from 0 to 240. Must be higher then startHue.</param>
        /// <param name="inclusiveEnd">True to include the end hue in the palette. If you generate a full hue range, this can be set to False to avoid getting a duplicate red colour on it.</param>
        /// <returns>The generated palette, as array of System.Drawing.Color objects.</returns>
        public static Color[] GenerateRainbowPalette(Int32 bpp, Int32 blackIndex, Boolean[] transparencyGuide, Boolean reverseGenerated, Int32 startHue, Int32 endHue, Boolean inclusiveEnd)
        {
            Int32 colors = 1 << bpp;
            Color[] pal = new Color[colors];
            Double step = (Double)(endHue - startHue) / (inclusiveEnd ? colors - 1 : colors);
            Double start = startHue;
            Double satValue = ColorHSL.SCALE;
            Double lumValue = 0.5 * ColorHSL.SCALE;
            for (Int32 i = 0; i < colors; i++)
            {
                if (i + 1 == colors)
                {
                    i++;
                    i--;
                }
                Double curStep = start + step * i;
                pal[i] = new ColorHSL(curStep, satValue, lumValue);
            }
            if (reverseGenerated)
            {
                Color[] entries = pal.Reverse().ToArray();
                for (Int32 i = 0; i < pal.Length; i++)
                    pal[i] = entries[i];
            }
            if (blackIndex >= 0 && blackIndex < colors)
                pal[blackIndex] = Color.Black;
            // Apply transparency
            return ApplyTransparencyGuide(pal, transparencyGuide);
        }
    }
}
