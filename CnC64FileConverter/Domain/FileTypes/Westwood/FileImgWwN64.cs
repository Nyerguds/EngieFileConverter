using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImgWwN64 : SupportedFileType
    {

        public override FileClass FileClass
        {
            get
            {
                if (this.m_LoadedImage == null)
                    return FileClass.None;
                switch (hdrColorFormat)
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

        //bytes 84 21 ==> 8421 (BE) ==bin==> 1000 0100 0010 0001 ==split==> 10000 10000 10000 1 ==dec==> 16 16 16 1 ==x8==> 128 128 128 1
        private static PixelFormatter SixteenBppFormatter = new PixelFormatter(2, 5, 11, 5, 6, 5, 1, 1, 0, false);
        public override Int32 Width { get { return hdrWidth; } }
        public override Int32 Height { get { return hdrHeight; } }

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

        public override Int32 ColorsInPalette { get { return hdrPaletteOffset == 0 ? 0 : this.hdrColorsInPalette; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // If it is a hi-colour image, return empty
            if ((fileToSave.FileClass & FileClass.ImageHiCol) != 0)
                return new SaveOption[0];
            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = fileToSave != null && (fileToSave is FileImage || fileToSave.ColorsInPalette != 0);
            return new SaveOption[]
            {
                new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", (hasColors ? 1 : 0).ToString()),
            };
        }


        public override Int32 BitsPerColor
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
            LoadFromFileData(fileData);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData);
            SetFileNames(filename);
        }

        public override Color[] GetColors()
        {
            // ensures the UI can show the partial palette.
            return this.m_Palette == null ? null : this.m_Palette.ToArray();
        }

        public override void SetColors(Color[] palette)
        {
            if (this.m_BackupPalette == null)
                this.m_BackupPalette = GetColors();
            this.m_Palette = palette;
            base.SetColors(palette);
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
            return SaveImg(fileToSave.GetBitmap(), fileToSave.GetColors().Length, asPaletted);
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
            if (this.BitsPerColor == -1)
                throw new FileTypeLoadException("File does not have a valid color depth in the header.");

            // WARNING! The hi-colour format is 16 BPP, but the image data is converted to 32 bpp for creating the actual image!
            Int32 stride = ImageUtils.GetMinimumStride(this.Width, this.BitsPerColor);
            Int32 imageDataSize = ImageUtils.GetMinimumStride(this.Width, this.BitsPerColor) * this.Height;
            Byte[] imageData;
            Int32 expectedSize = this.hdrDataOffset + imageDataSize;
            if ((this.hdrColorFormat == 0 || this.hdrColorFormat == 1) && this.hdrPaletteOffset != 0)
                expectedSize = Math.Max(expectedSize, this.hdrPaletteOffset + this.hdrBytesPerColor * this.hdrColorsInPalette);
            if (fileData.Length < expectedSize)
                throw new FileTypeLoadException(String.Format("File data is too short. Got {0} bytes, expected {1} bytes.", fileData.Length, expectedSize));
            try
            {
                // Fill image data array. For 16-bit colour, convert to 32 bit. For 8 or lower, just copy.
                if (this.hdrColorFormat != 2)
                {
                    imageData = new Byte[imageDataSize];
                    Array.Copy(fileData, this.hdrDataOffset, imageData, 0, Math.Min(fileData.Length - this.hdrDataOffset, imageDataSize));
                }
                else
                    imageData = Convert16bTo32b(fileData, this.hdrDataOffset, this.Width, this.Height, ref stride);
                if (this.hdrPaletteOffset != 0)
                {
                    Int32 palSize = this.hdrBytesPerColor * this.hdrColorsInPalette;
                    Byte[] paletteData = new Byte[palSize];
                    Array.Copy(fileData, this.hdrPaletteOffset, paletteData, 0, palSize);
                    this.m_Palette = Get16BitColors(paletteData, this.hdrColorsInPalette, false);
                }
                else if (this.hdrColorFormat != 2)
                {
                    // No palette in file, but paletted color format. Generate grayscale palette.
                    Int32 bpp = this.BitsPerColor;

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
                //Int32 stride = ImageUtils.GetMinimumStride(this.Width, Image.GetPixelFormatSize(pf));
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
            Int32 bpp = Image.GetPixelFormatSize(image.PixelFormat);
            switch (bpp)
            {
                case 4:
                    colorFormat = 0;
                    break;
                case 8:
                    colorFormat = 1;
                    break;
                case 32:
                    colorFormat = 2;
                    break;
                default:
                    image = ImageUtils.PaintOn32bpp(image, Color.Transparent);
                    colorFormat = 2;
                    break;
            }
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            // Collapse stride
            imageData = ImageUtils.CollapseStride(imageData, width, height, bpp, ref stride);
            if (colorFormat == 2)
                imageData = Convert32bTo16b(imageData, width, height, ref stride);
            
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
                    SixteenBppFormatter.WriteColor(paletteData, i * 2, pal[i]);
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

        protected static Byte[] Convert16bTo32b(Byte[] imageData, Int32 startOffset, Int32 width, Int32 height, ref Int32 stride)
        {
            Int32 newImageStride = width * 4; ;
            Byte[] newImageData = new Byte[height * newImageStride];
            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x++)
                {
                    Int32 sourceOffset = y * stride + x * 2;
                    Int32 targetOffset = y * newImageStride + x * 4;
                    Color c = SixteenBppFormatter.GetColor(imageData, startOffset + sourceOffset);
                    PixelFormatter.Format32BitArgb.WriteColor(newImageData, targetOffset, c);
                }
            }
            stride = newImageStride;
            return newImageData;
        }

        protected static Byte[] Convert32bTo16b(Byte[] imageData, Int32 width, Int32 height, ref Int32 stride)
        {
            Int32 newStride = width * 2;
            Byte[] newImageData = new Byte[newStride * height];

            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = 0; x < width; x += 1)
                {
                    Int32 inputOffs = y * stride + x*4;
                    Int32 outputOffs = y * newStride + x*2;
                    Color c = PixelFormatter.Format32BitArgb.GetColor(imageData, inputOffs);
                    SixteenBppFormatter.WriteColor(newImageData, outputOffs, c);
                }
            }
            stride = newStride;
            return newImageData;
        }

        public void LoadGrayImage(Bitmap img, String displayFileName, String fullFilePath)
        {
            hdrBytesPerColor = 4;
            hdrReadBytesPerColor = 4;
            hdrColorFormat = 1;
            hdrColorsInPalette = 0;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            this.m_LoadedImage = ImageUtils.ConvertToPalettedGrayscale(img);
            this.LoadedFile = fullFilePath;
            this.LoadedFileName = displayFileName;
            this.ExtraInfo = "Palette: No";
        }

        /// <summary>
        /// Only for internal use; this assumes that 16 bit images are processed as 32 bit.
        /// </summary>
        /// <returns></returns>
        protected PixelFormat GetPixelFormat()
        {
            PixelFormat pf;
            switch (this.hdrColorFormat)
            {
                case 0:
                    pf = PixelFormat.Format4bppIndexed;
                    break;
                case 1:
                default: // Not sure if using this as default is a good idea...
                    pf = PixelFormat.Format8bppIndexed;
                    break;
                case 2:
                    pf = PixelFormat.Format32bppArgb;
                    break;
            }
            return pf;
        }

        protected Color[] Get16BitColors(Byte[] paletteData, Int32 paletteLength, Boolean saveFullPal)
        {
            if (paletteData == null)
                return null;
            Int32 maxPalSize = (Int32)Math.Pow(2, this.BitsPerColor);
            Int32 palSize = paletteLength;
            if (palSize == 0)
                palSize = maxPalSize;
            Int32 palLen = saveFullPal ? maxPalSize : palSize;
            Color[] entries = new Color[palLen];
            for (Int32 i = 0; i < palLen; i++)
            {
                if (i < palSize)
                    entries[i] = SixteenBppFormatter.GetColor(paletteData, i * 2);
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