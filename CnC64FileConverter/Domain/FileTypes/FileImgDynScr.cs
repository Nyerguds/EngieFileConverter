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
        public override String ShortTypeDescription { get { return "Dynamix Screen file"; } }

        public override Int32 BitsPerColor { get { return this.m_bpp; } }
        public override Int32 ColorsInPalette { get { return m_loadedPalette ? this.m_Palette.Length : 0; } }

        private Boolean m_loadedPalette = false;
        protected Color[] m_Palette = null;
        private Int32 m_bpp = 8;

        public override void SetColors(Color[] palette)
        {
            m_Palette = palette.ToArray();
            base.SetColors(palette);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFile(fileData, filename);
            SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFile(fileData, null);
        }

        public void LoadFile(Byte[] fileData, String sourceFilename)
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
                        if (m_Palette.Length > 0)
                            m_Palette[0] = Color.FromArgb(0, m_Palette[0]);
                        m_loadedPalette = true;
                        LoadedFileName += "/PAL";
                    }
                    catch (FileTypeLoadException) { /* ignore */ }
                }
            }
            if (fileData.Length < 0x10)
                throw new FileTypeLoadException("File is not long enough to be a valid SCR file.");
            DynamixChunk scrChunk = DynamixChunk.GetChunk(fileData, "SCR", true);
            /*/
            // Test to dump compressed data from ADS file
            if (scrChunk == null)
            {

                DynamixChunk adsChunk = DynamixChunk.GetChunk(fileData, "ADS", true);
                if (adsChunk != null)
                {
                    scrChunk = DynamixChunk.GetChunk(adsChunk.Data, "SCR", true);
                    if (scrChunk != null)
                    {
                        Byte compression = scrChunk.Data[0];
                        Int32 uncompressedLength = (Int32)ArrayUtils.ReadIntFromByteArray(scrChunk.Data, 1, 4, true);
                        Byte[] bindata = DynamixCompression.Decode(scrChunk.Data, 5, null, compression, uncompressedLength);
                        //File.WriteAllBytes("scrimagebin.bin", bindata);
                        File.WriteAllBytes((output ?? "scrimage") + "ads-scr.bin", bindata);
                    }
                }
            }
            //*/
            if (scrChunk == null ||scrChunk.Address != 0)
                throw new FileTypeLoadException("File does not start with an SCR chunk!");
            String firstChunk = new String(new Char[] { (Char)fileData[0x08], (Char)fileData[0x09], (Char)fileData[0x0A], (Char)fileData[0x0B] });
            if ("BIN:".Equals(firstChunk) || "VGA:".Equals(firstChunk))
            {
                DynamixChunk vgaChunk = DynamixChunk.GetChunk(scrChunk.Data, "VGA", true);
                if (vgaChunk == null)
                    throw new FileTypeLoadException("Cannot find VGA chunk!");
                Byte[] vgadata = DynamixCompression.DecodeChunk(vgaChunk.Data);
                //save debug output
                //File.WriteAllBytes((output ?? "scrimage") + "vga.bin", vgadata);
                Byte[] bindata = null;
                DynamixChunk binChunk = DynamixChunk.GetChunk(scrChunk.Data, "BIN", true);
                if (binChunk == null)
                    this.m_bpp = 4;
                else
                {
                    if (binChunk.DataLength < 5)
                        throw new FileTypeLoadException("BIN chunk too short!");
                    bindata = DynamixCompression.DecodeChunk(binChunk.Data);
                }
                //save debug output
                //File.WriteAllBytes((output ?? "scrimage") + "bin.bin", bindata);

                //Byte[] uncompressedPic = SaveToBytes(bindata, 0, (UInt32)bindata.Length, vgadata, 0, (UInt32)vgadata.Length);
                //File.WriteAllBytes((output ?? "scrimage") + "_auto_uncompress.scr", uncompressedPic);

                Byte[] fullData;
                PixelFormat pf;
                if (bindata == null)
                {
                    fullData = vgadata;
                    pf = PixelFormat.Format4bppIndexed;
                    if (m_Palette == null)
                        m_Palette = PaletteUtils.GenerateGrayPalette(4, true, false);
                    else
                    {
                        Color[] palette = new Color[16];
                        for (int i = 0; i < 16; i++)
                            palette[i] = this.m_Palette[i * 16 + 3];
                        this.m_Palette = palette;
                    }
                }
                else
                {
                    fullData = new Byte[vgadata.Length * 2];
                    pf = PixelFormat.Format8bppIndexed;
                    if (m_Palette == null)
                        m_Palette = PaletteUtils.GenerateGrayPalette(8, true, false);
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
                this.m_LoadedImage = ImageUtils.BuildImage(fullData, 320, 200, 320 * this.m_bpp / 8, pf, m_Palette, null);
                //save debug output
                //File.WriteAllBytes((output ?? "scrimage") + "_image.bin", fullData);
            }
            else if ("VQT:".Equals(firstChunk))
            {
                throw new FileTypeLoadException("SCR files with VQT section are currently not supported.");
                /*/
                DynamixChunk vqtChunk = DynamixChunk.GetChunk(scrChunk.Data, "VQT", true);
                if (vqtChunk == null)
                    throw new FileTypeLoadException("Cannot find VQT chunk!");
                //*/
            }
            else
                throw new FileTypeLoadException("Cannot find data chunk!");
            // ??? Magic ???
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave)
        {
            Bitmap image = fileToSave.GetBitmap();
            if (image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320x200 images can be saved as CPS!");
            Int32 stride;
            Byte[] data = ImageUtils.GetImageData(image, out stride);
            Byte[] dataHi = new Byte[data.Length / 2];
            Byte[] dataLo = new Byte[data.Length / 2];
            for (Int32 i = 0; i < data.Length; i++)
            {
                Byte pixData = data[i];
                Int32 pixHi = pixData & 0xF0;
                Int32 pixLo = pixData & 0x0F;
                if (i % 2 == 0)
                    pixLo = pixLo << 4;
                else
                    pixHi = pixHi >> 4;
                Int32 pixOffs = i/2;
                dataHi[pixOffs] |= (Byte)pixHi;
                dataLo[pixOffs] |= (Byte)pixLo;
            }
            UInt32 dataHiLen = (UInt32)dataHi.Length;
            UInt32 dataLoLen = (UInt32)dataLo.Length;
            Byte compressionHi = 0;
            Byte compressionLo = 0;
            // optional: add compression
            Byte[] dataHiCompr = DynamixCompression.RleEncode(dataHi);
            if (dataHiCompr.Length < dataHiLen)
            {
                dataHi = dataHiCompr;
                compressionHi = 1;
            }
            Byte[] dataLoCompr = DynamixCompression.RleEncode(dataLo);
            if (dataLoCompr.Length < dataLoLen)
            {
                dataLo = dataLoCompr;
                compressionLo = 1;
            }
            // save
            return SaveToBytes(dataLo, compressionLo, dataLoLen, dataHi, compressionHi, dataHiLen);
        }

        private Byte[] SaveToBytes(Byte[] dataBin, Byte compressionBin, UInt32 dataLenBin, Byte[] dataVga, Byte compressionVga, UInt32 dataLenVga)
        {
            Int32 offset = 0;
            Byte[] fullData = new Byte[34 + dataVga.Length + dataBin.Length];
            Array.Copy(Encoding.ASCII.GetBytes("SCR:"), 0, fullData, offset, 4);
            offset += 4;
            ArrayUtils.WriteIntToByteArray(fullData, offset, 4, true, (UInt32)(fullData.Length - 8) + 0x80000000);
            offset += 4;

            Array.Copy(Encoding.ASCII.GetBytes("BIN:"), 0, fullData, offset, 4);
            offset += 4;
            ArrayUtils.WriteIntToByteArray(fullData, offset, 4, true, (UInt32)dataBin.Length + 5);
            offset += 4;
            fullData[offset] = compressionBin;
            offset += 1;
            ArrayUtils.WriteIntToByteArray(fullData, offset, 4, true, dataLenBin);
            offset += 4;
            Array.Copy(dataBin, 0, fullData, offset, dataBin.Length);
            offset += dataBin.Length;

            Array.Copy(Encoding.ASCII.GetBytes("VGA:"), 0, fullData, offset, 4);
            offset += 4;
            ArrayUtils.WriteIntToByteArray(fullData, offset, 4, true, (UInt32)dataVga.Length + 5);
            offset += 4;
            fullData[offset] = compressionVga;
            offset += 1;
            ArrayUtils.WriteIntToByteArray(fullData, offset, 4, true, dataLenVga);
            offset += 4;
            Array.Copy(dataVga, 0, fullData, offset, dataVga.Length);
            //offset += dataHi.Length;
            return fullData;
        }
        
        private void ReadHeader(Byte[] headerBytes)
        {
        }
    }

}