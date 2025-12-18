using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileFramesKortBmp : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override String IdCode { get { return "KortBmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KORT BMP"; } }
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        public override String LongTypeName { get { return "KORT frames file"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

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
            Int32 datalen = fileData.Length;
            if (datalen < 4)
                throw new FileTypeLoadException("Bad header size.");
            Int32 nrOfFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            Int32 fixed0016 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 2);
            if (fixed0016 != 0x0016)
                throw new FileTypeLoadException("Bad value in header.");
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            Int32 offset = 4;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                if (offset + 12 >= datalen)
                    throw new FileTypeLoadException("File is too short to contain frame header " + i);
                Int32 frameNumber = ArrayUtils.ReadInt16FromByteArrayLe(fileData, offset);
                if (frameNumber != i)
                    throw new FileTypeLoadException("Bad frame order in file!");
                Int32 frWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 2);
                Int32 frHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 4);
                Int32 stride = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 6);
                if (frWidth > stride)
                    throw new FileTypeLoadException("Inconsistent data in file!");
                Int32 dataSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, offset + 8);
                if (offset + dataSize >= datalen)
                    throw new FileTypeLoadException("File is too short to contain data of frame " + i);
                offset += 12;
                Byte[] frameData = new Byte[dataSize];
                Array.Copy(fileData, offset, frameData, 0, dataSize);
                Bitmap frameImage = (frWidth != 0 && frHeight!= 0) ? ImageUtils.BuildImage(frameData, frWidth, frHeight, stride, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black) : null;
                // reorder lines
                if (frameImage != null)
                    frameImage.RotateFlip(RotateFlipType.Rotate180FlipX);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetNeedsPalette(true);
                this.m_FramesList[i] = frame;
                offset += dataSize;
            }
            this.m_LoadedImage = null;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] {fileToSave};
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException("No frames found in source data!", "fileToSave");
            if (nrOfFrames > 0xFFFF)
                throw new ArgumentException("Too many frames in source data!", "fileToSave");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null)
                    throw new ArgumentException("KORT BMP can't handle empty frames!", "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException("Not all frames in input type are 8-bit images!", "fileToSave");
            }
            Byte[][] frameData = new Byte[nrOfFrames][];
            Int32[] widths = new Int32[nrOfFrames];
            Int32[] heights = new Int32[nrOfFrames];
            Int32[] strides = new Int32[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Bitmap bm = frames[i].GetBitmap();
                Int32 stride;
                Byte[] frameDataRaw = ImageUtils.GetImageData(bm, out stride);
                Int32 width = bm.Width;
                Int32 height = bm.Height;
                Byte[] flippedData = new Byte[width * height];
                for (Int32 y = 0; y < height; ++y)
                    Array.Copy(frameDataRaw, (height - 1 - y) * stride, flippedData, y * width, width);
                frameData[i] = flippedData;
                widths[i] = width;
                heights[i] = height;
                strides[i] = width;
            }
            Int32 fullSize = 4 + nrOfFrames * 12 + frameData.Sum(x => x.Length);
            Byte[] fullData = new Byte[fullSize];
            ArrayUtils.WriteInt16ToByteArrayLe(fullData, 0, nrOfFrames);
            ArrayUtils.WriteInt16ToByteArrayLe(fullData, 2, 0x16);
            Int32 offset = 4;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                ArrayUtils.WriteInt16ToByteArrayLe(fullData, offset + 0, i);
                ArrayUtils.WriteInt16ToByteArrayLe(fullData, offset + 2, widths[i]);
                ArrayUtils.WriteInt16ToByteArrayLe(fullData, offset + 4, heights[i]);
                ArrayUtils.WriteInt16ToByteArrayLe(fullData, offset + 6, strides[i]);
                Int32 datalength = frameData[i].Length;
                ArrayUtils.WriteInt32ToByteArrayLe(fullData, offset + 8, datalength);
                offset += 12;
                Array.Copy(frameData[i], 0, fullData, offset, datalength);
                offset += datalength;
            }
            return fullData;
        }
    }
}