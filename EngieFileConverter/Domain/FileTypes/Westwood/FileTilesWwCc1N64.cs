using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{

    public abstract class FileTilesWwCc1N64 : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }
        protected abstract Int32 Bpp { get; }
        public override String[] FileExtensions { get { return new String[] { "da" + this.Bpp }; } }
        public override String ShortTypeName { get { return "C&C64 " + this.Bpp + "-bit tiles"; } }
        public override String ShortTypeDescription { get { return "Westwood C&C N64 " + this.Bpp + "-bit tile data"; } }
        protected abstract FilePaletteWwCc1N64 PaletteType { get; }
        protected String ExtData { get { return "da" + this.Bpp; } }
        protected String ExtPalIndex { get { return "nd" + this.Bpp; } }
        protected String ExtPalFile { get { return "pa" + this.Bpp; } }
        protected String ExtTileIds { get { return "tl" + this.Bpp; } }
        protected FileTileCc1N64[] m_tilesList = new FileTileCc1N64[0];
        protected Byte[][] m_rawTiles;
        protected Byte[] m_palIndexFile;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_tilesList.Cast<SupportedFileType>().ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }
        public override Int32 ColorsInPalette { get { return this.m_Palette == null ? 0 : this.m_Palette.Length; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        public override void LoadFile(Byte[] fileData)
        {
            throw new FileTypeLoadException("Tilesets cannot be loaded from byte array; they require file names.");
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            String ext = Path.GetExtension(filename).TrimStart('.');
            if (!String.Equals(ext, this.ExtData, StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("Not a " + this.ShortTypeDescription + " file!");
            String fileNameBase = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
            String filePathBase = Path.Combine(Path.GetDirectoryName(filename), fileNameBase) + ".";
            String dataFileName = filePathBase + this.ExtData;
            String palIndexFileName = filePathBase + this.ExtPalIndex;
            String palFileName = filePathBase + this.ExtPalFile;
            String tileIDsFileNamesFileName = filePathBase + this.ExtTileIds;
            if (!File.Exists(dataFileName) || !File.Exists(palIndexFileName) || !File.Exists(palFileName) || !File.Exists(tileIDsFileNamesFileName))
                throw new FileTypeLoadException("Tileset load failed: Can not load the " + this.ExtData + "/" + this.ExtPalIndex + "/" + this.ExtPalFile + "/" + this.ExtTileIds + " files collection.");
            // Actually 24*24*Bpp/8, but 24*24/8 is of course 72.
            Int32 tileSize = 72 * this.Bpp;
            Int32 entries = fileData.Length / tileSize;
            if (fileData.Length % tileSize != 0)
                throw new FileTypeLoadException("Tileset load failed: " + this.ExtData + "file is not a multiple of " + tileSize + " bytes.");
            this.m_palIndexFile = File.ReadAllBytes(palIndexFileName);
            if (this.m_palIndexFile.Length != entries)
                throw new FileTypeLoadException("Tileset load failed: amount of entries in " + this.ExtPalIndex + "file does not match those in " + this.ExtData + " file." );
            Int32 maxPalIndex = 0;
            foreach (Byte ind in this.m_palIndexFile)
                maxPalIndex = Math.Max(maxPalIndex, ind);
            FilePaletteWwCc1N64 palette = this.PaletteType;
            try
            {
                Byte[] palFileData = File.ReadAllBytes(palFileName);
                palette.LoadFile(palFileData, palFileName);
            }
            catch(FileTypeLoadException ex)
            {
                throw new FileTypeLoadException("Could not load tilesets palette file: " + ex.Message, ex);
            }
            Int32 singlePalSize = 1 << this.Bpp;
            if (palette.ColorsInPalette < (maxPalIndex + 1) * singlePalSize)
                throw new FileTypeLoadException("Palette indices file (" + this.ExtPalIndex + ") references higher index than the amount of colors in the palette!");
            Byte[] tileIdsFile = File.ReadAllBytes(tileIDsFileNamesFileName);
            if (tileIdsFile.Length != entries * 2)
                throw new FileTypeLoadException("Tileset load failed: amount of entries in " + this.ExtTileIds + "file does not match those in " + this.ExtData + " file.");
            this.MakeTilesList(filename, fileData, this.m_palIndexFile, tileIdsFile, palette);
            this.m_Palette = palette.GetColors();
            this.BuildFullImage();
            this.LoadedFile = filename;
            this.LoadedFileName = fileNameBase + "." + this.ExtData + "/" + this.ExtPalIndex + "/" + this.ExtPalFile + "/" + this.ExtTileIds;
        }

        private void MakeTilesList(String baseFileName, Byte[] dataFile, Byte[] palIndexFile, Byte[] tileIdsFile, FilePaletteWwCc1N64 palette)
        {
            Int32 len = palIndexFile.Length;
            Color[] fullPalette = palette.GetColors();
            FileTileCc1N64[] list = new FileTileCc1N64[len];
            Int32 stride = ImageUtils.GetMinimumStride(24, this.Bpp);
            Int32 tileSize = stride * 24;
            Int32 singlePalSize = 1 << this.Bpp;
            Int32 tiles = palIndexFile.Length;

            this.m_rawTiles = new Byte[palIndexFile.Length][];
            Int32 tilesWidth = 24;
            Int32 tilesHeight = this.m_rawTiles.Length * 24;
            Int32 tilesStride = ImageUtils.GetMinimumStride(tilesWidth, this.Bpp);
            Byte[] tilesData = this.AdjustTo8BitPalette(dataFile, this.m_palIndexFile, 24, tilesHeight, ref tilesStride);
            for (Int32 i = 0; i < tiles; i++)
            {
                Int32 index = i * tileSize;
                Color[] subPalette = new Color[singlePalSize];
                Array.Copy(fullPalette, palIndexFile[i] * singlePalSize, subPalette, 0, singlePalSize);
                Byte[] imageData = new Byte[tileSize];
                Array.Copy(dataFile, index, imageData, 0, tileSize);
                PixelFormat pf = this.GetPixelFormat(this.Bpp);
                Bitmap currentTile = ImageUtils.BuildImage(imageData, 24, 24, stride, pf, subPalette, Color.Black);
                Byte highByte = tileIdsFile[i * 2];
                Byte lowByte = tileIdsFile[i * 2 + 1];
                list[i] = new FileTileCc1N64(this, baseFileName, currentTile, highByte, lowByte, palIndexFile[i]);
                this.m_rawTiles[i] = ImageUtils.CopyFrom8bpp(tilesData, tilesWidth, tilesHeight, tilesStride, new Rectangle(0, i * 24, 24, 24));
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }

        protected override void BuildFullImage()
        {
            Int32 nrOftiles = this.m_rawTiles.Length;
            this.m_LoadedImage = ImageUtils.Tile8BitImages(this.m_rawTiles, 24, 24, 24, nrOftiles, this.m_Palette, 1);
            if (this.m_Palette.Length < 0x100)
                this.m_LoadedImage.Palette = BitmapHandler.GetPalette(this.m_Palette);
        }

        private Byte[] AdjustTo8BitPalette(Byte[] dataFile, Byte[] palIndexFile, Int32 width, Int32 height, ref Int32 stride)
        {
            Int32 singlePalSize = 1 << this.Bpp;
            Byte[] imageData = ImageUtils.ConvertTo8Bit(dataFile, width, height, 0, this.Bpp, true, ref stride);
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
            foreach (FileTileCc1N64 tile in this.m_tilesList)
            {
                TileInfo ti;
                if (!MapConversion.TILEINFO.TryGetValue(tile.CellData.HighByte, out ti))
                    throw new NotSupportedException("Bad mapping in " + this.ExtTileIds + " file!");
                String outputName = ti.TileName + "_" + tile.CellData.LowByte.ToString("D3") + "_pal" + tile.PaletteIndex.ToString("D2") + ext;
                String outputPath = Path.Combine(outputFolder, outputName);
                outputType.SaveAsThis(tile, outputPath, new SaveOption[0]);
            }
        }

    }

    public class FileTilesWwCc1N64Bpp4 : FileTilesWwCc1N64
    {
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }
        protected override Int32 Bpp { get { return 4; } }
        protected override FilePaletteWwCc1N64 PaletteType { get { return new FilePaletteWwCc1N64Pa4(); } }
    }

    public class FileTilesWwCc1N64Bpp8 : FileTilesWwCc1N64
    {
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected override Int32 Bpp { get { return 8; } }
        protected override FilePaletteWwCc1N64 PaletteType { get { return new FilePaletteWwCc1N64Pa8(); } }
    }

    public class FileTileCc1N64 : SupportedFileType
    {
        public override FileClass FileClass { get { return this.Bpp == 4 ? FileClass.Image4Bit : FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 Tile"; } }
        public override String ShortTypeDescription { get { return "C&C64 " + this.Bpp + "-bit terrain tile";} }
        public override String[] FileExtensions { get { return new String[0]; } }

        public String SourceFileName { get; private set; }
        public CnCMapCell CellData { get; private set; }
        public Int32 PaletteIndex { get; private set; }
        public TileInfo TileInfo { get; private set; }
        public Int32 Bpp { get { return Image.GetPixelFormatSize((this.m_LoadedImage.PixelFormat)); } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        public FileTileCc1N64(SupportedFileType origin, String sourceFileName, Bitmap tileImage, Byte? highByte, Byte lowByte, Int32 paletteIndex)
        {
            this.FrameParent = origin;
            this.SourceFileName = sourceFileName;
            this.m_LoadedImage = tileImage;
            TileInfo ti = null;
            if (highByte.HasValue && !MapConversion.TILEINFO.TryGetValue(highByte.Value, out ti))
                throw new FileTypeLoadException("Bad mapping!");
            this.TileInfo = ti;
            String baseName = ti == null ? Path.GetFileNameWithoutExtension(sourceFileName) : ti.TileName;
            this.LoadedFileName = baseName + "_" + lowByte.ToString("D3");
            this.LoadedFile = Path.Combine(Path.GetDirectoryName(sourceFileName), this.LoadedFileName);
            this.CellData = new CnCMapCell(highByte ?? 0xFF, lowByte);
            this.PaletteIndex = paletteIndex;
        }

        public override String ToString()
        {
            return this.SourceFileName + " " + this.CellData.ToString() + " (" + Image.GetPixelFormatSize((this.m_LoadedImage.PixelFormat)) + " BPP)";
        }

        public override void LoadFile(Byte[] fileData)
        {
            throw new NotSupportedException("Loading as this type is not supported.");
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            throw new NotSupportedException("Loading as this type is not supported.");
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotSupportedException("Saving as this type is not supported.");
        }
    }
}
