using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FilePaletteN64 : SupportedFileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "N64 C&C palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "pa4", "pa8" }; } }
        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return ColorsInPalette / 16; } }
        public override Int32 ColorsInPalette { get { return m_palette == null? 0 : m_palette.Length; } }
        public override SupportedFileType PreferredExportType { get { return new FilePalettePc(); } }

        protected Color[] m_palette;

        public override void LoadFile(Byte[] fileData)
        {
            Int32 len = fileData.Length;
            // Test on full 16-colour lines (16 x 3 bytes)
            if (len % 48 != 0)
                throw new FileTypeLoadException("Incorrect file size: not a multiple of 48.");
            // test on max of 16 sub-palettes
            if (fileData.Length > 768)
                throw new FileTypeLoadException("Incorrect file size: exceeds 768 bytes.");
            try
            {
                this.m_palette = ColorUtils.ReadEightBitPalette(fileData, false);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + ex.Message, ex);
            }
            Byte[] imageData = Enumerable.Range(0, Width*Height).Select(x => (Byte)x).ToArray();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, Width, Height, 16, PixelFormat.Format8bppIndexed, this.m_palette, Color.Empty);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            this.LoadFile(fileData);
            SetFileNames(filename);
        }

        public override Color[] GetColors()
        {
            return m_palette.ToArray();
        }

        public override void SetColors(Color[] palette)
        {
            if (m_BackupPalette == null)
                m_BackupPalette = GetColors();
            m_palette = palette;
            // update image
            base.SetColors(palette);
        }

        public override Boolean ColorsChanged()
        {
            // assume there's no palette, or no backup was ever made
            if (m_BackupPalette == null)
                return false;
            return !m_palette.SequenceEqual(m_BackupPalette);
        }

        public override void SaveAsThis(SupportedFileType fileToSave, String savePath)
        {
            throw new NotSupportedException("Use specific PA4 or PA8 type.");
        }

        protected void SaveAsThis(SupportedFileType fileToSave, String savePath, Boolean expandToFullSize)
        {
            Color[] palEntries = fileToSave.GetColors();
            if (palEntries == null || palEntries.Length == 0)
                throw new NotSupportedException("Cannot save 32-bit images as " + this.ShortTypeName);
            ColorUtils.WriteEightBitPaletteFile(palEntries, savePath, expandToFullSize);
        }
    }

    public class FilePaletteN64Pa4 : FilePaletteN64
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Pal4"; } }
        public override String ShortTypeDescription { get { return "N64 C&C 4-bit palettes file"; } }
        public override String[] FileExtensions { get { return new String[] { "pa4" }; } }

        public override void SaveAsThis(SupportedFileType fileToSave, String savePath)
        {
            SaveAsThis(fileToSave, savePath, false);
        }
    }

    public class FilePaletteN64Pa8 : FilePaletteN64
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Pal8"; } }
        public override String ShortTypeDescription { get { return "N64 C&C 8-bit palette file"; } }
        public override String[] FileExtensions { get { return new String[] { "pa8" }; } }

        public override void LoadFile(Byte[] fileData)
        {
            // test on colour triplets
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size: 8-bit palette needs to be 768 bytes.");
            try
            {
                this.m_palette = ColorUtils.ReadEightBitPalette(fileData, false);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + ex.Message, ex);
            }
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_palette, Color.Empty);
        }

        public override void SaveAsThis(SupportedFileType fileToSave, String savePath)
        {
            SaveAsThis(fileToSave, savePath, true);
        }

    }
}
