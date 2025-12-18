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

        public override String IdCode { get { return "WwLcw"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood LCW IMG"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String LongTypeName { get { return "Blade Runner LCW image"; } }
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            return this.SaveImg(fileToSave.GetBitmap());
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < DATAOFFSET)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);

            Byte[] hdrId = new Byte[3];
            Array.Copy(fileData, hdrId, 3);
            Int32 hdrWidth = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 3);
            Int32 hdrHeight = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 7);
            if (!Encoding.ASCII.GetBytes("LCW").SequenceEqual(hdrId))
                throw new FileTypeLoadException(ERR_BAD_HEADER);
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
                throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, e.Message), e);
            }
            try
            {
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, hdrWidth, hdrHeight, stride, PixelFormat.Format16bppRgb555, null, null);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FileTypeLoadException(String.Format(ERR_MAKING_IMG_ERR, e.Message), e);
            }
        }

        protected Byte[] SaveImg(Bitmap image)
        {
            Byte[] imageData = ImageUtils.GetImageData(image, PixelFormat.Format16bppRgb555, true);
            Byte[] compressedData = WWCompression.LcwCompress(imageData);
            Byte[] fullData = new Byte[compressedData.Length + DATAOFFSET];
            fullData[0] = (Byte)'L';
            fullData[1] = (Byte)'C';
            fullData[2] = (Byte)'W';
            ArrayUtils.WriteInt32ToByteArrayLe(fullData, 3, image.Width);
            ArrayUtils.WriteInt32ToByteArrayLe(fullData, 7, image.Height);
            compressedData.CopyTo(fullData, DATAOFFSET);
            return fullData;
        }
    }
}