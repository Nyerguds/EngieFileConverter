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
        public override Int32 Width { get { return 48; } }
        public override Int32 Height { get { return 48; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "AdvSoft Icons"; } }
        public override String[] FileExtensions { get { return new String[] { "DAT" }; } }
        public override String ShortTypeDescription { get { return "AdventureSoft icons file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 1; } }

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
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave.Frames == null)
            {
                Bitmap image = fileToSave.GetBitmap();
                if (image.Width != 24 || image.Height % 24 != 0)
                    throw new NotSupportedException("AdventureSoft icons format saved from a single image requires vertically stacked 24x24 pixel frames.");
                Bitmap hiColImage = ImageUtils.PaintOn32bpp(image, Color.Black);
                Int32 stride;
                Byte[] imageData32 = ImageUtils.GetImageData(hiColImage, out stride);
                return ImageUtils.Convert32bToGray(imageData32, image.Width, image.Height, 1, true, ref stride);
            }
            Byte[] imageData = new Byte[3 * fileToSave.Frames.Length * 24];
            for (Int32 i = 0; i < fileToSave.Frames.Length; i++)
            {
                SupportedFileType frame = fileToSave.Frames[i];
                if (frame.Width != 24 || frame.Height != 24)
                    throw new NotSupportedException("AdventureSoft icons format requires 24x24 pixel frames.");
                Bitmap hiColImage = ImageUtils.PaintOn32bpp(frame.GetBitmap(), Color.Black);
                Int32 stride;
                Byte[] imageData32 = ImageUtils.GetImageData(hiColImage, out stride);
                Byte[] frameData = ImageUtils.Convert32bToGray(imageData32, 24, 24, 1, true, ref stride);
                Array.Copy(frameData, 0, imageData, i * 0x48, 0x48);
            }
            return imageData;
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < 0x48)
                throw new FileTypeLoadException("Not long enough.");
            if (fileData.Length % 0x48 != 0)
                throw new FileTypeLoadException("Not a multiple of 1-bit 24x24 tiles.");
            Int32 frames = fileData.Length / 0x48;
            this.m_FramesList = new SupportedFileType[frames];
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 offset = i * 0x48;
                Byte[] frameData = new Byte[0x48];
                Array.Copy(fileData, offset, frameData, 0, 0x48);
                Bitmap frameImage = ImageUtils.BuildImage(frameData, 24, 24, 3, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this.ShortTypeName, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerColor);
                frame.SetColorsInPalette(0);
                this.m_FramesList[i] = frame;
            }
            m_LoadedImage = ImageUtils.BuildImage(fileData, 24, fileData.Length / 3, 3, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);
        }
    }
}