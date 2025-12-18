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
            if (fileData.Length == 0)
                throw new FileTypeLoadException("File is empty.");
            // First RLE byte value is 0. Not allowed.
            if ((fileData[0] & 0x7F) == 0)
                throw new FileTypeLoadException("Error decompressing file.");
            // Structure is: 1. An RLE command, and if the command is repeat (same X and Y dimension) 1 byte for the dimensions, else two bytes.
            if (fileData.Length < 2 && ((fileData[0] & 0x80) == 0 || fileData.Length < 3))
                throw new FileTypeLoadException("File too short to decompress header!");
            // Decompress just the dimensions header, to define the final decompression array size.
            Byte[] dimensions = new Byte[2];
            if (EACompression.RleDecode(fileData, 0, null, dimensions) <= 0)
                throw new FileTypeLoadException("Error decompressing file.");
            Int32 byteWidth = dimensions[0];
            Int32 imgHeight = dimensions[1];
            if (byteWidth == 0 || imgHeight == 0)
                throw new FileTypeLoadException("Image dimensions can't be 0.");
            Byte[] decompressed = new Byte[2 + byteWidth * 4 * imgHeight];
            if (EACompression.RleDecode(fileData, 0, null, decompressed) <= 0)
                throw new FileTypeLoadException("Error decompressing file.");
            
            // OVERALL PRINCIPLE:
            // Each full scanline is made up of four 1-bpp "lines" of the byte width found in the header.
            // So the real stride is (byte width * 4). These bytes are the data to create a 4bpp image, meaning,
            // two pixels per byte. So the actual image width is (real stride * 2), or, put differently, (byte width * 8).
            // As mentioned, the bits in such a line of data are four blocks of 1-bpp data, and the single bits of these
            // four lines need to be combined by x-offset, giving the final 4-bit pixel values.

            // Single line length for horizontally-composed image is
            // four "bit frames" with a stride equal to the given byte width.
            Int32 fourLinesStride = byteWidth * 4;
            // Actual final image pixel width. One scanline is four 1-bpp lines of stride
            // interpreted as 4bpp image, so with 2 pixels per byte.
            Int32 imgWidth = fourLinesStride * 2;
            // Some files seem cut off, but seem to end on an incomplete line of garbare which only contains some of the bits.
            // The play court images are notorious for this; I suspect they have a hardcoded cutoff height in the game code.
            // They all seem to use the Rio one (which is complete) for the court image itself.
            Int32 cutoffHeight = (decompressed.Length - 2) / fourLinesStride;
            // Final 1-bit image is four times the image width. Convert to 1 byte per bit for editing convenience.
            Byte[] oneBitQuadImage = ImageUtils.ConvertTo8Bit(decompressed, imgWidth * 4, cutoffHeight, 2, 1, true, ref fourLinesStride);
            // Array for 4-bit image where each byte is one pixel. Will be converted to true 4bpp later.
            Byte[] pixelImage = new Byte[imgWidth * imgHeight];
            // Combine the bits into the new array.
            for (Int32 y = 0; y < cutoffHeight; y++)
            {
                Int32 offset = fourLinesStride * y;
                Int32 finalOffset = imgWidth * y;
                for (Int32 x = 0; x < imgWidth; x++)
                {
                    // Take the 4 bits by skipping imgWidth bytes for each next one.


                    Byte bit1 = oneBitQuadImage[offset + x];
                    Byte bit2 = (Byte)(oneBitQuadImage[offset + imgWidth + x] << 1);
                    Byte bit3 = (Byte)(oneBitQuadImage[offset + imgWidth * 2 + x] << 2);
                    Byte bit4 = (Byte)(oneBitQuadImage[offset + imgWidth * 3 + x] << 3);
                    pixelImage[finalOffset + x] = (Byte)(bit1 | bit2 | bit3 | bit4);
                }
            }
            Int32 stride = imgWidth;
            Byte[] fourbitImage = ImageUtils.ConvertFrom8Bit(pixelImage, imgWidth, imgHeight, 4, true, ref stride);
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, null, false);
            this.m_LoadedImage = ImageUtils.BuildImage(fourbitImage, imgWidth, imgHeight, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
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