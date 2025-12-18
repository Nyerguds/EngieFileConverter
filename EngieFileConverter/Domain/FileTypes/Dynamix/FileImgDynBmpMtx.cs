using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.GameData.Dynamix;
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
        public override String ShortTypeDescription { get { return "Dynamix BMP Matrix image"; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename, true);
            this.SetFileNames(filename);
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
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
                blockWidth = fullWidth;
                while (blockWidth > 7)
                {
                    if (fullWidth % blockWidth == 0 && (fullWidth % 8 == 0))
                        matchingWidths.Add(blockWidth);
                    blockWidth--;
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
            SaveOption[] opts = new SaveOption[is4bpp ? 3 : 4];
            Int32 opt = 0;
            if (!is4bpp)
            {
                Int32 saveType = fileToSave is FileImgDynScr && ((FileImgDynScr)fileToSave).IsMa8 ? 1 : 0;
                opts[opt++] = new SaveOption("TYP", SaveOptionType.ChoicesList, "Save type:", "VGA/BIN,MA8", saveType.ToString());
            }
            opts[opt++] = new SaveOption("BLW", SaveOptionType.Number, "Block width", "0,", blockWidth.ToString());
            opts[opt++] = new SaveOption("BLH", SaveOptionType.Number, "Block height", "0,", blockHeight.ToString());
            opts[opt++] = new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.savecompressionTypes), 1.ToString());
            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null)
                throw new NotSupportedException("File to save is empty!");
            Bitmap image = fileToSave.GetBitmap();
            if(image == null)
                throw new NotSupportedException("File to save is empty!");
            Int32 bpp = fileToSave.BitsPerPixel;
            PixelFormat pf = image.PixelFormat;
            Color[] palette = fileToSave.GetColors();
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 saveType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP"), out saveType);
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            Int32 blockWidth;
            Int32 blockHeight;
            if (!Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "BLW"), out blockWidth))
                throw new NotSupportedException("Could not parse block width!");
            if (blockWidth <= 0)
                throw new NotSupportedException("Bad block height: needs to be more than 0!");
            if (blockWidth % 8 != 0)
                throw new NotSupportedException("Bad block width: needs to be a multiple of 8!");
            if (width % blockWidth != 0)
                throw new NotSupportedException("Bad block width: not an exact part of the full image width!");
            if (!Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "BLH"), out blockHeight))
                throw new NotSupportedException("Could not parse block height!");
            if (blockHeight <= 0)
                throw new NotSupportedException("Bad block height: needs to be more than 0!");
            if (height % blockHeight != 0)
                throw new NotSupportedException("Bad block height: not an exact part of the full image height!");
            Int32 blockStride = ImageUtils.GetMinimumStride(blockWidth, bpp);
            // Cut into frames (from SaveOptions)
            Int32 matrixWidth = width / blockWidth;
            Int32 matrixHeight = height / blockHeight;
            Int32 nrOfFrames = matrixWidth * matrixHeight;
            if (nrOfFrames > Int16.MaxValue)
                throw new NotSupportedException("Blocks too small or image too large; cannot address more than " + Int16.MaxValue + " tiles.");
            Int32 stride;
            Byte[] fullImageData = ImageUtils.GetImageData(image, out stride);
            Byte[][] allFrames = new Byte[nrOfFrames][];
            Int32[] frameMatrix = new Int32[nrOfFrames];
            UInt32[] frameHashes = new UInt32[nrOfFrames];
            Dictionary<UInt32, List<Int32>> hashmap = new Dictionary<UInt32, List<Int32>>();
            for (Int32 y = 0; y < matrixHeight; y++)
            {
                for (Int32 x = 0; x < matrixWidth; x++)
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
            for (Int32 i = 0; i < nrOfFrames; i++)
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
                foreach (Int32 dupIndex in duplicates)
                {
                    if (dupIndex == i)
                        continue;
                    Byte[] dupData = allFrames[dupIndex];
                    // double-check if crc-equal data is actually equal.
                    if (dupData.SequenceEqual(curData))
                    {
                        allFrames[dupIndex] = null;
                        frameMatrix[dupIndex] = i;
                    }
                }
            }
            // Fix frame references to collapsed indices.
            for (Int32 i = 0; i < frameMatrix.Length; i++)
                frameMatrix[i] = translationTable[frameMatrix[i]];
            // Post-processing: Exchange rows and columns.
            Byte[] frameMatrixFinal = new Byte[4 + nrOfFrames * 2];
            ArrayUtils.WriteIntToByteArray(frameMatrixFinal, 0, 2, true, (UInt32)matrixWidth);
            ArrayUtils.WriteIntToByteArray(frameMatrixFinal, 2, 2, true, (UInt32)matrixHeight);

            for (Int32 i = 0; i < nrOfFrames; i++)
                ArrayUtils.WriteIntToByteArray(frameMatrixFinal, 4 + i * 2, 2, true, (UInt32)frameMatrix[i] + 0);
            
            // Make FileImageFrames object filled with frames
            FileFrames frs = new FileFrames();
            for (Int32 i = 0; i < currentActual; i++)
            {
                FileImageFrame fr = new FileImageFrame();
                Bitmap frImage = ImageUtils.BuildImage(allFramesActual[i], blockWidth, blockHeight, blockStride, pf, palette, null);
                fr.LoadFileFrame(this, this, frImage, null, i);
                fr.SetColorsInPalette(palette.Length);
                fr.SetColors(this.m_Palette);
                fr.SetBitsPerColor(bpp);
                fr.SetFileClass(m_bpp == 8 ? FileClass.Image8Bit : FileClass.Image4Bit);
                fr.SetTransparencyMask(this.TransparencyMask);
                frs.AddFrame(fr);
            }
            // Call SaveToBmpChunk to turn into normal bmp
            List<DynamixChunk> imageChunks = this.SaveToChunks(frs, compressionType, saveType);
            // Fill matrix data
            DynamixChunk mtxChunk = new DynamixChunk("MTX", frameMatrixFinal);
            imageChunks.Add(mtxChunk);
            // Build final bmp
            DynamixChunk bmpChunk = DynamixChunk.BuildChunk("BMP", imageChunks.ToArray());
            return bmpChunk.WriteChunk();
        }
    }
}