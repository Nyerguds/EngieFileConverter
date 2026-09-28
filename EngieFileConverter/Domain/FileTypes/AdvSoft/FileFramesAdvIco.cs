using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// AdventureSoft / HorrorSoft ICO forma; a very simple 4bpp planar image. Used by the AGOS engine.
    /// </summary>
    public class FileFramesAdvIco : SupportedFileType
    {
        protected const int iconWidth = 24;
        protected const int iconHeight = 24;

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }

        public override int Width { get { return fullWidth; } }
        public override int Height { get { return fullHeight; } }
        protected int fullWidth = iconWidth;
        protected int fullHeight = iconHeight;

        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        public override string IdCode { get { return "AdvIco"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "AdvSoft Icons"; } }
        public override string[] FileExtensions { get { return new string[] { "dat" }; } }
        public override string LongTypeName { get { return "AdventureSoft icons file"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 4; } }

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

        public void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            int iconDataStride = iconWidth / 8;
            int iconDataHeight = iconHeight * 4;
            int iconDataSize = iconDataStride * iconDataHeight;

            if (fileData.Length < iconDataSize)
                throw new FileTypeLoadException("Not long enough.");
            if (fileData.Length % iconDataSize != 0)
                throw new FileTypeLoadException("Not a multiple of 4-bit " + iconWidth + "×" + iconHeight + " tiles.");
            int frames = fileData.Length / iconDataSize;
            byte[][] framesList = new byte[frames][];
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            int frameSize = iconWidth * iconHeight;
            for (int i = 0; i < frames; ++i)
            {
                // Should probably convert this to use the planattolinear function...
                int offset = i * iconDataSize;
                byte[] frameData = new byte[iconDataSize];
                Array.Copy(fileData, offset, frameData, 0, iconDataSize);
                int stride = iconDataStride;
                byte[] frame8bit1 = ImageUtils.ConvertTo8Bit(frameData, iconWidth, iconDataHeight, 0, 1, true, ref stride);
                byte[] frame8bit4 = new byte[frameSize];
                // Go over all pixels of the image, and combine them per 4.
                for (int fr = 0; fr < frameSize; ++fr)
                {
                    frame8bit4[fr] = (byte)((frame8bit1[fr] << 3) | (frame8bit1[frameSize + fr] << 2) | (frame8bit1[frameSize * 2 + fr] << 1) | frame8bit1[frameSize * 3 + fr]);
                }
                framesList[i] = frame8bit4;
                byte[] frame4bit = ImageUtils.ConvertFrom8Bit(frame8bit4, iconWidth, iconHeight, 4, true, ref stride);
                Bitmap frameImage = ImageUtils.BuildImage(frame4bit, iconWidth, iconHeight, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetFileClass(this.FrameInputFileClass);
                frame.SetNeedsPalette(true);
                this.m_FramesList[i] = frame;
            }
            //this.fullWidth = iconWidth;
            //this.fullHeight = iconHeight * frames;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            const int framePixSize = iconWidth * iconHeight;
            int stride;
            SupportedFileType[] frames;
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null || fileToSave.Frames.Length == 0)
            {
                if (fileToSave.GetBitmap() == null)
                    throw new FileTypeSaveException(ERR_FRAMES_EMPTY);
                frames = new SupportedFileType[] { fileToSave };
            }
            else
            {
                frames = fileToSave.Frames;
            }
            int nrOfFrames = frames.Length;
            byte[] imageDataFull8 = new byte[nrOfFrames * framePixSize];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap frameImage = frame.GetBitmap();
                // TODO allow this to support 8bpp input
                if (frameImage.PixelFormat != PixelFormat.Format4bppIndexed)
                    throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 4);
                if (frameImage.Width != iconWidth || frameImage.Height != iconHeight)
                    throw new FileTypeSaveException(ERR_DIMENSIONS_INPUT, iconWidth, iconHeight);
                byte[] imageData4 = ImageUtils.GetImageData(frameImage, out stride);
                byte[] imageData8 = ImageUtils.ConvertTo8Bit(imageData4, iconWidth, iconHeight, 0, 4, true, ref stride);
                Array.Copy(imageData8, 0, imageDataFull8, framePixSize * i, framePixSize);
            }

            // Create the 1-bit array with each frame split into four planes.
            byte[] fileData8 = new byte[imageDataFull8.Length * 4];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                int frameAddr = framePixSize * i;
                int frameAddr4 = frameAddr * 4;
                for (int j = 0; j < framePixSize; ++j)
                {
                    byte fourbitpixel = imageDataFull8[frameAddr + j];
                    fileData8[frameAddr4 /* + framePixSize * 0 */ + j] = (byte)((fourbitpixel >> 3) & 1);
                    fileData8[frameAddr4 + framePixSize /* * 1 */ + j] = (byte)((fourbitpixel >> 2) & 1);
                    fileData8[frameAddr4 + framePixSize * 2 + j] = (byte)((fourbitpixel >> 1) & 1);
                    fileData8[frameAddr4 + framePixSize * 3 + j] = (byte)((fourbitpixel /* >> 0 */) & 1);
                }
            }
            stride = iconWidth;
            // Convert the array as if it is one long 1-bit image.
            return ImageUtils.ConvertFrom8Bit(fileData8, iconWidth, nrOfFrames * iconHeight * 4, 1, true, ref stride);
        }
    }
}