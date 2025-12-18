using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.Text;
using Nyerguds.GameData.Dynamix;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImgDynScr : SupportedFileType
    {
        public override String[] FileExtensions { get { return new String[] { "scr" }; } }
        public override String ShortTypeName { get { return "Dynamix SCR"; } }
        public override String ShortTypeDescription { get { return "Dynamix Screen file v1"; } }

        public override Int32 BitsPerColor { get { return this.m_bpp; } }
        public override Int32 ColorsInPalette { get { return m_loadedPalette ? this.m_Palette.Length : 0; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.
        /// Dynamix SCR is one of the rare cases where the frames visualisation is completely extra. 
        /// </summary>
        public override Boolean IsFramesContainer { get { return false; } }
        

        protected Boolean m_loadedPalette = false;
        protected Int32 m_bpp = 8;
        private SupportedFileType[] m_FramesList;

        public override void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            palette = palette.ToArray();
            if (this.BitsPerColor == 4)
                palette = Make4BitPalette(palette);
            base.SetColors(palette, updateSource);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            SetFileNames(filename);
            LoadFile(fileData, filename, false, false);
        }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFile(fileData, null, false, false);
        }

        public void LoadFile(Byte[] fileData, String sourceFilename, Boolean v2, Boolean asFrame)
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
                        m_loadedPalette = true;
                    }
                    catch
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
            Byte[] fullData = null;
            PixelFormat pf;

            if (vgadata == null)
            {
                fullData = bindata;
                pf = PixelFormat.Format4bppIndexed;
                if (m_Palette != null)
                    this.m_Palette = Make4BitPalette(this.m_Palette);
                if (m_loadedPalette)
                    this.LoadedFileName += "/PAL";
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
                content[chIndex] = new DynamixChunk("BIN", 0, (UInt32)vgadata.Length, vgadata);
                DynamixChunk fourBitImage = DynamixChunk.BuildChunk("SCR", content);
                FileImgDynScr fourbitFrame = new FileImgDynScr();
                FileImgDynScr eightbitFrame = new FileImgDynScr();
                fourbitFrame.SetFileNames(sourceFilename);
                fourbitFrame.LoadFile(fourBitImage.WriteChunk(), sourceFilename, v2, true);
                fourbitFrame.FrameParent = this;
                eightbitFrame.SetFileNames(sourceFilename);
                eightbitFrame.LoadFile(fileData, sourceFilename, v2, true);
                if (m_loadedPalette)
                    this.LoadedFileName += "/PAL";
                eightbitFrame.FrameParent = this;
                this.m_FramesList[0] = eightbitFrame;
                this.m_FramesList[1] = fourbitFrame;
                pf = PixelFormat.Format8bppIndexed;
                this.m_LoadedImage = eightbitFrame.GetBitmap();
            }
            else
            {
                fullData = new Byte[vgadata.Length * 2];
                pf = PixelFormat.Format8bppIndexed;
                // ENRICHED 4-BIT IMAGE LOGIC
                // Basic principle: The data in the VGA chunk is already perfectly viewable as 4-bit image. The colour palettes
                // are designed so each block of 16 colours consists of different tints of the same colour. The 16-colour palette
                // for the VGA chunk alone can be constructed by taking a palette slice where each colour is 16 entries apart.

                // This VGA data [AB] gets "ennobled" to 8-bit by adding detail data [ab] from the BIN chunk, to get bytes [Aa Bb].
                // The colour palettes are designed so the vga file itself is already viewable as 4-bit image,
                // by using a sub-palette slice where each colour is 16 entries apart.
                for (Int32 i = 0; i < vgadata.Length; i++)
                {
                    Int32 offs = i * 2;
                    // This can be written much simpler, but I expanded it to clearly show each step.
                    Byte vgaPix = vgadata[i]; // 0xAB
                    Byte binPix = bindata[i]; // 0xab
                    Byte vgaPixHi = (Byte)((vgaPix & 0xF0) >> 4); // 0xA0
                    Byte binPixHi = (Byte)((binPix & 0xF0) >> 4); // 0xa0
                    Byte finalPixHi = (Byte)((vgaPixHi << 4) + binPixHi); // Aa
                    Byte vgaPixLo = (Byte)(vgaPix & 0x0F); // 0x0B
                    Byte binPixLo = (Byte)(binPix & 0x0F); // 0x0b
                    Byte finalPixLo = (Byte)((vgaPixLo << 4) + binPixLo); // Bb
                    // Final result: AB + ab == [Aa Bb] - 
                    fullData[offs] = finalPixHi;
                    fullData[offs + 1] = finalPixLo;
                }
                if (m_loadedPalette)
                    this.LoadedFileName += "/PAL";
            }
            if (m_Palette == null)
                m_Palette = PaletteUtils.GenerateGrayPalette(this.m_bpp, null, false);
            if (fullData != null)
                this.m_LoadedImage = ImageUtils.BuildImage(fullData, width, height, ImageUtils.GetMinimumStride(width, this.m_bpp), pf, m_Palette, null);
            //save debug output
            //File.WriteAllBytes((output ?? "scrimage") + "_image.bin", fullData);
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            return SaveToBytesAsThis(fileToSave, dontCompress, false);
        }

        public Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress, Boolean v2)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
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
            scrChunk.IsContainer = true;
            return scrChunk.WriteChunk();
        }

    }

}