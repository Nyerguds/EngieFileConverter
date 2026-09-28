using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FilePaletteWwAmiga : SupportedFileType
    {
        public override string IdCode { get { return "PalAmi"; } }
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Amiga Pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return "Westwood Amiga palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions { get { return new string[] { "pal" }; } }
        public override int Width { get { return 16; } }
        public override int Height { get { return (this.m_Palette.Length + 15) / 16; } }
        public override bool[] TransparencyMask { get { return new bool[0]; } }

        public override void LoadFile(byte[] fileData)
        {
            int len = fileData.Length;
            if (len == 0)
                throw new FileTypeLoadException("File is empty.");
            // Test on full 16-color lines (16 x 3 bytes)
            if (len % 32 != 0)
                throw new FileTypeLoadException("Incorrect file size: not a multiple of 48.");
            // test on max of 16 sub-palettes
            if (fileData.Length > 512)
                throw new FileTypeLoadException("Incorrect file size: exceeds 512 bytes.");
            try
            {
                for (int i = 0; i < len; i+=2)
                {
                    if ((fileData[i] & 0xF0) != 0)
                        throw new FileTypeLoadException("Incorrect data: this is not an Amiga X444 RGB palette.");
                }
                int palSize = len / 2;
                PixelFormatter pf = FileImgWwCps.Format16BitRgbX444Be;
                this.m_Palette = pf.GetColorRange(fileData, 0, palSize);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
            byte[] imageData = Enumerable.Range(0, this.Width * this.Height).Select(x => (byte) x).ToArray();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Empty);
            if (this.m_Palette.Length < 0x100)
                this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFile(fileData);
            this.SetFileNames(filename);
        }

        public override bool ColorsChanged()
        {
            // assume there's no palette, or no backup was ever made
            if (this.m_BackupPalette == null)
                return false;
            return !this.m_Palette.SequenceEqual(this.m_BackupPalette);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Color[] cols = CheckInputForColors(fileToSave, this.BitsPerPixel, false);
            if (cols.Length % 16 != 0)
                throw new ArgumentException("Amiga palettes must be a multiple of 16 colors.", "fileToSave");
            byte[] outBytes = new byte[cols.Length * 2];
            PixelFormatter pf = FileImgWwCps.Format16BitRgbX444Be;
            for (int i = 0; i < cols.Length; ++i)
                pf.WriteColor(outBytes, i << 1, cols[i]);
            return outBytes;
        }

        protected byte[] SaveToBytesAsThis(SupportedFileType fileToSave, bool expandToFullSize)
        {
            Color[] cols = CheckInputForColors(fileToSave, this.BitsPerPixel, false);
            return ColorUtils.GetEightBitPaletteData(cols, expandToFullSize);
        }
    }
}
