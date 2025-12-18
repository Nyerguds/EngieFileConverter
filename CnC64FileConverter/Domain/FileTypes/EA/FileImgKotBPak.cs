using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Compression;

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
        public override Int32 BitsPerPixel { get { return 4; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Bitmap image = fileToSave == null ? null : fileToSave.GetBitmap();
            if (image == null || image.PixelFormat != PixelFormat.Format4bppIndexed)
                return new SaveOption[0];
            // Check if last line is completely made of colour #0 pixels. If so, suggest cutoff option.
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] imageBytes = ImageUtils.GetImageData(image, out stride);
            Int32 lastLineOffs = stride * (height - 1);
            Byte[] lastLine = ImageUtils.ConvertTo8Bit(imageBytes, width, 1, lastLineOffs, 4, true, ref stride);
            for (Int32 x = 0; x < width; x++)
                if (lastLine[x] != 0)
                    return new SaveOption[0];
            return new SaveOption[] { new SaveOption("CUT", SaveOptionType.Boolean, "Trim 0-value lines off the end.", "1") };
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < 4)
                throw new FileTypeLoadException("File too short to decompress header!");
            // First RLE byte value is 0. Not allowed.
            if ((fileData[0] & 0x7F) == 0)
                throw new FileTypeLoadException("Error decompressing file.");
            Int32 dataEnd = fileData.Length - 2;
            UInt32 dataLen = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, dataEnd, 2, true);
            if (dataLen < 2)
                throw new FileTypeLoadException("Decompressed size in file does not match expected data length.");
            Byte[] decompressed = null;
            Int32 decompressedLength = RleCompressionHighBitCopy.RleDecode(fileData, 0, (UInt32)dataEnd, ref decompressed, true);
            if (decompressedLength == -1)
                throw new FileTypeLoadException("Decompression failed: illegal RLE value encountered.");
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
            Int32 endHeight = (decompressedLength - 2) / fourLinesStride;
            if (endHeight < imgHeight)
                this.ExtraInfo = "Data cut off at " + endHeight + " lines";
            // Final 1-bit image is four times the image width. Convert to 1 byte per bit for editing convenience.
            Byte[] oneBitQuadImage = ImageUtils.ConvertTo8Bit(decompressed, imgWidth * 4, endHeight, 2, 1, true, ref fourLinesStride);
            // Array for 4-bit image where each byte is one pixel. Will be converted to true 4bpp later.
            Byte[] pixelImage = new Byte[imgWidth * imgHeight];
            // Combine the bits into the new array.
            Int32 lineOffset = 0;
            for (Int32 y = 0; y < endHeight; y++)
            {
                Int32 offset1 = fourLinesStride * y;
                Int32 offset2 = offset1 + imgWidth;
                Int32 offset3 = offset2 + imgWidth;
                Int32 offset4 = offset3 + imgWidth;
                Int32 realOffset = lineOffset;
                for (Int32 x = 0; x < imgWidth; x++)
                {
                    // Take the 4 bits by skipping imgWidth bytes for each next one.
                    Byte bit1 = oneBitQuadImage[offset1 + x];
                    Byte bit2 = (Byte)(oneBitQuadImage[offset2 + x] << 1);
                    Byte bit3 = (Byte)(oneBitQuadImage[offset3 + x] << 2);
                    Byte bit4 = (Byte)(oneBitQuadImage[offset4 + x] << 3);
                    pixelImage[realOffset] = (Byte)(bit1 | bit2 | bit3 | bit4);
                    realOffset++;
                }
                lineOffset += imgWidth;
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
                eightbitImage = ImageUtils.ChangeStride(eightbitImage, stride, imgHeight, alignedWidth, false, 0);
            // Trim end, creating cut-off images like the original court ones. The original height is saved,
            // and the decompressed data value at the end will be used to calculate the true height.
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
            Byte[] compressedData = RleCompressionHighBitCopy.RleEncode(finalData);
            Int32 dataEnd = compressedData.Length;
            Byte[] finalCompressedData = new Byte[dataEnd + 2];
            Array.Copy(compressedData, finalCompressedData, dataEnd);
            ArrayUtils.WriteIntToByteArray(finalCompressedData, dataEnd, 2, true, (UInt32)finalData.Length);
            return finalCompressedData;
        }
    }
}