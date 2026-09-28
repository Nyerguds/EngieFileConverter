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
        protected const int DATAOFFSET = 11;

        public override string IdCode { get { return "WwLcw"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood LCW IMG"; } }
        public override string[] FileExtensions { get { return new string[] { "img" }; } }
        public override string LongTypeName { get { return "Blade Runner LCW image"; } }
        public override int BitsPerPixel { get{ return 16; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public override bool ColorsChanged()
        {
            return false;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            return this.SaveImg(fileToSave.GetBitmap());
        }

        protected void LoadFromFileData(byte[] fileData)
        {
            if (fileData.Length < DATAOFFSET)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);

            byte[] hdrId = new byte[3];
            Array.Copy(fileData, hdrId, 3);
            int hdrWidth = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 3);
            int hdrHeight = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 7);
            if (!Encoding.ASCII.GetBytes("LCW").SequenceEqual(hdrId))
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            int stride = ImageUtils.GetMinimumStride(hdrWidth, this.BitsPerPixel);
            int imageDataSize = stride * hdrHeight;
            byte[] imageData = new byte[imageDataSize];
            try
            {
                int offset = DATAOFFSET;
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

        protected byte[] SaveImg(Bitmap image)
        {
            byte[] imageData = ImageUtils.GetImageData(image, PixelFormat.Format16bppRgb555, true);
            byte[] compressedData = WWCompression.LcwCompress(imageData);
            byte[] fullData = new byte[compressedData.Length + DATAOFFSET];
            fullData[0] = (byte)'L';
            fullData[1] = (byte)'C';
            fullData[2] = (byte)'W';
            ArrayUtils.WriteInt32ToByteArrayLe(fullData, 3, image.Width);
            ArrayUtils.WriteInt32ToByteArrayLe(fullData, 7, image.Height);
            compressedData.CopyTo(fullData, DATAOFFSET);
            return fullData;
        }
    }
}