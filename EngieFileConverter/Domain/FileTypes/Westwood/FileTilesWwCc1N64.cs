using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public abstract class FileTilesWwCc1N64 : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }
        protected abstract Int32 Bpp { get; }
        public override String[] FileExtensions { get { return new String[] { "da" + this.Bpp }; } }
        public override String ShortTypeName { get { return "C&C64 " + this.Bpp + "-bit tiles"; } }
        public override String LongTypeName { get { return "Westwood C&C N64 " + this.Bpp + "-bit tile data"; } }
        protected abstract FilePaletteWwCc1N64 PaletteType { get; }
        protected String ExtData { get { return "da" + this.Bpp; } }
        protected String ExtPalIndex { get { return "nd" + this.Bpp; } }
        protected String ExtPalFile { get { return "pa" + this.Bpp; } }
        protected String ExtTileIds { get { return "tl" + this.Bpp; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];
        protected Byte[][] m_rawTiles;
        protected Byte[] m_palIndexFile;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return ArrayUtils.CloneArray(this.m_FramesList); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }
        public override Boolean NeedsPalette { get { return this.m_Palette == null; } }
        public override  Boolean FramesHaveCommonPalette { get { return this.Bpp == 8; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public override Boolean CanSave { get { return false; } }

        public override void LoadFile(Byte[] fileData)
        {
            throw new FileTypeLoadException("Tilesets cannot be loaded from byte array; they require file names.");
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            String ext = Path.GetExtension(filename).TrimStart('.');
            if (!String.Equals(ext, this.ExtData, StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("Not a " + this.LongTypeName + " file!");
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
            for (Int32 i = 0; i < entries; ++i)
                maxPalIndex = Math.Max(maxPalIndex, this.m_palIndexFile[i]);
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
            if (palette.GetColors().Length < (maxPalIndex + 1) * singlePalSize)
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
            SupportedFileType[] list = new SupportedFileType[len];
            Int32 stride = ImageUtils.GetMinimumStride(24, this.Bpp);
            Int32 tileSize = stride * 24;
            Int32 singlePalSize = 1 << this.Bpp;

            this.m_rawTiles = new Byte[len][];
            Int32 tilesWidth = 24;
            Int32 tilesHeight = this.m_rawTiles.Length * 24;
            Int32 tilesStride = ImageUtils.GetMinimumStride(tilesWidth, this.Bpp);
            Byte[] tilesData = this.AdjustTo8BitPalette(dataFile, this.m_palIndexFile, 24, tilesHeight, ref tilesStride);
            for (Int32 i = 0; i < len; ++i)
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

                FileImageFrame cell = new FileImageFrame();
                cell.LoadFileFrame(this, this, currentTile, baseFileName, (Byte)i);
                cell.SetBitsPerColor(this.Bpp);
                cell.SetFileClass(this.FrameInputFileClass);
                cell.SetNeedsPalette(false);
                TileInfo ti;
                bool foundMapping = MapConversion.TILEINFO_TD.TryGetValue(highByte, out ti);
                string formattedHighByte = String.Format("[{0:X2}]", highByte);
                String baseName = ti == null ? formattedHighByte : ti.TileName;
                String cellFileName = baseName + "_" + lowByte.ToString("D3");
                cell.SetFrameFileName(cellFileName);
                StringBuilder extraInfo = new StringBuilder();
                extraInfo.Append("Tileset: ").Append(formattedHighByte).Append(foundMapping ? " " + ti.TileName : " NOT FOUND");
                extraInfo.Append("\nTileset number: ").Append(lowByte);
                extraInfo.Append("\nUsed subpalette: ").Append(palIndexFile[i]);
                cell.ExtraInfo = extraInfo.ToString();
                list[i] = cell;
                //list[i] = new FileTileCc1N64(this, baseFileName, currentTile, highByte, lowByte, palIndexFile[i]);
                this.m_rawTiles[i] = ImageUtils.CopyFrom8bpp(tilesData, tilesWidth, tilesHeight, tilesStride, new Rectangle(0, i * 24, 24, 24));
            }
            this.m_FramesList = list;
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotImplementedException();
        }

        protected void BuildFullImage()
        {
            Int32 nrOftiles = this.m_rawTiles.Length;
            this.m_LoadedImage = ImageUtils.Tile8BitImages(this.m_rawTiles, 24, 24, 24, nrOftiles, this.m_Palette, 1);
            if (this.m_Palette.Length < 0x100)
                this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
        }

        private Byte[] AdjustTo8BitPalette(Byte[] dataFile, Byte[] palIndexFile, Int32 width, Int32 height, ref Int32 stride)
        {
            Int32 singlePalSize = 1 << this.Bpp;
            Byte[] imageData = ImageUtils.ConvertTo8Bit(dataFile, width, height, 0, this.Bpp, true, ref stride);
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 increase = palIndexFile[y / 24] * singlePalSize;
                for (Int32 x = 0; x < width; ++x)
                {
                    Int32 index = y * stride + x;
                    imageData[index] = (Byte)(imageData[index] + increase);
                }
            }
            stride = width;
            return imageData;
        }
    }

    public class FileTilesWwCc1N64Bpp4 : FileTilesWwCc1N64
    {
        public override String IdCode { get { return "WwDa4"; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }
        protected override Int32 Bpp { get { return 4; } }
        protected override FilePaletteWwCc1N64 PaletteType { get { return new FilePaletteWwCc1N64Pa4(); } }
    }

    public class FileTilesWwCc1N64Bpp8 : FileTilesWwCc1N64
    {
        public override String IdCode { get { return "WwDa8"; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected override Int32 Bpp { get { return 8; } }
        protected override FilePaletteWwCc1N64 PaletteType { get { return new FilePaletteWwCc1N64Pa8(); } }
    }
}
