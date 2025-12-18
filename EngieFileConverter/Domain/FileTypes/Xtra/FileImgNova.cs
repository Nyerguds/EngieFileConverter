using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.IO;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Image format of Cover Girl Strip Poker.
    /// Uses a 4-byte flag-based RLE compression.
    /// Does not compress repeating sequences of less than 5 bytes.
    /// </summary>
    public class FileImgNova : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Nova image"; } }
        public override String[] FileExtensions { get { return new String[] { "ppp" }; } }
        public override String ShortTypeDescription { get { return "Nova image file"; } }
        public override Int32 ColorsInPalette { get { return this.m_PaletteLoaded ? base.ColorsInPalette : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_PaletteLoaded;
        public Boolean StartPosSeven { get; private set; }

        private static readonly Byte[] IdBytesNova = Encoding.ASCII.GetBytes("NOVA");
        
        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            Int32 len = fileData.Length;
            if (len < 8)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeName + ".");
            if (!fileData.Take(IdBytesNova.Length).SequenceEqual(IdBytesNova))
                throw new FileTypeLoadException("Not a " + ShortTypeName + ".");
            Int32 width = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            Int32 height = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 6, 1, true);
            List<String> extraInfo = new List<String>();
            String paletteFilename = Path.GetFileNameWithoutExtension(sourcePath) + ".pal";
            String palettePath = sourcePath == null ? null : Path.Combine(Path.GetDirectoryName(sourcePath), paletteFilename);
            if (palettePath != null && File.Exists(palettePath) && new FileInfo(palettePath).Length == 0x300)
            {
                m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPaletteFile(palettePath));
                m_PaletteLoaded = true;
                extraInfo.Add("Palette loaded from " + paletteFilename);
            }
            Int32 imageSize = width * height;
            Byte[] imageData = new Byte[imageSize];
            Int32 ptr = 0;
            Int32 startPos = 8;
            // If it starts with a repetition of the value 00, then index 7 is used as start.
            this.StartPosSeven = len >= 11 && fileData[7] == 0xFF && fileData[8] == 0x00;
            if (this.StartPosSeven)
                startPos--;
            extraInfo.Add("Data started from position " + startPos);
            // Decompress flag-based RLE.
            // The flag is 0xFF. It is followed by one byte for the value to fill,
            // and then two bytes for the amount of repetitions.
            Int32 i;
            for (i = startPos; i < len && ptr < imageSize; i++)
            {
                Byte value = fileData[i];
                if (value != 0xFF)
                    imageData[ptr++] = value;
                else
                {
                    if (i + 4 > len)
                        break;
                    value = fileData[++i];
                    Int32 repeat = fileData[++i] + (fileData[++i] << 8);
                    Int32 endPoint = repeat + ptr;
                    if (endPoint > imageSize)
                        endPoint = imageSize;
                    for (; ptr < endPoint; ptr++)
                        imageData[ptr] = value;
                }
            }
            m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, m_Palette, null);
            this.ExtraInfo = String.Join("\n", extraInfo.ToArray());
            this.SetFileNames(sourcePath);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("No source data given!");
            if (fileToSave.BitsPerPixel != 8)
                throw new NotSupportedException("This format needs an 8bpp image.");
            Int32 width = fileToSave.Width;
            Int32 height = fileToSave.Height;

            if (width > 0xFFFF || height > 0xFF)
                throw new NotSupportedException("The given image is too large.");
            Int32 stride;
            Byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Int32 len = imageBytes.Length;
            Int32 curBufLen = len;
            // Compressed data should never exceed original size since compression only triggers on sequences of 4 or more,
            // though it could in case of 0xFF bytes, since they always need to be encoded with a flag.
            Byte[] compressBuffer = new Byte[curBufLen];
            Int32 ptr = 0;
            for (Int32 i = 0; i < len; i++)
            {
                Byte curval = imageBytes[i];
                Int32 repeat = i;
                for (; repeat < len && imageBytes[repeat] == curval; repeat++) { }
                repeat -= i;
                Boolean compress = repeat >= 4 || curval == 0xFF;
                if (curBufLen <= ptr + (compress ? 3 : 0))
                {
                    Int32 newLen = curBufLen + len;
                    Byte[] newCompressBuffer = new Byte[newLen];
                    Array.Copy(compressBuffer, 0, newCompressBuffer, 0, curBufLen);
                    curBufLen = newLen;
                }
                if (compress)
                {
                    compressBuffer[ptr++] = 0xFF;
                    compressBuffer[ptr++] = curval;
                    compressBuffer[ptr++] = (Byte) (repeat & 0xFF);
                    compressBuffer[ptr++] = (Byte) (repeat >> 0xFF);
                    i += repeat - 1;
                }
                else
                    compressBuffer[ptr++] = curval;
            }
            // Check if the data starts with a repeat of a 00 value.
            Boolean fromSeven = len >= 4 && compressBuffer[0] == 0xFF && compressBuffer[1] == 0x00;
            Int32 headerLen = fromSeven ? 7 : 8;
            Byte[] compressedData = new Byte[ptr + headerLen];
            Array.Copy(IdBytesNova, 0, compressedData, 0, IdBytesNova.Length);
            ArrayUtils.WriteIntToByteArray(compressedData, 4, 2, true, (UInt16) width);
            compressedData[6] = (Byte) height;
            Array.Copy(compressBuffer, 0, compressedData, headerLen, ptr);
            return compressedData;
        }
    }

}