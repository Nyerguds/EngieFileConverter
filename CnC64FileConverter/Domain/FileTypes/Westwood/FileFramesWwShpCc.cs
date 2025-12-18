using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
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
        //protected String[] formats = new String[] { "Dune II", "Legend of Kyrandia", "C&C1/RA1", "Tiberian Sun" };
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
            // Throw a HeaderParseException from the moment it's detected as a specific type that's not the requested one.
            // OffsetInfo / ShapeFileHeader
            Int32 hdrSize = 0x0E;
            if (fileData.Length < hdrSize)
                throw new FileTypeLoadException("Not long enough for header.");
            UInt16 hdrFrames = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            //UInt16 hdrXPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            //UInt16 hdrYPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            UInt16 hdrWidth = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 6, 2, true);
            UInt16 hdrHeight = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            //UInt16 hdrDeltaSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x0A, 2, true);
            UInt16 hdrFlags = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 2, true);
            if (hdrFrames == 0) // Can be TS SHP; it identifies with an empty first byte IIRC.
                throw new FileTypeLoadException("Not a C&C1/RA1 SHP file!");
            if (hdrWidth == 0 || hdrHeight == 0)
                throw new FileTypeLoadException("Illegal values in header!");
            this.m_HasPalette = (hdrFlags & 1) != 0;
            Int32 palSize = m_HasPalette ? 0x300 : 0;
            Byte[][] frames = new Byte[hdrFrames][];
            OffsetInfo[] offsets = new OffsetInfo[hdrFrames];
            Dictionary<Int32, Int32> offsetIndices = new Dictionary<Int32, Int32>();
            Int32 offsSize = 8;
            Int32 fileSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, hdrSize + palSize + offsSize * hdrFrames, 3, true);
            if (fileData.Length != fileSize)
                throw new FileTypeLoadException("File size does not match size value in header!");
            if (fileData.Length < hdrSize + palSize + offsSize * (hdrFrames + 2))
                throw new FileTypeLoadException("Header is too small to contain the frames index!");
            // Read palette if flag enabled. No games I know support using it, but, might as well be complete.
            if (m_HasPalette)
            {
                try { this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(fileData, hdrSize)); }
                catch (ArgumentException argex) { throw new FileTypeLoadException("Illegal values in embedded palette.", argex);}
            }
            else
            {
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            }
            this.m_FramesList = new SupportedFileType[hdrFrames];
            this.m_Width = hdrWidth;
            this.m_Height = hdrHeight;
            // Frames decompression
            Int32 curOffs = hdrSize + palSize;
            Int32 frameSize = hdrWidth * hdrHeight;
            OffsetInfo currentFrame = OffsetInfo.Read(fileData, curOffs);
            Int32 lastKeyFrameNr = 0;
            OffsetInfo lastKeyFrame = currentFrame;
            Int32 frameOffs = currentFrame.DataOffset;

            for (Int32 i = 0; i < hdrFrames; i++)
            {
                Int32 realIndex = -1;
                if (!offsetIndices.ContainsKey(currentFrame.DataOffset))
                    offsetIndices.Add(currentFrame.DataOffset, i);
                else
                    offsetIndices.TryGetValue(currentFrame.DataOffset, out realIndex);
                offsets[i] = currentFrame;
                curOffs += offsSize;
                OffsetInfo nextFrame = OffsetInfo.Read(fileData, curOffs);
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
                if (frameOffsFormat == CcShpFrameFormat.Lcw)
                {
                    // Actual LCW-compressed frame data.
                    WWCompression.LcwDecompress(fileData, ref frameOffs, frame);
                    frameEnd = realIndex >= 0 ? frameStart : frameOffs;
                    lastKeyFrame = currentFrame;
                    lastKeyFrameNr = i;
                }
                else
                {
                    if (frameOffsFormat == CcShpFrameFormat.XorChain)
                    {
                        // 0x20 = XOR with previous frame. Only used for chaining to previous XOR frames.
                        // Don't actually need this, but I do the integrity checks:
                        refIndex20 = currentFrame.ReferenceOffset;
                        CcShpFrameFormat refFormat = currentFrame.ReferenceFormat;
                        if ((refFormat != CcShpFrameFormat.XorChainRef) || (refIndex20 >= i || offsets[refIndex20].DataFormat != CcShpFrameFormat.XorBase))
                            throw new FileTypeLoadException("Bad frame reference information for frame " + i + ".");
                        frames[i - 1].CopyTo(frame, 0);
                    }
                    else if (frameOffsFormat == CcShpFrameFormat.XorBase)
                    {
                        // 0x40 = XOR with a previous frame. Could technically reference anything, but normally only references the last LCW "keyframe".
                        // This load method ignores the format saved in ReferenceFormat since the decompressed frame is stored already.
                        if (lastKeyFrame.DataOffset == currentFrame.ReferenceOffset)
                            refIndex = lastKeyFrameNr;
                        else if (!offsetIndices.TryGetValue(currentFrame.ReferenceOffset, out refIndex))
                        {
                            // not found, but in file anyway?? Whatever; if itùs LCW, just read it.
                            Int32 readOffs = currentFrame.ReferenceOffset;
                            CcShpFrameFormat readFormat = currentFrame.ReferenceFormat;
                            if (readFormat == CcShpFrameFormat.Lcw)
                                WWCompression.LcwDecompress(fileData, ref readOffs, frame);
                            else
                                throw new FileTypeLoadException("No reference found for XOR frame!");
                        }
                        if (refIndex >= i)
                            throw new FileTypeLoadException("XOR cannot reference later frames!");
                        frames[refIndex].CopyTo(frame, 0);
                    }
                    else
                        throw new FileTypeLoadException("Unknown frame type \"" + frameOffsFormat.ToString("X2") + "\".");
                    WWCompression.ApplyXorDelta(frame, fileData, ref frameOffs, 0);
                    frameEnd = frameOffs;
                }
                frames[i] = frame;
                // Convert frame data to image and frame object
                Bitmap curFrImg = ImageUtils.BuildImage(frame, this.m_Width, this.m_Height, this.m_Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetColorsInPalette(this.ColorsInPalette);
                framePic.SetTransparencyMask(this.TransparencyMask);
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
                        extraInfo.Append("chained from frame ").Append(refIndex20);
                    else
                        extraInfo.Append("with key frame " + refIndex);
                }
                Int32 frDataSize = frameEnd - frameStart;
                extraInfo.Append("\nData size: ").Append(frDataSize).Append(" bytes");
                if (frDataSize > 0)
                    extraInfo.Append(" @ 0x").Append(frameStart.ToString("X")); ;
                framePic.SetExtraInfo(extraInfo.ToString());
                this.m_FramesList[i] = framePic;
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
            PerformPreliminarychecks(ref fileToSave, out width, out height);
            return new SaveOption[]
            {
                new SaveOption("TDL", SaveOptionType.Boolean, "Trim duplicate LCW frames", "1"),
                new SaveOption("FDL", SaveOptionType.Boolean, "Save frames that have duplicates as LCW for more trimming. Useful on small graphics with many duplicates.", "0"),
            };
        }


        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 width;
            Int32 height;
            PerformPreliminarychecks(ref fileToSave, out width, out height);
            Boolean trimDuplicates = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "TDL"));
            Boolean forceDuplicates = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "FDL"));

            Int32 frames = fileToSave.Frames.Length;
            Int32 hdrSize = 0x0E;
            Byte[] header = new Byte[hdrSize];

            ArrayUtils.WriteIntToByteArray(header, 0, 2, true, (UInt16)frames);
            //ArrayUtils.WriteIntToByteArray(header, 2, 2, true, 0); // XPos
            //ArrayUtils.WriteIntToByteArray(header, 4, 2, true, 0); // YPos
            ArrayUtils.WriteIntToByteArray(header, 6, 2, true, (UInt16)width);
            ArrayUtils.WriteIntToByteArray(header, 8, 2, true, (UInt16)height);
            //ArrayUtils.WriteIntToByteArray(header, 0x0A, 2, true, (UInt16)DeltaSize);
            //ArrayUtils.WriteIntToByteArray(header, 0x0C, 2, true, 0); // Flags

            OffsetInfo[] framesIndex = new OffsetInfo[frames + 2];
            Byte[][] framesUncompr = new Byte[frames][];
            Byte[][] framescompr = new Byte[frames][];
            Int32[] frameOffsets = new Int32[frames];
            Int32[] framesDup = new Int32[frames];
            Boolean[] framesDupSrc = new Boolean[frames];
            for (Int32 i = 0; i < frames; i++)
                framesDup[i] = -1;
            // Get these in advance. Will need them all in the end anyway.
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 stride;
                Byte[] uncompr = ImageUtils.GetImageData(fileToSave.Frames[i].GetBitmap(), out stride);
                uncompr = ImageUtils.CollapseStride(uncompr, width, height, 8, ref stride);
                framesUncompr[i] = uncompr;
                // Detect identical frames.
                if (!trimDuplicates)
                    continue;
                for (Int32 j = 0; j < i; j++)
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

            // Overall strategy:
            // -Check compression and see which is smallest:
            //    1. LCW -> new keyframe
            //    2. XOR against key frame: XORBase -> Store INDEX of keyframe in ref field of OffsetInfo
            //    3. XORChain with previous if previous is XORBase or XORChain: XORChain -> chain back and store index of XORBase frame in ref field of OffsetInfo
            // -Take smallest result.

            for (Int32 i = 1; i < frames; i++)
            {
                Int32 duplicate = framesDup[i];
                // Currently only doing this for LCW frames since compressed lcw of the same frame data is guaranteed to be the same, which is not true for XOR.
                // Also, xorchain length of identicals is empty anyway.
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
                Int32 comprMin = Math.Min(Math.Min(comprLCWLen, comprXORBaseLen), comprXORChainLen);
                if (comprLCWLen == comprMin)
                {
                    // LCW: new keyframe
                    framesIndex[i] = new OffsetInfo(curDataOffs, CcShpFrameFormat.Lcw, 0, CcShpFrameFormat.Empty);
                    framescompr[i] = comprLCW;
                    keyFrame = i;
                }
                else if (comprXORBaseLen == comprMin)
                {
                    //XOR against key frame: XORBase
                    framesIndex[i] = new OffsetInfo(curDataOffs, CcShpFrameFormat.XorBase, frameOffsets[keyFrame], CcShpFrameFormat.Lcw);
                    framescompr[i] = comprXORBase;
                }
                else if (comprXORChainLen == comprMin)
                {
                    // XORChain with previous if previous is XORBase or XORChain: XORChain -> chain back and store index of XORBase frame in ref field of OffsetInfo
                    Int32 xorChainBase = i - 1;
                    while (framesIndex[xorChainBase].DataFormat == CcShpFrameFormat.XorChain)
                        xorChainBase--;
                    framesIndex[i] = new OffsetInfo(curDataOffs, CcShpFrameFormat.XorChain, xorChainBase, CcShpFrameFormat.XorChainRef);
                    framescompr[i] = comprXORChain;
                }
                frameOffsets[i] = curDataOffs;
                curDataOffs += comprMin;
            }

            Int32 sizeOffs = hdrSize + frames * 8; // + palSize
            Int32 size = curDataOffs;
            Byte[] finalData = new Byte[size];
            Int32 maxDeltaSize = framescompr.Max(f => f.Length);
            ArrayUtils.WriteIntToByteArray(header, 0x0A, 2, true, (UInt16)maxDeltaSize);
            header.CopyTo(finalData, 0);
            Int32 indexOffs = hdrSize;
            for (Int32 i = 0; i < frames; i++)
            {
                framesIndex[i].Write(finalData, indexOffs);
                indexOffs += 8;
                Byte[] frameCompr = framescompr[i];
                if (frameCompr.Length == 0)
                    continue;
                frameCompr.CopyTo(finalData, frameOffsets[i]);
            }
            ArrayUtils.WriteIntToByteArray(finalData, sizeOffs, 3, true, (UInt32)size);
            return finalData;
        }

        private void PerformPreliminarychecks(ref SupportedFileType fileToSave, out Int32 width, out Int32 height)
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
                Int32 dataOffset = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset, 3, true);
                CcShpFrameFormat dataFormat = (CcShpFrameFormat)ArrayUtils.ReadIntFromByteArray(fileData, offset + 3, 1, true);
                Int32 referenceOffset = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 3, true);
                CcShpFrameFormat referenceFormat = (CcShpFrameFormat)ArrayUtils.ReadIntFromByteArray(fileData, offset + 7, 1, true);
                return new OffsetInfo(dataOffset, dataFormat, referenceOffset, referenceFormat);
            }

            public void Write(Byte[] fileData, Int32 offset)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset + 0, 3, true, (UInt32)this.DataOffset);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 3, 1, true, (Byte)this.DataFormat);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 4, 3, true, (UInt32)ReferenceOffset);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 7, 1, true, (Byte)this.ReferenceFormat);
            }
        }

        private enum CcShpFrameFormat
        {
            Empty = 0x00,
            Lcw = 0x80,
            XorChainRef = 0x48,
            XorBase = 0x40,
            XorChain = 0x20,
        }

    }

}