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
    public class FileFramesWwShpD2 : SupportedFileType, Dune2ShpType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return this.m_Width; } }
        public override int Height { get { return this.m_Height; } }
        protected int m_Width;
        protected int m_Height;
        public override string IdCode { get { return "WwShpD2"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood Dune II Shape"; } }
        public override string[] FileExtensions { get { return new string[] { "shp" }; } }
        public override string LongTypeName { get { return "Westwood Shape File - Dune II"; } }
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

        public bool IsVersion107 { get; set; }
        public int[] RemappedIndices { get; set; }
        public int[] UncompressedIndices { get; set; }
        protected readonly string GAMENAME = "Dune II";

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, this.GAMENAME);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename, this.GAMENAME);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath, string gameOverride)
        {
            bool isVersion107;
            int[] remapFrames;
            int[] notCompressedFrames;
            this.m_FramesList = LoadFromFileData(fileData, sourcePath, this, gameOverride, out isVersion107, out remapFrames, out notCompressedFrames);
            SupportedFileType frame0 = this.m_FramesList.FirstOrDefault();
            if (frame0 != null)
            {
                this.m_Palette = frame0.GetColors();
                this.m_Height = this.m_FramesList.Max(fr => fr.Height);
                this.m_Width = this.m_FramesList.Max(fr => fr.Width);
            }
            this.IsVersion107 = isVersion107;
            this.RemappedIndices = remapFrames;
            this.UncompressedIndices = notCompressedFrames;
            StringBuilder extraInfoGlobal = new StringBuilder();
            extraInfoGlobal.Append("Game version: ").Append(isVersion107 ? "v1.07" : "v1.00");
            extraInfoGlobal.Append("\nRemapped indices: ");
            if (remapFrames.Length == 0)
                extraInfoGlobal.Append("None");
            else
                extraInfoGlobal.AppendNumbersGrouped(remapFrames);
            extraInfoGlobal.Append("\nUncompressed indices: ");
            if (notCompressedFrames.Length == 0)
                extraInfoGlobal.Append("None");
            else
                extraInfoGlobal.AppendNumbersGrouped(notCompressedFrames);
            this.ExtraInfo = extraInfoGlobal.ToString();
        }

        public static SupportedFileType[] LoadFromFileData(byte[] fileData, string sourcePath, SupportedFileType target, string gameOverride, out bool isVersion107, out int[] remapFrames, out int[] notCompressedFrames)
        {
            // OffsetInfo / ShapeFileHeader
            if (fileData.Length < 6)
                throw new FileTypeLoadException("Not long enough for header.");
            int hdrFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            if (hdrFrames == 0)
                throw new FileTypeLoadException("Not a " + gameOverride + " SHP file");
            if (fileData.Length < 2 + (hdrFrames + 1) * 2)
                throw new FileTypeLoadException("Not long enough for frames index.");
            // Length. Done -2 because everything that follows is relative to the location after the header
            uint endoffset = (uint) fileData.Length;

            // test v1.00 first, since it might accidentally be possible that the offset 2x as far happens to contain data matching the file end address.
            // However, in 32-bit addressing, it is impossible for even partial addresses halfway down the array to ever match the file end value.
            if (endoffset < ushort.MaxValue && (endoffset >= 2 + (hdrFrames + 1) * 2 && ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2 + hdrFrames * 2) == endoffset))
                isVersion107 = false;
            else if (endoffset >= 2 + (hdrFrames + 1) * 4 && ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 2 + hdrFrames * 4) == endoffset - 2)
                isVersion107 = true;
            else
                throw new FileTypeLoadException("File size in header does not match; cannot detect version.");
            // v1.07 is relative to offsets array start, so the found end offset will be 2 lower.
            if (isVersion107)
                endoffset -= 2;

            SupportedFileType[] framesList = new SupportedFileType[hdrFrames];
            bool[] remapped = new bool[hdrFrames];
            bool[] notCompressed = new bool[hdrFrames];
            // Frames
            int curOffs = 2;
            int readLen = isVersion107 ? 4 : 2;
            Color[] palette = PaletteUtils.GenerateGrayPalette(8, new bool[] { true }, false);
            int nextOFfset = (int) ArrayUtils.ReadIntFromByteArray(fileData, curOffs, readLen, true);
            for (int i = 0; i < hdrFrames; ++i)
            {
                // Set current read address to previously-fetched "next entry" address
                int readOffset = nextOFfset;
                // Reached end; process completed.
                if (endoffset == readOffset)
                    break;
                // Check illegal values.
                if (readOffset <= 0 || readOffset + 0x0A > endoffset)
                    throw new FileTypeLoadException("Illegal address in frame indices.");

                // Set header ptr to next address
                curOffs += readLen;
                // Read next entry address, to act as end of current entry.
                nextOFfset = (int)ArrayUtils.ReadIntFromByteArray(fileData, curOffs, readLen, true);

                // Compensate for header size
                int realReadOffset = readOffset;
                if (isVersion107)
                    realReadOffset += 2;

                Dune2ShpFrameFlags frameFlags = (Dune2ShpFrameFlags)ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x00);
                byte frmSlices = fileData[realReadOffset + 0x02];
                ushort frmWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x03);
                byte frmHeight = fileData[realReadOffset + 0x05];
                // Size of all frame data: header, lookup table, and compressed data.
                ushort frmDataSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x06);
                ushort frmZeroCompressedSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x08);
                realReadOffset += 0x0A;
                // Bit 1: Contains remap palette
                // Bit 2: Don't decompress with LCW
                // Bit 3: Has custom remap palette size.
                bool hasRemap = (frameFlags & Dune2ShpFrameFlags.HasRemapTable) != 0;
                bool noLcw = (frameFlags & Dune2ShpFrameFlags.NoLcw) != 0;
                notCompressed[i] = noLcw;
                bool customRemap = (frameFlags & Dune2ShpFrameFlags.CustomSizeRemap) != 0;
                remapped[i] = hasRemap;
                int curEndOffset = readOffset + frmDataSize;
                if (curEndOffset > endoffset) // curEndOffset > nextOFfset
                    throw new FileTypeLoadException("Illegal address in frame indices.");
                // I assume this is illegal...?
                if (frmWidth == 0 || frmHeight == 0)
                    throw new FileTypeLoadException("Illegal values in frame header.");

                int remapSize;
                byte[] remapTable;
                if (hasRemap)
                {
                    if (customRemap)
                    {
                        remapSize = fileData[realReadOffset];
                        realReadOffset++;
                    }
                    else
                        remapSize = 16;
                    remapTable = new byte[remapSize];
                    Array.Copy(fileData, realReadOffset, remapTable, 0, remapSize);
                    realReadOffset += remapSize;
                }
                else
                {
                    remapSize = 0;
                    remapTable = null;
                    // Dunno if this should be done?
                    if (customRemap)
                        realReadOffset++;
                }
                byte[] zeroDecompressData = new byte[frmZeroCompressedSize];
                if (noLcw)
                {
                    Array.Copy(fileData, realReadOffset, zeroDecompressData, 0, frmZeroCompressedSize);
                }
                else
                {
                    byte[] lcwDecompressData = new byte[frmZeroCompressedSize * 3];
                    int predictedEndOff = realReadOffset + frmDataSize - remapSize;
                    if (customRemap)
                        predictedEndOff--;
                    int lcwReadOffset = realReadOffset;
                    int decompressedSize = WWCompression.LcwDecompress(fileData, ref lcwReadOffset, lcwDecompressData, 0);
                    if (decompressedSize != frmZeroCompressedSize)
                        throw new FileTypeLoadException("LCW decompression failed.");
                    if (lcwReadOffset > predictedEndOff)
                        throw new FileTypeLoadException("LCW decompression exceeded data bounds.");
                    Array.Copy(lcwDecompressData, zeroDecompressData, frmZeroCompressedSize);

                }
                int refOffs = 0;
                byte[] fullFrame;
                try
                {
                    fullFrame = WestwoodRleZero.DecompressRleZeroD2(zeroDecompressData, ref refOffs, frmWidth, frmSlices);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR + " (frame {1})", GeneralUtils.RecoverArgExceptionMessage(ex, true), i), ex);
                }
                if (remapTable != null)
                {
                    byte[] remap = remapTable;
                    int remapLen = remap.Length;
                    for(int j = 0; j < fullFrame.Length; ++j)
                    {
                        byte val = fullFrame[j];
                        if (val < remapLen)
                            fullFrame[j] = remap[val];
                        else
                            throw new FileTypeLoadException("Remapping failed: value is larger than remap table.");
                    }
                }
                // Convert frame data to image and frame object
                Bitmap curFrImg = ImageUtils.BuildImage(fullFrame, frmWidth, frmHeight, frmWidth, PixelFormat.Format8bppIndexed, palette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(target, target, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(target.BitsPerPixel);
                framePic.SetFileClass(FileClass.Image8Bit);
                framePic.SetNeedsPalette(target.NeedsPalette);
                StringBuilder sbFrInfo = new StringBuilder();
                sbFrInfo.Append("Flags: ");
                sbFrInfo.Append(Convert.ToString((int)frameFlags & 0xFF, 2).PadLeft(8, '0')).Append(" (");
                bool hasData = false;
                if (hasRemap)
                {
                    sbFrInfo.Append("Remap");
                    hasData = true;
                }
                if (noLcw)
                {
                    if (hasData)
                        sbFrInfo.Append(", ");
                    sbFrInfo.Append("No LCW");
                    hasData = true;
                }
                if (customRemap)
                {
                    if (hasData)
                        sbFrInfo.Append(", ");
                    sbFrInfo.Append("Table size: ").Append(remapSize);
                }
                if (frameFlags == Dune2ShpFrameFlags.Empty)
                    sbFrInfo.Append("None");
                sbFrInfo.Append(")");
                sbFrInfo.Append("\nData size: ").Append(frmDataSize).Append(" bytes @ ").Append(realReadOffset);
                if (hasRemap)
                    sbFrInfo.Append("\nRemap table: ").Append(String.Join(" ", remapTable.Select(b => b.ToString("X2")).ToArray()));
                framePic.SetExtraInfo(sbFrInfo.ToString());
                framesList[i] = framePic;
            }
            remapFrames = Enumerable.Range(0, hdrFrames).Where(i => remapped[i]).ToArray();
            notCompressedFrames = Enumerable.Range(0, hdrFrames).Where(i => notCompressed[i]).ToArray();
            return framesList;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            PerformPreliminaryChecks(fileToSave);
            Dune2ShpType d2File = fileToSave as Dune2ShpType;
            bool isDunev100 = d2File != null && !d2File.IsVersion107;
            bool hasRemap = d2File != null && d2File.RemappedIndices != null && d2File.RemappedIndices.Length > 0;
            string remapped = hasRemap ? GeneralUtils.GroupNumbers(d2File.RemappedIndices) : String.Empty;
            bool hasUncompressed = d2File != null && d2File.UncompressedIndices != null && d2File.UncompressedIndices.Length > 0;
            string uncompressed = hasUncompressed ? GeneralUtils.GroupNumbers(d2File.UncompressedIndices) : String.Empty;

            return new Option[]
            {
                new Option("VER", OptionInputType.ChoicesList, "Game version", "v1.00,v1.07", isDunev100 ? "0" : "1"),
                // Remap tables allow units to be remapped. Seems house remap is only applied to those tables, not the whole graphic.
                new Option("RMT", OptionInputType.Boolean, "Add remapping tables to allow frames to be remapped to House colors.", hasRemap ? "1" : "0"),
                new Option("RMA", OptionInputType.Boolean, "Auto-detect remap on the existence of color indices 144-150.", null, "0", new EnableFilter("RMT", true, "1")),
                new Option("RMS", OptionInputType.String, "Specify remapped indices (Comma separated. Can use ranges like \"0-20\"). Leave empty to remap all.", "0123456789-, " + Environment.NewLine, remapped, new EnableFilter("RMA", false, "1")),
                new Option("NCA", OptionInputType.Boolean, "Auto-detect best compression usage.", "1"),
                new Option("NCS", OptionInputType.String, "Specify non-compressed indices (Comma separated. Can use ranges like \"0-20\"). Leave empty to treat all as non-compressed.", "0123456789-, " + Environment.NewLine, uncompressed, new EnableFilter("NCA", true, "0"))
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave);
            // VErsions: 1.00, 1.07 and Lands of Lore (which is 1.07 without LCW compression)
            int version;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "VER"), out version);

            bool isVersion107 = version != 0;
            // Remap tables allow units to be remapped. Seems house remap is only applied to those tables, not the whole graphic.
            bool addRemap = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "RMT"));
            bool addRemapAuto = addRemap && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "RMA"));
            string remapSpecificStr = Option.GetSaveOptionValue(saveOptions, "RMS");
            bool remapAll = addRemap && !addRemapAuto && String.IsNullOrEmpty(remapSpecificStr);
            int[] remappedFrames = addRemap && !addRemapAuto && !remapAll ? GeneralUtils.GetRangedNumbers(remapSpecificStr) : null;
            bool compressAuto = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "NCA"));
            string uncomprSpecificStr = Option.GetSaveOptionValue(saveOptions, "NCS");
            int[] uncompFrames = compressAuto ? null : GeneralUtils.GetRangedNumbers(uncomprSpecificStr);
            int nrOfFrames = frames.Length;
            bool[] remapFrame = new bool[nrOfFrames];
            if (addRemap)
            {
                if (remapAll || remappedFrames.Length == 0)
                {
                    for (int i = 0; i < nrOfFrames; ++i)
                        remapFrame[i] = true;
                }
                else
                {
                    int remapLen = remappedFrames.Length;
                    for (int i = 0; i < remapLen; ++i)
                    {
                        int remappedFrameIndex = remappedFrames[i];
                        if (remappedFrameIndex >= 0 && remappedFrameIndex < nrOfFrames)
                            remapFrame[remappedFrameIndex] = true;
                    }
                }
            }
            bool[] dontCompress = new bool[nrOfFrames];
            if (!compressAuto)
            {
                if (uncompFrames.Length == 0)
                {
                    for (int i = 0; i < nrOfFrames; ++i)
                        dontCompress[i] = true;
                }
                else
                {
                    int noCompLen = uncompFrames.Length;
                    for (int i = 0; i < noCompLen; ++i)
                    {
                        int noCompFrameIndex = uncompFrames[i];
                        if (noCompFrameIndex >= 0 && noCompFrameIndex < nrOfFrames)
                            dontCompress[noCompFrameIndex] = true;
                    }
                }
            }
            int addressSize = isVersion107 ? 4 : 2;
            int offset = addressSize * (nrOfFrames + 1);
            if (!isVersion107)
                offset += 2;
            int[] header = new int[nrOfFrames];
            byte[][] frameImage = new byte[nrOfFrames][];
            bool[] frameRemapped = new bool[nrOfFrames];
            byte[][] frameHeaders = new byte[nrOfFrames][];
            byte[][] frameData = new byte[nrOfFrames][];
            //ArrayUtils.WriteInt16ToByteArrayLe(header, 0, frames);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm = frame.GetBitmap();
                int frmWidth = bm.Width;
                int frmHeight = bm.Height;
                int stride;
                byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                int imageDataLength = imageData.Length;
                bool remapThis;
                if (addRemapAuto)
                {
                    remapThis = false;
                    for (int j = 0; j < imageDataLength; ++j)
                    {
                        byte b = imageData[j];
                        if (b < 144 || b > 150)
                            continue;
                        remapThis = true;
                        break;
                    }
                }
                else
                    remapThis = remapFrame[i];
                frameRemapped[i] = remapThis;
                // Check if any of the already-handled frames equals this one.
                int dupeIndex = -1;
                for (int j = 0; j < i; ++j)
                {
                    SupportedFileType prevFrame = frames[j];
                    if (prevFrame.Width != frmWidth || prevFrame.Height != frmHeight || frameRemapped[j] != remapThis)
                        continue;
                    if (!ArrayUtils.ArraysAreEqual(frameImage[j], imageData))
                        continue;
                    dupeIndex = j;
                    break;
                }
                if (dupeIndex != -1)
                {
                    // Same dimensions and same remap handling. Just copy the data and move on to the next frame.
                    frameHeaders[i] = frameHeaders[dupeIndex];
                    frameData[i] = null;
                    header[i] = header[dupeIndex];
                    continue;
                }
                // Needs to be a duplicate; otherwise the remapping system messes up the reference array.
                frameImage[i] = new byte[imageDataLength];
                Array.Copy(imageData, frameImage[i], imageDataLength);
                byte[] remapTable;
                bool largeTable;
                if (!remapThis)
                {
                    remapTable = null;
                    largeTable = false;
                }
                else
                {
                    // Remap table: get distinct values, remove zero to put it at the front.
                    byte[] noZeroRemapTable = imageData.Distinct().Where(b => b != 0).ToArray();
                    int tableLength = noZeroRemapTable.Length + 1;
                    remapTable = new byte[Math.Max(tableLength, 16)];
                    Array.Copy(noZeroRemapTable, 0, remapTable, 1, noZeroRemapTable.Length);
                    // Remap the image data
                    byte[] reverseTable = new byte[0x100];
                    for (int r = 1; r < tableLength; ++r)
                        reverseTable[remapTable[r]] = (byte) r;
                    for (int j = 0; j < imageData.Length; ++j)
                        imageData[j] = reverseTable[imageData[j]];
                    largeTable = tableLength > 16;
                }
                imageData = WestwoodRleZero.CompressRleZeroD2(imageData, frmWidth, frmHeight);
                int zeroDataLen = imageData.Length;
                byte[] lcwData = dontCompress[i] ? null : WWCompression.LcwCompress(imageData);
                bool isCompressed = lcwData != null && lcwData.Length < imageData.Length;
                if (isCompressed)
                    imageData = lcwData;
                // Write header. Remap table will be considered part of the header to avoid extra copies to add it to the image data.
                int frameHeaderLen = 0x0A;
                if (remapThis)
                {
                    if (largeTable)
                        frameHeaderLen++;
                    frameHeaderLen += remapTable.Length;
                }
                byte[] frameHeader = new byte[frameHeaderLen];
                Dune2ShpFrameFlags flags = Dune2ShpFrameFlags.Empty;
                if (!isCompressed)
                    flags |= Dune2ShpFrameFlags.NoLcw;
                if (remapThis)
                    flags |= Dune2ShpFrameFlags.HasRemapTable;
                if (largeTable)
                    flags |= Dune2ShpFrameFlags.CustomSizeRemap;
                // The entire data length; header plus table plus byte for table size plus compressed data.
                int frmDataSize = frameHeaderLen + imageData.Length;
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x00, (ushort)flags);
                frameHeader[0x02] = (byte) frmHeight;
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x03, (ushort)frmWidth);
                frameHeader[0x05] = (byte) frmHeight;
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x06, (ushort)frmDataSize);
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x08, (ushort)zeroDataLen);
                if (remapThis)
                {
                    int writeOffs = 0x0A;
                    if (largeTable)
                    {
                        frameHeader[writeOffs] = (byte) remapTable.Length;
                        writeOffs++;
                    }
                    Array.Copy(remapTable, 0, frameHeader, writeOffs, remapTable.Length);
                }
                frameHeaders[i] = frameHeader;
                frameData[i] = imageData;
                header[i] = offset;
                offset += frmDataSize;
            }
            int actualLen = offset;
            if (isVersion107)
                actualLen += 2;
            byte[] finalData = new byte[actualLen];
            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0, (ushort)nrOfFrames);
            int headerOffset = 2;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                int currentOffset = header[i];
                ArrayUtils.WriteIntToByteArray(finalData, headerOffset, addressSize, true, (uint) currentOffset);
                if (isVersion107)
                    currentOffset += 2;
                headerOffset += addressSize;
                byte[] frHeader = frameHeaders[i];
                int headerLen = frHeader.Length;
                Array.Copy(frHeader, 0, finalData, currentOffset, headerLen);
                currentOffset += headerLen;
                byte[] frData = frameData[i];
                if (frData != null)
                    Array.Copy(frData, 0, finalData, currentOffset, frData.Length);
            }
            // Add final length to frame offsets list.
            ArrayUtils.WriteIntToByteArray(finalData, headerOffset, addressSize, true, (uint)offset);
            return finalData;
        }

        public static SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_FRAMES_EMPTY, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
            }
            return frames;
        }

        [Flags]
        private enum Dune2ShpFrameFlags
        {
            Empty = 0x00,
            // Bit 1: Contains remap table
            HasRemapTable = 0x01,
            // Bit 2: Don't decompress with LCW
            NoLcw = 0x02,
            // Bit 3: Has custom remap table size.
            CustomSizeRemap = 0x04
        }
    }

    public interface Dune2ShpType
    {
        bool IsVersion107 { get; set; }
        int[] RemappedIndices { get; set; }
        int[] UncompressedIndices { get; set; }
    }
}