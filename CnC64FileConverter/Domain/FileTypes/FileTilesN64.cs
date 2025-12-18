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
    
    public abstract class FileTilesN64 : SupportedFileType
    {
        protected abstract Int32 Bpp { get; }
        public override String[] FileExtensions { get { return new String[] { "da" + Bpp }; } }
        public override String ShortTypeDescription { get { return "N64 " + Bpp + "-bit tile data"; } }
        protected abstract FilePaletteN64 PaletteType { get; }
        protected String ExtData { get { return "da" + Bpp; } }
        protected String ExtPalIndex { get { return "nd" + Bpp; } }
        protected String ExtPalFile { get { return "pa" + Bpp; } }
        protected String ExtTileIds { get { return "tl" + Bpp; } }
        protected Color[] m_palette;
        protected N64Tile[] m_tilesList = new N64Tile[0];
        protected Byte[][] m_rawTiles;
        protected Byte[] m_palIndexFile;

        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return true; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_tilesList.Cast<SupportedFileType>().ToArray(); } }

        public override Int32 ColorsInPalette { get { return this.m_palette == null ? 0 : this.m_palette.Length; } }
        public override Int32 BitsPerColor { get { return 8; } }

        public override Color[] GetColors()
        {
            return this.m_palette == null ? new Color[0] : this.m_palette.ToArray();
        }
        
        public override void SetColors(Color[] palette)
        {
            if (this.m_backupPalette == null)
                this.m_backupPalette = GetColors();
            this.m_palette = palette;
            base.SetColors(palette);
            Int32 singlePalSize = 1 << Bpp;
            foreach (N64Tile tile in this.m_tilesList)
            {
                Color[] subPal = new Color[singlePalSize];
                if ((tile.PaletteIndex + 1) * singlePalSize > palette.Length)
                    continue;
                Array.Copy(this.m_palette, tile.PaletteIndex * singlePalSize, subPal, 0, singlePalSize);
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
            Byte[] m_dataFile = File.ReadAllBytes(dataFileName);
            // Actually 24*24*Bpp/8, but 24*24/8 is of course 72.
            Int32 tileSize = 72 * Bpp;
            Int32 entries = m_dataFile.Length / tileSize;
            if (m_dataFile.Length % tileSize != 0)
                throw new FileTypeLoadException("Tileset load failed: " + ExtData + "file is not a multiple of " + tileSize + " bytes.");
            m_palIndexFile = File.ReadAllBytes(palIndexFileName);
            if (m_palIndexFile.Length != entries)
                throw new FileTypeLoadException("Tileset load failed: amount of entries in " + ExtPalIndex + "file does not match those in " + ExtData + " file." );
            Int32 maxPalIndex = 0;
            foreach (Byte ind in m_palIndexFile)
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
            this.MakeTilesList(filename, m_dataFile, m_palIndexFile, tileIdsFile, palette);
            this.m_palette = palette.GetColors();
            this.BuildFullImage();
            this.LoadedFile = filename;
            this.LoadedFileName = fileNameBase + "." + ExtData + "/" + ExtPalIndex + "/" + ExtPalFile + "/" + ExtTileIds;
        }

        private void MakeTilesList(String baseFileName, Byte[] dataFile, Byte[] palIndexFile, Byte[] tileIdsFile, FilePaletteN64 palette)
        {
            Int32 len = palIndexFile.Length;
            Color[] fullPalette = palette.GetColors();
            N64Tile[] list = new N64Tile[len];
            Int32 stride = ImageUtils.GetMinimumStride(24, Bpp);
            Int32 tileSize = stride * 24;
            Int32 singlePalSize = 1 << Bpp;
            Int32 tiles = palIndexFile.Length;

            this.m_rawTiles = new Byte[palIndexFile.Length][];
            Int32 tilesWidth = 24;
            Int32 tilesHeight = m_rawTiles.Length * 24;
            Int32 tilesStride = ImageUtils.GetMinimumStride(tilesWidth, Bpp);
            Byte[] tilesData = AdjustTo8BitPalette(dataFile, this.m_palIndexFile, 24, tilesHeight, ref tilesStride);
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
                Int32 tilesStrideOut;
                m_rawTiles[i] = ImageUtils.CopyFrom8bpp(tilesData, tilesWidth, tilesHeight, tilesStride, out tilesStrideOut, new Rectangle(0, i * 24, 24, 24));
            }
            this.m_tilesList = list;
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }
        
        protected override void BuildFullImage()
        {
            Int32 nrOftiles = this.m_rawTiles.Length;
            this.m_LoadedImage = ImageUtils.Tile8BitImages(this.m_rawTiles, 24, 24, 24, nrOftiles, this.m_palette, this.m_CompositeFrameTilesWidth);
            if (this.m_palette.Length < 0x100)
                this.m_LoadedImage.Palette = BitmapHandler.GetPalette(this.m_palette);
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
        
        public void ConvertToTiles(String outputFolder, String baseName, SupportedFileType outputType)
        {
            if (!(outputType is FileImage))
                throw new NotSupportedException("Exporting tileset as type " + outputType.ShortTypeName + " is not supported.");
            if (outputType is FileImageJpg)
                throw new NotSupportedException("JPEG? No. Fuck off. Don't do that to those poor 24x24 paletted images.");
            String ext = "." + outputType.FileExtensions[0];
            foreach (N64Tile tile in this.m_tilesList)
            {
                TileInfo ti;
                if (!MapConversion.TILEINFO.TryGetValue(tile.CellData.HighByte, out ti))
                    throw new NotSupportedException("Bad mapping in " + this.ExtTileIds + " file!");
                String outputName = ti.TileName + "_" + tile.CellData.LowByte.ToString("D3") + "_pal" + tile.PaletteIndex.ToString("D2") + ext;
                String outputPath = Path.Combine(outputFolder, outputName);
                outputType.SaveAsThis(tile, outputPath, true);
            }
        }

    }

    public class FileTilesN64Bpp4 : FileTilesN64
    {
        protected override Int32 Bpp { get { return 4; } }
        protected override FilePaletteN64 PaletteType { get { return new FilePaletteN64Pa4(); } }
    }

    public class FileTilesN64Bpp8 : FileTilesN64
    {
        protected override Int32 Bpp { get { return 8; } }
        protected override FilePaletteN64 PaletteType { get { return new FilePaletteN64Pa8(); } }
    }

    public class N64Tile : SupportedFileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Tile"; } }
        public override String ShortTypeDescription { get { return "N64 " +this.Bpp + "-bit terrain tile";} }
        public override String[] FileExtensions { get { return new String[0]; } }
        public override SupportedFileType PreferredExportType { get { return new FileImagePng(); } }

        public SupportedFileType Origin { get; set; }
        public String SourceFileName { get; private set; }
        public CnCMapCell CellData { get; private set; }
        public Int32 PaletteIndex { get; private set; }
        public TileInfo TileInfo { get; private set; }
        public Int32 Bpp { get { return Image.GetPixelFormatSize((m_LoadedImage.PixelFormat)); } }

        public N64Tile(SupportedFileType origin, String sourceFileName, Bitmap tileImage, Byte? highByte, Byte lowByte, Int32 paletteIndex)
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }
    }
}
