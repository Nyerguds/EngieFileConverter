using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.KotB;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileImgKotB : SupportedFileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KotB PAK"; } }
        public override String[] FileExtensions { get { return new String[] { "pak" }; } }
        public override String ShortTypeDescription { get { return "KotB PAK file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get{ return 4; } }

        public FileImgKotB() { }

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
            Int32 datalen = fileData.Length;
            Byte[] decompressed = EACompression.RleDecode(fileData, 0, null);
            if (decompressed == null || decompressed.Length == 0)
                throw new FileLoadException("Error decompressing file.");
            /*/
            String dumpPath;
            if (sourcePath == null)
                dumpPath = "kotb_decomp.pak";
            else
                dumpPath = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath)) + "_decomp" + Path.GetExtension(sourcePath);
            File.WriteAllBytes(dumpPath, decompressed);
            //*/
            Int32 byteWidth = decompressed[0];
            Int32 height = decompressed[1];
            Int32 actualWidth = byteWidth * 8; // 4 times a 1/2 byte image
            Int32 fourLinesStride = byteWidth * 4;
            // Some files seem cut off, but seem to end on an incomplete line of garbare which only contains some of the bits.
            // The play court images are notorious for this; I suspect they have a hardcoded cutoff height in the game code.
            // They all seem to use the Rio one (which is complete) for the court image itself.
            Int32 cutoffHeight = (decompressed.Length - 2) / fourLinesStride;
            Byte[] oneBitQuadImage = ImageUtils.ConvertTo8Bit(decompressed, actualWidth * 4, cutoffHeight, 2, 1, true, ref fourLinesStride);
            // For a 12x83, the data is actually (12*4)x83. This forms quadruple-width rows to be combined to one.
            Byte[] actualImage = new Byte[actualWidth * height];
            for (Int32 y = 0; y < cutoffHeight; y++)
            {
                Int32 offset = fourLinesStride * y;
                Int32 finalOffset = actualWidth * y;
                for (Int32 x = 0; x < actualWidth; x++)
                {
                    Byte bit1 = oneBitQuadImage[offset + x];
                    Byte bit2 = (Byte)(oneBitQuadImage[offset + actualWidth + x] << 1);
                    Byte bit3 = (Byte)(oneBitQuadImage[offset + actualWidth * 2 + x] << 2);
                    Byte bit4 = (Byte)(oneBitQuadImage[offset + actualWidth * 3 + x] << 3);
                    actualImage[finalOffset + x] = (Byte)(bit1 | bit2 | bit3 | bit4);
                }
            }
            Int32 stride = actualWidth;
            Byte[] fourbitImage = ImageUtils.ConvertFrom8Bit(actualImage, actualWidth, height, 4, true, ref stride);
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            this.m_LoadedImage = ImageUtils.BuildImage(fourbitImage, actualWidth, height, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Bitmap image = fileToSave.GetBitmap();
            if (image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new NotSupportedException("Only 4-bit images can be saved as KotB PAK file!");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            Int32 dataLen = imageData.Length;
            Int32 imgWidth = image.Width;
            Int32 imgHeight = image.Height;
            Int32 saveWidth = ((image.Width + 7) / 8) * 8;
            Int32 byteWidth = saveWidth / 8;
            Int32 eightBitWidth = saveWidth * 4;
            if(saveWidth > 255 || imgHeight > 255)
                throw new NotSupportedException("Image too large to be saved into this format!");

            Byte[] eightbitImage = ImageUtils.ConvertTo8Bit(imageData, imgWidth, imgHeight, 0, 4, true, ref stride);
            if (saveWidth > imgWidth)
                eightbitImage = ImageUtils.Change8BitStride(eightbitImage, stride, imgHeight, saveWidth, false, 0);
            // For a 12x83, the data is actually (12*4)x83. This forms quadruple-width rows to be combined to one.
            Byte[] oneBitQuadImage = new Byte[eightBitWidth * imgHeight];
            for (Int32 y = 0; y < imgHeight; y++)
            {
                Int32 offset = saveWidth * y;
                Int32 finalOffset = eightBitWidth * y;
                for (Int32 x = 0; x < saveWidth; x++)
                {
                    oneBitQuadImage[finalOffset + x] = (Byte)(eightbitImage[offset + x] & 1);
                    oneBitQuadImage[finalOffset + imgWidth + x] = (Byte)((eightbitImage[offset + x] >> 1) & 1);
                    oneBitQuadImage[finalOffset + imgWidth * 2 + x] = (Byte)((eightbitImage[offset + x] >> 2) & 1);
                    oneBitQuadImage[finalOffset + imgWidth * 3 + x] = (Byte)((eightbitImage[offset + x] >> 3) & 1);
                }
            }
            Byte[] finalImageData = ImageUtils.ConvertFrom8Bit(oneBitQuadImage, eightBitWidth, imgHeight, 1, true, ref eightBitWidth);
            Byte[] finalData = new Byte[finalImageData.Length + 2];
            finalData[0] = (Byte)byteWidth;
            finalData[1] = (Byte)imgHeight;
            Array.Copy(finalImageData, 0, finalData, 2, finalImageData.Length);
            //return finalData;
            Byte[] compressedData = EACompression.RleEncode(finalData);
            return compressedData;
        }
    }
}