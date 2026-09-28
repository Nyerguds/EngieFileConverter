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
        protected enum DynBmpInternalType
        {
            Unknown = 0,
            Bin,
            Scn,
            BinVga,
            Ma8,
            Vqt,
        }

        protected enum DynBmpInternalCompression
        {
            None = 0,
            Rle = 1,
            Lzw = 2,
            Lzss = 3
        }

        protected int m_bpp;

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override string IdCode { get { return "DynBmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Dynamix BMP"; } }
        public override string[] FileExtensions { get { return new string[] { "bmp" }; } }
        public override string LongTypeName { get { return "Dynamix BMP sprites file"; } }

        protected static string[] CompressionTypes = new string[] { "None", "RLE", "LZW", "LZSS" };
        protected static string[] SaveCompressionTypes = new string[] { "None", "RLE" };

        //protected String[] endchunks = new String[] { "None", "OFF (trims X and Y)" };
        public override bool NeedsPalette { get { return this.m_loadedPalette == null; } }
        public override bool[] TransparencyMask { get { return new bool[] { true }; } }

        public override int BitsPerPixel { get { return this.m_bpp; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];
        protected string m_loadedPalette;

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return ArrayUtils.CloneArray(this.m_FramesList); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }

        protected bool m_IsMatrixImage;
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return this.m_IsMatrixImage; } }
        protected DynBmpInternalType InternalType { get; private set; }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, false);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename, false);
            this.SetFileNames(filename);
            if (m_loadedPalette != null)
                this.LoadedFileName += "/" + Path.GetExtension(m_loadedPalette).TrimStart('.');
        }

        public override bool ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath, bool asMatrixImage)
        {
            DynamixChunk mainChunk = DynamixChunk.ReadChunk(fileData, "BMP");
            if (mainChunk == null || mainChunk.Address != 0 || mainChunk.DataLength + 8 != fileData.Length)
                throw new FileTypeLoadException("BMP chunk not found: not a valid Dynamix BMP file header.");
            byte[] data = mainChunk.Data;
            DynamixChunk infChunk = DynamixChunk.ReadChunk(data, "INF");
            if (infChunk == null)
                throw new FileTypeLoadException("INF chunk not found: not a valid Dynamix BMP file header.");
            byte[] frameInfo = infChunk.Data;
            int frames = ArrayUtils.ReadUInt16FromByteArrayLe(frameInfo, 0);
            if (frameInfo.Length != 2 + frames * 4)
                throw new FileTypeLoadException("Bad header size: INF chunk is not long enough.");
            if (sourcePath != null)
            {
                FilePaletteDyn palDyn = CheckForPalette<FilePaletteDyn>(sourcePath);
                if (palDyn != null)
                    this.m_loadedPalette = palDyn.LoadedFile;
            }
            int[] widths = new int[frames];
            int[] heights = new int[frames];
            int fullDataSize8bit = 0;
            int widthStart = 2;
            int heightStart = frames * 2 + 2;
            for (int i = 0; i < frames; ++i)
            {
                widths[i] = ArrayUtils.ReadUInt16FromByteArrayLe(frameInfo, widthStart + i * 2);
                heights[i] = ArrayUtils.ReadUInt16FromByteArrayLe(frameInfo, heightStart + i * 2);
                fullDataSize8bit += (widths[i] * heights[i]);
            }
            int addr2 = infChunk.Address + infChunk.Length;
            if (fileData.Length < addr2+0x0B)
                throw new FileTypeLoadException("File not long enough to find data chunk.");
            string dataChunk = new string(new char[] { (char)fileData[addr2 + 0x08], (char)fileData[addr2 + 0x09], (char)fileData[addr2 + 0x0A], (char)fileData[addr2 + 0x0B] });
            bool vqt = "VQT:".Equals(dataChunk);
            bool isScn = "SCN:".Equals(dataChunk);
            DynamixChunk matrix = DynamixChunk.ReadChunk(mainChunk.Data, "MTX");
            if (matrix != null && !asMatrixImage)
                throw new FileTypeLoadException("This is a matrix-type image.");
            if (matrix == null && asMatrixImage)
                throw new FileTypeLoadException("This is not a matrix-type image.");
            byte[] fullData;
            PixelFormat pf;
            if (vqt)
            {
                this.InternalType = DynBmpInternalType.Vqt;
                this.m_bpp = 8;
                pf = PixelFormat.Format8bppIndexed;
                fullData = new byte[fullDataSize8bit];
                this.ExtraInfo = "Internal type: " + this.InternalType.ToString().ToUpper() + ".\nCurrently unsupported. Frames are blank but given as size reference.";
            }
            else if (isScn)
            {
                this.InternalType = DynBmpInternalType.Scn;
                // this will be about twice as large as needed, so let's use that as buffer for now.
                fullData = new byte[fullDataSize8bit];
                DynamixChunk scnChunk = DynamixChunk.ReadChunk(mainChunk.Data, "SCN");
                DynamixChunk offChunk = DynamixChunk.ReadChunk(mainChunk.Data, "OFF");
                if (offChunk == null)
                    throw new FileTypeLoadException("SCN chunk is not accompanied by an OFF chunk.");
                int[] scnOffsets = new int[frames];
                int[] scnLengths = new int[frames];
                int int32Offs = 0;
                int lastOffs = 0;
                int frm;
                for (frm = 0; frm < frames; ++frm)
                {
                    int currScnOffs = ArrayUtils.ReadInt32FromByteArrayLe(offChunk.Data, int32Offs);
                    scnOffsets[frm] = currScnOffs;
                    int32Offs += 4;
                    if (frm > 0)
                        scnLengths[frm - 1] = currScnOffs - lastOffs;
                    lastOffs = currScnOffs;
                }
                scnLengths[frm - 1] = scnChunk.DataLength - lastOffs;
                bool eightBitFound = false;
                int maxLen = scnChunk.DataLength;
                for (int i = 0; i < frames; ++i)
                {
                    // Check for 8-bit add-values; switch whole image to 8-bit if needed.
                    int offs = scnOffsets[i];
                    if (offs < maxLen && scnChunk.Data[offs] > 0x0F)
                        eightBitFound = true;
                }
                int currOffsOut = 0;
                try
                {
                    for (int i = 0; i < frames; ++i)
                    {
                        int bpp = eightBitFound ? 8 : 4;
                        byte[] decoded;
                        try
                        {
                            decoded = DynamixCompression.ScnDecode(scnChunk.Data, scnOffsets[i], scnOffsets[i] + scnLengths[i], widths[i], heights[i], ref bpp);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
                        }
                        Array.Copy(decoded, 0, fullData, currOffsOut, decoded.Length);
                        currOffsOut += decoded.Length;
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
                }
                this.m_bpp = eightBitFound ? 8 : 4;
                pf = eightBitFound ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed;
                this.ExtraInfo = "Internal type: SCN+OFF";
            }
            else
            {
                DynamixChunk binChunk = DynamixChunk.ReadChunk(mainChunk.Data, "BIN");
                this.InternalType = DynBmpInternalType.Bin;
                if (binChunk == null)
                {
                    binChunk = DynamixChunk.ReadChunk(mainChunk.Data, "MA8");
                    if (binChunk == null)
                        throw new FileTypeLoadException("Cannot find BIN chunk.");
                    this.InternalType = DynBmpInternalType.Ma8;
                }
                if (binChunk.Data.Length == 0)
                    throw new FileTypeLoadException("Empty BIN chunk.");
                int compressionType = binChunk.Data[0];
                if (compressionType >= CompressionTypes.Length)
                    throw new FileTypeLoadException("Unknown compression type " + compressionType + ".");
                string compressionStr = "Compression: " + (this.InternalType == DynBmpInternalType.Ma8 ? "MA8" : "BIN") + ":" + CompressionTypes[compressionType];
                byte[] binData;
                try
                {
                    binData = DynamixCompression.DecodeChunk(binChunk.Data);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
                }
                if (this.InternalType == DynBmpInternalType.Ma8) // MA8 seems to have indices 0 and FF switched
                    DynamixCompression.SwitchBackground(binData);
                byte[] vgaData = null;
                DynamixChunk vgaChunk = DynamixChunk.ReadChunk(mainChunk.Data, "VGA");
                if (vgaChunk == null)
                {
                    this.m_bpp = this.InternalType == DynBmpInternalType.Ma8 ? 8 : 4;
                    this.ExtraInfo = "Internal type: " + (InternalType == DynBmpInternalType.Ma8 ? "MA8" : "BIN");
                }
                else
                {
                    if (vgaChunk.Data.Length == 0)
                        throw new FileTypeLoadException("Empty VGA chunk.");
                    this.InternalType = DynBmpInternalType.BinVga;
                    this.m_bpp = 8;
                    compressionType = vgaChunk.Data[0];
                    if (compressionType < CompressionTypes.Length)
                        compressionStr += ", VGA:" + CompressionTypes[compressionType];
                    else
                        throw new FileTypeLoadException("Unknown compression type " + compressionType);
                    try
                    {
                        vgaData = DynamixCompression.DecodeChunk(vgaChunk.Data);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
                    }
                    this.ExtraInfo = "Internal type: BIN+VGA";
                }
                if (vgaData == null)
                {
                    fullData = binData;
                    pf = this.InternalType == DynBmpInternalType.Ma8 ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed;
                }
                else
                {
                    pf = PixelFormat.Format8bppIndexed;
                    fullData = DynamixCompression.EnrichFourBit(vgaData, binData);
                }
                this.ExtraInfo += "\n" + compressionStr;
            }
            if (this.m_loadedPalette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.m_bpp, this.TransparencyMask, false);
            else if (this.m_bpp == 4 && this.m_Palette.Length > 16)
            {
                Color[] newPal = new Color[16];
                Array.Copy(this.m_Palette, newPal, newPal.Length);
                this.m_Palette = newPal;
            }

            int offset = 0;
            this.m_FramesList = new SupportedFileType[frames];
            byte[][] framesData = null;
            if (matrix != null)
                framesData = new byte[frames][];
            for (int i = 0; i < frames; ++i)
            {
                int stride = ImageUtils.GetMinimumStride(widths[i], this.m_bpp);
                int curSize = stride * heights[i];
                byte[] image = new byte[curSize];
                Array.Copy(fullData, offset, image, 0, curSize);
                if (matrix != null)
                    framesData[i] = image;
                offset += curSize;
                Bitmap frameImage = ImageUtils.BuildImage(image, widths[i], heights[i], stride, pf, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetFileClass(this.m_bpp == 8 ? FileClass.Image8Bit : FileClass.Image4Bit);
                frame.SetNeedsPalette(this.m_loadedPalette == null);
                this.m_FramesList[i] = frame;
            }
            if (matrix != null && frames > 0)
            {
                int blockWidth = widths[0];
                int blockHeight = heights[0];
                if (widths.Any(w => w != blockWidth) || heights.Any(h => h != blockHeight))
                    throw new FileTypeLoadException("Dimensions of all frames must be equal in Matrix image.");
                byte[] matrixData = matrix.Data;
                int matrixWidth = ArrayUtils.ReadInt16FromByteArrayLe(matrixData, 0);
                int matrixHeight = ArrayUtils.ReadInt16FromByteArrayLe(matrixData, 2);
                int matrixLen = matrixHeight * matrixWidth;
                if (matrixLen < frames)
                    return;
                if ((matrixData.Length - 4) / 2 != matrixLen)
                    return;
                byte[][] matrixFrames = new byte[matrixLen][];
                for (int i = 0; i < matrixLen; ++i)
                {
                    int frame = ArrayUtils.ReadInt16FromByteArrayLe(matrixData, 4 + i * 2);
                    // Switch rows and columns; write into corresponding column.
                    matrixFrames[i % matrixHeight * matrixWidth + i / matrixHeight] = framesData[frame];
                }
                int blockStride = ImageUtils.GetMinimumStride(blockWidth, this.m_bpp);
                this.m_LoadedImage = ImageUtils.Tile8BitImages(matrixFrames, blockWidth, blockHeight, blockStride, matrixLen, this.m_Palette, matrixWidth);
                this.m_IsMatrixImage = true;
                this.ExtraInfo += "\nMatrix size: " + matrixWidth + " x " + matrixHeight
                             + "\nBlock size: " + blockWidth + " x " + blockHeight
                             + "\nMatrix ratio: " + frames + " / " + matrixLen + " = " + (frames * 100 / matrixLen) + "%";
                this.ExtraInfo = this.ExtraInfo.Trim('\n');
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            bool is4bpp = fileToSave.BitsPerPixel == 4;
            Option[] opts = new Option[3];
            int opt = 0;
            bool isScn = false;
            if (is4bpp)
            {
                FileFramesDynBmp bmp = fileToSave as FileFramesDynBmp;
                int saveType = 0;
                if (bmp != null)
                {
                    switch (bmp.InternalType)
                    {
                        case DynBmpInternalType.Bin: saveType = 0; break;
                        case DynBmpInternalType.Scn: saveType = 1; break;
                    }
                }
                isScn = saveType == 1;
                opts[opt++] = new Option("TYP4", OptionInputType.ChoicesList, "Save type:", "BIN,SCN", saveType.ToString());
            }
            else
            {
                FileFramesDynBmp bmp = fileToSave as FileFramesDynBmp;
                int saveType = 0;
                if (bmp != null)
                {
                    switch (bmp.InternalType)
                    {
                        case DynBmpInternalType.BinVga: saveType = 0; break;
                        case DynBmpInternalType.Ma8: saveType = 1; break;
                        case DynBmpInternalType.Scn: saveType = 2; break;
                    }
                }
                isScn = saveType == 2;
                opts[opt++] = new Option("TYP8", OptionInputType.ChoicesList, "Save type:", "BIN / VGA,MA8,SCN", saveType.ToString());
            }
            opts[opt++] = new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", SaveCompressionTypes), isScn ? "0" : "1", is4bpp ? new EnableFilter("TYP4", false, "1") : new EnableFilter("TYP8", false, "2"));
            opts[opt] = new Option("SCL", OptionInputType.Boolean, "SCN: Include line end at end of compressed data", null, "1", is4bpp ? new EnableFilter("TYP4", true, "1") : new EnableFilter("TYP8", true, "2"));

            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            DynBmpInternalType saveType = DynBmpInternalType.Unknown;
            if (fileToSave.BitsPerPixel == 4)
            {
                int saveType4;
                if (Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TYP4"), out saveType4))
                {
                    switch (saveType4)
                    {
                        case 0:
                            saveType = DynBmpInternalType.Bin;
                            break;
                        case 1:
                            saveType = DynBmpInternalType.Scn;
                            break;
                    }
                }
            }
            else
            {
                int saveType8;
                if (Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TYP8"), out saveType8))
                {
                    switch (saveType8)
                    {
                        case 0:
                            saveType = DynBmpInternalType.BinVga;
                            break;
                        case 1:
                            saveType = DynBmpInternalType.Ma8;
                            break;
                        case 2:
                            saveType = DynBmpInternalType.Scn;
                            break;
                    }
                }
            }
            DynBmpInternalCompression compressionType;
            if (saveType == DynBmpInternalType.Scn)
            {
                bool lineEnd = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "SCL"));
                // SCN has its own compression. Use the "Compression" parameter to transfer its options instead.
                compressionType = lineEnd ? DynBmpInternalCompression.Rle : DynBmpInternalCompression.None;
            }
            else
            {
                int compression;
                Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compression);
                compressionType = (DynBmpInternalCompression) compression;
            }
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            List<DynamixChunk> basicChunks;
            try
            {
                basicChunks = this.SaveToChunks(fileToSave, saveType, compressionType);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
            DynamixChunk bmpChunk = DynamixChunk.BuildChunk("BMP", basicChunks.ToArray());
            return bmpChunk.WriteChunk();
        }

        /// <summary>
        /// Saves the given image data to chunks.
        /// </summary>
        /// <param name="fileToSave">File to save.</param>
        /// <param name="saveType">Save type</param>
        /// <param name="compressionType">Compression type. If savetype is SCN, any value besides 'None' will enable saving final line skips in the compressed data.</param>
        /// <returns>A list of Dynamix chunks to write into the container chunk.</returns>
        protected List<DynamixChunk> SaveToChunks(SupportedFileType fileToSave, DynBmpInternalType saveType, DynBmpInternalCompression compressionType)
        {
            if (fileToSave == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
            if (saveType == DynBmpInternalType.Unknown)
                throw new FileTypeSaveException(ERR_UNKN_COMPR);
            // write save logic for frames
            PixelFormat pf = PixelFormat.Undefined;
            int bpp = 0;
            byte[][] frameBytes = new byte[nrOfFrames][];
            int[] frameWidths = new int[nrOfFrames];
            int[] frameHeights = new int[nrOfFrames];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new FileTypeSaveException(ERR_FRAMES_EMPTY);
                if (bm.Width % 8 != 0)
                    throw new FileTypeSaveException("Dynamix image formats only support image widths divisible by 8.");
                if (pf == PixelFormat.Undefined)
                {
                    pf = bm.PixelFormat;
                    if (pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                        throw new FileTypeSaveException(ERR_BPP_INPUT_4_8);
                    bpp = Image.GetPixelFormatSize(pf);
                }
                else if (pf != bm.PixelFormat)
                    throw new FileTypeSaveException(ERR_FRAMES_BPP_DIFF);
                int stride;
                int width = bm.Width;
                int height = bm.Height;
                frameBytes[i] = ImageUtils.GetImageData(bm, out stride, true);
                frameWidths[i] = width;
                frameHeights[i] = height;
            }
            List<DynamixChunk> chunks = new List<DynamixChunk>();
            int fullDataLen = frameBytes.Sum(x => x.Length);
            byte[] framesIndex = new byte[nrOfFrames * 4 + 2];
            ArrayUtils.WriteUInt16ToByteArrayLe(framesIndex, 0, (ushort)nrOfFrames);
            byte[] fullData = saveType == DynBmpInternalType.Scn ? null : new byte[fullDataLen];
            int offset = 0;
            int widthStart = 2;
            int heightStart = nrOfFrames * 2 + 2;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                ArrayUtils.WriteUInt16ToByteArrayLe(framesIndex, widthStart + i * 2, (ushort)frameWidths[i]);
                ArrayUtils.WriteUInt16ToByteArrayLe(framesIndex, heightStart + i * 2, (ushort)frameHeights[i]);
                // This operation is not needed for SCN saving; it builds the array after compressing.
                if (fullData != null)
                {
                    byte[] frameData = frameBytes[i];
                    Array.Copy(frameData, 0, fullData, offset, frameData.Length);
                    offset += frameData.Length;
                }
            }
            chunks.Add(new DynamixChunk("INF", framesIndex));
            DynamixChunk dataChunk;
            byte binCompression;
            byte[] binData;
            uint binDataLen;
            switch (saveType)
            {
                case DynBmpInternalType.Bin:
                case DynBmpInternalType.Ma8:
                    bool isMa8 = saveType == DynBmpInternalType.Ma8;
                    binCompression = 0;
                    binData = fullData;
                    if (isMa8) // MA8 seems to have indices 0 and FF switched
                        DynamixCompression.SwitchBackground(binData);
                    binDataLen = (uint)binData.Length;
                    // TODO find and implement the other types... eventually.
                    switch (compressionType)
                    {
                        case DynBmpInternalCompression.None:
                            break;
                        case DynBmpInternalCompression.Rle:
                            byte[] dataCompr = DynamixCompression.RleEncode(binData);
                            if (dataCompr.Length < binDataLen)
                            {
                                binData = dataCompr;
                                binCompression = (byte)compressionType;
                            }
                            break;
                        case DynBmpInternalCompression.Lzw:
                        case DynBmpInternalCompression.Lzss:
                            throw new FileTypeSaveException("Compression type \"{0}\" is not implemented.", CompressionTypes[(int)compressionType]);
                        default:
                            throw new FileTypeSaveException(ERR_UNKN_COMPR_X, compressionType);
                    }
                    dataChunk = new DynamixChunk(isMa8 ? "MA8" : "BIN", binCompression, binDataLen, binData);
                    chunks.Add(dataChunk);
                    break;
                case DynBmpInternalType.Scn:
                    // I just dumped it in here, because, eh, why not. Can't be arsed to make another parameter.
                    bool addFinalLineWrap = compressionType != DynBmpInternalCompression.None;
                    byte[][] frameBytesCompressed = new byte[nrOfFrames][];
                    byte[] offsets = new byte[nrOfFrames * 4];
                    int curOffset = 0;
                    // Compress all frames
                    for (int i = 0; i < nrOfFrames; ++i)
                    {
                        // Write start indices into the data for the OFF chunk
                        ArrayUtils.WriteInt32ToByteArrayLe(offsets, i << 2, curOffset);
                        // Compress frame
                        byte[] comprFrame;
                        try
                        {
                           comprFrame = DynamixCompression.ScnEncode(frameBytes[i], frameWidths[i], frameHeights[i], bpp, addFinalLineWrap);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
                        }
                        frameBytesCompressed[i] = comprFrame;
                        // Increase index
                        curOffset += comprFrame.Length;
                    }
                    binData = new byte[curOffset];
                    curOffset = 0;
                    // Combine all frames into one array
                    for (int i = 0; i < nrOfFrames; ++i)
                    {
                        byte[] comprFrame = frameBytesCompressed[i];
                        int comprFrameLen = comprFrame.Length;
                        Array.Copy(comprFrame, 0, binData, curOffset, comprFrameLen);
                        curOffset += comprFrameLen;
                    }
                    dataChunk = new DynamixChunk("SCN", binData);
                    chunks.Add(dataChunk);
                    DynamixChunk offChunk = new DynamixChunk("OFF", offsets);
                    chunks.Add(offChunk);
                    break;
                case DynBmpInternalType.BinVga:
                    byte[] vgaData;
                    DynamixCompression.SplitEightBit(fullData, out vgaData, out binData);
                    uint vgaDataLen = (uint)vgaData.Length;
                    binDataLen = (uint)binData.Length;
                    byte compressionVga = 0;
                    binCompression = 0;
                    // TODO find and implement the other types... eventually.
                    switch (compressionType)
                    {
                        case DynBmpInternalCompression.None:
                            break;
                        case DynBmpInternalCompression.Rle:
                            byte[] dataHiCompr = DynamixCompression.RleEncode(vgaData);
                            if (dataHiCompr.Length < vgaDataLen)
                            {
                                vgaData = dataHiCompr;
                                compressionVga = (byte)compressionType;
                            }
                            byte[] dataLoCompr = DynamixCompression.RleEncode(binData);
                            if (dataLoCompr.Length < binDataLen)
                            {
                                binData = dataLoCompr;
                                binCompression = (byte)compressionType;
                            }
                            break;
                        case DynBmpInternalCompression.Lzw:
                        case DynBmpInternalCompression.Lzss:
                            throw new FileTypeSaveException("Compression type \"{0}\" is not implemented.", CompressionTypes[(int)compressionType]);
                        default:
                            throw new FileTypeSaveException(ERR_UNKN_COMPR_X, compressionType);
                    }
                    dataChunk = new DynamixChunk("BIN", binCompression, binDataLen, binData);
                    chunks.Add(dataChunk);
                    DynamixChunk vgaChunk = new DynamixChunk("VGA", compressionVga, vgaDataLen, vgaData);
                    chunks.Add(vgaChunk);
                    break;
            }
            return chunks;
        }

    }
}