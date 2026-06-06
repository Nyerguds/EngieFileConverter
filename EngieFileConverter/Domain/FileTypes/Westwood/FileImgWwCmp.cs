using Nyerguds.FileData.Compression;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    internal class FileImgWwCmp : SupportedFileType
    {
        public override FileClass FileClass { get { return m_IsEightBit ? FileClass.Image8Bit : FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override int Width { get { return m_Width; } }
        public override int Height { get { return m_Height; } }
        protected static int m_Width = 320;
        protected static int m_Height = 200;
        public Int32 CompressionType { get; protected set; }

        public override string IdCode { get { return "WwCmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood CMP"; } }
        public override string[] FileExtensions { get { return new string[] { "cmp" }; } }
        public override string LongTypeName { get { return "Westwood CMP File"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return m_IsEightBit ? 8 : 4; } }
        protected bool m_IsEightBit = false;

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(byte[] fileData, string filename)
        {
            byte[] imageData = GetImageData(fileData, 0, fileData.Length, out int imageType, out m_IsEightBit);
            CompressionType = imageType;
            this.SetFileNames(filename);
            try
            {
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
                PixelFormat pf = m_IsEightBit ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed;
                int stride = ImageUtils.GetMinimumStride(m_Width, m_IsEightBit ? 8 : 4);
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, stride, pf, this.m_Palette, Color.Black);
                StringBuilder info = new StringBuilder()
                    .Append("Type ").Append(imageType).Append(": ")
                    .Append(m_IsEightBit ? 8 : 4).Append("-bit, ");
                switch (imageType)
                {
                    case 1:
                        info.Append("RLE, horizontal"); break;
                    case 2:
                        info.Append("RLE, vertical"); break;
                    case 3:
                        info.Append("LZW-12"); break;
                    case 4:
                        info.Append("RLE"); break;
                    case 6:
                        info.Append("LZW-14"); break;
                }
                ExtraInfo = info.ToString();
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
        /// <param name="dataLen">Length of the data</param>
        /// <param name="imageType">Output arg for returning the image type.</param>
        /// <param name="eightBit">Output arg for returning whether the file was eight bit.</param>
        /// <returns>The raw image data.</returns>
        protected static byte[] GetImageData(byte[] fileData, int start, int dataLen, out int imageType, out bool eightBit)
        {
            if (dataLen < 3)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            int fileSize = (int)ArrayUtils.ReadIntFromByteArray(fileData, start, 2, true);
            imageType = fileData[2];
            int bufferSize = 32000;
            eightBit = imageType > 2 || (imageType == 0 && fileSize == bufferSize * 2);
            int checkSize = fileSize + 2;
            if (imageType == 4 || imageType == 5)
                checkSize++;
            if (checkSize != dataLen)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);
            if (eightBit)
                bufferSize *= 2;
            int dataOffset = start + 3;
            int dataLength = dataLen - dataOffset;
            int endOffset = start + checkSize;
            byte[] imageData;
            bool flip = false;
            int len;
            try
            {
                switch (imageType)
                {
                    case 1: // CONFIRMED - 4-bit, RLE, horizontal
                    case 2: // CONFIRMED - 4-bit, RLE, vertical
                    case 4: // CONFIRMED - 8-bit, RLE, horizontal, with big-endian 16 bit repeats for some reason.
                        imageData = null;
                        // large repeat values are normally read as big-endian on PC systems (yes, it's weird),
                        // but apparently for the 4-bit formats they read them as little-endian.
                        len = WestwoodRle.RleDecode(fileData, (uint)dataOffset, (uint)endOffset, ref imageData, !eightBit, true);
                        if (len != bufferSize)
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, ERR_DECOMPR_LEN));
                        flip = imageType == 2;
                        break;
                    case 3: // CONFIRMED - 8-bit, LZW-12, with 2 more header bytes.
                    case 6: // CONFIRMED - 8-bit, LZW-14, with 2 more header bytes.
                        int expected = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 3);
                        if (expected != bufferSize)
                            throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                        dataOffset += 2;
                        imageData = new byte[bufferSize];
                        LzwCompression lzw1 = new LzwCompression(imageType == 3 ? LzwSize.Size12Bit : LzwSize.Size14Bit);
                        imageData = lzw1.Decompress(fileData, dataOffset, bufferSize);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported format \"" + imageType + "\".");
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, e.Message), e);
            }
            if (imageData == null)
                throw new FileTypeLoadException(ERR_DECOMPR);
            if (flip)
            {
                int stride = ImageUtils.GetMinimumStride(m_Width, eightBit ? 8 : 4);
                byte[] outBuffer2 = new byte[imageData.Length];
                // Post-processing: Exchange rows and columns.
                for (int i = 0; i < imageData.Length; ++i)
                    outBuffer2[i % m_Height * stride + i / m_Height] = imageData[i];
                imageData = outBuffer2;
            }
            return imageData;
        }

        private string CheckFileToSave(SupportedFileType fileToSave, out Bitmap image, out bool is4bpp)
        {
            is4bpp = false;
            image = null;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                return ERR_EMPTY_FILE;
            is4bpp = image.PixelFormat == PixelFormat.Format4bppIndexed;
            bool is8bpp = image.PixelFormat == PixelFormat.Format8bppIndexed;
            if (image.Width != 320 || image.Height != 200 || (!is4bpp && !is8bpp))
                return ErrFixedBppsAndSize(320, 200, ShortTypeName, 4, 8);
            return null;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            string fileErr = CheckFileToSave(fileToSave, out Bitmap image, out bool is4bpp);
            if (fileErr != null)
                throw new FileTypeSaveException(fileErr, "fileToSave");
            FileImgWwCmp cmp = fileToSave as FileImgWwCmp;
            if (is4bpp)
            {
                return new Option[]
                {
                    new Option("VRT", OptionInputType.Boolean, "Optimize size (compress vertically if smaller)", "1")
                };
            }
            else
            {
                int compression = cmp?.CompressionType ?? 4;
                switch (cmp?.CompressionType ?? 4)
                {
                    case 3:
                        compression = 1; break;
                    case 4:
                    case 5:
                        compression = 0; break;
                    case 6:
                        compression = 2; break;
                }
                return new Option[]
                {
                    new Option("VER", OptionInputType.ChoicesList, "Storage type", "RLE,LZW-12,LZW-14", compression.ToString()),
                };
            }
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            string fileErr = CheckFileToSave(fileToSave, out Bitmap image, out bool is4bpp);
            if (fileErr != null)
                throw new FileTypeSaveException(fileErr, "fileToSave");
            if (is4bpp)
                return SaveToBytes4bpp(image, saveOptions);
            else
                return SaveToBytes8bpp(image, saveOptions);
        }

        public byte[] SaveToBytes4bpp(Bitmap image, Option[] saveOptions)
        {
            bool trySaveVertical = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "VRT"));
            byte[] imageData = ImageUtils.GetImageData(image, out int stride, true);
            byte[] compressedData = WestwoodRle.RleEncode(imageData, true);
            byte imageType = 1;
            if (trySaveVertical)
            {
                // Pre-processing: Exchange rows and columns.
                int height = image.Height;
                int imgLength = imageData.Length;
                byte[] imageData2 = new byte[imgLength];
                for (int i = 0; i < imgLength; ++i)
                    imageData2[i] = imageData[(i % height) * stride + (i / height)];
                byte[] compressedData2 = WestwoodRle.RleEncode(imageData2, true);
                if (compressedData2.Length < compressedData.Length)
                {
                    imageType = 2;
                    compressedData = compressedData2;
                }
            }
            byte[] data = new byte[compressedData.Length + 3];
            ArrayUtils.WriteUInt16ToByteArrayLe(data, 0, (ushort)(compressedData.Length + 1));
            data[2] = imageType;
            Array.Copy(compressedData, 0, data, 3, compressedData.Length);
            return data;
        }

        public byte[] SaveToBytes8bpp(Bitmap image, Option[] saveOptions)
        {
            int.TryParse(Option.GetSaveOptionValue(saveOptions, "VER"), out int compression);
            byte[] imageData = ImageUtils.GetImageData(image, out int stride, true);
            byte[] compressedData;
            byte imageType;
            switch (compression)
            {
                case 0: // imageType = 4; break;
                    imageType = 4;
                    compressedData = WestwoodRle.RleEncode(imageData, false);
                    break;
                case 1:
                case 2:
                    imageType = (byte)(compression == 1 ? 3 : 6);
                    LzwCompression lzw1 = new LzwCompression(compression == 1 ? LzwSize.Size12Bit : LzwSize.Size14Bit);
                    compressedData = lzw1.Compress(imageData);
                    break;
                default:
                    throw new FileTypeSaveException(ERR_UNKN_COMPR);
            }
            // Type 4 includes the type byte in the header, not in the data length.
            int headerLen = compression == 0 ? 3 : 2;
            // Type 3 & 5 have two more bytes, and include everything after the size in the data length.
            int dataLen = compressedData.Length + (compression == 0 ? 0 : 3);
            byte[] data = new byte[headerLen + dataLen];
            ArrayUtils.WriteUInt16ToByteArrayLe(data, 0, (ushort)dataLen);
            data[2] = imageType;
            int writeOffs = 3;
            if (compression != 0)
            {
                ArrayUtils.WriteUInt16ToByteArrayLe(data, writeOffs, (ushort)(m_Width * m_Height));
                writeOffs += 2;
            }
            Array.Copy(compressedData, 0, data, writeOffs, compressedData.Length);
            return data;
        }
    }
}
