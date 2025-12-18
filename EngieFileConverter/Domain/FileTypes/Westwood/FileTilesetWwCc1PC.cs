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

        public override String IdCode { get { return "WwTmp"; } }
        public override String[] FileExtensions { get { return new String[] { "icn", "tem", "win", "des", "sno" }; } }
        public override String ShortTypeName { get { return "C&C Tileset"; } }
        public override String LongTypeName { get { return "Westwood Tileset File - C&C PC"; } }

        public override Int32 BitsPerPixel { get { return 8; } }
        public override Boolean NeedsPalette { get { return true; } }


        protected SupportedFileType[] m_FramesList;

        protected Boolean[] m_TileUseList;
                
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.
        /// C&amp;C tileset files are bit of an edge case, though, since they contains no overall dimensions. Files with known tile names as filename get their X and Y from the tile info.
        /// </summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        private void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            Int32 fileLen = fileData.Length;
            if (fileLen < 0x20)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            Int16 hdrWidth = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x00);
            Int16 hdrHeight = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x02);
            // Amount of icons to form the full icon set. Not necessarily the same as the amount of actual icons.
            Int16 hdrCount = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x04);
            // Always 0
            Int16 hdrAllocated = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x06);
            Int32 hdrSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x08);
            // Offset of start of actual icon data. Generally always 0x20
            Int32 hdrIconsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0C);
            // Offset of start of palette data. Probably always 0.
            Int32 hdrPalettesPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x10);
            // Offset of remaps data? Always fixed value "0x0D1AFFFF", which makes no sense as ptr.
            Int32 hdrRemapsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x14);
            // Offset of 'transparency flags'? Generally points to an empty array at the end of the file.
            Int32 hdrTransFlagPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x18);
            // Offset of actual icon set definition, defining for each index which icon data to use. FF for none.
            Int32 hdrMapPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x1C);
            
            // File size check
            if (hdrSize != fileData.Length)
                throw new FileTypeLoadException("File size in header does not match.");
            // Only allowing standard 24x24 size
            if (hdrHeight != 24 || hdrWidth != 24)
                throw new FileTypeLoadException("Only 24×24 pixel tiles are supported.");
            // Checking some normally hardcoded values
            if (hdrAllocated != 00 || hdrPalettesPtr != 0 || hdrRemapsPtr != 0x0D1AFFFF)
                throw new FileTypeLoadException("Invalid values encountered in header.");
            if (hdrCount == 0)
                throw new FileTypeLoadException("Tileset files with 0 tiles are not supported!");
            // Checking if data is all inside the file
            if (hdrIconsPtr >= fileLen || (hdrMapPtr + hdrCount) > fileLen)
                throw new FileTypeLoadException("Invalid header values: indices outside file range.");
            Int32 tileSize = hdrWidth * hdrHeight;
            // Maps the available images onto the full iconset definition
            Byte[] map = new Byte[hdrCount];
            Array.Copy(fileData, hdrMapPtr, map, 0, hdrCount);
            // Get max index plus one for real images count. Nothing in the file header actually specifies this directly.
            Int32 actualImages = map.Max(x => x == 0xFF ? -1 : (Int32)x) + 1;
            if (hdrTransFlagPtr + actualImages > fileLen)
                throw new FileTypeLoadException("Invalid header values: indices outside file range.");
            if (hdrIconsPtr + actualImages * tileSize > fileLen)
                throw new FileTypeLoadException("Tile image data outside file range!");
            Byte[] imagesIndex = new Byte[actualImages];
            Array.Copy(fileData, hdrTransFlagPtr, imagesIndex, 0, actualImages);
            m_FramesList = new SupportedFileType[map.Length];
            m_Palette = PaletteUtils.GenerateGrayPalette(8, TransparencyMask, false);
            Byte[][] tiles = new Byte[hdrCount][];
            m_TileUseList = new Boolean[map.Length];
            for (Int32 i = 0; i < map.Length; ++i)
            {
                Byte dataIndex = map[i];
                Boolean used = dataIndex != 0xFF;
                m_TileUseList[i] = used;
                Byte[] tileData = new Byte[tileSize];;
                if (used)
                {
                    Int32 offset = hdrIconsPtr + dataIndex * tileSize;
                    if ((offset + tileSize) > fileLen)
                        throw new FileTypeLoadException("Tile data outside file range");
                    Array.Copy(fileData, offset, tileData, 0, tileSize);
                }
                tiles[i] = tileData;
                Bitmap tileImage = ImageUtils.BuildImage(tileData, hdrWidth, hdrHeight, hdrWidth, PixelFormat.Format8bppIndexed, m_Palette, Color.Black);
                FileImageFrame cell = new FileImageFrame();
                cell.LoadFileFrame(this, this, tileImage, sourcePath, (Byte)i);
                cell.SetBitsPerColor(this.BitsPerPixel);
                cell.SetFileClass(this.FrameInputFileClass);
                cell.SetNeedsPalette(this.NeedsPalette);
                if (used)
                {
                    if (imagesIndex[dataIndex] != 0)
                    {
                        Byte imageIndexVal = imagesIndex[dataIndex];
                        cell.SetExtraInfo("Images index data: " + imageIndexVal.ToString("X2"));
                    }
                }
                else
                    cell.SetExtraInfo("Unused block");
                m_FramesList[i] = cell;
            }
            String[] extraInfo = Enumerable.Range(0, map.Length).Where(i => map[i] != 0xFF && imagesIndex[map[i]] != 0).Select(x => x.ToString()).ToArray();
            if (extraInfo.Length > 0)
                this.ExtraInfo = "Extra image info on cell " + String.Join(", ", extraInfo);
            // attempt width autodetect from filename
            Int32 xDim = -1;
            if (sourcePath != null)
            {
                String baseName = Path.GetFileNameWithoutExtension(sourcePath);
                foreach (TileInfo tileInfo in MapConversion.TILEINFO.Values)
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
        
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
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
                        continue;
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
        public override String LongTypeName { get { return "C&C terrain tile"; } }
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