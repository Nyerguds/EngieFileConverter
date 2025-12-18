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
        public override String ShortTypeDescription { get { return "ZSoft Picture Exchange Format"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "pcx" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] {this.ShortTypeDescription }; } }
        public override Int32 ColorsInPalette { get { return this.m_ColorsInPalette; } }
        public override Int32 BitsPerPixel { get { return this.m_BitsPerPixel; } }

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

        protected Int32 m_ColorsInPalette;
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
                throw new FileTypeLoadException("Not a PCX file!");
            if (fileData[0] != 10) // ID byte
                throw new FileTypeLoadException("Not a PCX file!");
            Byte version = fileData[1];
            Byte encoding = fileData[2];
            if (encoding > 1)
                throw new FileTypeLoadException("Not a PCX file!");
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
            UInt16 windowXmin = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            UInt16 windowYmin = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 6, 2, true);
            UInt16 windowXmax = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            UInt16 windowYmax = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 10, 2, true);
            if (windowXmax < windowXmin || windowYmax < windowYmin)
                throw new FileTypeLoadException("Bad dimensions in PCX file!");
            //UInt16 hDpi = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 12, 2, true); // Horizontal Resolution of image in DPI
            //UInt16 vDpi = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 14, 2, true); // Vertical Resolution of image in DPI
            Byte numPlanes = fileData[65]; // Number of color planes
            UInt16 bytesPerLine = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 66, 2, true); // Number of bytes to allocate for a scanline plane.  MUST be an EVEN number.  Do NOT calculate from Xmax-Xmin. 
            UInt16 paletteInfo = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 68, 2, true); // How to interpret palette- 1 = Color/BW, 2 = Grayscale (ignored in PB IV/ IV +) 
            //UInt16 hscreenSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 70, 2, true); // Horizontal screen size in pixels. New field found only in PB IV/IV Plus 
            //UInt16 vscreenSize = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 72, 2, true); // Vertical screen size in pixels. New field found only in PB IV/IV Plus 

            Int32 width = windowXmax - windowXmin + 1;
            Int32 height = windowYmax - windowYmin + 1;
            UInt32 fileEnd = (UInt32)fileData.Length;
            Int32 stride = numPlanes * bytesPerLine;
            UInt32 endOfData;
            Byte[] imageData;

            //Boolean exceedsLines = false;
            if (usesRLE)
                imageData = PcxCompression.RleDecode(fileData, 128, null, bytesPerLine, numPlanes, height, out endOfData);
            else
            {
                Int32 fullSize = stride * height;
                imageData = new Byte[fullSize];
                Array.Copy(fileData, 128, imageData, 0, fullSize);
                endOfData = (UInt32)(128 + fullSize);
            }
            //System.IO.File.WriteAllBytes(filename + ".raw", imageData);
            m_BitsPerPixel = numPlanes * bitsPerPlane;
            PixelFormat pf = PixelFormat.Undefined;
            StringBuilder extraInfo = new StringBuilder("Version: ").Append(version).Append(": ");
            switch (version)
            {
                case 0:
                    extraInfo.Append("v2.5");
                    break;
                case 2:
                    extraInfo.Append("v2.8, with pal info");
                    if (m_BitsPerPixel == 1)
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

            Int32 nrOfcolors = 1 << m_BitsPerPixel;
            m_ColorsInPalette = nrOfcolors > 0x100 ? 0 : nrOfcolors;
            extraInfo.Append("\n").Append(numPlanes).Append("-plane, ").Append(bitsPerPlane).Append(" bpp, ").Append(nrOfcolors).Append(" color image.");
            Int32 palOffset = 16;
            Int32 palsize = nrOfcolors * 3;
            if (version >= 5 && nrOfcolors <= 0x100)
            {
                // detect palette: first check behind data, then check end of file, and if those two are the same but the byte before it doesn't match 0C, accept it anyway.
                if (endOfData + 1 + palsize <= fileEnd && fileData[endOfData] == 0x0C)
                    palOffset = (Int32)endOfData + 1;
                else if (fileData[fileEnd - palsize - 1] == 0x0C)
                    palOffset = (Int32)fileEnd - palsize;
                else if (m_BitsPerPixel == 8 && endOfData + 1 == fileEnd - palsize)
                {
                    palOffset = (Int32)endOfData + 1;
                    extraInfo.Append("\nNonstandard palette indicator \"").Append(fileData[endOfData].ToString("X2")).Append("\"");
                }
            }
            else if (paletteInfo != 2 && m_ColorsInPalette > 16)
                throw new FileTypeLoadException("No palette found for indexed image with more than 16 colors!");
            if (numPlanes == 1)
            {
                switch (bitsPerPlane)
                {
                    case 1:
                        pf = PixelFormat.Format1bppIndexed;
                        if (width == 640 && height == 200)
                        {
                            m_Palette = LoadPaletteCga(fileData, palOffset, paletteInfo, bitsPerPlane);
                            extraInfo.Append("\nHandled as CGA");
                        }
                        //else if (version == 1 || version == 3 || (version == 4 && paletteInfo != 1))
                        else if (version <= 3 || (version == 4 && paletteInfo != 1))
                            m_Palette = new[] {Color.Black, Color.White};
                        else
                            m_Palette = ColorUtils.ReadEightBitPaletteFrom(fileData, palOffset, nrOfcolors);
                        break;
                    case 2:
                        pf = PixelFormat.Format4bppIndexed;
                        if (version < 4 || (width == 320 && height == 200))
                        {
                            m_Palette = LoadPaletteCga(fileData, palOffset, paletteInfo, bitsPerPlane);
                            extraInfo.Append("\nHandled as CGA");
                            if (paletteInfo != 0)
                                extraInfo.Append(" (v4 palette handling)");
                        }
                        else
                            m_Palette = ColorUtils.ReadEightBitPaletteFrom(fileData, palOffset, m_ColorsInPalette);
                        Byte[] tmpImageData = ImageUtils.ConvertTo8Bit(imageData, width, height, 0, 2, true, ref stride);
                        imageData = ImageUtils.ConvertFrom8Bit(tmpImageData, width, height, 4, true, ref stride);
                        break;
                    case 4:
                        pf = PixelFormat.Format4bppIndexed;
                        m_Palette = ColorUtils.ReadEightBitPaletteFrom(fileData, palOffset, m_ColorsInPalette);
                        break;
                    case 8:
                        pf = PixelFormat.Format8bppIndexed;
                        if (paletteInfo == 2)
                            m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
                        else
                            m_Palette = ColorUtils.ReadEightBitPaletteFrom(fileData, palOffset, m_ColorsInPalette);
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
                    m_Palette = ColorUtils.ReadEightBitPaletteFrom(fileData, palOffset, m_ColorsInPalette);
                else if (bitsPerPlane == 1 && numPlanes == 2 && (version < 4 || (width == 320 && height == 200)))
                {
                    m_Palette = LoadPaletteCga(fileData, palOffset, paletteInfo, bitsPerPlane);
                    extraInfo.Append("\nHandled as CGA");
                    if (paletteInfo != 0)
                        extraInfo.Append(" (v4 palette handling)");
                }
                else if (version == 0 || version == 3 && numPlanes < 8 || palOffset == -1)
                    m_Palette = PaletteUtils.GetEgaPalette(bitsPerPlane * numPlanes);
                else
                    m_Palette = ColorUtils.ReadEightBitPaletteFrom(fileData, palOffset, m_ColorsInPalette);
                imageData = ImageUtils.PlanarLinesToLinear(imageData, 0, width, height, numPlanes, bytesPerLine, bitsPerPlane, Image.GetPixelFormatSize(pf), out stride);
            }
            else if (bitsPerPlane == 8 && (numPlanes == 3 || numPlanes == 4))
            {
                pf = numPlanes == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format32bppArgb;
                imageData = ImageUtils.GetPlanarRgb(imageData, 0, width, height, numPlanes == 4, bytesPerLine, out stride);
            }
            if (pf == PixelFormat.Undefined)
                throw new FileTypeLoadException("Unsupported for now.");
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, stride, pf, m_Palette, Color.Empty);
            //if (hDpi != 0 && vDpi != 0 && !(hDpi == width && vDpi == height))
            //    m_LoadedImage.SetResolution(hDpi, vDpi);
            if (nrOfcolors < 0x100 && nrOfcolors != (1 << Image.GetPixelFormatSize(pf)))
                m_LoadedImage.Palette = ImageUtils.GetPalette(m_Palette, m_ColorsInPalette);
            this.ExtraInfo = extraInfo.ToString();
        }

        private Color[] LoadPaletteCga(Byte[] fileData, Int32 index, UInt16 paletteInfo, Int32 bitsPerPixel)
        {
            /* Get the CGA background color */
            Byte cgaBackgroundColor = (Byte)(fileData[index] >> 4);   // 0 to 15
            /* Get the CGA foreground palette */
            if (bitsPerPixel == 1)
                return PaletteUtils.GetCgaPalette(cgaBackgroundColor, false, false, false, bitsPerPixel);

            Boolean cgaPaletteValue;
            Boolean cgaIntensityValue;
            Boolean cgaColorBurstEnable;
            if (paletteInfo != 0) // PB 4.0
            {
                // Evaluate values of RGB colour slot #1 (skip background colour info)
                Byte greenValue1 = fileData[index + 4];
                Byte blueValue1 = fileData[index + 5];
                // Pick green palette (0) if G > B
                cgaPaletteValue = greenValue1 <= blueValue1;
                // Pick bright palette if max(G,B) > 200
                cgaIntensityValue = Math.Max(greenValue1, blueValue1) > 200;
                // Check for palette 2 by testing if the first check returned palette 1, and in the third colour entry, red is smaller or equal to blue.
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
            return PaletteUtils.GetCgaPalette(cgaBackgroundColor, cgaColorBurstEnable, cgaPaletteValue, cgaIntensityValue, bitsPerPixel);
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            throw new NotSupportedException();
            if (fileToSave.IsFramesContainer && fileToSave.Frames != null)
                throw new NotSupportedException("PCX does not allow frames input.");
            Color[] palEntries = fileToSave.GetColors();
            Int32 colors = palEntries.Length;
            if (colors == 0)
                return new SaveOption[] { new SaveOption("PLN", SaveOptionType.Boolean, "Save data as linear, not planar.", "1") };

            SaveOption version = new SaveOption("VER", SaveOptionType.ChoicesList, "PCX version:", "0: Paintbrush v2.5 (pure EGA colors only),2: Paintbrush v2.8 (with palette),3: Paintbrush v2.8 (no palette),4: Paintbrush for Windows,5: Paintbrush v3.0+", "4");
            
            if (colors == 2 || colors == 4)
            {
                Byte backgroundColor;
                Boolean colorBurst;
                Boolean palette;
                Boolean intensity;
                if (PaletteUtils.DetectCgaPalette(palEntries, out backgroundColor, out colorBurst, out palette, out intensity))
                {
                    return new SaveOption[]
                    {
                        new SaveOption("CGA", SaveOptionType.Boolean, "Save as CGA palette.", "1"),
                        new SaveOption("NCG", SaveOptionType.Boolean, "Save as new type CGA.", "0")
                    };
                }
            }

            return base.GetSaveOptions(fileToSave, targetFileName);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Byte[] bytes = new Byte[0];
            // TODO
            throw new NotSupportedException();
            //return bytes;
        }

    }
}