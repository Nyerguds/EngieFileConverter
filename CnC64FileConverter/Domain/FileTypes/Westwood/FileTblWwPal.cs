using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileTblWwPal : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        //public override Int32 Width { get { return m_Width; } }
        //public override Int32 Height { get { return m_Height; } }
        //protected Int32 m_Width = 0x101;
        //protected Int32 m_Height = 0x101;
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood PAL Table"; } }
        public override String[] FileExtensions { get { return new String[] {"pal"}; } }
        public override String ShortTypeDescription  { get { return "Westwood Palette Stretch Table"; } }
        public override Int32 ColorsInPalette  { get { return 0; } }
        public override Int32 BitsPerPixel  { get { return 8; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return new SaveOption[]
            {
                new SaveOption("IGI", SaveOptionType.String, "Exclude these color indices from the matching process", String.Empty),
                new SaveOption("DUP", SaveOptionType.Boolean, "Duplicate on excluded indices", String.Empty),
                new SaveOption("IGM", SaveOptionType.String, "Prohibit matching to these color indices", String.Empty)
            };
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Color[] cols = this.CheckInputForColors(fileToSave, true);
            List<Int32> ignorelistInput = this.GetIndices(SaveOption.GetSaveOptionValue(saveOptions, "IGI"));
            List<Int32> ignorelistMatch = this.GetIndices(SaveOption.GetSaveOptionValue(saveOptions, "IGM"));
            Boolean dupOnExcluded = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "DUP"));
            return ColorUtils.GenerateInterlaceTable(cols, ignorelistInput, dupOnExcluded, ignorelistMatch);
        }

        protected List<Int32> GetIndices(String excl)
        {
            String[] indices = excl.Split(new Char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<Int32> indicesInt = new List<Int32>();
            foreach (String index in indices)
            {
                try { indicesInt.Add(Byte.Parse(index)); }
                catch (Exception e) { throw new NotSupportedException("Given indices contain illegal values!", e); }
            }
            return indicesInt;
        }


        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            const Int32 reqSize = 0x10000;
            if (fileData.Length != reqSize)
                throw new FileTypeLoadException("File is not " + reqSize + " bytes long!");
            for (Int32 y = 0; y < 256; y++)
            {
                for (Int32 x = y; x < 256; x++)
                {
                    if (fileData[x*256 + y] != fileData[y*256 + x])
                        throw new FileTypeLoadException("File format redundancy check failed!");
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