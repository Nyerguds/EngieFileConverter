using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nyerguds.FileData.Compression.Penguin;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesDogsDb : SupportedFileType
    {
        public static readonly Byte FlagByte = 0x1B;
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override String IdCode { get { return "AllDogsDb"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "All Dogs DB"; } }
        public override String[] FileExtensions { get { return new String[] { "db0", "db1", "db2", "db3", "db4", "db5", "db6" }; } }
        public override String ShortTypeDescription { get { return "All Dogs Go To Heaven DB file"; } }
        public override Boolean NeedsPalette { get { return false; } }
        public override Int32 BitsPerPixel { get { return m_bpp; } }
        protected Int32 m_bpp;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        protected Dictionary<Int32, String> m_textEntries = new Dictionary<Int32, String>();
        protected Dictionary<Int32, Int32> m_textLengths = new Dictionary<Int32, Int32>();

        protected Dictionary<Int32, String> GetTextEntries()
        {
            return m_textEntries;
        }

        protected Dictionary<Int32, Int32> GetTextLengths()
        {
            return m_textLengths;
        }

        public override void LoadFile(Byte[] fileData)
        {
            Boolean cgaLoadSucceeded = this.LoadFromFileData(fileData, null, false);
            if (!cgaLoadSucceeded)
                this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            Boolean cgaLoadSucceeded = this.LoadFromFileData(fileData, filename, false);
            if (!cgaLoadSucceeded)
                this.LoadFromFileData(fileData, null, true);
            this.SetFileNames(filename);
        }

        protected Boolean LoadFromFileData(Byte[] fileData, String sourcePath, Boolean forceEga)
        {
            Int32 datalen = fileData.Length;
            if (datalen < 2)
                throw new FileTypeLoadException("Bad header size.");
            Int32 nrOfFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            Int32 headerEnd = 2 + (nrOfFrames * 8);
            if (datalen < headerEnd)
                throw new FileTypeLoadException("File is too short to contain an index of " + nrOfFrames + " files");
            // No longer using filename method; it's annoying for testing.
            Boolean isCga = !forceEga; // && (sourcePath == null || Path.GetFileNameWithoutExtension(sourcePath).EndsWith("C", StringComparison.InvariantCultureIgnoreCase));
            ColorPalette cgaPal = null;
            if (isCga)
            {
                this.m_Palette = PaletteUtils.GetCgaPalette(0, true, true, true, 2);
                cgaPal = ImageUtils.GetPalette(this.m_Palette);
                m_bpp = 2;
            }
            else
            {
                this.m_Palette = PaletteUtils.GetEgaPalette(4);
                m_bpp = 4;
            }
            Int32 offset = 2;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            this.m_textEntries.Clear();
            this.m_textLengths.Clear();
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Int32 decompSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset);
                Int32 compSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 2);
                Int32 dataStart = (Int32)ArrayUtils.ReadUInt32FromByteArrayLe(fileData, offset + 4);
                if (dataStart < headerEnd)
                    throw new FileTypeLoadException("Frame " + i + " data starts before the header end.");
                offset += 8;
                Byte[] frameData;
                try
                {
                    Byte[] frameDataCopied = new Byte[compSize];
                    Array.Copy(fileData, dataStart, frameDataCopied, 0, compSize);
                    frameData = PenguinCompression.DecompressDogsFlagRle(frameDataCopied, 0, compSize, decompSize, FlagByte);
                    //frameData = PenguinCompression.DecompressDogsFlagRle(fileData, dataStart, compSize, decompSize, FlagByte);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, false) + " (frame " + i + ")", ex);
                }
                Int32 stride = ArrayUtils.ReadUInt16FromByteArrayLe(frameData, 0);
                Int32 height = ArrayUtils.ReadUInt16FromByteArrayLe(frameData, 2);
                Int32 imageSize = ArrayUtils.ReadUInt16FromByteArrayLe(frameData, 4);
                String textFrame = null;
                if (imageSize != decompSize - 6 || stride * height != imageSize)
                {
                    Int32 j = 0;
                    Boolean validAscii = true;
                    for (; j < decompSize; j++)
                    {
                        Byte cur = frameData[j];
                        if (cur == 0)
                            break;
                        // I doubt tab is allowed, but eh.
                        if ((cur < 0x20 && cur != 0x09) || cur >= 0x80)
                        {
                            validAscii = false;
                            break;
                        }
                    }
                    for (Int32 k = j; k < decompSize; ++k) 
                    {
                        if (frameData[j] != 0)
                        {
                            validAscii = false;
                            break;
                        }
                    }
                    if (validAscii)
                        textFrame = Encoding.ASCII.GetString(frameData, 0, j);
                    else
                        throw new FileTypeLoadException("Decompressed size in frame " + i + " does not match.");
                }
                List<String> extraInfo = new List<String>();
                Bitmap frameImage = null;
                if (textFrame != null)
                {
                    this.m_textEntries.Add(i, textFrame);
                    this.m_textLengths.Add(i, decompSize);

                    extraInfo.Add("Type: ASCII text entry");
                    extraInfo.Add("Value: \"" + textFrame + "\"");
                }
                else
                {
                    Int32 width = stride*(isCga ? 4 : 2);
                    // Try again as EGA.
                    if (width > 320)
                    {
                        if (isCga)
                            return false;
                        throw new FileTypeLoadException("Image width exceeds 320.");
                    }
                    Byte[] frameData2 = new Byte[imageSize];
                    Array.Copy(frameData, 6, frameData2, 0, imageSize);
                    if (isCga)
                    {
                        frameData2 = ImageUtils.ConvertTo8Bit(frameData2, width, height, 0, 2, true, ref stride);
                        frameData2 = ImageUtils.ConvertFrom8Bit(frameData2, width, height, 4, true, ref stride);
                    }
                    frameImage = ImageUtils.BuildImage(frameData2, width, height, stride, PixelFormat.Format4bppIndexed, this.m_Palette, Color.Black);
                    if (isCga)
                        frameImage.Palette = cgaPal;
                    extraInfo.Add("Type: " + (isCga ? "CGA" : "EGA") + " image frame");
                }

                if (compSize == decompSize)
                    extraInfo.Add("Uncompressed entry\nSize: " + decompSize + " bytes");
                else
                    extraInfo.Add("Compressed entry\nCompressed size: " + compSize + "\nDecompressed size: " + decompSize);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, sourcePath, i);
                frame.SetBitsPerColor(textFrame == null ? this.BitsPerPixel : 0);
                frame.SetNeedsPalette(false);
                frame.SetExtraInfo(String.Join("\n", extraInfo.ToArray()));
                this.m_FramesList[i] = frame;
            }
            this.ExtraInfo = "Type: " + (isCga ? "C" : "E") + "GA images";
            if (this.m_textEntries.Count > 0)
            {
                this.ExtraInfo += "\nContains text entries at frames " + GeneralUtils.GroupNumbers(this.m_textEntries.Keys) + ".";
            }
            this.m_LoadedImage = null;
            return true;
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 maxCol = 0;
            Boolean canBeCga = true;
            List<Int32> emptyEntries = new List<Int32>();
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            FileFramesDogsDb native = fileToSave as FileFramesDogsDb;
            Int32 nrOfFrames = frames.Length;
            for (int i = 0; i < nrOfFrames; i++)
            {
                SupportedFileType frame;
                Bitmap bm;
                if ((frame = frames[i]) == null || (bm = frame.GetBitmap()) == null)
                {
                    emptyEntries.Add(i);
                    continue;
                }
                if (frame.Width > 320)
                    throw new ArgumentException("The images can't exceed a width of 320 pixels.", "fileToSave");
                if (frame.Height > 200)
                    throw new ArgumentException("The images can't exceed a height of 200 pixels.", "fileToSave");
                if (frame.GetColors().Length == 4)
                {
                    // Don't actually check 4 colour images.
                    maxCol = 3;
                    continue;
                }
                Byte[] pixels = ImageUtils.GetImageData(bm, PixelFormat.Format8bppIndexed);
                Int32 len = pixels.Length;
                for (Int32 p = 0; p < len; ++p)
                {
                    maxCol = Math.Max(maxCol, pixels[p]);
                    if (maxCol >= 16)
                        throw new ArgumentException("The images contain colours on indices higher than 15.", "fileToSave");
                    if (maxCol >= 4)
                        canBeCga = false;

                }
            }
            String[] empty = null;
            Int32 emptyAmount = emptyEntries.Count;
            if (emptyAmount > 0)
            {
                Dictionary<Int32, String> textEntries = native == null ? null : native.GetTextEntries();
                Dictionary<Int32, Int32> textLengths = native == null ? null : native.GetTextLengths();
                if (textEntries == null || textEntries.Keys.Count == 0)
                {
                    empty = emptyEntries.Select(x => x + ":  : ").ToArray();
                }
                else
                {
                    empty = new String[emptyAmount];
                    for (int i = 0; i < emptyAmount; i++)
                    {
                        Int32 emptyNum = emptyEntries[i];
                        String textVal;
                        textEntries.TryGetValue(emptyNum, out textVal);
                        Int32 textLen;
                        textLengths.TryGetValue(emptyNum, out textLen);
                        empty[i] = emptyNum + " : " + (textLen != 0 ? textLen.ToString() : "len") + " : " + (textVal ?? String.Empty);
                    }
                }

            }
            Int32 nrOpts = 0;
            if (canBeCga)
                nrOpts++;
            if (empty != null)
                nrOpts++;
            SaveOption[] opts = new SaveOption[nrOpts];
            Int32 opt = 0;
            if (canBeCga)
                opts[opt++] = new SaveOption("CGA", SaveOptionType.Boolean, "Save as CGA", null, "1", true);
            if (empty != null)
                opts[opt++] = new SaveOption("TXT", SaveOptionType.String, "Text entries. Format: \"nr : size : text\". The easiest way to get this is to go to the save options of the original archive and copy it from the text field.", String.Join(Environment.NewLine, empty));

            return opts;
        }
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Boolean isCga = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CGA"));
            String emptyEntries = SaveOption.GetSaveOptionValue(saveOptions, "TXT");
            HashSet<Int32> emptyKeys = new HashSet<Int32>();
            Dictionary<Int32, String> textEntries = new Dictionary<Int32, String>();
            Dictionary<Int32, Int32> textLengths = new Dictionary<Int32, Int32>();
            if (emptyEntries != null)
            {
                String[] empties = emptyEntries.Split(new Char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                Regex emptyPattern = new Regex("^\\s*(\\d+)\\s*:\\s*(\\d*)\\s*:\\s*(.*?)\\s*$");
                for (int i = 0; i < empties.Length; i++)
                {
                    Match m = emptyPattern.Match(empties[i]);
                    if (m.Success)
                    {
                        Int32 index = Int32.Parse(m.Groups[1].Value);
                        if (m.Groups[2].Value.Length == 0)
                            throw new ArgumentException("Syntax for blank data is \"nr : size : text\".", "saveOptions");
                        Int32 length = Int32.Parse(m.Groups[2].Value);
                        String text = m.Groups[3].Value;
                        if (length < text.Length)
                            throw new ArgumentException("Text entry can't be longer than its entry length!", "saveOptions");
                        if (emptyKeys.Contains(index))
                            throw new ArgumentException("Duplicate detected in text entries!", "saveOptions");
                        emptyKeys.Add(index);
                        textEntries[index] = text;
                        textLengths[index] = length;
                    }
                }
            }
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] {fileToSave};
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException("No frames found in source data!", "fileToSave");
            if (nrOfFrames > 0xFFFF)
                throw new ArgumentException("Too many frames in source data!", "fileToSave");
            Int32 targetBpp = isCga ? 2 : 4;
            Byte[][] frameData = new Byte[nrOfFrames][];
            Int32 dataOffset = 2 + (nrOfFrames*8);
            Int32 offset = 2;
            Byte[] header = new Byte[dataOffset];
            ArrayUtils.WriteUInt16ToByteArrayLe(header, 0, (UInt16)nrOfFrames);
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Bitmap bm;
                Int32 uncompressedSize;
                Byte[] curFrameCompressed;
                if (frames[i] == null || (bm = frames[i].GetBitmap()) == null)
                {
                    // Text entry
                    if (!emptyKeys.Contains(i))
                        throw new ArgumentException("No text information given for empty entry " + i, "fileToSave");
                    String text = textEntries[i];
                    Int32 textLength = textLengths[i];
                    curFrameCompressed = new Byte[textLength];
                    uncompressedSize = textLength;
                    Byte[] textArr = Encoding.ASCII.GetBytes(text);
                    Array.Copy(textArr, curFrameCompressed, textArr.Length);
                }
                else
                {
                    Int32 width = bm.Width;
                    Int32 height = bm.Height;
                    Int32 stride;
                    Byte[] frameDataRaw = ImageUtils.GetImageData(bm, out stride, PixelFormat.Format8bppIndexed,true);
                    frameDataRaw = ImageUtils.ConvertFrom8Bit(frameDataRaw, width, height, targetBpp, true, ref stride);
                    uncompressedSize = frameDataRaw.Length + 6;
                    Byte[] frameDataFinal = new Byte[uncompressedSize];
                    ArrayUtils.WriteUInt16ToByteArrayLe(frameDataFinal, 0, (UInt16) stride);
                    ArrayUtils.WriteUInt16ToByteArrayLe(frameDataFinal, 2, (UInt16) height);
                    ArrayUtils.WriteUInt16ToByteArrayLe(frameDataFinal, 4, (UInt16)frameDataRaw.Length);
                    Array.Copy(frameDataRaw, 0, frameDataFinal, 6, frameDataRaw.Length);
                    curFrameCompressed = PenguinCompression.CompressDogsFlagRle(frameDataFinal, FlagByte);
                }
                frameData[i] = curFrameCompressed;
                ArrayUtils.WriteUInt16ToByteArrayLe(header, offset, (UInt16)uncompressedSize);
                ArrayUtils.WriteUInt16ToByteArrayLe(header, offset + 2, (UInt16)curFrameCompressed.Length);
                ArrayUtils.WriteUInt32ToByteArrayLe(header, offset + 4, (UInt32)dataOffset);
                dataOffset += curFrameCompressed.Length;
                offset += 8;
            }

            Byte[] fullData = new Byte[dataOffset];
            Array.Copy(header, fullData, header.Length);
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Byte[] curFrame = frameData[i];
                Int32 curLen = curFrame.Length;
                Array.Copy(frameData[i], 0, fullData, offset, curLen);
                offset += curLen;
            }
            return fullData;
        }
    }
}