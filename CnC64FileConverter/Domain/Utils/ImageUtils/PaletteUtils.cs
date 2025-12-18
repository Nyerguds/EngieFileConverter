using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace Nyerguds.ImageManipulation
{
    public static class PaletteUtils
    {

        public static PixelFormat GetPalettedFormat(Int32 bitsPerPixel)
        {
            switch (bitsPerPixel)
            {
                case 1:
                    return PixelFormat.Format1bppIndexed;
                case 4:
                    return PixelFormat.Format4bppIndexed;
                case 8:
                    return PixelFormat.Format8bppIndexed;
            }
            throw new NotSupportedException("No indexed PixelFormat available for " + bitsPerPixel + " bpp.");
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="pixelFormat">Pixel format for which to generate the new palette</param>
        /// <param name="addTransparentZero">True to make index #0 transparent</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, PixelFormat pixelFormat, Boolean addTransparentZero)
        {
            return MakePalette(sourcePalette, pixelFormat, addTransparentZero, null);
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="pixelFormat">Pixel format for which to generate the new palette</param>
        /// <param name="addTransparentZero">True to make index #0 transparent</param>
        /// <param name="defaultColor">Default colour if the source palette is smaller than the returned palette. If not filled in, leftover colors will be Color.Empty</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, PixelFormat pixelFormat, Boolean addTransparentZero, Color? defaultColor)
        {
            Int32 bpp = Image.GetPixelFormatSize(pixelFormat);
            return MakePalette(sourcePalette, bpp, addTransparentZero, defaultColor);
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="bpp">Bits per pixel for which to generate the new palette</param>
        /// <param name="addTransparentZero">True to make index #0 transparent</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, Int32 bpp, Boolean addTransparentZero)
        {
            return MakePalette(sourcePalette, bpp, addTransparentZero, null);
        }

        /// <summary>
        /// Creates a new palette with the full amount of colour for the given bits per pixel value, and pours the given colours into it.
        /// </summary>
        /// <param name="sourcePalette">Source colours</param>
        /// <param name="bpp">Bits per pixel for which to generate the new palette</param>
        /// <param name="addTransparentZero">True to make index #0 transparent</param>
        /// <param name="defaultColor">Default colour if the source palette is smaller than the returned palette. If not filled in, leftover colors will be Color.Empty</param>
        /// <returns>The new palette</returns>
        public static Color[] MakePalette(Color[] sourcePalette, Int32 bpp, Boolean addTransparentZero, Color? defaultColor)
        {
            Int32 palLen = bpp > 8 ? 0 : 1 << bpp;
            Color[] pal = new Color[palLen];
            for (Int32 i = 0; i < palLen; i++)
            {
                if (sourcePalette != null && i < sourcePalette.Length)
                    pal[i] = sourcePalette[i];
                else if (defaultColor.HasValue)
                    pal[i] = defaultColor.Value;
                else
                    pal[i] = Color.Empty;
            }
            // make color 0 transparent
            if (addTransparentZero && palLen > 0)
                pal[0] = Color.FromArgb(0, pal[0]);
            return pal;
        }

        public static Color[] GenerateGrayPalette(Int32 bpp, Boolean addTransparentZero, Boolean reverseGenerated)
        {
            Int32 palSize = 1 << bpp;
            Color[] pal = new Color[palSize];
            // generate greyscale palette.
            Double step = 255.0 / (palSize - 1);
            for (Int32 i = 0; i < palSize; i++)
            {
                Double curval = reverseGenerated ? pal.Length - 1 - i : i;
                Byte grayval = (Byte)Math.Min(255, Math.Round(curval * step, MidpointRounding.AwayFromZero));
                pal[i] = Color.FromArgb(255, grayval, grayval, grayval);
            }
            // make color 0 transparent
            if (addTransparentZero)
                pal[0] = Color.FromArgb(0, pal[0]);
            return pal;
        }

        public static Color[] GenerateDefWindowsPalette(Int32 bpp, Boolean addTransparentZero, Boolean reverseGenerated)
        {
            Color[] pal;
            using (Bitmap bm = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
                pal = bm.Palette.Entries;
            for (Int32 i = 0; i < pal.Length; i++)
                if (pal[i].A < 0xFF)
                    pal[i] = Color.FromArgb(0xFF, pal[i]);
            // Cut down to requested size
            pal = MakePalette(pal, bpp, false, Color.Black);
            // Reverse after cutting since otherwise we won't get the default 16 color palette.
            if (reverseGenerated)
            {
                Color[] entries = pal.Reverse().ToArray();
                for (Int32 i = 0; i < pal.Length; i++)
                    pal[i] = entries[i];
            }
            // make color 0 transparent
            if (addTransparentZero)
                pal[0] = Color.FromArgb(0, pal[0]);
            return pal;
        }

        public static Color[] GenerateDoubleRainbow(Boolean blackOnZero, Boolean addTransparentZero, Boolean reverseGenerated)
        {
            Color[] smallPal = GenerateRainbowPalette(4, blackOnZero, addTransparentZero, reverseGenerated);
            Color[] bigPal = GenerateRainbowPalette(8, blackOnZero, addTransparentZero, reverseGenerated);
            Array.Copy(smallPal, 0, bigPal, 0, smallPal.Length);
            return bigPal;
        }

        public static Color[] GenerateRainbowPalette(Int32 bpp, Boolean blackOnZero, Boolean addTransparentZero, Boolean reverseGenerated)
        {
            return GenerateRainbowPalette(bpp, blackOnZero, addTransparentZero, reverseGenerated, 0, (Int32)ColorHSL.SCALE, false);
        }

        /// <summary>
        /// Generates a colour palette of the given bits per pixel containing a hue rotation of the given range.
        /// </summary>
        /// <param name="bpp">Bits per pixel of the image the palette is for.</param>
        /// <param name="blackOnZero">Replace the first colour on the palette with black.</param>
        /// <param name="addTransparentZero">Make the first colour on the palette transparent.</param>
        /// <param name="reverseGenerated">Reverse the generated range. This happens after the generating, and before the operations on the first index/</param>
        /// <param name="startHue">Start hue range. Value from 0 to 240.</param>
        /// <param name="endHue">End hue range. Value from 0 to 240. Must be higher then startHue.</param>
        /// <param name="inclusiveEnd">True to include the end hue in the palette. If you generate a full hue range, this can be set to False to avoid getting a duplicate red colour on it.</param>
        /// <returns>The generated palette, as array of System.Drawing.Color objects.</returns>
        public static Color[] GenerateRainbowPalette(Int32 bpp, Boolean blackOnZero, Boolean addTransparentZero, Boolean reverseGenerated, Int32 startHue, Int32 endHue, Boolean inclusiveEnd)
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
                Double curStep =  start + step * i;
                pal[i] = new ColorHSL(curStep, satValue, lumValue);
            }
            if (reverseGenerated)
            {
                Color[] entries = pal.Reverse().ToArray();
                for (Int32 i = 0; i < pal.Length; i++)
                    pal[i] = entries[i];
            }
            if (blackOnZero)
                pal[0] = Color.Black;
            // make color 0 transparent
            if (addTransparentZero)
                pal[0] = Color.FromArgb(0, pal[0]);
            return pal;
        }
    }
}
