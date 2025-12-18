using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileImgKort : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 240; } }

        public override String IdCode { get { return "KortImg"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KORT Image"; } }
        public override String[] FileExtensions { get { return new String[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009", "010", "011", "012", "013", "014", "015", "016", "017" }; } }
        public override String ShortTypeDescription { get { return "KORT Image file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerPixel { get{ return 8; } }

        public FileImgKort() { }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            Int32 datalen = fileData.Length;
            if (datalen == 0 || datalen % 2 != 0)
                throw new FileTypeLoadException("Illegal size: KORT image files contain byte pairs!");
            Int32 len = this.Width * this.Height;
            Byte[] imageData = new Byte[len];
            Int32 destOffs = 0;
            //Poor Man's RLE: each byte is just followed by the repetition amount, without grouping for non-repeating bytes.
            for (Int32 i = 0; i < datalen; i += 2)
            {
                Int32 col = fileData[i];
                Int32 rep = fileData[i+1];
                if (rep == 0)
                    throw new FileTypeLoadException("Repetition value 0 encountered. Not a KORT image file.");
                for (UInt32 replen = 0; replen < rep; ++replen)
                {
                    if (destOffs >= len)
                        throw new FileTypeLoadException("Decoded image does not fit in 320×240!");
                    imageData[destOffs++] = (Byte)col;
                }
            }
            if (destOffs < len)
                throw new FileTypeLoadException("Decoded image is smaller than 320×240!");
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
            Bitmap image = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            // reorder lines
            image.RotateFlip(RotateFlipType.Rotate180FlipX);
            this.m_LoadedImage = image;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Bitmap image = fileToSave.GetBitmap();
            if (image.Width != 320 || image.Height != 240 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320×240 images can be saved as KORT image file!");
            Int32 stride;
            // stride collapse is probably not needed... 320 is divisible by 4.
            Byte[] imageData = ImageUtils.GetImageData(image, out stride, true);
            Int32 dataLen = imageData.Length;
            Byte[] flippedData = new Byte[dataLen];
            for (Int32 y = 0; y < this.Height; ++y)
                Array.Copy(imageData, (this.Height - 1 - y) * this.Width, flippedData, y * this.Width, this.Width);
            Byte[] comprData = new Byte[dataLen * 2];
            Int32 inPtr = 0;
            Int32 outPtr = 0;
            while (inPtr < dataLen)
            {
                Int32 start = inPtr;
                Int32 end = Math.Min(inPtr + 0xFF, dataLen);
                Byte cur = flippedData[inPtr];
                for (; inPtr < end && flippedData[inPtr] == cur; ++inPtr) { }
                comprData[outPtr++] = cur;
                comprData[outPtr++] = (Byte)(inPtr - start);
            }
            Byte[] finalData = new Byte[outPtr];
            Array.Copy(comprData, 0, finalData, 0, outPtr);
            return finalData;
        }
    }
}