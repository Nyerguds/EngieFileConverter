using CnC64FileConverter.Domain.FileTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.CCTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileTilesetPC : SupportedFileType
    {
        public override String[] FileExtensions { get { return new String[] { "tmp", "tem", "win", "des", "sno" }; } }
        public override String ShortTypeName { get { return "PCTile"; } }
        public override String ShortTypeDescription { get { return "PC Tileset File"; } }

        public override Int32 BitsPerColor { get { return 8; } }
        public override Int32 ColorsInPalette { get { return 0; } }

        protected Int16 hdrTileWidth;
        protected Int16 hdrTileHeight;
        protected Int16 hdrNrOfTiles;
        protected Int16 hdrZero1;
        protected Int32 hdrSize;
        protected Int32 hdrImgStart;
        protected Int32 hdrZero2;
        protected Int16 hdrID1;
        protected Int16 hdrID2;
        protected Int32 hdrIndexImages;
        protected Int32 hdrIndexTilesetImagesList;
        protected List<PCTile> m_TilesList;
        protected Color[] m_Palette = null;
        protected Byte[][] m_Tiles;
        protected Boolean[] m_TileUseList;

        public Byte[][] GetRawTiles()
        {
            Byte[][] tiles = new Byte[this.m_Tiles.Length][];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = this.m_Tiles[i].ToArray();
            return tiles;
        }

        public Boolean[] GetUsedEntriesList()
        {
            return this.m_TileUseList.ToArray();
        }

        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return true; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return m_TilesList.Cast<SupportedFileType>().ToArray(); } }

        public override void SetColors(Color[] palette)
        {
            m_Palette = palette.ToArray();
            base.SetColors(palette);
            if (m_TilesList == null)
                return;
            foreach (PCTile pcTile in this.m_TilesList)
            {
                pcTile.Origin = null;
                pcTile.SetColors(palette);
                pcTile.Origin = this;
            }
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData, filename);
            SetFileNames(filename);
        }

        public override void LoadFile(byte[] fileData)
        {
            LoadFromFileData(fileData, null);
        }

        public override void SaveAsThis(SupportedFileType fileToSave, string savePath)
        {
            if (fileToSave.BitsPerColor != 8)
                throw new NotSupportedException("Can only save 8 BPP images as this type.");
            Bitmap bitmap = fileToSave.GetBitmap();
            if (bitmap == null || bitmap.Width % 24 != 0 || bitmap.Height % 24 != 0)
                throw new NotSupportedException("The file dimensions are not a multiple of 24x24!");
            Int32 nrOfFramesX = bitmap.Width / 24;
            Int32 nrOfFramesY = bitmap.Height / 24;
            Int32 nrOfFrames = nrOfFramesX * nrOfFramesY;
            if (nrOfFrames > 255)
                throw new NotSupportedException("File dimensions are too large!");
            Int32 stride;
            Byte[] fullImageData = ImageUtils.GetImageData(bitmap, out stride);
            Byte[][] tempFrames = new Byte[nrOfFrames][];
            Byte[] finalIndices = new Byte[nrOfFrames];
            Int32 actualFrames = 0;
            for (Int32 y = 0; y < nrOfFramesY; y++)
            {
                for (Int32 x = 0; x < nrOfFramesX; x++)
                {
                    Int32 index = y * nrOfFramesX + x;
                    Int32 frameStride;
                    Byte[] frameData = ImageUtils.CopyFrom8bpp(fullImageData, bitmap.Width, bitmap.Height, stride, out frameStride, new Rectangle(x * 24, y * 24, 24, 24));
                    if (frameData.All(b => b == 0))
                        finalIndices[index] = 0xFF;
                    else
                    {
                        finalIndices[index]= (Byte)actualFrames;
                        tempFrames[actualFrames] = frameData;
                        actualFrames++;
                    }
                }
            }
            // Order: (Header) , (data) , (all tiles index) , (actual frames index)
            Int32 tileLength = 24 * 24;
            Int32 size = 0x20;
            Int32 indexImgStart = size;
            size += actualFrames * tileLength;
            Int32 IndexTilesetImagesList = size;
            size += nrOfFrames;
            Int32 IndexImages = size;
            size += actualFrames;
            Byte[] finalData = new Byte[size];

            // Width
            ArrayUtils.SetBEShortInByteArray(finalData, 0x00, 24);
            // Height
            ArrayUtils.SetBEShortInByteArray(finalData, 0x02, 24);
            ArrayUtils.SetBEShortInByteArray(finalData, 0x04, (Int16)nrOfFrames);
            ArrayUtils.SetBEIntInByteArray(finalData, 0x08, size);
            ArrayUtils.SetBEIntInByteArray(finalData, 0x0C, indexImgStart);
            ArrayUtils.SetBEShortInByteArray(finalData, 0x14, -1);
            ArrayUtils.SetBEShortInByteArray(finalData, 0x16, 0x0D1A);
            ArrayUtils.SetBEIntInByteArray(finalData, 0x18, IndexImages);
            ArrayUtils.SetBEIntInByteArray(finalData, 0x1C, IndexTilesetImagesList);

            for (Int32 i = 0; i < actualFrames; i++)
            {
                Byte[] frameData = tempFrames[i];
                Array.Copy(frameData, 0, finalData, indexImgStart + tileLength * i, tileLength);
            }
            Array.Copy(finalIndices, 0, finalData, IndexTilesetImagesList, finalIndices.Length);
            File.WriteAllBytes(savePath, finalData);
        }
        
        private void LoadFromFileData(Byte[] fileData, String sourceFileName)
        {
            // todo: load logic
            Int32 fileLen = fileData.Length;
            if (fileLen < 0x20)
                throw new FileTypeLoadException("File is not long enough to be a valid IMG file.");
            try
            {
                this.ReadHeader(fileData);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading header data: " + e.Message);
            }
            Int32 tileSize = this.hdrTileWidth * this.hdrTileHeight;
            Byte[] imagesList = new Byte[this.hdrNrOfTiles];
            if (this.hdrIndexTilesetImagesList + this.hdrNrOfTiles >= fileLen)
                throw new FileTypeLoadException("Tile info outside file range!");
            Array.Copy(fileData, this.hdrIndexTilesetImagesList, imagesList, 0, this.hdrNrOfTiles);
            Int32 actualImages = imagesList.Max(x => x == 0xFF ? -1 : (Int32)x) + 1;
            if (this.hdrImgStart + actualImages * tileSize > fileLen)
                throw new FileTypeLoadException("Tile image data outside file range!");
            // Probably not needed, but eh.
            //Byte[] actualImagesList = new Byte[actualImages];
            //Array.Copy(fileData, actualImagesList, actualImagesList.Length);

            m_TilesList = new List<PCTile>();
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, false, false);
            // ONly way to set a palette is through SetPaletre, and that ensures 256 colours.
            this.m_Palette[0] = Color.FromArgb(0, this.m_Palette[0]);
            this.m_Tiles = new Byte[this.hdrNrOfTiles][];
            this.m_TileUseList = new Boolean[imagesList.Length];
            for (Int32 i = 0; i < imagesList.Length; i++)
            {
                Byte dataIndex = imagesList[i];
                Boolean used = dataIndex != 0xFF;
                m_TileUseList[i] = used;
                Byte[] tileData;
                if (used)
                {
                    tileData = new Byte[tileSize];
                    Int32 offset = this.hdrImgStart + dataIndex * tileSize;
                    if ((offset + tileSize) > fileLen)
                        throw new FileTypeLoadException("Tile data outside file range");
                    Array.Copy(fileData, offset, tileData, 0, tileSize);
                }
                else
                {
                    tileData = Enumerable.Repeat((Byte)0, tileSize).ToArray();
                }
                this.m_Tiles[i] = tileData;
                Bitmap tileImage = ImageUtils.BuildImage(tileData, this.hdrTileWidth, this.hdrTileHeight, this.hdrTileWidth, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                m_TilesList.Add(new PCTile(this, sourceFileName, tileImage, (Byte)i));
            }
            Int32 xDim = -1;
            if (sourceFileName != null)
            {
                String baseName = Path.GetFileNameWithoutExtension(sourceFileName);
                foreach (TileInfo tileInfo in MapConversion.TILEINFO.Values)
                {
                    if (!String.Equals(baseName, tileInfo.TileName, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (tileInfo.Width * tileInfo.Height != this.hdrNrOfTiles)
                        continue;
                    xDim = tileInfo.Width;
                    break;
                }
            }
            if (xDim == -1)
                xDim = 1;
            this.CompositeFrameTilesWidth = xDim;
            BuildFullImage();
        }

        protected override void BuildFullImage()
        {
            Int32 nrOftiles = this.m_Tiles.Length;
            this.m_LoadedImage = ImageUtils.Tile8BitImages(this.m_Tiles, this.hdrTileWidth, this.hdrTileHeight, this.hdrTileWidth, nrOftiles, this.m_Palette, this.m_CompositeFrameTilesWidth);
        }

        private void ReadHeader(Byte[] headerBytes)
        {
            Int32 fileLen = headerBytes.Length;
            if (fileLen < 0x20)
                return;
            this.hdrTileWidth = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x00);
            this.hdrTileHeight = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x02);
            this.hdrNrOfTiles = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x04);
            this.hdrZero1 = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x06);
            this.hdrSize = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0x08);
            this.hdrImgStart = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0x0C);
            this.hdrZero2 = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0x10);
            this.hdrID1 = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x14);
            this.hdrID2 = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x16);
            //61020000 == 0x261
            this.hdrIndexImages = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0x18);
            //60020000 = 0x260
            this.hdrIndexTilesetImagesList = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0x1C);
            if (this.hdrSize != headerBytes.Length)
                throw new FileTypeLoadException("file size in header does not match.");
            if (this.hdrTileHeight != 24 || this.hdrTileWidth != 24)
                throw new FileTypeLoadException("Only 24x24 pixel tiles are supported.");
            if (this.hdrZero1 != 00 || hdrZero2 != 0 || this.hdrID1 != -1 || this.hdrID2 != 0x0D1A)
                throw new FileTypeLoadException("Invalid values encountered in header.");
            if (this.hdrImgStart >= fileLen || this.hdrIndexTilesetImagesList >= fileLen || this.hdrIndexImages >= fileLen)
                throw new FileTypeLoadException("Invalid header values: indices outside file range.");
            if (this.hdrNrOfTiles == 0)
                throw new FileTypeLoadException("Tileset files with 0 tiles are not supported!");
        }
    }

    public class PCTile: N64Tile
    {
        public override Int32 BitsPerColor { get { return 8; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        
        public PCTile(SupportedFileType origin, String sourceFileName, Bitmap tileImage, Byte index)
            : base(origin, sourceFileName, tileImage, null, index, 0)
        { }

        public override String ToString()
        {
            return SourceFileName + " tile #" + this.CellData.LowByte;
        }
    }
}