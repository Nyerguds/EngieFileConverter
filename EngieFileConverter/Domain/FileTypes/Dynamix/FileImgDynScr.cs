using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Dynamix;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgDynScr : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }

        public override string IdCode { get { return "DynScr"; } }
        public override string[] FileExtensions { get { return new string[] { "scr" }; } }
        public override string ShortTypeName { get { return "Dynamix SCR"; } }
        public override string LongTypeName { get { return "Dynamix Screen file v1"; } }

        protected string[] saveTypes = new string[] { "VGA/BIN", "MA8" };
        protected string[] compressionTypes = new string[] { "None", "RLE", "LZW", "LZSS" };
        protected string[] savecompressionTypes = new string[] { "None", "RLE" /*, "LZW", "LZSS" */};
        public override int BitsPerPixel { get { return this.m_bpp; } }
        public override bool NeedsPalette { get { return this.m_loadedPalette == null; } }

        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames.
        /// Types with frames where this is set to false wil not get an index -1 in the frames list.
        /// Dynamix SCR is one of the rare cases where the frames visualisation is completely extra.
        /// </summary>
        public override bool IsFramesContainer { get { return false; } }
        public bool IsMa8 { get; private set; }

        protected string m_loadedPalette;
        protected int m_bpp = 8;

        public override void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            if (updateSource != null)
            {
                // System to use a vertical slice of the 8-bit palette as 4-bit palette.
                if (this.BitsPerPixel == 4 && updateSource.BitsPerPixel == 8)
                    palette = this.Make4BitPalette(palette);
                else if (this.BitsPerPixel == 8 && updateSource.BitsPerPixel == 4)
                    palette = this.Set4BitPalette(this.m_Palette, palette);
            }
            base.SetColors(palette, updateSource);
        }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, false);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.SetFileNames(filename);
            this.LoadFromFileData(fileData, filename, false);
        }

        public void LoadFromFileData(byte[] fileData, string sourcePath, bool v2)
        {
            if (fileData.Length < 0x10)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            DynamixChunk scrChunk = DynamixChunk.ReadChunk(fileData, "SCR");
            if (scrChunk == null || scrChunk.Address != 0)
                throw new FileTypeLoadException("File does not start with an SCR chunk.");

            string firstChunk = Encoding.ASCII.GetString(fileData, 8, 4);
            int width = 320;
            int height = 200;
            if ("DIM:".Equals(firstChunk))
            {
                if (!v2)
                    throw new FileTypeLoadException("Dimensions chunk is only supported in v2 format.");
            }
            else if (v2)
                throw new FileTypeLoadException("Cannot find image dimensions chunk.");
            // This does not gracefully continue in the autodetect: the type is confirmed, but not supported.
            if ("VQT:".Equals(firstChunk))
                throw new FileTypeLoadException("SCR files with VQT section are currently not supported.");
            if (sourcePath != null)
            {
                FilePaletteDyn palDyn = CheckForPalette<FilePaletteDyn>(sourcePath);
                if (palDyn != null)
                    this.m_loadedPalette = palDyn.LoadedFile;
            }
            this.ExtraInfo = String.Empty;
            DynamixChunk dimChunk = DynamixChunk.ReadChunk(scrChunk.Data, "DIM");
            if (dimChunk != null && dimChunk.DataLength == 4)
            {
                width = ArrayUtils.ReadUInt16FromByteArrayLe(dimChunk.Data, 0);
                height = ArrayUtils.ReadUInt16FromByteArrayLe(dimChunk.Data, 2);
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
                    binChunk = DynamixChunk.ReadChunk(scrChunk.Data, "VGA");
                    if (binChunk == null)
                        throw new FileTypeLoadException("Cannot find BIN chunk.");
                }
            }
            if (binChunk.Data.Length == 0)
                throw new FileTypeLoadException("Empty BIN chunk.");
            int compressionType = binChunk.Data[0];
            if (compressionType < this.compressionTypes.Length)
                this.ExtraInfo = "Compression: " + binChunk.Identifier + ":" + this.compressionTypes[compressionType];
            else
                throw new FileTypeLoadException("Unknown compression type " + compressionType);
            byte[] bindata;
            try
            {
                bindata = DynamixCompression.DecodeChunk(binChunk.Data);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
            }
            if (this.IsMa8) // MA8 seems to have indices 0 and FF switched
                DynamixCompression.SwitchBackground(bindata);
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "vga.bin", vgadata);
            byte[] vgadata = null;
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
                    throw new FileTypeLoadException("Empty VGA chunk.");
                this.m_bpp = 8;
                compressionType = vgaChunk.Data[0];
                if (compressionType < this.compressionTypes.Length)
                    this.ExtraInfo += ", VGA:" + this.compressionTypes[compressionType];
                else
                    throw new FileTypeLoadException("Unknown compression type " + compressionType);
                try
                {
                    vgadata = DynamixCompression.DecodeChunk(vgaChunk.Data);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
                }
            }
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "bin.bin", bindata);
            byte[] fullData;
            PixelFormat pf;

            if (vgadata == null)
            {
                fullData = bindata;
                if (!this.IsMa8)
                {
                    pf = PixelFormat.Format4bppIndexed;
                    if (this.m_Palette != null)
                        this.m_Palette = this.m_Palette.Take(Math.Min(this.m_Palette.Length, 16)).ToArray();
                }
                else
                    pf = PixelFormat.Format8bppIndexed;
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
                return ArrayUtils.CloneArray(col);
            Color[] fourbitpal = new Color[16];
            for (int i = 0; i < 16; ++i)
                fourbitpal[i] = col[i * 16 + 3];
            return fourbitpal;
        }

        protected Color[] Set4BitPalette(Color[] fullPal, Color[] fourbitpal)
        {
            if (fullPal.Length < 256 || fourbitpal.Length != 16)
                return ArrayUtils.CloneArray(fullPal);
            for (int i = 0; i < 16; ++i)
                fullPal[i * 16 + 3] = fourbitpal[i];
            return fullPal;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            Bitmap img = fileToSave.GetBitmap();
            if (img == null)
                return null;
            bool is4bpp = img.PixelFormat == PixelFormat.Format4bppIndexed;
            Option[] opts = new Option[is4bpp ? 1 : 2];
            int opt = 0;
            int compressionType = 1;
            if (!is4bpp)
            {
                FileImgDynScr toSaveScr = fileToSave as FileImgDynScr;
                int saveType = toSaveScr != null && toSaveScr.IsMa8 ? 1 : 0;
                opts[opt++] = new Option("TYP", OptionInputType.ChoicesList, "Save type:", String.Join(",", saveTypes), saveType.ToString());
                if (fileToSave.ExtraInfo != null && fileToSave.ExtraInfo.Contains(saveTypes[saveType] + ":" + this.savecompressionTypes[0]))
                    compressionType = 0;
            }
            else if (fileToSave.ExtraInfo != null && fileToSave.ExtraInfo.Contains("BIN:" + this.savecompressionTypes[0]))
                compressionType = 0;
            opts[opt] = new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", this.savecompressionTypes), compressionType.ToString());
            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            return this.SaveToBytesAsThis(fileToSave, saveOptions, false);
        }

        protected byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions, bool v2)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            int compressionType;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            int saveType;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TYP"), out saveType);
            Bitmap image = fileToSave.GetBitmap();
            if (image.PixelFormat != PixelFormat.Format8bppIndexed && image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new FileTypeSaveException("This format needs 4bpp or 8bpp images.");

            if (!v2 && (image.Width != 320 || image.Height != 200))
                throw new FileTypeSaveException("Dynamix SCR version 1 only supports 320×200 images.");
            List<DynamixChunk> chunks = new List<DynamixChunk>();
            if (v2)
            {
                if (image.Width % 8 !=0)
                    throw new FileTypeSaveException("Dynamix image formats only support image widths divisible by 8.");
                byte[] dimensions = new byte[4];
                ArrayUtils.WriteUInt16ToByteArrayLe(dimensions, 0, (ushort)image.Width);
                ArrayUtils.WriteUInt16ToByteArrayLe(dimensions, 2, (ushort)image.Height);
                DynamixChunk dimChunk = new DynamixChunk("DIM", dimensions);
                chunks.Add(dimChunk);
            }
            int stride;
            // collapse stride should not be needed since this type only handles widths divisible by 8, but whatevs.
            byte[] data = ImageUtils.GetImageData(image, out stride, true);
            if (compressionType < 0 || compressionType > this.compressionTypes.Length)
                throw new FileTypeSaveException(ERR_UNKN_COMPR_X, compressionType);

            // Remove this if LZW actually gets implemented
            if (compressionType == 2)
                throw new FileTypeSaveException("LZW compression is currently not supported.");
            bool asMa8 = saveType == 1;
            if (asMa8) // MA8 seems to have indices 0 and FF switched
                DynamixCompression.SwitchBackground(data);
            if (image.PixelFormat == PixelFormat.Format4bppIndexed || asMa8)
            {
                uint dataLen = (uint)data.Length;
                byte compression = 0;
                if (compressionType != 0)
                {
                    byte[] dataCompr;
                    try
                    {
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
                    }
                    catch (ArgumentException ex)
                    {
                        throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
                    }
                    if (dataCompr == null || dataCompr.Length < dataLen)
                    {
                        data = dataCompr;
                        compression = (byte)compressionType;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk(asMa8? "MA8" : "BIN", compression, dataLen, data);
                chunks.Add(binChunk);
            }
            else
            {
                byte[] dataVga;
                byte[] dataBin;
                DynamixCompression.SplitEightBit(data, out dataVga, out dataBin);

                uint dataLenVga = (uint)dataVga.Length;
                uint dataLenBin = (uint)dataBin.Length;
                byte compressionVga = 0;
                byte compressionBin = 0;
                // optional: add compression
                try
                {
                    if (compressionType != 0)
                    {
                        byte[] dataHiCompr = compressionType == 1 ? DynamixCompression.RleEncode(dataVga) : DynamixCompression.LzwEncode(dataVga);
                        if (dataHiCompr.Length < dataLenVga)
                        {
                            dataVga = dataHiCompr;
                            compressionVga = (byte)compressionType;
                        }
                        byte[] dataLoCompr = compressionType == 1 ? DynamixCompression.RleEncode(dataBin) : DynamixCompression.LzwEncode(dataBin);
                        if (dataLoCompr.Length < dataLenBin)
                        {
                            dataBin = dataLoCompr;
                            compressionBin = (byte)compressionType;
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
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