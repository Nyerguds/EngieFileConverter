using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImgDynScr : SupportedFileType
    {
        public override String[] FileExtensions { get { return new String[] { "scr" }; } }
        public override String ShortTypeName { get { return "DynScr"; } }
        public override String ShortTypeDescription { get { return "Dynamix Screen file v1"; } }

        public override Int32 BitsPerColor { get { return this.m_bpp; } }
        public override Int32 ColorsInPalette { get { return m_loadedPalette ? this.m_Palette.Length : 0; } }

        protected Boolean m_loadedPalette = false;
        protected Color[] m_Palette = null;
        protected Int32 m_bpp = 8;

        public override void SetColors(Color[] palette)
        {
            m_Palette = palette.ToArray();
            base.SetColors(palette);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            SetFileNames(filename);
            LoadFile(fileData, filename, false);
        }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFile(fileData, null, false);
        }

        public void LoadFile(Byte[] fileData, String sourceFilename, Boolean v2)
        {
            String output = null;
            if (sourceFilename != null)
            {
                output = Path.Combine(Path.GetDirectoryName(sourceFilename), Path.GetFileNameWithoutExtension(sourceFilename));
                String palName = output + ".pal";
                if (File.Exists(palName))
                {
                    try
                    {
                        FilePaletteDyn palDyn = new FilePaletteDyn();
                        palDyn.LoadFile(palName);
                        m_Palette = palDyn.GetColors();
                        //if (m_Palette.Length > 0)
                        //    m_Palette[0] = Color.FromArgb(0, m_Palette[0]);
                        m_loadedPalette = true;
                        LoadedFileName += "/PAL";
                    }
                    catch (FileTypeLoadException)
                    {
                        /* ignore */
                    }
                }
            }
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
            if ("VQT:".Equals(firstChunk))
                throw new FileTypeLoadException("SCR files with VQT section are currently not supported.");

            DynamixChunk dimChunk = DynamixChunk.ReadChunk(scrChunk.Data, "DIM");
            if (dimChunk != null && dimChunk.DataLength == 4)
            {
                width = (Int32)ArrayUtils.ReadIntFromByteArray(dimChunk.Data, 0, 2, true);
                height = (Int32)ArrayUtils.ReadIntFromByteArray(dimChunk.Data, 2, 2, true);
            }
            DynamixChunk binChunk = DynamixChunk.ReadChunk(scrChunk.Data, "BIN");
            if (binChunk == null)
                throw new FileTypeLoadException("Cannot find BIN chunk!");
            Byte[] bindata = DynamixCompression.DecodeChunk(binChunk.Data);
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "vga.bin", vgadata);
            Byte[] vgadata = null;
            DynamixChunk vgaChunk = DynamixChunk.ReadChunk(scrChunk.Data, "VGA");
            if (vgaChunk == null)
            {
                this.m_bpp = 4;
            }
            else
            {
                this.m_bpp = 8;
                vgadata = DynamixCompression.DecodeChunk(vgaChunk.Data);
            }
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "bin.bin", bindata);
            Byte[] fullData;
            PixelFormat pf;
            if (vgadata == null)
            {
                fullData = bindata;
                pf = PixelFormat.Format4bppIndexed;
                if (m_Palette != null)
                {
                    Color[] palette = new Color[16];
                    for (Int32 i = 0; i < 16; i++)
                        palette[i] = this.m_Palette[v2 ? i : i * 16 + 3];
                    this.m_Palette = palette;
                }
            }
            else
            {
                fullData = new Byte[vgadata.Length * 2];
                pf = PixelFormat.Format8bppIndexed;
                for (Int32 i = 0; i < vgadata.Length; i++)
                {
                    Int32 offs = i * 2;
                    Byte binPix = bindata[i]; // 0x11
                    Byte vgaPix = vgadata[i]; // 0x33
                    //vga + bin = [vga|bin]; 3 + 1  => 31
                    Byte binPix1 = (Byte)(binPix & 0x0F); // 0x01
                    Byte vgaPix1 = (Byte)(vgaPix & 0x0F); // 0x03
                    Byte finalPix1 = (Byte)((vgaPix1 << 4) + binPix1);
                    Byte binPix2 = (Byte)((binPix & 0xF0) >> 4); // 0x10
                    Byte vgaPix2 = (Byte)((vgaPix & 0xF0) >> 4); // 0x30
                    Byte finalPix2 = (Byte)((vgaPix2 << 4) + binPix2);
                    fullData[offs] = finalPix2;
                    fullData[offs + 1] = finalPix1;
                }
            }
            if (m_Palette == null)
                m_Palette = PaletteUtils.GenerateGrayPalette(this.m_bpp, false, false);
            this.m_LoadedImage = ImageUtils.BuildImage(fullData, width, height, ImageUtils.GetMinimumStride(width, this.m_bpp), pf, m_Palette, null);
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "_image.bin", fullData);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            return SaveToBytesAsThis(fileToSave, dontCompress, false);
        }

        public Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress, Boolean v2)
        {
            Bitmap image = fileToSave.GetBitmap();
            if (image.PixelFormat != PixelFormat.Format8bppIndexed && image.PixelFormat != PixelFormat.Format4bppIndexed)
                throw new NotSupportedException("Only 4-bit or 8-bit images can be saved as Dynamix SCR!");

            if (!v2 && (image.Width != 320 || image.Height != 200))
                throw new NotSupportedException("Only 320x200 images can be saved as Dynamix SCR v1!");
            List<DynamixChunk> chunks = new List<DynamixChunk>();
            if (v2)
            {
                Byte[] dimensions = new Byte[4];
                ArrayUtils.WriteIntToByteArray(dimensions, 0, 2, true, (UInt32)image.Width);
                ArrayUtils.WriteIntToByteArray(dimensions, 2, 2, true, (UInt32)image.Height);
                DynamixChunk dimChunk = new DynamixChunk("DIM", dimensions);
                chunks.Add(dimChunk);
            }
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(image, out stride);
            if (image.PixelFormat == PixelFormat.Format4bppIndexed)
            {
                UInt32 dataLen = (UInt32)data.Length;
                Byte compression = 0;
                if (!dontCompress)
                {
                    Byte[] dataCompr = DynamixCompression.RleEncode(data);
                    if (dataCompr.Length < dataLen)
                    {
                        data = dataCompr;
                        compression = 1;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk("BIN", compression, dataLen, data);
                chunks.Add(binChunk);
            }
            else
            {
                Byte[] dataVga = new Byte[data.Length / 2];
                Byte[] dataBin = new Byte[data.Length / 2];
                for (Int32 i = 0; i < data.Length; i++)
                {
                    Byte pixData = data[i];
                    Int32 pixHi = pixData & 0xF0;
                    Int32 pixLo = pixData & 0x0F;
                    if (i % 2 == 0)
                        pixLo = pixLo << 4;
                    else
                        pixHi = pixHi >> 4;
                    Int32 pixOffs = i / 2;
                    dataVga[pixOffs] |= (Byte)pixHi;
                    dataBin[pixOffs] |= (Byte)pixLo;
                }
                UInt32 dataLenVga = (UInt32)dataVga.Length;
                UInt32 dataLenBin = (UInt32)dataBin.Length;
                Byte compressionVga = 0;
                Byte compressionBin = 0;
                // optional: add compression
                if (!dontCompress)
                {
                    Byte[] dataHiCompr = DynamixCompression.RleEncode(dataVga);
                    if (dataHiCompr.Length < dataLenVga)
                    {
                        dataVga = dataHiCompr;
                        compressionVga = 1;
                    }
                    Byte[] dataLoCompr = DynamixCompression.RleEncode(dataBin);
                    if (dataLoCompr.Length < dataLenBin)
                    {
                        dataBin = dataLoCompr;
                        compressionBin = 1;
                    }
                }
                DynamixChunk binChunk = new DynamixChunk("BIN", compressionBin, dataLenBin, dataBin);
                chunks.Add(binChunk);
                DynamixChunk vgaChunk = new DynamixChunk("VGA", compressionVga, dataLenVga, dataVga);
                chunks.Add(vgaChunk);
            }
            DynamixChunk scrChunk = DynamixChunk.BuildChunk("SCR", chunks.ToArray());
            scrChunk.BitFlag = true;
            return scrChunk.WriteChunk();
        }
       
    }

}