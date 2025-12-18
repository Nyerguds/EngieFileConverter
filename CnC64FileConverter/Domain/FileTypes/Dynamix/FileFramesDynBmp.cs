using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.GameData.Dynamix;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileFramesDynBmp : SupportedFileType
    {
        protected Int32 m_bpp;

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Dynamix BMP"; } }
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        public override String ShortTypeDescription { get { return "Dynamix BMP sprites file"; } }

        protected String[] compressionTypes = new String[] { "None", "RLE", "LZW", "LZSS" };
        protected String[] savecompressionTypes = new String[] { "None", "RLE", "LZSS" };
        //protected String[] endchunks = new String[] { "None", "OFF (trims X and Y)" };
        public override Int32 ColorsInPalette { get { return this.m_loadedPalette ? this.m_Palette.Length : 0; } }

        public override Int32 BitsPerPixel { get { return this.m_bpp; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];
        protected Boolean m_loadedPalette;

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }

        protected Boolean m_IsMatrixImage;
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return this.m_IsMatrixImage; } }
        public Boolean IsMa8 { get; private set; }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Boolean is4bpp = fileToSave.BitsPerPixel == 4;
            SaveOption[] opts = new SaveOption[is4bpp ? 1 : 2];
            Int32 opt = 0;
            if (!is4bpp)
            {
                FileFramesDynBmp bmp = fileToSave as FileFramesDynBmp;
                Int32 saveType = bmp != null && bmp.IsMa8 ? 1 : 0;
                opts[opt++] = new SaveOption("TYP", SaveOptionType.ChoicesList, "Save type:", "VGA/BIN,MA8", saveType.ToString());
            }
            opts[opt] = new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.savecompressionTypes), 1.ToString());
            return opts;
        }
        
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
            Int32 frames = (Int32)ArrayUtils.ReadIntFromByteArray(frameInfo, 0, 2, true);
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
            for (Int32 i = 0; i < frames; i++)
            {
                widths[i] = (Int32)ArrayUtils.ReadIntFromByteArray(frameInfo, widthStart + i * 2, 2, true);
                heights[i] = (Int32)ArrayUtils.ReadIntFromByteArray(frameInfo, heightStart + i * 2, 2, true);
                fullDataSize8bit += (widths[i] * heights[i]);
            }
            Int32 addr2 = infChunk.Address + infChunk.Length;
            if (fileData.Length < addr2+0x0B)
                throw new FileTypeLoadException("File not long enough to find data chunk.");
            String dataChunk = new String(new Char[] { (Char)fileData[addr2 + 0x08], (Char)fileData[addr2 + 0x09], (Char)fileData[addr2 + 0x0A], (Char)fileData[addr2 + 0x0B] });
            Boolean vqt = "VQT:".Equals(dataChunk);
            Boolean scn = "SCN:".Equals(dataChunk);
            DynamixChunk matrix = DynamixChunk.ReadChunk(mainChunk.Data, "MTX");
            if (matrix != null && !asMatrixImage)
                throw new FileTypeLoadException("This is not a matrix-type image.");
            if (matrix == null && asMatrixImage)
                throw new FileTypeLoadException("This is a matrix-type image.");
            Byte[] fullData;
            PixelFormat pf;
            if (vqt || scn)
            {
                this.m_bpp = 8;
                pf = PixelFormat.Format8bppIndexed;
                fullData = new Byte[fullDataSize8bit];
                this.ExtraInfo = "File uses unsupported " + dataChunk.TrimEnd(':') + " format. Frames are blank but given as size reference.";
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
                if (compressionType < this.compressionTypes.Length)
                    this.ExtraInfo = "Compression: " + (this.IsMa8 ? "MA8" : "BIN") + ":" + this.compressionTypes[compressionType];
                else
                    throw new FileTypeLoadException("Unknown compression type " + compressionType);
                Byte[] bindata = DynamixCompression.DecodeChunk(binChunk.Data);
                if (this.IsMa8) // MA8 seems to have indices 0 and FF switched
                    DynamixCompression.SwitchBackground(bindata);
                Byte[] vgadata = null;
                DynamixChunk vgaChunk = DynamixChunk.ReadChunk(mainChunk.Data, "VGA");
                if (vgaChunk == null)
                {
                    if (!this.IsMa8)
                        this.m_bpp = 4;
                    else
                        this.m_bpp = 8;
                }
                else
                {
                    if (vgaChunk.Data.Length == 0)
                        throw new FileTypeLoadException("Empty VGA chunk!");
                    this.m_bpp = 8;
                    compressionType = vgaChunk.Data[0];
                    if (compressionType < this.compressionTypes.Length)
                        this.ExtraInfo += ", VGA:" + this.compressionTypes[compressionType];
                    else
                        throw new FileTypeLoadException("Unknown compression type " + compressionType);
                    vgadata = DynamixCompression.DecodeChunk(vgaChunk.Data);
                }
                if (vgadata == null)
                {
                    fullData = bindata;
                    if (!this.IsMa8)
                        pf = PixelFormat.Format4bppIndexed;
                    else
                        pf = PixelFormat.Format8bppIndexed;
                }
                else
                {
                    pf = PixelFormat.Format8bppIndexed;
                    fullData = DynamixCompression.EnrichFourBit(vgadata, bindata);
                }
            }
            if (!this.m_loadedPalette || this.m_bpp == 4)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.m_bpp, null, false);

            Int32 offset = 0;
            this.m_FramesList = new SupportedFileType[frames];
            Byte[][] framesData = null;
            if (matrix != null)
                framesData = new Byte[frames][];
            for (Int32 i = 0; i < frames; i++)
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
                frame.SetColorsInPalette(this.m_loadedPalette ? this.m_Palette.Length : 0);
                frame.SetTransparencyMask(this.TransparencyMask);
                this.m_FramesList[i] = frame;
            }
            if (matrix != null && frames > 0)
            {
                Int32 blockWidth = widths[0];
                Int32 blockHeight = heights[0];
                if (widths.Any(w => w != blockWidth))
                    return;
                if (heights.Any(h => h != blockHeight))
                    return;
                Byte[] matrixData = matrix.Data;
                Int32 matrixWidth = (Int16)ArrayUtils.ReadIntFromByteArray(matrixData, 0, 2, true);
                Int32 matrixHeight = (Int16)ArrayUtils.ReadIntFromByteArray(matrixData, 2, 2, true);
                Int32 matrixLen = matrixHeight * matrixWidth;
                if(matrixLen < frames)
                    return;
                if ((matrixData.Length - 4) / 2 != matrixLen)
                    return;
                Byte[][] matrixFrames = new Byte[matrixLen][];
                for (Int32 i = 0; i < matrixLen; i++)
                {
                    Int32 frame = (Int16)ArrayUtils.ReadIntFromByteArray(matrixData, 4 + i * 2, 2, true);
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Int32 saveType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP"), out saveType);
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            List<DynamixChunk> basicChunks = this.SaveToChunks(fileToSave, compressionType, saveType);
            DynamixChunk bmpChunk = DynamixChunk.BuildChunk("BMP", basicChunks.ToArray());
            return bmpChunk.WriteChunk();
        }

        protected List<DynamixChunk> SaveToChunks(SupportedFileType fileToSave, Int32 compressionType, Int32 saveType)
        {
            if (fileToSave == null)
                throw new NotSupportedException("File to save is empty!");
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");

            if (compressionType < 0 || compressionType > this.compressionTypes.Length)
                throw new NotSupportedException("Unknown compression type " + compressionType);

            // Remove this if LZW actually gets implemented
            if (compressionType == 2)
                throw new NotSupportedException("LZW compression is currently not supported!");

            // write save logic for frames
            PixelFormat pf = PixelFormat.Undefined;
            Int32 bpp = 0;
            Int32 frames = fileToSave.Frames.Length;
            Byte[][] frameBytes = new Byte[frames][];
            Int32[] frameWidths = new Int32[frames];
            Int32[] frameHeights = new Int32[frames];
            for (Int32 i = 0; i < fileToSave.Frames.Length; i++)
            {
                SupportedFileType frame = fileToSave.Frames[i];
                Bitmap bm = frame.GetBitmap();
                if (bm == null)
                    throw new NotSupportedException("Dynamix BMP cannot handle empty frames!");
                if (bm.Width % 8 != 0)
                    throw new NotSupportedException("Dynamix image formats only support image widths divisible by 8!");
                if (pf == PixelFormat.Undefined)
                {
                    pf = bm.PixelFormat;
                    bpp = Image.GetPixelFormatSize(pf);
                    if (pf != PixelFormat.Format4bppIndexed && pf != PixelFormat.Format8bppIndexed)
                        throw new NotSupportedException("Dynamix BMP frames must be 4bpp or 8bpp!");
                }
                else if (pf != bm.PixelFormat)
                    throw new NotSupportedException("All frames must have the same color depth!");
                Int32 stride;
                Int32 width = bm.Width;
                Int32 height = bm.Height;
                Byte[] frameData = ImageUtils.GetImageData(bm, out stride);
                frameBytes[i] = ImageUtils.CollapseStride(frameData, width, height, bpp, ref stride);
                frameWidths[i] = width;
                frameHeights[i] = height;
            }
            List<DynamixChunk> chunks = new List<DynamixChunk>();
            Int32 fullDataLen = frameBytes.Sum(x => x.Length);
            Byte[] framesIndex = new Byte[frames * 4 + 2];
            ArrayUtils.WriteIntToByteArray(framesIndex, 0, 2, true, (UInt32)frames);
            Byte[] fullData = new Byte[fullDataLen];
            Int32 offset = 0;
            Int32 widthStart = 2;
            Int32 heightStart = frames * 2 + 2;
            for (Int32 i = 0; i < frames; i++)
            {
                ArrayUtils.WriteIntToByteArray(framesIndex, widthStart + i * 2, 2, true, (UInt32)frameWidths[i]);
                ArrayUtils.WriteIntToByteArray(framesIndex, heightStart + i * 2, 2, true, (UInt32)frameHeights[i]);
                Byte[] frameData = frameBytes[i];
                Array.Copy(frameData, 0, fullData, offset, frameData.Length);
                offset += frameData.Length;
            }
            chunks.Add(new DynamixChunk("INF", framesIndex));
            Boolean isMa8 = saveType == 1;
            if (bpp == 4 || isMa8)
            {
                Byte compressionBin = 0;
                Byte[] binData = fullData;
                if (isMa8) // MA8 seems to have indices 0 and FF switched
                    DynamixCompression.SwitchBackground(binData);
                UInt32 dataLenBin = (UInt32)binData.Length;
                if (compressionType != 0) 
                {
                    Byte[] dataCompr = compressionType == 1 ? DynamixCompression.RleEncode(binData) : DynamixCompression.LzwEncode(binData);
                    if (dataCompr.Length < dataLenBin)
                    {
                        binData = dataCompr;
                        compressionBin = (Byte)compressionType;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk(isMa8 ? "MA8" : "BIN", compressionBin, dataLenBin, binData);
                chunks.Add(binChunk);
            }
            else
            {
                Byte[] vgaData;
                Byte[] binData;
                DynamixCompression.SplitEightBit(fullData, out vgaData, out binData);
                UInt32 dataLenVga = (UInt32)vgaData.Length;
                UInt32 dataLenBin = (UInt32)binData.Length;
                Byte compressionVga = 0;
                Byte compressionBin = 0;
                // optional: add compression
                if (compressionType != 0)
                {
                    Byte[] dataHiCompr = compressionType == 1 ? DynamixCompression.RleEncode(vgaData) : DynamixCompression.LzwEncode(vgaData);
                    if (dataHiCompr.Length < dataLenVga)
                    {
                        vgaData = dataHiCompr;
                        compressionVga = (Byte)compressionType;
                    }
                    Byte[] dataLoCompr = compressionType == 1 ? DynamixCompression.RleEncode(binData) : DynamixCompression.LzwEncode(binData);
                    if (dataLoCompr.Length < dataLenBin)
                    {
                        binData = dataLoCompr;
                        compressionBin = (Byte)compressionType;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk("BIN", compressionBin, dataLenBin, binData);
                chunks.Add(binChunk);
                DynamixChunk vgaChunk = new DynamixChunk("VGA", compressionVga, dataLenVga, vgaData);
                chunks.Add(vgaChunk);
            }
            return chunks;
        }

    }
}