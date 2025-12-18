using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.FileData.Dynamix;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FilePaletteDyn : SupportedFileType
    {
        public override String IdCode { get { return "PalDyn"; } }
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Dynamix Palette"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Dynamix palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "pal" }; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[0]; } }

        public override Int32 Width { get { return 16; } }
        public override Int32 Height { get { return 16; } }
        public override Boolean NeedsPalette { get { return false; } }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFile(fileData);
            this.SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            if (fileData.Length < 0x10)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            DynamixChunk palChunk = DynamixChunk.ReadChunk(fileData, "PAL");
            if (palChunk == null || palChunk.Address != 0)
                throw new FileTypeLoadException("File does not start with a PAL chunk.");
            DynamixChunk vgaChunk = DynamixChunk.ReadChunk(palChunk.Data, "VGA");
            if (vgaChunk == null)
                throw new FileTypeLoadException("File does not contain a VGA chunk.");
            if (vgaChunk.DataLength != 768)
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            Color[] palette = null;
            try
            {
                palette = ColorUtils.ReadSixBitPaletteAsEightBit(vgaChunk.Data);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Failed to load file as palette: " + GeneralUtils.RecoverArgExceptionMessage(ex), ex);
            }
            this.m_Palette = palette;
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
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
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
            Color[] palEntries = fileToSave.GetColors();
            if (palEntries == null || palEntries.Length == 0)
                throw new ArgumentException(ERR_NO_COL, "fileToSave");
            Color[] cols = new Color[256];
            for (Int32 i = 0; i < cols.Length; ++i)
            {
                if (i < palEntries.Length)
                    cols[i] = palEntries[i];
                else
                    cols[i] = Color.Black;
            }
            ColorSixBit[] sbcp = ColorUtils.GetSixBitColorPalette(palEntries);
            Byte[] paletteData = ColorUtils.GetSixBitPaletteData(sbcp);
            // write as Dynamix chunks
            DynamixChunk vgaChunk = new DynamixChunk("VGA", paletteData);
            DynamixChunk palChunk = DynamixChunk.BuildChunk("PAL", vgaChunk);
            palChunk.IsContainer = true;
            return palChunk.WriteChunk();
        }

    }
}