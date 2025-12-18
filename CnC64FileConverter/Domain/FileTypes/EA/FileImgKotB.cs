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

        public override FileClass FileClass { get { return FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KotB PAK"; } }
        public override String[] FileExtensions { get { return new String[] { "pak" }; } }
        public override String ShortTypeDescription { get { return "Kings of the Beach PAK file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 4; } }
        public Boolean WasCutOff { get; private set; }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Bitmap image = fileToSave == null ? null : fileToSave.GetBitmap();
            if (image == null)
                return new SaveOption[0];
            Int32 stride;
            Byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride);
            Int32 lastLineOffs = stride * (image.Height-1);
            byte[] lastLine = ImageUtils.ConvertTo8Bit(imageBytes, image.Width, 1, lastLineOffs, 4, true, ref stride);
            for (Int32 x = 0; x < image.Width; x++)
                if (lastLine[x] != 0)
                    return new SaveOption[0];
            return new SaveOption[] {new SaveOption("CUT", SaveOptionType.Boolean, "Trim 0-value lines off the end.", "1") };
        }

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
            if (fileData.Length < 2)
                throw new FileTypeLoadException("File is empty.");
            // First RLE byte value is 0. Not allowed.
            if ((fileData[0] & 0x7F) == 0)
                throw new FileTypeLoadException("Error decompressing file.");
            if (fileData.Length < 4)
                throw new FileTypeLoadException("File too short to decompress header!");
            UInt32 dataLen = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, fileData.Length - 2, 2, true);
            Byte[] decompressed = new Byte[dataLen];
            Int32 decompressedLength = EACompression.RleDecode(fileData, 0, (UInt32)(fileData.Length-2), decompressed, true);
            if (decompressedLength != dataLen)
                throw new FileTypeLoadException("Decompressed size does not match length in file.");
            Int32 byteWidth = decompressed[0];
            Int32 imgHeight = decompressed[1];
            if (byteWidth == 0 || imgHeight == 0)
                throw new FileTypeLoadException("Image dimensions can't be 0.");
            Int32 expectedSize = byteWidth * 4 * imgHeight;
            if (expectedSize > UInt16.MaxValue)
                throw new FileTypeLoadException("Image dimensions too large.");

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
            // Some files seem cut off, but the data length at the end of the file accurately indicates this.
            // The play court images do this: their cut-off height is always set at 85 lines.
            // They use the Rio one (which is complete) for the court image itself.
            if ((decompressedLength - 2) % fourLinesStride != 0)
                throw new FileTypeLoadException("Data cutoff is not exactly on one line!");
            Int32 cutoffHeight = (decompressedLength - 2) / fourLinesStride;
            if (cutoffHeight < imgHeight)
            {
                this.ExtraInfo = "Data cut off at " + cutoffHeight + " lines";
                this.WasCutOff = true;
            }
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Bitmap image = fileToSave.GetBitmap();
            if (image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new NotSupportedException("Only 4-bit images can be saved as KotB PAK file!");
            Boolean trimEnd = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CUT"));
            Int32 imgWidth = image.Width;
            Int32 imgHeight = image.Height;
            if (imgWidth * imgHeight / 2 > UInt16.MaxValue)
                throw new NotSupportedException("Image too large to be saved into this format!");
            if (imgWidth > 320 || imgHeight > 200)
                throw new NotSupportedException("Image too large to be saved into this format!");
            Int32 saveHeight = image.Height;
            // Width has to be a multiple of 8.
            Int32 byteWidth = (image.Width + 7) / 8;
            Int32 alignedWidth = byteWidth * 8;
            // Width is multiplied by 4. This forms quadruple-width rows to be filled with the bits from one row.
            Int32 eightBitWidth = alignedWidth * 4;
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            Byte[] eightbitImage = ImageUtils.ConvertTo8Bit(imageData, imgWidth, imgHeight, 0, 4, true, ref stride);
            if (alignedWidth > imgWidth)
                eightbitImage = ImageUtils.Change8BitStride(eightbitImage, stride, imgHeight, alignedWidth, false, 0);
            // Trim end, creating "broken" images like the original court ones.
            if (trimEnd)
            {
                for (Int32 y = saveHeight-1; y > 0; y--)
                {
                    Int32 offset = stride * y;
                    Boolean isEmpty = true;
                    for (Int32 x = 0; x < stride; x++)
                    {
                        if (eightbitImage[offset + x] == 0)
                            continue;
                        isEmpty = false;
                        break;
                    }
                    if (isEmpty)
                        imgHeight--;
                    else
                        break;
                }
            }
            if (imgHeight == image.Height)
                trimEnd = false;
            Byte[] oneBitQuadImage = new Byte[eightBitWidth * imgHeight];
            for (Int32 y = 0; y < imgHeight; y++)
            {
                Int32 offset = alignedWidth * y;
                Int32 finalOffset = eightBitWidth * y;
                for (Int32 x = 0; x < alignedWidth; x++)
                {
                    // Split up and write the 4 bits.
                    for (Int32 i = 0; i < 4; i++)
                        oneBitQuadImage[finalOffset + imgWidth * i + x] = (Byte)((eightbitImage[offset + x] >> i) & 1);
                }
            }
            // Compact to 1bpp image
            Byte[] finalImageData = ImageUtils.ConvertFrom8Bit(oneBitQuadImage, eightBitWidth, imgHeight, 1, true, ref eightBitWidth);
            Byte[] finalData = new Byte[finalImageData.Length + 2];
            finalData[0] = (Byte)byteWidth;
            finalData[1] = (Byte)saveHeight;
            Array.Copy(finalImageData, 0, finalData, 2, finalImageData.Length);
            //return finalData;
            Byte[] compressedData = EACompression.RleEncode(finalData);
            Int32 dataEnd = compressedData.Length;
            Byte[] finalCompressedData = new Byte[dataEnd + 2];
            Array.Copy(compressedData, finalCompressedData, dataEnd);
            ArrayUtils.WriteIntToByteArray(finalCompressedData, dataEnd, 2, true, (UInt32)finalData.Length);
            return finalCompressedData;
        }
    }
}