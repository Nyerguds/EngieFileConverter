using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FilePalette6Bit : SupportedFileType
    {
        public override String IdCode { get { return "Pal6bit"; } }
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "6-bit pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "6-bit palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions {  get { return new String[]{ "pal" }; } }

        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return 16; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[0]; } }

        public FilePalette6Bit() { }

        public FilePalette6Bit(Color[] contents)
        {
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            this.m_Palette = new Color[0x100];
            Array.Copy(contents, m_Palette, Math.Min(contents.Length, 0x100));
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
        }

        public override void LoadFile(Byte[] fileData)
        {
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size.");
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            Color[] palette;
            try
            {
                palette = ColorUtils.ReadSixBitPaletteAsEightBit(fileData);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + ex.Message, ex);
            }
            this.m_Palette = palette;
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
            Color[] cols = CheckInputForColors(fileToSave, this.BitsPerPixel, true);
            ColorSixBit[] sbcp = ColorUtils.GetSixBitColorPalette(cols);
            return ColorUtils.GetSixBitPaletteData(sbcp);
        }

    }
}
