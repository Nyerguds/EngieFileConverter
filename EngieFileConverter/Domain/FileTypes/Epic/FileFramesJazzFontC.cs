using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Epic;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileFramesJazzFontC : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override string IdCode { get { return "JazzFontC"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Jazz Compressed Font"; } }
        public override string[] FileExtensions { get { return new string[] { "0fn" }; } }
        public override string LongTypeName { get { return "Jazz Compressed Font "; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return new bool[] { true }; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected const string header = "Digital Dimensions";
        protected int SpaceWidth = -1;
        protected int LineHeight = -1;

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            if (fileData.Length < 0x17)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            byte[] hdrBytesCheck = Encoding.ASCII.GetBytes(header);
            for (int i = 0; i < hdrBytesCheck.Length; ++i)
                if (fileData[i] != hdrBytesCheck[i])
                    throw new FileTypeLoadException(ERR_BAD_HEADER);
            if (fileData[0x12] != 0x1A || fileData[0x15] != 0x00 || fileData[0x16] != 0x00)
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            this.SpaceWidth = fileData[0x13];
            this. LineHeight = fileData[0x14];
            int offset = 0x17;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            List<SupportedFileType> imageDataList = new List<SupportedFileType>();
            List<string> foundHidden = new List<string>();
            int index = 0;
            while (offset < fileData.Length)
            {
                int frameOffset = offset;
                int comprLen;
                int actualComprLen = -1;
                uint decompSize = (uint)ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true);
                offset += 2;
                Bitmap symb;
                if (decompSize == 0)
                {
                    symb = null;
                    comprLen = 2;
                }
                else
                {
                    comprLen = (int)ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true);
                    if (offset + 2 + comprLen > fileData.Length)
                        throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                    byte[] decompressedData = JazzRleCompression.RleDecodeJazz(fileData, (uint)offset, true, decompSize, false, out actualComprLen);
                    offset += comprLen + 2;
                    if (decompressedData == null || decompressedData.Length < 4)
                        throw new FileTypeLoadException(ERR_DECOMPR);
                    int symbWidth = (int)ArrayUtils.ReadIntFromByteArray(decompressedData, 0, 2, true);
                    int symbHeight = (int)ArrayUtils.ReadIntFromByteArray(decompressedData, 2, 2, true);
                    int imgSize = symbWidth * symbHeight;
                    if (imgSize != decompSize - 4)
                        throw new FileTypeLoadException(ERR_DECOMPR_LEN);
                    byte[] symbol = new byte[imgSize];
                    Array.Copy(decompressedData, 4, symbol, 0, imgSize);
                    symb = imgSize == 0 ? null : ImageUtils.BuildImage(symbol, symbWidth, symbHeight, symbWidth, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                }
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, symb, sourcePath, index);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetNeedsPalette(true);
                StringBuilder extraInfo = new StringBuilder();
                extraInfo.Append("Data offset: ").Append(frameOffset);
                extraInfo.Append('\n').Append("Data size: ").Append(comprLen);
                extraInfo.Append('\n').Append("Uncompressed size: ").Append(decompSize);
                if (actualComprLen != -1 && actualComprLen < comprLen)
                {
                    extraInfo.Append('\n').Append("Hidden data detected: ").Append(comprLen - actualComprLen).Append(" bytes at ").Append(frameOffset + 4 + actualComprLen);
                    foundHidden.Add(index.ToString());
                }
                if (symb == null)
                    extraInfo.Append('\n').Append("Empty frame.");
                frame.SetExtraInfo(extraInfo.ToString());
                imageDataList.Add(frame);
                index++;
            }
            this.ExtraInfo = "Space width in header: " + this.SpaceWidth
                           + "\nLine height in header: " + this.LineHeight;
            int hiddenFound = foundHidden.Count;
            if (hiddenFound > 0)
            {
                this.ExtraInfo += "\nHidden data found on ind" + (hiddenFound == 1 ? "ex " : "ices:\n")
                    + String.Join(", ", foundHidden.ToArray());
            }
            this.m_FramesList = imageDataList.ToArray();
            this.m_LoadedImage = null;

        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            FileFramesJazzFontC toSave = fileToSave as FileFramesJazzFontC;
            int spaceWidth = -1;
            int lineHeight = -1;
            if (toSave != null)
            {
                // Space width. As baseline, take the maximum width * 2 / 3
                spaceWidth = toSave.SpaceWidth;
                // Line height. Default calculation uses the most commonly used lowest point in the font.
                lineHeight = toSave.LineHeight;
            }
            if (spaceWidth == -1 || lineHeight == -1)
            {
                int maxWidth = 0;
                Dictionary<int, int> lastRowFreq = new Dictionary<int, int>();
                Dictionary<int, int> widthFreq = new Dictionary<int, int>();
                SupportedFileType[] frames = fileToSave.Frames;
                for (int i = 0; i < frames.Length; ++i)
                {
                    SupportedFileType frame = frames[i];
                    if (frame == null || frame.Width == 0 || frame.Height == 0 || frame.GetBitmap() == null)
                        continue;
                    int w = frame.Width;
                    int curAmount;
                    if (spaceWidth == -1)
                    {
                        maxWidth = Math.Max(maxWidth, w);
                        if (!widthFreq.TryGetValue(w, out curAmount))
                            curAmount = 0;
                        widthFreq[w] = curAmount + 1;
                    }
                    if (lineHeight == -1)
                    {
                        byte[] imageData = ImageUtils.GetImageData(frame.GetBitmap(), PixelFormat.Format8bppIndexed);
                        int height = imageData.Length - 1;
                        while (height >= 0 && imageData[height] == 0)
                            height--;
                        // Last found Y, from 0 to full height.
                        height = ((height + w) / w);
                        if (!lastRowFreq.TryGetValue(height, out curAmount))
                            curAmount = 0;
                        lastRowFreq[height] = curAmount + 1;
                    }
                }
                if (lineHeight == -1)
                {
                    int maxFound = 0;
                    int maxFoundAt = -1;
                    int[] rows = lastRowFreq.Keys.ToArray();
                    Array.Sort(rows);
                    for (int i = rows.Length-1; i >= 0; --i)
                    {
                        int row = rows[i];
                        int rowAmount = lastRowFreq[row];
                        if (rowAmount > maxFound)
                        {
                            maxFound = rowAmount;
                            maxFoundAt = row;
                        }
                    }
                    lineHeight = Math.Max(0, maxFoundAt);
                }
                if (spaceWidth == -1)
                {
                    int maxFound = 0;
                    int maxFoundAt = -1;
                    int[] widths = widthFreq.Keys.ToArray();
                    Array.Sort(widths);
                    for (int i = widths.Length - 1; i >= 0; --i)
                    {
                        int width = widths[i];
                        int widthAmount = widthFreq[width];
                        if (widthAmount > maxFound)
                        {
                            maxFound = widthAmount;
                            maxFoundAt = width;
                        }
                    }
                    // 2/3rd of the most commonly found width
                    spaceWidth = Math.Max(0, maxFoundAt) * 2 / 3;
                }
            }
            return new Option[] {
                new Option("SPW", OptionInputType.Number, "Space width", "0,255", lineHeight.ToString()),
                new Option("LNH", OptionInputType.Number, "Line height", "0,255", spaceWidth.ToString())
            };
        }
        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave);
            int spaceWidth;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "SPW"), out spaceWidth);
            int lineHeight;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "LNH"), out lineHeight);
            int nrOfSymbols = frames.Length;
            byte[][] imageData = new byte[nrOfSymbols][];
            for (int i = 0; i < nrOfSymbols; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.Width == 0 || frame.Height == 0 || frame.GetBitmap() == null)
                {
                    // Write dummy entry
                    imageData[i] = new byte[2];
                    continue;
                }
                byte[] byteData = ImageUtils.GetImageData(frame.GetBitmap(), true);
                // Trim off zeroes
                byte[] writeData = new byte[byteData.Length + 4];
                ArrayUtils.WriteIntToByteArray(writeData, 0, 2, true, (ulong)frame.Width);
                ArrayUtils.WriteIntToByteArray(writeData, 2, 2, true, (ulong)frame.Height);
                Array.Copy(byteData, 0, writeData, 4, byteData.Length);
                byte[] comprData = JazzRleCompression.RleEncodeJazz(writeData);
                // Add uncompressed size in front
                byte[] comprWriteData = new byte[comprData.Length + 2];
                ArrayUtils.WriteIntToByteArray(comprWriteData, 0, 2, true, (ulong)byteData.Length + 4);
                Array.Copy(comprData, 0, comprWriteData, 2, comprData.Length);
                imageData[i] = comprWriteData;
            }
            int bufLen = 0x17;
            for (int i = 0; i < nrOfSymbols; ++i)
                bufLen += imageData[i].Length;
            byte[] fontData = new byte[bufLen];
            Encoding.ASCII.GetBytes(header, 0, header.Length, fontData, 0);
            fontData[0x12] = 0x1A;
            fontData[0x13] = (byte)Math.Min(255, lineHeight);
            fontData[0x14] = (byte)Math.Min(255, spaceWidth);
            // 0x15 & 0x16 are both 00.
            int writeOffs = 0x17;
            for (int i = 0; i < nrOfSymbols; ++i)
            {
                byte[] curData = imageData[i];
                int curLength = curData.Length;
                Array.Copy(curData, 0, fontData, writeOffs, curLength);
                writeOffs += curLength;
            }
            return fontData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames == null ? 0 : frames.Length;
            if (nrOfFrames == 0)
                throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    continue;
                if (frame.BitsPerPixel != 8)
                    throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 8);
            }
            return frames;
        }
    }
}