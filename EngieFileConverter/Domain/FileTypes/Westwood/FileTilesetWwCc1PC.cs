using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileTilesetWwCc1PC : SupportedFileType
    {
        public override String IdCode { get { return "WwTmp"; } }
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        // maybe add FrameSet input later... not supported for now though.
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }

        public override String[] FileExtensions { get { return new String[] { "icn", "tem", "win", "des", "sno" }; } }
        public override String ShortTypeName { get { return "C&C Tileset"; } }
        public override String ShortTypeDescription { get { return "Westwood Tileset File - C&C PC"; } }

        public override Int32 BitsPerPixel { get { return 8; } }
        public override Boolean NeedsPalette { get { return true; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }


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
        protected List<FileTileCc1Pc> m_TilesList;
        protected Byte[][] m_Tiles;
        protected Boolean[] m_TileUseList;
        protected Int32 m_CompositeFrameTilesWidth = 1;

        public Byte[][] GetRawTiles()
        {
            Byte[][] tiles = new Byte[this.m_Tiles.Length][];
            for (Int32 i = 0; i < tiles.Length; ++i)
                tiles[i] = ArrayUtils.CloneArray(this.m_Tiles[i]);
            return tiles;
        }

        public Boolean[] GetUsedEntriesList()
        {
            return ArrayUtils.CloneArray(this.m_TileUseList);
        }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_TilesList == null ? null : this.m_TilesList.Cast<SupportedFileType>().ToArray(); } }
        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.
        /// C&amp;C tileset files are bit of an edge case, though, since they contains no overall dimensions. Files with known tile names as filename get their X and Y from the tile info.
        /// </summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        private void LoadFromFileData(Byte[] fileData, String sourceFileName)
        {
            Int32 fileLen = fileData.Length;
            if (fileLen < 0x20)
                throw new FileTypeLoadException("File is not long enough to be a valid C&C tileset file.");
            try
            {
                this.ReadHeader(fileData);
            }
            catch (FileTypeLoadException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading header data: " + e.Message, e);
            }
            Int32 tileSize = this.hdrTileWidth * this.hdrTileHeight;
            Byte[] imagesList = new Byte[this.hdrNrOfTiles];
            if (this.hdrIndexTilesetImagesList + this.hdrNrOfTiles >= fileLen)
                throw new FileTypeLoadException("Tile info outside file range!");
            Array.Copy(fileData, this.hdrIndexTilesetImagesList, imagesList, 0, this.hdrNrOfTiles);
            Int32 actualImages = imagesList.Max(x => x == 0xFF ? -1 : (Int32)x) + 1;
            if (this.hdrImgStart + actualImages * tileSize > fileLen)
                throw new FileTypeLoadException("Tile image data outside file range!");
            Byte[] imagesIndex = new Byte[actualImages];
            Array.Copy(fileData, this.hdrIndexImages, imagesIndex, 0, actualImages);
            this.m_TilesList = new List<FileTileCc1Pc>();
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            this.m_Tiles = new Byte[this.hdrNrOfTiles][];
            this.m_TileUseList = new Boolean[imagesList.Length];
            Byte[] extraInfoList = new Byte[imagesList.Length];
            for (Int32 i = 0; i < imagesList.Length; ++i)
            {
                Byte dataIndex = imagesList[i];
                Boolean used = dataIndex != 0xFF;
                this.m_TileUseList[i] = used;
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
                FileTileCc1Pc cell = new FileTileCc1Pc(this, sourceFileName, tileImage, (Byte) i, used);
                if (used && imagesIndex[dataIndex] != 0)
                {
                    Byte imageIndexVal = imagesIndex[dataIndex];
                    cell.SetExtraInfo("Images index data: " + imageIndexVal.ToString("X2"));
                    extraInfoList[i] = imageIndexVal;
                }

                this.m_TilesList.Add(cell);
            }
            String[] extraInfo = Enumerable.Range(0, imagesList.Length).Where(i => imagesList[i] != 0xFF && imagesIndex[imagesList[i]] != 0).Select(x => x.ToString()).ToArray();
            if (extraInfo.Length > 0)
                this.ExtraInfo = "Extra image info on cell " + String.Join(", ", extraInfo);

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
            this.m_CompositeFrameTilesWidth = xDim;
            this.BuildFullImage();
        }

        private void ReadHeader(Byte[] fileData)
        {
            Int32 fileLen = fileData.Length;
            if (fileLen < 0x20)
                return;
            this.hdrTileWidth = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x00);
            this.hdrTileHeight = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x02);
            this.hdrNrOfTiles = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x04);
            this.hdrZero1 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x06);
            this.hdrSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x08);
            this.hdrImgStart = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0C);
            this.hdrZero2 = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x10);
            this.hdrID1 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x14);
            this.hdrID2 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x16);
            //61020000 == 0x261
            this.hdrIndexImages = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x18);
            //60020000 = 0x260
            this.hdrIndexTilesetImagesList = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x1C);
            if (this.hdrSize != fileData.Length)
                throw new FileTypeLoadException("File size in header does not match.");
            if (this.hdrTileHeight != 24 || this.hdrTileWidth != 24)
                throw new FileTypeLoadException("Only 24×24 pixel tiles are supported.");
            if (this.hdrZero1 != 00 || this.hdrZero2 != 0 || this.hdrID1 != -1 || this.hdrID2 != 0x0D1A)
                throw new FileTypeLoadException("Invalid values encountered in header.");
            if (this.hdrImgStart >= fileLen || this.hdrIndexTilesetImagesList >= fileLen || this.hdrIndexImages >= fileLen)
                throw new FileTypeLoadException("Invalid header values: indices outside file range.");
            if (this.hdrNrOfTiles == 0)
                throw new FileTypeLoadException("Tileset files with 0 tiles are not supported!");
        }

        protected void BuildFullImage()
        {
            Int32 nrOftiles = this.m_Tiles.Length;
            this.m_LoadedImage = ImageUtils.Tile8BitImages(this.m_Tiles, this.hdrTileWidth, this.hdrTileHeight, this.hdrTileWidth, nrOftiles, this.m_Palette, this.m_CompositeFrameTilesWidth);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException("Can only save 8 BPP images as this type.", "fileToSave");
            Byte[][] framesData;
            Int32 nrOfFrames;
            if (!fileToSave.IsFramesContainer)
            {
                Bitmap bitmap = fileToSave.GetBitmap();
                if (bitmap == null || bitmap.Width % 24 != 0 || bitmap.Height % 24 != 0)
                    throw new ArgumentException("The file dimensions are not a multiple of 24×24!", "fileToSave");
                Int32 nrOfFramesX = bitmap.Width / 24;
                Int32 nrOfFramesY = bitmap.Height / 24;
                nrOfFrames = nrOfFramesX * nrOfFramesY;
                framesData = new Byte[nrOfFrames][];
                if (nrOfFrames > 255)
                    throw new ArgumentException("Too many tiles in file!", "fileToSave");
                Int32 stride;
                Byte[] fullImageData = ImageUtils.GetImageData(bitmap, out stride);
                for (Int32 y = 0; y < nrOfFramesY; ++y)
                {
                    for (Int32 x = 0; x < nrOfFramesX; ++x)
                    {
                        Int32 index = y * nrOfFramesX + x;
                        framesData[index] = ImageUtils.CopyFrom8bpp(fullImageData, bitmap.Width, bitmap.Height, stride, new Rectangle(x * 24, y * 24, 24, 24));
                    }
                }
            }
            else
            {
                SupportedFileType[] frames = fileToSave.Frames;
                nrOfFrames = frames.Length;
                if (nrOfFrames > 255)
                    throw new ArgumentException("Too many tiles in file!", "fileToSave");
                framesData = new Byte[nrOfFrames][];
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    Bitmap bitmap;
                    if (frames[i] == null || (bitmap = frames[i].GetBitmap()) == null)
                        continue; // gonna allow this; this format has empty frames.
                        //throw new ArgumentException("Cannot handle empty frames!", "fileToSave");
                    if (bitmap.Width != 24 || bitmap.Height != 24)
                        throw new ArgumentException("All frames must be 24×24!", "fileToSave");
                    framesData[i] = ImageUtils.GetImageData(bitmap, true);
                }
            }
            Byte[][] tempFrames = new Byte[nrOfFrames][];
            Byte[] finalIndices = new Byte[nrOfFrames];
            Int32 actualFrames = 0;
            for (Int32 index = 0; index < nrOfFrames; ++index)
            {
                Byte[] frameData = framesData[index];
                if (frameData == null || frameData.All(b => b == 0))
                    finalIndices[index] = 0xFF;
                else
                {
                    Int32 foundIndex = -1;
                    for (Int32 i = 0; i < actualFrames; ++i)
                    {
                        if (tempFrames[i].SequenceEqual(frameData))
                        {
                            foundIndex = i;
                            break;
                        }
                    }
                    if (foundIndex != -1)
                        finalIndices[index] = (Byte)foundIndex;
                    else
                    {
                        finalIndices[index] = (Byte) actualFrames;
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
            Int32 indexTilesetImagesList = size;
            size += nrOfFrames;
            Int32 indexImages = size;
            size += actualFrames;
            Byte[] finalData = new Byte[size];

            // Width
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x00, 24);
            // Height
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x02, 24);
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x04, nrOfFrames);
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x08, size);
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x0C, indexImgStart);
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x14, 0xFFFF);
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x16, 0x0D1A);
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x18, indexImages);
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x1C, indexTilesetImagesList);

            for (Int32 i = 0; i < actualFrames; ++i)
                Array.Copy(tempFrames[i], 0, finalData, indexImgStart + tileLength * i, tileLength);
            // Not done: write data to offset indexImages. Because, no one really knows what it does.
            Array.Copy(finalIndices, 0, finalData, indexTilesetImagesList, finalIndices.Length);
            return finalData;
        }
    }

    public class FileTileCc1Pc: FileTileCc1N64
    {
        public override Int32 BitsPerPixel { get { return 8; } }
        public override String ShortTypeDescription { get { return "C&C terrain tile"; } }
        public override Boolean NeedsPalette { get { return true; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        public FileTileCc1Pc(SupportedFileType origin, String sourceFileName, Bitmap tileImage, Byte index, Boolean used)
            : base(origin, sourceFileName, tileImage, null, index, 0)
        {
            if (!used)
                this.ExtraInfo = "Unused block";
        }

        public void SetExtraInfo(String str)
        {
            this.ExtraInfo = str;
        }

        public override String ToString()
        {
            return this.SourceFileName + " tile #" + this.CellData.LowByte;
        }
    }
}