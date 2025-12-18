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

        public override FileClass FileClass
        {
            get
            {
                if (this.m_LoadedImage == null)
                    return FileClass.None;
                switch (this.hdrColorFormat)
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
        public override FileClass InputFileClass { get { return FileClass.Image; } }

        //bytes 84 21 ==> 8421 (BE) ==bin==> 1000 0100 0010 0001 ==split==> R=10000 G=10000 B=10000 A=1 ==dec==> 16 16 16 1 ==x8==> 128 128 128 1
        private static readonly PixelFormatter Format16BitRgba5551Be = new PixelFormatter(2, 0x0001, 0xF800, 0x07C0, 0x003E, false);
        public override Int32 Width { get { return this.hdrWidth; } }
        public override Int32 Height { get { return this.hdrHeight; } }

        protected Int32 hdrDataOffset;
        protected Int32 hdrPaletteOffset;
        protected Int16 hdrWidth;
        protected Int16 hdrHeight;

        /// <summary>Bytes per color on the palette. 0 for no palette. Is 4 on grayscale images for some reason.</summary>
        protected Byte hdrBytesPerColor;
        /// <summary>Original 'Bytes per color' value in the header. Can be 0 for grayscale, but in that case hdrBytesPerColor will be replaced by the value for the generated palette.</summary>
        protected Byte hdrReadBytesPerColor;
        /// <summary>0 = 4bpp, 1=8bpp, 2 = 16bpp</summary>
        protected Byte hdrColorFormat;
        protected Int32 hdrColorsInPalette;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 IMG"; } }
        public override String[] FileExtensions { get { return new String[] { "img", "jim" }; } }
        public override String ShortTypeDescription { get { return "Westwood C&C N64 image"; } }

        public override Int32 ColorsInPalette { get { return this.hdrPaletteOffset == 0 ? 0 : this.hdrColorsInPalette; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // If it is a hi-colour image, return empty
            if (fileToSave == null || (fileToSave.FileClass & FileClass.ImageHiCol) != 0)
                return new SaveOption[0];
            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = (fileToSave is FileImage || fileToSave.ColorsInPalette != 0);
            return new SaveOption[]
            {
                new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", (hasColors ? 1 : 0).ToString()),
            };
        }


        public override Int32 BitsPerPixel
        {
            get
            {
                switch (this.hdrColorFormat)
                {
                    case 0:
                        return 4;
                    case 1:
                        return 8;
                    case 2:
                        return 16; // odd format; big-endian
                    default:
                        return -1;
                }
            }
        }

        public FileImgWwN64() { }

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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Boolean asPaletted = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            return this.SaveImg(fileToSave.GetBitmap(), fileToSave.GetColors().Length, asPaletted);
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 16)
                throw new FileTypeLoadException("File is not long enough to be a valid N64 IMG file.");
            try
            {
                this.ReadHeader(fileData);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading header data: " + e.Message);
            }
            if (this.hdrDataOffset != 16)
                throw new FileTypeLoadException("File does not have a valid IMG header.");
            // This doesn't support 4bpp with odd width at the moment. Should add code from the font editor to fix that.
            if (this.BitsPerPixel == -1)
                throw new FileTypeLoadException("File does not have a valid color depth in the header.");

            // WARNING! The hi-colour format is 16 BPP, but the image data is converted to 32 bpp for creating the actual image!
            Int32 stride = ImageUtils.GetMinimumStride(this.Width, this.BitsPerPixel);
            Int32 imageDataSize = ImageUtils.GetMinimumStride(this.Width, this.BitsPerPixel) * this.Height;
            Byte[] imageData;
            Int32 expectedSize = this.hdrDataOffset + imageDataSize;
            if ((this.hdrColorFormat == 0 || this.hdrColorFormat == 1) && this.hdrPaletteOffset != 0)
                expectedSize = Math.Max(expectedSize, this.hdrPaletteOffset + this.hdrBytesPerColor * this.hdrColorsInPalette);
            if (fileData.Length < expectedSize)
                throw new FileTypeLoadException(String.Format("File data is too short. Got {0} bytes, expected {1} bytes.", fileData.Length, expectedSize));
            try
            {
                // Fill image data array. For 16-bit colour, convert to 32 bit. For 8 or lower, just copy.                
                imageData = new Byte[imageDataSize];
                Array.Copy(fileData, this.hdrDataOffset, imageData, 0, Math.Min(fileData.Length - this.hdrDataOffset, imageDataSize));
                if (this.hdrColorFormat == 2)
                    ImageUtils.ReorderBits(imageData, this.Width, this.Height, stride, Format16BitRgba5551Be, PixelFormatter.Format16BitArgb1555);
                //imageData = Convert16bTo32b(fileData, this.hdrDataOffset, this.Width, this.Height, ref stride);
                if (this.hdrPaletteOffset != 0)
                {
                    Int32 palSize = this.hdrBytesPerColor * this.hdrColorsInPalette;
                    Byte[] paletteData = new Byte[palSize];
                    Array.Copy(fileData, this.hdrPaletteOffset, paletteData, 0, palSize);
                    this.m_Palette = this.Get16BitColors(paletteData, this.hdrColorsInPalette, false);
                }
                else if (this.hdrColorFormat != 2)
                {
                    // No palette in file, but paletted color format. Generate grayscale palette.
                    Int32 bpp = this.BitsPerPixel;
                    
                    this.m_Palette = PaletteUtils.GenerateGrayPalette(bpp, null, false);
                    // Ignore original value here.
                    this.hdrBytesPerColor = 4;
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
                PixelFormat pf = this.GetPixelFormat();
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, stride, pf, this.m_Palette, null);

                if (this.m_Palette != null)
                    this.m_LoadedImage.Palette = BitmapHandler.GetPalette(this.m_Palette);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        protected Byte[] SaveImg(Bitmap image, Int32 colors, Boolean savePalette)
        {
            if (image.Width > 0xFFFF || image.Height > 0xFFFF)
                throw new NotSupportedException("Image is too large!");
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
            }
            else
                imageData = ImageUtils.GetImageData(image, out stride, true);
            if (colorFormat == 2)
                ImageUtils.ReorderBits(imageData, width, height, stride, PixelFormatter.Format16BitArgb1555, Format16BitRgba5551Be);
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
                for (Int32 i = 0; i < maxEntry; i++)
                    Format16BitRgba5551Be.WriteColor(paletteData, i * 2, pal[i]);
            }
            Int32 paletteOffset = paletteColors == 0 ? 0 : 16 + imageData.Length;
            Int32 palbpc = colorFormat > 1 ? 0 : (!savePalette ? 4 : 2);
            // Header
            Byte[] fullData = new Byte[0x10 + imageData.Length + paletteData.Length];
            //DataOffset
            ArrayUtils.WriteIntToByteArray(fullData, 0x00, 4, false, 16);
            //PaletteOffset
            ArrayUtils.WriteIntToByteArray(fullData, 0x04, 4, false, (UInt32)paletteOffset);
            //Width
            ArrayUtils.WriteIntToByteArray(fullData, 0x08, 2, false, (UInt32)width);
            //Height
            ArrayUtils.WriteIntToByteArray(fullData, 0x0A, 2, false, (UInt32)height);
            //BytesPerColor
            fullData[0x0C] = (Byte)palbpc;
            //ColorFormat
            fullData[0x0D] = colorFormat;
            //ColorsInPalette
            ArrayUtils.WriteIntToByteArray(fullData, 0x0E, 2, false, (UInt32)paletteColors);
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
            this.hdrBytesPerColor = 4;
            this.hdrReadBytesPerColor = 4;
            this.hdrColorFormat = 1;
            this.hdrColorsInPalette = 0;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            this.m_LoadedImage = ImageUtils.ConvertToPalettedGrayscale(img);
            this.LoadedFile = fullFilePath;
            this.LoadedFileName = displayFileName;
        }

        /// <summary>
        /// Returns the pixel format corresponding to the N64 IMG header value.
        /// </summary>
        /// <returns>The pixel format.</returns>
        protected PixelFormat GetPixelFormat()
        {
            PixelFormat pf;
            switch (this.hdrColorFormat)
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
            for (Int32 i = 0; i < palLen; i++)
            {
                if (i < palSize)
                    entries[i] = Format16BitRgba5551Be.GetColor(paletteData, i * 2);
                else
                    entries[i] = Color.Empty;
            }
            return entries;
        }

        protected void ReadHeader(Byte[] headerBytes)
        {
            if (headerBytes.Length < 10)
                return;
            this.hdrDataOffset = (Int32)ArrayUtils.ReadIntFromByteArray(headerBytes, 0, 4, false);
            this.hdrPaletteOffset = (Int32)ArrayUtils.ReadIntFromByteArray(headerBytes, 4, 4, false);
            this.hdrWidth = (Int16)ArrayUtils.ReadIntFromByteArray(headerBytes, 8, 2, false);
            this.hdrHeight = (Int16)ArrayUtils.ReadIntFromByteArray(headerBytes, 0x0A, 2, false);
            this.hdrReadBytesPerColor = headerBytes[0x0C];
            this.hdrBytesPerColor = this.hdrReadBytesPerColor;
            this.hdrColorFormat = headerBytes[0x0D];
            this.hdrColorsInPalette = (Int16)ArrayUtils.ReadIntFromByteArray(headerBytes, 0x0E, 2, false);
        }
    }
}