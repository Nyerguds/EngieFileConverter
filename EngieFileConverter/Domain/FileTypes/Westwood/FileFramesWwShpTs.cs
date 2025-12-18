using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileFramesWwShpTs : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood TS Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String ShortTypeDescription { get { return "Westwood TS Shape File"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
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
            UInt16 hdrWidth = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            UInt16 hdrHeight = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            UInt16 hdrFrames = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 6, 2, true);
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
            for (Int32 i = 0; i < hdrFrames; i++)
            {
                UInt16 frmX = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x00, 2, true);
                UInt16 frmY = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x02, 2, true);
                UInt16 frmWidth = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x04, 2, true);
                UInt16 frmHeight = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x06, 2, true);
                UInt32 frmFlags = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x08, 4, true);
                Color frmColor = Color.FromArgb((Int32) (ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x0C, 3, false) | 0xFF000000));
                UInt32 frmReserved = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x10, 4, true);
                UInt32 frmDataOffset = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, curOffs + 0x14, 4, true);
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
                        new Rectangle(frmX, frmY, frmWidth, frmHeight), transMask, true);
                }
                // Convert frame data to image and frame object
                Bitmap curFrImg = ImageUtils.BuildImage(fullFrame, this.m_Width, this.m_Height, this.m_Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetColorsInPalette(this.ColorsInPalette);
                framePic.SetTransparencyMask(this.TransparencyMask);
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
                extraInfo.Append("\nData size: ").Append(frameBytes).Append(" bytes @ 0x").Append(frmDataOffset.ToString("X"));
                extraInfo.Append("\nData location: [").Append(frmX).Append(", ").Append(frmY).Append("]");
                extraInfo.Append("\nData dimensions: ").Append(frmWidth).Append("x").Append(frmHeight);
                extraInfo.Append("\nFrame colour: #").Append((frmColor.ToArgb() & 0xFFFFFF).ToString("X6"));
                framePic.SetExtraInfo(extraInfo.ToString());

                this.m_FramesList[i] = framePic;
            }
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 width;
            Int32 height;
            Color[] palette;
            PerformPreliminarychecks(ref fileToSave, out width, out height, out palette);
            Boolean adjust = palette != null && palette.Length > 0;
            if (adjust)
            {
                foreach (Color c in palette)
                {
                    if (c.R % 4 == 0 && c.G % 4 == 0 && c.B % 4 == 0)
                        continue;
                    adjust = false;
                    break;
                }
            }
            SaveOption[] opts = new SaveOption[adjust ? 3 : 2];
            opts[0] = new SaveOption("TDL", SaveOptionType.Boolean, "Trim duplicate frames", "1");
            opts[1] = new SaveOption("ALI", SaveOptionType.Boolean, "Align to 8-byte boundaries", "0");
            if (adjust)
                opts[2] = new SaveOption("AJC", SaveOptionType.Boolean, "Fix the palette's 6-bit colours to save the average frame colour.", "0");
            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 width;
            Int32 height;
            Color[] palette;
            PerformPreliminarychecks(ref fileToSave, out width, out height, out palette);
            Boolean trimDuplicates = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "TDL"));
            Boolean align = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "ALI"));
            Boolean adjustColors = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "AJC"));

            // Fix for 6-bit colour palettes: stretch the 0-252 ranges out over the full 0-255.
            if (adjustColors)
            {
                PixelFormatter sixBittoEight = new PixelFormatter(3, 0x00, 0x00003F, 0x003F00, 0x3F0000, true);
                Color[] newpal = new Color[palette.Length];
                Byte[] colArr = new Byte[3];
                for (Int32 i = 0; i < palette.Length; i++)
                {
                    Color col = palette[i];
                    colArr[0] = (Byte) (col.R / 4);
                    colArr[1] = (Byte) (col.G / 4);
                    colArr[2] = (Byte) (col.B / 4);
                    newpal[i] = sixBittoEight.GetColor(colArr, 0);
                }
                palette = newpal;
            }
            Int32 frames = fileToSave.Frames.Length;
            const Int32 hdrSize = 0x08;
            Byte[] header = new Byte[hdrSize];
            ArrayUtils.WriteIntToByteArray(header, 2, 2, true, (UInt16) width);
            ArrayUtils.WriteIntToByteArray(header, 4, 2, true, (UInt16) height);
            ArrayUtils.WriteIntToByteArray(header, 6, 2, true, (UInt16) frames);
            const Int32 frameHdrSize = 0x18;
            Byte[] frameHeaders = new Byte[frames * frameHdrSize];

            UInt32[] frameOffsets = new UInt32[frames];
            Byte[][] framesDataCropped = trimDuplicates ? new Byte[frames][] : null;
            Byte[][] framesDataCompressed = new Byte[frames][];
            Byte[] framesDataCompressedFlags = trimDuplicates ? new Byte[frames] : null;
            Color[] framesDataColors = trimDuplicates ? new Color[frames] : null;

            Int32 frameHeaderOffset = 0;
            UInt32 frameDataOffset = (UInt32) (hdrSize + frameHdrSize * frames);
            if (align && frameDataOffset % 8 > 0)
                frameDataOffset += 8 - (frameDataOffset % 8);
            Byte[] dummy = trimDuplicates ? new Byte[0] : null;
            for (Int32 i = 0; i < fileToSave.Frames.Length; i++)
            {
                SupportedFileType frame = fileToSave.Frames[i];
                Bitmap bm = frame.GetBitmap();
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride);
                Int32 xOffset = 0;
                Int32 yOffset = 0;
                Int32 newWidth = bm.Width;
                Int32 newHeight = bm.Height;
                imageData = ImageUtils.CollapseStride(imageData, newWidth, newHeight, 8, ref stride);
                imageData = ImageUtils.OptimizeXWidth(imageData, ref newWidth, newHeight, ref xOffset, true, 0, 0xFFFF, true);
                imageData = ImageUtils.OptimizeYHeight(imageData, newWidth, ref newHeight, ref yOffset, true, 0, 0xFFFF, true);
                Int32 founddup = -1;
                if (trimDuplicates)
                {
                    for (Int32 j = 0; j < i; j++)
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
                    col = this.GetAverageColor(imageData, palette);
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
                        // Collapse whitespace, check if smaller or not.
                        imageDataToStore = WestwoodRleZero.CompressRleZeroTs(imageData, newWidth, newHeight);
                        if (imageDataToStore.Length > imageData.Length)
                            imageDataToStore = imageData;
                        else flags |= 2;
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
                frameDataOffset += (UInt32) imageDataToStore.Length;
                if (align && frameDataOffset % 8 > 0)
                    frameDataOffset += 8 - (frameDataOffset % 8);
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x00, 2, true, (UInt16) xOffset); //frmX
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x02, 2, true, (UInt16) yOffset); //frmY
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x04, 2, true, (UInt16) newWidth); //frmWidth
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x06, 2, true, (UInt16) newHeight); //frmHeight
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x08, 4, true, flags); //frmFlags     
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x0C, 3, false, (UInt32) col.ToArgb()); //frmColor
                //ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x10, 4, true, 00);  //frmReserved
                ArrayUtils.WriteIntToByteArray(frameHeaders, frameHeaderOffset + 0x14, 4, true, dataOffset); //frmDataOffset
                frameHeaderOffset += frameHdrSize;
            }
            Byte[] finalData = new Byte[frameDataOffset];
            header.CopyTo(finalData, 0);
            frameHeaders.CopyTo(finalData, hdrSize);
            for (Int32 i = 0; i < frameOffsets.Length; i++)
                framesDataCompressed[i].CopyTo(finalData, frameOffsets[i]);
            return finalData;
        }

        private void PerformPreliminarychecks(ref SupportedFileType fileToSave, out Int32 width, out Int32 height, out Color[] palette)
        {
            // Preliminary checks
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");
            width = -1;
            height = -1;
            palette = null;
            foreach (SupportedFileType frame in fileToSave.Frames)
            {
                if (frame == null)
                    throw new NotSupportedException("SHP can't handle empty frames!");
                if (frame.BitsPerPixel != 8)
                    throw new NotSupportedException("Not all frames in input type are 8-bit images!");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new NotSupportedException("Not all frames in input type are the same size!");
                if (palette == null)
                    palette = frame.GetColors();
            }
        }

        private Color GetAverageColor(Byte[] imageData, Color[] palette)
        {
            Int32[] colCount = new Int32[256];
            Int32 pixCount = 0;
            foreach (Byte b in imageData)
            {
                if (b != 0)
                    pixCount++;
                colCount[b]++;
            }
            if (pixCount == 0)
                return Color.Empty;
            Int64 allR = 0;
            Int64 allG = 0;
            Int64 allB = 0;
            for (Int32 palCol = 0; palCol < 256; palCol++)
            {
                Color c = palette[palCol];
                Int32 amount = colCount[palCol];
                allR += c.R * amount;
                allG += c.G * amount;
                allB += c.B * amount;
            }
            return Color.FromArgb((Byte) (allR / pixCount), (Byte) (allG / pixCount), (Byte) (allB / pixCount));
        }

        public static void PreCheckSplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex, Boolean forCombine)
        {
            if (file == null)
                throw new NotSupportedException("No source given!");
            if (!file.IsFramesContainer || file.Frames.Length == 0)
                throw new NotSupportedException("File contains no frames!");
            Int32 frLen = file.Frames.Length;
            if ((file.FrameInputFileClass & FileClass.ImageIndexed) != 0)
                return;
            if (forCombine && frLen % 2 != 0)
                throw new NotSupportedException("File does not contains an even number of frames!");
            for (Int32 i = 0; i < frLen; i++)
            {
                SupportedFileType frame = file.Frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new NotSupportedException("Empty frames found!");
                if ((frame.FileClass & FileClass.Image8Bit) == 0)
                    throw new NotSupportedException("All frames need to be 8-bit paletted!");
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new NotSupportedException("This operation is not supported for types with empty frames!");
                Int32 bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                if (bpp > 8)
                    throw new NotSupportedException("Non-paletted frames found!");
                Int32 colors = bm.Palette.Entries.Length;
                if (colors < sourceShadowIndex)
                    throw new NotSupportedException("Not all frames have enough colours to contain the source shadow index!");
                if (forCombine && colors < destShadowIndex)
                    throw new NotSupportedException("Not all frames have enough colours to contain the destination shadow index!");
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
            FileFrames newfile = new FileFrames();
            newfile.SetCommonPalette(true);
            newfile.SetBitsPerColor(8);
            newfile.SetColorsInPalette(file.ColorsInPalette);
            Boolean[] transMask = file.TransparencyMask;
            newfile.SetTransparencyMask(transMask);
            Int32 frames = file.Frames.Length;
            SupportedFileType[] shadowFrames = new SupportedFileType[frames];
            Boolean shadowFound = false;
            Color[] palette = null;
            for (Int32 i = 0; i < frames; i++)
            {
                SupportedFileType frame = file.Frames[i];
                Bitmap bm = frame.GetBitmap();
                if (palette == null)
                    palette = bm.Palette.Entries;
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride);
                Boolean shadowInFrame = imageData.Contains(sourceShadowIndex);
                if (!shadowFound && shadowInFrame)
                    shadowFound = true;
                Byte[] imageDataShadow;
                if (!shadowFound)
                    imageDataShadow = new Byte[imageData.Length];
                else
                {
                    imageDataShadow = new Byte[imageData.Length];
                    for (Int32 y = 0; y < height; y++)
                    {
                        Int32 offs = y * stride;
                        for (Int32 x = 0; x < width; x++)
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
                frameNoShadows.SetColorsInPalette(frame.ColorsInPalette);
                frameNoShadows.SetTransparencyMask(transMask);
                newfile.AddFrame(frameNoShadows);

                Bitmap imageOnlyShadows = ImageUtils.BuildImage(imageDataShadow, width, height, stride, bm.PixelFormat, palette, null);
                String nameOnlyShadows = name + "_s" + ext;
                if (folder != null)
                    nameOnlyShadows = Path.Combine(folder, nameOnlyShadows);
                FileImageFrame frameOnlyShadows = new FileImageFrame();
                frameOnlyShadows.LoadFileFrame(newfile, file, imageOnlyShadows, nameOnlyShadows, i);
                frameOnlyShadows.SetBitsPerColor(frame.BitsPerPixel);
                frameOnlyShadows.SetFileClass(frame.FileClass);
                frameOnlyShadows.SetColorsInPalette(frame.ColorsInPalette);
                frameOnlyShadows.SetTransparencyMask(transMask);
                shadowFrames[i] = frameOnlyShadows;
            }
            foreach (SupportedFileType shadowFrame in shadowFrames)
                newfile.AddFrame(shadowFrame);
            newfile.SetColors(palette);
            return newfile;
        }

        public static FileFrames CombineShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex)
        {
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, true);
            String name = String.Empty;
            if (file.LoadedFile != null)
                name = file.LoadedFile;
            else if (file.LoadedFileName != null)
                name = file.LoadedFileName;
            FileFrames newfile = new FileFrames();
            newfile.SetFileNames(name);
            newfile.SetCommonPalette(true);
            newfile.SetBitsPerColor(8);
            newfile.SetColorsInPalette(file.ColorsInPalette);
            Boolean[] transMask = file.TransparencyMask;
            newfile.SetTransparencyMask(transMask);
            Int32 combinedFrames = file.Frames.Length / 2;
            Color[] palette = null;
            for (Int32 i = 0; i < combinedFrames; i++)
            {
                SupportedFileType frame = file.Frames[i];
                SupportedFileType shadowFrame = file.Frames[i + combinedFrames];
                Bitmap bm = frame.GetBitmap();
                if (palette == null)
                    palette = bm.Palette.Entries;
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride);

                Bitmap shBm = shadowFrame.GetBitmap();
                Int32 shWidth = shadowFrame.Width;
                Int32 shHeight = shadowFrame.Height;
                Int32 shStride;
                Byte[] shadowData = ImageUtils.GetImageData(shBm, out shStride);
                // Convert to shadow-only image
                shadowData = shadowData.Select(b => (Byte) (b != sourceShadowIndex ? 0 : destShadowIndex)).ToArray();
                ImageUtils.PasteOn8bpp(imageData, width, height, stride, shadowData, shWidth, shHeight, shStride, new Rectangle(0, 0, shWidth, shHeight), transMask, true);

                Bitmap imageCombined = ImageUtils.BuildImage(imageData, width, height, stride, bm.PixelFormat, palette, null);
                FileImageFrame frameCombined = new FileImageFrame();
                frameCombined.LoadFileFrame(newfile, file, imageCombined, name, i);
                frameCombined.SetBitsPerColor(frame.BitsPerPixel);
                frameCombined.SetFileClass(frame.FileClass);
                frameCombined.SetColorsInPalette(frame.ColorsInPalette);
                frameCombined.SetTransparencyMask(transMask);
                newfile.AddFrame(frameCombined);
            }
            newfile.SetColors(palette);
            return newfile;
        }

    }
}