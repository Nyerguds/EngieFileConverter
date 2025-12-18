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
        protected const Int32 iconWidth = 24;
        protected const Int32 iconHeight = 24;

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }

        public override Int32 Width { get { return fullWidth; } }
        public override Int32 Height { get { return fullHeight; } }
        protected Int32 fullWidth = iconWidth;
        protected Int32 fullHeight = iconHeight;

        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        public override String IdCode { get { return "AdvIco"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "AdvSoft Icons"; } }
        public override String[] FileExtensions { get { return new String[] { "dat" }; } }
        public override String LongTypeName { get { return "AdventureSoft icons file"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 4; } }

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

        public void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            Int32 iconDataStride = iconWidth / 8;
            Int32 iconDataHeight = iconHeight * 4;
            Int32 iconDataSize = iconDataStride * iconDataHeight;

            if (fileData.Length < iconDataSize)
                throw new FileTypeLoadException("Not long enough.");
            if (fileData.Length % iconDataSize != 0)
                throw new FileTypeLoadException("Not a multiple of 4-bit " + iconWidth + "×" + iconHeight + " tiles.");
            Int32 frames = fileData.Length / iconDataSize;
            Byte[][] framesList = new Byte[frames][];
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            Int32 frameSize = iconWidth * iconHeight;
            for (Int32 i = 0; i < frames; ++i)
            {
                // Should probably convert this to use the planattolinear function...
                Int32 offset = i * iconDataSize;
                Byte[] frameData = new Byte[iconDataSize];
                Array.Copy(fileData, offset, frameData, 0, iconDataSize);
                Int32 stride = iconDataStride;
                Byte[] frame8bit1 = ImageUtils.ConvertTo8Bit(frameData, iconWidth, iconDataHeight, 0, 1, true, ref stride);
                Byte[] frame8bit4 = new Byte[frameSize];
                // Go over all pixels of the image, and combine them per 4.
                for (Int32 fr = 0; fr < frameSize; ++fr)
                {
                    frame8bit4[fr] = (Byte)((frame8bit1[fr] << 3) | (frame8bit1[frameSize + fr] << 2) | (frame8bit1[frameSize * 2 + fr] << 1) | frame8bit1[frameSize * 3 + fr]);
                }
                framesList[i] = frame8bit4;
                Byte[] frame4bit = ImageUtils.ConvertFrom8Bit(frame8bit4, iconWidth, iconHeight, 4, true, ref stride);
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            const Int32 framePixSize = iconWidth * iconHeight;
            Int32 stride;
            SupportedFileType[] frames;
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null || fileToSave.Frames.Length == 0)
            {
                if (fileToSave.GetBitmap() == null)
                    throw new ArgumentException("Image is empty.", "fileToSave");
                frames = new SupportedFileType[] { fileToSave };
            }
            else
            {
                frames = fileToSave.Frames;
            }
            Int32 nrOfFrames = frames.Length;
            Byte[] imageDataFull8 = new Byte[nrOfFrames * framePixSize];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap frameImage = frame.GetBitmap();
                if (frameImage.PixelFormat != PixelFormat.Format4bppIndexed)
                    throw new ArgumentException("AdventureSoft icons require 4 bits per pixel input.", "fileToSave");
                if (frameImage.Width != iconWidth || frameImage.Height != iconHeight)
                    throw new ArgumentException("AdventureSoft icons format needs " + iconWidth + "×" + iconHeight + " pixel frames.", "fileToSave");
                Byte[] imageData4 = ImageUtils.GetImageData(frameImage, out stride);
                Byte[] imageData8 = ImageUtils.ConvertTo8Bit(imageData4, iconWidth, iconHeight, 0, 4, true, ref stride);
                Array.Copy(imageData8, 0, imageDataFull8, framePixSize * i, framePixSize);
            }

            // Create the 1-bit array with each frame split into four planes.
            Byte[] fileData8 = new Byte[imageDataFull8.Length * 4];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Int32 frameAddr = framePixSize * i;
                Int32 frameAddr4 = frameAddr * 4;
                for (Int32 j = 0; j < framePixSize; ++j)
                {
                    Byte fourbitpixel = imageDataFull8[frameAddr + j];
                    fileData8[frameAddr4 /* + framePixSize * 0 */ + j] = (Byte)((fourbitpixel >> 3) & 1);
                    fileData8[frameAddr4 + framePixSize /* * 1 */ + j] = (Byte)((fourbitpixel >> 2) & 1);
                    fileData8[frameAddr4 + framePixSize * 2 + j] = (Byte)((fourbitpixel >> 1) & 1);
                    fileData8[frameAddr4 + framePixSize * 3 + j] = (Byte)((fourbitpixel /* >> 0 */) & 1);
                }
            }
            stride = iconWidth;
            // Convert the array as if it is one long 1-bit image.
            return ImageUtils.ConvertFrom8Bit(fileData8, iconWidth, nrOfFrames * iconHeight * 4, 1, true, ref stride);
        }
    }
}