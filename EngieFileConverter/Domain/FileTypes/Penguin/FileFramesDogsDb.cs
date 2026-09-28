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
        public static readonly byte FlagByte = 0x1B;
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override string IdCode { get { return "AllDogsDb"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "All Dogs DB"; } }
        public override string[] FileExtensions { get { return new string[] { "db0", "db1", "db2", "db3", "db4", "db5", "db6" }; } }
        public override string LongTypeName { get { return "All Dogs Go To Heaven DB file"; } }
        public override bool NeedsPalette { get { return this.m_bpp == -2; } }
        public override int BitsPerPixel { get { return this.m_bpp; } }
        protected int m_bpp;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return null; } }

        protected Dictionary<int, string> m_textEntries = new Dictionary<int, string>();
        protected Dictionary<int, int> m_textLengths = new Dictionary<int, int>();

        protected Dictionary<int, string> GetTextEntries()
        {
            return this.m_textEntries;
        }

        protected Dictionary<int, int> GetTextLengths()
        {
            return this.m_textLengths;
        }

        public override void LoadFile(byte[] fileData)
        {
            bool cgaLoadSucceeded = this.LoadFromFileData(fileData, null, false);
            if (!cgaLoadSucceeded)
                this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            bool cgaLoadSucceeded = this.LoadFromFileData(fileData, filename, false);
            if (!cgaLoadSucceeded)
                this.LoadFromFileData(fileData, null, true);
            this.SetFileNames(filename);
        }

        protected bool LoadFromFileData(byte[] fileData, string sourcePath, bool forceEga)
        {
            int datalen = fileData.Length;
            if (datalen < 2)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            int nrOfFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            if (nrOfFrames == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            int headerEnd = 2 + (nrOfFrames * 8);
            if (datalen < headerEnd)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            // No longer using filename method; it's annoying for testing.
            bool isCga = !forceEga;
            ColorPalette cgaPal = null;
            Color[] pal;
            if (isCga)
            {
                pal = PaletteUtils.GetCgaPalette(0, true, true, true, 2);
                cgaPal = ImageUtils.GetPalette(pal);
                this.m_bpp = -2;
            }
            else
            {
                pal = PaletteUtils.GetEgaPalette(4);
                this.m_bpp = 4;
            }
            int offset = 2;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            this.m_textEntries.Clear();
            this.m_textLengths.Clear();
            for (int i = 0; i < nrOfFrames; ++i)
            {
                int decompSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset);
                int compSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 2);
                int dataStart = (int)ArrayUtils.ReadUInt32FromByteArrayLe(fileData, offset + 4);
                if (dataStart < headerEnd)
                    throw new FileTypeLoadException("Frame " + i + " data starts before the header end.");
                if (dataStart + compSize > datalen)
                    throw new FileTypeLoadException( "Frame " + i + " data exceeds file.");
                offset += 8;
                byte[] frameData;
                byte[] frameDataCopied = new byte[compSize];
                Array.Copy(fileData, dataStart, frameDataCopied, 0, compSize);
                try
                {
                    frameData = PenguinCompression.DecompressDogsFlagRle(frameDataCopied, 0, compSize, decompSize, FlagByte);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR + " (frame {1})", GeneralUtils.RecoverArgExceptionMessage(ex, true), i), ex);
                }
                int stride = ArrayUtils.ReadUInt16FromByteArrayLe(frameData, 0);
                int height = ArrayUtils.ReadUInt16FromByteArrayLe(frameData, 2);
                int imageSize = ArrayUtils.ReadUInt16FromByteArrayLe(frameData, 4);
                string textFrame = null;
                if (imageSize != decompSize - 6 || stride * height != imageSize)
                {
                    int j = 0;
                    bool validAscii = true;
                    for (; j < decompSize; ++j)
                    {
                        byte cur = frameData[j];
                        if (cur == 0)
                            break;
                        // I doubt tab is allowed, but eh.
                        if ((cur < 0x20 && cur != 0x09) || cur >= 0x80)
                        {
                            validAscii = false;
                            break;
                        }
                    }
                    for (int k = j; k < decompSize; ++k) 
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
                // Using List<String> and not StringBuilder because it's easier to add line breaks in between.
                List<string> extraInfo = new List<string>();
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
                    int width = stride*(isCga ? 4 : 2);
                    // Try again as EGA.
                    if (width > 320)
                    {
                        if (isCga)
                            return false;
                        throw new FileTypeLoadException("Image width exceeds 320.");
                    }
                    byte[] frameData2 = new byte[imageSize];
                    Array.Copy(frameData, 6, frameData2, 0, imageSize);
                    if (isCga)
                    {
                        frameData2 = ImageUtils.ConvertTo8Bit(frameData2, width, height, 0, 2, true, ref stride);
                        frameData2 = ImageUtils.ConvertFrom8Bit(frameData2, width, height, 4, true, ref stride);
                    }
                    frameImage = ImageUtils.BuildImage(frameData2, width, height, stride, PixelFormat.Format4bppIndexed, pal, Color.Black);
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
                frame.SetNeedsPalette(this.m_bpp == -2);
                frame.SetExtraInfo(String.Join("\n", extraInfo.ToArray()));
                this.m_FramesList[i] = frame;
            }
            this.ExtraInfo = "Type: " + (isCga ? "C" : "E") + "GA images";
            if (this.m_textEntries.Count > 0)
            {
                this.ExtraInfo += "\nContains text entries at frames " + GeneralUtils.GroupNumbers(this.m_textEntries.Keys) + ".";
            }
            this.SetColors(pal);
            this.m_LoadedImage = null;
            return true;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            int maxCol = 0;
            bool canBeCga = true;
            List<int> emptyEntries = new List<int>();
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            FileFramesDogsDb native = fileToSave as FileFramesDogsDb;
            int nrOfFrames = frames.Length;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame;
                Bitmap bm;
                if ((frame = frames[i]) == null || (bm = frame.GetBitmap()) == null)
                {
                    emptyEntries.Add(i);
                    continue;
                }
                if (frame.Width > 320)
                    throw new FileTypeSaveException(ERR_DIMENSIONS_TOO_WIDE_DIM, 320);
                if (frame.Height > 200)
                    throw new FileTypeSaveException(ERR_DIMENSIONS_TOO_HIGH_DIM, 200);
                if (frame.GetColors().Length == 4)
                {
                    // Don't actually check 4 color images.
                    maxCol = 3;
                    continue;
                }
                byte[] pixels = ImageUtils.GetImageData(bm, PixelFormat.Format8bppIndexed);
                int len = pixels.Length;
                for (int p = 0; p < len; ++p)
                {
                    maxCol = Math.Max(maxCol, pixels[p]);
                    if (maxCol >= 16)
                        throw new FileTypeSaveException(ERR_BPP_LOW_INPUT, "4bpp or 2", 15);
                    if (maxCol >= 4)
                        canBeCga = false;

                }
            }
            string[] empty = null;
            int emptyAmount = emptyEntries.Count;
            if (emptyAmount > 0)
            {
                Dictionary<int, string> textEntries = native == null ? null : native.GetTextEntries();
                Dictionary<int, int> textLengths = native == null ? null : native.GetTextLengths();
                if (textEntries == null || textEntries.Keys.Count == 0)
                {
                    empty = emptyEntries.Select(x => x + " : 50 : ").ToArray();
                }
                else
                {
                    empty = new string[emptyAmount];
                    for (int i = 0; i < emptyAmount; ++i)
                    {
                        int emptyNum = emptyEntries[i];
                        string textVal;
                        textEntries.TryGetValue(emptyNum, out textVal);
                        int textLen;
                        textLengths.TryGetValue(emptyNum, out textLen);
                        empty[i] = emptyNum + " : " + (textLen != 0 ? textLen.ToString() : "50") + " : " + (textVal ?? String.Empty);
                    }
                }

            }
            int nrOpts = 0;
            if (canBeCga)
                nrOpts++;
            if (empty != null)
                nrOpts++;
            Option[] opts = new Option[nrOpts];
            int opt = 0;
            if (canBeCga)
                opts[opt++] = new Option("CGA", OptionInputType.Boolean, "Save as CGA", null, "1", true);
            if (empty != null)
                opts[opt++] = new Option("TXT", OptionInputType.String, "Text entries. Format: \"nr : size : text\". The easiest way to get this is to go to the save options of the original archive and copy it from the text field.", String.Join("," + Environment.NewLine, empty));

            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            bool isCga = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CGA"));
            string emptyEntries = Option.GetSaveOptionValue(saveOptions, "TXT");
            HashSet<int> emptyKeys = new HashSet<int>();
            Dictionary<int, string> textEntries = new Dictionary<int, string>();
            Dictionary<int, int> textLengths = new Dictionary<int, int>();
            if (emptyEntries != null)
            {
                string[] empties = emptyEntries.Split(new char[] {'\r', '\n', ',', ';'}, StringSplitOptions.RemoveEmptyEntries);
                Regex emptyPattern = new Regex("^\\s*(\\d+)\\s*:\\s*(\\d*)\\s*:\\s*(.*?)\\s*$");
                for (int i = 0; i < empties.Length; ++i)
                {
                    Match m = emptyPattern.Match(empties[i]);
                    if (m.Success)
                    {
                        int index = Int32.Parse(m.Groups[1].Value);
                        if (m.Groups[2].Value.Length == 0)
                            throw new FileTypeSaveException("Syntax for blank data is \"nr : size : text\".");
                        int length = Int32.Parse(m.Groups[2].Value);
                        string text = m.Groups[3].Value;
                        if (length < text.Length)
                            throw new FileTypeSaveException("Text entry can't be longer than its entry length.");
                        if (emptyKeys.Contains(index))
                            throw new FileTypeSaveException("Duplicate detected in text entries.");
                        emptyKeys.Add(index);
                        textEntries[index] = text;
                        textLengths[index] = length;
                    }
                }
            }
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] {fileToSave};
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new FileTypeSaveException("No frames found in source data.");
            if (nrOfFrames > 0xFFFF)
                throw new FileTypeSaveException("Too many frames in source data.");
            int targetBpp = isCga ? 2 : 4;
            byte[][] frameData = new byte[nrOfFrames][];
            int dataOffset = 2 + (nrOfFrames*8);
            int offset = 2;
            byte[] header = new byte[dataOffset];
            ArrayUtils.WriteUInt16ToByteArrayLe(header, 0, (ushort)nrOfFrames);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                Bitmap bm;
                int uncompressedSize;
                byte[] curFrameCompressed;
                if (frames[i] == null || (bm = frames[i].GetBitmap()) == null)
                {
                    // Text entry
                    if (!emptyKeys.Contains(i))
                        throw new ArgumentException("No text information given for empty entry " + i, "fileToSave");
                    string text = textEntries[i];
                    int textLength = textLengths[i];
                    curFrameCompressed = new byte[textLength];
                    uncompressedSize = textLength;
                    byte[] textArr = Encoding.ASCII.GetBytes(text);
                    Array.Copy(textArr, curFrameCompressed, textArr.Length);
                }
                else
                {
                    int width = bm.Width;
                    int height = bm.Height;
                    int stride;
                    byte[] frameDataRaw = ImageUtils.GetImageData(bm, out stride, PixelFormat.Format8bppIndexed,true);
                    frameDataRaw = ImageUtils.ConvertFrom8Bit(frameDataRaw, width, height, targetBpp, true, ref stride);
                    uncompressedSize = frameDataRaw.Length + 6;
                    byte[] frameDataFinal = new byte[uncompressedSize];
                    ArrayUtils.WriteUInt16ToByteArrayLe(frameDataFinal, 0, (ushort) stride);
                    ArrayUtils.WriteUInt16ToByteArrayLe(frameDataFinal, 2, (ushort) height);
                    ArrayUtils.WriteUInt16ToByteArrayLe(frameDataFinal, 4, (ushort)frameDataRaw.Length);
                    Array.Copy(frameDataRaw, 0, frameDataFinal, 6, frameDataRaw.Length);
                    try
                    {
                        curFrameCompressed = PenguinCompression.CompressDogsFlagRle(frameDataFinal, FlagByte, true);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
                    }
                }
                frameData[i] = curFrameCompressed;
                ArrayUtils.WriteUInt16ToByteArrayLe(header, offset, (ushort)uncompressedSize);
                ArrayUtils.WriteUInt16ToByteArrayLe(header, offset + 2, (ushort)curFrameCompressed.Length);
                ArrayUtils.WriteUInt32ToByteArrayLe(header, offset + 4, (uint)dataOffset);
                dataOffset += curFrameCompressed.Length;
                offset += 8;
            }

            byte[] fullData = new byte[dataOffset];
            Array.Copy(header, fullData, header.Length);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                byte[] curFrame = frameData[i];
                int curLen = curFrame.Length;
                Array.Copy(frameData[i], 0, fullData, offset, curLen);
                offset += curLen;
            }
            return fullData;
        }
    }
}