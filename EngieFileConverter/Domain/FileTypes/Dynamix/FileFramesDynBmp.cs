using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.FileData.Dynamix;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileFramesDynBmp : SupportedFileType
    {
        protected enum DynBmpSaveType
        {
            Unknown = 0,
            VgaBin,
            Bin,
            Ma8,
            Scn,
        }


        protected Int32 m_bpp;

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override String IdCode { get { return "DynBmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Dynamix BMP"; } }
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        public override String ShortTypeDescription { get { return "Dynamix BMP sprites file"; } }

        protected static String[] CompressionTypes = new String[] { "None", "RLE", "LZW", "LZSS" };
        protected static String[] SaveCompressionTypes = new String[] { "None", "RLE" };

        //protected String[] endchunks = new String[] { "None", "OFF (trims X and Y)" };
        public override Boolean NeedsPalette { get { return !this.m_loadedPalette; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        public override Int32 BitsPerPixel { get { return this.m_bpp; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];
        protected Boolean m_loadedPalette;

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return ArrayUtils.CloneArray(this.m_FramesList); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }

        protected Boolean m_IsMatrixImage;
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return this.m_IsMatrixImage; } }
        public Boolean IsScn { get; private set; }
        public Boolean IsMa8 { get; private set; }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, false);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename, false);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath, Boolean asMatrixImage)
        {
            DynamixChunk mainChunk = DynamixChunk.ReadChunk(fileData, "BMP");
            if (mainChunk == null || mainChunk.Address != 0 || mainChunk.DataLength + 8 != fileData.Length)
                throw new FileTypeLoadException("BMP chunk not found: not a valid Dynamix BMP file header.");
            Byte[] data = mainChunk.Data;
            DynamixChunk infChunk = DynamixChunk.ReadChunk(data, "INF");
            if (infChunk == null)
                throw new FileTypeLoadException("INF chunk not found: not a valid Dynamix BMP file header.");
            Byte[] frameInfo = infChunk.Data;
            Int32 frames = ArrayUtils.ReadUInt16FromByteArrayLe(frameInfo, 0);
            if (frameInfo.Length != 2 + frames * 4)
                throw new FileTypeLoadException("Bad header size: INF chunk is not long enough.");
            if (sourcePath != null)
            {
                String output = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath));
                String palName = output + ".pal";
                if (File.Exists(palName))
                {
                    try
                    {
                        FilePaletteDyn palDyn = new FilePaletteDyn();
                        Byte[] palData = File.ReadAllBytes(palName);
                        palDyn.LoadFile(palData, palName);
                        this.m_Palette = palDyn.GetColors();
                        PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, this.TransparencyMask);
                        this.m_loadedPalette = true;
                        this.LoadedFileName += "/PAL";
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            }
            Int32[] widths = new Int32[frames];
            Int32[] heights = new Int32[frames];
            Int32 fullDataSize8bit = 0;
            Int32 widthStart = 2;
            Int32 heightStart = frames * 2 + 2;
            for (Int32 i = 0; i < frames; ++i)
            {
                widths[i] = ArrayUtils.ReadUInt16FromByteArrayLe(frameInfo, widthStart + i * 2);
                heights[i] = ArrayUtils.ReadUInt16FromByteArrayLe(frameInfo, heightStart + i * 2);
                fullDataSize8bit += (widths[i] * heights[i]);
            }
            Int32 addr2 = infChunk.Address + infChunk.Length;
            if (fileData.Length < addr2+0x0B)
                throw new FileTypeLoadException("File not long enough to find data chunk.");
            String dataChunk = new String(new Char[] { (Char)fileData[addr2 + 0x08], (Char)fileData[addr2 + 0x09], (Char)fileData[addr2 + 0x0A], (Char)fileData[addr2 + 0x0B] });
            Boolean vqt = "VQT:".Equals(dataChunk);
            this.IsScn = "SCN:".Equals(dataChunk);
            DynamixChunk matrix = DynamixChunk.ReadChunk(mainChunk.Data, "MTX");
            if (matrix != null && !asMatrixImage)
                throw new FileTypeLoadException("This is a matrix-type image.");
            if (matrix == null && asMatrixImage)
                throw new FileTypeLoadException("This is not a matrix-type image.");
            Byte[] fullData;
            PixelFormat pf;
            if (vqt)
            {
                this.m_bpp = 8;
                pf = PixelFormat.Format8bppIndexed;
                fullData = new Byte[fullDataSize8bit];
                this.ExtraInfo = "File uses unsupported " + dataChunk.TrimEnd(':') + " format. Frames are blank but given as size reference.";
            }
            else if (this.IsScn)
            {
                // this will be about twice as large as needed, so let's use that as buffer for now.
                fullData = new Byte[fullDataSize8bit];
                DynamixChunk scnChunk = DynamixChunk.ReadChunk(mainChunk.Data, "SCN");
                DynamixChunk offChunk = DynamixChunk.ReadChunk(mainChunk.Data, "OFF");
                if (offChunk == null)
                    throw new FileTypeLoadException("SCN chunk is not accompanied by an OFF chunk.");
                Int32[] scnOffsets = new Int32[frames];
                Int32[] scnLengths = new Int32[frames];
                Int32 int32Offs = 0;
                Int32 lastOffs = 0;
                Int32 frm;
                for (frm = 0; frm < frames; frm++)
                {
                    Int32 currScnOffs = ArrayUtils.ReadInt32FromByteArrayLe(offChunk.Data, int32Offs);
                    scnOffsets[frm] = currScnOffs;
                    int32Offs += 4;
                    if (frm > 0)
                        scnLengths[frm - 1] = currScnOffs - lastOffs;
                    lastOffs = currScnOffs;
                }
                scnLengths[frm - 1] = scnChunk.DataLength - lastOffs;
                Boolean eightBitFound = false;
                Int32 maxLen = scnChunk.DataLength;
                for (Int32 i = 0; i < frames; i++)
                {
                    // Check for 8-bit add-values; switch whole image to 8-bit if needed.
                    Int32 offs = scnOffsets[i];
                    if (offs < maxLen && scnChunk.Data[offs] > 0x0F)
                        eightBitFound = true;
                }
                Int32 currOffsOut = 0;
                try
                {
                    for (Int32 i = 0; i < frames; i++)
                    {
                        int bpp = eightBitFound ? 8 : 4;
                        Byte[] decoded = DynamixCompression.ScnDecode(scnChunk.Data, scnOffsets[i], scnOffsets[i] + scnLengths[i], widths[i], heights[i], ref bpp);
                        Array.Copy(decoded, 0, fullData, currOffsOut, decoded.Length);
                        currOffsOut += decoded.Length;
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(ex.Message, ex);
                }
                this.m_bpp = eightBitFound ? 8 : 4;
                pf = eightBitFound ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed;
                this.ExtraInfo = "Compression: SCN/OFF";
            }
            else
            {
                DynamixChunk binChunk = DynamixChunk.ReadChunk(mainChunk.Data, "BIN");
                this.IsMa8 = false;
                if (binChunk == null)
                {
                    binChunk = DynamixChunk.ReadChunk(mainChunk.Data, "MA8");
                    if (binChunk == null)
                        throw new FileTypeLoadException("Cannot find BIN chunk!");
                    this.IsMa8 = true;

                }
                if (binChunk.Data.Length == 0)
                    throw new FileTypeLoadException("Empty BIN chunk!");
                Int32 compressionType = binChunk.Data[0];
                if (compressionType < CompressionTypes.Length)
                    this.ExtraInfo = "Compression: " + (this.IsMa8 ? "MA8" : "BIN") + ":" + CompressionTypes[compressionType];
                else
                    throw new FileTypeLoadException("Unknown compression type " + compressionType);
                Byte[] bindata = DynamixCompression.DecodeChunk(binChunk.Data);
                if (this.IsMa8) // MA8 seems to have indices 0 and FF switched
                    DynamixCompression.SwitchBackground(bindata);
                Byte[] vgadata = null;
                DynamixChunk vgaChunk = DynamixChunk.ReadChunk(mainChunk.Data, "VGA");
                if (vgaChunk == null)
                {
                    this.m_bpp = this.IsMa8 ? 8 : 4;
                }
                else
                {
                    if (vgaChunk.Data.Length == 0)
                        throw new FileTypeLoadException("Empty VGA chunk!");
                    this.m_bpp = 8;
                    compressionType = vgaChunk.Data[0];
                    if (compressionType < CompressionTypes.Length)
                        this.ExtraInfo += ", VGA:" + CompressionTypes[compressionType];
                    else
                        throw new FileTypeLoadException("Unknown compression type " + compressionType);
                    vgadata = DynamixCompression.DecodeChunk(vgaChunk.Data);
                }
                if (vgadata == null)
                {
                    fullData = bindata;
                    pf = this.IsMa8 ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed;
                }
                else
                {
                    pf = PixelFormat.Format8bppIndexed;
                    fullData = DynamixCompression.EnrichFourBit(vgadata, bindata);
                }
            }
            if (!this.m_loadedPalette || this.m_bpp == 4)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.m_bpp, this.TransparencyMask, false);

            Int32 offset = 0;
            this.m_FramesList = new SupportedFileType[frames];
            Byte[][] framesData = null;
            if (matrix != null)
                framesData = new Byte[frames][];
            for (Int32 i = 0; i < frames; ++i)
            {
                Int32 stride = ImageUtils.GetMinimumStride(widths[i], this.m_bpp);
                Int32 curSize = stride * heights[i];
                Byte[] image = new Byte[curSize];
                Array.Copy(fullData, offset, image, 0, curSize);
                if (matrix != null)
                    framesData[i] = image;
                offset += curSize;
                Bitmap frameImage = ImageUtils.BuildImage(image, widths[i], heights[i], stride, pf, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetFileClass(this.m_bpp == 8 ? FileClass.Image8Bit : FileClass.Image4Bit);
                frame.SetNeedsPalette(!this.m_loadedPalette);
                this.m_FramesList[i] = frame;
            }
            if (matrix != null && frames > 0)
            {
                Int32 blockWidth = widths[0];
                Int32 blockHeight = heights[0];
                if (widths.Any(w => w != blockWidth) || heights.Any(h => h != blockHeight))
                    throw new FileTypeLoadException("Dimensions of all frames must be equal in Matrix image!");
                Byte[] matrixData = matrix.Data;
                Int32 matrixWidth = ArrayUtils.ReadInt16FromByteArrayLe(matrixData, 0);
                Int32 matrixHeight = ArrayUtils.ReadInt16FromByteArrayLe(matrixData, 2);
                Int32 matrixLen = matrixHeight * matrixWidth;
                if(matrixLen < frames)
                    return;
                if ((matrixData.Length - 4) / 2 != matrixLen)
                    return;
                Byte[][] matrixFrames = new Byte[matrixLen][];
                for (Int32 i = 0; i < matrixLen; ++i)
                {
                    Int32 frame = ArrayUtils.ReadInt16FromByteArrayLe(matrixData, 4 + i * 2);
                    // Switch rows and columns; write into corresponding column.
                    matrixFrames[i % matrixHeight * matrixWidth + i / matrixHeight] = framesData[frame];
                }
                Int32 blockStride = ImageUtils.GetMinimumStride(blockWidth, this.m_bpp);
                this.m_LoadedImage = ImageUtils.Tile8BitImages(matrixFrames, blockWidth, blockHeight, blockStride, matrixLen, this.m_Palette, matrixWidth);
                this.m_IsMatrixImage = true;
                this.ExtraInfo += "\nMatrix size: " + matrixWidth + " x " + matrixHeight
                             + "\nBlock size: " + blockWidth + " x " + blockHeight
                             + "\nMatrix ratio: " + frames + " / " + matrixLen + " = " + (frames * 100 / matrixLen) + "%";
                this.ExtraInfo = this.ExtraInfo.Trim('\n');
            }
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Boolean is4bpp = fileToSave.BitsPerPixel == 4;
            SaveOption[] opts = new SaveOption[3];
            Int32 opt = 0;
            Boolean isScn = false;
            if (is4bpp)
            {
                FileFramesDynBmp bmp = fileToSave as FileFramesDynBmp;
                isScn = bmp != null && bmp.IsScn;
                Int32 saveType = isScn ? 1 : 0;
                opts[opt++] = new SaveOption("TYP4", SaveOptionType.ChoicesList, "Save type:", "VGA,SCN", saveType.ToString());
            }
            else
            {
                FileFramesDynBmp bmp = fileToSave as FileFramesDynBmp;
                Int32 saveType = bmp != null && bmp.IsMa8 ? 1 : 0;
                opts[opt++] = new SaveOption("TYP8", SaveOptionType.ChoicesList, "Save type:", "VGA / BIN,MA8,SCN", saveType.ToString());
            }
            opts[opt++] = new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", SaveCompressionTypes), (isScn ? 0 : 1).ToString(), false, new SaveEnableFilter("TYP4", true, "1"), new SaveEnableFilter("TYP8", true, "2"));
            opts[opt] = new SaveOption("SCL", SaveOptionType.Boolean, "SCN: Include line end at end of compressed data", null, "1", true, new SaveEnableFilter("TYP4", false, "1"), new SaveEnableFilter("TYP8", false, "2"));

            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 saveType4;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP4"), out saveType4);
            Int32 saveType8;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP8"), out saveType8);
            DynBmpSaveType saveType = DynBmpSaveType.Unknown;
            if (fileToSave.BitsPerPixel == 4)
            {
                switch (saveType4)
                {
                    case 0: saveType = DynBmpSaveType.Bin;
                        break;
                    case 1: saveType = DynBmpSaveType.Scn;
                        break;
                }
            }
            else
            {
                switch (saveType8)
                {
                    case 0: saveType = DynBmpSaveType.VgaBin;
                        break;
                    case 1: saveType = DynBmpSaveType.Ma8;
                        break;
                    case 2: saveType = DynBmpSaveType.Scn;
                        break;
                }
            }
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            Boolean lineEnd = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "SCL"));
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            List<DynamixChunk> basicChunks = this.SaveToChunks(fileToSave, saveType, saveType == DynBmpSaveType.Scn ? (lineEnd ? 1 : 0) : compressionType);
            DynamixChunk bmpChunk = DynamixChunk.BuildChunk("BMP", basicChunks.ToArray());
            return bmpChunk.WriteChunk();
        }

        /// <summary>
        /// Saves the given image data to chunks.
        /// </summary>
        /// <param name="fileToSave">File to save.</param>
        /// <param name="compressionType">Compression type. If savetype is SCN, any value besides 0 will enable saving final line skips in the compressed data.</param>
        /// <param name="saveType">Save type</param>
        /// <returns>A list of Dynamix chunks to write into the container chunk.</returns>
        protected List<DynamixChunk> SaveToChunks(SupportedFileType fileToSave, DynBmpSaveType saveType, Int32 compressionType)
        {
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_NO_FRAMES, "fileToSave");

            if (saveType == DynBmpSaveType.Unknown)
                throw new ArgumentException(String.Format(ERR_UNKN_COMPR, String.Empty).TrimEnd(' ', '\"'), "saveType");
            if (saveType != DynBmpSaveType.Scn && (compressionType < 0 || compressionType > CompressionTypes.Length))
                throw new ArgumentException(String.Format(ERR_UNKN_COMPR, compressionType), "compressionType");

            // write save logic for frames
            PixelFormat pf = PixelFormat.Undefined;
            Int32 bpp = 0;
            Byte[][] frameBytes = new Byte[nrOfFrames][];
            Int32[] frameWidths = new Int32[nrOfFrames];
            Int32[] frameHeights = new Int32[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new ArgumentException(ERR_EMPTY_FRAMES, "fileToSave");
                if (bm.Width % 8 != 0)
                    throw new ArgumentException("Dynamix image formats only support image widths divisible by 8.", "fileToSave");
                if (pf == PixelFormat.Undefined)
                {
                    pf = bm.PixelFormat;
                    Int32 newbpp = Image.GetPixelFormatSize(pf);
                    if (pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                        throw new ArgumentException("This format needs 4bpp or 8bpp frames.", "fileToSave");
                    if (bpp != 0 && newbpp != bpp)
                        throw new ArgumentException("All frames in this format need to be the same colour depth.", "fileToSave");
                    bpp = newbpp;
                }
                else if (pf != bm.PixelFormat)
                    throw new ArgumentException(ERR_FRAMES_BPPDIFF, "fileToSave");
                Int32 stride;
                Int32 width = bm.Width;
                Int32 height = bm.Height;
                frameBytes[i] = ImageUtils.GetImageData(bm, out stride, true);
                frameWidths[i] = width;
                frameHeights[i] = height;
            }
            List<DynamixChunk> chunks = new List<DynamixChunk>();
            Int32 fullDataLen = frameBytes.Sum(x => x.Length);
            Byte[] framesIndex = new Byte[nrOfFrames * 4 + 2];
            ArrayUtils.WriteInt16ToByteArrayLe(framesIndex, 0, nrOfFrames);
            Byte[] fullData = saveType == DynBmpSaveType.Scn ? null : new Byte[fullDataLen];
            Int32 offset = 0;
            Int32 widthStart = 2;
            Int32 heightStart = nrOfFrames * 2 + 2;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                ArrayUtils.WriteInt16ToByteArrayLe(framesIndex, widthStart + i * 2, frameWidths[i]);
                ArrayUtils.WriteInt16ToByteArrayLe(framesIndex, heightStart + i * 2, frameHeights[i]);
                // This operation is not needed for SCN saving; it builds the array after compressing.
                if (fullData != null)
                {
                    Byte[] frameData = frameBytes[i];
                    Array.Copy(frameData, 0, fullData, offset, frameData.Length);
                    offset += frameData.Length;
                }
            }
            chunks.Add(new DynamixChunk("INF", framesIndex));
            DynamixChunk dataChunk;
            Byte compressionBin;
            Byte[] data;
            UInt32 dataLenBin;
            switch (saveType)
            {
                case DynBmpSaveType.Bin:
                case DynBmpSaveType.Ma8:
                    Boolean isMa8 = saveType == DynBmpSaveType.Ma8;
                    compressionBin = 0;
                    data = fullData;
                    if (isMa8) // MA8 seems to have indices 0 and FF switched
                        DynamixCompression.SwitchBackground(data);
                    dataLenBin = (UInt32)data.Length;
                    if (compressionType == 1)
                    {
                        // TODO find and implement the other types... eventually.
                        Byte[] dataCompr = DynamixCompression.RleEncode(data);
                        if (dataCompr.Length < dataLenBin)
                        {
                            data = dataCompr;
                            compressionBin = (Byte)compressionType;
                        }
                    }
                    dataChunk = new DynamixChunk(isMa8 ? "MA8" : "BIN", compressionBin, dataLenBin, data);
                    chunks.Add(dataChunk);
                    break;
                case DynBmpSaveType.Scn:
                    // I just dumped it in here, because, eh, why not.
                    Boolean addFinalLineWrap = compressionType == 1;
                    Byte[][] frameBytesCompressed = new Byte[nrOfFrames][];
                    Byte[] offsets = new Byte[nrOfFrames * 4];
                    Int32 curOffset = 0;
                    // Compress all frames
                    for (Int32 i = 0; i < nrOfFrames; i++)
                    {
                        // Write start indices into the data for the OFF chunk
                        ArrayUtils.WriteInt32ToByteArrayLe(offsets, i << 2, curOffset);
                        // Compress frame
                        Byte[] comprFrame = DynamixCompression.ScnEncode(frameBytes[i], frameWidths[i], frameHeights[i], bpp, addFinalLineWrap);
                        frameBytesCompressed[i] = comprFrame;
                        // Increase index
                        curOffset += comprFrame.Length;
                    }
                    data = new Byte[curOffset];
                    curOffset = 0;
                    // Combine all frames into one array
                    for (Int32 i = 0; i < nrOfFrames; i++)
                    {
                        Byte[] comprFrame = frameBytesCompressed[i];
                        Int32 comprFrameLen = comprFrame.Length;
                        Array.Copy(comprFrame, 0, data, curOffset, comprFrameLen);
                        curOffset += comprFrameLen;
                    }
                    dataChunk = new DynamixChunk("SCN", data);
                    chunks.Add(dataChunk);
                    DynamixChunk offChunk = new DynamixChunk("OFF", offsets);
                    chunks.Add(offChunk);
                    break;
                case DynBmpSaveType.VgaBin:
                    Byte[] vgaData;
                    DynamixCompression.SplitEightBit(fullData, out vgaData, out data);
                    UInt32 dataLenVga = (UInt32)vgaData.Length;
                    dataLenBin = (UInt32)data.Length;
                    Byte compressionVga = 0;
                    compressionBin = 0;
                    // optional: add compression
                    if (compressionType != 0)
                    {
                        Byte[] dataHiCompr = compressionType == 1 ? DynamixCompression.RleEncode(vgaData) : DynamixCompression.LzwEncode(vgaData);
                        if (dataHiCompr.Length < dataLenVga)
                        {
                            vgaData = dataHiCompr;
                            compressionVga = (Byte)compressionType;
                        }
                        Byte[] dataLoCompr = compressionType == 1 ? DynamixCompression.RleEncode(data) : DynamixCompression.LzwEncode(data);
                        if (dataLoCompr.Length < dataLenBin)
                        {
                            data = dataLoCompr;
                            compressionBin = (Byte)compressionType;
                        }
                    }
                    dataChunk = new DynamixChunk("BIN", compressionBin, dataLenBin, data);
                    chunks.Add(dataChunk);
                    DynamixChunk vgaChunk = new DynamixChunk("VGA", compressionVga, dataLenVga, vgaData);
                    chunks.Add(vgaChunk);
                    break;
            }
            return chunks;
        }

    }
}