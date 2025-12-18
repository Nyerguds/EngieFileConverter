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
    public class FileFramesFntD2k : SupportedFileType
    {
        protected const String ERR_NOHEADER = "File data too short to contain header.";
        protected const String ERR_SIZEHEADER = "File size value in header does not match file data length.";
        protected const String ERR_BADHEADER = "Identifying bytes in header do not match.";

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "FntD2k"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "IG Font (Dune 2000)"; } }
        public override String[] FileExtensions { get { return new String[] { "fnt" }; } }
        public override String ShortTypeDescription { get { return "IG Font (Dune 2000)"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 8; } }

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
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.FromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public void FromFileData(Byte[] fileData, String sourcePath)
        {
            // Technically header + first symbol header, but whatev :p
            if (fileData.Length < 0x410)
                throw new FileTypeLoadException(ERR_NOHEADER);
            Byte index00 = fileData[00]; // "FontLoadedFlag" according to Siberian GRemlin. No idea why he called it that.
            Byte spaceWidth = fileData[01];
            Byte firstSymbol = fileData[02];
            // 'Interval' is right-edge X optimization much like WW does Y optimization. Pad it onto the font. The Save will trim it off again.
            Byte padding = fileData[03];
            Byte maxHeight = fileData[04];
            Byte empty05 = fileData[05];
            Byte empty06 = fileData[06];
            Byte empty07 = fileData[07];
            //No clue if this is ok as test...
            if (index00 != 1 || empty05 != 0 || empty06 != 0 || empty07 != 0)
                throw new FileTypeLoadException(ERR_BADHEADER);
            this.m_Height = maxHeight;
            // Wlll be increased to the max found in the file.
            this.m_Width = spaceWidth;
            this.m_FramesList = new SupportedFileType[0x100];
            Boolean[] transMask = this.TransparencyMask;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, transMask, false);
            // Prepare space
            Int32 spacePos = firstSymbol - 1;
            if (spacePos < 0)
                spacePos += 0x100;
            Int32 actualSpaceWidth = spaceWidth + padding;
            Bitmap spaceImg = ImageUtils.BuildImage(new Byte[maxHeight * actualSpaceWidth], actualSpaceWidth, maxHeight, actualSpaceWidth, PixelFormat.Format8bppIndexed, m_Palette, null);
            FileImageFrame space = CreateFrame(spaceImg, sourcePath, spacePos, 1, 1, -1, -1, 0);
            space.SetExtraInfo(space.ExtraInfo + "\nSpace width in header: " + spaceWidth + "\nApplied padding: " + padding);
            // Read the rest of the symbols.
            Int32 readOffset = 0x408;
            Int32 datalen = fileData.Length;
            for (Int32 i = 0; i < 0x100; ++i)
            {
                Byte currentSymbol = (Byte)((firstSymbol + i) & 0xFF);
                if (readOffset + 8 > datalen)
                    throw new FileTypeLoadException("File data too short for symbol header of symbol #" + firstSymbol + ".");
                Int32 origSymbolWidth = ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset);
                Int32 symbolWidth = origSymbolWidth + padding;
                this.m_Width = Math.Max(symbolWidth, this.m_Width);
                readOffset += 4;
                Int32 symbolHeight = ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset);
                readOffset += 4;
                Int32 symbolReadSize = origSymbolWidth * symbolHeight;
                Bitmap curFrImg = null;
                if (symbolReadSize > 0)
                {
                    if (readOffset + symbolReadSize > datalen)
                        throw new FileTypeLoadException("File data too short for symbol data of symbol #" + firstSymbol + ".");
                    // Space symbol; break after all checks are done.
                    if (i == 0xFF)
                        break;
                    Byte[] symbolData = new Byte[symbolReadSize];
                    Array.Copy(fileData, readOffset, symbolData, 0, symbolData.Length);
                    if (padding > 0)
                        symbolData = ImageUtils.ChangeStride(symbolData, origSymbolWidth, symbolHeight, symbolWidth, false, 0);
                    curFrImg = ImageUtils.BuildImage(symbolData, symbolWidth, symbolHeight, symbolWidth, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                }
                if (i == 0xFF)
                    break;
                CreateFrame(curFrImg, sourcePath, currentSymbol, readOffset - 8, symbolReadSize + 8, origSymbolWidth, symbolHeight, padding);
                readOffset += symbolReadSize;
            }
            this.ExtraInfo = "Space symbol width in header: " + spaceWidth
                + "\nFirst symbol index: " + firstSymbol
                + "\nPadding between symbols: " + padding + " pixel" + (padding == 1 ? String.Empty : "s")
                + "\nFont height in header: " + maxHeight;
        }

        /// <summary>
        /// Creates a frame, with the nevessary ExtraInfo, and adds it to the frames list at the given index.
        /// </summary>
        /// <param name="curFrImg">Current frame image.</param>
        /// <param name="dataOffset">Offset from which the data was read.</param>
        /// <param name="dataLength">Length read at the data offset</param>
        /// <param name="sourcePath">Path of the source file.</param>
        /// <param name="currentSymbol">Current symbol index.</param>
        /// <param name="width">Width of the symbol</param>
        /// <param name="height">Height of the symbol.</param>
        /// <param name="padding">Added padding from the header.</param>
        /// <returns>The created frame.</returns>
        private FileImageFrame CreateFrame(Bitmap curFrImg, String sourcePath, Int32 currentSymbol, Int32 dataOffset, Int32 dataLength, Int32 width, Int32 height, Int32 padding)
        {
            FileImageFrame framePic = new FileImageFrame();
            framePic.LoadFileFrame(this, this, curFrImg, sourcePath, currentSymbol);
            framePic.SetBitsPerColor(this.BitsPerPixel);
            framePic.SetFileClass(this.FrameInputFileClass);
            framePic.SetNeedsPalette(this.NeedsPalette);
            framePic.SetColors(this.m_Palette);
            StringBuilder extraInfo = new StringBuilder();
            extraInfo.Append("Data: ").Append(dataLength).Append(" byte");
            if (dataLength != 1)
                extraInfo.Append("s");
            extraInfo.Append(" @ 0x").Append(dataOffset.ToString("X"));
            if (width >= 0 && height >= 0)
            {
                extraInfo.Append("\nStored image dimensions: ").Append(width).Append("x").Append(height);
                if (width > 0 && height > 0 && padding > 0)
                    extraInfo.Append("\nApplied padding: ").Append(padding).Append(" pixel").Append(padding == 1 ? String.Empty : "s");
            }
            framePic.SetExtraInfo(extraInfo.ToString());
            this.m_FramesList[currentSymbol] = framePic;
            return framePic;
        }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 maxUsedHeight;
            this.PerformPreliminaryChecks(fileToSave, out maxUsedHeight);
            FileFramesWwFntV3 fontFile = fileToSave as FileFramesWwFntV3;
            Int32 fontHeight = fontFile != null ? fontFile.Height : maxUsedHeight;
            return new SaveOption[]
            {
                new SaveOption("FHE", SaveOptionType.Number, "Font height", fontHeight +",255", fontHeight.ToString())
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return this.SaveFont(fileToSave, saveOptions);
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out Int32 height)
        {
            // Preliminary checks
            SupportedFileType[] frames = fileToSave.Frames;
            if (!fileToSave.IsFramesContainer || frames == null)
                throw new ArgumentException("No frames found in source data!", "fileToSave");
            Int32 frameLen = frames.Length;
            if (frameLen == 0)
                throw new ArgumentException("No frames found in source data!", "fileToSave");
            if (frameLen < 32)
                throw new ArgumentException("Dune 2000 font needs to contain at least 32 characters!", "fileToSave");
            if (frameLen > 256)
                throw new ArgumentException("Dune 2000 font can only handle up to 256 frames!", "fileToSave");
            height = -1;
            for (Int32 i = 0; i < frameLen; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame.BitsPerPixel != this.BitsPerPixel)
                    throw new ArgumentException("Not all frames in input type are " + this.BitsPerPixel + "-bit images!", "fileToSave");
                height = Math.Max(height, frame.Height);
                if (i == 32 && frame.Width > 255)
                    throw new ArgumentException("Not all frames in input type are " + this.BitsPerPixel + "-bit images!", "fileToSave");
                if (height > 255)
                    throw new ArgumentException("Frame dimensions exceed 255!", "fileToSave");
            }
            return frames;
        }

        protected Byte[] SaveFont(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 fontHeight;
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave, out fontHeight);
            // Override the one from the preliminary check.
            fontHeight = Int32.Parse(SaveOption.GetSaveOptionValue(saveOptions, "FHE"));
            Int32 origFrameLen = frames.Length;
            if (origFrameLen < 0x100)
            {
                SupportedFileType[] newFrames = new SupportedFileType[0x100];
                Array.Copy(frames, 0, newFrames, 0, origFrameLen);
                for (Int32 i = origFrameLen; i < 0x100; ++i)
                {
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(fileToSave, fileToSave, null, null, i);
                    framePic.SetBitsPerColor(fileToSave.BitsPerPixel);
                    framePic.SetFileClass(fileToSave.FrameInputFileClass);
                    framePic.SetNeedsPalette(fileToSave.NeedsPalette);
                    framePic.SetColors(fileToSave.GetColors());
                    newFrames[i] = fileToSave;
                }
                frames = newFrames;
            }
            SupportedFileType[] baseList = new SupportedFileType[0x100];
            Byte[][] framesList = new Byte[0x100][];
            Byte spaceWidth = (Byte)frames[0x20].Width;
            // Final saved data does in fact contain a 0x0 dummy entry for the space character... further invalidating the whole optimisation effort.
            Byte firstSymbol = 0x21;
            // this is FF and not 100 because the space itself is omitted.
            Int32 remainingSymbols = 0x100 - firstSymbol; // 222 ?

            Array.Copy(frames, firstSymbol, baseList, 0, remainingSymbols);
            Array.Copy(frames, 0, baseList, remainingSymbols, firstSymbol);
            // Remove space. don't want to make a new object for this; I'll just check on null.
            baseList[0xFF] = null;

            // Code to detect how much space at the right edge is added padding to create space between pixels.
            // This space is trimmed off and added in the header instead.
            // Start from max that can be trimmed off the space, since it's not in the list.
            Int32 globalOpenSpace = spaceWidth;
            for (Int32 i = 0; i < 0x100; ++i)
            {
                SupportedFileType frame = baseList[i];
                // ignore completely empty characters; they'd reduce it to 0 for no reason.
                if (frame == null || frame.Width == 0 && frame.Height == 0 || frame.GetBitmap() == null)
                    continue;
                Int32 stride;
                Byte[] byteData = ImageUtils.GetImageData(frame.GetBitmap(), out stride, true);
                framesList[i] = byteData;
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 minOpenSpace = width;
                for (Int32 y = 0; y < height; ++y)
                {
                    Byte[] line = new Byte[width];
                    Array.Copy(byteData, y * stride, line, 0, width);
                    minOpenSpace = Math.Min(minOpenSpace, line.Reverse().TakeWhile(x => x == 0).Count());
                }
                globalOpenSpace = Math.Min(globalOpenSpace, minOpenSpace);
                if (globalOpenSpace == 0)
                    break;
            }
            if (globalOpenSpace > 0)
            {
                spaceWidth -= (Byte)globalOpenSpace;
                for (Int32 i = 0; i < 0x100; ++i)
                {
                    SupportedFileType frame = baseList[i];
                    if (frame == null || frame.Width == 0 && frame.Height == 0 || frame.GetBitmap() == null)
                        continue;
                    Int32 width = frame.Width;
                    Int32 height = frame.Height;
                    Byte[] byteData = framesList[i];
                    byteData = ImageUtils.ChangeStride(byteData, width, height, width - globalOpenSpace, false, 0);
                    framesList[i] = byteData;
                }
                // The global font width is not actually saved, so there's no use in adjusting it too.
            }
            Int32 fileLen = 0x408 + framesList.Select(x => (x == null ? 0 : x.Length) + 8).Sum();
            Byte[] fileData = new Byte[fileLen];
            fileData[0] = 0x01;
            fileData[1] = spaceWidth; // space width
            fileData[2] = firstSymbol;
            fileData[3] = (Byte)globalOpenSpace; // space between characters
            fileData[4] = (Byte)fontHeight;
            //fileData[5] = 0x00;
            //fileData[6] = 0x00;
            //fileData[7] = 0x00;
            //0x08 => 0x408: giant load of crap. Leave empty, I guess?
            Int32 writeOffset = 0x408;
            for (Int32 i = 0; i < 0x100; ++i)
            {
                SupportedFileType frame = baseList[i];
                Int32 width = frame == null || frame.Width == 0 ? 0 : (frame.Width - globalOpenSpace);
                Int32 height = frame == null ? 0 : frame.Height;
                ArrayUtils.WriteInt32ToByteArrayLe(fileData, writeOffset, width);
                writeOffset += 4;
                ArrayUtils.WriteInt32ToByteArrayLe(fileData, writeOffset, height);
                writeOffset += 4;
                Byte[] bdata = framesList[i];
                if (bdata != null)
                {
                    Array.Copy(bdata, 0, fileData, writeOffset, bdata.Length);
                    writeOffset += bdata.Length;
                }
            }
            return fileData;
        }
    }
}
