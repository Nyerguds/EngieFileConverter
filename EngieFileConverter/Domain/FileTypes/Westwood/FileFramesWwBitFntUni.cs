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
        protected const String ERR_SIZEHEADER = "File size value in header does not match file data length.";

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image1Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwBitFntUni"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood Unicode BitFont"; } }
        public override String[] FileExtensions { get { return new String[] { "fnt" }; } }
        public override String LongTypeName { get { return "Westwood Unicode BitFont (RA2)"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 1; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        // <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        //public override Boolean[] TransparencyMask { get { return null; }

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
            if (fileData.Length < 0x1C)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            String format = Encoding.ASCII.GetString(fileData, 0, 4);
            if (!String.Equals(format, "fonT", StringComparison.InvariantCulture))
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            Int32 ideographicSpaceWidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true);
            //UInt32 dataStart = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true);
            Int32 stride = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x08, 4, true);
            Int32 fontDataHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 4, true);
            this.m_Height = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x10, 4, true);
            // Start at 0
            this.m_Width = 0;
            // count: highest encountered ID. But all IDs are +1.
            Int32 count = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x14, 4, true);
            Int32 symbolDataSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x18, 4, true);
            Int32 symbolImageSize = stride * fontDataHeight;
            Int32 hiddenDataLen = symbolDataSize - 1 - symbolImageSize;
            if (hiddenDataLen < 0)
                throw new FileTypeLoadException("Symbol size is too small to contain specified width * height!");
            bool hasPadding = hiddenDataLen >= 1;
            Int32 readOffset = 0x1C;
            List<Int32>[] symbolUsage = new List<Int32>[count];
            if (fileData.Length <= readOffset + 0x20000)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            for (Int32 i = 0; i <= 0xFFFF; ++i)
            {
                Int32 symbolIndex = (UInt16)(ArrayUtils.ReadIntFromByteArray(fileData, readOffset, 2, true)) - 1;
                if (symbolIndex >= count)
                    throw new FileTypeLoadException("Symbol index exceeds number of symbols!");
                if (symbolIndex >= 0)
                {
                    if (symbolUsage[symbolIndex] == null)
                        symbolUsage[symbolIndex] = new List<Int32>();
                    symbolUsage[symbolIndex].Add(i);
                }
                readOffset += 2;
            }
            this.m_Palette = new Color[] { Color.White, Color.Black };
            this.m_FramesList = new SupportedFileType[0x10000];
            HashSet<Int32>  emptySymbols = new HashSet<Int32>();

            for (Int32 i = 0; i < count; ++i)
            {
                Int32 symbReadOffset = readOffset;
                readOffset += symbolDataSize;
                if (readOffset > fileData.Length)
                    throw new FileTypeLoadException("File is not long enough to contain all symbols!");
                List<Int32> curSymbolUsage = symbolUsage[i];
                if (curSymbolUsage == null)
                    break;
                Byte symbolWidth = fileData[symbReadOffset++];
                // Technically the read font width is irrelevant, and thus it might be wrong.
                if (symbolWidth > m_Width && ImageUtils.GetMinimumStride(symbolWidth, 1) <= stride)
                    m_Width = symbolWidth;
                Byte[] symbolData = new Byte[symbolImageSize];
                int curSymbolOffset = symbReadOffset;
                Array.Copy(fileData, symbReadOffset, symbolData, 0, symbolImageSize);
                symbReadOffset += symbolImageSize;
                // Extract hidden data.
                Byte[] hiddenData = new byte[hiddenDataLen];
                Array.Copy(fileData, symbReadOffset, hiddenData, 0, hiddenDataLen);
                symbReadOffset += hiddenDataLen;
                Byte padding = 0;
                if (hasPadding)
                    padding = hiddenData[0];
                // This should only happen once, on the ideographic space.
                if (symbolWidth == 0)
                {
                    emptySymbols.UnionWith(curSymbolUsage);
                }
                Int32 usageCount = curSymbolUsage.Count;
                for (Int32 use = 0; use < usageCount; ++use)
                {
                    int index = curSymbolUsage[use];
                    bool isIdeoSpace = index == 0x3000;
                    int widthToUse = symbolWidth;
                    int strideToUse = stride;
                    Byte[] dataToUse = symbolData;
                    if (isIdeoSpace)
                    {
                        widthToUse = ideographicSpaceWidth;
                        strideToUse = ImageUtils.GetMinimumStride(ideographicSpaceWidth, 1);
                        int expectedSize = fontDataHeight * strideToUse;
                        if (symbolData.Length < expectedSize)
                        {
                            dataToUse = new Byte[expectedSize];
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
                                Byte[] data8Bit = ImageUtils.ConvertTo8Bit(dataToUse, widthToUse, fontDataHeight, 0, 1, true, ref strideToUse);
                                int newWidth = widthToUse + padding;
                                Byte[] newData = new Byte[newWidth * fontDataHeight];
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
                        String hiddenStr = String.Join(", ", hiddenData.Select(b => b.ToString("X2")).ToArray());
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
            for (Int32 i = 0; i <= 0xFFFF; ++i)
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
                String emptySymbolsStr = GeneralUtils.GroupNumbers(emptySymbolsArr, true);
                if (extraInfo.Length > 0)
                    extraInfo.AppendLine();
                extraInfo.Append("Empty but stored symbols: " + emptySymbolsStr);
            }
            this.ExtraInfo = extraInfo.ToString();
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 maxUsedHeight;
            this.PerformPreliminaryChecks(fileToSave, out maxUsedHeight);
            FileFramesWwBitFntUni fontFile = fileToSave as FileFramesWwBitFntUni;
            Int32 fontHeight = fontFile != null ? Math.Max(fontFile.Height, maxUsedHeight) : maxUsedHeight;
            String emptySymbolsStr = "3000-301F, 3303-33CD";
            return new Option[]
            {
                new Option("HE", OptionInputType.Number, "Font height:", maxUsedHeight + ",255", fontHeight.ToString()),
                new Option("IDR", OptionInputType.String, "Hexadecimal ranges where empty entries will use the ideographic symbol. 0x3000 is included in this automatically, even if not added.", emptySymbolsStr),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Int32 calcFontHeight;
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave, out calcFontHeight);
            Int32 fontHeight;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "HE"), out fontHeight))
                fontHeight = calcFontHeight;
            Int32 idSpaceWidth = frames.Length > 0x3000 ? frames[0x3000].Width : 0x14;
            Int32 spaceWidth = frames.Length > 0x20 ? frames[0x20].Width : 0;
            String emptySymbolsStr = Option.GetSaveOptionValue(saveOptions, "IDR");
            HashSet<Int32> emptySymbols = new HashSet<Int32>(GeneralUtils.GetRangedNumbers(emptySymbolsStr, true));
            // Always save ideographic width as real symbol.
            emptySymbols.Add(0x3000);

            Int32 imageListcount = frames.Length; // should always be 0x10000
            // This doesn't use calcFontHeight because the trimming might be able to take a few pixels off.
            Int32 fontDataHeight = 0;
            // Needs to be at least either the ideological space width or the double of the normal space width.
            Int32 fontDataWidth = Math.Max(spaceWidth * 2, idSpaceWidth);
            Byte[][] fontListBin = new Byte[0x10000][];
            Byte[] fontListStrides = new Byte[0x10000];
            for (Int32 i = 0; i < imageListcount; ++i)
            {
                SupportedFileType ffs = frames[i];
                Int32 symbWidth = ffs.Width;
                if (symbWidth == 0)
                    continue;
                if (fontDataWidth < symbWidth)
                    fontDataWidth = symbWidth;
                Int32 yoffSet = 0;
                Int32 height = ffs.Height;
                Int32 imStride;
                Byte[] imageData = ImageUtils.GetImageData(ffs.GetBitmap(), out imStride, true);
                // Image width cannot exceed 255, so 1-bit image stride can only be up to 32
                fontListStrides[i] = (Byte)imStride;
                fontListBin[i] = imageData;
                // Write as little data as possible, by trimming the bottoms of the frames.
                ImageUtils.OptimizeYHeight(imageData, imStride, ref height, ref yoffSet, true, 0, fontHeight, false);
                Int32 newHeight = height + yoffSet;
                if (newHeight > fontDataHeight)
                    fontDataHeight = newHeight;
            }
            Int32 stride = ImageUtils.GetMinimumStride(fontDataWidth, 1);
            Int32 dataLength = stride * fontDataHeight;
            //int hideData = 2;
            //Int32 blockLength = dataLength + 1 + hideData;
            Int32 blockLength = dataLength;
            Byte[] ideographData = new Byte[blockLength];
            // Make list of binary entries, skipping any with width == 0
            for (Int32 i = 0; i < imageListcount; ++i)
            {
                SupportedFileType ffs = frames[i];
                Int32 symbWidth = ffs.Width;
                Int32 symbHeight = ffs.Height;
                Byte[] imageData = fontListBin[i];
                Byte imStride = fontListStrides[i];
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
                Byte[] output = new Byte[blockLength];
                output[0] = (Byte)symbWidth;
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
            Int32[] emptyRange = emptySymbols.ToArray();
            Array.Sort(emptyRange);
            for (Int32 i = 0; i < emptyRange.Length; ++i)
            {
                int actualIndex = emptyRange[i];
                if (actualIndex < imageListcount)
                    continue;
                // For the entries beyond those handled in the previous loop.
                fontListBin[actualIndex] = ideographData;
            }
            // Optimise list by removing all duplicates, and write all entries to the index.
            // this list is an array of 2-byte Words. It's treated as simple byte array for convenience.
            Byte[] index = new Byte[0x20000];
            Int32 curNum = 0;
            for (Int32 i = 0; i < 0x10000; ++i)
            {
                Byte[] curWritesymbol = fontListBin[i];
                if (curWritesymbol == null)
                    continue;
                curNum++;
                if (curNum > 0xFFFF)
                    throw new NotSupportedException("This type can only contain 65535 (0xFFFF) different characters!");
                ArrayUtils.WriteIntToByteArray(index, i << 1, 2, true, (UInt64)curNum);
                // Find any duplicates of this symbol in the following data, set their index to the same as this one,
                // and mark them as "ignore" by setting them to null. Start at i; everything before it is already checked.
                // This means the inner loop becomes shorter as this progresses. The checked block includes the symbol width.
                for (Int32 j = i + 1; j < 0x10000; ++j)
                {
                    Byte[] curChecksymbol = fontListBin[j];
                    if (curChecksymbol == null)
                        continue;
                    Boolean isEqual = true;
                    // If the reference is the same, this is the ideograph space, and it gets an immediate pass.
                    if (curWritesymbol != curChecksymbol)
                    {
                        // Seems x.SequenceEquals(y) is about 4x as slow as a simple 'for' loop here, so I stopped using it.
                        // Since they're stride-adjusted, the arrays are all of equal length at this point anyway.
                        for (Int32 b = 0; b < blockLength; ++b)
                        {
                            if (curWritesymbol[b] == curChecksymbol[b])
                                continue;
                            isEqual = false;
                            break;
                        }
                        if (!isEqual)
                            continue;
                    }
                    // Saved as UInt16, so j needs to be doubled to get the index.
                    ArrayUtils.WriteIntToByteArray(index, j << 1, 2, true, (UInt64)curNum);
                    // Remove it from any following equal checks, to further increase speed,
                    // and to end up with a list containing only uniques.
                    fontListBin[j] = null;
                }
            }
            Byte[] outputArray = new Byte[0x1C + index.Length + curNum * blockLength];
            Array.Copy(Encoding.ASCII.GetBytes("fonT"), outputArray, 4);
            // ideographic width.
            ArrayUtils.WriteIntToByteArray(outputArray, 0x04, 4, true, (UInt64)idSpaceWidth);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x08, 4, true, (UInt64)stride);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x0C, 4, true, (UInt64)fontDataHeight);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x10, 4, true, (UInt64)fontHeight);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x14, 4, true, (UInt64)curNum);
            ArrayUtils.WriteIntToByteArray(outputArray, 0x18, 4, true, (UInt32)blockLength);
            // currently at 0x1C.
            Array.Copy(index, 0, outputArray, 0x1C, index.Length);
            Int32 curIndex = 0x1C + index.Length;
            // Go over fontListBin and write all symbols that remain in it;
            // that should be exactly and only the remaining non-duplicates.
            for (Int32 i = 0; i < fontListBin.Length; ++i)
            {
                Byte[] symbolBytes = fontListBin[i];
                if (symbolBytes == null)
                    continue;
                Array.Copy(symbolBytes, 0, outputArray, curIndex, blockLength);
                curIndex += blockLength;
            }
            return outputArray;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out Int32 height)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            if (frames == null || frames.Length == 0)
                throw new ArgumentException(ERR_NEEDS_FRAMES, "fileToSave");
            int max = UInt16.MaxValue + 1;
            if (frames.Length > UInt16.MaxValue + 1)
                throw new ArgumentException("This type can only handle up to "+ max + " frames!", "fileToSave");
            height = -1;
            Int32 nrOfFrames = frames.Length;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                // Allow without further checks.
                if (frame.GetBitmap() == null)
                    continue;
                // Actual checks.
                if (frame.BitsPerPixel != this.BitsPerPixel)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 1), "fileToSave");
                height = Math.Max(height, frame.Height);
                if (frame.Width> 255)
                    throw new ArgumentException("Frame width exceeds 255!", "fileToSave");
            }
            return frames;
        }
    }
}
