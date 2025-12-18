using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileFramesAdvIco : SupportedFileType
    {
        protected const Int32 iconWidth = 24;
        protected const Int32 iconHeight = 24;

        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }

        public override Int32 Width { get { return fullWidth; } }
        public override Int32 Height { get { return fullHeight; } }
        protected Int32 fullWidth = iconWidth;
        protected Int32 fullHeight = iconHeight;
        
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "AdvSoft Icons"; } }
        public override String[] FileExtensions { get { return new String[] { "dat" }; } }
        public override String ShortTypeDescription { get { return "AdventureSoft icons file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerPixel { get { return 4; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }

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
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 offset = i * iconDataSize;
                Byte[] frameData = new Byte[iconDataSize];
                Array.Copy(fileData, offset, frameData, 0, iconDataSize);
                Int32 stride = iconDataStride;
                Byte[] frame8bit1 = ImageUtils.ConvertTo8Bit(frameData, iconWidth, iconDataHeight, 0, 1, true, ref stride);
                Byte[] frame8bit4 = new Byte[frameSize];
                // Go over all pixels of the image, and combine them per 4.
                for (Int32 fr = 0; fr < frameSize; fr++)
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
                frame.SetColorsInPalette(0);
                this.m_FramesList[i] = frame;
            }
            this.fullWidth = iconWidth;
            this.fullHeight = iconHeight * frames;
            this.m_LoadedImage = TileImages(framesList, frames, this.m_Palette);
        }

        /// <summary>
        /// Make full images. This uses the in-between 8-bit data as source, so it can use the Tile8BitData function.
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="nrOftiles"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        public static Bitmap TileImages(Byte[][] tiles, Int32 nrOftiles, Color[] palette)
        {
            Int32 fullImageHeight = nrOftiles * iconHeight;
            Int32 stride = iconWidth;
            Byte[] fullImageData8 = ImageUtils.Tile8BitData(tiles, iconWidth, iconHeight, stride, nrOftiles, palette, 1);
            Byte[] fullImageData4 = ImageUtils.ConvertFrom8Bit(fullImageData8, iconWidth, fullImageHeight, 4, true, ref stride);
            return ImageUtils.BuildImage(fullImageData4, iconWidth, fullImageHeight, stride, PixelFormat.Format4bppIndexed, palette, Color.Empty);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            const Int32 framePixSize = iconWidth * iconHeight;
            Int32 frames;
            Byte[] imageDataFull8;
            Int32 stride;
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null || fileToSave.Frames.Length == 0)
            {
                Bitmap image = fileToSave.GetBitmap();
                if (image == null)
                    throw new NotSupportedException("Image is empty.");
                Int32 inputFullHeight = image.Height;
                if (image.PixelFormat != PixelFormat.Format4bppIndexed)
                    throw new NotSupportedException("AdventureSoft icons require 4 bits per pixel input.");
                if (image.Width != iconWidth || inputFullHeight % iconHeight != 0)
                    throw new NotSupportedException("AdventureSoft icons format saved from a single image requires vertically stacked " + iconWidth + "×" + iconHeight + " pixel frames.");
                frames = inputFullHeight / iconHeight;
                Byte[] imageData4 = ImageUtils.GetImageData(image, out stride);
                imageDataFull8 = ImageUtils.ConvertTo8Bit(imageData4, iconWidth, inputFullHeight, 0, 4, true, ref stride);
            }
            else
            {
                frames = fileToSave.Frames.Length;
                imageDataFull8 = new Byte[frames * framePixSize];
                for (Int32 i = 0; i < fileToSave.Frames.Length; i++)
                {
                    SupportedFileType frame = fileToSave.Frames[i];
                    Bitmap frameImage = frame.GetBitmap();
                    if (frameImage.PixelFormat != PixelFormat.Format4bppIndexed)
                        throw new NotSupportedException("AdventureSoft icons require 4 bits per pixel input.");
                    if (frameImage.Width != iconWidth || frameImage.Height != iconHeight)
                        throw new NotSupportedException("AdventureSoft icons format needs " + iconWidth + "×" + iconHeight + " pixel frames.");
                    Byte[] imageData4 = ImageUtils.GetImageData(frameImage, out stride);
                    Byte[] imageData8 = ImageUtils.ConvertTo8Bit(imageData4, iconWidth, iconHeight, 0, 4, true, ref stride);
                    Array.Copy(imageData8, 0, imageDataFull8, framePixSize * i, framePixSize);
                }
            }
            // Create the 1-bit array with each frame split into four planes.
            Byte[] fileData8 = new Byte[imageDataFull8.Length * 4];
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 frameAddr = framePixSize * i;
                Int32 frameAddr4 = frameAddr * 4;
                for (Int32 j = 0; j < framePixSize; j++)
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
            return ImageUtils.ConvertFrom8Bit(fileData8, iconWidth, frames * iconHeight * 4, 1, true, ref stride);
        }
    }
}