using Nyerguds.GameData.Westwood;
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

    public class FileImgKort : SupportedFileType
    {
        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 240; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KORT Image"; } }
        public override String[] FileExtensions { get { return new String[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009", "010", "011", "012", "013", "014", "015", "016", "017" }; } }
        public override String ShortTypeDescription { get { return "KORT Image file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get{ return 8; } }

        public FileImgKort() { }

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

        protected void LoadFromFileData(Byte[] fileData)
        {
            Int32 datalen = fileData.Length;
            if (datalen == 0 || datalen % 2 != 0)
                throw new FileTypeLoadException("Illegal size: KORT image files contain byte pairs!");
            Int32 len = this.Width * this.Height;
            Byte[] targetData = new Byte[len];
            Int32 destOffs = 0;
            for (Int32 i = 0; i < datalen; i += 2)
            {
                Int32 col = fileData[i];
                Int32 rep = fileData[i+1];
                if (rep == 0)
                    throw new FileTypeLoadException("Repetition value 0 encountered. Not a KORT image file.");


                for (UInt32 replen = 0; replen < rep; replen++)
                {
                    if (destOffs >= len)
                        throw new FileTypeLoadException("Decoded image does not fit in 320x240!");
                    targetData[destOffs++] = (Byte)col;
                }
            }
            // reorder lines
            Byte[] imageData = new Byte[len];
            for (Int32 y = 0; y < this.Height; y++)
                Array.Copy(targetData, (this.Height - 1 - y) * this.Width, imageData, y * this.Width, this.Width);
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerColor, null, false);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, m_Palette, null);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Bitmap image = fileToSave.GetBitmap();
            if (image.Width != 320 || image.Height != 240 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320x240 images can be saved as KORT image file!");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            Int32 dataLen = imageData.Length;
            Byte[] flippedData = new Byte[dataLen];
            for (Int32 y = 0; y < this.Height; y++)
                Array.Copy(imageData, (this.Height - 1 - y) * this.Width, flippedData, y * this.Width, this.Width);
            Byte[] finalData;
            // Well the option is there, so why not, lol.
            if (dontCompress)
            {
                finalData = new Byte[dataLen * 2];
                for (Int32 i = 0; i < dataLen; i++)
                {
                    Int32 i2 = i*2;
                    finalData[i2 + 0] = flippedData[i];
                    finalData[i2 + 1] = 1;
                }
            }
            else
            {
                Byte[] comprData = new Byte[dataLen * 2];
                Int32 inPtr = 0;
                Int32 outPtr = 0;
                while (inPtr < dataLen)
                {
                    Int32 start = inPtr;
                    Int32 end = Math.Min(inPtr + 0xFF, dataLen);
                    Byte cur = flippedData[inPtr];
                    for (; inPtr < end && flippedData[inPtr] == cur; inPtr++) { }
                    comprData[outPtr++] = cur;
                    comprData[outPtr++] = (Byte)(inPtr - start);
                }
                finalData = new Byte[outPtr];
                Array.Copy(comprData, 0, finalData, 0, outPtr);
            }
            return finalData;
        }
    }
}