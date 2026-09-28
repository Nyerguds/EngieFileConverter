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
        public override string ShortTypeName { get { return "Dynamix BMP MTX"; } }
        public override string[] FileExtensions { get { return new string[] { "bmp" }; } }
        public override string LongTypeName { get { return "Dynamix BMP Matrix image"; } }
        public override bool[] TransparencyMask { get { return new bool[0]; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename, true);
            this.SetFileNames(filename);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            int fullWidth = fileToSave.Width;
            int fullHeight = fileToSave.Height;
            int bpp = fileToSave.BitsPerPixel;
            int blockWidth;
            int blockHeight;
            if (fileToSave is FileImgDynBmpMtx && fileToSave.Frames != null && fileToSave.Frames.Length > 0)
            {
                blockWidth = fileToSave.Frames[0].Width;
                blockHeight = fileToSave.Frames[0].Height;
            }
            else
            {
                List<int> matchingWidths = new List<int>();
                blockWidth = (fullWidth + 7 / 8) * 8;
                while (blockWidth > 7)
                {
                    if (fullWidth % blockWidth == 0)
                        matchingWidths.Add(blockWidth);
                    blockWidth -= 8;
                }
                blockWidth = matchingWidths.Count == 0 ? 8 : matchingWidths.Min();
                List<int> matchingHeights = new List<int>();
                blockHeight = fullHeight;
                while (blockHeight > 5)
                {
                    if (fullHeight % blockHeight == 0)
                        matchingHeights.Add(blockHeight);
                    blockHeight--;
                }
                blockHeight = matchingHeights.Count == 0 ? 5 : matchingHeights.Min();
            }
            bool is4bpp = bpp == 4;
            Option[] opts = new Option[is4bpp ? 3 : 4];
            int opt = 0;
            if (!is4bpp)
                opts[opt++] = new Option("TYP", OptionInputType.ChoicesList, "Save type:", "BIN / VGA,MA8", "0");
            opts[opt++] = new Option("BLW", OptionInputType.Number, "Block width", "0,", blockWidth.ToString());
            opts[opt++] = new Option("BLH", OptionInputType.Number, "Block height", "0,", blockHeight.ToString());
            opts[opt++] = new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", SaveCompressionTypes), "1");
            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE, "fileToSave");
            int bpp = fileToSave.BitsPerPixel;
            PixelFormat pf = image.PixelFormat;
            Color[] palette = fileToSave.GetColors();
            int width = image.Width;
            int height = image.Height;
            int saveTypeInt;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TYP"), out saveTypeInt);
            DynBmpInternalType saveType = saveTypeInt == 0 ? DynBmpInternalType.BinVga : DynBmpInternalType.Ma8;
            int compression;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compression);
            DynBmpInternalCompression compressionType = (DynBmpInternalCompression)compression;
            int blockWidth;
            int blockHeight;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "BLW"), out blockWidth))
                throw new FileTypeSaveException("Could not parse block width.");
            if (blockWidth <= 0)
                throw new FileTypeSaveException("Bad block height: needs to be more than 0.");
            if (blockWidth % 8 != 0)
                throw new FileTypeSaveException("Bad block width: needs to be a multiple of 8.");
            if (width % blockWidth != 0)
                throw new FileTypeSaveException("Bad block width: not an exact part of the full image width.");
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "BLH"), out blockHeight))
                throw new FileTypeSaveException("Could not parse block height.");
            if (blockHeight <= 0)
                throw new FileTypeSaveException("Bad block height: needs to be more than 0.");
            if (height % blockHeight != 0)
                throw new FileTypeSaveException("Bad block height: not an exact part of the full image height.");
            int blockStride = ImageUtils.GetMinimumStride(blockWidth, bpp);
            // Cut into frames (from SaveOptions)
            int matrixWidth = width / blockWidth;
            int matrixHeight = height / blockHeight;
            int nrOfFrames = matrixWidth * matrixHeight;
            if (nrOfFrames > short.MaxValue)
                throw new FileTypeSaveException("Blocks too small or image too large; cannot address more than " + short.MaxValue + " tiles.");
            int stride;
            byte[] fullImageData = ImageUtils.GetImageData(image, out stride);
            byte[][] allFrames = new byte[nrOfFrames][];
            int[] frameMatrix = new int[nrOfFrames];
            uint[] frameHashes = new uint[nrOfFrames];
            // The Dictionary is used for a preliminary sorting of chunks into those with the same hash.
            // A secondary operation then checks which of these are actually equal.
            Dictionary<uint, List<int>> hashmap = new Dictionary<uint, List<int>>();
            for (int y = 0; y < matrixHeight; ++y)
            {
                for (int x = 0; x < matrixWidth; ++x)
                {
                    int i = x * matrixHeight + y;
                    byte[] frameData = ImageUtils.CopyFrom8bpp(fullImageData, width, height, stride, new Rectangle(x * blockStride, y * blockHeight, blockStride, blockHeight));
                    allFrames[i] = frameData;
                    frameMatrix[i] = i;
                    uint hash = Crc32.ComputeChecksum(frameData);
                    frameHashes[i] = hash;
                    if (!hashmap.ContainsKey(hash))
                        hashmap.Add(hash, new List<int>(new int[] {i}));
                    else
                        hashmap[hash].Add(i);
                }
            }
            // Detect and replace duplicates.
            int currentActual = 0;
            byte[][] allFramesActual = new byte[nrOfFrames][];
            int[] translationTable = new int[nrOfFrames];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                byte[] curData = allFrames[i];
                if (curData == null)
                    continue;
                allFramesActual[currentActual] = curData;
                translationTable[i] = currentActual;
                currentActual++;
                List<int> duplicates = hashmap[frameHashes[i]];
                if (duplicates.Count < 2)
                    continue;
                int dupCount = duplicates.Count;
                for (int j = 0; j < dupCount; ++j)
                {
                    int dupIndex = duplicates[j];
                    if (dupIndex == i)
                        continue;
                    byte[] dupData = allFrames[dupIndex];
                    // double-check if crc-equal data is actually equal.
                    if (!ArrayUtils.ArraysAreEqual(curData, dupData))
                        continue;
                    allFrames[dupIndex] = null;
                    frameMatrix[dupIndex] = i;
                }
            }
            // Fix frame references to collapsed indices.
            for (int i = 0; i < nrOfFrames; ++i)
                frameMatrix[i] = translationTable[frameMatrix[i]];
            // Post-processing: Exchange rows and columns.
            byte[] frameMatrixFinal = new byte[4 + nrOfFrames * 2];
            ArrayUtils.WriteUInt16ToByteArrayLe(frameMatrixFinal, 0, (ushort)matrixWidth);
            ArrayUtils.WriteUInt16ToByteArrayLe(frameMatrixFinal, 2, (ushort)matrixHeight);

            for (int i = 0; i < nrOfFrames; ++i)
                ArrayUtils.WriteUInt16ToByteArrayLe(frameMatrixFinal, 4 + i * 2, (ushort)frameMatrix[i]);

            // Make FileImageFrames object filled with frames
            FileFrames frs = new FileFrames();
            for (int i = 0; i < currentActual; ++i)
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