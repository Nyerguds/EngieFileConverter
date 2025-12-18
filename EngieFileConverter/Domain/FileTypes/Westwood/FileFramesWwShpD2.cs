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

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwShpD2"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood Dune II Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String LongTypeName { get { return "Westwood Shape File - Dune II"; } }
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

        public Boolean IsVersion107 { get; set; }
        public Int32[] RemappedIndices { get; set; }
        public Int32[] UncompressedIndices { get; set; }
        protected readonly String GAMENAME = "Dune II";

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, this.GAMENAME);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename, this.GAMENAME);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath, String gameOverride)
        {
            Boolean isVersion107;
            Int32[] remapFrames;
            Int32[] notCompressedFrames;
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

        public static SupportedFileType[] LoadFromFileData(Byte[] fileData, String sourcePath, SupportedFileType target, String gameOverride, out Boolean isVersion107, out Int32[] remapFrames, out Int32[] notCompressedFrames)
        {
            // OffsetInfo / ShapeFileHeader
            if (fileData.Length < 6)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 hdrFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            if (hdrFrames == 0)
                throw new FileTypeLoadException("Not a " + gameOverride + " SHP file");
            if (fileData.Length < 2 + (hdrFrames + 1) * 2)
                throw new FileTypeLoadException("Not long enough for frames index.");
            // Length. Done -2 because everything that follows is relative to the location after the header
            UInt32 endoffset = (UInt32) fileData.Length;

            // test v1.00 first, since it might accidentally be possible that the offset 2x as far happens to contain data matching the file end address.
            // However, in 32-bit addressing, it is impossible for even partial addresses halfway down the array to ever match the file end value.
            if (endoffset < UInt16.MaxValue && (endoffset >= 2 + (hdrFrames + 1) * 2 && ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2 + hdrFrames * 2) == endoffset))
                isVersion107 = false;
            else if (endoffset >= 2 + (hdrFrames + 1) * 4 && ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 2 + hdrFrames * 4) == endoffset - 2)
                isVersion107 = true;
            else
                throw new FileTypeLoadException("File size in header does not match; cannot detect version.");
            // v1.07 is relative to offsets array start, so the found end offset will be 2 lower.
            if (isVersion107)
                endoffset -= 2;

            SupportedFileType[] framesList = new SupportedFileType[hdrFrames];
            Boolean[] remapped = new Boolean[hdrFrames];
            Boolean[] notCompressed = new Boolean[hdrFrames];
            // Frames
            Int32 curOffs = 2;
            Int32 readLen = isVersion107 ? 4 : 2;
            Color[] palette = PaletteUtils.GenerateGrayPalette(8, new Boolean[] { true }, false);
            Int32 nextOFfset = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, curOffs, readLen, true);
            for (Int32 i = 0; i < hdrFrames; ++i)
            {
                // Set current read address to previously-fetched "next entry" address
                Int32 readOffset = nextOFfset;
                // Reached end; process completed.
                if (endoffset == readOffset)
                    break;
                // Check illegal values.
                if (readOffset <= 0 || readOffset + 0x0A > endoffset)
                    throw new FileTypeLoadException("Illegal address in frame indices.");

                // Set header ptr to next address
                curOffs += readLen;
                // Read next entry address, to act as end of current entry.
                nextOFfset = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, curOffs, readLen, true);

                // Compensate for header size
                Int32 realReadOffset = readOffset;
                if (isVersion107)
                    realReadOffset += 2;

                Dune2ShpFrameFlags frameFlags = (Dune2ShpFrameFlags)ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x00);
                Byte frmSlices = fileData[realReadOffset + 0x02];
                UInt16 frmWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x03);
                Byte frmHeight = fileData[realReadOffset + 0x05];
                // Size of all frame data: header, lookup table, and compressed data.
                UInt16 frmDataSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x06);
                UInt16 frmZeroCompressedSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, realReadOffset + 0x08);
                realReadOffset += 0x0A;
                // Bit 1: Contains remap palette
                // Bit 2: Don't decompress with LCW
                // Bit 3: Has custom remap palette size.
                Boolean hasRemap = (frameFlags & Dune2ShpFrameFlags.HasRemapTable) != 0;
                Boolean noLcw = (frameFlags & Dune2ShpFrameFlags.NoLcw) != 0;
                notCompressed[i] = noLcw;
                Boolean customRemap = (frameFlags & Dune2ShpFrameFlags.CustomSizeRemap) != 0;
                remapped[i] = hasRemap;
                Int32 curEndOffset = readOffset + frmDataSize;
                if (curEndOffset > endoffset) // curEndOffset > nextOFfset
                    throw new FileTypeLoadException("Illegal address in frame indices.");
                // I assume this is illegal...?
                if (frmWidth == 0 || frmHeight == 0)
                    throw new FileTypeLoadException("Illegal values in frame header!");

                Int32 remapSize;
                Byte[] remapTable;
                if (hasRemap)
                {
                    if (customRemap)
                    {
                        remapSize = fileData[realReadOffset];
                        realReadOffset++;
                    }
                    else
                        remapSize = 16;
                    remapTable = new Byte[remapSize];
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
                Byte[] zeroDecompressData = new Byte[frmZeroCompressedSize];
                if (noLcw)
                {
                    Array.Copy(fileData, realReadOffset, zeroDecompressData, 0, frmZeroCompressedSize);
                }
                else
                {
                    Byte[] lcwDecompressData = new Byte[frmZeroCompressedSize * 3];
                    Int32 predictedEndOff = realReadOffset + frmDataSize - remapSize;
                    if (customRemap)
                        predictedEndOff--;
                    Int32 lcwReadOffset = realReadOffset;
                    Int32 decompressedSize = WWCompression.LcwDecompress(fileData, ref lcwReadOffset, lcwDecompressData, 0);
                    if (decompressedSize != frmZeroCompressedSize)
                        throw new FileTypeLoadException("LCW decompression failed.");
                    if (lcwReadOffset > predictedEndOff)
                        throw new FileTypeLoadException("LCW decompression exceeded data bounds!");
                    Array.Copy(lcwDecompressData, zeroDecompressData, frmZeroCompressedSize);

                }
                Int32 refOffs = 0;
                Byte[] fullFrame = WestwoodRleZero.DecompressRleZeroD2(zeroDecompressData, ref refOffs, frmWidth, frmSlices);
                if (remapTable != null)
                {
                    Byte[] remap = remapTable;
                    Int32 remapLen = remap.Length;
                    for(Int32 j = 0; j < fullFrame.Length; ++j)
                    {
                        Byte val = fullFrame[j];
                        if (val < remapLen)
                            fullFrame[j] = remap[val];
                        else
                            throw new FileTypeLoadException("Remapping failed: value is larger than remap table!");
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
                sbFrInfo.Append(Convert.ToString((Int32)frameFlags & 0xFF, 2).PadLeft(8, '0')).Append(" (");
                Boolean hasData = false;
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

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            PerformPreliminaryChecks(fileToSave);
            Dune2ShpType d2File = fileToSave as Dune2ShpType;
            Boolean isDunev100 = d2File != null && !d2File.IsVersion107;
            Boolean hasRemap = d2File != null && d2File.RemappedIndices != null && d2File.RemappedIndices.Length > 0;
            String remapped = hasRemap ? GeneralUtils.GroupNumbers(d2File.RemappedIndices) : String.Empty;
            Boolean hasUncompressed = d2File != null && d2File.UncompressedIndices != null && d2File.UncompressedIndices.Length > 0;
            String uncompressed = hasUncompressed ? GeneralUtils.GroupNumbers(d2File.UncompressedIndices) : String.Empty;

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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave);
            // VErsions: 1.00, 1.07 and Lands of Lore (which is 1.07 without LCW compression)
            Int32 version;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "VER"), out version);

            Boolean isVersion107 = version != 0;
            // Remap tables allow units to be remapped. Seems house remap is only applied to those tables, not the whole graphic.
            Boolean addRemap = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "RMT"));
            Boolean addRemapAuto = addRemap && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "RMA"));
            String remapSpecificStr = Option.GetSaveOptionValue(saveOptions, "RMS");
            Boolean remapAll = addRemap && !addRemapAuto && String.IsNullOrEmpty(remapSpecificStr);
            Int32[] remappedFrames = addRemap && !addRemapAuto && !remapAll ? GeneralUtils.GetRangedNumbers(remapSpecificStr) : null;
            Boolean compressAuto = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "NCA"));
            String uncomprSpecificStr = Option.GetSaveOptionValue(saveOptions, "NCS");
            Int32[] uncompFrames = compressAuto ? null : GeneralUtils.GetRangedNumbers(uncomprSpecificStr);
            Int32 nrOfFrames = frames.Length;
            Boolean[] remapFrame = new Boolean[nrOfFrames];
            if (addRemap)
            {
                if (remapAll || remappedFrames.Length == 0)
                {
                    for (Int32 i = 0; i < nrOfFrames; ++i)
                        remapFrame[i] = true;
                }
                else
                {
                    Int32 remapLen = remappedFrames.Length;
                    for (Int32 i = 0; i < remapLen; ++i)
                    {
                        Int32 remappedFrameIndex = remappedFrames[i];
                        if (remappedFrameIndex >= 0 && remappedFrameIndex < nrOfFrames)
                            remapFrame[remappedFrameIndex] = true;
                    }
                }
            }
            Boolean[] dontCompress = new Boolean[nrOfFrames];
            if (!compressAuto)
            {
                if (uncompFrames.Length == 0)
                {
                    for (Int32 i = 0; i < nrOfFrames; ++i)
                        dontCompress[i] = true;
                }
                else
                {
                    Int32 noCompLen = uncompFrames.Length;
                    for (Int32 i = 0; i < noCompLen; ++i)
                    {
                        Int32 noCompFrameIndex = uncompFrames[i];
                        if (noCompFrameIndex >= 0 && noCompFrameIndex < nrOfFrames)
                            dontCompress[noCompFrameIndex] = true;
                    }
                }
            }
            Int32 addressSize = isVersion107 ? 4 : 2;
            Int32 offset = addressSize * (nrOfFrames + 1);
            if (!isVersion107)
                offset += 2;
            Int32[] header = new Int32[nrOfFrames];
            Byte[][] frameImage = new Byte[nrOfFrames][];
            Boolean[] frameRemapped = new Boolean[nrOfFrames];
            Byte[][] frameHeaders = new Byte[nrOfFrames][];
            Byte[][] frameData = new Byte[nrOfFrames][];
            //ArrayUtils.WriteInt16ToByteArrayLe(header, 0, frames);
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm = frame.GetBitmap();
                Int32 frmWidth = bm.Width;
                Int32 frmHeight = bm.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                Int32 imageDataLength = imageData.Length;
                Boolean remapThis;
                if (addRemapAuto)
                {
                    remapThis = false;
                    for (Int32 j = 0; j < imageDataLength; ++j)
                    {
                        Byte b = imageData[j];
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
                Int32 dupeIndex = -1;
                for (Int32 j = 0; j < i; ++j)
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
                frameImage[i] = new Byte[imageDataLength];
                Array.Copy(imageData, frameImage[i], imageDataLength);
                Byte[] remapTable;
                Boolean largeTable;
                if (!remapThis)
                {
                    remapTable = null;
                    largeTable = false;
                }
                else
                {
                    // Remap table: get distinct values, remove zero to put it at the front.
                    Byte[] noZeroRemapTable = imageData.Distinct().Where(b => b != 0).ToArray();
                    Int32 tableLength = noZeroRemapTable.Length + 1;
                    remapTable = new Byte[Math.Max(tableLength, 16)];
                    Array.Copy(noZeroRemapTable, 0, remapTable, 1, noZeroRemapTable.Length);
                    // Remap the image data
                    Byte[] reverseTable = new Byte[0x100];
                    for (Int32 r = 1; r < tableLength; ++r)
                        reverseTable[remapTable[r]] = (Byte) r;
                    for (Int32 j = 0; j < imageData.Length; ++j)
                        imageData[j] = reverseTable[imageData[j]];
                    largeTable = tableLength > 16;
                }
                imageData = WestwoodRleZero.CompressRleZeroD2(imageData, frmWidth, frmHeight);
                Int32 zeroDataLen = imageData.Length;
                Byte[] lcwData = dontCompress[i] ? null : WWCompression.LcwCompress(imageData);
                Boolean isCompressed = lcwData != null && lcwData.Length < imageData.Length;
                if (isCompressed)
                    imageData = lcwData;
                // Write header. Remap table will be considered part of the header to avoid extra copies to add it to the image data.
                Int32 frameHeaderLen = 0x0A;
                if (remapThis)
                {
                    if (largeTable)
                        frameHeaderLen++;
                    frameHeaderLen += remapTable.Length;
                }
                Byte[] frameHeader = new Byte[frameHeaderLen];
                Dune2ShpFrameFlags flags = Dune2ShpFrameFlags.Empty;
                if (!isCompressed)
                    flags |= Dune2ShpFrameFlags.NoLcw;
                if (remapThis)
                    flags |= Dune2ShpFrameFlags.HasRemapTable;
                if (largeTable)
                    flags |= Dune2ShpFrameFlags.CustomSizeRemap;
                // The entire data length; header plus table plus byte for table size plus compressed data.
                Int32 frmDataSize = frameHeaderLen + imageData.Length;
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x00, (UInt16)flags);
                frameHeader[0x02] = (Byte) frmHeight;
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x03, (UInt16)frmWidth);
                frameHeader[0x05] = (Byte) frmHeight;
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x06, (UInt16)frmDataSize);
                ArrayUtils.WriteUInt16ToByteArrayLe(frameHeader, 0x08, (UInt16)zeroDataLen);
                if (remapThis)
                {
                    Int32 writeOffs = 0x0A;
                    if (largeTable)
                    {
                        frameHeader[writeOffs] = (Byte) remapTable.Length;
                        writeOffs++;
                    }
                    Array.Copy(remapTable, 0, frameHeader, writeOffs, remapTable.Length);
                }
                frameHeaders[i] = frameHeader;
                frameData[i] = imageData;
                header[i] = offset;
                offset += frmDataSize;
            }
            Int32 actualLen = offset;
            if (isVersion107)
                actualLen += 2;
            Byte[] finalData = new Byte[actualLen];
            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0, (UInt16)nrOfFrames);
            Int32 headerOffset = 2;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Int32 currentOffset = header[i];
                ArrayUtils.WriteIntToByteArray(finalData, headerOffset, addressSize, true, (UInt32) currentOffset);
                if (isVersion107)
                    currentOffset += 2;
                headerOffset += addressSize;
                Byte[] frHeader = frameHeaders[i];
                Int32 headerLen = frHeader.Length;
                Array.Copy(frHeader, 0, finalData, currentOffset, headerLen);
                currentOffset += headerLen;
                Byte[] frData = frameData[i];
                if (frData != null)
                    Array.Copy(frData, 0, finalData, currentOffset, frData.Length);
            }
            // Add final length to frame offsets list.
            ArrayUtils.WriteIntToByteArray(finalData, headerOffset, addressSize, true, (UInt32)offset);
            return finalData;
        }

        public static SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_NEEDS_FRAMES, "fileToSave");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_EMPTY_FRAMES, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
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
        Boolean IsVersion107 { get; set; }
        Int32[] RemappedIndices { get; set; }
        Int32[] UncompressedIndices { get; set; }
    }
}