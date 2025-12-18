using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.Linq;
using System.Text;
using Nyerguds.Util.UI.SaveOptions;
using Nyerguds.FileData.NullSoft;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImagePcx: SupportedFileType
    {
        public override String IdCode { get { return "PCX"; } }
        public override String ShortTypeName { get { return "PCX"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String LongTypeName { get { return "ZSoft Picture Exchange Format"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "pcx" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] {this.LongTypeName }; } }
        public override Int32 BitsPerPixel { get { return this.m_BitsPerPixel; } }

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public override Boolean CanSave { get { return false; } }

        public override FileClass FileClass
        {
            get
            {
                if (this.m_LoadedImage == null)
                    return FileClass.None;
                switch (this.m_LoadedImage.PixelFormat)
                {
                    case PixelFormat.Format1bppIndexed:
                        return FileClass.Image1Bit;
                    case PixelFormat.Format4bppIndexed:
                        return FileClass.Image4Bit;
                    case PixelFormat.Format8bppIndexed:
                        return FileClass.Image8Bit;
                    default:
                        return FileClass.ImageHiCol;
                }
            }
        }

        public override FileClass InputFileClass { get { return FileClass.Image; } }

        protected Int32 m_BitsPerPixel;


        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, "null");
        }

        public void LoadFromFileData(Byte[] fileData, String filename)
        {
            if (fileData.Length < 128)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            if (fileData[0] != 10) // ID byte
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            Byte version = fileData[1];
            Byte encoding = fileData[2];
            if (encoding > 1)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            Boolean reservedByteFree = fileData[64] == 0; // reserved byte
            Boolean reservedSpaceFree = true;
            for (Int32 i = 74; i < 128; ++i)
            {
                // End of header reserved space
                if (fileData[i] != 0)
                {
                    reservedSpaceFree = false;
                    break;
                }
            }
            Boolean usesRLE = encoding == 1;
            Int32 bitsPerPlane = fileData[3]; // Number of bits to represent a pixel (per Plane) - 1, 2, 4, or 8
            UInt16 windowXmin = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            UInt16 windowYmin = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
            UInt16 windowXmax = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 8);
            UInt16 windowYmax = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 10);
            if (windowXmax < windowXmin || windowYmax < windowYmin)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            //UInt16 hDpi = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 12); // Horizontal Resolution of image in DPI
            //UInt16 vDpi = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 14); // Vertical Resolution of image in DPI
            Byte numPlanes = fileData[65]; // Number of color planes
            UInt16 bytesPerLine = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 66); // Number of bytes to allocate for a scanline plane.  MUST be an EVEN number.  Do NOT calculate from Xmax-Xmin.
            UInt16 paletteInfo = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 68); // How to interpret palette: 1 = Color/BW, 2 = Grayscale (ignored in PB IV/ IV Plus)
            //UInt16 hscreenSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 70); // Horizontal screen size in pixels. New field found only in PB IV/IV Plus
            //UInt16 vscreenSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 72); // Vertical screen size in pixels. New field found only in PB IV/IV Plus

            Int32 width = windowXmax - windowXmin + 1;
            Int32 height = windowYmax - windowYmin + 1;
            UInt32 fileEnd = (UInt32)fileData.Length;
            Int32 stride = numPlanes * bytesPerLine;
            UInt32 endOfData;
            Boolean zeroRepeatsFound = false;
            Byte[] imageData;

            //Boolean exceedsLines = false;
            if (usesRLE)
                imageData = PcxCompression.RleDecode(fileData, 128, null, bytesPerLine, numPlanes, height, out endOfData, out zeroRepeatsFound);
            else
            {
                Int32 fullSize = stride * height;
                imageData = new Byte[fullSize];
                Array.Copy(fileData, 128, imageData, 0, fullSize);
                endOfData = (UInt32)(128 + fullSize);
            }
            //System.IO.File.WriteAllBytes(filename + ".raw", imageData);
            this.m_BitsPerPixel = numPlanes * bitsPerPlane;
            PixelFormat pf = PixelFormat.Undefined;
            StringBuilder extraInfo = new StringBuilder("Version: ").Append(version).Append(": ");
            switch (version)
            {
                case 0:
                    extraInfo.Append("v2.5");
                    break;
                case 2:
                    extraInfo.Append("v2.8, with pal info");
                    if (this.m_BitsPerPixel == 1)
                        extraInfo.Append(" (unused for 1bpp)");
                    break;
                case 3:
                    extraInfo.Append("v2.8, no pal info");
                    break;
                case 4:
                    extraInfo.Append("PB4Win");
                    break;
                case 5:
                    extraInfo.Append("v3.0+");
                    break;
                default:
                    extraInfo.Append("Unknown version");
                    break;
            }
            if (zeroRepeatsFound)
                extraInfo.Append("\nNon-standard repeat commands of length 0 found!");
            if (!usesRLE)
                extraInfo.Append("\nNo RLE compression");
            //else if (exceedsLines)
            //    extraInfo.Append("\nRLE exceeds lines!");
            if (!reservedByteFree)
                extraInfo.Append("\nReserved header byte has value ").Append(fileData[64].ToString("X2")).Append(" instead of 00");
            if (!reservedSpaceFree)
                extraInfo.Append("\nJunk found in reserved header space!");
            if (windowXmin != 0 || windowYmin != 0)
                extraInfo.Append("\nImage is shifted down to (").Append(windowXmin).Append(",").Append(windowYmin).Append(")");

            Int32 nrOfcolors = 1 << this.m_BitsPerPixel;
            Int32 m_ColorsInPalette = nrOfcolors > 0x100 ? 0 : nrOfcolors;
            extraInfo.Append("\n").Append(numPlanes).Append("-plane, ").Append(bitsPerPlane).Append(" bpp, ").Append(nrOfcolors).Append(" color image.");
            Int32 palOffset = 16;
            Int32 palsize = nrOfcolors * 3;
            if (version >= 5 && nrOfcolors <= 0x100)
            {
                // detect palette: first check behind data, then check end of file, and if those two are the same but the byte before it doesn't match 0C, accept it anyway.
                Int32 behindData = (endOfData + 1 + palsize <= fileEnd) ? (Int32)endOfData : -1;
                Int32 fromEnd = (fileEnd - palsize - 1 > 0) ? (Int32)(fileEnd - palsize - 1) : -1;

                if (behindData != -1 && fileData[behindData] == 0x0C)
                {
                    palOffset = (Int32)endOfData + 1;
                    if (behindData == fromEnd)
                        extraInfo.Append("\nPalette found behind data, at end of file.");
                    else
                        extraInfo.Append("\nPalette found behind data.");
                }
                else if (fromEnd != -1 && fileData[fromEnd] == 0x0C)
                {
                    palOffset = (Int32)fileEnd - palsize;
                    extraInfo.Append("\nPalette found behind data.");
                }
                else if (this.m_BitsPerPixel == 8 && endOfData + 1 == fileEnd - palsize)
                {
                    palOffset = (Int32)endOfData + 1;
                    extraInfo.Append("\nNonstandard palette indicator \"").Append(fileData[endOfData].ToString("X2")).Append("\"");
                }
            }
            else if (paletteInfo != 2 && m_ColorsInPalette > 16)
                throw new FileTypeLoadException("No palette found for indexed image with more than 16 colors!");
            Boolean usesHeaderPal = palOffset == 16;
            if (usesHeaderPal && m_ColorsInPalette > 16)
                m_ColorsInPalette = 16;
            if (numPlanes == 1)
            {
                switch (bitsPerPlane)
                {
                    case 1:
                        pf = PixelFormat.Format1bppIndexed;
                        if (width == 640 && height == 200)
                        {
                            this.m_Palette = this.LoadPaletteCga(fileData, palOffset, paletteInfo, bitsPerPlane);
                            extraInfo.Append("\nHandled as CGA");
                        }
                        //else if (version == 1 || version == 3 || (version == 4 && paletteInfo != 1))
                        else if (version <= 3 || (version == 4 && paletteInfo != 1))
                            this.m_Palette = new[] {Color.Black, Color.White};
                        else
                            this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, palOffset, m_ColorsInPalette);
                        break;
                    case 2:
                        pf = PixelFormat.Format4bppIndexed;
                        if (version < 4 || (width == 320 && height == 200))
                        {
                            this.m_Palette = this.LoadPaletteCga(fileData, palOffset, paletteInfo, bitsPerPlane);
                            extraInfo.Append("\nHandled as CGA");
                            if (paletteInfo != 0)
                                extraInfo.Append(" (v4 palette handling)");
                        }
                        else
                            this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, palOffset, m_ColorsInPalette);
                        Byte[] tmpImageData = ImageUtils.ConvertTo8Bit(imageData, width, height, 0, 2, true, ref stride);
                        imageData = ImageUtils.ConvertFrom8Bit(tmpImageData, width, height, 4, true, ref stride);
                        break;
                    case 4:
                        pf = PixelFormat.Format4bppIndexed;
                        this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, palOffset, m_ColorsInPalette);
                        break;
                    case 8:
                        pf = PixelFormat.Format8bppIndexed;
                        if (paletteInfo == 2)
                            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
                        else
                            this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, palOffset, m_ColorsInPalette);
                        break;
                    case 24:
                        pf = PixelFormat.Format24bppRgb;
                        break;
                    case 32:
                        pf = PixelFormat.Format32bppArgb;
                        break;
                }
            }
            else if (((bitsPerPlane == 1 || bitsPerPlane == 2) && (numPlanes == 2 || numPlanes == 3 || numPlanes == 4)) || (bitsPerPlane == 1 && numPlanes == 8))
            {
                // Supports 8-bit planar. You never know...
                pf = m_ColorsInPalette > 16 ? PixelFormat.Format8bppIndexed : PixelFormat.Format4bppIndexed;
                if (m_ColorsInPalette > 16)
                    this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, palOffset, m_ColorsInPalette);
                else if (bitsPerPlane == 1 && numPlanes == 2 && (version < 4 || (width == 320 && height == 200)))
                {
                    this.m_Palette = this.LoadPaletteCga(fileData, palOffset, paletteInfo, bitsPerPlane);
                    extraInfo.Append("\nHandled as CGA");
                    if (paletteInfo != 0)
                        extraInfo.Append(" (v4 palette handling)");
                }
                else if ((version == 0 || version == 3) && (numPlanes < 8))
                    this.m_Palette = PaletteUtils.GetEgaPalette(bitsPerPlane * numPlanes);
                else
                    this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, palOffset, m_ColorsInPalette);
                imageData = ImageUtils.PlanarLinesToLinear(imageData, 0, width, height, numPlanes, bytesPerLine, bitsPerPlane, Image.GetPixelFormatSize(pf), out stride);
            }
            else if (bitsPerPlane == 8 && (numPlanes == 3 || numPlanes == 4))
            {
                pf = numPlanes == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
                imageData = ImageUtils.GetPlanarRgb(imageData, 0, width, height, numPlanes == 4, bytesPerLine, out stride);
            }
            if (pf == PixelFormat.Undefined)
                throw new FileTypeLoadException("Unsupported for now.");
            if (this.m_BitsPerPixel == 8 && usesHeaderPal)
            {
                extraInfo.Append("\nNo palette found for indexed image with more than 16 colors! Reverting to 16-color palette.");
                Color[] palette = new Color[256];
                for (Int32 i = 0; i < 0x100; i+=0x10)
                    Array.Copy(m_Palette, 0, palette, i, 0x10);
                m_Palette = palette;
            }
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, stride, pf, this.m_Palette, Color.Black);
            //if (hDpi != 0 && vDpi != 0 && !(hDpi == width && vDpi == height))
            //    m_LoadedImage.SetResolution(hDpi, vDpi);
            if (nrOfcolors < 0x100 && nrOfcolors != (1 << Image.GetPixelFormatSize(pf)))
                this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette, m_ColorsInPalette);
            this.ExtraInfo = extraInfo.ToString();
        }

        private Color[] LoadPaletteCga(Byte[] fileData, Int32 index, UInt16 paletteInfo, Int32 bitsPerPixel)
        {
            /* Get the explicitly defined color */
            Byte cgaDefinedColor = (Byte)(fileData[index] >> 4);   // 0 to 15
            /* Get the CGA foreground palette */
            if (bitsPerPixel == 1)
                return PaletteUtils.GetCgaPalette(cgaDefinedColor, false, false, false, bitsPerPixel);

            Boolean cgaPaletteValue;
            Boolean cgaIntensityValue;
            Boolean cgaColorBurstEnable;
            if (paletteInfo != 0) // PB 4.0
            {
                // Evaluate values of RGB color slot #1 (skip background color info)
                Byte greenValue1 = fileData[index + 4];
                Byte blueValue1 = fileData[index + 5];
                // Pick green palette (0) if G > B
                cgaPaletteValue = greenValue1 <= blueValue1;
                // Pick bright palette if max(G,B) > 200
                cgaIntensityValue = Math.Max(greenValue1, blueValue1) > 200;
                // Check for palette 2 by testing if the first check returned palette 1, and in the third color entry, red is smaller or equal to blue.
                // This should default to true in a nulled palette, and be equal on palette 1 because it is AA00AA.
                // Not sure if this is really filled in, but it would make sense.
                Byte redValue2 = fileData[index + 6];
                Byte blueValue2 = fileData[index + 8];
                cgaColorBurstEnable = !cgaPaletteValue || redValue2 <= blueValue2;
            }
            else
            {
                // Technically 0 on "Burst" means Grayscale. Seems in practice it means "mode 5" and allows acces to the extra palette 2
                Byte val = fileData[index + 3];
                cgaColorBurstEnable = ((val & 0x80) >> 7) == 1;
                cgaPaletteValue = ((val & 0x40) >> 6) == 1;
                cgaIntensityValue = ((val & 0x20) >> 5) == 1;
            }
            return PaletteUtils.GetCgaPalette(cgaDefinedColor, cgaColorBurstEnable, cgaPaletteValue, cgaIntensityValue, bitsPerPixel);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            throw new NotImplementedException();
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            Color[] palEntries = fileToSave.GetColors();
            Int32 colors = palEntries.Length;
            if (colors == 0)
                return new Option[] { new Option("PLN", OptionInputType.Boolean, "Save data as linear, not planar.", "1") };

            Option version = new Option("VER", OptionInputType.ChoicesList, "PCX version:", "0: Paintbrush v2.5 (pure EGA colors only),2: Paintbrush v2.8 (with palette),3: Paintbrush v2.8 (no palette),4: Paintbrush for Windows,5: Paintbrush v3.0+", "4");

            if (colors == 2 || colors == 4)
            {
                Byte backgroundColor;
                Boolean colorBurst;
                Boolean palette;
                Boolean intensity;
                if (PaletteUtils.DetectCgaPalette(palEntries, out backgroundColor, out colorBurst, out palette, out intensity))
                {
                    return new Option[]
                    {
                        new Option("CGA", OptionInputType.Boolean, "Save as CGA palette.", "1"),
                        new Option("NCG", OptionInputType.Boolean, "Save as new type CGA.", "0")
                    };
                }
            }

            return base.GetSaveOptions(fileToSave, targetFileName);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // TODO
            throw new NotImplementedException();
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");

        }

    }
}