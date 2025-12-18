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

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwShpTs"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood TS Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String LongTypeName { get { return "Westwood Shape File - Tiberian Sun"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] {true}; } }

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
            // OffsetInfo / ShapeFileHeader
            const Int32 hdrSize = 0x08;
            if (fileData.Length < hdrSize)
                throw new FileTypeLoadException("Not long enough for header.");
            if (fileData[0] != 0 || fileData[1] != 0)
                throw new FileTypeLoadException("Not a TS SHP file!");
            UInt16 hdrWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            UInt16 hdrHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            UInt16 hdrFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
            if (hdrFrames == 0)
                throw new FileTypeLoadException("Not a TS SHP file");
            if (hdrWidth == 0 || hdrHeight == 0)
                throw new FileTypeLoadException("Illegal values in header!");
            const Int32 frameHdrSize = 0x18;
            if (fileData.Length < hdrSize + frameHdrSize * hdrFrames)
                throw new FileTypeLoadException("File data is not long enough for frame headers!");
            this.m_FramesList = new SupportedFileType[hdrFrames];
            this.m_Width = hdrWidth;
            this.m_Height = hdrHeight;
            Boolean[] transMask = this.TransparencyMask;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, transMask, false);
            // Frames
            Int32 curOffs = hdrSize;
            Int32 fullFrameSize = hdrWidth * hdrHeight;
            for (Int32 i = 0; i < hdrFrames; ++i)
            {
                UInt16 frmX = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x00);
                UInt16 frmY = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x02);
                UInt16 frmWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x04);
                UInt16 frmHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curOffs + 0x06);
                UInt32 frmFlags = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, curOffs + 0x08);
                Color frmColor = Color.FromArgb((Int32) (ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x0C, 3, false) | 0xFF000000));
                UInt32 frmReserved = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, curOffs + 0x10);
                UInt32 frmDataOffset = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, curOffs + 0x14);
                curOffs += frameHdrSize;
                Boolean usesRle = (frmFlags & 2) != 0;
                Boolean hasTrans = (frmFlags & 1) != 0;
                if (frmDataOffset != 0 && (frmX + frmWidth > hdrWidth || frmY + frmHeight > hdrHeight || frmReserved != 0
                                           || (usesRle && frmDataOffset + frmHeight * 2 > fileData.Length) || (!usesRle && frmDataOffset + frmWidth * frmHeight > fileData.Length)))
                    throw new FileTypeLoadException("Illegal values in frame header!");
                Byte[] fullFrame = new Byte[fullFrameSize];
                Int32 frameBytes;
                if (frmDataOffset == 0)
                    frameBytes = 0;
                else
                {
                    Byte[] frame;
                    if (usesRle)
                    {
                        Int32 frameStart = (Int32) frmDataOffset;
                        frame = WestwoodRleZero.DecompressRleZeroTs(fileData, ref frameStart, frmWidth, frmHeight);
                        frameBytes = frameStart - (Int32) frmDataOffset;
                    }
                    else
                    {
                        Int32 frameDataSize = frmWidth * frmHeight;
                        frame = new Byte[frameDataSize];
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

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 width;
            Int32 height;
            Color[] palette;
            this.PerformPreliminaryChecks(fileToSave, out width, out height, out palette);
            SupportedFileType[] frames = fileToSave.Frames;
            Int32 frameLen = frames.Length;
            Boolean evenFrames = frameLen % 2 == 0;
            Boolean hasShadow = evenFrames;
            if (hasShadow)
            {
                for (Int32 i = frameLen / 2; i < frameLen; ++i)
                {
                    Int32 stride;
                    Byte[] data = ImageUtils.GetImageData(frames[i].GetBitmap(), out stride, true);
                    if (data.Any(x => x > 1))
                    {
                        hasShadow = false;
                        break;
                    }
                }
            }
            Int32 nrOfOpts = 4;
            if (evenFrames)
                nrOfOpts++;
            Option[] opts = new Option[nrOfOpts];
            Int32 opt = 0;
            opts[opt++] = new Option("CMP", OptionInputType.Boolean, "Enable transparency compression", "1");
            opts[opt++] = new Option("TDL", OptionInputType.Boolean, "Save duplicate frames only once", "1");
            opts[opt++] = new Option("ALI", OptionInputType.Boolean, "Align to 8-byte boundaries", "0");
            //opts[opt++] = new SaveOption("REM", SaveOptionType.Boolean, "Treat as remappable when calculating average color (ignores hue of remap pixels)", "0");
            opts[opt++] = new Option("TIB", OptionInputType.Boolean, "Average color calculation: treat remap as tiberium", null, "0"); // "(treats remap as green instead of ignoring hue)", new EnableFilter("REM", false, "1"));
            if (evenFrames)
                opts[opt] = new Option("SHD", OptionInputType.Boolean, "Average color calculation: Ignore shadow frames", null, hasShadow ? "1" : "0");
            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Int32 width;
            Int32 height;
            Color[] palette;
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave, out width, out height, out palette);
            Boolean compress = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CMP"));
            Boolean trimDuplicates = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "TDL"));
            Boolean align = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "ALI"));
            //Boolean adjustForRemap = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "REM"));
            Boolean asTib = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "TIB"));
            Boolean hasShadow = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "SHD"));

            Int32 nrOfFrames = frames.Length;
            Int32 shadowLimit = nrOfFrames / 2;
            const Int32 hdrSize = 0x08;
            Byte[] header = new Byte[hdrSize];
            ArrayUtils.WriteInt16ToByteArrayLe(header, 2, width);
            ArrayUtils.WriteInt16ToByteArrayLe(header, 4, height);
            ArrayUtils.WriteInt16ToByteArrayLe(header, 6, nrOfFrames);
            const Int32 frameHdrSize = 0x18;
            Byte[] frameHeaders = new Byte[nrOfFrames * frameHdrSize];

            UInt32[] frameOffsets = new UInt32[nrOfFrames];
            Byte[][] framesDataCropped = trimDuplicates ? new Byte[nrOfFrames][] : null;
            Byte[][] framesDataCompressed = new Byte[nrOfFrames][];
            Byte[] framesDataCompressedFlags = trimDuplicates ? new Byte[nrOfFrames] : null;
            Color[] framesDataColors = trimDuplicates ? new Color[nrOfFrames] : null;

            Int32 frameHeaderOffset = 0;
            UInt32 frameDataOffset = (UInt32) (hdrSize + frameHdrSize * nrOfFrames);
            if (align)
            {
                UInt32 alignment = frameDataOffset % 8;
                if (alignment > 0)
                    frameDataOffset += 8 - alignment;
            }
            Byte[] dummy = trimDuplicates ? new Byte[0] : null;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm = frame.GetBitmap();
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                Int32 xOffset = 0;
                Int32 yOffset = 0;
                Int32 newWidth = bm.Width;
                Int32 newHeight = bm.Height;
                imageData = ImageUtils.OptimizeXWidth(imageData, ref newWidth, newHeight, ref xOffset, true, 0, 0xFFFF, true);
                imageData = ImageUtils.OptimizeYHeight(imageData, newWidth, ref newHeight, ref yOffset, true, 0, 0xFFFF, true);
                Int32 founddup = -1;
                if (trimDuplicates)
                {
                    for (Int32 j = 0; j < i; ++j)
                    {
                        Byte[] prevFrame = framesDataCropped[j];
                        if (prevFrame.Length == 0 || !prevFrame.SequenceEqual(imageData))
                            continue;
                        founddup = j;
                        break;
                    }
                    if (founddup != -1)
                        imageData = dummy;
                    framesDataCropped[i] = imageData;
                }
                Byte flags;
                UInt32 dataOffset;
                Byte[] imageDataToStore;
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
                    // get average colour, ignoring zero and compensating for remap range.
                    if (hasShadow && i >= shadowLimit)
                        col = Color.Empty;
                    //else if (!saveColors) // XCC emulating test
                    //    col = Color.FromArgb(0, 0, 7);
                    else
                        col = this.GetAverageColor(imageData, palette, asTib, asTib);
                    // compress stuff here
                    // No whitespace in image: store raw
                    if (imageData.All(b => b != 0))
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
                UInt32 dataLen = (UInt32) imageDataToStore.Length;
                if (dataLen == 0)
                    dataOffset = 0;
                frameDataOffset += dataLen;
                if (align)
                {
                    UInt32 alignment = frameDataOffset % 8;
                    if (alignment > 0)
                        frameDataOffset += 8 - alignment;
                }
                ArrayUtils.WriteInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x00, xOffset); //frmX
                ArrayUtils.WriteInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x02, yOffset); //frmY
                ArrayUtils.WriteInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x04, newWidth); //frmWidth
                ArrayUtils.WriteInt16ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x06, newHeight); //frmHeight
                ArrayUtils.WriteInt32ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x08, flags); //frmFlags
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x0C, 3, false, (UInt32)col.ToArgb()); //frmColor
                //ArrayUtils.WriteInt32ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x10, 00);  //frmReserved
                ArrayUtils.WriteInt32ToByteArrayLe(frameHeaders, frameHeaderOffset + 0x14, dataOffset); //frmDataOffset
                frameHeaderOffset += frameHdrSize;
            }
            Byte[] finalData = new Byte[frameDataOffset];
            header.CopyTo(finalData, 0);
            frameHeaders.CopyTo(finalData, hdrSize);
            for (Int32 i = 0; i < frameOffsets.Length; ++i)
                framesDataCompressed[i].CopyTo(finalData, frameOffsets[i]);
            return finalData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out Int32 width, out Int32 height, out Color[] palette)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_NO_FRAMES, "fileToSave");
            width = -1;
            height = -1;
            palette = null;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_EMPTY_FRAMES, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new ArgumentException(ERR_FRAMES_DIFF, "fileToSave");
                if (palette == null)
                    palette = frame.GetColors();
            }
            return frames;
        }

        private Color GetAverageColor(Byte[] imageData, Color[] palette, Boolean adjustForRemap, Boolean forTiberium)
        {
            Int32[] colCount = new Int32[256];
            // All pixels
            Int32 pixCount1 = 0;
            // All non-remap pixels
            Int32 pixCount2 = 0;
            // Remap colours for tiberium.
            Byte[] tibGr = {0xF8, 0xE4, 0xDC, 0xD0, 0xC4, 0xB8, 0xA8, 0x98, 0x88, 0x74, 0x64, 0x50, 0x3C, 0x28, 0x10, 0x00};
            Byte[] tibNonGr = {0x38, 0x28, 0x20, 0x18, 0x18, 0x10, 0x08, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            Boolean[] remapRange = new Boolean[0x100];
            remapRange[0] = true;
            if (adjustForRemap)
                for (Int32 i = 16; i < 32; ++i)
                    remapRange[i] = true;
            Int32 imageDataLength = imageData.Length;
            for (Int32 i = 0; i < imageDataLength; ++i)
            {
                Byte b = imageData[i];
                if (b == 0)
                    continue;
                pixCount1++;
                if (!remapRange[b])
                    pixCount2++;
                colCount[b]++;
            }
            // No colour, or this is a shadow frame.
            if (pixCount1 == 0 || pixCount1 == colCount[1])
                return Color.Empty;

            // For remap, this should give the average overall luminosity,
            // with the average hue and saturation of the non-remap pixels.
            // All pixels
            Int64 allR1 = 0;
            Int64 allG1 = 0;
            Int64 allB1 = 0;
            // All non-remap pixels
            Int64 allR2 = 0;
            Int64 allG2 = 0;
            Int64 allB2 = 0;
            for (Int32 palCol = 1; palCol < 256; ++palCol)
            {
                Color c = palette[palCol];
                Int32 amount = colCount[palCol];
                if (amount == 0)
                    continue;
                if (remapRange[palCol])
                {
                    // Remap: 'gray' values of 15 -> 255 in steps of 16.
                    if (forTiberium)
                    {
                        Int32 tibIndex = palCol - 16;
                        allR1 += tibNonGr[tibIndex] * amount;
                        allG1 += tibGr[tibIndex] * amount;
                        allB1 += tibNonGr[tibIndex] * amount;
                    }
                    else
                    {
                        Int32 grayMul = (((palCol - 15) * 16) - 1) * amount;
                        allR1 += grayMul;
                        allG1 += grayMul;
                        allB1 += grayMul;
                    }
                }
                else
                {
                    // Add to both.
                    Int32 rMul = c.R * amount;
                    Int32 gMul = c.G * amount;
                    Int32 bMul = c.B * amount;
                    allR1 += rMul;
                    allG1 += gMul;
                    allB1 += bMul;
                    allR2 += rMul;
                    allG2 += gMul;
                    allB2 += bMul;
                }
            }
            Color all = Color.FromArgb((Byte)(allR1 / pixCount1), (Byte)(allG1 / pixCount1), (Byte)(allB1 / pixCount1));
            if (pixCount2 == 0 || (adjustForRemap && forTiberium))
                return all;
            ColorHSL nonRemap = Color.FromArgb((Byte)(allR2 / pixCount2), (Byte)(allG2 / pixCount2), (Byte)(allB2 / pixCount2));
            return new ColorHSL(nonRemap.Hue, nonRemap.Saturation, ((ColorHSL)all).Luminosity);
        }

        public static void PreCheckSplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex, Boolean forCombine)
        {
            if (file == null)
                throw new ArgumentException("No source given!", "file");
            if (!file.IsFramesContainer || file.Frames.Length == 0)
                throw new ArgumentException("File contains no frames!", "file");
            Int32 frLen = file.Frames.Length;
            if ((file.FrameInputFileClass & FileClass.ImageIndexed) != 0)
                return;
            if (forCombine && frLen % 2 != 0)
                throw new ArgumentException("File does not contains an even number of frames!", "file");
            for (Int32 i = 0; i < frLen; ++i)
            {
                SupportedFileType frame = file.Frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException("Empty frames found!", "file");
                if ((frame.FileClass & FileClass.Image8Bit) == 0)
                    throw new ArgumentException("All frames need to be 8-bit paletted!", "file");
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new ArgumentException("This operation is not supported for types with empty frames!", "file");
                Int32 bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                if (bpp > 8)
                    throw new ArgumentException("Non-paletted frames found!", "file");
                Int32 colors = bm.Palette.Entries.Length;
                if (colors < sourceShadowIndex)
                    throw new ArgumentException("Not all frames have enough colors to contain the source shadow index!", "file");
                if (forCombine && colors < destShadowIndex)
                    throw new ArgumentException("Not all frames have enough colors to contain the destination shadow index!", "file");
            }
        }

        public static FileFrames SplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex)
        {
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, false);
            String folder = null;
            String name = String.Empty;
            String ext = String.Empty;
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
            Boolean[] transMask = file.TransparencyMask;
            newfile.SetTransparencyMask(transMask);
            Int32 frames = file.Frames.Length;
            SupportedFileType[] shadowFrames = new SupportedFileType[frames];
            Boolean shadowFound = false;
            Color[] palette = null;
            for (Int32 i = 0; i < frames; ++i)
            {
                SupportedFileType frame = file.Frames[i];
                Bitmap bm = frame.GetBitmap();
                if (palette == null)
                    palette = bm.Palette.Entries;
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                if (!shadowFound && imageData.Contains(sourceShadowIndex))
                    shadowFound = true;
                Byte[] imageDataShadow;
                if (!shadowFound)
                    imageDataShadow = new Byte[imageData.Length];
                else
                {
                    imageDataShadow = new Byte[imageData.Length];
                    for (Int32 y = 0; y < height; ++y)
                    {
                        Int32 offs = y * stride;
                        for (Int32 x = 0; x < width; ++x)
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
                String nameNoShadows = name + ext;
                if (folder != null)
                    nameNoShadows = Path.Combine(folder, nameNoShadows);
                FileImageFrame frameNoShadows = new FileImageFrame();
                frameNoShadows.LoadFileFrame(newfile, file, imageNoShadows, nameNoShadows, i);
                frameNoShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameNoShadows.SetFileClass(frame.FileClass);
                frameNoShadows.SetNeedsPalette(frame.NeedsPalette);
                newfile.AddFrame(frameNoShadows);

                Bitmap imageOnlyShadows = ImageUtils.BuildImage(imageDataShadow, width, height, stride, bm.PixelFormat, palette, null);
                String nameOnlyShadows = name + "_s" + ext;
                if (folder != null)
                    nameOnlyShadows = Path.Combine(folder, nameOnlyShadows);
                FileImageFrame frameOnlyShadows = new FileImageFrame();
                frameOnlyShadows.LoadFileFrame(newfile, file, imageOnlyShadows, nameOnlyShadows, i);
                frameOnlyShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameOnlyShadows.SetFileClass(frame.FileClass);
                frameOnlyShadows.SetNeedsPalette(frame.NeedsPalette);
                shadowFrames[i] = frameOnlyShadows;
            }
            for (Int32 i = 0; i < frames; ++i)
                newfile.AddFrame(shadowFrames[i]);
            return newfile;
        }

        public static FileFrames CombineShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex)
        {
            Int32 transIndex;
            Boolean[] transMask = file.TransparencyMask;
            if (transMask == null || !transMask.Any(i => i))
                transIndex = 0;
            else
            {
                transIndex = Enumerable.Repeat(0, transMask.Length).First(i => transMask[i]);
            }
            if (sourceShadowIndex == transIndex)
                throw new ArgumentOutOfRangeException("sourceShadowIndex", "Source index cannot equal transparency index!");
            if (destShadowIndex == transIndex)
                throw new ArgumentOutOfRangeException("destShadowIndex", "Destination index cannot equal transparency index!");
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, true);
            String name = String.Empty;
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
            Int32 combinedFrames = file.Frames.Length / 2;
            Color[] palette = null;
            for (Int32 i = 0; i < combinedFrames; ++i)
            {
                SupportedFileType frame = file.Frames[i];
                SupportedFileType shadowFrame = file.Frames[i + combinedFrames];
                Bitmap bm = frame.GetBitmap();
                if (palette == null)
                    palette = bm.Palette.Entries;
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);

                Bitmap shBm = shadowFrame.GetBitmap();
                Int32 shWidth = shadowFrame.Width;
                Int32 shHeight = shadowFrame.Height;
                Int32 shStride;
                Byte[] shadowData = ImageUtils.GetImageData(shBm, out shStride, true);
                // Convert to shadow-only image
                shadowData = shadowData.Select(b => (Byte)(b != sourceShadowIndex ? transIndex : destShadowIndex)).ToArray();

                Int32 finalWidth = Math.Max(width, shWidth);
                Int32 finalHeight = Math.Max(height, shHeight);
                Int32 finalstride = finalWidth;
                // Create new array, then first paste shadow and then frame data.
                Byte[] finalImageData = new Byte[finalstride * finalHeight];
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