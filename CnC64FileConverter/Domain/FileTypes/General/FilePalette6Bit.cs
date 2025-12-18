using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FilePalette6Bit : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "6-bit pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "6-bit palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions {  get { return new String[]{ "pal" }; } }

        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return 16; } }
        public override Int32 ColorsInPalette { get { return 256; } }

        public override void LoadFile(Byte[] fileData)
        {
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size.");
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            ColorSixBit[] palette = null;
            Exception e = null;
            try
            {
                palette = ColorUtils.ReadSixBitPalette(fileData);
            }
            catch (ArgumentException ex) { e = ex; }
            catch (NotSupportedException ex2) { e = ex2; }
            if (e != null)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + e.Message, e);
            }
            this.m_Palette = ColorUtils.GetEightBitColorPalette(palette);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
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
            Color[] cols = this.CheckInputForColors(fileToSave, true);
            ColorSixBit[] sbcp = ColorUtils.GetSixBitColorPalette(cols);
            return ColorUtils.GetSixBitPaletteData(sbcp);
        }

    }
}
