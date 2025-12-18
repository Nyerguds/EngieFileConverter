using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.CCTypes;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileTilesN64Bpp4 : N64FileType
    {
        protected virtual Int32 Bpp { get { return 4; } }
        public override String[] FileExtensions { get { return new String[] { "da" + Bpp }; } }
        public override String ShortTypeDescription { get { return "N64 " + Bpp + "-bit tile data"; } }

        protected virtual FilePaletteN64 PaletteType { get { return new FilePaletteN64Pa4(); } }
        protected String ExtData { get { return "da" + Bpp; } }
        protected String ExtPalIndex { get { return "nd" + Bpp; } }
        protected String ExtPalFile { get { return "pa" + Bpp; } }
        protected String ExtTileIds { get { return "tl" + Bpp; } }
        protected Color[] m_Palette;
        protected N64Tile[] m_TilesList = new N64Tile[0];

        /// <summary>Sub-frames inside this file.</summary>
        public override N64FileType[] Frames { get { return m_TilesList.Cast<N64FileType>().ToArray(); } }
        
        public override Int32 ColorsInPalette { get { return m_Palette == null ? 0 : m_Palette.Length; } }
        public override Int32 BitsPerColor { get { return 8; } }

        public override Color[] GetColors()
        {
            return m_Palette == null ? new Color[0] : m_Palette.ToArray();
        }
        
        public override void SetColors(Color[] palette)
        {
            if (m_BackupPalette == null)
                m_BackupPalette = GetColors();
            m_Palette = palette;
            base.SetColors(palette);
            Int32 singlePalSize = 1 << Bpp;
            foreach (N64Tile tile in m_TilesList)
            {
                Color[] subPal = new Color[singlePalSize];
                if ((tile.PaletteIndex + 1) * singlePalSize > palette.Length)
                    continue;
                Array.Copy(m_Palette, tile.PaletteIndex * singlePalSize, subPal, 0, singlePalSize);
                tile.Origin = null;
                tile.SetColors(subPal);
                tile.Origin = this;
            }
        }
        
        public override void LoadFile(Byte[] fileData)
        {
            throw new FileTypeLoadException("Tilesets cannot be loaded from byte array; they require file names.");
        }

        public override void LoadFile(String filename)
        {
            String ext = Path.GetExtension(filename).TrimStart('.');
            if (!String.Equals(ext, ExtData, StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("Not a " + ShortTypeDescription + " file!");
            String fileNameBase = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            String filePathBase = Path.Combine(Path.GetDirectoryName(filename), fileNameBase) + ".";
            String dataFileName = filePathBase + ExtData;
            String palIndexFileName = filePathBase + ExtPalIndex;
            String palFileName = filePathBase + ExtPalFile;
            String tileIDsFileNamesFileName = filePathBase + this.ExtTileIds;
            if (!File.Exists(dataFileName) || !File.Exists(palIndexFileName) || !File.Exists(palFileName) || !File.Exists(tileIDsFileNamesFileName))
                throw new FileTypeLoadException("Tileset load failed: Can not load the " + ExtData + "/" + ExtPalIndex + "/" + ExtPalFile + "/" + this.ExtTileIds + " files collection.");
            Byte[] dataFile = File.ReadAllBytes(dataFileName);
            // Actually 24*24*Bpp/8, but 24/8 is of course 3.
            Int32 tileSize = 24 * 3 * Bpp;
            Int32 entries = dataFile.Length / tileSize;
            if (dataFile.Length % tileSize != 0)
                throw new FileTypeLoadException("Tileset load failed: " + ExtData + "file is not a multiple of " + tileSize + " bytes.");
            Byte[] palIndexFile = File.ReadAllBytes(palIndexFileName);
            if (palIndexFile.Length != entries)
                throw new FileTypeLoadException("Tileset load failed: amount of entries in " + ExtPalIndex + "file does not match those in " + ExtData + " file." );

            Int32 maxPalIndex = 0;
            foreach (Byte ind in palIndexFile)
                maxPalIndex = Math.Max(maxPalIndex, ind);
            FilePaletteN64 palette = PaletteType;
            try
            {
                palette.LoadFile(palFileName);
            }
            catch(FileTypeLoadException ex)
            {
                throw new FileTypeLoadException("Could not load tilesets palette file: " + ex.Message, ex);
            }
            Int32 singlePalSize = 1 << Bpp;
            if (palette.ColorsInPalette < (maxPalIndex + 1) * singlePalSize)
                throw new FileTypeLoadException("Palette indices file (" + ExtPalIndex + ") references higher index than the amount of colors in the palette!");
            Byte[] tileIdsFile = File.ReadAllBytes(tileIDsFileNamesFileName);
            if (tileIdsFile.Length != entries * 2)
                throw new FileTypeLoadException("Tileset load failed: amount of entries in " + ExtTileIds + "file does not match those in " + ExtData + " file.");
            this.MakeTilesList(filename, dataFile, palIndexFile, tileIdsFile, palette);
            //this.MakeSingleImageHorizontal(dataFile, palIndexFile, palette);
            this.MakeSingleImageVertical(dataFile, palIndexFile, palette);
            this.m_Palette = palette.GetColors();
            this.LoadedFile = filename;
            this.LoadedFileName = fileNameBase + "." + ExtData + "/" + ExtPalIndex + "/" + ExtPalFile + "/" + ExtTileIds;
        }

        private void MakeTilesList(String baseFileName, Byte[] dataFile, Byte[] palIndexFile, Byte[] tileIdsFile, FilePaletteN64 palette)
        {
            Int32 len = palIndexFile.Length;
            Color[] fullPalette = palette.GetColors();
            N64Tile[] list = new N64Tile[len];
            Int32 stride = ImageUtils.GetMinStride(24, Bpp);
            Int32 tileSize = stride * 24;
            Int32 singlePalSize = 1 << Bpp;
            Int32 tiles = palIndexFile.Length;
            for (Int32 i = 0; i < tiles; i++)
            {
                Int32 index = i * tileSize;
                Color[] subPalette = new Color[singlePalSize];
                Array.Copy(fullPalette, palIndexFile[i] * singlePalSize, subPalette, 0, singlePalSize);
                Byte[] imageData = new Byte[tileSize];
                Array.Copy(dataFile, index, imageData, 0, tileSize);
                PixelFormat pf = GetPixelFormat(this.Bpp);
                Bitmap currentTile = ImageUtils.BuildImage(imageData, 24, 24, stride, pf, subPalette, Color.Black);
                Byte highByte = tileIdsFile[i * 2];
                Byte lowByte = tileIdsFile[i * 2 + 1];
                list[i] = new N64Tile(this, baseFileName, currentTile, highByte, lowByte, palIndexFile[i]);
            }
            this.m_TilesList = list;
        }

        private PixelFormat GetPixelFormat(Int32 bpp)
        {
            switch (bpp)
            {
                case 4:
                    return PixelFormat.Format4bppIndexed;
                case 8:
                    return PixelFormat.Format8bppIndexed;
            }
            return PixelFormat.DontCare;
        }

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }

        private void MakeSingleImageVertical(Byte[] dataFile, Byte[] palIndexFile, FilePaletteN64 palette)
        {
            Int32 width = 24;
            Int32 height = palIndexFile.Length * 24;
            Int32 stride = ImageUtils.GetMinStride(width, Bpp);
            Byte[] imageData = AdjustTo8BitPalette(dataFile, palIndexFile, width, height, ref stride);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, palette.GetColors(), Color.Empty);
        }

        private void MakeSingleImageHorizontal(Byte[] dataFile, Byte[] palIndexFile, FilePaletteN64 palette)
        {
            Int32 width = 24;
            Int32 height = palIndexFile.Length * 24;
            Int32 stride = ImageUtils.GetMinStride(width, Bpp);
            Byte[] imageData = AdjustTo8BitPalette(dataFile, palIndexFile, width, height, ref stride);

            Int32 newWidth = palIndexFile.Length * 24;
            Int32 newHeight = 24;
            Int32 newStride = ImageUtils.GetMinStride(newWidth, Bpp);
            Byte[] newImageData = new Byte[stride*height];
            for (Int32 i = 0; i < palIndexFile.Length; i++)
            {
                Int32 tileStride;
                Byte[] curTile = ImageUtils.CopyFrom8bpp(imageData, width, height, stride, out tileStride, new Rectangle(0, i * 24, 24, 24));
                ImageUtils.PasteOn8bpp(newImageData, newWidth, newHeight, newStride, curTile, 24, 24, tileStride, new Rectangle(i * 24, 0, 24, 24), null, true);
            }
            this.m_LoadedImage = ImageUtils.BuildImage(newImageData, newWidth, newHeight, newStride, PixelFormat.Format8bppIndexed, palette.GetColors(), Color.Empty);
        }

        private Byte[] AdjustTo8BitPalette(Byte[] dataFile, Byte[] palIndexFile, Int32 width, Int32 height, ref Int32 stride)
        {
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
            stride = width;
            return imageData;
        }
        
        public void ConvertToTiles(String outputFolder, String baseName, N64FileType outputType)
        {
            if (!(outputType is FileImage))
                throw new NotSupportedException("Exporting tileset as type " + outputType.ShortTypeName + " is not supported.");
            if (outputType is FileImageJpg)
                throw new NotSupportedException("JPEG? No. Fuck off. Don't do that to those poor 24x24 paletted images.");
            String ext = "." + outputType.FileExtensions[0];
            for (int i = 0; i < this.m_TilesList.Length; i++)
            {
                N64Tile tile = this.m_TilesList[i];
                TileInfo ti;
                if (!MapConversion.TILEINFO.TryGetValue(tile.CellData.HighByte, out ti))
                    throw new NotSupportedException("Bad mapping in " + this.ExtTileIds + " file!");
                //String outputName = baseName + "_" + i.ToString("D4") + "_" + 
                String outputName = ti.TileName + "_" + tile.CellData.LowByte.ToString("D3") + "_pal" + tile.PaletteIndex.ToString("D2") + ext;
                String outputPath = Path.Combine(outputFolder, outputName);
                FileImage fi = new FileImage();
                fi.LoadImage(tile.GetBitmap(), 1 << this.Bpp, outputName);
                outputType.SaveAsThis(tile, outputPath);
            }
        }
    }

    public class FileTilesN64Bpp8 : FileTilesN64Bpp4
    {
        protected override Int32 Bpp { get { return 8; } }
        protected override FilePaletteN64 PaletteType { get { return new FilePaletteN64Pa8(); } }
    }

    public class N64Tile : N64FileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Tile"; } }
        public override String ShortTypeDescription { get { return "N64 " +this.Bpp + "-bit terrain tile";} }
        public override String[] FileExtensions { get { return new String[0]; } }
        public override N64FileType PreferredExportType { get { return new FileImagePng(); } }

        public N64FileType Origin { get; set; }
        public String SourceFileName { get; private set; }
        public CnCMapCell CellData { get; private set; }
        public Int32 PaletteIndex { get; private set; }
        public TileInfo TileInfo { get; private set; }
        public Int32 Bpp { get { return Image.GetPixelFormatSize((m_LoadedImage.PixelFormat)); } }

        public N64Tile(N64FileType origin, String sourceFileName, Bitmap tileImage, Byte? highByte, Byte lowByte, Int32 paletteIndex)
        {
            this.Origin = origin;
            this.SourceFileName = sourceFileName;
            m_LoadedImage = tileImage;
            TileInfo ti = null;
            if (highByte.HasValue && !MapConversion.TILEINFO.TryGetValue(highByte.Value, out ti))
                throw new FileTypeLoadException("Bad mapping!");
            this.TileInfo = ti;
            String baseName = ti == null ? Path.GetFileNameWithoutExtension(sourceFileName) : ti.TileName;
            this.LoadedFileName = baseName + "_" + lowByte.ToString("D3");
            this.LoadedFile = Path.Combine(Path.GetDirectoryName(sourceFileName), LoadedFileName);
            this.CellData = new CnCMapCell(highByte ?? 0xFF, lowByte);
            this.PaletteIndex = paletteIndex;
        }

        public override void SetColors(Color[] palette)
        {
            base.SetColors(palette);
            if (this.Origin == null || this.PaletteIndex < 0)
                return;
            // Sets color in origin object, which will in turn adjust all separate tile palettes.
            Int32 singlePalSize = 1 << Bpp;
            if (palette.Length > singlePalSize)
                return;
            Color[] cols = this.Origin.GetColors();
            if (cols.Length < singlePalSize * (PaletteIndex + 1))
                return;
            Array.Copy(palette, 0, cols, singlePalSize * PaletteIndex, singlePalSize);
            this.Origin.SetColors(cols);
        }

        public override String ToString()
        {
            return SourceFileName + " " + this.CellData.ToString() + " (" + Image.GetPixelFormatSize((m_LoadedImage.PixelFormat)) + " BPP)";
        }

        public override void LoadFile(Byte[] fileData)
        {
            throw new NotSupportedException("Loading as this type is not supported.");
        }

        public override void LoadFile(String filename)
        {
            throw new NotSupportedException("Loading as this type is not supported.");
        }

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }
    }
}
