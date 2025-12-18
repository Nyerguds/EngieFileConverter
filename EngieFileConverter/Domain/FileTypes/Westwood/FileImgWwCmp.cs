using Nyerguds.FileData.Compression;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    internal class FileImgWwCmp : SupportedFileType
    {
        public override FileClass FileClass { get { return m_IsEightBit ? FileClass.Image8Bit : FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected readonly Int32 m_Width = 320;
        protected readonly Int32 m_Height = 200;

        public override String IdCode { get { return "WwCmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood CMP"; } }
        public override String[] FileExtensions { get { return new String[] { "cmp" }; } }
        public override String LongTypeName { get { return "Westwood CMP File"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return m_IsEightBit ? 8 : 4; } }
        protected Boolean m_IsEightBit = false;

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String filename)
        {
            bool isVertical;
            bool eightBit;
            Byte[] imageData = GetImageData(fileData, 0, out isVertical, out eightBit);
            this.SetFileNames(filename);
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
            try
            {
                int stride = this.m_Width / 2;
                if (isVertical)
                {
                    Byte[] outBuffer2 = new Byte[imageData.Length];
                    // Post-processing: Exchange rows and columns.
                    for (Int32 i = 0; i < imageData.Length; ++i)
                        outBuffer2[i % m_Height * stride + i / m_Height] = imageData[i];
                    imageData = outBuffer2;
                    this.ExtraInfo = "Saved vertically.";
                }
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, stride, PixelFormat.Format4bppIndexed, this.m_Palette, Color.Black);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FileTypeLoadException("Cannot construct image from read data.", e);
            }
        }

        /// <summary>
        /// Retrieves the image data and sets the file properties and palette.
        /// </summary>
        /// <param name="fileData">Original file data.</param>
        /// <param name="start">Start offset of the data.</param>
        /// <param name="isVertical">Output arg for returning whether the file was indicated as compressed vertically.</param>
        /// <returns>The raw 4-bit image data in a 32000 byte array.</returns>
        protected static Byte[] GetImageData(Byte[] fileData, Int32 start, out bool isVertical, out bool eightBit)
        {
            Int32 dataLen = fileData.Length - start;
            if (dataLen < 3)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            Int32 fileSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, start, 2, true);
            int imageType = fileData[2];
            eightBit = imageType > 2;
            // Not sure what's up with that; image type 3 has a byte more.
            if (fileSize + (imageType == 3 ? 3 : 2) != dataLen)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);
            isVertical = imageType == 2 || imageType == 4;
            int bufferSize = 32000;
            if (eightBit)
                bufferSize *= 2;
            Int32 dataOffset = start + 3;
            Byte[] imageData;
            int len;
            try
            {
                switch (imageType)
                {
                    case 1:
                    case 2:
                        imageData = WestwoodRle.RleDecode(fileData, (UInt32)dataOffset, null, bufferSize, true, true);
                        len = imageData.Length;
                        break;
                    case 4:
                        imageData = new Byte[bufferSize];
                        len = WWCompression.LcwDecompress(fileData, ref dataOffset, imageData, 0);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported format \"" + imageType + "\".");
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error decompressing image data: " + e.Message, e);
            }
            if (imageData == null)
                throw new FileTypeLoadException("Error decompressing image data.");
            return imageData;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return new Option[]
            {
                new Option("VRT", OptionInputType.Boolean, "Optimize size (compress vertically if smaller)", "1")
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            bool trySaveVertical = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "VRT"));
            Bitmap image = fileToSave.GetBitmap();
            if (image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new ArgumentException(ErrFixedBppAndSize, "fileToSave");
            int stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride, true);
            Byte[] compressedData = WestwoodRle.RleEncode(imageData, true);
            bool saveVertical = false;
            if (trySaveVertical)
            {
                // Pre-processing: Exchange rows and columns.
                int height = image.Height;
                int imgLength = imageData.Length;
                Byte[] imageData2 = new Byte[imgLength];
                for (Int32 i = 0; i < imgLength; ++i)
                    imageData2[i] = imageData[(i % height) * stride + (i / height)];
                Byte[] compressedData2 = WestwoodRle.RleEncode(imageData2, true);
                if (compressedData2.Length < compressedData.Length)
                {
                    saveVertical = true;
                    compressedData = compressedData2;
                }
            }
            Byte[] data = new Byte[compressedData.Length + 3];
            ArrayUtils.WriteUInt16ToByteArrayLe(data, 0, (UInt16)(compressedData.Length + 1));
            data[2] = (Byte)(saveVertical ? 2 : 1);
            Array.Copy(compressedData, 0, data, 3, compressedData.Length);
            return data;
        }
    }
}
