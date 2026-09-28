using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwBitFntUni : SupportedFileType
    {
        protected const string ERR_SIZEHEADER = "File size value in header does not match file data length.";

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image1Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return this.m_Width; } }
        public override int Height { get { return this.m_Height; } }
        protected int m_Width;
        protected int m_Height;
        public override string IdCode { get { return "WwBitFntUni"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood Unicode BitFont"; } }
        public override string[] FileExtensions { get { return new string[] { "fnt" }; } }
        public override string LongTypeName { get { return "Westwood Unicode BitFont (RA2)"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 1; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }
        // <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        //public override Boolean[] TransparencyMask { get { return null; }

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
            if (fileData.Length < 0x1C)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            string format = Encoding.ASCII.GetString(fileData, 0, 4);
            if (!String.Equals(format, "fonT", StringComparison.InvariantCulture))
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            int ideographicSpaceWidth = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true);
            //UInt32 dataStart = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true);
            int stride = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0x08, 4, true);
            int fontDataHeight = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 4, true);
            this.m_Height = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0x10, 4, true);
            // Start at 0
            this.m_Width = 0;
            // count: highest encountered ID. But all IDs are +1.
            int count = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0x14, 4, true);
            int symbolDataSize = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0x18, 4, true);
            int symbolImageSize = stride * fontDataHeight;
            int hiddenDataLen = symbolDataSize - 1 - symbolImageSize;
            if (hiddenDataLen < 0)
                throw new FileTypeLoadException("Symbol size is too small to contain specified width * height.");
            bool hasPadding = hiddenDataLen >= 1;
            int readOffset = 0x1C;
            List<int>[] symbolUsage = new List<int>[count];
            if (fileData.Length <= readOffset + 0x20000)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            for (int i = 0; i <= 0xFFFF; ++i)
            {
                int symbolIndex = (ushort)(ArrayUtils.ReadIntFromByteArray(fileData, readOffset, 2, true)) - 1;
                if (symbolIndex >= count)
                    throw new FileTypeLoadException("Symbol index exceeds number of symbols.");
                if (symbolIndex >= 0)
                {
                    if (symbolUsage[symbolIndex] == null)
                        symbolUsage[symbolIndex] = new List<int>();
                    symbolUsage[symbolIndex].Add(i);
                }
                readOffset += 2;
            }
            this.m_Palette = new Color[] { Color.White, Color.Black };
            this.m_FramesList = new SupportedFileType[0x10000];
            HashSet<int>  emptySymbols = new HashSet<int>();

            for (int i = 0; i < count; ++i)
            {
                int symbReadOffset = readOffset;
                readOffset += symbolDataSize;
                if (readOffset > fileData.Length)
                    throw new FileTypeLoadException("File is not long enough to contain all symbols.");
                List<int> curSymbolUsage = symbolUsage[i];
                if (curSymbolUsage == null)
                    break;
                byte symbolWidth = fileData[symbReadOffset++];
                // Technically the read font width is irrelevant, and thus it might be wrong.
                if (symbolWidth > m_Width && ImageUtils.GetMinimumStride(symbolWidth, 1) <= stride)
                    m_Width = symbolWidth;
                byte[] symbolData = new byte[symbolImageSize];
                int curSymbolOffset = symbReadOffset;
                Array.Copy(fileData, symbReadOffset, symbolData, 0, symbolImageSize);
                symbReadOffset += symbolImageSize;
                // Extract hidden data.
                byte[] hiddenData = new byte[hiddenDataLen];
                Array.Copy(fileData, symbReadOffset, hiddenData, 0, hiddenDataLen);
                symbReadOffset += hiddenDataLen;
                byte padding = 0;
                if (hasPadding)
                    padding = hiddenData[0];
                // This should only happen once, on the ideographic space.
                if (symbolWidth == 0)
                {
                    emptySymbols.UnionWith(curSymbolUsage);
                }
                int usageCount = curSymbolUsage.Count;
                for (int use = 0; use < usageCount; ++use)
                {
                    int index = curSymbolUsage[use];
                    bool isIdeoSpace = index == 0x3000;
                    int widthToUse = symbolWidth;
                    int strideToUse = stride;
                    byte[] dataToUse = symbolData;
                    if (isIdeoSpace)
                    {
                        widthToUse = ideographicSpaceWidth;
                        strideToUse = ImageUtils.GetMinimumStride(ideographicSpaceWidth, 1);
                        int expectedSize = fontDataHeight * strideToUse;
                        if (symbolData.Length < expectedSize)
                        {
                            dataToUse = new byte[expectedSize];
                        }
                    }
                    Bitmap curFrImg = null;
                    if (widthToUse > 0)
                    {
                        try
                        {
                            if (padding > 0)
                            {
                                // Quite tedious; convert to 8-bit, paste in larger frame, convert back to 1-bit.
                                byte[] data8Bit = ImageUtils.ConvertTo8Bit(dataToUse, widthToUse, fontDataHeight, 0, 1, true, ref strideToUse);
                                int newWidth = widthToUse + padding;
                                byte[] newData = new byte[newWidth * fontDataHeight];
                                ImageUtils.PasteOn8bpp(newData, newWidth, fontDataHeight, newWidth, data8Bit, widthToUse, fontDataHeight, strideToUse,
                                    new Rectangle(0, 0, widthToUse, fontDataHeight), null, true);
                                strideToUse = newWidth;
                                widthToUse = newWidth;
                                dataToUse = ImageUtils.ConvertFrom8Bit(newData, widthToUse, fontDataHeight, 1, true, ref strideToUse);
                            }
                            curFrImg = ImageUtils.BuildImage(dataToUse, widthToUse, fontDataHeight, strideToUse, PixelFormat.Format1bppIndexed, this.m_Palette, null);
                        }
                        catch (Exception ex)
                        {
                            throw new FileTypeLoadException("Error building image: " + ex.Message, ex);
                        }
                    }
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this, curFrImg, sourcePath, index);
                    framePic.SetBitsPerColor(this.BitsPerPixel);
                    framePic.SetFileClass(this.FrameInputFileClass);
                    framePic.SetNeedsPalette(this.NeedsPalette);
                    StringBuilder extraInfoFr = new StringBuilder();
                    extraInfoFr.Append("Data: ").Append(symbolImageSize).Append(" bytes");
                    if (symbolImageSize > 0)
                        extraInfoFr.Append(" @ 0x").Append(curSymbolOffset.ToString("X"));
                    extraInfoFr.Append("\nStored image dimensions: ").Append(symbolWidth).Append("x").Append(fontDataHeight);
                    if (hiddenDataLen > 0)
                    {
                        string hiddenStr = String.Join(", ", hiddenData.Select(b => b.ToString("X2")).ToArray());
                        extraInfoFr.AppendLine();
                        extraInfoFr.Append("Hidden data applied as padding: " + padding + " pixels");
                        if (hiddenDataLen > 1)
                            extraInfoFr.AppendLine().Append("Full hidden data: " + hiddenStr);
                    }
                    if (widthToUse == 0)
                    {
                        extraInfoFr.AppendLine().Append("Empty frame defaulting to ideographical space.");
                    }
                    else if (isIdeoSpace)
                    {
                        extraInfoFr.AppendLine();
                        extraInfoFr.Append("Ideographical space; width overridden by header value.");
                    }
                    framePic.SetExtraInfo(extraInfoFr.ToString());
                    this.m_FramesList[index] = framePic;
                }
            }
            for (int i = 0; i <= 0xFFFF; ++i)
            {
                if (m_FramesList[i] == null)
                {
                    FileImageFrame frameEmpty = new FileImageFrame();
                    frameEmpty.LoadFileFrame(this, this, null, sourcePath, i);
                    frameEmpty.SetBitsPerColor(this.BitsPerPixel);
                    frameEmpty.SetFileClass(this.FrameInputFileClass);
                    frameEmpty.SetNeedsPalette(this.NeedsPalette);
                    frameEmpty.SetExtraInfo("Empty frame");
                    m_FramesList[i] = frameEmpty;
                }
            }
            StringBuilder extraInfo = new StringBuilder();
            if (hiddenDataLen > 0)
            {
                extraInfo.AppendFormat("Symbols contain {0} bytes of hidden data.\nFirst byte is used as extra padding.", hiddenDataLen);
            }
            if (emptySymbols.Count > 0)
            {
                int[] emptySymbolsArr = emptySymbols.ToArray();
                Array.Sort(emptySymbolsArr);
                string emptySymbolsStr = GeneralUtils.GroupNumbers(emptySymbolsArr, true);
                if (extraInfo.Length > 0)
                    extraInfo.AppendLine();
                extraInfo.Append("Empty but stored symbols: " + emptySymbolsStr);
            }
            this.ExtraInfo = extraInfo.ToString();
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            int maxUsedHeight;
            this.PerformPreliminaryChecks(fileToSave, out maxUsedHeight);
            FileFramesWwBitFntUni fontFile = fileToSave as FileFramesWwBitFntUni;
            int fontHeight = fontFile != null ? Math.Max(fontFile.Height, maxUsedHeight) : maxUsedHeight;
            string emptySymbolsStr = "3000-301F, 3303-33CD";
            return new Option[]
            {
                new Option("HE", OptionInputType.Number, "Font height:", maxUsedHeight + ",255", fontHeight.ToString()),
                new Option("IDR", OptionInputType.String, "Hexadecimal ranges where empty entries will use the ideographic symbol. 0x3000 is included in this automatically, even if not added.", emptySymbolsStr),
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            int calcFontHeight;
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave, out calcFontHeight);
            int fontHeight;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "HE"), out fontHeight))
                fontHeight = calcFontHeight;
            int idSpaceWidth = frames.Length > 0x3000 ? frames[0x3000].Width : 0x14;
            int spaceWidth = frames.Length > 0x20 ? frames[0x20].Width : 0;
            string emptySymbolsStr = Option.GetSaveOptionValue(saveOptions, "IDR");
            HashSet<int> emptySymbols = new HashSet<int>(GeneralUtils.GetRangedNumbers(emptySymbolsStr, true));
            // Always save ideographic width as real symbol.
            emptySymbols.Add(0x3000);

            int imageListcount = frames.Length; // should always be 0x10000
            // This doesn't use calcFontHeight because the trimming might be able to take a few pixels off.
            int fontDataHeight = 0;
            // Needs to be at least either the ideological space width or the double of the normal space width.
            int fontDataWidth = Math.Max(spaceWidth * 2, idSpaceWidth);
            byte[][] fontListBin = new byte[0x10000][];
            byte[] fontListStrides = new byte[0x10000];
            for (int i = 0; i < imageListcount; ++i)
            {
                SupportedFileType ffs = frames[i];
                int symbWidth = ffs.Width;
                if (symbWidth == 0)
                    continue;
                if (fontDataWidth < symbWidth)
                    fontDataWidth = symbWidth;
                int yoffSet = 0;
                int height = ffs.Height;
                int imStride;
                byte[] imageData = ImageUtils.GetImageData(ffs.GetBitmap(), out imStride, true);
                // Image width cannot exceed 255, so 1-bit image stride can only be up to 32
                fontListStrides[i] = (byte)imStride;
                fontListBin[i] = imageData;
                // Write as little data as possible, by trimming the bottoms of the frames.
                ImageUtils.OptimizeYHeight(imageData, imStride, ref height, ref yoffSet, true, 0, fontHeight, false);
                int newHeight = height + yoffSet;
                if (newHeight > fontDataHeight)
                    fontDataHeight = newHeight;
            }
            int stride = ImageUtils.GetMinimumStride(fontDataWidth, 1);
            int dataLength = stride * fontDataHeight;
            //int hideData = 2;
            //Int32 blockLength = dataLength + 1 + hideData;
            int blockLength = dataLength;
            byte[] ideographData = new byte[blockLength];
            // Make list of binary entries, skipping any with width == 0
            for (int i = 0; i < imageListcount; ++i)
            {
                SupportedFileType ffs = frames[i];
                int symbWidth = ffs.Width;
                int symbHeight = ffs.Height;
                byte[] imageData = fontListBin[i];
                byte imStride = fontListStrides[i];
                // Always write the ideographic width as empty symbol.
                if (symbWidth == 0 || i == 0x3000)
                {
                    // Special case: force saving these.
                    if (emptySymbols.Contains(i))
                        fontListBin[i] = ideographData;
                    continue;
                }
                if (imStride != stride)
                {
                    imageData = ImageUtils.ChangeStride(imageData, imStride, symbHeight, stride, false, 0);
                }
                byte[] output = new byte[blockLength];
                output[0] = (byte)symbWidth;
                // This might trim the bottom of an image, or not fill the entire array. Either way, should work correctly.
                Array.Copy(imageData, 0, output, 1, dataLength);
                /*
                if (hideData > 1)
                {
                    // Test: write amount of pixels into the data behind the normal image data.
                    int refStride = stride;
                    Byte[] imageData8 = ImageUtils.ConvertTo8Bit(imageData, symbWidth, symbHeight, 0, 1, true, ref refStride);
                    int px = imageData8.Count(b => b != 0);
                    ArrayUtils.WriteUInt16ToByteArrayLe(output, dataLength + 1, (UInt16)px);
                }
                */
                fontListBin[i] = output;
            }
            // This ensures that even if the amount of given images does not reach the end of the ranges of empty symbols,
            // these are still explicitly set in the full data array.
            int[] emptyRange = emptySymbols.ToArray();
            Array.Sort(emptyRange);
            for (int i = 0; i < emptyRange.Length; ++i)
            {
                int actualIndex = emptyRange[i];
                if (actualIndex < imageListcount)
                    continue;
                // For the entries beyond those handled in the previous loop.
                fontListBin[actualIndex] = ideographData;
            }
            // Optimise list by removing all duplicates, and write all entries to the index.
            // this list is an array of 2-byte Words. It's treated as simple byte array for convenience.
            byte[] index = new byte[0x20000];
            int curNum = 0;
            for (int i = 0; i < 0x10000; ++i)
            {
                byte[] curWritesymbol = fontListBin[i];
                if (curWritesymbol == null)
                    continue;
                curNum++;
                if (curNum > 0xFFFF)
                    throw new NotSupportedException("This type can only contain 65535 (0xFFFF) different characters.");
                ArrayUtils.WriteIntToByteArray(index, i << 1, 2, true, (ulong)curNum);
                // Find any duplicates of this symbol in the following data, set their index to the same as this one,
                // and mark them as "ignore" by setting them to null. Start at i; everything before it is already checked.
                // This means the inner loop becomes shorter as this progresses. The checked block includes the symbol width.
                for (int j = i + 1; j < 0x10000; ++j)
                {
                    byte[] curChecksymbol = fontListBin[j];
                    if (curChecksymbol == null)
                        continue;
                    bool isEqual = true;
                    // If the reference is the same, this is the ideograph space, and it gets an immediate pass.
                    if (curWritesymbol != curChecksymbol)
                    {
                        // Seems x.SequenceEquals(y) is about 4x as slow as a simple 'for' loop here, so I stopped using it.
                        // Since they're stride-adjusted, the arrays are all of equal length at this point anyway.
                        isEqual = ArrayUtils.ArraysAreEqual(curWritesymbol, curChecksymbol);
                        if (!isEqual)
                            continue;
                    }
                    // Saved as UInt16, so j needs to be doubled to get the index.
                    ArrayUtils.WriteIntToByteArray(index, j << 1, 2, true, (ulong)curNum);
                    // Remove it from any following equal checks, to further increase speed,
                    // and to end up with a list containing only uniques.
                    fontListBin[j] = null;
                }
            }
            byte[] outputArray = new byte[0x1C + index.Length + curNum * blockLength];
            Array.Copy(Encoding.ASCII.GetBytes("fonT"), outputArray, 4);
            // ideographic width.
            ArrayUtils.WriteIntToByteArray(outputArray, 0x04, 4, true, (ulong)idSpaceWidth);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x08, 4, true, (ulong)stride);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x0C, 4, true, (ulong)fontDataHeight);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x10, 4, true, (ulong)fontHeight);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x14, 4, true, (ulong)curNum);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x18, 4, true, (uint)blockLength);
            // currently at 0x1C.
            Array.Copy(index, 0, outputArray, 0x1C, index.Length);
            int curIndex = 0x1C + index.Length;
            // Go over fontListBin and write all symbols that remain in it;
            // that should be exactly and only the remaining non-duplicates.
            for (int i = 0; i < fontListBin.Length; ++i)
            {
                byte[] symbolBytes = fontListBin[i];
                if (symbolBytes == null)
                    continue;
                Array.Copy(symbolBytes, 0, outputArray, curIndex, blockLength);
                curIndex += blockLength;
            }
            return outputArray;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out int height)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            if (frames == null || frames.Length == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            int max = ushort.MaxValue + 1;
            if (frames.Length > ushort.MaxValue + 1)
                throw new ArgumentException("This type can only handle up to "+ max + " frames.", "fileToSave");
            height = -1;
            int nrOfFrames = frames.Length;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                // Allow without further checks.
                if (frame.GetBitmap() == null)
                    continue;
                // Actual checks.
                if (frame.BitsPerPixel != this.BitsPerPixel)
                    throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 1), "fileToSave");
                height = Math.Max(height, frame.Height);
                if (frame.Width> 255)
                    throw new ArgumentException("Frame width exceeds 255.", "fileToSave");
            }
            return frames;
        }
    }
}
