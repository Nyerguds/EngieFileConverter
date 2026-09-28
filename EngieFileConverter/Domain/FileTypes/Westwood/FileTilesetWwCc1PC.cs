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
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override string IdCode { get { return "WwTmp"; } }
        public override string[] FileExtensions { get { return new string[] { "icn", "tem", "win", "des", "sno" }; } }
        public override string ShortTypeName { get { return "C&C Tileset"; } }
        public override string LongTypeName { get { return "Westwood Tileset File - C&C PC"; } }

        public override int BitsPerPixel { get { return 8; } }
        public override bool NeedsPalette { get { return true; } }

        protected SupportedFileType[] m_FramesList;
        protected bool[] m_TileUseList;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false will not get an index -1 in the frames list.
        /// C&amp;C tileset files are bit of an edge case, though, since they contains no overall dimensions. Files with known tile names as filename get their X and Y from the tile info.
        /// </summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return true; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return new bool[] { true }; } }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        private void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            int fileLen = fileData.Length;
            if (fileLen < 0x20)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            short hdrWidth = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x00);
            short hdrHeight = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x02);
            // Amount of icons to form the full icon set. Not necessarily the same as the amount of actual icons.
            short hdrCount = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x04);
            // Always 0
            short hdrAllocated = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x06);
            int hdrSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x08);
            // Offset of start of actual icon data. Generally always 0x20
            int hdrIconsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0C);
            // Offset of start of palette data. Probably always 0.
            int hdrPalettesPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x10);
            // Offset of remaps data? Always fixed value "0x0D1AFFFF", which makes no sense as ptr.
            int hdrRemapsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x14);
            // Offset of 'transparency flags'? Generally points to an empty array at the end of the file.
            int hdrTransFlagPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x18);
            // Offset of actual icon set definition, defining for each index which icon data to use. FF for none.
            int hdrMapPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x1C);
            
            // File size check
            if (hdrSize != fileData.Length)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);
            // Only allowing standard 24x24 size
            if (hdrHeight != 24 || hdrWidth != 24)
                throw new FileTypeLoadException("Only 24×24 pixel tiles are supported.");
            // Checking some normally hardcoded values
            if (hdrAllocated != 00 || hdrPalettesPtr != 0) // || hdrRemapsPtr != 0x0D1AFFFF)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            if (hdrCount == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            // Checking if data is all inside the file
            if (hdrIconsPtr >= fileLen || (hdrMapPtr + hdrCount) > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            int tileSize = hdrWidth * hdrHeight;
            // Maps the available images onto the full iconset definition
            byte[] map = new byte[hdrCount];
            Array.Copy(fileData, hdrMapPtr, map, 0, hdrCount);
            // Get max index plus one for real images count. Nothing in the file header actually specifies this directly.
            int actualImages = map.Max(x => x == 0xFF ? -1 : (int)x) + 1;
            if (hdrTransFlagPtr + actualImages > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            if (hdrIconsPtr + actualImages * tileSize > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            byte[] imagesIndex = new byte[actualImages];
            Array.Copy(fileData, hdrTransFlagPtr, imagesIndex, 0, actualImages);
            m_FramesList = new SupportedFileType[map.Length];
            m_Palette = PaletteUtils.GenerateGrayPalette(8, TransparencyMask, false);
            byte[][] tiles = new byte[hdrCount][];
            m_TileUseList = new bool[map.Length];
            for (int i = 0; i < map.Length; ++i)
            {
                byte dataIndex = map[i];
                bool used = dataIndex != 0xFF;
                m_TileUseList[i] = used;
                byte[] tileData = new byte[tileSize];;
                if (used)
                {
                    int offset = hdrIconsPtr + dataIndex * tileSize;
                    if ((offset + tileSize) > fileLen)
                        throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                    Array.Copy(fileData, offset, tileData, 0, tileSize);
                }
                tiles[i] = tileData;
                Bitmap tileImage = ImageUtils.BuildImage(tileData, hdrWidth, hdrHeight, hdrWidth, PixelFormat.Format8bppIndexed, m_Palette, Color.Black);
                FileImageFrame cell = new FileImageFrame();
                cell.LoadFileFrame(this, this, tileImage, sourcePath, (byte)i);
                cell.SetBitsPerColor(this.BitsPerPixel);
                cell.SetFileClass(this.FrameInputFileClass);
                cell.SetNeedsPalette(this.NeedsPalette);
                if (used)
                {
                    if (imagesIndex[dataIndex] != 0)
                    {
                        byte imageIndexVal = imagesIndex[dataIndex];
                        cell.SetExtraInfo("Images index data: " + imageIndexVal.ToString("X2"));
                    }
                }
                else
                    cell.SetExtraInfo("Unused block");
                m_FramesList[i] = cell;
            }
            string[] extraInfo = Enumerable.Range(0, map.Length).Where(i => map[i] != 0xFF && imagesIndex[map[i]] != 0).Select(x => x.ToString()).ToArray();
            if (extraInfo.Length > 0)
                this.ExtraInfo = "Extra image info on cell " + String.Join(", ", extraInfo);
            // attempt width autodetect from filename
            int xDim = -1;
            if (sourcePath != null)
            {
                string baseName = Path.GetFileNameWithoutExtension(sourcePath);
                foreach (TileInfo tileInfo in MapConversion.TILEINFO_TD.Values)
                {
                    if (!String.Equals(baseName, tileInfo.TileName, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (tileInfo.Width * tileInfo.Height != hdrCount)
                        continue;
                    xDim = tileInfo.Width;
                    break;
                }
            }
            if (xDim == -1)
            {
                //try to fill in exactly square?
                xDim = 1;
            }
            this.m_LoadedImage = ImageUtils.Tile8BitImages(tiles, hdrWidth, hdrHeight, hdrWidth, tiles.Length, this.m_Palette, xDim);
        }
        
        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException("Can only save 8 BPP images as this type.", "fileToSave");
            byte[][] framesData;
            int hdrCount;
            if (!fileToSave.IsFramesContainer)
            {
                Bitmap bitmap = fileToSave.GetBitmap();
                if (bitmap == null || bitmap.Width % 24 != 0 || bitmap.Height % 24 != 0)
                    throw new ArgumentException("The file dimensions are not a multiple of 24×24.", "fileToSave");
                int nrOfFramesX = bitmap.Width / 24;
                int nrOfFramesY = bitmap.Height / 24;
                hdrCount = nrOfFramesX * nrOfFramesY;
                framesData = new byte[hdrCount][];
                if (hdrCount > 255)
                    throw new ArgumentException("Too many tiles in file.", "fileToSave");
                int stride;
                byte[] fullImageData = ImageUtils.GetImageData(bitmap, out stride);
                for (int y = 0; y < nrOfFramesY; ++y)
                {
                    for (int x = 0; x < nrOfFramesX; ++x)
                    {
                        int index = y * nrOfFramesX + x;
                        byte[] frameData = ImageUtils.CopyFrom8bpp(fullImageData, bitmap.Width, bitmap.Height, stride, new Rectangle(x * 24, y * 24, 24, 24));
                        framesData[index] = ArrayUtils.IsEmpty(frameData) ? null : frameData;
                    }
                }
            }
            else
            {
                SupportedFileType[] frames = fileToSave.Frames;
                hdrCount = frames.Length;
                if (hdrCount > 255)
                    throw new ArgumentException("Too many tiles in file.", "fileToSave");
                framesData = new byte[hdrCount][];
                for (int i = 0; i < hdrCount; ++i)
                {
                    Bitmap bitmap;
                    if (frames[i] == null || (bitmap = frames[i].GetBitmap()) == null)
                        continue;
                    if (bitmap.Width != 24 || bitmap.Height != 24)
                        throw new ArgumentException("All frames must be 24×24.", "fileToSave");
                    byte[] frameData = ImageUtils.GetImageData(bitmap, true);
                    framesData[i] = ArrayUtils.IsEmpty(frameData) ? null : frameData;
                }
            }

            byte[][] tempFrames = new byte[hdrCount][];
            byte[] finalIndices = new byte[hdrCount];
            int actualFrames = 0;
            for (int index = 0; index < hdrCount; ++index)
            {
                byte[] frameData = framesData[index];
                if (frameData == null)
                {
                    finalIndices[index] = 0xFF;
                }
                else
                {
                    int foundIndex = -1;
                    for (int i = 0; i < actualFrames; ++i)
                    {
                        if (ArrayUtils.ArraysAreEqual(tempFrames[i], frameData))
                        {
                            foundIndex = i;
                            break;
                        }
                    }
                    if (foundIndex != -1)
                    {
                        finalIndices[index] = (byte)foundIndex;
                    }
                    else
                    {
                        finalIndices[index] = (byte)actualFrames;
                        tempFrames[actualFrames] = frameData;
                        actualFrames++;
                    }
                }
            }
            // Order: (Header), (IconsPtr), (MapPtr), (TransFlagPtr)
            int tileLength = 24 * 24;
            int size = 0x20;
            int hdrIconsPtr = size;
            size += actualFrames * tileLength;
            int hdrMapPtr = size;
            size += hdrCount;
            int hdrTransFlagPtr = size;
            size += actualFrames;
            byte[] finalData = new byte[size];

            const int signature = 0x49474E45; // "ENGI". Original is typically 0x0D1AFFFF
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x00, 24); // hdrWidth
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x02, 24); // hdrHeight
            ArrayUtils.WriteInt16ToByteArrayLe(finalData, 0x04, (short)hdrCount);
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x08, (short)size);
            //ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x06, 0); // hdrAllocated
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x0C, (short)hdrIconsPtr);
            //ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x10, 0x00000000); // hdrPalettesPtr
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x14, signature); // hdrRemapsPtr
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x18, (short)hdrTransFlagPtr);
            ArrayUtils.WriteInt32ToByteArrayLe(finalData, 0x1C, (short)hdrMapPtr);

            for (int i = 0; i < actualFrames; ++i)
                Array.Copy(tempFrames[i], 0, finalData, hdrIconsPtr + tileLength * i, tileLength);
            // hdrTransFlagPtr is in between here, but nothing needs to be written to it.
            Array.Copy(finalIndices, 0, finalData, hdrMapPtr, finalIndices.Length);
            return finalData;
        }
    }
}