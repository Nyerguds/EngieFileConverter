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

    public class FileAdvIcons : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }

        public override Int32 Width { get { return 48; } }
        public override Int32 Height { get { return 48; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "AdvSoft Icons"; } }
        public override String[] FileExtensions { get { return new String[] { "dat" }; } }
        public override String ShortTypeDescription { get { return "AdventureSoft icons file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 4; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }

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

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < 0x120)
                throw new FileTypeLoadException("Not long enough.");
            if (fileData.Length % 0x120 != 0)
                throw new FileTypeLoadException("Not a multiple of 1-bit 24x24 tiles.");
            Int32 frames = fileData.Length / 0x120;
            Byte[][] framesList = new Byte[frames][];
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 offset = i * 0x120;
                Byte[] frameData = new Byte[0x120];
                Array.Copy(fileData, offset, frameData, 0, 0x120);
                Int32 stride = 3;
                Byte[] frame8bit1 = ImageUtils.ConvertTo8Bit(frameData, 24, 24*4, 0, 1, true, ref stride);
                Int32 frameSize = 24 * 24;
                Byte[] frame8bit4 = new Byte[frameSize];
                // Go over all pixels of the image, and combine them per 4.
                for (Int32 fr = 0; fr < frameSize; fr++)
                {
                    frame8bit4[fr] = (Byte)((frame8bit1[fr] << 3) + (frame8bit1[frameSize + fr] << 2) + (frame8bit1[frameSize * 2 + fr] << 1) + frame8bit1[frameSize * 3 + fr]);
                }
                framesList[i] = frame8bit4;
                Byte[] frame4bit = ImageUtils.ConvertFrom8Bit(frame8bit4, 24, 24, 4, true, ref stride);
                Bitmap frameImage = ImageUtils.BuildImage(frame4bit, 24, 24, stride, PixelFormat.Format4bppIndexed, m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this.ShortTypeName, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerColor);
                frame.SetColorsInPalette(0);
                this.m_FramesList[i] = frame;
            }
            m_LoadedImage = TileImages(framesList, frames, this.m_Palette);
        }

        public static Bitmap TileImages(Byte[][] tiles, Int32 nrOftiles, Color[] palette)
        {
            Int32 fullImageHeight = nrOftiles * 24;
            Int32 stride = 24;
            Byte[] fullImageData8 = ImageUtils.Tile8BitData(tiles, 24, 24, stride, nrOftiles, palette, 1);
            Byte[] fullImageData4 = ImageUtils.ConvertFrom8Bit(fullImageData8, 24, fullImageHeight, 4, true, ref stride);
            return ImageUtils.BuildImage(fullImageData4, 24, fullImageHeight, stride, PixelFormat.Format4bppIndexed, palette, Color.Empty);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            const Int32 framePixSize = 24 * 24;
            Int32 frames;
            Byte[] imageDataFull8;
            Int32 stride;
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null || fileToSave.Frames.Length == 0)
            {
                Bitmap image = fileToSave.GetBitmap();
                if (image == null)
                    throw new NotSupportedException("Image is empty.");
                Int32 inputFullHeight = image.Height;
                if (image.Width != 24 || inputFullHeight % 24 != 0)
                    throw new NotSupportedException("AdventureSoft icons format saved from a single image requires vertically stacked 24x24 pixel frames.");
                if (image.PixelFormat != PixelFormat.Format4bppIndexed)
                    throw new NotSupportedException("AdventureSoft icons require 4 bits per pixel input!");
                frames = inputFullHeight / 24;
                Byte[] imageData4 = ImageUtils.GetImageData(image, out stride);
                imageDataFull8 = ImageUtils.ConvertTo8Bit(imageData4, 24, inputFullHeight, 0, 4, true, ref stride);
            }
            else
            {
                frames = fileToSave.Frames.Length;
                imageDataFull8 = new Byte[frames * framePixSize];
                for (int i = 0; i < fileToSave.Frames.Length; i++)
                {
                    SupportedFileType frame = fileToSave.Frames[i];
                    Bitmap frameImage = frame.GetBitmap();
                    if (frameImage.Width != 24 || frameImage.Height != 24)
                        throw new NotSupportedException("AdventureSoft icons format saved from a single image requires vertically stacked 24x24 pixel frames.");
                    if (frameImage.PixelFormat != PixelFormat.Format4bppIndexed)
                        throw new NotSupportedException("AdventureSoft icons require 4 bits per pixel input!");
                    Byte[] imageData4 = ImageUtils.GetImageData(frameImage, out stride);
                    Byte[] imageData8 = ImageUtils.ConvertTo8Bit(imageData4, 24, 24, 0, 4, true, ref stride);
                    Array.Copy(imageData8, 0, imageDataFull8, framePixSize * i, framePixSize);
                }
            }
            Byte[] fileData8 = new Byte[imageDataFull8.Length * 4];
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 frameAddr = framePixSize * i;
                Int32 frameAddr4 = frameAddr * 4;
                for (Int32 j = 0; j < framePixSize; j++)
                {
                    Byte fourbitpixel = imageDataFull8[frameAddr + j];
                    fileData8[frameAddr4 + j] = (Byte)((fourbitpixel >> 3) & 1);
                    fileData8[frameAddr4 + framePixSize + j] = (Byte)((fourbitpixel >> 2) & 1);
                    fileData8[frameAddr4 + framePixSize * 2 + j] = (Byte)((fourbitpixel >> 1) & 1);
                    fileData8[frameAddr4 + framePixSize * 3 + j] = (Byte)((fourbitpixel) & 1);
                }
            }
            stride = 24;
            return ImageUtils.ConvertFrom8Bit(fileData8, 24, frames * 24 * 4, 1, true, ref stride);
        }
    }
}