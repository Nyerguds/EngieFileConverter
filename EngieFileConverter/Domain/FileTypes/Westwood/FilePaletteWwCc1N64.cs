using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FilePaletteWwCc1N64 : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 Pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Westwood C&C N64 palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "pa4", "pa8" }; } }
        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return (this.ColorsInPalette + 15) / 16; } }
        public override Int32 ColorsInPalette { get { return this.m_Palette.Length; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[0]; } }

        public override void LoadFile(Byte[] fileData)
        {
            Int32 len = fileData.Length;
            if (len == 0)
                throw new FileTypeLoadException("File is empty!");
            // Test on full 16-colour lines (16 x 3 bytes)
            if (len % 48 != 0)
                throw new FileTypeLoadException("Incorrect file size: not a multiple of 48.");
            // test on max of 16 sub-palettes
            if (fileData.Length > 768)
                throw new FileTypeLoadException("Incorrect file size: exceeds 768 bytes.");
            try
            {
                this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, false);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + ex.Message, ex);
            }
            Byte[] imageData = Enumerable.Range(0, this.Width * this.Height).Select(x => (Byte) x).ToArray();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Empty);
            if (this.m_Palette.Length < 0x100)
                this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFile(fileData);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            // assume there's no palette, or no backup was ever made
            if (this.m_BackupPalette == null)
                return false;
            return !this.m_Palette.SequenceEqual(this.m_BackupPalette);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotSupportedException("Use specific PA4 or PA8 type.");
        }

        protected Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean expandToFullSize)
        {
            Color[] cols = this.CheckInputForColors(fileToSave, expandToFullSize);
            return ColorUtils.GetEightBitPaletteData(cols, expandToFullSize);
        }
    }

    public class FilePaletteWwCc1N64Pa4 : FilePaletteWwCc1N64
    {
        public override FileClass InputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 Pal 4-bit"; } }
        public override String ShortTypeDescription { get { return "Westwood C&C N64 4-bit palettes file"; } }
        public override String[] FileExtensions { get { return new String[] { "pa4" }; } }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return this.SaveToBytesAsThis(fileToSave, false);
        }
    }

    public class FilePaletteWwCc1N64Pa8 : FilePaletteWwCc1N64
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 Pal 8-bit"; } }
        public override String ShortTypeDescription { get { return "Westwood C&C N64 8-bit palette file"; } }
        public override String[] FileExtensions { get { return new String[] { "pa8" }; } }

        public override void LoadFile(Byte[] fileData)
        {
            // test on colour triplets
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size: 8-bit palette needs to be 768 bytes.");
            try
            {
                this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, false);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + ex.Message, ex);
            }
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Empty);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return this.SaveToBytesAsThis(fileToSave, true);
        }

    }
}
