using CnC64FileConverter.Domain.Utils;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CnC64FileConverter.Domain.ImageFile
{

    // for the autodetection

    public class FileImgN64Basic1 : FileImgN64
    {
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
    }

    public class FileImgN64Basic2 : FileImgN64
    {
        public override String[] FileExtensions { get { return new String[] { "jim" }; } }
    }

    public class FileImgN64 : N64FileType
    {
        public override Boolean FileHasPalette { get { return this.hdrPaletteOffset != 0 && hdrColorsInPalette > 0; } }
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
        protected Color[] palette;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Img"; } }
        public override String[] FileExtensions { get { return new String[] { "img", "jim" }; } }
        public override String ShortTypeDescription { get { return "C&C64 Image file"; } }

        public override Int32 ColorsInPalette
        {
            get
            {
                if (this.hdrColorsInPalette > 0 || this.hdrColorFormat >= 2)
                    return this.hdrColorsInPalette;
                return (Int32)Math.Pow(2, this.GetBitsPerColor());
            }
        }

        public FileImgN64() { }
        
        public override void LoadImage(Byte[] fileData)
        {
            LoadFromFileData(fileData);
        }

        public override void LoadImage(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData);
            LoadedFileName = filename;
        }

        public override Int32 GetBitsPerColor()
        {
            switch (this.hdrColorFormat)
            {
                case 0:
                    return 4;
                case 1:
                    return 8;
                case 2:
                    return 16; // odd format; little-endian
                default:
                    return -1;
            }
        }

        public override Color[] GetColors()
        {
            // ensures the UI can show the partial palette.
            return palette;
        }
        
        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            SaveImg(fileToSave.GetBitmap(), savePath, false);
        }
        
        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 16)
                throw new FileTypeLoadException("File is not long enough to be a valid IMG file.");
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
            if (this.GetBitsPerColor() == -1)
                throw new FileTypeLoadException("File does not have a valid color depth in the header.");

            // WARNING! The hi-colour format is 16 BPP, but the image data is converted to 32 bpp for creating the actual image!
            Int32 imageDataSize = ImageUtils.GetMinimumStride(this.Width, GetBitsPerColor()) * this.Height;
            Byte[] imageData = new Byte[imageDataSize];
            Int32 expectedSize = this.hdrDataOffset + imageDataSize;
            if ((this.hdrColorFormat == 0 || this.hdrColorFormat == 1) && this.hdrPaletteOffset != 0)
                expectedSize = Math.Max(expectedSize, this.hdrPaletteOffset + this.hdrBytesPerColor * this.hdrColorsInPalette);
            if (fileData.Length < expectedSize)
                throw new FileTypeLoadException(String.Format("File data is too short. Got {0} bytes, expected {1} bytes.", fileData.Length, expectedSize));
            Color[] fullpalette;
            try
            {
                Array.Copy(fileData, this.hdrDataOffset, imageData, 0, Math.Min(fileData.Length - this.hdrDataOffset, imageDataSize));
                // Convert image data to usable format.
                if (this.hdrColorFormat == 2)
                    imageData = Convert16bTo32b(imageData, this.Width * this.Height);
                if (this.hdrPaletteOffset != 0)
                {
                    Int32 palSize = this.hdrBytesPerColor * this.ColorsInPalette;
                    Byte[] paletteData = new Byte[palSize];
                    Array.Copy(fileData, this.hdrPaletteOffset, paletteData, 0, palSize);
                    this.palette = GetColors(paletteData, this.hdrBytesPerColor, this.ColorsInPalette, false);
                    fullpalette = GetColors(paletteData, this.hdrBytesPerColor, this.ColorsInPalette, true);
                }
                else if (this.hdrColorFormat != 2)
                {
                    // No palette in file, but paletted color format. Generate grayscale palette.
                    Int32 bpp = this.GetBitsPerColor();
                    this.palette = ColorUtils.GenerateGrayPalette(bpp);
                    fullpalette = ColorUtils.GenerateGrayPalette(bpp);
                    // Ignore original value here.
                    this.hdrBytesPerColor = 4;
                }
                else
                {
                    this.palette = null;
                    fullpalette = null;
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message);
            }
            try
            {
                PixelFormat pf = this.GetPixelFormat();
                Int32 stride = ImageUtils.GetMinimumStride(this.Width, Image.GetPixelFormatSize(pf));
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, stride, pf, fullpalette, Color.Black);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        protected void SaveImg(Bitmap image, String savePath, Boolean asNoPalGray8bpp)
        {
            if (image.Width > 0xFFFF || image.Height > 0xFFFF)
                throw new NotSupportedException("Image is too large!");
            Int32 origPfs = Image.GetPixelFormatSize(image.PixelFormat);

            if (asNoPalGray8bpp && image.PixelFormat != PixelFormat.Format32bppArgb)
            {
                if (image.PixelFormat == PixelFormat.Format1bppIndexed || !ColorUtils.HasGrayPalette(image))
                    image = ImageUtils.PaintOn32bpp(image);
                else
                {
                    if (!ColorUtils.HasGrayPalette(image))
                        image = ImageUtils.PaintOn32bpp(image);
                }
            }

            // 0 = 4bpp, 1=8bpp, 2 = 16bpp
            Byte colorFormat;
            Int32 width = image.Width;
            Int32 height = image.Height;
            switch (image.PixelFormat)
            {
                case PixelFormat.Format4bppIndexed:
                    colorFormat = 0;
                    break;
                case PixelFormat.Format8bppIndexed:
                    colorFormat = 1;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppRgb:
                    colorFormat = 2;
                    break;
                default:
                    image = ImageUtils.PaintOn32bpp(image);
                    colorFormat = 2;
                    break;
            }
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            if (colorFormat == 2)
            {
                if (asNoPalGray8bpp)
                {
                    Int32 grayPfs = Math.Min(8, origPfs);
                    imageData = ImageUtils.Convert32bToGray(imageData, width, height, grayPfs, ref stride);
                    colorFormat = (Byte)(grayPfs == 4 ? 0 : 1);
                }
                else
                    imageData = Convert32bTo16b(imageData, width, height, ref stride);
            }
            Byte[] paletteData;
            Int32 paletteColors;
            if (colorFormat == 2 || asNoPalGray8bpp)
            {
                paletteData = new Byte[0];
                paletteColors = 0;
            }
            else
            {
                Color[] pal = image.Palette.Entries;
                paletteColors = pal.Length;
                paletteData = new Byte[paletteColors * 2];
                for (Int32 i=0; i < pal.Length; i++)
                {
                    WriteColorToData(pal[i], paletteData, i*2, 2, true);
                }
            }
            Int32 paletteOffset = paletteColors == 0 ? 0 : 16 + imageData.Length;
            Int32 palbpc = colorFormat > 1 ? 0 : (asNoPalGray8bpp ? 4 : 2);

            Byte[] header = new Byte[16];
            //DataOffset
            header[0x00] = 00;
            header[0x01] = 00;
            header[0x02] = 00;
            header[0x03] = 16;
            //PaletteOffset
            header[0x04] = (Byte)((paletteOffset >> 24) & 0xFF);
            header[0x05] = (Byte)((paletteOffset >> 16) & 0xFF);
            header[0x06] = (Byte)((paletteOffset >> 8) & 0xFF);
            header[0x07] = (Byte)(paletteOffset & 0xFF);
            //Width
            header[0x08] = (Byte)((width >> 8) & 0xFF);
            header[0x09] = (Byte)(width & 0xFF);
            //Height
            header[0x0A] = (Byte)((height >> 8) & 0xFF);
            header[0x0B] = (Byte)(height & 0xFF);
            //BytesPerColor
            header[0x0C] = (Byte)palbpc;
            //ColorFormat
            header[0x0D] = colorFormat;
            //ColorsInPalette
            header[0x0E] = (Byte)((paletteColors >> 8) & 0xFF);
            header[0x0F] = (Byte)(paletteColors & 0xFF);

            Byte[] fullData = new Byte[header.Length + imageData.Length + paletteData.Length];
            Int32 targetOffs = 0;
            Array.Copy(header, 0, fullData, targetOffs, header.Length);
            targetOffs += header.Length;
            Array.Copy(imageData, 0, fullData, targetOffs, imageData.Length);
            targetOffs += imageData.Length;
            Array.Copy(paletteData, 0, fullData, targetOffs, paletteData.Length);

            File.WriteAllBytes(savePath, fullData);
        }

        protected static Byte[] Convert16bTo32b(Byte[] imageData, Int32 entries)
        {
            Byte[] newImageData = new Byte[entries*4];
            for (Int32 i = 0; i < entries; i++)
            {
                Int32 offs = i * 4;
                Color c = GetColor(imageData, i*2, 2, true);
                newImageData[offs + 3] = c.A;
                newImageData[offs + 2] = c.R;
                newImageData[offs + 1] = c.G;
                newImageData[offs + 0] = c.B; 
            }
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
                    Color c = GetColor(imageData, inputOffs, 4, true);
                    WriteColorToData(c, newImageData, outputOffs, 2, true);
                }
            }
            stride = newStride;
            return newImageData;
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

        public Color[] GetColors(Byte[] paletteData, Int32 bytesPerColor, Int32 paletteLength, Boolean saveFullPal)
        {
            Int32 bytespercol = bytesPerColor;
            if (bytesPerColor == 0 || paletteData == null)
                return null;
            Int32 maxPalSize = (Int32)Math.Pow(2, GetBitsPerColor());
            Int32 palSize = paletteLength;
            if (palSize == 0)
                palSize = maxPalSize;
            Int32 palLen = saveFullPal ? maxPalSize : palSize;
            Color[] entries = new Color[palLen];
            // Only 2 color lengths are supported.
            if (bytespercol != 2 && bytespercol != 4)
                return new Color[0];
            for (Int32 i = 0; i < palLen; i++)
            {
                if (i < palSize)
                    entries[i] = GetColor(paletteData, i * bytespercol, bytespercol, true);
                else
                    entries[i] = Color.Empty;
            }
            return entries;
        }
        
        /// <summary>
        /// Gets a colour from data
        /// </summary>
        /// <param name="data">The data array</param>
        /// <param name="offset">The offset of the color in the data array.</param>
        /// <param name="colorLength">Length of one color in the data array</param>
        /// <param name="allowTransparency">False to force transaprency to 255</param>
        /// <returns>The color data at that position</returns>
        protected static Color GetColor(Byte[] data, Int32 offset, Int32 colorLength, Boolean allowTransparency)
        {
            if (offset >= data.Length)
                return Color.Empty;
            if (colorLength == 4)
                return ImageUtils.GetColorFrom32BitData(data, offset, allowTransparency);
            if (colorLength != 2)
                return Color.Empty;
            //8421 ==bin==> 1000 0100 0010 0001 ==split==> 10000 10000 10000 1 ==dec==> 16 16 16 1 ==x8==> 128 128 128 1
            Int32 val = (data[offset] << 8) + data[offset + 1];
            Int32 alpha = allowTransparency? (val & 0x01) * 255 : 255;
            Int32 blue = ((val >> 1) & 0x1F) * 8;
            Int32 green = ((val >> 6) & 0x1F) * 8;
            Int32 red = ((val >> 11) & 0x1F) * 8;
            return Color.FromArgb(alpha, red, green, blue);
        }

        /// <summary>
        /// Gets a colour from data
        /// </summary>
        /// <param name="color">The color to convert</param>
        /// <param name="data">The data array that's being written. Should be defined and large enough.</param>
        /// <param name="offset">The offset in the data array that's being written.</param>
        /// <param name="colorLength">Length of one color in the data array</param>
        /// <param name="allowTransparency">False to force transaprency to 255</param>
        /// <returns>The color data at that position</returns>
        protected static void WriteColorToData(Color color, Byte[] data, Int32 offset, Int32 colorLength, Boolean allowTransparency)
        {
            if (offset + colorLength > data.Length)
                return;
            if (colorLength == 4)
            {
                ImageUtils.Write32BitColorToData(color, data, offset, allowTransparency);
                return;
            }
            //8421 ==bin==> 1000 0100 0010 0001 ==split==> 10000 10000 10000 1 ==dec==> 16 16 16 1 ==x8==> 128 128 128 1
            // A 00000 00000 00000 1 = val & 0x01
            // B 00000 00000 11111 0 = (val & 0x1F) << 1
            // G 00000 11111 10000 0 = (val & 0x1F) << 6
            // R 11111 00000 00000 0 = (val & 0x1F) << 11

            Int32 alpha = allowTransparency && color.A < 127 ? 0 : 1;
            Int32 blue = ((color.B / 8) & 0x1F) << 1;
            Int32 green = ((color.G / 8) & 0x1F) << 6;
            Int32 red = ((color.R / 8) & 0x1F) << 11;
            Int32 val = red | green | blue | alpha;
            data[offset] = (Byte)((val >> 8) & 0xFF);
            data[offset + 1] = (Byte)(val & 0xFF);
        }

        protected void ReadHeader(Byte[] headerBytes)
        {
            if (headerBytes.Length < 10)
                return;
            this.hdrDataOffset = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0);
            this.hdrPaletteOffset = ArrayUtils.GetBEIntFromByteArray(headerBytes, 4);
            this.hdrWidth = ArrayUtils.GetBEShortFromByteArray(headerBytes, 8);
            this.hdrHeight = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x0A);
            this.hdrReadBytesPerColor = headerBytes[0x0C];
            this.hdrBytesPerColor = this.hdrReadBytesPerColor;
            this.hdrColorFormat = headerBytes[0x0D];
            this.hdrColorsInPalette = ArrayUtils.GetBEShortFromByteArray(headerBytes, 0x0E);
        }
    }
}