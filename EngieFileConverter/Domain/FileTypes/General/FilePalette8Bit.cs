using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FilePalette8Bit : SupportedFileType
    {
        public override String IdCode { get { return "Pal8Bit"; } }
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "8-bit pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "8-bit palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions {  get { return new String[]{ "pal" }; } }

        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return 16; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override Int32 ColorsInPalette { get { return 256; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[0]; } }

        public override void LoadFile(Byte[] fileData)
        {
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size.");
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, true);
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
            return ColorUtils.GetEightBitPaletteData(cols, true);
        }

    }
}
