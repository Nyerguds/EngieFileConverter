using Nyerguds.GameData.Agos;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileFramesAdvVga : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }

        public override Int32 Width { get { return 0; } }
        public override Int32 Height { get { return 0; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "AdvSoft VGA"; } }
        public override String[] FileExtensions { get { return new String[] { "vga" }; } }
        public override String ShortTypeDescription { get { return "AdventureSoft VGA file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 4; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return new SaveOption[]
            {
                new SaveOption("NOCMP", SaveOptionType.Boolean, "Don't use compression", null)
            };
        }
        //public FileFramesElv() { }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData, null);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData, filename);
            SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < 16)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 firstNonEmpty = 0;
            Int32 headerEnd;
            while ((headerEnd = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, firstNonEmpty * 8, 4, false)) == 0)
                firstNonEmpty++;
            if (headerEnd < 0 || headerEnd >= fileData.Length || headerEnd % 8 != 0)
                throw new FileTypeLoadException("Invalid header length.");
            List<Int32> offsets = new List<Int32>();
            List<Int32> widths = new List<Int32>();
            List<Int32> heights = new List<Int32>();
            List<Boolean> compressedFlags = new List<Boolean>();
            Int32 readOffset = 0;

            while (readOffset + 8 < fileData.Length && readOffset < headerEnd)
            {
                Int32 dataOffset = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffset, 4, false);
                if (dataOffset < 0 || (dataOffset != 0 && dataOffset < headerEnd))
                    throw new FileTypeLoadException("Bad offset in header.");
                offsets.Add(dataOffset);
                Int32 imageHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffset + 4, 2, false);
                compressedFlags.Add((imageHeight & 0x8000) != 0);
                imageHeight = imageHeight & 0x7FFF;
                if (imageHeight < 0)
                    throw new FileTypeLoadException("Bad height in header.");
                heights.Add(imageHeight);
                Int32 imagewidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffset + 6, 2, false);
                if (imagewidth < 0)
                    throw new FileTypeLoadException("Bad width in header.");
                widths.Add(imagewidth);
                readOffset += 8;
            }
            Int32 frames = offsets.Count;
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 imageOffset = offsets[i];
                Int32 imageHeight = heights[i];
                Int32 imageWidth = widths[i];
                Boolean compressed = compressedFlags[i];
                Int32 dataEnd;
                Int32 skip = 0;
                // Skip any 0 entries following this one to get the actual offset following this one,
                // to determine the data length to read.
                while ((dataEnd = (i + skip + 1 < frames ? offsets[i + skip + 1] : fileData.Length)) == 0)
                    skip++;
                Int32 dataSize = dataEnd - imageOffset;
                Int32 dataStride = ImageUtils.GetMinimumStride(imageWidth, 4);
                Int32 neededDataSize = imageHeight * dataStride;
                Bitmap frameImage;

                if ((imageHeight == 0 && imageWidth == 0) || imageOffset == 0 || dataSize == 0)
                {
                    frameImage = null;
                }
                else if (!compressed)
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
                    Int32 stride = ImageUtils.GetMinimumStride(imageWidth, 4);
                    Byte[] outbuff = AgosCompression.DecodeImage(data, null, null, imageHeight, stride);
                    frameImage = ImageUtils.BuildImage(outbuff, imageWidth, imageHeight, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                }
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this.ShortTypeName, frameImage, sourcePath, i);
                frame.SetColorsInPalette(0);
                frame.SetBitsPerColor(4);
                this.m_FramesList[i] = frame;
            }
        }
        
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null)
                throw new NotSupportedException("File to save is empty!");
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
                throw new NotSupportedException("AdventureSoft VGA saving for single frame is not supported!");
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");
            Boolean noCompression = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "NOCMP"));
            Int32 nrOfFr = fileToSave.Frames.Length;
            Byte[][] data = new Byte[nrOfFr][];
            Int32[] offsets = new Int32[nrOfFr];
            Int32[] widths = new Int32[nrOfFr];
            Int32[] heights = new Int32[nrOfFr];
            Boolean[] compressed = new Boolean[nrOfFr];
            Int32 offset = nrOfFr*8;
            for (Int32 i = 0; i < nrOfFr; i++)
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
                else if (frame.BitsPerColor != 4)
                    throw new NotSupportedException("AdventureSoft VGA frames need to be 4 BPP images!");
                else
                {
                    Int32 width = image.Width;
                    Int32 height = image.Height;
                    Int32 stride;
                    Byte[] byteData = ImageUtils.GetImageData(image, out stride);
                    byteData = ImageUtils.CollapseStride(byteData, width, height, 4, ref stride);
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
            for (Int32 i = 0; i < nrOfFr; i++)
            {
                Int32 indexOffset = i * 8;
                ArrayUtils.WriteIntToByteArray(finalFile, indexOffset, 4, false, (UInt32)offsets[i]);
                Int32 height = heights[i];
                if (compressed[i])
                    height |= 0x8000;
                ArrayUtils.WriteIntToByteArray(finalFile, indexOffset + 4, 2, false, (UInt32)height);
                ArrayUtils.WriteIntToByteArray(finalFile, indexOffset + 6, 2, false, (UInt32)widths[i]);
                if (data[i] != null)
                    Array.Copy(data[i], 0, finalFile, offsets[i], data[i].Length);
            }
            return finalFile;
        }
        
    }
}