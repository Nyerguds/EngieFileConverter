using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace CnC64FileConverter.Domain.ImageFile
{
    public class FileTilesN64Bpp4 : N64FileType
    {
        public override String[] FileExtensions { get { return new String[] { "da" + Bpp }; } }
        public override String ShortTypeDescription { get { return "N64 " + Bpp + "-bit tile data"; } }

        protected virtual Int32 Bpp { get { return 4; } }
        protected virtual FilePaletteN64 PaletteType { get { return new FilePaletteN64Pa4(); } }
        protected String ExtData { get { return "da" + Bpp; } }
        protected String ExtPalIndex { get { return "nd" + Bpp; } }
        protected String ExtPalFile { get { return "pa" + Bpp; } }
        protected FilePaletteN64 Palette;

        public override Boolean FileHasPalette { get { return true; } }
        public override Int32 ColorsInPalette { get { return Palette.ColorsInPalette; } }

        public override Color[] GetColors()
        {
            return Palette.GetColors();
        }

        public override void LoadImage(Byte[] fileData)
        {
            throw new FileTypeLoadException("Tilesets cannot be loaded from byte array; they require file names.");
        }

        public override void LoadImage(String filename)
        {
            String filenameBase = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".";
            String dataFileName = filenameBase + ExtData;
            String palIndexFileName = filenameBase + ExtPalIndex;
            String palFileName = filenameBase + ExtPalFile;
            if (!File.Exists(dataFileName) || !File.Exists(palIndexFileName) || !File.Exists(palFileName))
                throw new FileTypeLoadException("Tileset load failed: Can not load the " + ExtData + "/" + ExtPalIndex + "/" + ExtPalFile + " file triplet.");
            Byte[] dataFile = File.ReadAllBytes(dataFileName);
            Int32 tileSize = 24 * 3 * Bpp;
            if (dataFile.Length % tileSize != 0)
                throw new FileTypeLoadException("Tileset load failed: " + ExtData + "file is not a multiple of " + tileSize + ".");
            Byte[] palIndexFile = File.ReadAllBytes(palIndexFileName);
            if (dataFile.Length / tileSize != palIndexFile.Length)
                throw new FileTypeLoadException("Tileset load failed: amount of entries in " + ExtPalIndex + "file does not match those in " + ExtData + " file." );
            this.Palette = PaletteType;
            try
            {
                this.Palette.LoadImage(palFileName);
            }
            catch(FileTypeLoadException ex)
            {
                throw new FileTypeLoadException("Could not load tilesets palette file: " + ex.Message, ex);
            }
            LoadData(dataFile, palIndexFile, this.Palette);
        }

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }

        private void LoadData(Byte[] dataFile, Byte[] palIndexFile, FilePaletteN64 palette)
        {
            Int32 width = 24;
            Int32 height = palIndexFile.Length * 24;
            Int32 stride = ImageUtils.GetMinStride(width, Bpp);
            Int32 singlePalSize = 1 << Bpp;
            Byte[] imageData = ImageUtils.ConvertTo8Bit(dataFile, width, height, 0, Bpp, true, ref stride);
            for (Int32 y = 0; y < height; y++)
            {
                Int32 increase = palIndexFile[y / 24] * singlePalSize;
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 index = y * stride + x;
                    imageData[index] = (Byte)(imageData[index] + increase);
                }
            }
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, palette.GetColors(), Color.Empty);
        }
    }

    public class FileTilesN64Bpp8 : FileTilesN64Bpp4
    {
        protected override Int32 Bpp { get { return 8; } }
        protected virtual FilePaletteN64 PaletteType { get { return new FilePaletteN64Pa8(); } }
    }
}
