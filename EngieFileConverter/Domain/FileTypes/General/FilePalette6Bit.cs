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
        public override string IdCode { get { return "Pal6bit"; } }
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "6-bit pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return "6-bit palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions {  get { return new string[]{ "pal" }; } }

        public override int Width { get { return 16; } }
        public override int Height { get { return 16; } }
        public override int BitsPerPixel { get { return 8; } }
        public override bool[] TransparencyMask { get { return new bool[0]; } }

        public FilePalette6Bit() { }

        public override void LoadFile(byte[] fileData)
        {
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size.");
            byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (byte)x).ToArray();
            Color[] palette;
            try
            {
                palette = ColorUtils.ReadSixBitPalette(fileData);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
            this.m_Palette = palette;
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
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
            Color[] cols = CheckInputForColors(fileToSave, this.BitsPerPixel, true);
            return ColorUtils.GetSixBitPaletteData(cols);
        }

    }
}
