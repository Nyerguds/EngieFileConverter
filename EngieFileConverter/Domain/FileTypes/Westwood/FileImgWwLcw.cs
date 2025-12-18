using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgWwLcw : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.ImageHiCol; } }
        public override FileClass InputFileClass { get { return FileClass.Image; } }
        protected const Int32 DATAOFFSET = 11;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood LCW IMG"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String ShortTypeDescription { get { return "Westwood LCW image (Blade Runner)"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerPixel { get{ return 16; } }
        
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

            Byte[] hdrId = new Byte[3];
            Array.Copy(fileData, hdrId, 3);
            Int32 hdrWidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 3, 4, true);
            Int32 hdrHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 7, 4, true);
            if (!Encoding.ASCII.GetBytes("LCW").SequenceEqual(hdrId))
                throw new FileTypeLoadException("File does not start with signature \"LCW\".");
            Int32 stride = ImageUtils.GetMinimumStride(hdrWidth, this.BitsPerPixel);
            Int32 imageDataSize = stride * hdrHeight;
            Byte[] imageData = new Byte[imageDataSize];
            try
            {
                Int32 offset = DATAOFFSET;
                WWCompression.LcwDecompress(fileData, ref offset, imageData, 0);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error decompressing image data: " + e.Message);
            }
            try
            {
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, hdrWidth, hdrHeight, stride, PixelFormat.Format16bppRgb555, null, Color.Black);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        protected Byte[] SaveImg(Bitmap image)
        {
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride, PixelFormat.Format16bppRgb555, true);
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
    }
}