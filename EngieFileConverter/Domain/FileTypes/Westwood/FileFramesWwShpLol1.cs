using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Compression;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwShpLol1 : SupportedFileType
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
        public override String ShortTypeName { get { return "Westwood LOL 1 Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String ShortTypeDescription { get { return "Westwood Lands of Lore 1 Shape File"; } }
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

        public Boolean IsVersion107 { get; set; }
        public Int32[] RemappedIndices { get; set; }


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
            if (fileData.Length < 0X0A)
                throw new FileTypeLoadException("Not long enough for header.");
            UInt16 hdrSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x00, 2, true);
            if (hdrSize + 2 > fileData.Length)
                throw new FileTypeLoadException("Not a Lands of Lore SHP file");
            UInt16 hdrCompression = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x02, 2, true);
            Int32 hdrUncompressedSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 4, true);
            if (hdrUncompressedSize < 2)
                throw new FileTypeLoadException("Not a Lands of Lore SHP file");
            UInt16 hdrPalSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x08, 2, true);
            Int32 dataOffset = 0x0A;

            if (hdrPalSize == 768)
            {
                try
                {
                    this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(fileData, dataOffset));
                    PaletteUtils.ApplyTransparencyGuide(this.m_Palette, this.TransparencyMask);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException("Illegal values in embedded palette.", argex);
                }
            }
            else
            {
                this.m_Palette = null;
            }
            dataOffset += hdrPalSize;

            Byte[] uncompressedData;

            UInt32 endOffset = (UInt32)hdrSize + 2;            
            try
            {
                switch (hdrCompression)
                {
                    case 0:
                        uncompressedData = new Byte[hdrUncompressedSize];
                        Array.Copy(fileData, dataOffset, uncompressedData, 0, hdrUncompressedSize);
                        break;
                    case 1:
                        LzwCompression lzw12 = new LzwCompression(LzwSize.Size12Bit);
                        uncompressedData = lzw12.Decompress(fileData, dataOffset, hdrUncompressedSize);
                        break;
                    case 2:
                        LzwCompression lzw14 = new LzwCompression(LzwSize.Size14Bit);
                        uncompressedData = lzw14.Decompress(fileData, dataOffset, hdrUncompressedSize);
                        break;
                    case 3:
                        uncompressedData = WestwoodRle.RleDecode(fileData, (UInt32)dataOffset, endOffset, hdrUncompressedSize, false, true);
                        break;
                    case 4:
                        uncompressedData = new Byte[hdrUncompressedSize];
                        Int32 written = WWCompression.LcwDecompress(fileData, ref dataOffset, uncompressedData, 0);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported compression format \"" + hdrCompression + "\".");
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error decompressing image data: " + e.Message, e);
            }
            //File.WriteAllBytes(sourcePath + ".unlcw", uncompressedData);
            Boolean isVersion107;
            Int32[] remapFrames;
            this.m_FramesList = FileFramesWwShpD2.LoadFromFileData(uncompressedData, sourcePath, this, "Lands of Lore 1", out isVersion107, out remapFrames);
            if (this.m_Palette == null)
            {
                SupportedFileType frame0 = this.m_FramesList.FirstOrDefault();
                if (frame0 != null)
                    m_Palette = frame0.GetColors();
            }
            else
            {
                // Apply previously-loaded palette to all frames. Maybe I should give it to the load function instead...
                this.SetColors(m_Palette);
            }
            this.IsVersion107 = isVersion107;
            this.RemappedIndices = remapFrames;
            StringBuilder extraInfoGlobal = new StringBuilder();
            extraInfoGlobal.Append("Embedded format: Dune II ").Append(isVersion107 ? "v1.07" : "v1.00").Append(" SHP");
            extraInfoGlobal.Append("\nRemapped indices: ");
            if (remapFrames.Length == 0)
                extraInfoGlobal.Append("None");
            else
                extraInfoGlobal.AppendNumbersGrouped(remapFrames);
            this.ExtraInfo = extraInfoGlobal.ToString();
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            this.PerformPreliminarychecks(ref fileToSave);
            FileFramesWwShpD2 d2File = fileToSave as FileFramesWwShpD2;
            Boolean isdune100shape = d2File != null && !d2File.IsVersion107;
            Boolean hasRemap = d2File != null && d2File.RemappedIndices != null && d2File.RemappedIndices.Length > 0;
            String remapped = hasRemap ? GeneralUtils.GroupNumbers(d2File.RemappedIndices) : String.Empty;

            return new SaveOption[]
            {
                new SaveOption("VER", SaveOptionType.ChoicesList, "Game version", "v1.00,v1.07", isdune100shape? "0" : "1"),
                // Remap tables allow units to be remapped. Seems house remap is only applied to those tables, not the whole graphic.
                new SaveOption("RMT", SaveOptionType.Boolean, "Add remapping tables to allow frames to be remapped to House colours.", hasRemap ? "1" : "0"),
                new SaveOption("RMA", SaveOptionType.Boolean, "Auto-detect remap on the existence of colour indices 144-150.", null, "0", new SaveEnableFilter("RMT", false, "1")),
                new SaveOption("RMS", SaveOptionType.String, "Specify remapped indices (Comma separated. Can use ranges like \"0-20\"). Leave empty to process all.", "0123456789-, ", remapped, new SaveEnableFilter("RMA", true, "1")),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            this.PerformPreliminarychecks(ref fileToSave);

            // Cheaty way of getting a 2-choices dropdown option.
            Boolean isVersion107 = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "VER"));
            // Remap tables allow units to be remapped. Seems house remap is only applied to those tables, not the whole graphic.
            Boolean addRemap = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "RMT"));
            Boolean addRemapAuto = addRemap && GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "RMA"));
            String remapSpecificStr = SaveOption.GetSaveOptionValue(saveOptions, "RMS");
            Boolean remapAll = addRemap && !addRemapAuto && String.IsNullOrEmpty(remapSpecificStr);
            Int32[] remappedIndices = addRemap && !addRemapAuto && !remapAll ? GeneralUtils.GetRangedNumbers(remapSpecificStr) : null;

            Int32 frames = fileToSave.Frames.Length;
            Boolean[] remapIndex = new Boolean[frames];
            if (addRemap)
            {
                if (remapAll)
                {
                    for (Int32 i = 0; i < frames; i++)
                        remapIndex[i] = true;
                }
                else if (remappedIndices != null)
                {
                    foreach (Int32 remappedIndex in remappedIndices.Where(remappedIndex => remappedIndex >= 0 && remappedIndex < frames))
                        remapIndex[remappedIndex] = true;
                }
            }
            Int32 addressSize = isVersion107 ? 4 : 2;
            Int32 offset = addressSize * (frames + 1);
            if (!isVersion107)
                offset += 2;
            Int32[] header = new Int32[frames];
            Byte[][] frameHeaders = new Byte[frames][];
            Byte[][] frameData = new Byte[frames][];
            //ArrayUtils.WriteIntToByteArray(header, 0, 2, true, (UInt64)frames);
            for (Int32 i = 0; i < frames; i++)
            {
                header[i] = offset;
                SupportedFileType frame = fileToSave.Frames[i];
                Bitmap bm = frame.GetBitmap();
                Int32 frmWidth = bm.Width;
                Int32 frmHeight = bm.Height;
                Int32 stride;
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride, true);
                Boolean remapThis = addRemapAuto ? imageData.Any(b => b >= 144 && b <= 150) : remapIndex[i];
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
                    for (Int32 r = 1; r < tableLength; r++)
                        reverseTable[remapTable[r]] = (Byte) r;
                    for (Int32 j = 0; j < imageData.Length; j++)
                        imageData[j] = reverseTable[imageData[j]];
                    largeTable = tableLength > 16;
                }
                imageData = WestwoodRleZero.CompressRleZeroD2(imageData, frmWidth, frmHeight);
                Int32 zeroDataLen = imageData.Length;
                Byte[] lcwData = WWCompression.LcwCompress(imageData);
                Boolean isCompressed = lcwData.Length < imageData.Length;
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
                ArrayUtils.WriteIntToByteArray(frameHeader, 0x00, 2, true, (UInt32)flags);
                frameHeader[0x02] = (Byte)frmHeight;
                ArrayUtils.WriteIntToByteArray(frameHeader, 0x03, 2, true, (UInt32)frmWidth);
                frameHeader[0x05] = (Byte)frmHeight;
                ArrayUtils.WriteIntToByteArray(frameHeader, 0x06, 2, true, (UInt32)frmDataSize);
                ArrayUtils.WriteIntToByteArray(frameHeader, 0x08, 2, true, (UInt32)zeroDataLen);
                if (remapThis)
                {
                    Int32 writeOffs = 0x0A;
                    if (largeTable)
                    {
                        frameHeader[writeOffs] = (Byte)remapTable.Length;
                        writeOffs++;
                    }
                    Array.Copy(remapTable, 0, frameHeader, writeOffs, remapTable.Length);
                }
                frameHeaders[i] = frameHeader;
                frameData[i] = imageData;
                offset += frmDataSize;
            }
            Int32 actualLen = offset;
            if (isVersion107)
                actualLen += 2;
            Byte[] finalData = new Byte[actualLen];
            ArrayUtils.WriteIntToByteArray(finalData, 0, 2, true, (UInt16) frames);
            Int32 headerOffset = 2;
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 currentOffset = header[i];
                ArrayUtils.WriteIntToByteArray(finalData, headerOffset, addressSize, true, (UInt32)currentOffset);
                if (isVersion107)
                    currentOffset += 2;
                headerOffset += addressSize;
                Byte[] frHeader = frameHeaders[i];
                Int32 headerLen = frHeader.Length;
                Array.Copy(frHeader, 0, finalData, currentOffset, headerLen);
                currentOffset += headerLen;
                Byte[] frData = frameData[i];
                Array.Copy(frData, 0, finalData, currentOffset, frData.Length);
            }
            // Add final length to frame offsets list.
            ArrayUtils.WriteIntToByteArray(finalData, headerOffset, addressSize, true, (UInt32)offset);
            return finalData;
        }

        private void PerformPreliminarychecks(ref SupportedFileType fileToSave)
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
            foreach (SupportedFileType frame in fileToSave.Frames)
            {
                if (frame == null || frame.GetBitmap() == null)
                    throw new NotSupportedException("SHP can't handle empty frames!");
                if (frame.BitsPerPixel != 8)
                    throw new NotSupportedException("Not all frames in input type are 8-bit images!");
            }
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
}