using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageWwCelSaturn : SupportedFileType
    {

        const int HEADERSIZE = 0x14;
        const int PALSIZE = 0x300;

        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwSatCel"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood C&C Saturn Cel"; } }
        public override String[] FileExtensions { get { return new String[] { "cel" }; } }
        public override String LongTypeName { get { return "Westwood C&C Sega Saturn Map Image data"; } }
        public override Boolean NeedsPalette => false;
        public override Int32 BitsPerPixel => 8;

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < HEADERSIZE + PALSIZE)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            int matrixWidth = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 0x00);
            int matrixHeight = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 0x04);
            int nrOfTiles = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 0x08);
            int tiletype = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 0x0C);
            int type = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 0x10);
            int tileWidth;
            int tileHeight;

            switch (tiletype)
            {
                case 0:
                    // Size = 32. Not sure if these values are correct.
                    tileWidth = 8;
                    tileHeight = 4;
                    break;
                case 1:
                    //tileSize = 64.
                    tileWidth = 8;
                    tileHeight = 8;
                    break;
                case 2:
                case 3:
                    //tileSize = 128. Not sure if these values are correct.
                    tileWidth = 8;
                    tileHeight = 16;
                    break;
                case 4:
                    //tileSize = 256. Not sure if these values are correct.
                    tileWidth = 16;
                    tileHeight = 16;
                    break;
                default:
                    throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            }
            this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, 0x14, 256);
            int matrixSize = matrixWidth * matrixHeight * 2;
            int tileSize = tileWidth * tileHeight;
            int tilesSize = tileSize * nrOfTiles;
            int fullLength = HEADERSIZE + PALSIZE + tilesSize + matrixSize;

            if (fullLength != fileData.Length)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);

            byte[][] tiles = new byte[nrOfTiles][];
            int index = HEADERSIZE + PALSIZE;
            for (int i = 0; i < nrOfTiles; ++i)
            {
                tiles[i] = new byte[tileSize];
                Array.Copy(fileData, index, tiles[i], 0, tileSize);
                index += tileSize;
            }
            index = HEADERSIZE + PALSIZE + tilesSize;
            int fullWidth = matrixWidth * tileWidth;
            int fullHeight = matrixHeight * tileHeight;
            byte[] imageData = new byte[fullWidth * fullHeight];
            for (int y = 0; y < matrixHeight; ++y)
            {
                for (int x = 0; x < matrixWidth; ++x)
                {
                    int tileId = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, index);
                    index += 2;
                    if ((tileId % 1) == 1)
                        throw new FileTypeLoadException(ERR_MAKING_IMG);
                    tileId /= 2;
                    if (tileId >= nrOfTiles)
                    {
                        throw new FileTypeLoadException(ERR_MAKING_IMG);
                    }
                    ImageUtils.PasteOn8bpp(
                        imageData, fullWidth, fullHeight, fullWidth, tiles[tileId], tileWidth, tileHeight, tileWidth,
                        new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight), null, true);
                }
            }
            m_Width = fullWidth;
            m_Height = fullHeight;
            m_LoadedImage = ImageUtils.BuildImage(imageData, fullWidth, fullHeight, fullWidth, PixelFormat.Format8bppIndexed, m_Palette, null);
            ExtraInfo = String.Format("Image built up from {0} {1}×{2} chunks.", nrOfTiles, tileWidth, tileHeight);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE, "fileToSave");
            if (fileToSave.BitsPerPixel != 8)
                throw new FileTypeSaveException(String.Format(ERR_BPP_INPUT_EXACT, 8));
            Color[] palette = fileToSave.GetColors();
            Int32 width = image.Width;
            Int32 height = image.Height;
            const Int32 tileWidth = 8;
            const Int32 tileHeight = 8;
            if (width % tileWidth != 0 || height % tileHeight != 0)
                throw new FileTypeSaveException("Cannot save images that are not an exact multiple of 8 pixels.");
            // Cut into tiles. This method is pretty much just the one from the Dynamix BMP matrix image, but without swapping rows and columns.
            Int32 matrixWidth = width / tileWidth;
            Int32 matrixHeight = height / tileHeight;
            Int32 nrOfTiles = matrixWidth * matrixHeight;
            Int32 stride;
            Byte[] fullImageData = ImageUtils.GetImageData(image, out stride);
            Byte[][] allTiles = new Byte[nrOfTiles][];
            Int32[] tileMatrix = new Int32[nrOfTiles];
            UInt32[] tileHashes = new UInt32[nrOfTiles];
            // The Dictionary is used for a preliminary sorting of chunks into those with the same hash.
            // A secondary operation then checks which of these are actually equal.
            Dictionary<UInt32, List<Int32>> hashmap = new Dictionary<UInt32, List<Int32>>();
            int matrixIndex = 0;
            for (Int32 y = 0; y < matrixHeight; ++y)
            {
                for (Int32 x = 0; x < matrixWidth; ++x)
                {
                    Byte[] tileData = ImageUtils.CopyFrom8bpp(fullImageData, width, height, stride, new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight));
                    allTiles[matrixIndex] = tileData;
                    tileMatrix[matrixIndex] = matrixIndex;
                    UInt32 hash = Crc32.ComputeChecksum(tileData);
                    tileHashes[matrixIndex] = hash;
                    if (!hashmap.ContainsKey(hash))
                    {
                        hashmap.Add(hash, new List<Int32>(new Int32[] { matrixIndex }));
                    }
                    else
                    {
                        hashmap[hash].Add(matrixIndex);
                    }
                    matrixIndex++;
                }
            }
            // Detect and replace duplicates.
            Int32 currentActual = 0;
            Byte[][] allTilesActual = new Byte[nrOfTiles][];
            Int32[] translationTable = new Int32[nrOfTiles];
            for (Int32 i = 0; i < nrOfTiles; ++i)
            {
                Byte[] curData = allTiles[i];
                if (curData == null)
                    continue;
                allTilesActual[currentActual] = curData;
                translationTable[i] = currentActual;
                currentActual++;
                List<Int32> duplicates = hashmap[tileHashes[i]];
                if (duplicates.Count < 2)
                    continue;
                Int32 dupCount = duplicates.Count;
                for (Int32 j = 0; j < dupCount; ++j)
                {
                    Int32 dupIndex = duplicates[j];
                    if (dupIndex == i)
                        continue;
                    Byte[] dupData = allTiles[dupIndex];
                    // double-check if crc-equal data is actually equal.
                    if (!ArrayUtils.ArraysAreEqual(curData, dupData))
                        continue;
                    allTiles[dupIndex] = null;
                    tileMatrix[dupIndex] = i;
                }
            }
            const int max = Int16.MaxValue / 2;
            if (currentActual > max)
                throw new FileTypeSaveException("Too many unique 8x8 chunks in image; cannot address more than {0} tiles.", max);

            // Fix tile references to collapsed indices.
            for (Int32 i = 0; i < nrOfTiles; ++i)
                tileMatrix[i] = translationTable[tileMatrix[i]];

            int tileSize = tileWidth * tileHeight;
            int tilesSize = tileSize * currentActual;
            int matrixSize = nrOfTiles * 2;
            int fullLength = HEADERSIZE + PALSIZE + tilesSize + matrixSize;
            byte[] fullFile = new byte[fullLength];
            ArrayUtils.WriteInt32ToByteArrayBe(fullFile, 0x00, matrixWidth);
            ArrayUtils.WriteInt32ToByteArrayBe(fullFile, 0x04, matrixHeight);
            ArrayUtils.WriteInt32ToByteArrayBe(fullFile, 0x08, currentActual);
            ArrayUtils.WriteInt32ToByteArrayBe(fullFile, 0x0C, 1); // indicates 8x8 format
            ArrayUtils.WriteInt32ToByteArrayBe(fullFile, 0x10, 0);
            byte[] palData = ColorUtils.GetEightBitPaletteData(palette, true);
            Array.Copy(palData, 0, fullFile, HEADERSIZE, palData.Length);
            int index = HEADERSIZE + PALSIZE;
            for (Int32 i = 0; i < currentActual; ++i)
            {
                Array.Copy(allTilesActual[i], 0, fullFile, index, tileSize);
                index += tileSize;
            }
            index = HEADERSIZE + PALSIZE + tilesSize;
            for (Int32 i = 0; i < nrOfTiles; ++i)
            {
                ArrayUtils.WriteUInt16ToByteArrayBe(fullFile, index, (ushort)(tileMatrix[i] * 2));
                index += 2;
            }
            return fullFile;
        }
    }
}
