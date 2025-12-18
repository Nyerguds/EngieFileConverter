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
    public class FileImgDynScr : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }

        public override String[] FileExtensions { get { return new String[] { "scr" }; } }
        public override String ShortTypeName { get { return "Dynamix SCR"; } }
        public override String ShortTypeDescription { get { return "Dynamix Screen file v1"; } }

        protected String[] compressionTypes = new String[] { "None", "RLE", "LZW", "LZSS" };
        protected String[] savecompressionTypes = new String[] { "None", "RLE" /*, "LZW", "LZSS" */};
        public override Int32 BitsPerPixel { get { return this.m_bpp; } }
        public override Int32 ColorsInPalette { get { return this.m_loadedPalette ? this.m_Palette.Length : 0; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        // The SetColors function takes care of this. Return true to negate the automatic effect of IsFramescontainer.
        public override Boolean FramesHaveCommonPalette { get { return true; } }

        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.
        /// Dynamix SCR is one of the rare cases where the frames visualisation is completely extra. 
        /// </summary>
        public override Boolean IsFramesContainer { get { return false; } }
        public Boolean IsMa8 { get; private set; }
                
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Bitmap img = fileToSave.GetBitmap();
            if (img == null)
                return null;
            Boolean is4bpp = img.PixelFormat == PixelFormat.Format4bppIndexed;
            SaveOption[] opts = new SaveOption[is4bpp ? 1 : 2];
            Int32 opt = 0;
            if (!is4bpp)
            {
                Int32 saveType = fileToSave is FileImgDynScr && ((FileImgDynScr)fileToSave).IsMa8 ? 1 : 0;
                opts[opt++] = new SaveOption("TYP", SaveOptionType.ChoicesList, "Save type:", "VGA/BIN,MA8", saveType.ToString());
            }
            opts[opt] = new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.savecompressionTypes), 1.ToString());
            return opts;
        }

        protected Boolean m_loadedPalette = false;
        protected Int32 m_bpp = 8;
        private SupportedFileType[] m_FramesList;

        public override void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            if (palette.Length == 16)
                return; // Don't propagate reduced palette to lower level.
            if (this.BitsPerPixel == 4)
                palette = this.Make4BitPalette(palette);
            base.SetColors(palette, updateSource);
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null, false, false);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.SetFileNames(filename);
            this.LoadFile(fileData, filename, false, false);
        }
        
        public void LoadFile(Byte[] fileData, String sourcePath, Boolean v2, Boolean asFrame)
        {
            if (fileData.Length < 0x10)
                throw new FileTypeLoadException("File is not long enough to be a valid SCR file.");
            DynamixChunk scrChunk = DynamixChunk.ReadChunk(fileData, "SCR");
            if (scrChunk == null || scrChunk.Address != 0)
                throw new FileTypeLoadException("File does not start with an SCR chunk!");
            String firstChunk = new String(new Char[] {(Char)fileData[0x08], (Char)fileData[0x09], (Char)fileData[0x0A], (Char)fileData[0x0B]});
            Int32 width = 320;
            Int32 height = 200;
            if ("DIM:".Equals(firstChunk))
            {
                if (!v2)
                    throw new FileTypeLoadException("Dimensions chunk is only supported in v2 format.");
            }
            else if (v2)
                throw new FileTypeLoadException("Cannot find image dimensions chunk!");
            // This does not gracefully continue in the autodetect: the type is confirmed, but not supported.
            if ("VQT:".Equals(firstChunk))
                throw new NotSupportedException("SCR files with VQT section are currently not supported.");
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
            this.ExtraInfo = String.Empty;
            DynamixChunk dimChunk = DynamixChunk.ReadChunk(scrChunk.Data, "DIM");
            if (dimChunk != null && dimChunk.DataLength == 4)
            {
                width = (Int32)ArrayUtils.ReadIntFromByteArray(dimChunk.Data, 0, 2, true);
                height = (Int32)ArrayUtils.ReadIntFromByteArray(dimChunk.Data, 2, 2, true);
            }
            DynamixChunk binChunk = DynamixChunk.ReadChunk(scrChunk.Data, "BIN");
            this.IsMa8 = false;
            if (binChunk == null)
            {
                binChunk = DynamixChunk.ReadChunk(scrChunk.Data, "MA8");
                if (binChunk != null)
                {
                    this.IsMa8 = true;
                }
                else
                {
                    if (asFrame)
                        binChunk = DynamixChunk.ReadChunk(scrChunk.Data, "VGA");
                    if (binChunk == null)
                        throw new FileTypeLoadException("Cannot find BIN chunk!");
                }
            }
            if (binChunk.Data.Length == 0)
                throw new FileTypeLoadException("Empty BIN chunk!");
            Int32 compressionType = binChunk.Data[0];
            if (compressionType < this.compressionTypes.Length)
                this.ExtraInfo = "Compression: " + binChunk.Identifier + ":" + this.compressionTypes[compressionType];
            else
                throw new FileTypeLoadException("Unknown compression type " + compressionType);
            Byte[] bindata = DynamixCompression.DecodeChunk(binChunk.Data);
            if (this.IsMa8) // MA8 seems to have indices 0 and FF switched
                DynamixCompression.SwitchBackground(bindata);
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "vga.bin", vgadata);
            Byte[] vgadata = null;
            DynamixChunk vgaChunk = binChunk.Identifier == "VGA" ? null : DynamixChunk.ReadChunk(scrChunk.Data, "VGA");
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
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "bin.bin", bindata);
            Byte[] fullData = null;
            PixelFormat pf;

            if (vgadata == null)
            {
                fullData = bindata;
                if (!this.IsMa8)
                {
                    pf = PixelFormat.Format4bppIndexed;
                    if (this.m_Palette != null)
                    {
                        if (asFrame)
                            this.m_Palette = this.Make4BitPalette(this.m_Palette);
                        else
                            this.m_Palette = this.m_Palette.Take(Math.Min(this.m_Palette.Length, 16)).ToArray();
                    }
                }
                else
                    pf = PixelFormat.Format8bppIndexed;
            }
            else if (!asFrame)
            {
                this.m_FramesList = new SupportedFileType[2];
                DynamixChunk[] content = new DynamixChunk[dimChunk == null ? 1 : 2];
                Int32 chIndex = 0;
                if (dimChunk != null)
                {
                    content[0] = dimChunk;
                    chIndex++;
                }
                content[chIndex] = vgaChunk;
                DynamixChunk fourBitImage = DynamixChunk.BuildChunk("SCR", content);
                FileImgDynScr fourbitFrame = new FileImgDynScr();
                FileImgDynScr eightbitFrame = new FileImgDynScr();
                fourbitFrame.SetFileNames(sourcePath);
                fourbitFrame.LoadFile(fourBitImage.WriteChunk(), sourcePath, v2, true);
                fourbitFrame.FrameParent = this;
                eightbitFrame.SetFileNames(sourcePath);
                eightbitFrame.LoadFile(fileData, sourcePath, v2, true);
                if (this.m_loadedPalette)
                    this.LoadedFileName += "/PAL";
                eightbitFrame.FrameParent = this;
                this.m_FramesList[0] = eightbitFrame;
                this.m_FramesList[1] = fourbitFrame;
                pf = PixelFormat.Format8bppIndexed;
                this.m_LoadedImage = eightbitFrame.GetBitmap();
            }
            else
            {
                pf = PixelFormat.Format8bppIndexed;
                fullData = DynamixCompression.EnrichFourBit(vgadata, bindata);
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.m_bpp, null, false);
            if (fullData != null)
                this.m_LoadedImage = ImageUtils.BuildImage(fullData, width, height, ImageUtils.GetMinimumStride(width, this.m_bpp), pf, this.m_Palette, null);
        }

        protected Color[] Make4BitPalette(Color[] col)
        {
            if (col.Length < 256)
                return col.ToArray();
            Color[] fourbitpal = new Color[16];
            for (Int32 i = 0; i < 16; i++)
                fourbitpal[i] = col[i * 16 + 3];
            return fourbitpal;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return this.SaveToBytesAsThis(fileToSave, saveOptions, false);
        }

        protected Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean v2)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            Int32 saveType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYP"), out saveType);
            Bitmap image = fileToSave.GetBitmap();
            if (image.PixelFormat != PixelFormat.Format8bppIndexed && image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new NotSupportedException("Only 4-bit or 8-bit images can be saved as Dynamix SCR!");

            if (!v2 && (image.Width != 320 || image.Height != 200))
                throw new NotSupportedException("Only 320x200 images can be saved as Dynamix SCR v1!");
            List<DynamixChunk> chunks = new List<DynamixChunk>();
            if (v2)
            {
                if (image.Width % 8 !=0)
                    throw new NotSupportedException("Dynamix image formats only support image widths divisible by 8!");
                Byte[] dimensions = new Byte[4];
                ArrayUtils.WriteIntToByteArray(dimensions, 0, 2, true, (UInt32)image.Width);
                ArrayUtils.WriteIntToByteArray(dimensions, 2, 2, true, (UInt32)image.Height);
                DynamixChunk dimChunk = new DynamixChunk("DIM", dimensions);
                chunks.Add(dimChunk);
            }
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(image, out stride);
            if (compressionType < 0 || compressionType > this.compressionTypes.Length)
                throw new NotSupportedException("Unknown compression type " + compressionType);

            // Remove this if LZW actually gets implemented
            if (compressionType == 2)
                throw new NotSupportedException("LZW compression is currently not supported!");
            Boolean asMa8 = saveType == 1;
            if (asMa8) // MA8 seems to have indices 0 and FF switched
                DynamixCompression.SwitchBackground(data);
            if (image.PixelFormat == PixelFormat.Format4bppIndexed || asMa8)
            {
                UInt32 dataLen = (UInt32)data.Length;
                Byte compression = 0;
                if (compressionType != 0)
                {
                    Byte[] dataCompr;
                    switch (compressionType)
                    {
                        case 1:
                            dataCompr = DynamixCompression.RleEncode(data);
                            break;
                        case 2:
                            dataCompr = DynamixCompression.LzwEncode(data);
                            break;
                        case 3:
                            dataCompr = DynamixCompression.LzssEncode(data);
                            break;
                        default:
                            dataCompr = null;
                            break;
                    }
                    if (dataCompr == null || dataCompr.Length < dataLen)
                    {
                        data = dataCompr;
                        compression = (Byte)compressionType;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk(asMa8? "MA8" : "BIN", compression, dataLen, data);
                chunks.Add(binChunk);
            }
            else
            {
                Byte[] dataVga;
                Byte[] dataBin;
                DynamixCompression.SplitEightBit(data, out dataVga, out dataBin);

                UInt32 dataLenVga = (UInt32)dataVga.Length;
                UInt32 dataLenBin = (UInt32)dataBin.Length;
                Byte compressionVga = 0;
                Byte compressionBin = 0;
                // optional: add compression
                if (compressionType != 0)
                {
                    Byte[] dataHiCompr = compressionType == 1 ? DynamixCompression.RleEncode(dataVga) : DynamixCompression.LzwEncode(dataVga);
                    if (dataHiCompr.Length < dataLenVga)
                    {
                        dataVga = dataHiCompr;
                        compressionVga = (Byte)compressionType;
                    }
                    Byte[] dataLoCompr = compressionType == 1 ? DynamixCompression.RleEncode(dataBin) : DynamixCompression.LzwEncode(dataBin);
                    if (dataLoCompr.Length < dataLenBin)
                    {
                        dataBin = dataLoCompr;
                        compressionBin = (Byte)compressionType;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk("BIN", compressionBin, dataLenBin, dataBin);
                chunks.Add(binChunk);
                DynamixChunk vgaChunk = new DynamixChunk("VGA", compressionVga, dataLenVga, dataVga);
                chunks.Add(vgaChunk);
            }
            DynamixChunk scrChunk = DynamixChunk.BuildChunk("SCR", chunks.ToArray());
            scrChunk.IsContainer = true;
            return scrChunk.WriteChunk();
        }

    }

}