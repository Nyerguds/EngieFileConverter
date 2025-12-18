using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        public override String ShortTypeName { get { return "Westwood Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String ShortTypeDescription { get { return "Westwood Shape File"; } }
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
                throw new FileTypeLoadException("Not a C&C1/RA1 SHP! file");
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
            // Read palette if flag enabled?
            if (m_HasPalette)
                this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(fileData, hdrSize));
            else
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            
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
                Int32 frameEnd = frameOffsEnd;
                CCShpFrameFormat frameOffsFormat = currentFrame.DataFormat;
                //Int32 dataLen = frameOffsEnd - frameOffs;
                if (frameOffs > fileData.Length || frameOffsEnd > fileData.Length)
                    throw new FileTypeLoadException("File is too small to contain all frame data!");
                Byte[] frame = new Byte[frameSize];
                Int32 refIndex = -1;
                Int32 refIndex20 = -1;
                if (frameOffsFormat == CCShpFrameFormat.LCW)
                {
                    // Actual LCW-compressed frame data.
                    WWCompression.LcwDecompress(fileData, ref frameOffs, frame);
                    frameEnd = realIndex >= 0 ? frameStart : frameOffs;
                    lastKeyFrame = currentFrame;
                    lastKeyFrameNr = i;
                }
                else
                {
                    if (frameOffsFormat == CCShpFrameFormat.XORChain)
                    {
                        // 0x20 = XOR with previous frame. Only used for chaining to previous XOR frames.
                        // Don't actually need this, but I do the integrity checks:
                        refIndex20 = currentFrame.ReferenceOffset;
                        CCShpFrameFormat refFormat = currentFrame.ReferenceFormat;
                        if ((refFormat != CCShpFrameFormat.XORChainRef) || (refIndex20 >= i || offsets[refIndex20].DataFormat != CCShpFrameFormat.XORBase))
                            throw new FileTypeLoadException("Bad frame reference information for frame " + i + ".");
                        frames[i - 1].CopyTo(frame, 0);
                    }
                    else if (frameOffsFormat == CCShpFrameFormat.XORBase)
                    {
                        // 0x40 = XOR with a previous frame. Could technically reference anything, but normally only references the last LCW "keyframe".
                        // This load method ignores the format saved in ReferenceFormat since the decompressed frame is stored already.
                        if (lastKeyFrame.DataOffset == currentFrame.ReferenceOffset)
                            refIndex = lastKeyFrameNr;
                        else if (!offsetIndices.TryGetValue(currentFrame.ReferenceOffset, out refIndex))
                        {
                            // not found, but in file anyway?? Whatever; if itùs LCW, just read it.
                            Int32 readOffs = currentFrame.ReferenceOffset;
                            CCShpFrameFormat readFormat = currentFrame.ReferenceFormat;
                            if (readFormat == CCShpFrameFormat.LCW)
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
                if (frameOffsFormat == CCShpFrameFormat.LCW)
                {
                    if (realIndex >= 0)
                        extraInfo.Append("Reusing LCW data of frame ").Append(realIndex);
                    else
                        extraInfo.Append("LCW");
                }
                else
                {
                    extraInfo.Append("XOR ");
                    if (frameOffsFormat == CCShpFrameFormat.XORChain)
                        extraInfo.Append("chained from frame ").Append(refIndex20);
                    else
                        extraInfo.Append("with key frame " + refIndex);
                }
                extraInfo.Append("\nData size: ").Append(frameEnd - frameStart);
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
            return new SaveOption[]
            {
                new SaveOption("TDL", SaveOptionType.Boolean, "Trim duplicate LCW frames", "1"),
                new SaveOption("FDL", SaveOptionType.Boolean, "Save frames that have duplicates as LCW for more trimming. Useful on small graphics with many duplicates.", "0"),
            };
        }


        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
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
            Int32 width = -1;
            Int32 height = -1;
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
            framesIndex[0] = new OffsetInfo(curDataOffs, CCShpFrameFormat.LCW, 0, 0);
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
                if (duplicate != -1 && framesIndex[duplicate].DataFormat == CCShpFrameFormat.LCW)
                {
                    Int32 origFrameOffs = frameOffsets[duplicate];
                    framesIndex[i] = new OffsetInfo(origFrameOffs, CCShpFrameFormat.LCW, 0, CCShpFrameFormat.Empty);
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
                Byte[] comprXORChain = framesIndex[i - 1].DataFormat == CCShpFrameFormat.LCW || framesDupSrc[i] ? null : WWCompression.GenerateXorDelta(uncompr, framesUncompr[i - 1]);
                Int32 comprLCWLen = comprLCW.Length;
                Int32 comprXORBaseLen = comprXORBase == null ? Int32.MaxValue : comprXORBase.Length;
                Int32 comprXORChainLen = comprXORChain == null ? Int32.MaxValue : comprXORChain.Length;
                Int32 comprMin = Math.Min(Math.Min(comprLCWLen, comprXORBaseLen), comprXORChainLen);
                if (comprLCWLen == comprMin)
                {
                    // LCW: new keyframe
                    framesIndex[i] = new OffsetInfo(curDataOffs, CCShpFrameFormat.LCW, 0, CCShpFrameFormat.Empty);
                    framescompr[i] = comprLCW;
                    keyFrame = i;
                }
                else if (comprXORBaseLen == comprMin)
                {
                    //XOR against key frame: XORBase
                    framesIndex[i] = new OffsetInfo(curDataOffs, CCShpFrameFormat.XORBase, frameOffsets[keyFrame], CCShpFrameFormat.LCW);
                    framescompr[i] = comprXORBase;
                }
                else if (comprXORChainLen == comprMin)
                {
                    // XORChain with previous if previous is XORBase or XORChain: XORChain -> chain back and store index of XORBase frame in ref field of OffsetInfo
                    Int32 xorChainBase = i - 1;
                    while (framesIndex[xorChainBase].DataFormat == CCShpFrameFormat.XORChain)
                        xorChainBase--;
                    framesIndex[i] = new OffsetInfo(curDataOffs, CCShpFrameFormat.XORChain, xorChainBase, CCShpFrameFormat.XORChainRef);
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

        public static void PreCheckSplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex, Boolean forCombine)
        {
            if (file == null)
                throw new NotSupportedException("No source given!");
            if (!file.IsFramesContainer || file.Frames.Length == 0)
                throw new NotSupportedException("File contains no frames!");
            if ((file.FrameInputFileClass & FileClass.ImageIndexed) != 0)
                return;
            if (forCombine && file.Frames.Length % 2 != 0)
                throw new NotSupportedException("File does not contains an even number of frames!");
            foreach (SupportedFileType frame in file.Frames)
            {
                if (frame == null || frame.GetBitmap() == null)
                    throw new NotSupportedException("Empty frames found!");
                if ((frame.FileClass & FileClass.ImageIndexed) == 0)
                    throw new NotSupportedException("All frames need to be 8-bit paletted!");
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new NotSupportedException("This operation is not supported for types with empty frames!");
                Int32 bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                if (bpp > 8)
                    throw new NotSupportedException("Non-paletted frames found!");
                Int32 colors = bm.Palette.Entries.Length;
                if (colors > sourceShadowIndex)
                    throw new NotSupportedException("Not all frames have enough colours to contain the source shadow index!");
                if (forCombine && colors > destShadowIndex)
                    throw new NotSupportedException("Not all frames have enough colours to contain the destination shadow index!");
            }
        }

        public static FileFrames SplitShadows(SupportedFileType file, Byte sourceShadowIndex, Byte destShadowIndex)
        {
            PreCheckSplitShadows(file, sourceShadowIndex, destShadowIndex, false);
            FileFrames newfile = new FileFrames();
            newfile.SetCommonPalette(true);
            newfile.SetBitsPerColor(8);
            newfile.SetColorsInPalette(0);
            Boolean[] transMask = file.TransparencyMask;
            newfile.SetTransparencyMask(transMask);
            SupportedFileType[] shadowFrames = new SupportedFileType[file.Frames.Length];
            Boolean shadowFound = false;
            Color[] palette = null;
            for (Int32 i = 0; i < file.Frames.Length; i++)
            {
                SupportedFileType frame = file.Frames[i];
                String folder = null;
                String name = String.Empty;
                String ext = String.Empty;
                if (frame.LoadedFile != null)
                {
                    name = Path.GetFileNameWithoutExtension(frame.LoadedFile);
                    ext = Path.GetExtension(frame.LoadedFile);
                    folder = Path.GetDirectoryName(frame.LoadedFile);
                }
                else if (frame.LoadedFileName != null)
                {
                    name = Path.GetFileNameWithoutExtension(frame.LoadedFileName);
                    ext = Path.GetExtension(frame.LoadedFileName);
                }
                if (folder == null && !String.IsNullOrEmpty(file.LoadedFile))
                    folder = Path.GetDirectoryName(file.LoadedFile);
                
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
                    Int32 bpp = Image.GetPixelFormatSize(bm.PixelFormat);
                    if (bpp < 8)
                        imageData = ImageUtils.ConvertTo8Bit(imageData, width, height, 0, bpp, bpp != 1, ref stride);
                    imageDataShadow = new Byte[imageData.Length];
                    for (Int32 y = 0; y < height; y++)
                    {
                        Int32 offs = y*stride;
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
                    if (bpp < 8)
                    {
                        Int32 stride2 = stride;
                        imageData = ImageUtils.ConvertFrom8Bit(imageData, width, height, bpp, bpp != 1, ref stride2);
                        stride2 = stride;
                        imageDataShadow = ImageUtils.ConvertFrom8Bit(imageDataShadow, width, height, bpp, bpp != 1, ref stride2);
                    }
                }
                Bitmap imageNoShadows = ImageUtils.BuildImage(imageData, width, height, stride, bm.PixelFormat, palette, null);
                String nameNoShadows = name + ext;
                if (folder != null)
                    nameNoShadows = Path.Combine(folder, nameNoShadows);
                FileImageFrame frameNoShadows = new FileImageFrame();
                frameNoShadows.LoadFileFrame(newfile, file, imageNoShadows, nameNoShadows, i);
                frameNoShadows.SetBitsPerColor(frame.BitsPerPixel);
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
            return null;
        }

        private class OffsetInfo
        {
            public Int32 DataOffset { get; set; }
            public CCShpFrameFormat DataFormat { get; set; }
            public Int32 ReferenceOffset { get; set; }
            public CCShpFrameFormat ReferenceFormat { get; set; }

            public OffsetInfo(Int32 dataOffset, CCShpFrameFormat dataFormat, Int32 referenceOffset, CCShpFrameFormat referenceFormat)
            {
                this.DataOffset = dataOffset;
                this.DataFormat = dataFormat;
                this.ReferenceOffset = referenceOffset;
                this.ReferenceFormat = referenceFormat;
            }

            public static OffsetInfo Read(Byte[] fileData, Int32 offset)
            {
                Int32 dataOffset = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, offset, 3, true);
                CCShpFrameFormat dataFormat = (CCShpFrameFormat)ArrayUtils.ReadIntFromByteArray(fileData, offset + 3, 1, true);
                Int32 referenceOffset = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 3, true);
                CCShpFrameFormat referenceFormat = (CCShpFrameFormat)ArrayUtils.ReadIntFromByteArray(fileData, offset + 7, 1, true);
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

        private enum CCShpFrameFormat
        {
            Empty = 0x00,
            LCW = 0x80,
            XORChainRef = 0x48,
            XORBase = 0x40,
            XORChain = 0x20,
        }

    }

}