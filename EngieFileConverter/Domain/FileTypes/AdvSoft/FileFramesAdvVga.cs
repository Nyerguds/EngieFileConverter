using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.FileData.Agos;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    /// <summary>
    /// AdventureSoft / HorrorSoft VGA format (even files). Used by the AGOS engine.
    /// </summary>
    public class FileFramesAdvVga : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }

        public override int Width { get { return 0; } }
        public override int Height { get { return 0; } }

        public override string IdCode { get { return "AdvVga"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "AdvSoft VGA"; } }
        public override string[] FileExtensions { get { return new string[] { "vga" }; } }
        public override string LongTypeName { get { return "AdventureSoft VGA file"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 4; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return ArrayUtils.CloneArray(this.m_FramesList); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override bool ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            int dataLen = fileData.Length;
            if (dataLen < 16)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            int firstNonEmpty = 0;
            int headerEnd = -1;
            while (firstNonEmpty + 8 <= dataLen && (headerEnd = ArrayUtils.ReadInt32FromByteArrayBe(fileData, firstNonEmpty)) == 0)
            {
                if (ArrayUtils.ReadUInt32FromByteArrayBe(fileData, firstNonEmpty + 4) != 0)
                    throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                firstNonEmpty += 8;
            }
            if (headerEnd <= 0 || headerEnd >= dataLen || headerEnd % 8 != 0 || firstNonEmpty > headerEnd)
                throw new FileTypeLoadException("Invalid header length.");

            int frames = (headerEnd) / 8;
            if (frames == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            uint[] offsets = new uint[frames];
            ushort[] widths = new ushort[frames];
            ushort[] heights = new ushort[frames];
            bool[] compressedFlags = new bool[frames];
            int readOffset = 0;
            int index = 0;
            while (readOffset + 8 < dataLen && readOffset < headerEnd)
            {
                uint dataOffset = ArrayUtils.ReadUInt32FromByteArrayBe(fileData, readOffset);
                if (dataOffset != 0 && (dataOffset < headerEnd || dataOffset > dataLen))
                    throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                offsets[index] = dataOffset;
                ushort imageHeight = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, readOffset + 4);
                compressedFlags[index] = (imageHeight & 0x8000) != 0;
                heights[index] = (ushort)(imageHeight & 0x7FFF);
                ushort imagewidth = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, readOffset + 6);
                widths[index] = imagewidth;
                readOffset += 8;
                index++;
            }
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            int emptyFrames = 0;
            for (int i = 0; i < frames; ++i)
            {
                uint imageOffset = offsets[i];
                int imageHeight = heights[i];
                int imageWidth = widths[i];
                bool compressed = compressedFlags[i];
                int dataStride = ImageUtils.GetMinimumStride(imageWidth, 4);
                int neededDataSize = imageHeight * dataStride;
                Bitmap frameImage;
                if (imageHeight == 0 || imageWidth == 0 || imageOffset == 0)
                {
                    frameImage = null;
                    emptyFrames++;
                }
                else
                {
                    // Skip any 0 entries following this one to get the actual offset following this one,
                    // to determine the data length to read.
                    uint dataEnd;
                    int skip = 0;
                    while ((dataEnd = (i + skip + 1 < frames ? offsets[i + skip + 1] : (uint)fileData.LongLength)) == 0)
                        skip++;
                    if (dataEnd < imageOffset)
                        throw new FileTypeLoadException("Data offsets are not consecutive.");
                    uint dataSize = dataEnd - imageOffset;
                    if (!compressed)
                    {
                        if (neededDataSize > dataSize)
                            throw new FileTypeLoadException("Invalid data length.");
                        byte[] data = new byte[neededDataSize];
                        Array.Copy(fileData, imageOffset, data, 0, neededDataSize);
                        frameImage = ImageUtils.BuildImage(data, imageWidth, imageHeight, dataStride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                    }
                    else
                    {
                        byte[] data = new byte[dataSize];
                        Array.Copy(fileData, imageOffset, data, 0, dataSize);
                        byte[] outbuff = AgosCompression.DecodeImage(data, null, null, imageHeight, dataStride);
                        frameImage = ImageUtils.BuildImage(outbuff, imageWidth, imageHeight, dataStride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                    }
                }
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, sourcePath, i);
                frame.SetNeedsPalette(true);
                frame.SetBitsPerColor(4);
                frame.SetFileClass(FileClass.Image4Bit);
                if (compressed)
                    frame.SetExtraInfo("Compressed with vertical RLE");
                else if (frameImage == null)
                    frame.SetExtraInfo("Empty frame");
                this.m_FramesList[i] = frame;
            }
            this.ExtraInfo = "Non-empty frames: " + (frames - emptyFrames) + "\n"
                             + "Empty frames: " + emptyFrames;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            return new Option[] { new Option("NOCMP", OptionInputType.Boolean, "Don't use compression", null) };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null || fileToSave.Frames.Length == 0)
                throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
            bool noCompression = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "NOCMP"));
            int nrOfFr = fileToSave.Frames.Length;
            byte[][] data = new byte[nrOfFr][];
            int[] offsets = new int[nrOfFr];
            int[] widths = new int[nrOfFr];
            int[] heights = new int[nrOfFr];
            bool[] compressed = new bool[nrOfFr];
            int offset = nrOfFr*8;
            for (int i = 0; i < nrOfFr; ++i)
            {
                SupportedFileType frame = fileToSave.Frames[i];
                Bitmap image = frame.GetBitmap();
                if (image == null)
                {
                    // Save empty frame
                    // This code is technically not needed since the arrays get initialised on these values.
                    data[i] = null;
                    widths[i] = 0;
                    heights[i] = 0;
                    compressed[i] = false;
                    offsets[i] = 0;
                }
                else if (frame.BitsPerPixel != 4)
                    throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 4);
                else
                {
                    int width = image.Width;
                    int height = image.Height;
                    int stride;
                    byte[] byteData = ImageUtils.GetImageData(image, out stride, true);
                    data[i] = byteData;
                    compressed[i] = false;
                    if (!noCompression)
                    {
                        byte[] dataCompr = AgosCompression.EncodeImage(byteData, stride);
                        if (dataCompr.Length < byteData.Length)
                        {
                            data[i] = dataCompr;
                            compressed[i] = true;
                        }
                    }
                    widths[i] = width;
                    heights[i] = height;
                    offsets[i] = offset;
                    offset += data[i].Length;
                }
            }
            byte[] finalFile = new byte[offset];
            for (int i = 0; i < nrOfFr; ++i)
            {
                int indexOffset = i * 8;
                ArrayUtils.WriteInt32ToByteArrayBe(finalFile, indexOffset, offsets[i]);
                int height = heights[i];
                if (compressed[i])
                    height |= 0x8000;
                ArrayUtils.WriteUInt16ToByteArrayBe(finalFile, indexOffset + 4, (ushort)height);
                ArrayUtils.WriteUInt16ToByteArrayBe(finalFile, indexOffset + 6, (ushort)widths[i]);
                if (data[i] != null)
                    Array.Copy(data[i], 0, finalFile, offsets[i], data[i].Length);
            }
            return finalFile;
        }

    }
}