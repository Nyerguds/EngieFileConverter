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
    public class FileFramesWwFntV3 : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return this.m_Width; } }
        public override int Height { get { return this.m_Height; } }
        protected int m_Width;
        protected int m_Height;
        public override string IdCode { get { return "WwFnt3"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood Font v3"; } }
        public override string[] FileExtensions { get { return new string[] { "fnt" }; } }
        public override string LongTypeName { get { return "Westwood Font v3 (Dune II, C&C1, RA1)"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 4; } }

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
            this.LoadFromFileData(fileData, null, false);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename, false);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath, bool forV4)
        {
            int fileLength = fileData.Length;
            if (fileLength < 0x14)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            int fileLSizeHeader = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x00);
            if (fileLSizeHeader != fileLength)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);
            byte dataFormat = fileData[0x02];
            //Byte unknown03 = fileData[0x03];
            //this.Unknown04 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x04);
            int fontDataOffsetsListOffset = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x06);
            int widthsListOffset = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x08);
            // use this for pos on TS format
            int fontDataOffset = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0A);
            int heightsListOffset = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0C);
            //UInt16 unknown0E = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0E);
            //Byte AlwaysZero = fileData[0x10];
            int length;
            bool isV4 = dataFormat == 0x02;
            if (isV4)
            {
                if (!forV4)
                    throw new FileTypeLoadException("Load type identifies as v4.");
                // "last symbol" byte 0x11 is not filled in on TS fonts, so instead, calculate it from the header offsets. Sort by offset and take the lowest two.
                int[] headerVals = new int[] { fontDataOffsetsListOffset, widthsListOffset, fontDataOffset, heightsListOffset }.OrderBy(n => n).Take(2).ToArray();
                // The difference between these two, divided by the item length in that particular list, is the amount of symbols.
                int divval = 1;
                if (headerVals[0] == fontDataOffsetsListOffset || headerVals[0] == heightsListOffset)
                    divval = 2;
                length = (headerVals[1] - headerVals[0]) / divval;
            }
            else if (dataFormat == 0x00)
            {
                if (forV4)
                    throw new FileTypeLoadException("Load type identifies as v3.");
                length = fileData[0x11] + 1; // "last symbol" byte, so actual amount is this value + 1.
            }
            else
                throw new FileTypeLoadException(String.Format("Unknown font type identifier, '{0}'.", dataFormat));
            this.m_Height = fileData[0x12];
            this.m_Width = fileData[0x13];
            if (fontDataOffsetsListOffset + length * 2 > fileLength)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            if (widthsListOffset + length > fileLength)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            if (heightsListOffset + length * 2 > fileLength)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            //FontDataOffset
            int[] fontDataOffsetsList = new int[length];
            for (int i = 0; i < length; ++i)
                fontDataOffsetsList[i] = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, fontDataOffsetsListOffset + i * 2) + (isV4 ? fontDataOffset : 0);
            List<byte> widthsList = new List<byte>();
            for (int i = 0; i < length; ++i)
            {
                byte width = fileData[widthsListOffset + i];
                if (width > this.Width)
                    throw new FileTypeLoadException(String.Format("Illegal value '{0}' in symbol widths list at entry #{1}: the value is larger than global width '{2}'.", width, i, this.Width));
                widthsList.Add(width);
            }
            List<byte> yOffsetsList = new List<byte>();
            List<byte> heightsList = new List<byte>();
            for (int i = 0; i < length; ++i)
            {
                yOffsetsList.Add(fileData[heightsListOffset + i * 2]);
                byte height = fileData[heightsListOffset + i * 2 + 1];
                if (height > this.Height)
                    throw new FileTypeLoadException(String.Format("Illegal value '{0}' in symbol heights list at entry #{1}: the value is larger than global height '{2}'.", height, i, this.Height));
                heightsList.Add(height);
            }
            this.m_FramesList = new SupportedFileType[length];
            int bitsLength = this.BitsPerPixel;
            bool[] transMask = this.TransparencyMask;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, transMask, false);
            for (int i = 0; i < length; ++i)
            {
                int start = fontDataOffsetsList[i];
                byte width = widthsList[i];
                byte height = heightsList[i];
                byte yOffset = yOffsetsList[i];
                int fullHeight = height + yOffset;
                int origStride = ImageUtils.GetMinimumStride(width, bitsLength);
                int stride = origStride;
                int frDataSize = height * origStride;
                int frFullSize8bit = width * fullHeight;
                Bitmap curFrImg = null;
                if (frFullSize8bit > 0)
                {
                    try
                    {
                        byte[] fullData8Bit;
                        byte[] data8Bit = ImageUtils.ConvertTo8Bit(fileData, width, height, start, bitsLength, false, ref stride);
                        if (yOffset == 0)
                            fullData8Bit = data8Bit;
                        else
                        {
                            fullData8Bit = new byte[frFullSize8bit];
                            ImageUtils.PasteOn8bpp(fullData8Bit, width, fullHeight, width, data8Bit, width, height, stride, new Rectangle(0, yOffset, width, height), transMask, true);
                        }
                        byte[] fullData = ImageUtils.ConvertFrom8Bit(fullData8Bit, width, fullHeight, bitsLength, true, ref stride);
                        curFrImg = ImageUtils.BuildImage(fullData, width, fullHeight, stride, isV4 ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed, this.m_Palette, null);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw new FileTypeLoadException(String.Format("Data for font entry #{0} exceeds file bounds.", i), ex);
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        throw new FileTypeLoadException(String.Format("Data for font entry #{0} exceeds file bounds.", i), ex);
                    }
                }
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetNeedsPalette(this.NeedsPalette);
                StringBuilder extraInfo = new StringBuilder();
                extraInfo.Append("Data: ").Append(frDataSize).Append(" bytes");
                if (frDataSize > 0)
                    extraInfo.Append(" @ 0x").Append(start.ToString("X"));
                extraInfo.Append("\nStored image dimensions: ").Append(width).Append("x").Append(height);
                extraInfo.Append("\nStored image Y-offset: ").Append(yOffset);
                framePic.SetExtraInfo(extraInfo.ToString());
                this.m_FramesList[i] = framePic;
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            int maxUsedWidth;
            int maxUsedHeight;
            this.PerformPreliminaryChecks(fileToSave, out maxUsedWidth, out maxUsedHeight);
            FileFramesWwFntV3 fontFile = fileToSave as FileFramesWwFntV3;
            int fontWidth = fontFile != null ? fontFile.Width : maxUsedWidth;
            int fontHeight = fontFile != null ? fontFile.Height : maxUsedHeight;
            return new Option[]
            {
                new Option("WI", OptionInputType.Number, "Font width", fontWidth +",255", fontWidth.ToString()),
                new Option("HE", OptionInputType.Number, "Font height", fontHeight +",255", fontHeight.ToString()),
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            return this.SaveV3V4Font(fileToSave, saveOptions, false);
        }

        protected byte[] SaveV3V4Font(SupportedFileType fileToSave, Option[] saveOptions, bool forV4)
        {
            int fontWidth;
            int fontHeight;
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave, out fontWidth, out fontHeight);
            //Ignore values returned from check; overwrite with save options.
            fontWidth = Int32.Parse(Option.GetSaveOptionValue(saveOptions, "WI"));
            fontHeight = Int32.Parse(Option.GetSaveOptionValue(saveOptions, "HE"));

            int imagesCount = frames.Length;
            byte[][] imageData = new byte[imagesCount][];
            byte[] widthsList = new byte[imagesCount];
            byte[] heightsList = new byte[imagesCount * 2];
            // header + UInt16 index + Byte heights
            int offsetsListOffset = 0x14;
            int widthsListOffset = offsetsListOffset + imagesCount * 2;
            int heightsListOffset = 0;
            // V4 (TS) has its Y/height list before the image data.
            if (forV4)
                heightsListOffset = widthsListOffset + imagesCount;
            int fontOffsetStart = (!forV4) ? widthsListOffset + imagesCount : heightsListOffset + imagesCount * 2;
            int bitsLength = forV4 ? 8 : 4;
            for (int i = 0; i < imagesCount; ++i)
            {
                Bitmap bm = frames[i].GetBitmap();
                int imgWidth = bm == null ? 0 : bm.Width;
                int imgHeight = bm == null ? 0 : bm.Height;
                int stride = 0;
                byte[] imgData8bit = bm == null ? new byte[0] : ImageUtils.GetImageData(bm, out stride, true);
                // Small optimization; no need to go converting the TS stuff; it doesn't change.
                if (bitsLength < 8)
                    imgData8bit = ImageUtils.ConvertTo8Bit(imgData8bit, imgWidth, imgHeight, 0, bitsLength, true, ref stride);
                // Y-optimization.
                int yOffset = 0;
                int refHeight = imgHeight;
                imgData8bit = ImageUtils.OptimizeYHeight(imgData8bit, imgWidth, ref refHeight, ref yOffset, true, 0, fontHeight, true);
                if (refHeight == 0)
                    yOffset = imgHeight;
                imgHeight = refHeight;

                if (bitsLength < 8)
                    imageData[i] = ImageUtils.ConvertFrom8Bit(imgData8bit, imgWidth, imgHeight, bitsLength, false, ref stride);
                else
                    imageData[i] = ArrayUtils.CloneArray(imgData8bit);

                //StringBuilder sb = new StringBuilder();
                //for (int y = 0; y < imgHeight; ++y)
                //{
                //    for (int x = 0; x < stride; ++x)
                //    {
                //        sb.Append(imageData[i][y * stride + x].ToString("X2"));
                //    }
                //    sb.AppendLine();
                //}
                //String icon = sb.ToString();

                widthsList[i] = (byte)imgWidth;
                heightsList[i * 2] = (byte)yOffset;
                heightsList[i * 2 + 1] = (byte)imgHeight;
            }
            int fontOffset = forV4 ? 0 : fontOffsetStart;
            byte[] fontDataOffsetsList;
            try
            {
                fontDataOffsetsList = this.CreateImageIndex(imageData, 0, false, ref fontOffset, true, true, true);
            }
            catch (OverflowException ex)
            {
                throw new ArgumentException(ex.Message, "fileToSave", ex);
            }
            // V3 (C&C/RA) has its Y/height list after the image data.
            if (!forV4)
                heightsListOffset = fontOffset;
            int fullLength = !forV4 ? (heightsListOffset + imagesCount * 2) : (fontOffset + fontOffsetStart);
            if (fullLength > ushort.MaxValue)
                throw new ArgumentException("The full font data size exceeds the maximum of " + ushort.MaxValue + " bytes supported for " + this.ShortTypeName + ".", "fileToSave");
            byte[] fullData = new byte[fullLength];

            // write header
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0, (ushort)fullLength);
            fullData[0x02] = (byte)(forV4 ? 0x02 : 0x00);       // Byte DataFormat
            fullData[0x03] = (byte)(forV4 ? 0 : 5);             // Byte Unknown03 (0x05 in EOB/C&C/RA1, 0x00 in TS)
            fullData[0x04] = 0x0e;                              // UInt16 Unknown04, low byte; (always 0x0e)
            fullData[0x05] = 0x00;                              // UInt16 Unknown04, high byte; (always 0x00)
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0x06, (ushort)offsetsListOffset);
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0x08, (ushort)widthsListOffset);
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0x0A, (ushort)fontOffsetStart);
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0x0C, (ushort)heightsListOffset);
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, 0x0E, (ushort)(forV4 ? 0 : 0x1012));
            fullData[0x10] = 0x00;                              // Byte AlwaysZero (Always 0x00)
            fullData[0x11] = (byte)(forV4 ? 0 : imagesCount - 1);  // Byte LastSymbolIndex (for non-TS)
            fullData[0x12] = (byte)fontHeight;                // Byte FontHeight
            fullData[0x13] = (byte)fontWidth;                 // Byte FontWidth
            Array.Copy(fontDataOffsetsList, 0, fullData, offsetsListOffset, fontDataOffsetsList.Length);
            Array.Copy(widthsList, 0, fullData, widthsListOffset, widthsList.Length);
            int imageDataOffs = fontOffsetStart;
            for (int i = 0; i < imagesCount; ++i)
            {
                byte[] symbolImgData = imageData[i];
                if (symbolImgData == null || symbolImgData.Length == 0)
                    continue;
                int dataLen = symbolImgData.Length;
                Array.Copy(symbolImgData, 0, fullData, imageDataOffs, dataLen);
                imageDataOffs += dataLen;
            }
            // at this point, heightsListOffset should equal imageDataOffs, and the next operation should exactly fill up the array.
            Array.Copy(heightsList, 0, fullData, heightsListOffset, heightsList.Length);
            // return data
            return fullData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out int width, out int height)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            if (frames == null || frames.Length == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            if (frames.Length > 256)
                throw new ArgumentException("Westwood Font v" + (this.BitsPerPixel == 4 ? 3 : 4) + " can only handle up to 256 frames.", "fileToSave");
            width = -1;
            height = -1;
            int nrOfFrames = frames.Length;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame.BitsPerPixel != this.BitsPerPixel)
                    throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
                width = Math.Max(width, frame.Width);
                height = Math.Max(height, frame.Height);
                if (width > 255 || height > 255)
                    throw new ArgumentException("Frame dimensions exceed 255.", "fileToSave");
            }
            return frames;
        }

        /// <summary>
        ///     Creates a 16-bit little endian index of reference addresses, starting from the given dataOffset.
        ///     After the procedure, dataOffset will have the address behind the last data to write.
        ///     If "optimize" is enabled this will remove duplicate images in the process.
        /// </summary>
        /// <param name="imageData">Image data. Duplicate arrays in this are set to 0-sized ones.</param>
        /// <param name="startIndex">Start index in the imageData array.</param>
        /// <param name="reduce">True to only start the index from the start index. False generates the full index with 0 on the empty spots.</param>
        /// <param name="dataOffset">Start offset of the addressing. Adjusted to the end offset.</param>
        /// <param name="usesNullOffset">Use 0 value for symbols with no data.</param>
        /// <param name="optimise">Optimise to remove duplicate indices.</param>
        /// <param name="unsigned">True if the Int16 values in the index are seen as unsigned.</param>
        /// <returns>The list of reference addresses, relative to the given font offset.</returns>
        protected byte[] CreateImageIndex(byte[][] imageData, int startIndex, bool reduce, ref int dataOffset, bool usesNullOffset, bool optimise, bool unsigned)
        {
            int maxValue = unsigned ? (int)ushort.MaxValue : short.MaxValue;
            int[] refslist = optimise ? this.CreateOptimizedRefsList(imageData, startIndex) : null;
            int symbols = imageData.Length;
            int writeDiff = reduce ? -startIndex : 0;
            byte[] fontDataOffsetsList = new byte[(reduce ? symbols - startIndex : symbols) * 2];

            for (int i = startIndex; i < symbols; ++i)
            {
                int replacei = optimise ? refslist[i] : i;
                if (usesNullOffset && imageData[i].Length == 0)
                {
                    // Data is null: just write 0
                    fontDataOffsetsList[(i + writeDiff) * 2] = 0;
                    fontDataOffsetsList[(i + writeDiff) * 2 + 1] = 0;
                }
                else if (replacei == i)
                {
                    if (dataOffset > maxValue)
                        throw new OverflowException("Data too large: this format cannot address data that exceeds " + maxValue + " bytes.");
                    // Data is not null and not a duplicate: write offset and advance offset ptr.
                    ArrayUtils.WriteUInt16ToByteArrayLe(fontDataOffsetsList, (i + writeDiff) * 2, (ushort)dataOffset);
                    dataOffset += imageData[i].Length;
                }
                else
                {
                    // Data is duplicate: clear data and copy previously written offset.
                    imageData[i] = new byte[0];
                    fontDataOffsetsList[(i + writeDiff) * 2] = fontDataOffsetsList[(replacei + writeDiff) * 2];
                    fontDataOffsetsList[(i + writeDiff) * 2 + 1] = fontDataOffsetsList[(replacei + writeDiff) * 2 + 1];
                }
            }
            return fontDataOffsetsList;
        }

        /// <summary>
        /// File size optimization. This function makes a map to re-map duplicate entries to the first found occurrence.
        /// In the final images array, any index not referencing itself is deemed a copy and should be removed in favour of the reference.
        /// If startindex is greater than 0, the returned references list will not be smaller; the ones before the start will simply not be processed.
        /// </summary>
        /// <param name="imageData">Image data array</param>
        /// <param name="startIndex">Start index in the array.</param>
        /// <returns></returns>
        protected int[] CreateOptimizedRefsList(byte[][] imageData, int startIndex)
        {
            int imagesCount = imageData.Length;
            int[] refsList = new int[imagesCount];
            for (int checkedEntry = startIndex; checkedEntry < imagesCount; ++checkedEntry)
            {
                for (int dupetest = startIndex; dupetest < imagesCount; ++dupetest)
                {
                    if (dupetest == checkedEntry || ArrayUtils.ArraysAreEqual(imageData[checkedEntry], imageData[dupetest]))
                    {
                        // reached the own index, or the data matches. Either way, set ref and continue with next one.
                        refsList[checkedEntry] = dupetest;
                        break;
                    }
                }
            }
            return refsList;
        }
    }

    public class FileFramesWwFntV4 : FileFramesWwFntV3
    {
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override string IdCode { get { return "WwFnt4"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood Font v4"; } }
        public override string LongTypeName { get { return "Westwood Font v4 (Tiberian Sun)"; } }
        public override int BitsPerPixel { get { return 8; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename, true);
            this.SetFileNames(filename);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            return this.SaveV3V4Font(fileToSave, saveOptions, true);
        }
    }
}
