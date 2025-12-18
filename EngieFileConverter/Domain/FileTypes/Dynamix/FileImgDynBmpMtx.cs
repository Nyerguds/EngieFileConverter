using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.FileData.Dynamix;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgDynBmpMtx : FileFramesDynBmp
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | (this.m_bpp == 8 ? FileClass.Image8Bit : FileClass.Image4Bit); } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.Image4Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }
        public override String ShortTypeName { get { return "Dynamix BMP MTX"; } }
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        public override String LongTypeName { get { return "Dynamix BMP Matrix image"; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[0]; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename, true);
            this.SetFileNames(filename);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 fullWidth = fileToSave.Width;
            Int32 fullHeight = fileToSave.Height;
            Int32 bpp = fileToSave.BitsPerPixel;
            Int32 blockWidth;
            Int32 blockHeight;
            if (fileToSave is FileImgDynBmpMtx && fileToSave.Frames != null && fileToSave.Frames.Length > 0)
            {
                blockWidth = fileToSave.Frames[0].Width;
                blockHeight = fileToSave.Frames[0].Height;
            }
            else
            {
                List<Int32> matchingWidths = new List<Int32>();
                blockWidth = (fullWidth + 7 / 8) * 8;
                while (blockWidth > 7)
                {
                    if (fullWidth % blockWidth == 0)
                        matchingWidths.Add(blockWidth);
                    blockWidth -= 8;
                }
                blockWidth = matchingWidths.Count == 0 ? 8 : matchingWidths.Min();
                List<Int32> matchingHeights = new List<Int32>();
                blockHeight = fullHeight;
                while (blockHeight > 5)
                {
                    if (fullHeight % blockHeight == 0)
                        matchingHeights.Add(blockHeight);
                    blockHeight--;
                }
                blockHeight = matchingHeights.Count == 0 ? 5 : matchingHeights.Min();
            }
            Boolean is4bpp = bpp == 4;
            Option[] opts = new Option[is4bpp ? 3 : 4];
            Int32 opt = 0;
            if (!is4bpp)
                opts[opt++] = new Option("TYP", OptionInputType.ChoicesList, "Save type:", "BIN / VGA,MA8", "0");
            opts[opt++] = new Option("BLW", OptionInputType.Number, "Block width", "0,", blockWidth.ToString());
            opts[opt++] = new Option("BLH", OptionInputType.Number, "Block height", "0,", blockHeight.ToString());
            opts[opt++] = new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", SaveCompressionTypes), "1");
            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            Int32 bpp = fileToSave.BitsPerPixel;
            PixelFormat pf = image.PixelFormat;
            Color[] palette = fileToSave.GetColors();
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 saveTypeInt;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TYP"), out saveTypeInt);
            DynBmpInternalType saveType = saveTypeInt == 0 ? DynBmpInternalType.BinVga : DynBmpInternalType.Ma8;
            Int32 compression;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compression);
            DynBmpInternalCompression compressionType = (DynBmpInternalCompression)compression;
            Int32 blockWidth;
            Int32 blockHeight;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "BLW"), out blockWidth))
                throw new ArgumentException("Could not parse block width.", "saveOptions");
            if (blockWidth <= 0)
                throw new ArgumentException("Bad block height: needs to be more than 0.", "saveOptions");
            if (blockWidth % 8 != 0)
                throw new ArgumentException("Bad block width: needs to be a multiple of 8.", "saveOptions");
            if (width % blockWidth != 0)
                throw new ArgumentException("Bad block width: not an exact part of the full image width.", "saveOptions");
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "BLH"), out blockHeight))
                throw new ArgumentException("Could not parse block height.", "saveOptions");
            if (blockHeight <= 0)
                throw new ArgumentException("Bad block height: needs to be more than 0.", "saveOptions");
            if (height % blockHeight != 0)
                throw new ArgumentException("Bad block height: not an exact part of the full image height.", "saveOptions");
            Int32 blockStride = ImageUtils.GetMinimumStride(blockWidth, bpp);
            // Cut into frames (from SaveOptions)
            Int32 matrixWidth = width / blockWidth;
            Int32 matrixHeight = height / blockHeight;
            Int32 nrOfFrames = matrixWidth * matrixHeight;
            if (nrOfFrames > Int16.MaxValue)
                throw new ArgumentException("Blocks too small or image too large; cannot address more than " + Int16.MaxValue + " tiles.");
            Int32 stride;
            Byte[] fullImageData = ImageUtils.GetImageData(image, out stride);
            Byte[][] allFrames = new Byte[nrOfFrames][];
            Int32[] frameMatrix = new Int32[nrOfFrames];
            UInt32[] frameHashes = new UInt32[nrOfFrames];
            Dictionary<UInt32, List<Int32>> hashmap = new Dictionary<UInt32, List<Int32>>();
            for (Int32 y = 0; y < matrixHeight; ++y)
            {
                for (Int32 x = 0; x < matrixWidth; ++x)
                {
                    Int32 i = x * matrixHeight + y;
                    Byte[] frameData = ImageUtils.CopyFrom8bpp(fullImageData, width, height, stride, new Rectangle(x * blockStride, y * blockHeight, blockStride, blockHeight));
                    allFrames[i] = frameData;
                    frameMatrix[i] = i;
                    UInt32 hash = Crc32.ComputeChecksum(frameData);
                    frameHashes[i] = hash;
                    if (!hashmap.ContainsKey(hash))
                        hashmap.Add(hash, new List<Int32>(new Int32[] {i}));
                    else
                        hashmap[hash].Add(i);
                }
            }
            // Detect and replace duplicates.
            Int32 currentActual = 0;
            Byte[][] allFramesActual = new Byte[nrOfFrames][];
            Int32[] translationTable = new Int32[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Byte[] curData = allFrames[i];
                if (curData == null)
                    continue;
                allFramesActual[currentActual] = curData;
                translationTable[i] = currentActual;
                currentActual++;
                List<Int32> duplicates = hashmap[frameHashes[i]];
                if (duplicates.Count < 2)
                    continue;
                Int32 dupCount = duplicates.Count;
                for (Int32 j = 0; j < dupCount; ++j)
                {
                    Int32 dupIndex = duplicates[j];
                    if (dupIndex == i)
                        continue;
                    Byte[] dupData = allFrames[dupIndex];
                    // double-check if crc-equal data is actually equal.
                    if (!ArrayUtils.ArraysAreEqual(curData, dupData))
                        continue;
                    allFrames[dupIndex] = null;
                    frameMatrix[dupIndex] = i;
                }
            }
            // Fix frame references to collapsed indices.
            for (Int32 i = 0; i < nrOfFrames; ++i)
                frameMatrix[i] = translationTable[frameMatrix[i]];
            // Post-processing: Exchange rows and columns.
            Byte[] frameMatrixFinal = new Byte[4 + nrOfFrames * 2];
            ArrayUtils.WriteUInt16ToByteArrayLe(frameMatrixFinal, 0, (UInt16)matrixWidth);
            ArrayUtils.WriteUInt16ToByteArrayLe(frameMatrixFinal, 2, (UInt16)matrixHeight);

            for (Int32 i = 0; i < nrOfFrames; ++i)
                ArrayUtils.WriteUInt16ToByteArrayLe(frameMatrixFinal, 4 + i * 2, (UInt16)frameMatrix[i]);

            // Make FileImageFrames object filled with frames
            FileFrames frs = new FileFrames();
            for (Int32 i = 0; i < currentActual; ++i)
            {
                FileImageFrame fr = new FileImageFrame();
                Bitmap frImage = ImageUtils.BuildImage(allFramesActual[i], blockWidth, blockHeight, blockStride, pf, palette, null);
                fr.LoadFileFrame(this, this, frImage, null, i);
                fr.SetColors(palette);
                fr.SetBitsPerColor(bpp);
                fr.SetFileClass(m_bpp == 8 ? FileClass.Image8Bit : FileClass.Image4Bit);
                frs.AddFrame(fr);
            }
            // Call SaveToChunks to turn into normal bmp
            List<DynamixChunk> imageChunks = this.SaveToChunks(frs, saveType, compressionType);
            // Fill matrix data
            DynamixChunk mtxChunk = new DynamixChunk("MTX", frameMatrixFinal);
            imageChunks.Add(mtxChunk);
            // Build final bmp
            DynamixChunk bmpChunk = DynamixChunk.BuildChunk("BMP", imageChunks.ToArray());
            return bmpChunk.WriteChunk();
        }
    }
}