using Nyerguds.ImageManipulation;
using Nyerguds.CCTypes;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FilePalettePc : SupportedFileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "PCPal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "PC C&C palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions {  get { return new String[]{ "pal" }; } }

        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return 16; } }
        public override Int32 ColorsInPalette { get { return 256; } }
        public override SupportedFileType PreferredExportType { get { return new FilePaletteN64(); } }

        protected Color[] m_palette;

        public override void LoadFile(Byte[] fileData)
        {
            if (fileData.Length != 768)
                throw new FileTypeLoadException("Incorrect file size.");
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            SixBitColor[] palette = null;
            Exception e = null;
            try
            {
                palette = ColorUtils.ReadSixBitPalette(fileData);
            }
            catch (ArgumentException ex) { e = ex;}
            catch (NotSupportedException ex2) { e = ex2;}
            if (e != null)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + e.Message, e);
            }
            m_palette = ColorUtils.GetEightBitColorPalette(palette);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, m_palette, Color.Black);
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
            if (this.m_backupPalette == null)
                this.m_backupPalette = GetColors();
            m_palette = palette;
            // update image
            base.SetColors(palette);
        }

        public override Boolean ColorsChanged()
        {
            // assume there's no palette, or no backup was ever made
            if (this.m_backupPalette == null)
                return false;
            return !m_palette.SequenceEqual(this.m_backupPalette);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            if (fileToSave.BitsPerColor != 8)
                throw new NotSupportedException(String.Empty);
            Color[] palEntries = fileToSave.GetColors();
            if (palEntries == null || palEntries.Length == 0)
                throw new NotSupportedException(String.Empty);
            Color[] cols = new Color[256];
            for (Int32 i = 0; i < cols.Length; i++)
            {
                if (i < palEntries.Length)
                    cols[i] = palEntries[i];
                else
                    cols[i] = Color.Black;
            }
            SixBitColor[] sbcp = ColorUtils.GetSixBitColorPalette(palEntries);
            return ColorUtils.GetSixBitPaletteData(sbcp);
        }

    }
}
