using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileImgWwLcw : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.ImageHiCol; } }
        public override FileClass InputFileClass { get { return FileClass.Image; } }
        public override Int32 Width { get { return this.hdrWidth; } }
        public override Int32 Height { get { return this.hdrHeight; } }
        protected const Int32 DATAOFFSET = 11;
        protected Byte[] hdrId;
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood LCW IMG"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String ShortTypeDescription { get { return "Westwood LCW image (Blade Runner)"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerPixel { get{ return 16; } }

        public FileImgWwLcw() { }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            return this.SaveImg(fileToSave.GetBitmap());
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < DATAOFFSET)
                throw new FileTypeLoadException("File is not long enough to be a valid Blade Runner IMG file.");
            try
            {
                this.ReadHeader(fileData);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading header data: " + e.Message);
            }
            if (!Encoding.ASCII.GetBytes("LCW").SequenceEqual(this.hdrId))
                throw new FileTypeLoadException("File does not start with signature \"LCW\".");
            // WARNING! The hi-colour format is 16 BPP, but the image data is converted to 32 bpp for creating the actual image!
            Int32 stride = ImageUtils.GetMinimumStride(this.Width, this.BitsPerPixel);
            Int32 imageDataSize = ImageUtils.GetMinimumStride(this.Width, this.BitsPerPixel) * this.Height;
            Byte[] imageData = new Byte[imageDataSize];
            try
            {
                Int32 offset = DATAOFFSET;
                WWCompression.LcwDecompress(fileData, ref offset, imageData, 0);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message);
            }
            try
            {
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, stride, PixelFormat.Format16bppRgb555, null, Color.Black);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        protected Byte[] SaveImg(Bitmap image)
        {
            Byte[] imageData;
            //using (Bitmap hiColImage = ImageUtils.PaintOn32bpp(image, Color.Black))
            {
                Int32 stride;
                imageData = ImageUtils.GetImageData(image, out stride, PixelFormat.Format16bppRgb555, true);
                imageData = ImageUtils.CollapseStride(imageData, image.Width, image.Height, 16, ref stride, true);
            }
            Byte[] compressedData = WWCompression.LcwCompress(imageData);
            Byte[] fullData = new Byte[compressedData.Length + DATAOFFSET];
            fullData[0] = (Byte)'L';
            fullData[1] = (Byte)'C';
            fullData[2] = (Byte)'W';
            ArrayUtils.WriteIntToByteArray(fullData, 3, 4, true, (UInt32)image.Width);
            ArrayUtils.WriteIntToByteArray(fullData, 7, 4, true, (UInt32)image.Height);
            compressedData.CopyTo(fullData, DATAOFFSET);
            return fullData;
        }

        protected void ReadHeader(Byte[] headerBytes)
        {
            this.hdrId = new Byte[3];
            Array.Copy(headerBytes, this.hdrId, 3);
            this.hdrWidth = (Int32)ArrayUtils.ReadIntFromByteArray(headerBytes, 3, 4, true);
            this.hdrHeight = (Int32)ArrayUtils.ReadIntFromByteArray(headerBytes, 7, 4, true);
        }
    }
}