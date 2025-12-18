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
        public override Boolean NeedsPalette { get { return this.m_loadedPalette == null; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }

        public override Int32 BitsPerPixel { get { return this.m_bpp; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];
        protected String m_loadedPalette;

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return ArrayUtils.CloneArray(this.m_FramesList); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }

        protected Boolean m_IsMatrixImage;
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return this.m_IsMatrixImage; } }
        protected DynBmpInternalType InternalType { get; private set; }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, false);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename, false);
            this.SetFileNames(filename);
            if (m_loadedPalette != null)
                this.LoadedFileName += "/" + Path.GetExtension(m_loadedPalette).TrimStart('.');
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
                FilePaletteDyn palDyn = CheckForPalette<FilePaletteDyn>(sourcePath);
                if (palDyn != null)
                    this.m_loadedPalette = palDyn.LoadedFile;
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
            Boolean isScn = "SCN:".Equals(dataChunk);
            DynamixChunk matrix = DynamixChunk.ReadChunk(mainChunk.Data, "MTX");
            if (matrix != null && !asMatrixImage)
                throw new FileTypeLoadException("This is a matrix-type image.");
            if (matrix == null && asMatrixImage)
                throw new FileTypeLoadException("This is not a matrix-type image.");
            Byte[] fullData;
            PixelFormat pf;
            if (vqt)
            {
                this.InternalType = DynBmpInternalType.Vqt;
                this.m_bpp = 8;
                pf = PixelFormat.Format8bppIndexed;
                fullData = new Byte[fullDataSize8bit];
                this.ExtraInfo = "Internal type: " + this.InternalType.ToString().ToUpper() + ".\nCurrently unsupported. Frames are blank but given as size reference.";
            }
            else if (isScn)
            {
                this.InternalType = DynBmpInternalType.Scn;
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
                        Int32 bpp = eightBitFound ? 8 : 4;
                        Byte[] decoded = DynamixCompression.ScnDecode(scnChunk.Data, scnOffsets[i], scnOffsets[i] + scnLengths[i], widths[i], heights[i], ref bpp);
                        Array.Copy(decoded, 0, fullData, currOffsOut, decoded.Length);
                        currOffsOut += decoded.Length;
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex), ex);
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
                        throw new FileTypeLoadException("Cannot find BIN chunk!");
                    this.InternalType = DynBmpInternalType.Ma8;
                }
                if (binChunk.Data.Length == 0)
                    throw new FileTypeLoadException("Empty BIN chunk!");
                Int32 compressionType = binChunk.Data[0];
                if (compressionType >= CompressionTypes.Length)
                    throw new FileTypeLoadException("Unknown compression type " + compressionType);
                String compressionStr = "Compression: " + (this.InternalType == DynBmpInternalType.Ma8 ? "MA8" : "BIN") + ":" + CompressionTypes[compressionType];
                Byte[] binData = DynamixCompression.DecodeChunk(binChunk.Data);
                if (this.InternalType == DynBmpInternalType.Ma8) // MA8 seems to have indices 0 and FF switched
                    DynamixCompression.SwitchBackground(binData);
                Byte[] vgaData = null;
                DynamixChunk vgaChunk = DynamixChunk.ReadChunk(mainChunk.Data, "VGA");
                if (vgaChunk == null)
                {
                    this.m_bpp = this.InternalType == DynBmpInternalType.Ma8 ? 8 : 4;
                    this.ExtraInfo = "Internal type: " + (InternalType == DynBmpInternalType.Ma8 ? "MA8" : "BIN");
                }
                else
                {
                    if (vgaChunk.Data.Length == 0)
                        throw new FileTypeLoadException("Empty VGA chunk!");
                    this.InternalType = DynBmpInternalType.BinVga;
                    this.m_bpp = 8;
                    compressionType = vgaChunk.Data[0];
                    if (compressionType < CompressionTypes.Length)
                        compressionStr += ", VGA:" + CompressionTypes[compressionType];
                    else
                        throw new FileTypeLoadException("Unknown compression type " + compressionType);
                    vgaData = DynamixCompression.DecodeChunk(vgaChunk.Data);
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
                frame.SetNeedsPalette(this.m_loadedPalette == null);
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
                Int32 saveType = 0;
                if (bmp != null)
                {
                    switch (bmp.InternalType)
                    {
                        case DynBmpInternalType.Bin: saveType = 0; break;
                        case DynBmpInternalType.Scn: saveType = 1; break;
                    }
                }
                isScn = saveType == 1;
                opts[opt++] = new SaveOption("TYP4", SaveOptionType.ChoicesList, "Save type:", "BIN,SCN", saveType.ToString());
            }
            else
            {
                FileFramesDynBmp bmp = fileToSave as FileFramesDynBmp;
                Int32 saveType = 0;
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
                opts[opt++] = new SaveOption("TYP8", SaveOptionType.ChoicesList, "Save type:", "BIN / VGA,MA8,SCN", saveType.ToString());
            }
            opts[opt++] = new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", SaveCompressionTypes), isScn ? "0" : "1", false, new SaveEnableFilter("TYP4", true, "1"), new SaveEnableFilter("TYP8", true, "2"));
            opts[opt] = new SaveOption("SCL", SaveOptionType.Boolean, "SCN: Include line end at end of compressed data", null, "1", true, new SaveEnableFilter("TYP4", false, "1"), new SaveEnableFilter("TYP8", false, "2"));

            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            DynBmpInternalType saveType = DynBmpInternalType.Unknown;
            if (fileToSave.BitsPerPixel == 4)
            {
                Int32 saveType4;
                if (Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP4"), out saveType4))
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
                Int32 saveType8;
                if (Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP8"), out saveType8))
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
                Boolean lineEnd = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "SCL"));
                // SCN has its own compression. Use the "Compression" parameter to transfer its options instead.
                compressionType = lineEnd ? DynBmpInternalCompression.Rle : DynBmpInternalCompression.None;
            }
            else
            {
                Int32 compression;
                Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compression);
                compressionType = (DynBmpInternalCompression) compression;
            }
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            List<DynamixChunk> basicChunks = this.SaveToChunks(fileToSave, saveType, compressionType);
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
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_NO_FRAMES, "fileToSave");
            if (saveType == DynBmpInternalType.Unknown)
                throw new ArgumentException(ERR_UNKN_COMPR, "saveType");
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
                    if (pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                        throw new ArgumentException(ERR_INPUT_4BPP_8BPP, "fileToSave");
                    bpp = Image.GetPixelFormatSize(pf);
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
            Byte[] fullData = saveType == DynBmpInternalType.Scn ? null : new Byte[fullDataLen];
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
            Byte binCompression;
            Byte[] binData;
            UInt32 binDataLen;
            switch (saveType)
            {
                case DynBmpInternalType.Bin:
                case DynBmpInternalType.Ma8:
                    Boolean isMa8 = saveType == DynBmpInternalType.Ma8;
                    binCompression = 0;
                    binData = fullData;
                    if (isMa8) // MA8 seems to have indices 0 and FF switched
                        DynamixCompression.SwitchBackground(binData);
                    binDataLen = (UInt32)binData.Length;
                    // TODO find and implement the other types... eventually.
                    switch (compressionType)
                    {
                        case DynBmpInternalCompression.None:
                            break;
                        case DynBmpInternalCompression.Rle:
                            Byte[] dataCompr = DynamixCompression.RleEncode(binData);
                            if (dataCompr.Length < binDataLen)
                            {
                                binData = dataCompr;
                                binCompression = (Byte)compressionType;
                            }
                            break;
                        case DynBmpInternalCompression.Lzw:
                        case DynBmpInternalCompression.Lzss:
                            throw new ArgumentException(String.Format("Compression type \"{0}\" is not implemented.", CompressionTypes[(Int32)compressionType]), "compressionType");
                        default:
                            throw new ArgumentException(String.Format(ERR_UNKN_COMPR_X, compressionType), "compressionType");
                    }
                    dataChunk = new DynamixChunk(isMa8 ? "MA8" : "BIN", binCompression, binDataLen, binData);
                    chunks.Add(dataChunk);
                    break;
                case DynBmpInternalType.Scn:
                    // I just dumped it in here, because, eh, why not. Can't be arsed to make another parameter.
                    Boolean addFinalLineWrap = compressionType != DynBmpInternalCompression.None;
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
                    binData = new Byte[curOffset];
                    curOffset = 0;
                    // Combine all frames into one array
                    for (Int32 i = 0; i < nrOfFrames; i++)
                    {
                        Byte[] comprFrame = frameBytesCompressed[i];
                        Int32 comprFrameLen = comprFrame.Length;
                        Array.Copy(comprFrame, 0, binData, curOffset, comprFrameLen);
                        curOffset += comprFrameLen;
                    }
                    dataChunk = new DynamixChunk("SCN", binData);
                    chunks.Add(dataChunk);
                    DynamixChunk offChunk = new DynamixChunk("OFF", offsets);
                    chunks.Add(offChunk);
                    break;
                case DynBmpInternalType.BinVga:
                    Byte[] vgaData;
                    DynamixCompression.SplitEightBit(fullData, out vgaData, out binData);
                    UInt32 vgaDataLen = (UInt32)vgaData.Length;
                    binDataLen = (UInt32)binData.Length;
                    Byte compressionVga = 0;
                    binCompression = 0;
                    // TODO find and implement the other types... eventually.
                    switch (compressionType)
                    {
                        case DynBmpInternalCompression.None:
                            break;
                        case DynBmpInternalCompression.Rle:
                            Byte[] dataHiCompr = DynamixCompression.RleEncode(vgaData);
                            if (dataHiCompr.Length < vgaDataLen)
                            {
                                vgaData = dataHiCompr;
                                compressionVga = (Byte)compressionType;
                            }
                            Byte[] dataLoCompr = DynamixCompression.RleEncode(binData);
                            if (dataLoCompr.Length < binDataLen)
                            {
                                binData = dataLoCompr;
                                binCompression = (Byte)compressionType;
                            }
                            break;
                        case DynBmpInternalCompression.Lzw:
                        case DynBmpInternalCompression.Lzss:
                            throw new ArgumentException(String.Format("Compression type \"{0}\" is not implemented.", CompressionTypes[(Int32)compressionType]), "compressionType");
                        default:
                            throw new ArgumentException(String.Format(ERR_UNKN_COMPR_X, compressionType), "compressionType");
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