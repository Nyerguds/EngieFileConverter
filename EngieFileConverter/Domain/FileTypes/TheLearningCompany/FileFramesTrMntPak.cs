using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EngieFileConverter.Domain.FileTypes
{

    /// <summary>
    /// PAK Sprite format from "Treasure Mountain" (1990).
    /// See the <see href="https://moddingwiki.shikadi.net/wiki/PAK_Format_(The_Learning_Company)">PAK Format (The Learning Company)</see>
    /// page on the Shikadi modding wiki.
    /// </summary>
    public class FileFramesTrMntPak : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override string IdCode { get { return "TrMountPan"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Treasure Mountain Pak"; } }
        public override string[] FileExtensions { get { return new string[] { "pak" }; } }
        public override string LongTypeName { get { return "Treasure Mountain PAK sprites"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 4; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return new[] { true }; } }

        public string FrameOptions { get; private set; }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            if (fileData.Length < 12)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            Int32 imageCount = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0);
            if (imageCount == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            if (imageCount < 0)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            UInt16 magic1 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            UInt16 magic2 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
            if (magic1 != 0x3E || magic2 != 0x3A)
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            int indexEnd = 8 + imageCount * 2;
            if (fileData.Length < 12 + imageCount * 2)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            int indexEndVal = ArrayUtils.ReadInt32FromByteArrayLe(fileData, indexEnd);
            if (indexEndVal != -1)
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            SupportedFileType[] framesList = new SupportedFileType[imageCount];
            int hdrOffset = 8;
            int index = 0;
            Color[] palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            StringBuilder frameOpts = new StringBuilder();
            while (hdrOffset < indexEnd)
            {
                // read image offset
                int startOffs = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, hdrOffset) << 4;
                if (startOffs + 17 > fileData.Length)
                    throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                int readOffset = startOffs;
                hdrOffset += 2;
                int imgEnd = hdrOffset == readOffset ? fileData.Length : ArrayUtils.ReadUInt16FromByteArrayLe(fileData, hdrOffset) << 4;
                // read image header
                int start = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, readOffset); // Always 0x0000
                if (start != 0)
                    throw new FileTypeLoadException(ERR_BAD_HEADER);
                int byteWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, readOffset + 2); // Image width in bytes
                int height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, readOffset + 4); // Image height in lines
                if (byteWidth == 0 || height == 0)
                    throw new FileTypeLoadException(ERR_DIM_ZERO);
                int xOrigin = ArrayUtils.ReadInt16FromByteArrayLe(fileData, readOffset + 6); // X origin of image
                int yOrigin = ArrayUtils.ReadInt16FromByteArrayLe(fileData, readOffset + 8); // Y origin of image
                // More data, unknown format
                int extra1 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, readOffset + 10); // Unknown.
                int extra2 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, readOffset + 12); // Unknown.
                int extra3 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, readOffset + 14); // Unknown.
                readOffset += 16;
                // read RLE from this point until imgEnd
                // first: number of bytes to read per line. >80 means repeats are used, <80 means straight range.
                int imageLen = byteWidth * height;
                byte[] imageData = new byte[imageLen];
                int writeLineOffset = 0;
                while (writeLineOffset < imageLen && readOffset < imgEnd)
                {
                    byte firstByte = fileData[readOffset++];
                    byte byteLineLen = (byte)(firstByte & ~0x80);
                    bool isRepeats = (firstByte & 0x80) == 0;
                    if (isRepeats)
                    {
                        if (byteLineLen % 2 == 1)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "Length/Value pairs should always be an even number of bytes."));
                        int lineEnd = readOffset + byteLineLen;
                        int writeOffset = writeLineOffset;
                        int writeLineEnd = writeLineOffset + byteWidth;
                        while (readOffset < lineEnd)
                        {
                            byte length = fileData[readOffset++];
                            byte value = fileData[readOffset++];
                            if (writeOffset + length > writeLineEnd)
                                throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "Repeat result exceeds line length."));
                            for (; length > 0; --length)
                                imageData[writeOffset++] = value;
                        }
                        if (writeOffset < writeLineEnd)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "Repeat result did not fill a full line."));
                    }
                    else
                    {
                        if (byteLineLen != byteWidth)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, "Uncompressed lines should always have the width of the image."));
                        Array.Copy(fileData, readOffset, imageData, writeLineOffset, byteLineLen);
                        readOffset += byteLineLen;
                    }
                    writeLineOffset += byteWidth;
                }
                // create frame                
                Bitmap curFrImg = ImageUtils.BuildImage(imageData, byteWidth * 2, height, byteWidth, PixelFormat.Format4bppIndexed, palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, index);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(FileClass.Image4Bit);
                framePic.SetNeedsPalette(this.NeedsPalette);
                StringBuilder extraInfo = new StringBuilder();
                extraInfo.Append("Data: ").Append(readOffset- startOffs).Append(" bytes at offset ").Append(startOffs).Append(".");
                extraInfo.Append("\nX/Y origin: (").Append(xOrigin).Append(",").Append(yOrigin).Append(")");
                extraInfo.Append('\n').Append("Unknown 1: ").Append(extra1);
                extraInfo.Append('\n').Append("Unknown 2,3: (").Append(extra2).Append(",").Append(extra3).Append(")");
                framePic.ExtraInfo = extraInfo.ToString();
                frameOpts.Append(index).Append(": (").Append(xOrigin).Append(",").Append(yOrigin).Append("), ")
                    .Append(extra1).Append(", (").Append(extra2).Append(",").Append(extra3).AppendLine(")");
                framesList[index++] = framePic;
            }
            FrameOptions = frameOpts.ToString().TrimEnd('\r','\n');
            m_FramesList = framesList;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE);
            if (!fileToSave.IsFramesContainer)
                throw new ArgumentException(ERR_NEEDS_FRAMES);
            if(fileToSave.BitsPerPixel != 4 && fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(ERR_INPUT_4BPP_8BPP);
            SupportedFileType[] frames = fileToSave.Frames;
            string frameOpts;
            if (fileToSave is FileFramesTrMntPak ffp)
            {
                frameOpts = ffp.FrameOptions;
            }
            else
            {
                StringBuilder frOpts = new StringBuilder();
                for (int i = 0; i < frames.Length; ++i)
                {
                    frOpts.Append(i).AppendLine(": (0,0), 0, (0,0)");
                    SupportedFileType frame = frames[i];
                    Bitmap bm;
                    if (frame == null || (bm = frame.GetBitmap()) == null)
                        throw new ArgumentException(ERR_EMPTY_FRAMES);
                    if (frame.Width > 254)
                        throw new ArgumentException(String.Format(ERR_IMAGE_TOO_WIDE_DIM, 254), "fileToSave");
                    FileFramesTrMntPan.TestFourBit(bm, i, "fileToSave");
                }
                frameOpts = frOpts.ToString().TrimEnd('\r', '\n');
            }
            Option[] opts = new Option[1];
            opts[0] = new Option("FRO", OptionInputType.String,
                "Frame options. Use the format: \"0: (1,2), 3, (4,5)\"" +
                "\nTo get the original data, open this dialog for the original file, copy it out, and cancel the save.", null, frameOpts, true);
            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE);
            if (!fileToSave.IsFramesContainer)
                throw new ArgumentException(ERR_NEEDS_FRAMES);
            if (fileToSave.BitsPerPixel != 4 && fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(ERR_INPUT_4BPP_8BPP);
            SupportedFileType[] frames = fileToSave.Frames;
            // one frame: chop into sub-frames
            string opts = Option.GetSaveOptionValue(saveOptions, "FRO");
            Dictionary<int, int[]> frameOptions = new Dictionary<int, int[]>();
            if (opts != null)
            {
                //                          1      23   4        5            6        7     89   A      B          C
                Regex infoline = new Regex("(\\d+):((\\((-?\\d+),(-?\\d+)\\))|(\\d+?)),(\\d),((\\((\\d+),(\\d+)\\))|(\\d+?))");
                opts = opts.Replace(" ", String.Empty);
                MatchCollection matches = infoline.Matches(opts);
                
                foreach (Match match in matches)
                {
                    int[] info = new int[5];
                    int frame = Int32.Parse(match.Groups[1].Value);
                    if (frameOptions.ContainsKey(frame))
                        throw new ArgumentException("Duplicate key \"" + frame + "\" in frame options.");
                    if (!string.IsNullOrEmpty(match.Groups[3].Value))
                    {
                        // x and y offset
                        info[0] = Int32.Parse(match.Groups[4].Value);
                        info[1] = Int32.Parse(match.Groups[5].Value);
                    }
                    else if (!string.IsNullOrEmpty(match.Groups[6].Value))
                    {
                        info[0] = Int32.Parse(match.Groups[6].Value);
                        info[1] = info[0];
                    }
                    // bit flags
                    info[2] = Int32.Parse(match.Groups[7].Value);
                    if (!string.IsNullOrEmpty(match.Groups[9].Value))
                    {
                        // Unknown
                        info[3] = Int32.Parse(match.Groups[10].Value);
                        info[4] = Int32.Parse(match.Groups[11].Value);
                    }
                    else if (!string.IsNullOrEmpty(match.Groups[12].Value))
                    {
                        info[3] = Int32.Parse(match.Groups[12].Value);
                        info[4] = info[3];
                    }
                    if (info.Any(nr => nr > Int16.MaxValue))
                        throw new ArgumentException("Value too large in frame options for frame \"" + frame + "\".");
                    if (info.Any(nr => nr < Int16.MinValue))
                        throw new ArgumentException("Value too small in frame options for frame \"" + frame + "\".");
                    frameOptions.Add(frame, info);
                }
            }
            // Compression
            int nrOfFrames = frames.Length;
            int headerFramesOffs = 8;
            int headerLength = headerFramesOffs + nrOfFrames * 2;
            int headerLength16 = (headerLength + 15) / 16 * 16;
            byte[] header = new byte[headerLength16];
            ArrayUtils.WriteInt32ToByteArrayLe(header, 0, nrOfFrames);
            ArrayUtils.WriteInt16ToByteArrayLe(header, 4, 0x3E);
            ArrayUtils.WriteInt16ToByteArrayLe(header, 6, 0x3A);
            byte[][] frameData = new byte[nrOfFrames][];
            int currentFramePos = headerLength16;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm;
                if (frame == null || (bm = frame.GetBitmap()) == null)
                    throw new ArgumentException(ERR_EMPTY_FRAMES);
                if (bm.Width > 254)
                    throw new ArgumentException(String.Format(ERR_IMAGE_TOO_WIDE_DIM, 254), "fileToSave");
                byte[] frameBytes = FileFramesTrMntPan.TestFourBit(bm, i, "fileToSave", true, out int frStride);
                List<byte> curFrameData = new List<byte>();
                int frHeight = bm.Height;
                for (int y = 0; y < frHeight; y++) {
                    byte[] line = TryCompress(frameBytes, y, frStride);
                    curFrameData.AddRange(line);
                }
                byte[] curFrame = new byte[16 + curFrameData.Count];
                // because this saves the stride, not the image width, the width of the image will always become even.
                ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x2, (Int16)frStride);
                ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x4, (Int16)frHeight);
                if (frameOptions.TryGetValue(i, out int[] extraVals))
                {
                    ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x06, (Int16)extraVals[0]);
                    ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x08, (Int16)extraVals[1]);
                    ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x0A, (Int16)extraVals[2]);
                    ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x0C, (Int16)extraVals[3]);
                    ArrayUtils.WriteInt16ToByteArrayLe(curFrame, 0x0E, (Int16)extraVals[4]);
                }
                Array.Copy(curFrameData.ToArray(), 0, curFrame, 16, curFrameData.Count);
                frameData[i] = curFrame;
                ArrayUtils.WriteUInt16ToByteArrayLe(header, headerFramesOffs, (UInt16)(currentFramePos >> 4));
                headerFramesOffs += 2;
                currentFramePos = (currentFramePos + curFrame.Length + 15) / 16 * 16;
            }
            byte[] fullFile = new byte[currentFramePos];
            // Clear with FF. Start behind header as tiny optimisation.
            for (int i = headerLength; i < currentFramePos; ++i)
                fullFile[i] = 0xFF;
            Array.Copy(header, fullFile, headerLength);
            headerFramesOffs = 8;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                int currentFrameOffs = ArrayUtils.ReadUInt16FromByteArrayLe(header, headerFramesOffs) << 4;
                headerFramesOffs += 2;
                byte[] writeFrame = frameData[i];
                Array.Copy(writeFrame, 0, fullFile, currentFrameOffs, writeFrame.Length);
            }
            return fullFile;
        }

        private byte[] TryCompress(byte[] frameBytes, int y, int stride)
        {
            int index = stride * y;
            int endIndex = index + stride;
            byte[] lineData = new byte[stride + 1];
            int writeIndex = 1;
            int writeEndIndex = 1 + stride;
            bool useCompression = true;
            while (index < endIndex)
            {
                // Only write repeats if it is strictly smaller than copy.
                if (writeIndex + 2 >= writeEndIndex || writeIndex + 3 >= 0x100)
                {
                    useCompression = false;
                    break;
                }
                byte curVal = frameBytes[index];
                int repeat = 1;
                while (index + repeat < endIndex && repeat < 0xFF && frameBytes[index + repeat] == curVal)
                    repeat++;
                lineData[writeIndex++] = (byte)repeat;
                lineData[writeIndex++] = curVal;
                index += repeat;
            }
            if (useCompression)
            {
                lineData[0] = (byte)(writeIndex - 1);
                byte[] compr = new byte[writeIndex];
                Array.Copy(lineData, compr, writeIndex);
                return compr;
            }
            else
            {
                lineData[0] = (byte)(0x80 | stride);
                Array.Copy(frameBytes, stride * y, lineData, 1, stride);
                return lineData;
            }
        }
    }
}
