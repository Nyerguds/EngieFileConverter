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

    public class FileElvIcons : SupportedFileType
    {
        public override Int32 Width { get { return 48; } }
        public override Int32 Height { get { return 48; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;
        protected SupportedFileType[] m_Frames = new SupportedFileType[0];

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "ElvIco"; } }
        public override String[] FileExtensions { get { return new String[] { "DAT" }; } }
        public override String ShortTypeDescription { get { return "Elvira ICON.DAT file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 1; } }
        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return true; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_Frames.ToArray(); } }

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

        public override Boolean ColorsChanged()
        {
            return false;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            if (!fileToSave.ContainsFrames)
            {
                Bitmap image = fileToSave.GetBitmap();
                if (image.Width != 24 || image.Height % 24 != 0)
                    throw new NotSupportedException("Elvira icons format saved from a single image requires vertically stacked 24x24 pixel frames.");
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
                    throw new NotSupportedException("Elvira icons format requires 24x24 pixel frames.");
                Bitmap hiColImage = ImageUtils.PaintOn32bpp(frame.GetBitmap(), Color.Black);
                Int32 stride;
                Byte[] imageData32 = ImageUtils.GetImageData(hiColImage, out stride);
                Byte[] frameData = ImageUtils.Convert32bToGray(imageData32, 24, 24, 1, true, ref stride);
                Array.Copy(frameData, 0, imageData, i * 0x48, 0x48);
            }
            return imageData;
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 0x48)
                throw new FileTypeLoadException("Not long enough.");
            if (fileData.Length % 0x48 != 0)
                throw new FileTypeLoadException("Not a multiple of 1-bit 24x24 tiles.");
            Int32 frames = fileData.Length / 0x48;
            this.m_Frames = new SupportedFileType[frames];
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 offset = i * 0x48;
                Byte[] frameData = new Byte[0x48];
                Array.Copy(fileData, offset, frameData, 0, 0x48);
                Bitmap frameImage = ImageUtils.BuildImage(frameData, 24, 24, 3, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFile(frameImage, 2, "frame_" + i.ToString("D5"));
                frame.FrameParent = this;
                this.m_Frames[i] = frame;
            }
            m_LoadedImage = ImageUtils.BuildImage(fileData, 24, fileData.Length / 3, 3, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);
        }
    }
}