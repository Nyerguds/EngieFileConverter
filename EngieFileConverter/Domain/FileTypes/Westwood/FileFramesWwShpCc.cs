using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwShpCc : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwShpCc"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood C&C1 Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String ShortTypeDescription { get { return "Westwood C&C1 Shape File"; } }
        public override Int32 ColorsInPalette { get { return this.m_HasPalette ? 0x100 : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_HasPalette;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

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
            Int32 hdrSize = 0x0E;
            if (fileData.Length < hdrSize)
                throw new FileTypeLoadException("File is not long enough for header.");
            UInt16 hdrFrames = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            UInt16 hdrXPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            UInt16 hdrYPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            UInt16 hdrWidth = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 6, 2, true);
            UInt16 hdrHeight = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            //UInt16 hdrDeltaSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x0A, 2, true);
            UInt16 hdrFlags = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 2, true);
            if (hdrFrames == 0) // Can be TS SHP; it identifies with an empty first byte IIRC.
                throw new FileTypeLoadException("Not a C&C1/RA1 SHP file!");
            if (hdrWidth == 0 || hdrHeight == 0)
                throw new FileTypeLoadException("Illegal values in header!");
            this.m_HasPalette = (hdrFlags & 1) != 0;
            //Int32 palSize = m_HasPalette ? 0x300 : 0;
            Dictionary<Int32, Int32> offsetIndices = new Dictionary<Int32, Int32>();
            Int32 offsSize = 8;
            Int32 fileSizeOffs = hdrSize + offsSize * (hdrFrames + 1);
            if (fileData.Length < hdrSize + offsSize * (hdrFrames + 2))
                throw new FileTypeLoadException("File is not long enough to read the entire frames header!");

            Int32 fileSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, fileSizeOffs, 3, true);
            Boolean hasLoopFrame;
            if (fileSize != 0)
            {
                hasLoopFrame = true;
                hdrFrames++;
            }
            else
            {
                hasLoopFrame = false;
                fileSizeOffs -= offsSize;
                fileSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, fileSizeOffs, 3, true);
            }
            Byte[][] frames = new Byte[hdrFrames][];
            OffsetInfo[] offsets = new OffsetInfo[hdrFrames];
            if (fileData.Length != fileSize)
                throw new FileTypeLoadException("File size does not match size value in header!");
            // Read palette if flag enabled. No games I know support using it, but, might as well be complete.
            if (m_HasPalette)
            {
                try
                {
                    this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(fileData, hdrSize + offsSize * (hdrFrames + 2)));
                    PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, this.TransparencyMask);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException("Illegal values in embedded palette.", argex);
                }
            }
            else
            {
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            }
            List<CcShpFrameFormat> frameFormats = Enum.GetValues(typeof(CcShpFrameFormat)).Cast<CcShpFrameFormat>().ToList();
            this.m_FramesList = new SupportedFileType[hasLoopFrame ? hdrFrames - 1 : hdrFrames];
            this.m_Width = hdrWidth;
            this.m_Height = hdrHeight;
            if (hdrXPos != 0 || hdrYPos != 0)
            {
                this.ExtraInfo = "Image position (unused): [" + hdrXPos + ", " + hdrYPos + "]";
            }
            // Frames decompression
            Int32 curOffs = hdrSize;
            Int32 frameSize = hdrWidth * hdrHeight;
            // Read is always safe; we already checked that the header size is inside the file bounds.
            OffsetInfo currentFrame = OffsetInfo.Read(fileData, curOffs);
            if (currentFrame.DataFormat != CcShpFrameFormat.Lcw)
                throw new FileTypeLoadException("Cannot parse SHP frame info: iinvalid type on first frame.");
            Int32 lastKeyFrameNr = 0;
            OffsetInfo lastKeyFrame = currentFrame;
            Int32 frameOffs = currentFrame.DataOffset;
            for (Int32 i = 0; i < hdrFrames; ++i)
            {
                Int32 realIndex = -1;
                if (!offsetIndices.ContainsKey(currentFrame.DataOffset))
                    offsetIndices.Add(currentFrame.DataOffset, i);
                else
                    offsetIndices.TryGetValue(currentFrame.DataOffset, out realIndex);
                offsets[i] = currentFrame;
                curOffs += offsSize;
                OffsetInfo nextFrame = OffsetInfo.Read(fileData, curOffs);
                if (!frameFormats.Contains(nextFrame.DataFormat) || !frameFormats.Contains(nextFrame.ReferenceFormat))
                    throw new FileTypeLoadException("Cannot parse SHP frame info: invalid frame format.");
                Int32 frameOffsEnd = nextFrame.DataOffset;
                Int32 frameStart = frameOffs;
                Int32 frameEnd;
                CcShpFrameFormat frameOffsFormat = currentFrame.DataFormat;
                //Int32 dataLen = frameOffsEnd - frameOffs;
                if (frameOffs > fileData.Length || frameOffsEnd > fileData.Length)
                    throw new FileTypeLoadException("File is too small to contain all frame data!");
                Byte[] frame = new Byte[frameSize];
                Int32 refIndex = -1;
                Int32 refIndex20 = -1;
                switch (frameOffsFormat)
                {
                    case CcShpFrameFormat.Lcw:
                        // Actual LCW-compressed frame data.
                        WWCompression.LcwDecompress(fileData, ref frameOffs, frame, 0);
                        frameEnd = realIndex >= 0 ? frameStart : frameOffs;
                        lastKeyFrame = currentFrame;
                        lastKeyFrameNr = i;
                        break;
                    case CcShpFrameFormat.XorChain:
                        // 0x20 = XOR with previous frame. Only used for chaining to previous XOR frames.
                        // Don't actually need this, but I do the integrity checks:
                        refIndex20 = currentFrame.ReferenceOffset;
                        CcShpFrameFormat refFormat = currentFrame.ReferenceFormat;
                        if (i < 2 || refFormat != CcShpFrameFormat.XorChainRef || refIndex20 >= i || offsets[refIndex20].DataFormat != CcShpFrameFormat.XorBase
                            || (offsets[i - 1].DataFormat != CcShpFrameFormat.XorBase && offsets[i - 1].DataFormat != CcShpFrameFormat.XorChain))
                            throw new FileTypeLoadException("Bad frame reference information for frame " + i + ".");
                        frames[i - 1].CopyTo(frame, 0);
                        WWCompression.ApplyXorDelta(frame, fileData, ref frameOffs, 0);
                        frameEnd = frameOffs;
                        break;
                    case CcShpFrameFormat.XorBase:
                        if (currentFrame.ReferenceFormat != CcShpFrameFormat.Lcw)
                            throw new FileTypeLoadException("XOR base frames can only reference LCW frames!");
                        // 0x40 = XOR with a previous frame. Could technically reference anything, but normally only references the last LCW "keyframe".
                        // This load method ignores the format saved in ReferenceFormat since the decompressed frame is stored already.
                        if (lastKeyFrame.DataOffset == currentFrame.ReferenceOffset)
                            refIndex = lastKeyFrameNr;
                        else if (!offsetIndices.TryGetValue(currentFrame.ReferenceOffset, out refIndex))
                        {
                            // not found as referenced frame, but in the file anyway?? Whatever; if it's LCW, just read it.
                            Int32 readOffs = currentFrame.ReferenceOffset;
                            if (readOffs >= fileData.Length)
                                throw new FileTypeLoadException("File is too small to contain all frame data!");
                            WWCompression.LcwDecompress(fileData, ref readOffs, frame, 0);
                            refIndex = -1;
                        }
                        if (refIndex >= i)
                            throw new FileTypeLoadException("XOR cannot reference later frames!");
                        if (refIndex >= 0)
                            frames[refIndex].CopyTo(frame, 0);
                        WWCompression.ApplyXorDelta(frame, fileData, ref frameOffs, 0);
                        frameEnd = frameOffs;
                        break;
                    default:
                        throw new FileTypeLoadException("Unknown frame type \"" + frameOffsFormat.ToString("X2") + "\".");
                }
                frames[i] = frame;

                Boolean brokenLoop = false;
                if (hasLoopFrame && i + 1 == hdrFrames)
                {
                    brokenLoop = !frame.SequenceEqual(frames[0]);
                    this.ExtraInfo = "Has loop frame";
                    if(brokenLoop)
                        this.ExtraInfo += " (but doesn't match)";
                }
                if (!hasLoopFrame || i + 1 < hdrFrames || brokenLoop)
                {
                    // DO NOT APPLY X AND Y OFFSETS! They are often set in files as byproduct of processing the original images, but the ENGINE doesn't use them!
                    // Convert frame data to image and frame object
                    Bitmap curFrImg = ImageUtils.BuildImage(frame, this.m_Width, this.m_Height, this.m_Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                    framePic.SetBitsPerColor(this.BitsPerPixel);
                    framePic.SetFileClass(this.FrameInputFileClass);
                    framePic.SetColorsInPalette(this.ColorsInPalette);
                    // Get compression info for UI
                    StringBuilder extraInfo = new StringBuilder("Compression: ");
                    if (frameOffsFormat == CcShpFrameFormat.Lcw)
                    {
                        if (realIndex >= 0)
                            extraInfo.Append("Reusing LCW data of frame ").Append(realIndex);
                        else
                            extraInfo.Append("LCW");
                    }
                    else
                    {
                        extraInfo.Append("XOR ");
                        if (frameOffsFormat == CcShpFrameFormat.XorChain)
                        {
                            extraInfo.Append("chained from frame ").Append(refIndex20);
                            if (refIndex20 != i - 1)
                                extraInfo.Append(" - ").Append(i - 1);
                        }
                        else
                        {
                            if (refIndex < 0)
                            {
                                // The referenced LCW data is not the last keyframe. Look up which frame it is.
                                Int32 refOffs = currentFrame.ReferenceOffset;
                                for (Int32 j = 0; j < i; ++j)
                                {
                                    OffsetInfo frInfo = offsets[j];
                                    if (frInfo.DataFormat != CcShpFrameFormat.Lcw || frInfo.DataOffset != refOffs)
                                        continue;
                                    refIndex = j;
                                    break;
                                }
                            }
                            if (refIndex >= 0)
                                extraInfo.Append("with key frame " + refIndex);
                            else // LCW data that's not in the index so far. I dunno. Just say where it's read from.
                                extraInfo.Append("with LCW data at 0x" + currentFrame.ReferenceOffset.ToString("X"));
                        }

                    }
                    Int32 frDataSize = frameEnd - frameStart;
                    extraInfo.Append("\nData size: ").Append(frDataSize).Append(" bytes");
                    if (frDataSize > 0)
                        extraInfo.Append(" @ 0x").Append(frameStart.ToString("X"));
                    if (brokenLoop)
                        extraInfo.Append("\nLoop frame (damaged?)");
                    framePic.SetExtraInfo(extraInfo.ToString());
                    this.m_FramesList[i] = framePic;
                }
                if (frameOffsEnd == fileData.Length)
                    break;
                // Prepare for next loop
                currentFrame = nextFrame;
                frameOffs = frameOffsEnd;
            }
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 width;
            Int32 height;
            this.PerformPreliminaryChecks(ref fileToSave, out width, out height);
            return new SaveOption[]
            {
                new SaveOption("TDL", SaveOptionType.Boolean, "Trim duplicate LCW frames", "1"),
                new SaveOption("FDL", SaveOptionType.Boolean, "Save all frames that have duplicates as LCW to allow more trimming. Useful on small graphics with many duplicates.", null, "0", new SaveEnableFilter("TDL", false, "1")),
                new SaveOption("LMX", SaveOptionType.Boolean, "Limit XOR chaining length by comparing full XOR chain size to the size of its XOR base frame", null, "1")
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 width;
            Int32 height;
            this.PerformPreliminaryChecks(ref fileToSave, out width, out height);
            Boolean trimDuplicates = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "TDL"));
            Boolean forceDuplicates = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "FDL"));
            Boolean chainedSizeCheck = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "LMX"));

            Int32 frames = fileToSave.Frames.Length;
            Int32 hdrSize = 0x0E;
            Byte[] header = new Byte[hdrSize];

            ArrayUtils.WriteIntToByteArray(header, 0, 2, true, (UInt16) frames);
            //ArrayUtils.WriteIntToByteArray(header, 2, 2, true, 0); // XPos
            //ArrayUtils.WriteIntToByteArray(header, 4, 2, true, 0); // YPos
            ArrayUtils.WriteIntToByteArray(header, 6, 2, true, (UInt16) width);
            ArrayUtils.WriteIntToByteArray(header, 8, 2, true, (UInt16) height);
            //ArrayUtils.WriteIntToByteArray(header, 0x0A, 2, true, (UInt16)DeltaSize);
            //ArrayUtils.WriteIntToByteArray(header, 0x0C, 2, true, 0); // Flags

            OffsetInfo[] framesIndex = new OffsetInfo[frames + 2];
            Byte[][] framesUncompr = new Byte[frames][];
            Byte[][] framescompr = new Byte[frames][];
            Int32[] frameOffsets = new Int32[frames];
            Int32[] framesDup = new Int32[frames];
            Boolean[] framesDupSrc = new Boolean[frames];
            for (Int32 i = 0; i < frames; ++i)
                framesDup[i] = -1;
            // Get these in advance. Will need them all in the end anyway.
            for (Int32 i = 0; i < frames; ++i)
            {
                Int32 stride;
                Byte[] uncompr = ImageUtils.GetImageData(fileToSave.Frames[i].GetBitmap(), out stride, true);
                framesUncompr[i] = uncompr;
                // Detect identical frames.
                if (!trimDuplicates)
                    continue;
                for (Int32 j = 0; j < i; ++j)
                {
                    if (framesDup[j] != -1 || !uncompr.SequenceEqual(framesUncompr[j]))
                        continue;
                    framesDup[i] = j;
                    if (forceDuplicates)
                        framesDupSrc[j] = true;
                    break;
                }
            }
            Int32 curDataOffs = hdrSize + (frames + 2) * 8; // + palSize
            Byte[] comprLCW = WWCompression.LcwCompress(framesUncompr[0]);
            framescompr[0] = comprLCW;
            framesIndex[0] = new OffsetInfo(curDataOffs, CcShpFrameFormat.Lcw, 0, 0);
            Int32 keyFrame = 0;
            frameOffsets[0] = curDataOffs;
            curDataOffs += comprLCW.Length;

            Int32 lastChainStartLen = 0;//comprLCW.Length;
            Int32 curChainLen = 0;

            // Overall strategy:
            // -Check compression and see which is smallest:
            //    1. LCW -> new keyframe
            //    2. XOR against key frame: XORBase -> Store INDEX of keyframe in ref field of OffsetInfo
            //    3. XORChain with previous if previous is XORBase or XORChain: XORChain -> chain back and store index of XORBase frame in ref field of OffsetInfo
            // -Take smallest result.

            for (Int32 i = 1; i < frames; ++i)
            {
                Int32 duplicate = framesDup[i];
                // Currently only doing this for LCW frames since compressed lcw of the same frame data is guaranteed to be the same, which is not true for XOR.
                if (duplicate != -1 && framesIndex[duplicate].DataFormat == CcShpFrameFormat.Lcw)
                {
                    Int32 origFrameOffs = frameOffsets[duplicate];
                    framesIndex[i] = new OffsetInfo(origFrameOffs, CcShpFrameFormat.Lcw, 0, CcShpFrameFormat.Empty);
                    keyFrame = i;
                    // To allow easy sum of lengths later
                    framescompr[i] = new Byte[0];
                    frameOffsets[i] = origFrameOffs;
                    continue;
                }
                // Attempting all compression methods:
                Byte[] uncompr = framesUncompr[i];
                // 1. LCW compress. If 'force duplicates to LCW' is enabled and this is detected as a dupe source, only this is performed.
                comprLCW = WWCompression.LcwCompress(uncompr);
                // 2. XOR with key frame.
                Byte[] comprXORBase = framesDupSrc[i] ? null : WWCompression.GenerateXorDelta(uncompr, framesUncompr[keyFrame]);
                // 3. Chain: only if previous frame is XOR
                Byte[] comprXORChain = framesIndex[i - 1].DataFormat == CcShpFrameFormat.Lcw || framesDupSrc[i] ? null : WWCompression.GenerateXorDelta(uncompr, framesUncompr[i - 1]);
                Int32 comprLCWLen = comprLCW.Length;
                Int32 comprXORBaseLen = comprXORBase == null ? Int32.MaxValue : comprXORBase.Length;
                Int32 comprXORChainLen = comprXORChain == null ? Int32.MaxValue : comprXORChain.Length;
                Int32 comprMin = Math.Min(comprLCWLen, comprXORBaseLen);
                if (comprXORChainLen != Int32.MaxValue && (!chainedSizeCheck || comprXORChainLen + curChainLen < lastChainStartLen))
                    comprMin = Math.Min(comprMin, comprXORChainLen);
                // Possible extra optimisation: check all previous entries in framescompr and see if any are identical to the data in any of these 3 methods.
                // Would take some time, though.
                if (comprLCWLen == comprMin)
                {
                    // LCW: new keyframe
                    framesIndex[i] = new OffsetInfo(curDataOffs, CcShpFrameFormat.Lcw, 0, CcShpFrameFormat.Empty);
                    framescompr[i] = comprLCW;
                    keyFrame = i;
                    //lastChainStartLen = comprLCWLen;
                }
                else if (comprXORBaseLen == comprMin)
                {
                    //XOR against key frame: XORBase
                    framesIndex[i] = new OffsetInfo(curDataOffs, CcShpFrameFormat.XorBase, frameOffsets[keyFrame], CcShpFrameFormat.Lcw);
                    framescompr[i] = comprXORBase;
                    lastChainStartLen = comprXORBaseLen;
                    //curChainLen = comprXORBaseLen;
                    curChainLen = 0;
                }
                else if (comprXORChainLen == comprMin)
                {
                    // XORChain with previous if previous is XORBase or XORChain: XORChain -> chain back and store index of XORBase frame in ref field of OffsetInfo
                    Int32 xorChainBase = i - 1;
                    while (framesIndex[xorChainBase].DataFormat == CcShpFrameFormat.XorChain)
                        xorChainBase--;
                    framesIndex[i] = new OffsetInfo(curDataOffs, CcShpFrameFormat.XorChain, xorChainBase, CcShpFrameFormat.XorChainRef);
                    framescompr[i] = comprXORChain;
                    curChainLen += comprXORChainLen;
                }
                frameOffsets[i] = curDataOffs;
                curDataOffs += comprMin;
            }

            Int32 sizeOffs = hdrSize + frames * 8; // + palSize
            Int32 size = curDataOffs;
            Byte[] finalData = new Byte[size];
            Int32 maxDeltaSize = framescompr.Max(f => f.Length);
            ArrayUtils.WriteIntToByteArray(header, 0x0A, 2, true, (UInt16) maxDeltaSize);
            header.CopyTo(finalData, 0);
            Int32 indexOffs = hdrSize;
            for (Int32 i = 0; i < frames; ++i)
            {
                framesIndex[i].Write(finalData, indexOffs);
                indexOffs += 8;
                Byte[] frameCompr = framescompr[i];
                if (frameCompr.Length == 0)
                    continue;
                frameCompr.CopyTo(finalData, frameOffsets[i]);
            }
            ArrayUtils.WriteIntToByteArray(finalData, sizeOffs, 3, true, (UInt32) size);
            return finalData;
        }

        private void PerformPreliminaryChecks(ref SupportedFileType fileToSave, out Int32 width, out Int32 height)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new NotSupportedException("No source data given!");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames == null ? 0 : frames.Length;
            if (nrOfFrames == 0)
                throw new NotSupportedException("This format needs at least one frame.");
            width = -1;
            height = -1;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new NotSupportedException("SHP can't handle empty frames!");
                if (frame.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8bpp images.");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new NotSupportedException("Not all frames in input type are the same size!");
            }
        }

        private class OffsetInfo
        {
            public Int32 DataOffset { get; set; }
            public CcShpFrameFormat DataFormat { get; set; }
            public Int32 ReferenceOffset { get; set; }
            public CcShpFrameFormat ReferenceFormat { get; set; }
 
            public OffsetInfo(Int32 dataOffset, CcShpFrameFormat dataFormat, Int32 referenceOffset, CcShpFrameFormat referenceFormat)
            {
                this.DataOffset = dataOffset;
                this.DataFormat = dataFormat;
                this.ReferenceOffset = referenceOffset;
                this.ReferenceFormat = referenceFormat;
            }

            public static OffsetInfo Read(Byte[] fileData, Int32 offset)
            {
                Int32 dataOffset = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, offset, 3, true);
                CcShpFrameFormat dataFormat = (CcShpFrameFormat) ArrayUtils.ReadIntFromByteArray(fileData, offset + 3, 1, true);
                Int32 referenceOffset = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 3, true);
                CcShpFrameFormat referenceFormat = (CcShpFrameFormat) ArrayUtils.ReadIntFromByteArray(fileData, offset + 7, 1, true);
                return new OffsetInfo(dataOffset, dataFormat, referenceOffset, referenceFormat);
            }

            public void Write(Byte[] fileData, Int32 offset)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset + 0, 3, true, (UInt32) this.DataOffset);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 3, 1, true, (Byte) this.DataFormat);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 4, 3, true, (UInt32) ReferenceOffset);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 7, 1, true, (Byte) this.ReferenceFormat);
            }
        }

        private enum CcShpFrameFormat
        {
            Empty = 0x00,
            XorChain = 0x20,
            XorBase = 0x40,
            XorChainRef = 0x48,
            Lcw = 0x80,
        }

    }

}