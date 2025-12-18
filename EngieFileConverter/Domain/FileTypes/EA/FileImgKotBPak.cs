using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.FileData.Compression;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileImgKotB : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit; } }

        public override String IdCode { get { return "KotbPak"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "KotB PAK"; } }
        public override String[] FileExtensions { get { return new String[] { "pak" }; } }
        public override String LongTypeName { get { return "Kings of the Beach PAK file"; } }
        //public override Boolean NeedsPalette { get { return false; } }
        public override Int32 BitsPerPixel { get { return 4; } }

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
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            // First RLE byte value is 0. Not allowed.
            if ((fileData[0] & 0x7F) == 0)
                throw new FileTypeLoadException(ERR_DECOMPR);
            Int32 dataEnd = fileData.Length - 2;
            UInt32 dataLen = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, dataEnd);
            if (dataLen < 2)
                throw new FileTypeLoadException(ERR_DECOMPR_LEN);
            Byte[] decompressed = null;
            Int32 decompressedLength = RleCompressionHighBitCopy.RleDecode(fileData, 0, (UInt32)dataEnd, ref decompressed, true);
            if (decompressedLength == -1)
                throw new FileTypeLoadException("Decompression failed: illegal RLE value encountered.");
            if (decompressedLength != dataLen)
                throw new FileTypeLoadException(ERR_DECOMPR_LEN);
            Int32 byteWidth = decompressed[0];
            Int32 imgHeight = decompressed[1];
            if (byteWidth == 0 || imgHeight == 0)
                throw new FileTypeLoadException(ERR_DIM_ZERO);
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

            Int32 stride;
            Byte[] imageData = ImageUtils.PlanarLinesToLinear(decompressed, 2, imgWidth, endHeight, 4, byteWidth, 1, 4, out stride);
            if (endHeight < imgHeight)
            {
                Byte[] imageDataExpanded = new Byte[stride * imgHeight];
                Array.Copy(imageData, 0, imageDataExpanded, 0, imageData.Length);
                imageData = imageDataExpanded;
            }
            this.m_Palette = PaletteUtils.GetEgaPalette();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, imgWidth, imgHeight, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);

        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 imgWidth;
            Int32 imgHeight;
            Bitmap image = this.PerformPreliminaryChecks(fileToSave, out imgWidth, out imgHeight);
            Int32 stride;
            Byte[] imageBytes = ImageUtils.GetImageData(image, out stride);
            Int32 lastLineOffs = stride * (imgHeight - 1);
            Byte[] lastLine = ImageUtils.ConvertTo8Bit(imageBytes, imgWidth, 1, lastLineOffs, 4, true, ref stride);
            for (Int32 x = 0; x < imgWidth; ++x)
                if (lastLine[x] != 0)
                    return new Option[0];
            return new Option[] { new Option("CUT", OptionInputType.Boolean, "Trim 0-value lines off the end.", "1") };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Int32 imgWidth;
            Int32 imgHeight;
            Bitmap image = this.PerformPreliminaryChecks(fileToSave, out imgWidth, out imgHeight);
            Boolean trimEnd = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CUT"));
            Int32 saveHeight = imgHeight;
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
                    for (Int32 x = 0; x < stride; ++x)
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
            for (Int32 y = 0; y < imgHeight; ++y)
            {
                Int32 offset = alignedWidth * y;
                Int32 finalOffset = eightBitWidth * y;
                for (Int32 x = 0; x < alignedWidth; ++x)
                {
                    // Split up and write the 4 bits.
                    for (Int32 i = 0; i < 4; ++i)
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
            ArrayUtils.WriteInt16ToByteArrayLe(finalCompressedData, dataEnd, finalData.Length);
            return finalCompressedData;
        }

        private Bitmap PerformPreliminaryChecks(SupportedFileType fileToSave, out Int32 width, out Int32 height)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 4), "fileToSave");
            width = image.Width;
            height = image.Height;
            if (width * height / 2 > UInt16.MaxValue)
                throw new ArgumentException(ERR_IMAGE_TOO_LARGE, "fileToSave");
            if (width > 320 || height > 200)
                throw new ArgumentException(ERR_IMAGE_TOO_LARGE, "fileToSave");
            return image;
        }
    }
}