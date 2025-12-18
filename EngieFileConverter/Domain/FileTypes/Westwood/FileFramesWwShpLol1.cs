using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Compression;
using Nyerguds.FileData.Westwood;
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
            UInt16 hdrSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x00, 2, true);
            if (hdrSize + 2 > fileData.Length)
                throw new FileTypeLoadException("Not a Lands of Lore SHP file");
            UInt16 hdrCompression = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x02, 2, true);
            this.CompressionType = hdrCompression;
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
            extraInfoGlobal.Append("Compression: ").Append(this.compressionTypes[this.CompressionType]);
            extraInfoGlobal.Append("\nEmbedded format: Dune II ").Append(isVersion107 ? "v1.07" : "v1.00").Append(" SHP");
            extraInfoGlobal.Append("\nRemapped indices: ");
            if (remapFrames.Length == 0)
                extraInfoGlobal.Append("None");
            else
                extraInfoGlobal.AppendNumbersGrouped(remapFrames);
            this.ExtraInfo = extraInfoGlobal.ToString();
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            FileFramesWwShpD2.PerformPreliminaryChecks(ref fileToSave);
            // If it is a non-image format which does contain colours, offer to save with palette
            FileFramesWwShpLol1 shp = fileToSave as FileFramesWwShpLol1;
            Int32 compression = shp != null ? shp.CompressionType : 4;
            Boolean hasRemap = shp != null && shp.RemappedIndices != null && shp.RemappedIndices.Length > 0;
            Boolean probablyRemappedUnit = fileToSave.Frames.Length > 16;
            if (probablyRemappedUnit)
            {
                Bitmap bm = fileToSave.Frames[16].GetBitmap();
                Int32 width0 = bm.Width;
                Int32 height0 = bm.Height;
                Int32 width;
                Int32 height = bm.Height;
                Byte[] buffer = ImageUtils.GetImageData(bm, out width, true);
                buffer = ImageUtils.TrimYHeight(buffer, width, ref height, 0);
                buffer = ImageUtils.TrimXWidth(buffer, ref width, height, 0);
                Int32 frWidth = fileToSave.Frames[0].Width;
                Int32 frHeight = fileToSave.Frames[0].Height;
                // check if the 'image' on frame 16 is a compact rectangle without any transparent pixels in it, which might indicate it being a remap table.
                if (width0 == width + 1 && height0 == height + 1 && !buffer.Any(b => b == 0) && width < 30)
                {
                    // check if the other frames are all the same size.
                    for (Int32 i = 1; i < fileToSave.Frames.Length; ++i)
                    {
                        if (i == 16)
                            continue;
                        SupportedFileType frame = fileToSave.Frames[i];
                        probablyRemappedUnit = frame.Width == frWidth && frame.Height == frHeight;
                        if (!probablyRemappedUnit)
                            break;
                    }
                }
            }
            String remapped = hasRemap && !probablyRemappedUnit ? GeneralUtils.GroupNumbers(shp.RemappedIndices) : String.Empty;
            return new SaveOption[]
            {
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Overall file compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new SaveOption("NCM", SaveOptionType.Boolean, "Don't compress separate frames", "0"),
                new SaveOption("RMT", SaveOptionType.Boolean, "Add remapping tables to allow frames to be remapped.", "1"),
                new SaveOption("RMU", SaveOptionType.Boolean, "Treat as remapped unit (apply remapping to all frames except the remap table itself at index #16)", null, probablyRemappedUnit ? "1" : "0", new SaveEnableFilter("RMT", false, "1")),
                new SaveOption("RMS", SaveOptionType.String, "Specify remapped indices (Comma separated. Can use ranges like \"0-20\"). Leave empty to process all.", "0123456789-, ", remapped, new SaveEnableFilter("RMU", true, "1"))
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 compressionType;
            if (!Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType))
                compressionType = 4;
            Boolean noFramesCompr = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "NCM"));
            Boolean addRemap = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "RMT"));
            Boolean addUnitRemap = addRemap && fileToSave.Frames.Length > 16 && GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "RMU"));
            String remapRange = SaveOption.GetSaveOptionValue(saveOptions, "RMS");
            if (addUnitRemap)
            {
                Int32 frLen = fileToSave.Frames.Length;
                remapRange = "0-15";
                if (frLen > 17)
                    remapRange += ", 17";
                if (frLen > 18)
                    remapRange += "-" + (frLen - 1);
            }
            SaveOption[] saveOpts = new SaveOption[]
            {
                new SaveOption("VER", SaveOptionType.Boolean, null, null, noFramesCompr ? "2" : "1"),
                new SaveOption("RMT", SaveOptionType.Boolean, null, null, addRemap || !String.IsNullOrEmpty(remapRange) ? "1" : "0"),
                new SaveOption("RMS", SaveOptionType.String, null, null, remapRange)
            };
            FileFramesWwShpD2 baseShp = new FileFramesWwShpD2();
            Byte[] dune2shpData = baseShp.SaveToBytesAsThis(fileToSave, saveOpts);
            return FileImgWwCps.SaveCps(dune2shpData, null, 0, compressionType, CpsVersion.Pc);
        }
    }
}