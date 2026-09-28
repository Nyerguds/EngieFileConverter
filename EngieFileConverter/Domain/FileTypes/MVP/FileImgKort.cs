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

        public override int Width { get { return 320; } }
        public override int Height { get { return 240; } }

        public override string IdCode { get { return "KortImg"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "KORT Image"; } }
        public override string[] FileExtensions { get { return new string[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009", "010", "011", "012", "013", "014", "015", "016", "017" }; } }
        public override string LongTypeName { get { return "KORT Image file"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get{ return 8; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData)
        {
            int datalen = fileData.Length;
            if (datalen == 0 || datalen % 2 != 0)
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            int len = this.Width * this.Height;
            byte[] imageData = new byte[len];
            int destOffs = 0;
            //Poor Man's RLE: each byte is just followed by the repetition amount, without grouping for non-repeating bytes.
            for (int i = 0; i < datalen; i += 2)
            {
                int col = fileData[i];
                int rep = fileData[i+1];
                if (rep == 0)
                    throw new FileTypeLoadException(ERR_DECOMPR);
                for (uint replen = 0; replen < rep; ++replen)
                {
                    if (destOffs >= len)
                        throw new FileTypeLoadException(ERR_DECOMPR_LEN);
                    imageData[destOffs++] = (byte)col;
                }
            }
            if (destOffs < len)
                throw new FileTypeLoadException(ERR_DECOMPR_LEN);
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
            Bitmap image = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            // reorder lines
            image.RotateFlip(RotateFlipType.Rotate180FlipX);
            this.m_LoadedImage = image;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap image = this.PerformPreliminaryChecks(fileToSave);
            int stride;
            // stride collapse is probably not needed... 320 is divisible by 4.
            byte[] imageData = ImageUtils.GetImageData(image, out stride, true);
            int dataLen = imageData.Length;
            byte[] flippedData = new byte[dataLen];
            for (int y = 0; y < this.Height; ++y)
                Array.Copy(imageData, (this.Height - 1 - y) * this.Width, flippedData, y * this.Width, this.Width);
            byte[] comprData = new byte[dataLen * 2];
            int inPtr = 0;
            int outPtr = 0;
            while (inPtr < dataLen)
            {
                int start = inPtr;
                int end = Math.Min(inPtr + 0xFF, dataLen);
                byte cur = flippedData[inPtr];
                for (; inPtr < end && flippedData[inPtr] == cur; ++inPtr) { }
                comprData[outPtr++] = cur;
                comprData[outPtr++] = (byte)(inPtr - start);
            }
            byte[] finalData = new byte[outPtr];
            Array.Copy(comprData, 0, finalData, 0, outPtr);
            return finalData;
        }

        private Bitmap PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            if (image.Width != 320 || image.Height != 240 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new FileTypeSaveException(ErrFixedBppsAndSize(320, 240, ShortTypeName, 8));
            return image;
        }

    }
}