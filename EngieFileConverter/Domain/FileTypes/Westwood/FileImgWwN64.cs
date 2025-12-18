using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgWwN64 : SupportedFileType
    {

        public override FileClass InputFileClass { get { return FileClass.Image; } }

        //bytes 84 21 ==> 8421 (BE) ==bin==> 1000 0100 0010 0001 ==split==> R=10000 G=10000 B=10000 A=1 ==dec==> R=16 G=16 B=16 A=1 ==mul==> R=128 G=128 B=128 A=255
        private static readonly PixelFormatter Format16BitRgba5551Be = new PixelFormatter(2, 0x0001, 0xF800, 0x07C0, 0x003E, false);

        /// <summary>0 = 4bpp, 1=8bpp, 2 = 16bpp</summary>
        protected Byte HdrColorFormat;
        protected Int32 HdrColorsInPalette;

        public override String IdCode { get { return "WwImg64"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 IMG"; } }
        public override String[] FileExtensions { get { return new String[] { "img", "jim" }; } }
        public override String LongTypeName { get { return "Westwood C&C N64 image"; } }

        public override Boolean NeedsPalette { get { return this.HdrColorFormat != 2 && this.HdrColorsInPalette == 0; } }

        public override FileClass FileClass
        {
            get
            {
                if (this.m_LoadedImage == null)
                    return FileClass.None;
                switch (this.HdrColorFormat)
                {
                    case 0:
                        return FileClass.Image4Bit;
                    case 1:
                        return FileClass.Image8Bit;
                    case 2:
                        return FileClass.ImageHiCol;
                }
                return FileClass.None;
            }
        }

        public override Int32 BitsPerPixel
        {
            get
            {
                switch (this.HdrColorFormat)
                {
                    case 0:
                        return 4;
                    case 1:
                        return 8;
                    case 2:
                        return 16;
                    default:
                        return -1;
                }
            }
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            // assume there's no palette, or no backup was ever made
            if (this.m_BackupPalette == null)
                return false;
            return !this.m_Palette.SequenceEqual(this.m_BackupPalette);
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 16)
                throw new FileTypeLoadException("File is not long enough to be a valid N64 IMG file.");
            Int32 hdrDataOffset = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 0);
            Int32 hdrPaletteOffset = ArrayUtils.ReadInt32FromByteArrayBe(fileData, 4);
            Int16 hdrWidth = ArrayUtils.ReadInt16FromByteArrayBe(fileData, 8);
            Int16 hdrHeight = ArrayUtils.ReadInt16FromByteArrayBe(fileData, 0x0A);
            Byte hdrReadBytesPerColor = fileData[0x0C];
            Byte hdrBytesPerColor = hdrReadBytesPerColor;
            this.HdrColorFormat = fileData[0x0D];
            this.HdrColorsInPalette = ArrayUtils.ReadInt16FromByteArrayBe(fileData, 0x0E);
            //if (hdrColorFormat == 2 || hdrPaletteOffset == 0)
            //    hdrColorsInPalette = 0;
            if (hdrDataOffset != 16)
                throw new FileTypeLoadException("File does not have a valid IMG header.");
            if (this.BitsPerPixel == -1)
                throw new FileTypeLoadException("File does not have a valid color depth in the header.");
            Int32 stride = ImageUtils.GetMinimumStride(hdrWidth, this.BitsPerPixel);
            Int32 imageDataSize = stride * hdrHeight;
            Byte[] imageData;
            Int32 expectedSize = hdrDataOffset + imageDataSize;
            if ((this.HdrColorFormat == 0 || this.HdrColorFormat == 1) && hdrPaletteOffset != 0)
                expectedSize = Math.Max(expectedSize, hdrPaletteOffset + hdrBytesPerColor * this.HdrColorsInPalette);
            if (fileData.Length < expectedSize)
                throw new FileTypeLoadException(String.Format("File data is too short. Got {0} bytes, expected {1} bytes.", fileData.Length, expectedSize));
            try
            {
                // Fill image data array. For 16-bit color, reorder to existing Argb1555 format. For 8 or lower, just copy.
                imageData = new Byte[imageDataSize];
                Array.Copy(fileData, hdrDataOffset, imageData, 0, Math.Min(fileData.Length - hdrDataOffset, imageDataSize));
                if (this.HdrColorFormat == 2)
                {
                    PixelFormatter.ReorderBits(imageData, hdrWidth, hdrHeight, stride, Format16BitRgba5551Be, PixelFormatter.Format16BitArgb1555Le);
                }
                if (hdrPaletteOffset != 0)
                {
                    Int32 palSize = hdrBytesPerColor * this.HdrColorsInPalette;
                    Byte[] paletteData = new Byte[palSize];
                    Array.Copy(fileData, hdrPaletteOffset, paletteData, 0, palSize);
                    this.m_Palette = this.Get16BitColors(paletteData, this.HdrColorsInPalette, false);
                }
                else if (this.HdrColorFormat != 2)
                {
                    // No palette in file, but paletted color format. Generate grayscale palette.
                    Int32 bpp = this.BitsPerPixel;
                    this.m_Palette = PaletteUtils.GenerateGrayPalette(bpp, null, false);
                }
                else
                {
                    this.m_Palette = null;
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message);
            }
            try
            {
                PixelFormat pf = this.GetPixelFormat(this.HdrColorFormat);
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, hdrWidth, hdrHeight, stride, pf, this.m_Palette, null);
                // Corrects the number of colors in the palette.
                if (this.m_Palette != null)
                    this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data.");
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            this.PerformPreliminaryChecks(fileToSave);
            // If it is a hi-color image, return empty
            if (fileToSave == null || (fileToSave.FileClass & FileClass.ImageHiCol) != 0)
                return new Option[0];
            // If it is a non-image format which does contain colors, offer to save with palette.
            // Palette option is disabled by default if the palette it would save comes from the UI.
            return new Option[]
            {
                new Option("PAL", OptionInputType.Boolean, "Include palette", (!fileToSave.NeedsPalette ? 0 : 1).ToString()),
            };
        }

        private Bitmap PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (image.Width > 0xFFFF || image.Height > 0xFFFF)
                throw new ArgumentException(ERR_IMAGE_TOO_LARGE, "fileToSave");
            return image;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap image = this.PerformPreliminaryChecks(fileToSave);
            Int32 colors = fileToSave.GetColors().Length;
            Boolean savePalette = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PAL"));
            // 0 = 4bpp, 1 = 8bpp, 2 = 16bpp
            Byte colorFormat;
            Int32 width = image.Width;
            Int32 height = image.Height;
            switch (Image.GetPixelFormatSize(image.PixelFormat))
            {
                case 4:
                    colorFormat = 0;
                    break;
                case 8:
                    colorFormat = 1;
                    break;
                default:
                    colorFormat = 2;
                    break;
            }
            Byte[] imageData;
            Int32 stride;
            if (colorFormat == 2)
            {
                if (image.PixelFormat != PixelFormat.Format16bppArgb1555)
                {
                    using (Bitmap newImage = ImageUtils.PaintOn32bpp(image, null))
                        imageData = ImageUtils.GetImageData(newImage, out stride, PixelFormat.Format16bppArgb1555, true);
                }
                else
                    imageData = ImageUtils.GetImageData(image, out stride, PixelFormat.Format16bppArgb1555, true);
                PixelFormatter.ReorderBits(imageData, width, height, stride, PixelFormatter.Format16BitArgb1555Le, Format16BitRgba5551Be);
            }
            else
                imageData = ImageUtils.GetImageData(image, out stride, true);
            Byte[] paletteData;
            Int32 paletteColors;
            if (colorFormat == 2 || !savePalette)
            {
                paletteData = new Byte[0];
                paletteColors = 0;
            }
            else
            {
                Color[] pal = image.Palette.Entries;
                paletteColors = colors;
                paletteData = new Byte[paletteColors * 2];
                Int32 maxEntry = Math.Min(pal.Length, paletteColors);
                for (Int32 i = 0; i < maxEntry; ++i)
                    Format16BitRgba5551Be.WriteColor(paletteData, i * 2, pal[i]);
            }
            Int32 paletteOffset = paletteColors == 0 ? 0 : 16 + imageData.Length;
            Int32 palbpc = colorFormat > 1 ? 0 : (!savePalette ? 4 : 2);
            // Header
            Byte[] fullData = new Byte[0x10 + imageData.Length + paletteData.Length];
            //DataOffset
            ArrayUtils.WriteInt32ToByteArrayBe(fullData, 0x00, 16);
            //PaletteOffset
            ArrayUtils.WriteInt32ToByteArrayBe(fullData, 0x04, paletteOffset);
            //Width
            ArrayUtils.WriteUInt16ToByteArrayBe(fullData, 0x08, (UInt16)width);
            //Height
            ArrayUtils.WriteUInt16ToByteArrayBe(fullData, 0x0A, (UInt16)height);
            //BytesPerColor
            fullData[0x0C] = (Byte)palbpc;
            //ColorFormat
            fullData[0x0D] = colorFormat;
            //ColorsInPalette
            ArrayUtils.WriteUInt16ToByteArrayBe(fullData, 0x0E, (UInt16)paletteColors);
            Int32 targetOffs = 0x10;

            // Image data
            Array.Copy(imageData, 0, fullData, targetOffs, imageData.Length);
            targetOffs += imageData.Length;
            // Palette data
            Array.Copy(paletteData, 0, fullData, targetOffs, paletteData.Length);
            return fullData;
        }

        public void LoadGrayImage(Bitmap img, String displayFileName, String fullFilePath)
        {
            this.HdrColorFormat = 1;
            this.HdrColorsInPalette = 0;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            this.m_LoadedImage = ImageUtils.ConvertToPalettedGrayscale(img);
            this.LoadedFile = fullFilePath;
            this.LoadedFileName = displayFileName;
        }

        /// <summary>
        /// Returns the pixel format corresponding to the N64 IMG header value.
        /// </summary>
        /// <returns>The pixel format.</returns>
        protected PixelFormat GetPixelFormat(Int32 hdrColorFormat)
        {
            PixelFormat pf;
            switch (hdrColorFormat)
            {
                case 0:
                    pf = PixelFormat.Format4bppIndexed;
                    break;
                case 1:
                    pf = PixelFormat.Format8bppIndexed;
                    break;
                default:
                case 2:
                    pf = PixelFormat.Format16bppArgb1555;
                    break;
            }
            return pf;
        }

        protected Color[] Get16BitColors(Byte[] paletteData, Int32 paletteLength, Boolean saveFullPal)
        {
            if (paletteData == null)
                return null;
            Int32 maxPalSize = (Int32)Math.Pow(2, this.BitsPerPixel);
            Int32 palSize = paletteLength;
            if (palSize == 0)
                palSize = maxPalSize;
            Int32 palLen = saveFullPal ? maxPalSize : palSize;
            Color[] entries = new Color[palLen];
            for (Int32 i = 0; i < palLen; ++i)
            {
                if (i < palSize)
                    entries[i] = Format16BitRgba5551Be.GetColor(paletteData, i * 2);
                else
                    entries[i] = Color.Empty;
            }
            return entries;
        }

    }
}