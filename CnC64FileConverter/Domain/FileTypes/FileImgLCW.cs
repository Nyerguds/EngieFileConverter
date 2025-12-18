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

    public class FileImgLcw : SupportedFileType
    {
        private static PixelFormatter SixteenBppFormatter = new PixelFormatter(2, 5, 10, 5, 5, 5, 0, 0, 0, true);
        public override Int32 Width { get { return hdrWidth; } }
        public override Int32 Height { get { return hdrHeight; } }
        protected const Int32 DATAOFFSET = 11;
        protected Byte[] hdrId;
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "BRImg"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String ShortTypeDescription { get { return "LCW Image file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get{ return 16; } }

        public FileImgLcw() { }
        
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave)
        {
            return SaveImg(fileToSave.GetBitmap());
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
            if (!this.hdrId.SequenceEqual(Encoding.ASCII.GetBytes("LCW")))
                throw new FileTypeLoadException("File does not start with signature \"LCW\".");
            
            // WARNING! The hi-colour format is 16 BPP, but the image data is converted to 32 bpp for creating the actual image!
            Int32 stride = ImageUtils.GetMinimumStride(this.Width, this.BitsPerColor);
            Int32 imageDataSize = ImageUtils.GetMinimumStride(this.Width, this.BitsPerColor) * this.Height;
            Byte[] imageData = new Byte[imageDataSize];
            try
            {
                Int32 offset = DATAOFFSET;
                CHRONOLIB.Compression.WWCompression.LcwUncompress(fileData, ref offset, imageData);
                imageData = Convert16bTo32b(imageData, 0, this.Width, this.hdrHeight, ref stride);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message);
            }
            try
            {
                //Int32 stride = ImageUtils.GetMinimumStride(this.Width, Image.GetPixelFormatSize(pf));
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, stride, PixelFormat.Format32bppArgb, null, Color.Black);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        protected Byte[] SaveImg(Bitmap image)
        {
            if (image.PixelFormat != PixelFormat.Format32bppArgb)
                image = ImageUtils.PaintOn32bpp(image, Color.Black);
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            imageData = Convert32bTo16b(imageData, image.Width, image.Height,ref stride);
            Byte[] compressedData = CHRONOLIB.Compression.WWCompression.LcwCompress(imageData);
            Byte[] fullData = new Byte[compressedData.Length + DATAOFFSET];
            fullData[0] = (Byte)'L';
            fullData[1] = (Byte)'C';
            fullData[2] = (Byte)'W';
            ArrayUtils.WriteIntToByteArray(fullData, 3, 4, true, (UInt32)image.Width);
            ArrayUtils.WriteIntToByteArray(fullData, 7, 4, true, (UInt32)image.Height);
            Array.Copy(compressedData, 0, fullData, DATAOFFSET, compressedData.Length);
            return fullData;
        }

        protected static Byte[] Convert16bTo32b(Byte[] imageData, Int32 startOffset, Int32 width, Int32 height, ref Int32 stride)
        {
            Int32 newImageStride = width * 4;
            Byte[] newImageData = new Byte[height * newImageStride];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 sourceOffset = y * stride + x * 2;
                    Int32 targetOffset = y * newImageStride + x * 4;
                    Color c = SixteenBppFormatter.GetColor(imageData, startOffset + sourceOffset);
                    PixelFormatter.Format32BitArgb.WriteColor(newImageData, targetOffset, c);
                }
            }
            stride = newImageStride;
            return newImageData;
        }

        protected static Byte[] Convert32bTo16b(Byte[] imageData, Int32 width, Int32 height, ref Int32 stride)
        {
            Int32 newStride = width * 2;
            Byte[] newImageData = new Byte[newStride * height];

            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x += 1)
                {
                    Int32 inputOffs = y * stride + x*4;
                    Int32 outputOffs = y * newStride + x*2;
                    Color c = PixelFormatter.Format32BitArgb.GetColor(imageData, inputOffs);
                    SixteenBppFormatter.WriteColor(newImageData, outputOffs, c);
                }
            }
            stride = newStride;
            return newImageData;
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