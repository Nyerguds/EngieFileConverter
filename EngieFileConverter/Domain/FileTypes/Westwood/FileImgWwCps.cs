using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.GameData.Compression;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgWwCps : SupportedFileType
    {
        private static readonly PixelFormatter Format16BitRgbaX444Be = new PixelFormatter(2, 0x0000, 0x0F00, 0x00F0, 0x000F, false);
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width = 320;
        protected Int32 m_Height = 200;
        public Boolean HasPalette { get; protected set; }
        public Int32 CompressionType { get; protected set; }
        public CpsVersion CpsVersion { get; protected set; }
        protected String[] compressionTypes = new String[] { "No compression", "LZW-12", "LZW-14", "RLE", "LCW" };

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood CPS"; } }
        public override String[] FileExtensions { get { return new String[] {"cps"}; } }
        public override String ShortTypeDescription { get { return "Westwood CPS File"; } }
        public override Int32 ColorsInPalette { get { return this.HasPalette ? this.m_Palette.Length : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            Byte[] imageData = GetImageData(fileData, filename, false);
            try
            {
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                if (this.m_Palette.Length < 256)
                    this.m_LoadedImage.Palette = BitmapHandler.GetPalette(this.m_Palette);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!", e);
            }
            this.SetExtraInfo();
            this.SetFileNames(filename);
        }

        /// <summary>
        /// Retrieves the image data and sets the file properties and palette.
        /// </summary>
        /// <param name="fileData">Original file data.</param>
        /// <param name="sourcePath">Source path the file is loaded from.</param>
        /// <param name="asAmigaFourFrame">True to abort if the file is not an Amiga-format 4-frame file.</param>
        /// <returns>The raw 8-bit linear image data in a 64000 byte array.</returns>
        protected Byte[] GetImageData(Byte[] fileData, String sourcePath, Boolean asAmigaFourFrame)
        {
            if (fileData.Length < 10)
                throw new FileTypeLoadException("File is not long enough to be a valid CPS file.");
            Int32 fileSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            Int32 compression = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            if (compression < 0 || compression > 4)
                throw new FileTypeLoadException("Unknown compression type " + compression);
            if (!((compression != 0 || compression != 4) && fileSize == fileData.Length) && !((compression == 0 || compression == 4) && fileSize + 2 == fileData.Length))
                throw new FileTypeLoadException("File size in header does not match!");
            Int32 bufferSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 4, 4, true);
            Int32 paletteLength = (Int16) ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            Boolean isPc = bufferSize == 64000;
            Boolean amigaPal = bufferSize == 40064;
            Boolean isAmiga = amigaPal || bufferSize == 40000;
            Int32 amigaPalCount = 0;
            if (!isPc && !isAmiga)
                throw new FileTypeLoadException("Unknown CPS type!");

            if (paletteLength > 0)
            {
                if (paletteLength <= 0x100 && isAmiga && paletteLength % 0x40 == 0)
                    amigaPalCount = paletteLength / 0x40;
                if (amigaPalCount == 0 && paletteLength != 0x300)
                    throw new FileTypeLoadException("Invalid palette length in header!");
                Byte[] pal = new Byte[paletteLength];
                Array.Copy(fileData, 10, pal, 0, paletteLength);
                try
                {
                    if (amigaPalCount > 0)
                    {
                        Int32 palLen = paletteLength / 2;
                        Color[] palette = new Color[palLen];
                        for (Int32 i = 0; i < palLen; i++)
                            palette[i] = Format16BitRgbaX444Be.GetColor(pal, i << 1);
                        this.m_Palette = palette;
                    }
                    else
                    {
                        ColorSixBit[] palette = ColorUtils.ReadSixBitPaletteFile(pal);
                        this.m_Palette = ColorUtils.GetEightBitColorPalette(palette);
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + ex.Message, ex);
                }
                catch (NotSupportedException ex2)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + ex2.Message, ex2);
                }
                this.HasPalette = true;
            }
            if (amigaPalCount > 0 && amigaPal)
                throw new FileTypeLoadException("Cannot handle both EOB1 and EOB2 type palettes!");
            Boolean isAmigaFourFrames = isAmiga && amigaPalCount > 1;
            if (isAmigaFourFrames && !asAmigaFourFrame)
                throw new FileTypeLoadException("This is a four-frame Amiga CPS! Load it as that specific type.");
            if (!isAmigaFourFrames && asAmigaFourFrame)
                throw new FileTypeLoadException("This is not a four-frame Amiga CPS!");

            this.CpsVersion = isPc ? CpsVersion.Pc : (amigaPal || amigaPalCount == 0 ? CpsVersion.AmigaEob1 : CpsVersion.AmigaEob2);
            this.CompressionType = compression;
            Byte[] imageData;
            Int32 dataOffset = 10 + paletteLength;
            if (compression == 0 && fileData.Length < dataOffset + bufferSize)
                throw new FileTypeLoadException("File is not long enough to contain the image data!");
            try
            {
                switch (compression)
                {
                    case 0:
                        imageData = new Byte[bufferSize];
                        Array.Copy(fileData, dataOffset, imageData, 0, bufferSize);
                        break;
                    case 1:
                        LzwCompression lzw12 = new LzwCompression(LzwSize.Size12Bit);
                        imageData = lzw12.Decompress(fileData, dataOffset, bufferSize);
                        break;
                    case 2:
                        LzwCompression lzw14 = new LzwCompression(LzwSize.Size14Bit);
                        imageData = lzw14.Decompress(fileData, dataOffset, bufferSize);
                        break;
                    case 3:
                        imageData = WestwoodRle.RleDecode(fileData, (UInt32) dataOffset, null, bufferSize, isAmiga, true);
                        break;
                    case 4:
                        imageData = new Byte[bufferSize];
                        WWCompression.LcwDecompress(fileData, ref dataOffset, imageData, 0);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported compression format \"+compression+\".");
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error decompressing image data: " + e.Message, e);
            }
            if (imageData == null)
                throw new FileTypeLoadException("Error decompressing image data.");
            // Amiga-specific logic: extract EOB1 palette, and reorder 5-bit planar data to 8-bit linear data.
            if (isAmiga)
            {
                // EOB1 embedded palette
                if (amigaPal)
                {
                    Color[] palette = new Color[32];
                    Int32 offs = 40000;
                    for (Int32 i = 0; i < 32; i++)
                    {
                        palette[i] = Format16BitRgbaX444Be.GetColor(imageData, offs);
                        offs += 2;
                    }
                    this.m_Palette = palette;
                    this.HasPalette = true;
                }
                // Convert 5-bit planar data to 8-bit linear data.
                Byte[] imageData8bit = new Byte[64000];
                Int32[] frameOffs = new Int32[5];
                Int32 planeSize = 8000;
                for (Int32 i = 0; i < 5; i++)
                    frameOffs[i] = i * planeSize;

                for (Int32 i = 0; i < imageData8bit.Length; i++)
                {
                    Int32 bytePos = i / 8;
                    Int32 bitPos = 7 - (i % 8);
                    imageData8bit[i] = (Byte) ((((imageData[frameOffs[0] + bytePos] >> bitPos) & 1) << 0) |
                                               (((imageData[frameOffs[1] + bytePos] >> bitPos) & 1) << 1) |
                                               (((imageData[frameOffs[2] + bytePos] >> bitPos) & 1) << 2) |
                                               (((imageData[frameOffs[3] + bytePos] >> bitPos) & 1) << 3) |
                                               (((imageData[frameOffs[4] + bytePos] >> bitPos) & 1) << 4));
                }
                imageData = imageData8bit;
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
            return imageData;
        }

        protected void SetExtraInfo()
        {
            Boolean isAmiga = this.CpsVersion == CpsVersion.AmigaEob1 || this.CpsVersion == CpsVersion.AmigaEob2;
            Boolean amigaPal1 = this.HasPalette && this.CpsVersion == CpsVersion.AmigaEob1;
            Boolean amigaPal2 = this.HasPalette && this.CpsVersion == CpsVersion.AmigaEob2;
            this.ExtraInfo = "Version: " + (isAmiga ? "Amiga" : "PC")
                             + "\nCompression: " + this.compressionTypes[this.CompressionType]
                             + "\nIncludes palette: " + (this.HasPalette ? "Yes" + (amigaPal1 ? " (EOB 1)" : (amigaPal2 ? " (EOB 2)" : String.Empty)) : "No");
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = fileToSave != null && !(fileToSave is FileImage) && fileToSave.ColorsInPalette != 0;
            FileImgWwCps cps = fileToSave as FileImgWwCps;
            Int32 compression = cps != null ? cps.CompressionType : 4;
            CpsVersion ver = cps != null ? cps.CpsVersion : CpsVersion.Pc;
            return new SaveOption[]
            {
                new SaveOption("VER", SaveOptionType.ChoicesList, "Version", "PC,Amiga (EOB 1),Amiga (EOB 2)", ((Int32)ver).ToString()),
                new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", (hasColors ? 1 : 0).ToString()),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Boolean asPaletted = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            Int32 version;
            if (!Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "VER"), out version))
                version = 0;
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            Bitmap image = fileToSave.GetBitmap();
            if (image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320x200 images can be saved as CPS!");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride, true);
            return SaveCps(imageData, fileToSave.GetColors(), asPaletted ? 1 : 0, compressionType, (CpsVersion) version);
        }

        public static Byte[] SaveCps(Byte[] imageData, Color[] palette, Int32 savePalettes, Int32 compressionType, CpsVersion version)
        {
            if (imageData.Length != 64000)
                throw new NotSupportedException("Only 8-bit 320x200 images can be saved as CPS!");
            Boolean isAmiga = version == CpsVersion.AmigaEob1 || version == CpsVersion.AmigaEob2;
            Boolean amigaPal = version == CpsVersion.AmigaEob1 && savePalettes == 1;
            if (isAmiga)
            {
                if (imageData.Any(p => p >= 32))
                    throw new NotSupportedException("Input for amiga images cannot use palette indices higher than 32!");
                // bitplane this stuff!
                Int32 bufSize = 40000;
                if (amigaPal)
                    bufSize += 64;
                Byte[] imageDataPlanes = new Byte[bufSize];
                Int32[] frameOffs = new Int32[5];
                Int32 planeSize = 8000;
                for (Int32 i = 0; i < 5; i++)
                    frameOffs[i] = i * planeSize;
                for (Int32 i = 0; i < imageData.Length; i++)
                {
                    Int32 bytePos = i / 8;
                    Int32 bitPos = 7 - (i % 8);
                    Byte curByte = imageData[i];
                    Int32 offs0 = frameOffs[0] + bytePos;
                    imageDataPlanes[offs0] = (Byte)(imageDataPlanes[offs0] | (((curByte >> 0) & 1) << bitPos));
                    Int32 offs1 = frameOffs[1] + bytePos;
                    imageDataPlanes[offs1] = (Byte)(imageDataPlanes[offs1] | (((curByte >> 1) & 1) << bitPos));
                    Int32 offs2 = frameOffs[2] + bytePos;
                    imageDataPlanes[offs2] = (Byte)(imageDataPlanes[offs2] | (((curByte >> 2) & 1) << bitPos));
                    Int32 offs3 = frameOffs[3] + bytePos;
                    imageDataPlanes[offs3] = (Byte)(imageDataPlanes[offs3] | (((curByte >> 3) & 1) << bitPos));
                    Int32 offs4 = frameOffs[4] + bytePos;
                    imageDataPlanes[offs4] = (Byte)(imageDataPlanes[offs4] | (((curByte >> 4) & 1) << bitPos));
                }
                if (amigaPal)
                {
                    Int32 palOffset = 40000;
                    for (Int32 i = 0; i < 32; i++)
                    {
                        UInt16 col = (UInt16)Format16BitRgbaX444Be.GetValueFromColor(palette[i]);
                        ArrayUtils.WriteIntToByteArray(imageDataPlanes, palOffset, 2, false, col);
                        palOffset += 2;
                    }
                }
                imageData = imageDataPlanes;
            }

            Byte[] compressedData;
            switch (compressionType)
            {
                case 0:
                    compressedData = imageData;
                    break;
                case 1:
                    LzwCompression lzw12 = new LzwCompression(LzwSize.Size12Bit);
                    compressedData = lzw12.Compress(imageData);
                    break;
                case 2:
                    LzwCompression lzw14 = new LzwCompression(LzwSize.Size14Bit);
                    compressedData = lzw14.Compress(imageData);
                    break;
                case 3:
                    compressedData = WestwoodRle.RleEncode(imageData);
                    break;
                case 4:
                    compressedData = WWCompression.LcwCompress(imageData);
                    break;
                default:
                    throw new NotSupportedException("Unknown compression type given.");
            }
            Int32 dataLength = 10 + compressedData.Length;
            Int32 paletteLength;
            if (savePalettes > 0 && version != CpsVersion.AmigaEob1)
                paletteLength = isAmiga ? savePalettes * 64 : 0x300;
            else
                paletteLength = 0;
            dataLength += paletteLength;
            Byte[] fullData = new Byte[dataLength];
            ArrayUtils.WriteIntToByteArray(fullData, 0, 2, true, (UInt32) (dataLength - (compressionType == 0 || compressionType == 4 ? 2 : 0)));
            ArrayUtils.WriteIntToByteArray(fullData, 2, 2, true, (UInt32) compressionType);
            ArrayUtils.WriteIntToByteArray(fullData, 4, 4, true, (UInt32) imageData.Length);
            ArrayUtils.WriteIntToByteArray(fullData, 8, 2, true, (UInt32) paletteLength);
            Int32 offset = 10;
            if (paletteLength > 0)
            {
                Byte[] palData;
                if (isAmiga)
                {
                    Int32 palLen = savePalettes * 32;
                    palData = new Byte[palLen * 2];
                    for (Int32 i = 0; i < palLen; i++)
                    {
                        UInt16 col = (UInt16)Format16BitRgbaX444Be.GetValueFromColor(palette[i]);
                        ArrayUtils.WriteIntToByteArray(palData, i * 2, 2, false, col);
                    }
                }
                else
                {
                    if (palette.Length != 256)
                    {
                        Color[] pal = Enumerable.Repeat(Color.Black, 256).ToArray();
                        Array.Copy(palette, 0, pal, 0, Math.Min(palette.Length, 256));
                        palette = pal;
                    }
                    ColorSixBit[] sixbitPal = ColorUtils.GetSixBitColorPalette(palette);
                    palData = ColorUtils.GetSixBitPaletteData(sixbitPal);
                }
                Array.Copy(palData, 0, fullData, offset, palData.Length);
                offset += palData.Length;
            }
            Array.Copy(compressedData, 0, fullData, offset, compressedData.Length);
            return fullData;
        }
    }


    public enum CpsVersion
    {
        Pc = 0,
        AmigaEob1 = 1,
        AmigaEob2 = 2
    }
}