using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Compression;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwShpLol1 : SupportedFileType, Dune2ShpType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwShpLl"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood LOL 1 Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String LongTypeName { get { return "Westwood Shape File - Lands of Lore 1"; } }
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
        public Int32 CompressionType { get; protected set; }
        protected String[] compressionTypes = new String[] { "No compression", "LZW-12", "LZW-14", "RLE", "LCW" };


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
            UInt16 hdrSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x00);
            if (hdrSize + 2 > fileData.Length)
                throw new FileTypeLoadException("Not a Lands of Lore SHP file");
            UInt16 hdrCompression = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x02);
            this.CompressionType = hdrCompression;
            Int32 hdrUncompressedSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x04);
            if (hdrUncompressedSize < 2)
                throw new FileTypeLoadException("Not a Lands of Lore SHP file");
            UInt16 hdrPalSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x08);
            Int32 dataOffset = 0x0A;

            if (hdrPalSize == 768)
            {
                try
                {
                    this.m_Palette = ColorUtils.ReadSixBitPalette(fileData, dataOffset);
                    PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, this.TransparencyMask);
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
                        WWCompression.LcwDecompress(fileData, ref dataOffset, uncompressedData, 0);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported compression format \"" + hdrCompression + "\".");
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error decompressing image data.", e);
            }
            Boolean isVersion107;
            Int32[] remapFrames;
            Int32[] notCompressedFrames;
            this.m_FramesList = FileFramesWwShpD2.LoadFromFileData(uncompressedData, sourcePath, this, "Lands of Lore 1", out isVersion107, out remapFrames, out notCompressedFrames);
            SupportedFileType frame0 = this.m_FramesList.FirstOrDefault();
            if (frame0 != null)
            {
                this.m_Height = this.m_FramesList.Max(fr => fr.Height);
                this.m_Width = this.m_FramesList.Max(fr => fr.Width);
            }
            if (this.m_Palette == null)
            {
                if (frame0 != null)
                    this.m_Palette = frame0.GetColors();
            }
            else
            {
                // Apply previously-loaded palette to all frames. Maybe I should give it to the load function instead...
                this.SetColors(this.m_Palette);
            }
            this.IsVersion107 = isVersion107;
            this.RemappedIndices = remapFrames;
            this.UncompressedIndices = notCompressedFrames;
            StringBuilder extraInfoGlobal = new StringBuilder();
            extraInfoGlobal.Append("Compression: ").Append(this.compressionTypes[this.CompressionType]);
            extraInfoGlobal.Append("\nEmbedded format: Dune II ").Append(isVersion107 ? "v1.07" : "v1.00").Append(" SHP");
            extraInfoGlobal.Append("\nRemapped indices: ");
            if (remapFrames.Length == 0)
                extraInfoGlobal.Append("None");
            else
                extraInfoGlobal.AppendNumbersGrouped(remapFrames);
            this.ExtraInfo = extraInfoGlobal.ToString();
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            SupportedFileType[] frames = FileFramesWwShpD2.PerformPreliminaryChecks(fileToSave);
            // If it is a non-image format which does contain colors, offer to save with palette
            FileFramesWwShpLol1 shp = fileToSave as FileFramesWwShpLol1;
            Int32 compression = shp != null ? shp.CompressionType : 4;
            Dune2ShpType d2shp = fileToSave as Dune2ShpType;
            Boolean hasRemap = d2shp != null && d2shp.RemappedIndices != null && d2shp.RemappedIndices.Length > 0;
            Boolean probablyRemappedUnit = frames.Length > 16;
            if (probablyRemappedUnit)
            {
                Bitmap bm = frames[16].GetBitmap();
                Int32 width0 = bm.Width;
                Int32 height0 = bm.Height;
                Int32 width;
                Int32 height = bm.Height;
                Byte[] buffer = ImageUtils.GetImageData(bm, out width, true);
                buffer = ImageUtils.TrimYHeight(buffer, width, ref height, 0);
                buffer = ImageUtils.TrimXWidth(buffer, ref width, height, 0);
                Int32 frWidth = frames[0].Width;
                Int32 frHeight = frames[0].Height;
                // check if the 'image' on frame 16 is a compact rectangle without any transparent pixels in it, which might indicate it being a remap table.
                if (width0 == width + 1 && height0 == height + 1 && !buffer.Any(b => b == 0) && width < 30)
                {
                    // check if the other frames are all the same size.
                    for (Int32 i = 1; i < frames.Length; ++i)
                    {
                        if (i == 16)
                            continue;
                        SupportedFileType frame = frames[i];
                        probablyRemappedUnit = frame.Width == frWidth && frame.Height == frHeight;
                        if (!probablyRemappedUnit)
                            break;
                    }
                }
            }
            String remapped = hasRemap && !probablyRemappedUnit ? GeneralUtils.GroupNumbers(d2shp.RemappedIndices) : String.Empty;
            Boolean hasUncompressed = d2shp != null && d2shp.UncompressedIndices != null && d2shp.UncompressedIndices.Length > 0;
            String uncompressed = hasUncompressed ? GeneralUtils.GroupNumbers(d2shp.UncompressedIndices) : String.Empty;

            return new Option[]
            {
                new Option("CMP", OptionInputType.ChoicesList, "Overall file compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new Option("NCM", OptionInputType.Boolean, "Don't compress separate frames (might give better overall compression)", "0"),
                new Option("RMT", OptionInputType.Boolean, "Add remapping tables to allow frames to be remapped.", "1"),
                new Option("RMU", OptionInputType.Boolean, "Treat as remapped unit (apply remapping to all frames except the remap table itself at index #16)", null, probablyRemappedUnit ? "1" : "0", new EnableFilter("RMT", true, "1")),
                new Option("RMS", OptionInputType.String, "Specify remapped indices (Comma separated. Can use ranges like \"0-20\"). Leave empty to remap all.", "0123456789-, " + Environment.NewLine, remapped, new EnableFilter("RMU", false, "1")),
                new Option("NCA", OptionInputType.Boolean, "Auto-detect best compression usage.", "0", "1", new EnableFilter("NCM", false, "1")),
                new Option("NCS", OptionInputType.String, "Specify non-compressed indices (Comma separated. Can use ranges like \"0-20\"). Leave empty to treat all as non-compressed.", "0123456789-, " + Environment.NewLine, uncompressed, new EnableFilter("NCM", false, "1"), new EnableFilter("NCA", false, "1"))
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = FileFramesWwShpD2.PerformPreliminaryChecks(fileToSave);
            Int32 compressionType;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compressionType))
                compressionType = 4;
            Boolean noFramesCompr = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "NCM"));
            Boolean addRemap = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "RMT"));
            Boolean addUnitRemap = addRemap && frames.Length > 16 && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "RMU"));
            String remapRange = Option.GetSaveOptionValue(saveOptions, "RMS");
            if (addUnitRemap)
            {
                Int32 frLen = frames.Length;
                remapRange = "0-15";
                if (frLen > 17)
                    remapRange += ", 17";
                if (frLen > 18)
                    remapRange += "-" + (frLen - 1);
            }
            Option noCompAuto = Option.GetSaveOption(saveOptions, "NCA");
            Option noCompSpecified = Option.GetSaveOption(saveOptions, "NCS");
            List<Option> saveOpts = new List<Option>();
            saveOpts.Add(new Option("VER", OptionInputType.Boolean, null, null, "1"));
            saveOpts.Add(new Option("RMT", OptionInputType.Boolean, null, null, addRemap || !String.IsNullOrEmpty(remapRange) ? "1" : "0"));
            saveOpts.Add(new Option("RMS", OptionInputType.String, null, null, remapRange));
            if (noFramesCompr)
            {
                saveOpts.Add(new Option("NCA", OptionInputType.Boolean, "", "0"));
                saveOpts.Add(new Option("NCS", OptionInputType.String, String.Empty, String.Empty, String.Empty));
            }
            else
            {
                if (noCompAuto != null) saveOpts.Add(noCompAuto);
                if (noCompSpecified != null) saveOpts.Add(noCompSpecified);
            }

            // Lands of Lore Shape format is Dune 2 SHP embedded in CPS format.
            FileFramesWwShpD2 baseShp = new FileFramesWwShpD2();
            Byte[] dune2shpData = baseShp.SaveToBytesAsThis(fileToSave, saveOpts.ToArray());
            return FileImgWwCps.SaveCps(dune2shpData, null, 0, compressionType, CpsVersion.Pc);
        }

    }
}