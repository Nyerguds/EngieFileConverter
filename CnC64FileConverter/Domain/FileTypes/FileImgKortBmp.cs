using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileImgKortBmp : SupportedFileType
    {
        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 240; } }
        protected SupportedFileType[] m_frames;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KORTBmp"; } }
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        public override String ShortTypeDescription { get { return "KORT animation file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 8; } }

        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return true; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return m_frames; } }
        /// <summary>If the type supports frames, this determines whether an overview-frame is available as index '-1'. If not, index 0 is accessed directly.</summary>
        public override Boolean RenderCompositeFrame { get { return false; } }


        public FileImgKortBmp() { }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData);
            SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            Int32 datalen = fileData.Length;
            if (datalen < 4)
                throw new FileTypeLoadException("Bad header size.");
            Int32 nrOfFrames = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            if (nrOfFrames < 0)
                throw new FileTypeLoadException("Bad number of frames.");
            Int32 fixed0016 = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            if (fixed0016 != 0x0016)
                throw new FileTypeLoadException("Bad value in header.");
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, false, false);
            Int32 offset = 4;
            m_frames = new SupportedFileType[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; i++)
            {
                if (offset + 12 >= datalen)
                    throw new FileTypeLoadException("File is too short to contain frame header " + i);
                Int32 frameNumber = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true);
                if (frameNumber != i)
                    throw new FileTypeLoadException("Bad frame order in file!");
                Int32 frWidth = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset + 2, 2, true);
                Int32 frHeight = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 2, true);
                Int32 stride = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset + 6, 2, true);
                if (frWidth > stride)
                    throw new FileTypeLoadException("Inconsistent data in file!");
                Int32 dataSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 8, 4, true);
                if (dataSize < 0 || offset + dataSize >= datalen)
                    throw new FileTypeLoadException("File is too short to contain data of frame " + i);
                offset += 12;
                Byte[] frameData = new Byte[dataSize];
                Array.Copy(fileData, offset, frameData, 0, dataSize);
                Byte[] flippedData = new Byte[dataSize];
                for (Int32 y = 0; y < frHeight; y++)
                    Array.Copy(frameData, (frHeight - 1 - y) * frWidth, flippedData, y * frWidth, frWidth);
                Bitmap frameBitmap = ImageUtils.BuildImage(flippedData, frWidth, frHeight, ImageUtils.GetMinimumStride(frWidth, 8), PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFile(frameBitmap, 256, "frame_" + frameNumber.ToString("D5"));
                frame.FrameParent = this;
                this.m_frames[i] = frame;
                offset += dataSize;
            }
            this.m_LoadedImage = null;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            if (!fileToSave.ContainsFrames)
                throw new NotSupportedException("No frames in input type!");
            foreach (SupportedFileType frame in fileToSave.Frames)
                if (frame == null || frame.BitsPerColor != 8)
                    throw new NotSupportedException("Not all frames in input type are 8-bit images!");
            Int32 frames = fileToSave.Frames.Length;
            Byte[][] frameData = new Byte[frames][];
            Int32[] widths = new Int32[frames];
            Int32[] heights = new Int32[frames];
            Int32[] strides = new Int32[frames];
            for (Int32 i = 0; i < frames; i++)
            {
                Bitmap bm = fileToSave.Frames[i].GetBitmap();
                Int32 stride;
                Byte[] frameDataRaw = ImageUtils.GetImageData(bm, out stride);
                Int32 width = bm.Width;
                Int32 height = bm.Height;
                Byte[] flippedData = new Byte[width * height];
                for (Int32 y = 0; y < height; y++)
                    Array.Copy(frameDataRaw, (height - 1 - y) * stride, flippedData, y * width, width);
                frameData[i] = flippedData;
                widths[i] = width;
                heights[i] = height;
                strides[i] = width;
            }
            Int32 fullSize = 4 + frames * 12 + frameData.Sum(x => x.Length);
            Byte[] fullData = new Byte[fullSize];
            ArrayUtils.WriteIntToByteArray(fullData, 0, 2, true, (UInt32)frames);
            ArrayUtils.WriteIntToByteArray(fullData, 2, 2, true, 0x16);
            Int32 offset = 4;
            for (Int32 i = 0; i < frames; i++)
            {
                ArrayUtils.WriteIntToByteArray(fullData, offset + 0, 2, true, (UInt32)i);
                ArrayUtils.WriteIntToByteArray(fullData, offset + 2, 2, true, (UInt32)widths[i]);
                ArrayUtils.WriteIntToByteArray(fullData, offset + 4, 2, true, (UInt32)heights[i]);
                ArrayUtils.WriteIntToByteArray(fullData, offset + 6, 2, true, (UInt32)strides[i]);
                Int32 datalength = frameData[i].Length;
                ArrayUtils.WriteIntToByteArray(fullData, offset + 8, 4, true, (UInt32)datalength);
                offset += 12;
                Array.Copy(frameData[i], 0, fullData, offset, datalength);
                offset += datalength;
            }
            return fullData;
        }
    }
}