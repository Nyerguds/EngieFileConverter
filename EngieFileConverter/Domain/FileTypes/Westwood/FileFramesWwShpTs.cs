using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwShpTs : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return this.m_Width; } }
        public override int Height { get { return this.m_Height; } }
        protected int m_Width;
        protected int m_Height;
        public override string IdCode { get { return "WwShpTs"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood TS Shape"; } }
        public override string[] FileExtensions { get { return new string[] { "shp" }; } }
        public override string LongTypeName { get { return "Westwood Shape File - Tiberian Sun"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return new bool[] {true}; } }

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
            // OffsetInfo / ShapeFileHeader
            const int hdrSize = 0x08;
            if (fileData.Length < hdrSize)
                throw new FileTypeLoadException("Not long enough for header.");
            if (fileData[0] != 0 || fileData[1] != 0)
                throw new FileTypeLoadException("Not a TS SHP file.");
            ushort hdrWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            ushort hdrHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            ushort hdrFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
            if (hdrFrames == 0)
                throw new FileTypeLoadException("Not a TS SHP file");
            if (hdrWidth == 0 || hdrHeight == 0)
                throw new FileTypeLoadException("Illegal values in header.");
            const int frameHdrSize = 0x18;
            if (fileData.Length < hdrSize + frameHdrSize * hdrFrames)
                throw new FileTypeLoadException("File data is not long enough for frame headers.");
            this.m_FramesList = new SupportedFileType[hdrFrames];
            this.m_Width = hdrWidth;
            this.m_Height = hdrHeight;
            bool[] transMask = this.TransparencyMask;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, transMask, false);
            // Frames
            int curOffs = hdrSize;
            int fullFrameSize = hdrWidth * hdrHeight;
            for (int i = 0; i < hdrFrames; ++i)
            {
                ushort frmX = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x00);
                ushort frmY = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x02);
                ushort frmWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x04);
                ushort frmHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x06);
                uint frmFlags = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, curOffs + 0x08);
                Color frmColor = Color.FromArgb((int) (ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x0C, 3, false) | 0xFF000000));
                uint frmReserved = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, curOffs + 0x10);
                uint frmDataOffset = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, curOffs + 0x14);
                curOffs += frameHdrSize;
                bool usesRle = (frmFlags & 2) != 0;
                bool hasTrans = (frmFlags & 1) != 0;
                if (frmDataOffset != 0 && (frmX + frmWidth > hdrWidth || frmY + frmHeight > hdrHeight || frmReserved != 0
                                           || (usesRle && frmDataOffset + frmHeight * 2 > fileData.Length) || (!usesRle && frmDataOffset + frmWidth * frmHeight > fileData.Length)))
                    throw new FileTypeLoadException("Illegal values in frame header.");
                byte[] fullFrame = new byte[fullFrameSize];
                int frameBytes;
                if (frmDataOffset == 0)
                    frameBytes = 0;
                else
                {
                    byte[] frame;
                    if (usesRle)
                    {
                        int frameStart = (int) frmDataOffset;
                        try
                        {
                            frame = WestwoodRleZero.DecompressRleZeroTs(fileData, ref frameStart, frmWidth, frmHeight);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR + " (frame {1})", GeneralUtils.RecoverArgExceptionMessage(ex, true), i), ex);
                        }
                        frameBytes = frameStart - (int) frmDataOffset;
                    }
                    else
                    {
                        int frameDataSize = frmWidth * frmHeight;
                        frame = new byte[frameDataSize];
                        Array.Copy(fileData, frmDataOffset, frame, 0, frameDataSize);
                        frameBytes = frameDataSize;
                    }
                    ImageUtils.PasteOn8bpp(fullFrame, hdrWidth, hdrHeight, hdrWidth,
                        frame, frmWidth, frmHeight, frmWidth,
                        new Rectangle(frmX, frmY, frmWidth, frmHeight), null, true);
                }
                // Convert frame data to image and frame object
                Bitmap curFrImg = ImageUtils.BuildImage(fullFrame, this.m_Width, this.m_Height, this.m_Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetNeedsPalette(this.NeedsPalette);
                StringBuilder extraInfo = new StringBuilder("Blit flags: ");
                extraInfo.Append(Convert.ToString(frmFlags & 0xFF, 2).PadLeft(8, '0')).Append(" (");
                if (hasTrans)
                {
                    extraInfo.Append("Transparency");
                    if (usesRle)
                        extraInfo.Append(", ");
                }
                if (usesRle)
                    extraInfo.Append("RLE-Zero");
                if (!hasTrans && !usesRle)
                    extraInfo.Append("Opaque data");
                extraInfo.Append(")");
                extraInfo.Append("\nData: ").Append(frameBytes).Append(" bytes @ 0x").Append(frmDataOffset.ToString("X"));
                extraInfo.Append("\nStored image dimensions: ").Append(frmWidth).Append("x").Append(frmHeight);
                extraInfo.Append("\nStored image position: [").Append(frmX).Append(", ").Append(frmY).Append("]");
                extraInfo.Append("\nFrame color: #").Append((frmColor.ToArgb() & 0xFFFFFF).ToString("X6"));
                framePic.SetExtraInfo(extraInfo.ToString());
                this.m_FramesList[i] = framePic;
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            int width;
            int height;
            Color[] palette;
            this.PerformPreliminaryChecks(fileToSave, out width, out height, out palette);
            SupportedFileType[] frames = fileToSave.Frames;
            int frameLen = frames.Length;
            bool evenFrames = frameLen % 2 == 0;
            bool hasShadow = evenFrames;
            if (hasShadow)
            {
                for (int i = frameLen / 2; i < frameLen; ++i)
                {
                    int stride;
                    byte[] data = ImageUtils.GetImageData(frames[i].GetBitmap(), out stride, true);
                    if (data.Any(x => x > 1))
                    {
                        hasShadow = false;
                        break;
                    }
                }
            }
            int nrOfOpts = 4;
            if (evenFrames)
                nrOfOpts++;
            Option[] opts = new Option[nrOfOpts];
            int opt = 0;
            opts[opt++] = new Option("CMP", OptionInputType.Boolean, "Enable transparency compression", "1");
            opts[opt++] = new Option("TDL", OptionInputType.Boolean, "Save duplicate frames only once", "1");
            opts[opt++] = new Option("ALI", OptionInputType.Boolean, "Align to 8-byte boundaries", "0");
            //opts[opt++] = new SaveOption("REM", SaveOptionType.Boolean, "Treat as remappable when calculating average color (ignores hue of remap pixels)", "0");
            opts[opt++] = new Option("TIB", OptionInputType.Boolean, "Average color calculation: treat remap as tiberium", null, "0"); // "(treats remap as green instead of ignoring hue)", new EnableFilter("REM", false, "1"));
            if (evenFrames)
                opts[opt] = new Option("SHD", OptionInputType.Boolean, "Average color calculation: Ignore shadow frames", null, hasShadow ? "1" : "0");
            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            int width;
            int height;
            Color[] palette;
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave, out width, out height, out palette);
            bool compress = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CMP"));
            bool trimDuplicates = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "TDL"));
            bool align = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "ALI"));
            //Boolean adjustForRemap = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "REM"));
            bool asTib = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "TIB"));
            bool hasShadow = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "SHD"));

            int nrOfFrames = frames.Length;
            int shadowLimit = nrOfFrames / 2;
            const int hdrSize = 0x08;
            byte[] header = new byte[hdrSize];
            ArrayUtils.WriteUInt16ToByteArrayLe(header, 2, (ushort)width);
            ArrayUtils.WriteUInt16ToByteArrayLe(header, 4, (ushort)height);
            ArrayUtils.WriteUInt16ToByteArrayLe(header, 6, (ushort)nrOfFrames);
            const int frameHdrSize = 0x18;
            byte[] frameHeaders = new byte[nrOfFrames * frameHdrSize];

            uint[] frameOffsets = new uint[nrOfFrames];
            byte[][] framesDataCropped = trimDuplicates ? new byte[nrOfFrames][] : null;
            byte[][] framesDataCompressed = new byte[nrOfFrames][];
            byte[] framesDataCompressedFlags = trimDuplicates ? new byte[nrOfFrames] : null;
            Color[] framesDataColors = trimDuplicates ? new Color[nrOfFrames] : null;

            int frameHeaderOffset = 0;
            uint frameDataOffset = (uint) (hdrSize + frameHdrSize * nrOfFrames);
            if (align)
            {
                uint alignment = frameDataOffset % 8;
                if (alignment > 0)
                    frameDataOffset += 8 - alignment;
            }
            byte[] dummy = trimDuplicates ? new byte[0] : null;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm = frame.GetBitmap();
                int stride;
                byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                int xOffset = 0;
                int yOffset = 0;
                int newWidth = bm.Width;
                int newHeight = bm.Height;
                imageData = ImageUtils.OptimizeXWidth(imageData, ref newWidth, newHeight, ref xOffset, true, 0, 0xFFFF, true);
                imageData = ImageUtils.OptimizeYHeight(imageData, newWidth, ref newHeight, ref yOffset, true, 0, 0xFFFF, true);
                int founddup = -1;
                if (trimDuplicates)
                {
                    for (int j = 0; j < i; ++j)
                    {
                        byte[] prevFrame = framesDataCropped[j];
                        if (prevFrame.Length == 0 || !ArrayUtils.ArraysAreEqual(prevFrame,imageData))
                            continue;
                        founddup = j;
                        break;
                    }
                    if (founddup != -1)
                        imageData = dummy;
                    framesDataCropped[i] = imageData;
                }
                byte flags;
                uint dataOffset;
                byte[] imageDataToStore;
                Color col;
                if (trimDuplicates && founddup != -1)
                {
                    imageDataToStore = imageData;
                    framesDataCompressed[i] = imageDataToStore;
                    flags = framesDataCompressedFlags[founddup];
                    dataOffset = frameOffsets[founddup];
                    col = framesDataColors[founddup];
                }
                else
                {
                    // get average color, ignoring zero and compensating for remap range.
                    if (hasShadow && i >= shadowLimit)
                        col = Color.Empty;
                    //else if (!saveColors) // XCC emulating test
                    //    col = Color.FromArgb(0, 0, 7);
                    else
                        col = this.GetAverageColor(imageData, palette, asTib, asTib);
                    // compress stuff here
                    // No whitespace in image: store raw
                    bool hasBlank = false;
                    for (int bl = 0; bl < imageData.Length; ++bl)
                    {
                        if (imageData[i] != 0) continue;
                        hasBlank = true;
                        break;
                    }
                    if (!hasBlank)
                    {
                        flags = 0x00;
                        imageDataToStore = imageData;
                    }
                    else
                    {
                        flags = 0x01;
                        if (!compress)
                            imageDataToStore = imageData;
                        else
                        {
                            // Collapse whitespace, check if smaller or not.
                            imageDataToStore = WestwoodRleZero.CompressRleZeroTs(imageData, newWidth, newHeight);
                            if (imageDataToStore.Length >= imageData.Length)
                                imageDataToStore = imageData;
                            else
                                flags |= 2;
                        }
                    }
                    frameOffsets[i] = frameDataOffset;
                    framesDataCompressed[i] = imageDataToStore;
                    if (trimDuplicates)
                    {
                        framesDataCompressedFlags[i] = flags;
                        framesDataColors[i] = col;
                    }
                    dataOffset = frameDataOffset;
                }
                uint dataLen = (uint) imageDataToStore.Length;
                if (dataLen == 0)
                    dataOffset = 0;
                frameDataOffset += dataLen;
                if (align)
                {
                    uint alignment = frameDataOffset % 8;
                    if (alignment > 0)
                        frameDataOffset += 8 - alignment;
                }
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x00, (ushort)xOffset); //frmX
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x02, (ushort)yOffset); //frmY
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x04, (ushort)newWidth); //frmWidth
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x06, (ushort)newHeight); //frmHeight
                ArrayUtils.WriteUInt32ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x08, flags); //frmFlags
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x0C, 3, false, (uint)col.ToArgb()); //frmColor
                //ArrayUtils.WriteUInt32ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x10, 00);  //frmReserved
                ArrayUtils.WriteUInt32ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x14, dataOffset); //frmDataOffset
                frameHeaderOffset += frameHdrSize;
            }
            byte[] finalData = new byte[frameDataOffset];
            header.CopyTo(finalData, 0);
            frameHeaders.CopyTo(finalData, hdrSize);
            for (int i = 0; i < frameOffsets.Length; ++i)
                framesDataCompressed[i].CopyTo(finalData, frameOffsets[i]);
            return finalData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out int width, out int height, out Color[] palette)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            width = -1;
            height = -1;
            palette = null;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_FRAMES_EMPTY, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new ArgumentException(ERR_FRAMES_SIZE_DIFF, "fileToSave");
                if (palette == null)
                    palette = frame.GetColors();
            }
            return frames;
        }

        private Color GetAverageColor(byte[] imageData, Color[] palette, bool adjustForRemap, bool forTiberium)
        {
            int[] colCount = new int[256];
            // All pixels
            int pixCount1 = 0;
            // All non-remap pixels
            int pixCount2 = 0;
            // Remap colors for tiberium.
            byte[] tibGreen = {0xF8, 0xE4, 0xDC, 0xD0, 0xC4, 0xB8, 0xA8, 0x98, 0x88, 0x74, 0x64, 0x50, 0x3C, 0x28, 0x10, 0x00};
            byte[] tibRedBl = {0x38, 0x28, 0x20, 0x18, 0x18, 0x10, 0x08, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            int[] remapIndex = new int[0x100];
            // Grayscale version of row at index 0x40 on the TS palette.
            byte[] grayRange = { 0xCA, 0xBE, 0xB2, 0xA6, 0x9A, 0x8E, 0x82, 0x75, 0x69, 0x5D, 0x51, 0x45, 0x39, 0x2D, 0x20, 0x14 };
            if (adjustForRemap)
            {
                int remapnr = 0;
                for (int i = 0; i > 256; ++i)
                {
                    remapIndex[i] = i >= 16 || i < 32 ? remapnr++ : -1;
                }
            }
            int imageDataLength = imageData.Length;
            for (int i = 0; i < imageDataLength; ++i)
            {
                byte b = imageData[i];
                if (b == 0)
                    continue;
                pixCount1++;
                if (remapIndex[b] >= 0)
                    pixCount2++;
                colCount[b]++;
            }
            // No color, or this is a shadow frame.
            if (pixCount1 == 0 || pixCount1 == colCount[1])
                return Color.Empty;

            // For remap, this should give the average overall luminosity,
            // with the average hue and saturation of the non-remap pixels.
            // All pixels
            long allR1 = 0;
            long allG1 = 0;
            long allB1 = 0;
            // All non-remap pixels
            long allR2 = 0;
            long allG2 = 0;
            long allB2 = 0;
            for (int palCol = 1; palCol < 256; ++palCol)
            {
                Color c = palette[palCol];
                int amount = colCount[palCol];
                if (amount == 0)
                    continue;
                if (remapIndex[palCol] != -1)
                {
                    // Remap: 'gray' values of 15 -> 255 in steps of 16.
                    int remap = remapIndex[palCol];
                    if (forTiberium)
                    {
                        allR1 += tibRedBl[remap] * amount;
                        allG1 += tibGreen[remap] * amount;
                        allB1 += tibRedBl[remap] * amount;
                    }
                    else
                    {
                        int grayMul = grayRange[remap] * amount;
                        allR1 += grayMul;
                        allG1 += grayMul;
                        allB1 += grayMul;
                    }
                }
                else
                {
                    // Add to both.
                    int rMul = c.R * amount;
                    int gMul = c.G * amount;
                    int bMul = c.B * amount;
                    allR1 += rMul;
                    allG1 += gMul;
                    allB1 += bMul;
                    allR2 += rMul;
                    allG2 += gMul;
                    allB2 += bMul;
                }
            }
            Color all = Color.FromArgb((byte)(allR1 / pixCount1), (byte)(allG1 / pixCount1), (byte)(allB1 / pixCount1));
            if (pixCount2 == 0 || (adjustForRemap && forTiberium))
                return all;
            Color nonRemap = Color.FromArgb((byte)(allR2 / pixCount2), (byte)(allG2 / pixCount2), (byte)(allB2 / pixCount2));
            return new ColorHSL(nonRemap.GetHue(), nonRemap.GetSaturation(), all.GetBrightness());
        }

        public static void PreCheckSplitShadows(SupportedFileType file, byte sourceShadowIndex, byte destShadowIndex, bool forCombine)
        {
            if (file == null)
                throw new ArgumentException("No source given.", "file");
            if (!file.IsFramesContainer || file.Frames.Length == 0)
                throw new ArgumentException("File contains no frames.", "file");
            int frLen = file.Frames.Length;
            if ((file.FrameInputFileClass & FileClass.ImageIndexed) != 0)
                return;
            if (forCombine && frLen % 2 != 0)
                throw new ArgumentException("File does not contains an even number of frames.", "file");
            for (int i = 0; i < frLen; ++i)
            {
                SupportedFileType frame = file.Frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException("Empty frames found.", "file");
                if ((frame.FileClass & FileClass.Image8Bit) == 0)
                    throw new ArgumentException("All frames need to be 8-bit paletted.", "file");
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new ArgumentException("This operation is not supported for types with empty frames.", "file");
                int bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                if (bpp > 8)
                    throw new ArgumentException("Non-paletted frames found.", "file");
                int colors = bm.Palette.Entries.Length;
                if (colors < sourceShadowIndex)
                    throw new ArgumentException("Not all frames have enough colors to contain the source shadow index.", "file");
                if (forCombine && colors < destShadowIndex)
                    throw new ArgumentException("Not all frames have enough colors to contain the destination shadow index.", "file");
            }
        }

        public static FileFrames SplitShadows(SupportedFileType file, byte sourceShadowIndex, byte destShadowIndex)
        {
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, false);
            string folder = null;
            string name = String.Empty;
            string ext = String.Empty;
            if (file.LoadedFile != null)
            {
                name = Path.GetFileNameWithoutExtension(file.LoadedFile);
                ext = Path.GetExtension(file.LoadedFile);
                folder = Path.GetDirectoryName(file.LoadedFile);
            }
            else if (file.LoadedFileName != null)
            {
                name = Path.GetFileNameWithoutExtension(file.LoadedFileName);
                ext = Path.GetExtension(file.LoadedFileName);
            }
            FileFrames newfile = new FileFrames(file);
            newfile.SetCommonPalette(true);
            newfile.SetBitsPerPixel(8);
            newfile.SetNeedsPalette(file.NeedsPalette);
            bool[] transMask = file.TransparencyMask;
            newfile.SetTransparencyMask(transMask);
            int frames = file.Frames.Length;
            SupportedFileType[] shadowFrames = new SupportedFileType[frames];
            bool shadowFound = false;
            Color[] palette = null;
            for (int i = 0; i < frames; ++i)
            {
                SupportedFileType frame = file.Frames[i];
                Bitmap bm = frame.GetBitmap();
                if (palette == null)
                    palette = bm.Palette.Entries;
                int width = frame.Width;
                int height = frame.Height;
                int stride;
                byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                if (!shadowFound && imageData.Contains(sourceShadowIndex))
                    shadowFound = true;
                byte[] imageDataShadow;
                if (!shadowFound)
                    imageDataShadow = new byte[imageData.Length];
                else
                {
                    imageDataShadow = new byte[imageData.Length];
                    for (int y = 0; y < height; ++y)
                    {
                        int offs = y * stride;
                        for (int x = 0; x < width; ++x)
                        {
                            if (imageData[offs] == sourceShadowIndex)
                            {
                                imageData[offs] = 0;
                                imageDataShadow[offs] = destShadowIndex;
                            }
                            offs++;
                        }
                    }
                }
                Bitmap imageNoShadows = ImageUtils.BuildImage(imageData, width, height, stride, bm.PixelFormat, palette, null);
                string nameNoShadows = name + ext;
                if (folder != null)
                    nameNoShadows = Path.Combine(folder, nameNoShadows);
                FileImageFrame frameNoShadows = new FileImageFrame();
                frameNoShadows.LoadFileFrame(newfile, file, imageNoShadows, nameNoShadows, i);
                frameNoShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameNoShadows.SetFileClass(frame.FileClass);
                frameNoShadows.SetNeedsPalette(frame.NeedsPalette);
                newfile.AddFrame(frameNoShadows);

                Bitmap imageOnlyShadows = ImageUtils.BuildImage(imageDataShadow, width, height, stride, bm.PixelFormat, palette, null);
                string nameOnlyShadows = name + "_s" + ext;
                if (folder != null)
                    nameOnlyShadows = Path.Combine(folder, nameOnlyShadows);
                FileImageFrame frameOnlyShadows = new FileImageFrame();
                frameOnlyShadows.LoadFileFrame(newfile, file, imageOnlyShadows, nameOnlyShadows, i);
                frameOnlyShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameOnlyShadows.SetFileClass(frame.FileClass);
                frameOnlyShadows.SetNeedsPalette(frame.NeedsPalette);
                shadowFrames[i] = frameOnlyShadows;
            }
            for (int i = 0; i < frames; ++i)
                newfile.AddFrame(shadowFrames[i]);
            return newfile;
        }

        public static FileFrames CombineShadows(SupportedFileType file, byte sourceShadowIndex, byte destShadowIndex)
        {
            int transIndex;
            bool[] transMask = file.TransparencyMask;
            if (transMask == null || !transMask.Any(i => i))
                transIndex = 0;
            else
            {
                transIndex = Enumerable.Repeat(0, transMask.Length).First(i => transMask[i]);
            }
            if (sourceShadowIndex == transIndex)
                throw new ArgumentOutOfRangeException("sourceShadowIndex", "Source index cannot equal transparency index.");
            if (destShadowIndex == transIndex)
                throw new ArgumentOutOfRangeException("destShadowIndex", "Destination index cannot equal transparency index.");
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, true);
            string name = String.Empty;
            if (file.LoadedFile != null)
                name = file.LoadedFile;
            else if (file.LoadedFileName != null)
                name = file.LoadedFileName;
            FileFrames newfile = new FileFrames(file);
            newfile.SetFileNames(name);
            newfile.SetCommonPalette(true);
            newfile.SetBitsPerPixel(8);
            newfile.SetNeedsPalette(file.NeedsPalette);
            newfile.SetTransparencyMask(transMask);
            int combinedFrames = file.Frames.Length / 2;
            Color[] palette = null;
            for (int i = 0; i < combinedFrames; ++i)
            {
                SupportedFileType frame = file.Frames[i];
                SupportedFileType shadowFrame = file.Frames[i + combinedFrames];
                Bitmap bm = frame.GetBitmap();
                if (palette == null)
                    palette = bm.Palette.Entries;
                int width = frame.Width;
                int height = frame.Height;
                int stride;
                byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);

                Bitmap shBm = shadowFrame.GetBitmap();
                int shWidth = shadowFrame.Width;
                int shHeight = shadowFrame.Height;
                int shStride;
                byte[] shadowData = ImageUtils.GetImageData(shBm, out shStride, true);
                // Convert to shadow-only image
                shadowData = shadowData.Select(b => (byte)(b != sourceShadowIndex ? transIndex : destShadowIndex)).ToArray();

                int finalWidth = Math.Max(width, shWidth);
                int finalHeight = Math.Max(height, shHeight);
                int finalstride = finalWidth;
                // Create new array, then first paste shadow and then frame data.
                byte[] finalImageData = new byte[finalstride * finalHeight];
                ImageUtils.PasteOn8bpp(finalImageData, finalWidth, finalHeight, finalstride, shadowData, shWidth, shHeight, shStride, new Rectangle(0, 0, shWidth, shHeight), transMask, true);
                ImageUtils.PasteOn8bpp(finalImageData, finalWidth, finalHeight, finalstride, imageData, width, height, stride, new Rectangle(0, 0, width, height), transMask, true);

                Bitmap imageCombined = ImageUtils.BuildImage(finalImageData, finalWidth, finalHeight, finalstride, bm.PixelFormat, palette, null);
                FileImageFrame frameCombined = new FileImageFrame();
                frameCombined.LoadFileFrame(newfile, file, imageCombined, name, i);
                frameCombined.SetBitsPerColor(frame.BitsPerPixel);
                frameCombined.SetFileClass(frame.FileClass);
                frameCombined.SetNeedsPalette(frame.NeedsPalette);
                newfile.AddFrame(frameCombined);
            }
            newfile.SetColors(palette);
            return newfile;
        }

    }
}