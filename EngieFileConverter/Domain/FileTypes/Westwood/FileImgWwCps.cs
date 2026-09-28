using Nyerguds.FileData.Compression;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgWwCps : SupportedFileType
    {
        /// <summary>Pixel formatter for Amiga palettes.</summary>
        public static readonly PixelFormatter Format16BitRgbX444Be = new PixelFormatter(2, 0x0000, 0x0F00, 0x00F0, 0x000F, false);
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override int Width { get { return this.m_Width; } }
        public override int Height { get { return this.m_Height; } }
        protected int m_Width = 320;
        protected int m_Height = 200;
        public int CompressionType { get; protected set; }
        public CpsVersion CpsVersion { get; protected set; }
        protected string[] compressionTypes = new string[] { "No compression", "LZW-12", "LZW-14", "RLE", "LCW" };

        public override string IdCode { get { return "WwCps"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood CPS"; } }
        public override string[] FileExtensions { get { return new string[] {"cps", "cmp"}; } }
        public override string LongTypeName { get { return "Westwood CPS File"; } }
        public override bool NeedsPalette { get { return m_LoadedPalette == null; } }
        public override int BitsPerPixel { get { return m_ColorFormat; } }
        protected int m_ColorFormat = 8;
        protected string m_LoadedPalette = null;

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFile(fileData, filename, false);
        }

        protected void LoadFile(byte[] fileData, string filename, bool asToonstruck)
        {
            int startOffset = 0;
            // 0x4E435053 == "SPCN" string.
            if (asToonstruck)
            {
                if (fileData.Length > 4 && ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0) == 0x4E435053)
                    startOffset = 4;
                else
                    throw new FileTypeLoadException("Not a Toonstruck CPS.");
            }
            byte[] imageData = GetImageData(fileData, startOffset, fileData.Length - startOffset, filename, false, asToonstruck,
                out int compression, out Color[] palette, out CpsVersion cpsVersion, out int colorDepth, out int width, out int height);
            m_ColorFormat = colorDepth;
            if (asToonstruck && cpsVersion != CpsVersion.Toonstruck)
                throw new FileTypeLoadException("Bad format for Toonstruck CPS.");
            this.CompressionType = compression;
            this.CpsVersion = cpsVersion;
            string externalPalette = null;
            this.SetFileNames(filename);
            SupportedFileType pal = null;
            if (palette == null && filename != null)
            {
                string palName = Path.GetFileNameWithoutExtension(filename) + ".pal";
                string palettePath = Path.Combine(Path.GetDirectoryName(filename), palName);
                FileInfo palInfo = new FileInfo(palettePath);
                if (cpsVersion == CpsVersion.Pc && palInfo.Exists && palInfo.Length == 0x300)
                {
                    pal = CheckForPalette<FilePalette6Bit>(filename);
                    if (pal != null)
                    {
                        palette = pal.GetColors();
                        externalPalette = pal.LoadedFile;
                    }
                }
                else if ((cpsVersion == CpsVersion.AmigaEob1 || cpsVersion == CpsVersion.AmigaEob2) && palInfo.Exists && palInfo.Length == (2 << colorDepth))
                {
                    pal = CheckForPalette<FilePaletteWwAmiga>(filename);
                    if (pal != null)
                    {
                        palette = pal.GetColors();
                        externalPalette = pal.LoadedFile;
                    }
                }
            }
            this.m_LoadedPalette = pal != null ? pal.LoadedFile : (palette != null ? (filename ?? String.Empty): null);
            if (palette != null)
            {
                int palLen = palette.Length;
                if (palLen < 256 && imageData.Any(b => b >= palLen))
                    throw new FileTypeLoadException("Palette is too small for image data.");
                this.m_Palette = palette;
            }
            else
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
            try
            {
                this.m_Width = width;
                this.m_Height = height;
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                if (this.m_Palette.Length < 256)
                    this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FileTypeLoadException("Cannot construct image from read data.", e);
            }
            this.SetExtraInfo(Path.GetFileName(externalPalette));
        }

        /// <summary>
        /// Retrieves the image data and sets the file properties and palette.
        /// </summary>
        /// <param name="fileData">Original file data.</param>
        /// <param name="start">Start offset of the data.</param>
        /// <param name="sourcePath">Source path the file is loaded from.</param>
        /// <param name="asAmigaFourFrame">True to abort if the file is not an Amiga-format 4-frame file.</param>
        /// <param name="asToonstruck">Read as ToonStruck CPS file.</param>
        /// <param name="compression">Output arg for returning the compression</param>
        /// <param name="palette">Output arg for returning the palette.</param>
        /// <param name="cpsVersion">Output arg for returning the CPS version.</param>
        /// <returns>The raw 8-bit linear image data in a 64000 byte array.</returns>
        public static byte[] GetImageData(byte[] fileData, int start, int dataLen, string sourcePath, bool asAmigaFourFrame, bool asToonstruck,
            out int compression, out Color[] palette, out CpsVersion cpsVersion, out int colorDepth, out int width, out int height)
        {
            if (dataLen < (asToonstruck ? 12 : 10))
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            int fileSize = (int)ArrayUtils.ReadIntFromByteArray(fileData, start + 0, asToonstruck ? 4 : 2, true);
            // compensate for 4-byte file size.
            if (asToonstruck)
                start += 2;
            compression = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, start + 2);
            if (compression > 4)
                throw new FileTypeLoadException(String.Format(ERR_UNKN_COMPR_X, compression));
            // compressions other than 0 and 4 count the full file including size header.
            if (fileSize != dataLen && !asToonstruck && (compression == 0 || compression == 4))
                fileSize += 2;
            if (fileSize != dataLen)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);
            int bufferSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, start + 4);
            int paletteLength = ArrayUtils.ReadInt16FromByteArrayLe(fileData, start + 8);
            bool isPc = bufferSize == 64000;
            bool isToon = bufferSize == 256000;
            int[] planeOffsets = new int[6];
            colorDepth = 8;
            int amigaPlanes = 0;
            int amigaPalLength = 0;
            if (!isPc && !isToon)
            {
                planeOffsets = ImageUtils.GetPlaneBlockOffsets(0, 40, 200, 5);
                for (int i = 1; i < 6; ++i)
                {
                    int curPlaneOffset = planeOffsets[i];
                    if (bufferSize < curPlaneOffset)
                        break;
                    int pallen = 4 << (i - 1);
                    bool isPlanePal = bufferSize == curPlaneOffset + pallen;
                    if (bufferSize == curPlaneOffset || isPlanePal)
                    {
                        amigaPlanes = i;
                        amigaPalLength = isPlanePal ? pallen : 0;
                        colorDepth = i;
                        break;
                    }
                }
            }
            bool isAmiga = amigaPlanes > 0;
            int amigaPalCount = 0;
            if (!isPc && !isAmiga && !isToon)
                throw new FileTypeLoadException("Unknown CPS type.");
            if (paletteLength > 0)
            {
                if (paletteLength <= 0x100 && isAmiga && paletteLength % 0x40 == 0)
                    amigaPalCount = paletteLength / 0x40;
                if (amigaPalCount == 0 && paletteLength > 0x300)
                    throw new FileTypeLoadException(ERR_BAD_HEADER_PAL_SIZE);
                int palStart = start + 10;
                try
                {
                    if (amigaPalCount > 0)
                    {
                        if (paletteLength % 2 != 0)
                            throw new FileTypeLoadException("Bad length for Amiga CPS palette.");
                        int palLen = paletteLength / 2;
                        palette = Format16BitRgbX444Be.GetColorRange(fileData, palStart, palLen);
                    }
                    else
                    {
                        if (paletteLength % 3 != 0)
                            throw new FileTypeLoadException("Bad length for 6-bit CPS palette.");
                        int colors = paletteLength / 3;
                        palette = ColorUtils.ReadSixBitPalette(fileData, palStart, colors);
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
                }
            }
            else
                palette = null;
            if (amigaPalCount > 0 && amigaPalLength > 0)
                throw new FileTypeLoadException("Cannot handle both EOB1 and EOB2 type palettes.");
            bool isAmigaFourFrames = isAmiga && amigaPalCount > 1;
            if (isAmigaFourFrames && !asAmigaFourFrame)
                throw new FileTypeLoadException("This is a four-frame Amiga CPS! Load it as that specific type.");
            if (!isAmigaFourFrames && asAmigaFourFrame)
                throw new FileTypeLoadException("This is not a four-frame Amiga CPS.");

            if (isToon)
            {
                cpsVersion = CpsVersion.Toonstruck;
                width = 640;
                height = 400;
            }
            else
            {
                width = 320;
                height = 200;
                if (isPc)
                    cpsVersion = CpsVersion.Pc;
                else if (amigaPalLength > 0 || amigaPalCount == 0)
                    cpsVersion = CpsVersion.AmigaEob1;
                else
                    cpsVersion = CpsVersion.AmigaEob2;
            }
            byte[] imageData = DecompressData(fileData, start, dataLen, paletteLength, bufferSize, compression, isAmiga);

            if (imageData == null)
                throw new FileTypeLoadException(ERR_DECOMPR);
            // Amiga-specific logic: extract EOB1 palette, and reorder 5-bit planar data to 8-bit linear data.
            if (isAmiga)
            {
                int endOffs = planeOffsets[amigaPlanes];
                // EOB1 embedded palette
                if (amigaPalLength > 0)
                {
                    palette = Format16BitRgbX444Be.GetColorRange(imageData, endOffs, amigaPalLength / 2);
                }
                try
                {
                    imageData = ImageUtils.PlanarBlocksToLinear(imageData, 0, endOffs, 320, 200, 40, amigaPlanes);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                }
            }
            return imageData;
        }

        protected static byte[] DecompressData(byte[] fileData, int start, int length, int skip, int bufferSize, int compression, bool swapWordsRle)
        {
            byte[] imageData = null;
            int dataOffset = start + 10 + skip;
            if (compression == 0 && length < dataOffset + bufferSize)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            try
            {
                switch (compression)
                {
                    case 0:
                        imageData = new byte[bufferSize];
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
                        int len = WestwoodRle.RleDecode(fileData, (uint)dataOffset, null, ref imageData, swapWordsRle, true);
                        if (len != bufferSize)
                            throw new FileTypeLoadException(ERR_DECOMPR_LEN);
                        break;
                    case 4:
                        imageData = new byte[bufferSize];
                        WWCompression.LcwDecompress(fileData, ref dataOffset, imageData, 0);
                        break;
                    default:
                        throw new FileTypeLoadException(String.Format(ERR_UNKN_COMPR_X, compression));
                }
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, GeneralUtils.RecoverArgExceptionMessage(ex, true)), ex);
            }
            catch (Exception e)
            {
                if (e is FileTypeLoadException)
                    throw;
                throw new FileTypeLoadException(String.Format(ERR_DECOMPR_ERR, e.Message), e);
            }
            return imageData;
        }

        protected void SetExtraInfo(string externalPalette)
        {
            bool amigaV1 = this.CpsVersion == CpsVersion.AmigaEob1;
            bool amigaV2 = this.CpsVersion == CpsVersion.AmigaEob2;
            bool isAmiga = amigaV1 || amigaV2;
            bool isToon = this.CpsVersion == CpsVersion.Toonstruck;
            string compression = this.CompressionType == 5 ? "LZSS" : this.compressionTypes[this.CompressionType];
            this.ExtraInfo = "Version: " + (isAmiga ? "Amiga" : isToon ? "Toonstruck" : "PC")
                             + "\nCompression: " + compression
                             + (externalPalette != null ? ("\nPalette loaded from " + externalPalette) : 
                                ("\nIncludes palette: " + (!this.NeedsPalette ? "Yes" + (amigaV1 ? " (EOB 1)" : (amigaV2 ? " (EOB 2)" : String.Empty)) : "No")));
        }

        private string CheckFileToSave(SupportedFileType fileToSave, out Bitmap image)
        {
            image = null;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                return ERR_EMPTY_FILE;
            if (image == null || image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                return ErrFixedBppsAndSize(320, 200, ShortTypeName, 8);
            return null;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            string fileErr = CheckFileToSave(fileToSave, out Bitmap image);
            if (fileErr != null)
                throw new FileTypeSaveException(fileErr);

            FileImgWwCps cps = fileToSave as FileImgWwCps;
            int compression = cps != null ? cps.CompressionType : 4;
            CpsVersion ver = cps != null ? cps.CpsVersion : CpsVersion.Pc;
            return new Option[]
            {
                new Option("VER", OptionInputType.ChoicesList, "Version", "PC,Amiga (EOB 1),Amiga (EOB 2)", ((int)ver).ToString()),
                new Option("PAL", OptionInputType.Boolean, "Include palette", (fileToSave.NeedsPalette ? 0 : 1).ToString()),
                new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            string fileErr = CheckFileToSave(fileToSave, out Bitmap image);
            if (fileErr != null)
                throw new FileTypeSaveException(fileErr);

            bool asPaletted = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PAL"));
            int version;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "VER"), out version))
                version = 0;
            int compressionType;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            byte[] imageData = ImageUtils.GetImageData(image, true);
            return SaveCps(imageData, fileToSave.GetColors(), asPaletted ? 1 : 0, compressionType, (CpsVersion) version);
        }

        public static byte[] SaveCps(byte[] imageData, Color[] palette, int savePalettes, int compressionType, CpsVersion version)
        {
            bool isAmiga = version == CpsVersion.AmigaEob1 || version == CpsVersion.AmigaEob2;
            bool amigaPal = version == CpsVersion.AmigaEob1 && savePalettes == 1;
            if (isAmiga)
            {
                if (imageData.Any(p => p >= 32))
                    throw new FileTypeSaveException("Input for amiga images cannot use palette indices higher than 32.");
                // bitplane this stuff!
                int bufSize = 40000;
                if (amigaPal)
                    bufSize += 64;
                byte[] imageDataPlanes = new byte[bufSize];
                int[] frameOffs = new int[5];
                int planeSize = 8000;
                for (int i = 0; i < 5; ++i)
                    frameOffs[i] = i * planeSize;
                for (int i = 0; i < imageData.Length; ++i)
                {
                    int bytePos = i >> 3; // Bitwise optimisation of 'i / 8'
                    int bitPos = 7 - (i & 7); // Bitwise optimisation of '7 - (i % 8)'
                    byte curByte = imageData[i];
                    int offs0 = frameOffs[0] + bytePos;
                    imageDataPlanes[offs0] = (byte)(imageDataPlanes[offs0] | (((curByte >> 0) & 1) << bitPos));
                    int offs1 = frameOffs[1] + bytePos;
                    imageDataPlanes[offs1] = (byte)(imageDataPlanes[offs1] | (((curByte >> 1) & 1) << bitPos));
                    int offs2 = frameOffs[2] + bytePos;
                    imageDataPlanes[offs2] = (byte)(imageDataPlanes[offs2] | (((curByte >> 2) & 1) << bitPos));
                    int offs3 = frameOffs[3] + bytePos;
                    imageDataPlanes[offs3] = (byte)(imageDataPlanes[offs3] | (((curByte >> 3) & 1) << bitPos));
                    int offs4 = frameOffs[4] + bytePos;
                    imageDataPlanes[offs4] = (byte)(imageDataPlanes[offs4] | (((curByte >> 4) & 1) << bitPos));
                }
                if (amigaPal)
                {
                    int palOffset = 40000;
                    for (int i = 0; i < 32; ++i)
                    {
                        ushort col = (ushort)Format16BitRgbX444Be.GetValueFromColor(palette[i]);
                        ArrayUtils.WriteUInt16ToByteArrayBe(imageDataPlanes, palOffset, col);
                        palOffset += 2;
                    }
                }
                imageData = imageDataPlanes;
            }
            byte[] compressedData;
            try
            {
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
                        compressedData = WestwoodRle.RleEncode(imageData, isAmiga);
                        break;
                    case 4:
                        compressedData = WWCompression.LcwCompress(imageData);
                        break;
                    default:
                        throw new FileTypeSaveException(ERR_UNKN_COMPR_X, compressionType);
                }
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
            }
            int dataLength = 10 + compressedData.Length;
            int paletteLength;
            if (savePalettes > 0 && version != CpsVersion.AmigaEob1)
                paletteLength = isAmiga ? savePalettes * 64 : 0x300;
            else
                paletteLength = 0;
            dataLength += paletteLength;
            bool asToonstruck = version == CpsVersion.Toonstruck;
            if (asToonstruck)
                dataLength += 6;
            byte[] fullData = new byte[dataLength];
            int startOffset = 0;
            if (version == CpsVersion.Toonstruck)
            {
                // "SPCN" string.
                ArrayUtils.WriteInt32ToByteArrayLe(fullData, startOffset + 0, 0x4E435053);
                // 4-byte data length
                ArrayUtils.WriteInt32ToByteArrayLe(fullData, startOffset + 4, (dataLength - (compressionType == 0 || compressionType == 4 ? 2 : 0)));
                startOffset += 6;
            }
            else
            {
                ArrayUtils.WriteUInt16ToByteArrayLe(fullData, startOffset + 0, (ushort)(dataLength - (compressionType == 0 || compressionType == 4 ? 2 : 0)));
            }
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, startOffset + 2, (ushort)compressionType);
            ArrayUtils.WriteUInt32ToByteArrayLe(fullData, startOffset + 4, (uint)imageData.Length);
            ArrayUtils.WriteUInt16ToByteArrayLe(fullData, startOffset + 8, (ushort)paletteLength);
            int offset = 10;
            if (paletteLength > 0)
            {
                byte[] palData;
                if (isAmiga)
                {
                    int palLen = savePalettes * 32;
                    palData = new byte[palLen * 2];
                    for (int i = 0; i < palLen; ++i)
                    {
                        ushort col = (ushort)Format16BitRgbX444Be.GetValueFromColor(palette[i]);
                        ArrayUtils.WriteUInt16ToByteArrayBe(palData, i * 2, col);
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
                    palData = ColorUtils.GetSixBitPaletteData(palette);
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
        AmigaEob2 = 2,
        Toonstruck = 3
    }
}