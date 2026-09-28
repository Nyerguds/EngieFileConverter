using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileTblWwPal : SupportedFileType
    {
        public override string IdCode { get { return "WwTbl"; } }
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        //public override Int32 Width { get { return m_Width; } }
        //public override Int32 Height { get { return m_Height; } }
        //protected Int32 m_Width = 0x101;
        //protected Int32 m_Height = 0x101;
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood PAL Table"; } }
        public override string[] FileExtensions { get { return new string[] {"pal"}; } }
        public override string LongTypeName  { get { return "Westwood Palette Stretch Table"; } }
        public override bool NeedsPalette  { get { return true; } }
        public override int BitsPerPixel  { get { return 8; } }
        public override bool[] TransparencyMask { get { return new bool[0]; } }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            return new Option[]
            {
                new Option("IGI", OptionInputType.String, "Exclude these color indices from the matching process", "0123456789;, " + Environment.NewLine, String.Empty),
                new Option("DUP", OptionInputType.Boolean, "Duplicate on excluded indices", String.Empty),
                new Option("IGM", OptionInputType.String, "Prohibit matching to these color indices", "0123456789;, " + Environment.NewLine, String.Empty)
            };
        }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Color[] cols = CheckInputForColors(fileToSave, this.BitsPerPixel, true);
            List<int> ignorelistInput = this.GetIndices(Option.GetSaveOptionValue(saveOptions, "IGI"));
            List<int> ignorelistMatch = this.GetIndices(Option.GetSaveOptionValue(saveOptions, "IGM"));
            bool dupOnExcluded = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "DUP"));
            return GenerateInterlaceTable(cols, ignorelistInput, dupOnExcluded, ignorelistMatch);
        }

        /// <summary>
        /// Generates a table of best in-between values for all possible color pairs on a 256-color palette.
        /// </summary>
        /// <param name="colorPalette"></param>
        /// <param name="exclIndSrc"></param>
        /// <param name="dupOnExcluded"></param>
        /// <param name="exclIndTrg"></param>
        /// <returns></returns>
        public static byte[] GenerateInterlaceTable(Color[] colorPalette, List<int> exclIndSrc, bool dupOnExcluded, List<int> exclIndTrg)
        {
            if (colorPalette.Length > 0x100)
                return null;
            Color[] palette = new Color[0x100];
            colorPalette.CopyTo(palette, 0);

            bool[] excludedFrom = new bool[0x100];
            for (int i = 0; i < 0x100; ++i)
            {
                int index = exclIndSrc[i];
                if (index >= 0 && index < 0x100)
                    excludedFrom[index] = true;
            }
            byte[] interlaceTable = new byte[0x10000];
            for (int y = 0; y < 0x100; ++y)
            {
                for (int x = y; x < 0x100; ++x)
                {
                    byte value;
                    bool equal = y == x;
                    bool exclX = excludedFrom[x];
                    bool exclY = excludedFrom[y];
                    if (equal)
                        value = (byte)y;
                    else if (exclX || exclY)
                        value = dupOnExcluded ? (exclX ? (byte)x : (byte)y) : (byte)0;
                    else
                        value = (byte)ColorUtils.GetClosestPaletteIndexMatch(ColorUtils.GetAverageColor(colorPalette[x], colorPalette[y]), palette, exclIndTrg);
                    interlaceTable[y * 0x100 + x] = value;
                    if (!equal)
                        interlaceTable[x * 0x100 + y] = value;
                }
            }
            return interlaceTable;
        }

        protected List<int> GetIndices(string excl)
        {
            string[] indices = excl.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> indicesInt = new List<int>();
            int indicesLength = indices.Length;
            for (int i = 0; i < indicesLength; ++i)
            {
                string index = indices[i];
                try { indicesInt.Add(byte.Parse(index)); }
                catch (Exception e) { throw new NotSupportedException("Given indices contain illegal values.", e); }
            }
            return indicesInt;
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            const int reqSize = 0x10000;
            if (fileData.Length != reqSize)
                throw new FileTypeLoadException("File is not " + reqSize + " bytes long.");
            for (int y = 0; y < 256; ++y)
            {
                for (int x = y; x < 256; ++x)
                {
                    if (fileData[x*256 + y] != fileData[y*256 + x])
                        throw new FileTypeLoadException("File format redundancy check failed.");
                }
            }
            //*/
            // Simple format: show table only
            this.m_LoadedImage = ImageUtils.BuildImage(fileData, 0x100, 0x100, 0x100, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            /*/
            // Advanced format: show table outlined with original palette.
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);

            Int32 singledim = 0x106;
            Byte[] imageData = new Byte[singledim * singledim];
            Byte[] fullPal = Enumerable.Range(0, 256).Select(x => (Byte)x).ToArray();
            // Paint data on new image. Return value is not used in these calls since they all modify the original array.
            // Palette line at the top
            ImageUtils.PasteOn8bpp(imageData, singledim, singledim, singledim, fullPal, 0x100, 1, 0x100, new Rectangle(3, 1, 0x100, 1), null, true);
            // Palette line at the bottom
            ImageUtils.PasteOn8bpp(imageData, singledim, singledim, singledim, fullPal, 0x100, 1, 0x100, new Rectangle(3, singledim-2, 0x100, 1), null, true);
            // Palette line at the left
            ImageUtils.PasteOn8bpp(imageData, singledim, singledim, singledim, fullPal, 1, 0x100, 1, new Rectangle(1, 3, 1, 0x100), null, true);
            // Palette line at the right
            ImageUtils.PasteOn8bpp(imageData, singledim, singledim, singledim, fullPal, 1, 0x100, 1, new Rectangle(singledim - 2, 3, 1, 0x100), null, true);
            // Actual central table image.
            ImageUtils.PasteOn8bpp(imageData, singledim, singledim, singledim, fileData, 0x100, 0x100, 0x100, new Rectangle(3, 3, 0x100, 0x100), null, true);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, singledim, singledim, singledim, PixelFormat.Format8bppIndexed, m_Palette, null);
            //*/
        }

    }
}