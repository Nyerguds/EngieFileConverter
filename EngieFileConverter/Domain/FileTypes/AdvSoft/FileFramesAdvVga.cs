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

        public override Int32 Width { get { return 0; } }
        public override Int32 Height { get { return 0; } }

        public override String IdCode { get { return "AdvVga"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "AdvSoft VGA"; } }
        public override String[] FileExtensions { get { return new String[] { "vga" }; } }
        public override String LongTypeName { get { return "AdventureSoft VGA file"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 4; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return ArrayUtils.CloneArray(this.m_FramesList); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            Int32 dataLen = fileData.Length;
            if (dataLen < 16)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            Int32 firstNonEmpty = 0;
            Int32 headerEnd = -1;
            while (firstNonEmpty + 8 <= dataLen && (headerEnd = ArrayUtils.ReadInt32FromByteArrayBe(fileData, firstNonEmpty)) == 0)
            {
                if (ArrayUtils.ReadUInt32FromByteArrayBe(fileData, firstNonEmpty + 4) != 0)
                    throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                firstNonEmpty += 8;
            }
            if (headerEnd <= 0 || headerEnd >= dataLen || headerEnd % 8 != 0 || firstNonEmpty > headerEnd)
                throw new FileTypeLoadException("Invalid header length.");

            Int32 frames = (headerEnd) / 8;
            if (frames == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            UInt32[] offsets = new UInt32[frames];
            UInt16[] widths = new UInt16[frames];
            UInt16[] heights = new UInt16[frames];
            Boolean[] compressedFlags = new Boolean[frames];
            Int32 readOffset = 0;
            Int32 index = 0;
            while (readOffset + 8 < dataLen && readOffset < headerEnd)
            {
                UInt32 dataOffset = ArrayUtils.ReadUInt32FromByteArrayBe(fileData, readOffset);
                if (dataOffset != 0 && (dataOffset < headerEnd || dataOffset > dataLen))
                    throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                offsets[index] = dataOffset;
                UInt16 imageHeight = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, readOffset + 4);
                compressedFlags[index] = (imageHeight & 0x8000) != 0;
                heights[index] = (UInt16)(imageHeight & 0x7FFF);
                UInt16 imagewidth = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, readOffset + 6);
                widths[index] = imagewidth;
                readOffset += 8;
                index++;
            }
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            Int32 emptyFrames = 0;
            for (Int32 i = 0; i < frames; ++i)
            {
                UInt32 imageOffset = offsets[i];
                Int32 imageHeight = heights[i];
                Int32 imageWidth = widths[i];
                Boolean compressed = compressedFlags[i];
                Int32 dataStride = ImageUtils.GetMinimumStride(imageWidth, 4);
                Int32 neededDataSize = imageHeight * dataStride;
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
                    UInt32 dataEnd;
                    Int32 skip = 0;
                    while ((dataEnd = (i + skip + 1 < frames ? offsets[i + skip + 1] : (UInt32)fileData.LongLength)) == 0)
                        skip++;
                    if (dataEnd < imageOffset)
                        throw new FileTypeLoadException("Data offsets are not consecutive.");
                    UInt32 dataSize = dataEnd - imageOffset;
                    if (!compressed)
                    {
                        if (neededDataSize > dataSize)
                            throw new FileTypeLoadException("Invalid data length.");
                        Byte[] data = new Byte[neededDataSize];
                        Array.Copy(fileData, imageOffset, data, 0, neededDataSize);
                        frameImage = ImageUtils.BuildImage(data, imageWidth, imageHeight, dataStride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                    }
                    else
                    {
                        Byte[] data = new Byte[dataSize];
                        Array.Copy(fileData, imageOffset, data, 0, dataSize);
                        Byte[] outbuff = AgosCompression.DecodeImage(data, null, null, imageHeight, dataStride);
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

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return new Option[] { new Option("NOCMP", OptionInputType.Boolean, "Don't use compression", null) };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
                throw new ArgumentException("AdventureSoft VGA saving for single frame is not supported.", "fileToSave");
            if (fileToSave.Frames.Length == 0)
                throw new ArgumentException("No frames found in source data.", "fileToSave");
            Boolean noCompression = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "NOCMP"));
            Int32 nrOfFr = fileToSave.Frames.Length;
            Byte[][] data = new Byte[nrOfFr][];
            Int32[] offsets = new Int32[nrOfFr];
            Int32[] widths = new Int32[nrOfFr];
            Int32[] heights = new Int32[nrOfFr];
            Boolean[] compressed = new Boolean[nrOfFr];
            Int32 offset = nrOfFr*8;
            for (Int32 i = 0; i < nrOfFr; ++i)
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
                    throw new ArgumentException("AdventureSoft VGA frames need to be 4 BPP images.", "fileToSave");
                else
                {
                    Int32 width = image.Width;
                    Int32 height = image.Height;
                    Int32 stride;
                    Byte[] byteData = ImageUtils.GetImageData(image, out stride, true);
                    data[i] = byteData;
                    compressed[i] = false;
                    if (!noCompression)
                    {
                        Byte[] dataCompr = AgosCompression.EncodeImage(byteData, stride);
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
            Byte[] finalFile = new Byte[offset];
            for (Int32 i = 0; i < nrOfFr; ++i)
            {
                Int32 indexOffset = i * 8;
                ArrayUtils.WriteInt32ToByteArrayBe(finalFile, indexOffset, offsets[i]);
                Int32 height = heights[i];
                if (compressed[i])
                    height |= 0x8000;
                ArrayUtils.WriteUInt16ToByteArrayBe(finalFile, indexOffset + 4, (UInt16)height);
                ArrayUtils.WriteUInt16ToByteArrayBe(finalFile, indexOffset + 6, (UInt16)widths[i]);
                if (data[i] != null)
                    Array.Copy(data[i], 0, finalFile, offsets[i], data[i].Length);
            }
            return finalFile;
        }

    }
}