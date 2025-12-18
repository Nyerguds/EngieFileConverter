using Nyerguds.FileData.Compression;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Panning Image Set format from "Treasure Mountain" (1990).
    /// See https://moddingwiki.shikadi.net/wiki/Treasure_Mountain_Level_Format
    /// </summary>
    public class FileFramesTrMntPan : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override String IdCode { get { return "TrMountPan"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Treasure Mountain Pan"; } }
        public override String[] FileExtensions { get { return new String[] { "pan" }; } }
        public override String LongTypeName { get { return "Treasure Mountain Panning Image Set"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 4; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return null; } }

        const string ERR_FRAMES_MUL8 = "Frames of this format need to be a multiple of 8×8 pixels.";
        const string ERR_FRAMES_DIV = "Full image dimensions need to be exactly divisible by the given frame size.";
        const string ERR_TILES_OVERFLOW = "The total amount of unique 8×8 tiles is too large. One file can only contain {0} tiles.";
        const int TILES_MAX = 2047;
        const byte FLAG_VALUE = 0xA5;

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
            if (fileData.Length < 8)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            Int32 imgDataPos = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0) + 8;
            Int32 tilesX = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            Int32 tilesY = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            Int32 imgLen = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
            int frameTilesLen = tilesX * tilesY;
            // flag-based decompression
            int readOffs = imgDataPos;
            int writeOffs = 0;
            int imgEnd = imgDataPos + imgLen;
            // original data length, aligned to blocks of 32
            int expandValue = Math.Max((imgLen + 0x1F) / 0x20 * 0x1F, 0x100);
            byte[] buffer = new byte[expandValue];
            List<int> hiddenData = new List<int>();
            while (readOffs < imgEnd)
            {
                // increase buffer size
                if (writeOffs >= buffer.Length)
                {
                    byte[] buffer2 = new byte[buffer.Length + expandValue];
                    Array.Copy(buffer, 0, buffer2, 0, buffer.Length);
                    buffer = buffer2;
                }
                byte readValue = fileData[readOffs++];
                if (readValue != FLAG_VALUE)
                {
                    buffer[writeOffs++] = readValue;
                }
                else if (readOffs < imgEnd)
                {
                    byte repeatAmount = fileData[readOffs++];
                    if (repeatAmount == FLAG_VALUE)
                    {
                        buffer[writeOffs++] = repeatAmount;
                    }
                    else if (readOffs < imgEnd)
                    {
                        if (repeatAmount == 0)
                            hiddenData.Add(readOffs);
                        byte repeatValue = fileData[readOffs++];
                        int writeEnd = writeOffs + repeatAmount;
                        if (writeEnd >= buffer.Length)
                        {
                            byte[] buffer2 = new byte[buffer.Length + expandValue];
                            Array.Copy(buffer, 0, buffer2, 0, buffer.Length);
                            buffer = buffer2;
                        }
                        for (; writeOffs < writeEnd; ++writeOffs)
                        {
                            buffer[writeOffs] = repeatValue;
                        }
                    }
                }
            }
            // align to next full tile.
            int rem = writeOffs % 0x20;
            int imageDataLen = writeOffs - rem + (rem == 0 ? 0 : 0x20);
            byte[] imageData = new byte[imageDataLen];
            Array.Copy(buffer, 0, imageData, 0, writeOffs);
            Color[] palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            StringBuilder sb = new StringBuilder();
            sb.Append("X tiles: ").Append(tilesX)
                .Append("\nY tiles: ").Append(tilesY)
                .Append("\nTileset image data:\n * ").Append(writeOffs).Append(" bytes\n * ").Append(imageDataLen / 32).Append(" tiles");
            if (hiddenData.Count > 0)
            {
                sb.Append("\nHidden data @ ");
                
                string[] values = hiddenData.Select(val => val.ToString()).ToArray();
                string[] chars = hiddenData.Where(val => fileData[val] > 0x1F && fileData[val] < 0x80).Select(val => ((char)fileData[val]).ToString()).ToArray();
                sb.Append(String.Join(", ", values));
                sb.Append("\n = \"").Append(String.Join(String.Empty, chars)).Append("\"");
            }
            this.ExtraInfo = sb.ToString();
            readOffs = 8;
            // Read frames until reaching the image data
            List<Int32[]> frames = new List<Int32[]>();
            while (readOffs < imgDataPos)
            {
                Int32[] framePointers = new Int32[frameTilesLen];
                frames.Add(framePointers);
                writeOffs = 0;
                // Read tiles until the frame is filled.
                while (writeOffs < frameTilesLen)
                {
                    Int32 info = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, readOffs);
                    readOffs += 2;
                    if (info != 0x0FFF)
                    {
                        int added = (info & 0xF000) >> 12;
                        // Y-offset would be this value * 4, but the pointer is to the line in an 8 pixel wide image.
                        // So to get the byte pointer, multiply by 4 again, getting * 16, or << 4.
                        int pointer = (info & 0x0FFF) << 4;
                        int end = writeOffs + added + 1;
                        if (end > frameTilesLen)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "repeated amount of tiles exceeds current tile map size."));
                        end = Math.Min(end, frameTilesLen);
                        for (; writeOffs < end; ++writeOffs)
                        {
                            framePointers[writeOffs] = pointer;
                        }
                    }
                    else
                    {
                        // copy entire row from previous decompression.
                        if (writeOffs % tilesX != 0)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "tile map line copy commands are only supported at the start of a line."));
                        Int32 linecopy = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, readOffs);
                        readOffs += 2;
                        int rowStart = (linecopy & 0xFF) * tilesX;
                        int map = (linecopy & 0xFF00) >> 8;
                        int end = Math.Min(writeOffs + tilesX, frameTilesLen);
                        if (frames.Count <= map)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "tile map line copy command references unbuilt tile map."));
                        Int32[] copyPointers = frames[map];
                        for (; writeOffs < end; ++writeOffs)
                        {
                            framePointers[writeOffs] = copyPointers[rowStart++];
                        }
                    }
                }
            }
            /*/
            // Dump the frames list
            m_FramesList = new SupportedFileType[1];
            Bitmap tilesImg = ImageUtils.BuildImage(imageData, 8, imageDataLen / 4, 4, PixelFormat.Format4bppIndexed, palette, null);
            FileImageFrame tilesPic = new FileImageFrame();
            tilesPic.LoadFileFrame(this, this, tilesImg, sourcePath, 0);
            tilesPic.SetBitsPerColor(this.BitsPerPixel);
            tilesPic.SetFileClass(FileClass.Image4Bit);
            tilesPic.SetNeedsPalette(this.NeedsPalette);
            tilesPic.ExtraInfo = this.ExtraInfo;
            m_FramesList[0] = tilesPic;
            return;
            /*/
            // Build final images. Since the paste-method I got is only 8-bit, the image data is converted to 8-bit as in-between step.
            m_FramesList = new SupportedFileType[frames.Count];
            Int32 frameWidth = tilesX * 8;
            Int32 frameHeight = tilesY * 8;
            for (int i = 0; i < frames.Count; ++i)
            {
                int[] frameData = frames[i];
                byte[] fullFrame = new byte[tilesX * 8 * tilesY * 8];
                int curTileRowStart = 0;
                for (int y = 0; y < tilesY; ++y)
                {
                    int curTileIndex = curTileRowStart;
                    for (int x = 0; x < tilesX; ++x)
                    {
                        Byte[] tile4bpp = new Byte[32];
                        // Y-coord in 8-wide image to actual byte pointer
                        int ptr = frameData[curTileIndex++];
                        if (ptr + 32 <= imageDataLen)
                        {
                            Array.Copy(imageData, ptr, tile4bpp, 0, tile4bpp.Length);
                            Byte[] tile8bpp = ImageUtils.ConvertTo8Bit(imageData, 8, 8, ptr, 4, true);
                            ImageUtils.PasteOn8bpp(fullFrame, frameWidth, frameHeight, frameWidth, tile8bpp, 8, 8, 8, new Rectangle(x * 8, y * 8, 8, 8), null, true);
                        }
                    }
                    curTileRowStart += tilesX;
                }
                int stride = frameWidth;
                fullFrame = ImageUtils.ConvertFrom8Bit(fullFrame, frameWidth, frameHeight, 4, true, ref stride);
                Bitmap curFrImg = ImageUtils.BuildImage(fullFrame, frameWidth, frameHeight, stride, PixelFormat.Format4bppIndexed, palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(FileClass.Image4Bit);
                framePic.SetNeedsPalette(this.NeedsPalette);
                framePic.ExtraInfo = this.ExtraInfo;
                m_FramesList[i] = framePic;
            }
            //*/
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            int width;
            int height;
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave, out width, out height);
            // no options for cutting up the frame if there's multiple frames
            if (frames.Length > 1)
            {
                return new Option[0];
            }
            if (width / height > 0 && width % height == 0)
            {
                width = height;
            }
            else if (height / width > 0 && height % width == 0)
            {
                height = width;
            }
            else if (width % 128 == 0 && height % 128 == 0)
            {
                width = 128;
                height = 128;
            }
            Option[] opts = new Option[2];
            opts[0] = new Option("WDT", OptionInputType.Number, "Frame width", "8,", width.ToString(), true);
            opts[1] = new Option("HGT", OptionInputType.Number, "Frame height", "8,", height.ToString(), true);
            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            int width;
            int height;
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave, out width, out height);
            Int32 nrOfFrames = frames.Length;
            Int32 frameWidth = width;
            Int32 frameHeight = height;
            byte[][] frameBytes8bpp;
            if (nrOfFrames == 1)
            {
                // one frame: chop into sub-frames
                Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "WDT"), out frameWidth);
                Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "HGT"), out frameHeight);
                if (frameWidth % 8 != 0 || frameHeight % 8 != 0)
                    throw new ArgumentException(ERR_FRAMES_MUL8, "fileToSave");
                if (width % frameWidth != 0 || height % frameHeight != 0)
                    throw new ArgumentException(ERR_FRAMES_DIV, "fileToSave");
                Bitmap bm = frames[0].GetBitmap();
                int stride;
                byte[] data = ImageUtils.GetImageData(bm, out stride);
                // convert to 8bpp
                if (bm.PixelFormat == PixelFormat.Format4bppIndexed)
                {
                    data = ImageUtils.ConvertTo8Bit(data, width, height, 0, 4, true, ref stride);
                }
                int framesX = width / frameWidth;
                int framesY = height / frameHeight;
                nrOfFrames = framesX * framesY;
                frameBytes8bpp = new byte[nrOfFrames][];
                int lineOffs = 0;
                for (int y = 0; y < framesY; ++y)
                {
                    int offs = lineOffs;
                    for (int x = 0; x < framesX; ++x)
                    {
                        Rectangle cutout = new Rectangle(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                        frameBytes8bpp[offs] = ImageUtils.CopyFrom8bpp(data, width, height, stride, cutout);
                        offs++;
                    }
                    lineOffs += framesX;
                }
            }
            else
            {
                // multiple frames: convert to 8bpp
                frameBytes8bpp = new byte[nrOfFrames][];
                for (int i = 0; i < nrOfFrames; ++i)
                {
                    Bitmap bm = frames[i].GetBitmap();
                    int stride;
                    byte[] data = ImageUtils.GetImageData(bm, out stride);
                    if (bm.PixelFormat == PixelFormat.Format4bppIndexed)
                    {
                        data = ImageUtils.ConvertTo8Bit(data, width, height, 0, 4, true, ref stride);
                    }
                    frameBytes8bpp[i] = data;
                }

            }
            // Chop up into unique tiles.
            Int32[][] resultFrames = new Int32[nrOfFrames][];
            List<byte[]> uniqueTiles = new List<byte[]>();
            int tilesWidth = frameWidth / 8;
            int tilesHeight = frameHeight / 8;
            int frameTilesSize = tilesWidth * tilesHeight;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                byte[] frameData = frameBytes8bpp[i];
                Int32[] resultFrame = new int[frameTilesSize];
                resultFrames[i] = resultFrame;
                int tileLineOffset = 0;
                for (Int32 y = 0; y < frameHeight; y += 8)
                {
                    int tileOffset = tileLineOffset;
                    for (Int32 x = 0; x < frameWidth; x += 8)
                    {
                        byte[] curTile = ImageUtils.CopyFrom8bpp(frameData, frameWidth, frameHeight, frameWidth, new Rectangle(x, y, 8, 8));
                        int tileIndex = FindUniqueTile(uniqueTiles, curTile);
                        if (tileIndex > TILES_MAX)
                            throw new ArgumentException(String.Format(ERR_TILES_OVERFLOW, TILES_MAX), "fileToSave");
                        resultFrame[tileOffset] = tileIndex;
                        tileOffset++;
                    }
                    tileLineOffset += tilesWidth;
                }
            }
            // convert tiles back to 4-bit
            Int32 tilesetLength = uniqueTiles.Count;
            for (int i = 0; i < tilesetLength; ++i)
            {
                uniqueTiles[i] = ImageUtils.ConvertFrom8Bit(uniqueTiles[i], 8, 8, 4, true);
            }
            // Optimisation? Could sort so long beginnings and ends match up
            //OptimiseFrames(resultFrames, uniqueTiles);

            // Put image data into one large array.
            byte[] tileset = new byte[tilesetLength * 32];
            int tilesetOffset = 0;
            for (int i = 0; i < tilesetLength; ++i)
            {
                byte[] tile = uniqueTiles[i];
                Array.Copy(tile, 0, tileset, tilesetOffset, 32);
                tilesetOffset += 32;
            }
            //tileset = ImageUtils.ConvertFrom8Bit(tileset, 8, uniqueTiles.Count * 8, 4, true);

            // At this point, we have all the image data, and all the frames. The only thing left is to compress it all.
            // 1. Compress resultFrames. 2 bytes per tile is the absolute maximum possible.
            Byte[] resultFrameData = new byte[nrOfFrames * frameTilesSize * 2];
            int resultFrameDataOffset = 0;
            for (Int32 fr = 0; fr < nrOfFrames; ++fr)
            {
                Int32[] resultFrame = resultFrames[fr];
                int sourceDataLineOffs = 0;
                for (Int32 y = 0; y < tilesHeight; ++y)
                {
                    int sourceDataOffs = sourceDataLineOffs;
                    int lineEnd = sourceDataOffs + tilesWidth;
                    // first, check if repeat is a whole line.
                    int maxAhead = Math.Min(lineEnd, sourceDataOffs + 16);
                    int curVal = resultFrame[sourceDataOffs];
                    int repeat = 1;
                    while (sourceDataOffs + repeat < maxAhead && resultFrame[sourceDataOffs + repeat] == curVal)
                        repeat++;
                    // only look for earlier line if this isn't a full-width repeat.
                    int earlierLine = repeat == tilesWidth ? -1 : FindEarlierLine(resultFrames, tilesWidth, tilesHeight, fr, y);
                    if (earlierLine != -1)
                    {
                        ArrayUtils.WriteUInt16ToByteArrayLe(resultFrameData, resultFrameDataOffset, 0x0FFF);
                        resultFrameDataOffset += 2;
                        ArrayUtils.WriteUInt16ToByteArrayLe(resultFrameData, resultFrameDataOffset, (UInt16)earlierLine);
                        resultFrameDataOffset += 2;
                    }
                    else
                    {
                        sourceDataOffs += repeat;
                        int writeVal = (repeat - 1) << 12 | (curVal << 1);
                        ArrayUtils.WriteUInt16ToByteArrayLe(resultFrameData, resultFrameDataOffset, (UInt16)writeVal);
                        resultFrameDataOffset += 2;
                        while (sourceDataOffs < lineEnd)
                        {
                            maxAhead = Math.Min(lineEnd, sourceDataOffs + 16);
                            curVal = resultFrame[sourceDataOffs];
                            repeat = 1;
                            while (sourceDataOffs + repeat < maxAhead && resultFrame[sourceDataOffs + repeat] == curVal)
                                repeat++;
                            sourceDataOffs += repeat;
                            writeVal = (repeat - 1) << 12 | (curVal << 1);
                            ArrayUtils.WriteUInt16ToByteArrayLe(resultFrameData, resultFrameDataOffset, (UInt16)writeVal);
                            resultFrameDataOffset += 2;
                        }
                    }
                    sourceDataLineOffs += tilesWidth;
                }
            }
            // 2. Flag-based compression for image data.
            // tileset
            // Flag = FLAG_VALUE
            int imgSourceOffs = 0;
            int tilesetEnd = tileset.Length;
            int comprImgOffset = 0;
            byte[] comprImgData = new byte[tilesetEnd * 2];
            while (imgSourceOffs < tilesetEnd)
            {
                if (comprImgOffset + 3 >= comprImgData.Length)
                {
                    Byte[] newArr = new byte[comprImgData.Length + tilesetEnd];
                    Array.Copy(comprImgData, newArr, comprImgData.Length);
                    comprImgData = newArr;
                }
                byte curVal = tileset[imgSourceOffs];
                byte repeat = 1;
                // Since this stores unique 32 byte tiles, the max repeat can only be 31+32+31 = 94 anyway.
                int maxOffset = Math.Min(imgSourceOffs + FLAG_VALUE, tilesetEnd);
                while (imgSourceOffs + repeat < maxOffset && tileset[imgSourceOffs + repeat] == curVal)
                    repeat++;
                if (repeat == 1)
                {
                    if (curVal == FLAG_VALUE)
                    {
                        comprImgData[comprImgOffset++] = FLAG_VALUE;
                    }
                }
                else if (repeat > 3 || curVal == FLAG_VALUE)
                {
                    comprImgData[comprImgOffset++] = FLAG_VALUE;
                    comprImgData[comprImgOffset++] = repeat;
                    
                }
                else
                {
                    // Repeats of 2 or 3: just repeat the value.
                    for (int i = 1; i < repeat; i++)
                        comprImgData[comprImgOffset++] = curVal;
                }
                // For repeats of 2 and 3, just write one and repeat the whole loop.
                comprImgData[comprImgOffset++] = curVal;
                imgSourceOffs += repeat;

            }
            // Assemble everything. Header is 8 bytes.
            byte[] fileDataFinal = new byte[8 + resultFrameDataOffset + comprImgOffset];
            Array.Copy(resultFrameData, 0, fileDataFinal, 8, resultFrameDataOffset);
            Array.Copy(comprImgData, 0, fileDataFinal, 8 + resultFrameDataOffset, comprImgOffset);
            ArrayUtils.WriteUInt16ToByteArrayLe(fileDataFinal, 0, (UInt16)resultFrameDataOffset);
            ArrayUtils.WriteUInt16ToByteArrayLe(fileDataFinal, 2, (UInt16)tilesWidth);
            ArrayUtils.WriteUInt16ToByteArrayLe(fileDataFinal, 4, (UInt16)tilesHeight);
            ArrayUtils.WriteUInt16ToByteArrayLe(fileDataFinal, 6, (UInt16)comprImgOffset);
            return fileDataFinal;
        }

        private void OptimiseFrames(Int32[][] frames, List<Byte[]> tiles)
        {
            // Optimise: for each byte value, see if any tile exists that is fully filled with that byte value.
            // If so, find the tile with the longest range of that value at the end, and the tile with the longest
            // range of that value at the start, and make sure they are positioned behind each other. This optimises
            // the image compression later. Ensure that any swaps are applied to the frameData arrays as well.

            // 1. Find all tiles that are full.
            int tilesetLength = tiles.Count;
            List<int> fullTiles = new List<int>();
            for (int i = 0; i < tilesetLength; ++i)
            {
                byte[] currTile = tiles[i];
                byte firstVal = currTile[0];
                Boolean full = true;
                for (int j = 1; j < 32; ++j)
                {
                    if (currTile[j] != firstVal)
                    {
                        full = false;
                        break;
                    }
                }
                if (full)
                {
                    fullTiles.Add(i);
                }
            }
            // Found all full tiles.

        }

        private static int GetRepeat(int[] resultFrame, int sourceDataOffs, int maxAhead)
        {
            int curVal = resultFrame[sourceDataOffs];
            int repeat = 1;
            while (sourceDataOffs + repeat < maxAhead && resultFrame[sourceDataOffs + repeat] == curVal)
                repeat++;
            return repeat;
        }

        /// <summary>
        /// Find tile in list of unique tiles, and add this new tile to the list if it was not found.
        /// </summary>
        /// <param name="uniqueTiles">List of unique 8*8 byte tiles.</param>
        /// <param name="curTile">Current tile to find.</param>
        /// <returns>The index at which the tile was found or added.</returns>
        private int FindUniqueTile(List<byte[]> uniqueTiles, byte[] curTile)
        {
            Int32 length = uniqueTiles.Count;
            for (int i = 0; i < length; i++)
            {
                byte[] tile = uniqueTiles[i];
                bool match = true;
                for (int j = 0; j < 64; ++j)
                {
                    if (curTile[j] != tile[j])
                    {
                        match = false;
                        break;
                    }                    
                }
                if (match)
                {
                    return i;
                }
            }
            uniqueTiles.Add(curTile);
            return length;
        }

        private int FindEarlierLine(Int32[][] frames, int frameWidth, int frameHeight, int currentFrame, int currentOffset)
        {
            int[] resultFrameRow = new int[frameWidth];
            Array.Copy(frames[currentFrame], currentOffset * frameWidth, resultFrameRow, 0, frameWidth);
            int nrOfFrames = Math.Min(frames.Length, currentFrame + 1);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                int[] frame = frames[i];
                int frameEnd = i == currentFrame ? currentOffset : frameHeight;
                int lineOffs = 0;
                for (int y = 0; y < frameEnd; ++y)
                {
                    int offs = lineOffs;
                    bool isEqual = true;
                    for (int x = 0; x < frameWidth; ++x)
                    {
                        if (frame[offs] != resultFrameRow[x])
                        {
                            isEqual = false;
                            break;
                        }
                        offs++;
                    }
                    if (isEqual)
                    {
                        return i * 0x100 + y;
                    }
                    lineOffs += frameWidth;
                }
            }
            return -1;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out int width, out int height)
        {
            // Preliminary checks
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
            width = -1;
            height = -1;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm;
                if (frame == null || (bm = frame.GetBitmap()) == null)
                {
                    throw new ArgumentException(ERR_EMPTY_FRAMES, "fileToSave");
                }
                if (frame.BitsPerPixel != 4 && frame.BitsPerPixel != 8)
                {
                    throw new ArgumentException(ERR_INPUT_4BPP_8BPP, "fileToSave");
                }
                if (width == -1)
                {
                    width = frame.Width;
                    if (width % 8 != 0)
                        throw new ArgumentException(ERR_FRAMES_MUL8, "fileToSave");
                }
                else if (width != frame.Width)
                {
                    throw new ArgumentException(ERR_FRAMES_DIFF, "fileToSave");
                }
                if (height == -1)
                {
                    height = frame.Height;
                    if (height % 8 != 0)
                        throw new ArgumentException(ERR_FRAMES_MUL8, "fileToSave");
                }
                else if (height != frame.Height)
                {
                    throw new ArgumentException(ERR_FRAMES_DIFF, "fileToSave");
                }

                if (bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Byte[] imgData = ImageUtils.GetImageData(bm, true);
                    int dlen = imgData.Length;
                    for (int off = 0; off < dlen; ++off)
                    {
                        if (imgData[off] > 0x0F)
                            throw new ArgumentException("The images contain colors on indices higher than 15.", "fileToSave");
                    }
                }
            }
            return frames;
        }
    }
}
