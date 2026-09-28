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

        public override string IdCode { get { return "KortBmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "KORT BMP"; } }
        public override string[] FileExtensions { get { return new string[] { "bmp" }; } }
        public override string LongTypeName { get { return "KORT frames file"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return new bool[] { true }; } }

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
            int datalen = fileData.Length;
            if (datalen < 4)
                throw new FileTypeLoadException("Bad header size.");
            int nrOfFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            int fixed0016 = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 2);
            if (fixed0016 != 0x0016)
                throw new FileTypeLoadException("Bad value in header.");
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            int offset = 4;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                if (offset + 12 >= datalen)
                    throw new FileTypeLoadException("File is too short to contain frame header " + i);
                int frameNumber = ArrayUtils.ReadInt16FromByteArrayLe(fileData, offset);
                if (frameNumber != i)
                    throw new FileTypeLoadException("Bad frame order in file.");
                int frWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 2);
                int frHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 4);
                int stride = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 6);
                if (frWidth > stride)
                    throw new FileTypeLoadException("Inconsistent data in file.");
                int dataSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, offset + 8);
                if (offset + dataSize >= datalen)
                    throw new FileTypeLoadException("File is too short to contain data of frame " + i);
                offset += 12;
                byte[] frameData = new byte[dataSize];
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

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] {fileToSave};
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
            if (nrOfFrames > 0xFFFF)
                throw new FileTypeSaveException(ERR_FRAMES_OVERFLOW, 0xFFFF);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null)
                    throw new FileTypeSaveException(ERR_FRAMES_EMPTY);
                if (frame.BitsPerPixel != 8)
                    throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 8);
            }
            byte[][] frameData = new byte[nrOfFrames][];
            int[] widths = new int[nrOfFrames];
            int[] heights = new int[nrOfFrames];
            int[] strides = new int[nrOfFrames];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                Bitmap bm = frames[i].GetBitmap();
                int stride;
                byte[] frameDataRaw = ImageUtils.GetImageData(bm, out stride);
                int width = bm.Width;
                int height = bm.Height;
                byte[] flippedData = new byte[width * height];
                for (int y = 0; y < height; ++y)
                    Array.Copy(frameDataRaw, (height - 1 - y) * stride, flippedData, y * width, width);
                frameData[i] = flippedData;
                widths[i] = width;
                heights[i] = height;
                strides[i] = width;
            }
            int fullSize = 4 + nrOfFrames * 12 + frameData.Sum(x => x.Length);
            byte[] fullData = new byte[fullSize];
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0, (ushort)nrOfFrames);
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 2, 0x16);
            int offset = 4;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                ArrayUtils.WriteUInt16ToByteArrayLe(fullData, offset + 0, (ushort)i);
                ArrayUtils.WriteUInt16ToByteArrayLe(fullData, offset + 2, (ushort)widths[i]);
                ArrayUtils.WriteUInt16ToByteArrayLe(fullData, offset + 4, (ushort)heights[i]);
                ArrayUtils.WriteUInt16ToByteArrayLe(fullData, offset + 6, (ushort)strides[i]);
                int datalength = frameData[i].Length;
                ArrayUtils.WriteInt32ToByteArrayLe(fullData, offset + 8, datalength);
                offset += 12;
                Array.Copy(frameData[i], 0, fullData, offset, datalength);
                offset += datalength;
            }
            return fullData;
        }
    }
}