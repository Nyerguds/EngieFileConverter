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

        public override string IdCode { get { return "KotbPak"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "KotB PAK"; } }
        public override string[] FileExtensions { get { return new string[] { "pak" }; } }
        public override string LongTypeName { get { return "Kings of the Beach PAK file"; } }
        //public override Boolean NeedsPalette { get { return false; } }
        public override int BitsPerPixel { get { return 4; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            if (fileData.Length < 4)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            // First RLE byte value is 0. Not allowed.
            if ((fileData[0] & 0x7F) == 0)
                throw new FileTypeLoadException(ERR_DECOMPR);
            int dataEnd = fileData.Length - 2;
            uint dataLen = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, dataEnd);
            if (dataLen < 2)
                throw new FileTypeLoadException(ERR_DECOMPR_LEN);
            byte[] decompressed = null;
            int decompressedLength = RleCompressionHighBitCopy.RleDecode(fileData, 0, (uint)dataEnd, ref decompressed, true);
            if (decompressedLength == -1)
                throw new FileTypeLoadException("Decompression failed: illegal RLE value encountered.");
            if (decompressedLength != dataLen)
                throw new FileTypeLoadException(ERR_DECOMPR_LEN);
            int byteWidth = decompressed[0];
            int imgHeight = decompressed[1];
            if (byteWidth == 0 || imgHeight == 0)
                throw new FileTypeLoadException(ERR_DIM_ZERO);
            int expectedSize = byteWidth * 4 * imgHeight;
            if (expectedSize > ushort.MaxValue)
                throw new FileTypeLoadException("Image dimensions too large.");

            // OVERALL PRINCIPLE:
            // Each full scanline is made up of four 1-bpp "lines" of the byte width found in the header.
            // So the real stride is (byte width * 4). These bytes are the data to create a 4bpp image, meaning,
            // two pixels per byte. So the actual image width is (real stride * 2), or, put differently, (byte width * 8).
            // As mentioned, the bits in such a line of data are four blocks of 1-bpp data, and the single bits of these
            // four lines need to be combined by x-offset, giving the final 4-bit pixel values.

            // Single line length for horizontally-composed image is
            // four "bit frames" with a stride equal to the given byte width.
            int fourLinesStride = byteWidth * 4;
            // Actual final image pixel width. One scanline is four 1-bpp lines of stride
            // interpreted as 4bpp image, so with 2 pixels per byte.
            int imgWidth = fourLinesStride * 2;
            // Some files seem cut off, but the data length at the end of the file accurately indicates this.
            // The play court images do this: their cut-off height is always set at 85 lines.
            // They use the Rio one (which is complete) for the court image itself.
            if ((decompressedLength - 2) % fourLinesStride != 0)
                throw new FileTypeLoadException("Data cutoff is not exactly on one line.");
            int endHeight = (decompressedLength - 2) / fourLinesStride;
            if (endHeight < imgHeight)
                this.ExtraInfo = "Data cut off at " + endHeight + " lines";

            int stride;
            byte[] imageData = ImageUtils.PlanarLinesToLinear(decompressed, 2, imgWidth, endHeight, 4, byteWidth, 1, 4, out stride);
            if (endHeight < imgHeight)
            {
                byte[] imageDataExpanded = new byte[stride * imgHeight];
                Array.Copy(imageData, 0, imageDataExpanded, 0, imageData.Length);
                imageData = imageDataExpanded;
            }
            this.m_Palette = PaletteUtils.GetEgaPalette();
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, imgWidth, imgHeight, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);

        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            int imgWidth;
            int imgHeight;
            Bitmap image = this.PerformPreliminaryChecks(fileToSave, out imgWidth, out imgHeight);
            int stride;
            byte[] imageBytes = ImageUtils.GetImageData(image, out stride);
            int lastLineOffs = stride * (imgHeight - 1);
            byte[] lastLine = ImageUtils.ConvertTo8Bit(imageBytes, imgWidth, 1, lastLineOffs, 4, true, ref stride);
            for (int x = 0; x < imgWidth; ++x)
                if (lastLine[x] != 0)
                    return new Option[0];
            return new Option[] { new Option("CUT", OptionInputType.Boolean, "Trim 0-value lines off the end.", "1") };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            int imgWidth;
            int imgHeight;
            Bitmap image = this.PerformPreliminaryChecks(fileToSave, out imgWidth, out imgHeight);
            bool trimEnd = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CUT"));
            int saveHeight = imgHeight;
            // Width has to be a multiple of 8.
            int byteWidth = (image.Width + 7) / 8;
            int alignedWidth = byteWidth * 8;
            // Width is multiplied by 4. This forms quadruple-width rows to be filled with the bits from one row.
            int eightBitWidth = alignedWidth * 4;
            int stride;
            byte[] imageData = ImageUtils.GetImageData(image, out stride);
            byte[] eightbitImage = ImageUtils.ConvertTo8Bit(imageData, imgWidth, imgHeight, 0, 4, true, ref stride);
            if (alignedWidth > imgWidth)
                eightbitImage = ImageUtils.ChangeStride(eightbitImage, stride, imgHeight, alignedWidth, false, 0);
            // Trim end, creating cut-off images like the original court ones. The original height is saved,
            // and the decompressed data value at the end will be used to calculate the true height.
            if (trimEnd)
            {
                for (int y = saveHeight-1; y > 0; y--)
                {
                    int offset = stride * y;
                    bool isEmpty = true;
                    for (int x = 0; x < stride; ++x)
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
            byte[] oneBitQuadImage = new byte[eightBitWidth * imgHeight];
            for (int y = 0; y < imgHeight; ++y)
            {
                int offset = alignedWidth * y;
                int finalOffset = eightBitWidth * y;
                for (int x = 0; x < alignedWidth; ++x)
                {
                    // Split up and write the 4 bits.
                    for (int i = 0; i < 4; ++i)
                        oneBitQuadImage[finalOffset + imgWidth * i + x] = (byte)((eightbitImage[offset + x] >> i) & 1);
                }
            }
            // Compact to 1bpp image
            byte[] finalImageData = ImageUtils.ConvertFrom8Bit(oneBitQuadImage, eightBitWidth, imgHeight, 1, true, ref eightBitWidth);
            byte[] finalData = new byte[finalImageData.Length + 2];
            finalData[0] = (byte)byteWidth;
            finalData[1] = (byte)saveHeight;
            Array.Copy(finalImageData, 0, finalData, 2, finalImageData.Length);
            //return finalData;
            byte[] compressedData = RleCompressionHighBitCopy.RleEncode(finalData);
            int dataEnd = compressedData.Length;
            byte[] finalCompressedData = new byte[dataEnd + 2];
            Array.Copy(compressedData, finalCompressedData, dataEnd);
            ArrayUtils.WriteUInt16ToByteArrayLe(finalCompressedData, dataEnd, (ushort)finalData.Length);
            return finalCompressedData;
        }

        private Bitmap PerformPreliminaryChecks(SupportedFileType fileToSave, out int width, out int height)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            if (image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 4);
            width = image.Width;
            height = image.Height;
            if (width * height / 2 > ushort.MaxValue)
                throw new FileTypeSaveException(ERR_DIMENSIONS_TOO_LARGE);
            if (width > 320 || height > 200)
                throw new FileTypeSaveException(ERR_DIMENSIONS_TOO_LARGE);
            return image;
        }
    }
}