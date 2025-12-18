using ColorManipulation;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace CnC64ImgViewer.Domain
{
    public class ImgFile
    {

        public Int32 DataOffset { get; private set; }
        public Int32 PaletteOffset { get; private set; }
        public Int16 Width { get; private set; }
        public Int16 Height { get; private set; }
        /// <summary>Bytes per color on the palette. 0 for no palette (color mode 2 only)</summary>
        public Byte BytesPerColor { get; private set; }
        public Byte ReadBytesPerColor { get; private set; }
        public Byte ColorFormat { get; private set; } // 0 = 4bpp, 1=8bpp, 2 = 16bpp
        public Int32 ColorsInPalette
        {
            get
            {
                if (this.ReadColorsInPalette > 0 || this.ColorFormat >= 2)
                    return this.ReadColorsInPalette;
                return (Int32)Math.Pow(2, Image.GetPixelFormatSize(this.GetPixelFormat()));
            }
        }
        public Int32 ReadColorsInPalette { get; private set; }
        public Byte[] ImageData { get; private set; }
        public Byte[] PaletteData { get; private set; }

        private ImgFile()
        {

        }

        public static ImgFile LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 16)
                return null;
            ImgFile img = new ImgFile();
            img.ReadHeader(fileData);
            if (img.DataOffset != 16)
                return null;
            try
            {
                Int32 imageDataSize = (img.Width * img.Height * img.GetBpp()) / 8;
                Byte[] imageData = new Byte[imageDataSize];
                Array.Copy(fileData, img.DataOffset, imageData, 0, imageDataSize);
                if (img.ColorFormat == 2)
                    imageData = Convert16bTo32b(imageData, img.Width * img.Height);
                img.ImageData = imageData;
                if (img.PaletteOffset != 0)
                {
                    Int32 palSize = img.BytesPerColor * img.ColorsInPalette;
                    Byte[] paletteData = new Byte[palSize];
                    Array.Copy(fileData, img.PaletteOffset, paletteData, 0, palSize);
                    img.PaletteData = paletteData;
                }
                else if (img.ColorFormat != 2)
                {
                    Int32 palSize = img.ColorsInPalette;
                    if (palSize == 0)
                        palSize = (Int32)Math.Pow(2, Image.GetPixelFormatSize(img.GetPixelFormat()));
                    // generate greyscale palette. Ignore original value here.
                    img.BytesPerColor = 4;
                    Byte[] paletteData = new Byte[palSize * 4];
                    Double fraction = 256.0 / (Double)(palSize-1);
                    Int32 steps = 255 / (palSize - 1);
                    for (Int32 i = 0; i < palSize; i++)
                    {
                        Int32 offs = i * 4;
                        Byte grayval = (Byte)Math.Min(255, Math.Round((Double)i * steps, MidpointRounding.AwayFromZero));
                        paletteData[offs + 0] = 255;
                        paletteData[offs + 1] = grayval;
                        paletteData[offs + 2] = grayval;
                        paletteData[offs + 3] = grayval;
                    }
                    img.PaletteData = paletteData;
                }
                return img;
            }
            catch
            {
                return null;
            }
        }

        private static Byte[] Convert16bTo32b(Byte[] imageData, Int32 entries)
        {
            Byte[] newImageData = new Byte[entries*4];
            for (Int32 i = 0; i < entries; i++)
            {
                Int32 offs = i * 4;
                Color c = GetColor(imageData, i, 2, true);
                newImageData[offs + 3] = c.A;
                newImageData[offs + 2] = c.R;
                newImageData[offs + 1] = c.G;
                newImageData[offs + 0] = c.B; 
            }
            return newImageData;
        }

        public Int32 GetBpp()
        {
            Int32 bpp;
            switch (this.ColorFormat)
            {
                case 0:
                    bpp = 4;
                    break;
                case 1:
                default: // Not sure if using this as default is a good idea...
                    bpp = 8;
                    break;
                case 2:
                    bpp = 16; // odd format; little-endian
                    break;
            }
            return bpp;
        }
        
        public PixelFormat GetPixelFormat()
        {
            PixelFormat pf;
            switch (this.ColorFormat)
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

        public ColorPalette GetColorPalette()
        {
            Byte[] paletteData = this.PaletteData;
            Int32 bytespercol = this.BytesPerColor;
            if (this.ColorFormat > 2 || this.BytesPerColor == 0 || paletteData == null)
                return null;
            ColorPalette cp = new Bitmap(Width, Height, GetPixelFormat()).Palette;
            if (bytespercol != 2 && bytespercol != 4)
                return cp;
            Int32 palSize = this.ColorsInPalette;
            if (palSize == 0)
                palSize = (Int32)Math.Pow(2, Image.GetPixelFormatSize(this.GetPixelFormat()));
            for (Int32 i = 0; i < cp.Entries.Length; i++)
            {
                cp.Entries[i] = GetColor(paletteData, i, bytespercol, true);
            }
            return cp;
        }

        /// <summary>
        /// Gets a colour from data
        /// </summary>
        /// <param name="data">The data array</param>
        /// <param name="index">The index of the color. This is NOT the index in the data array; that would be index * length</param>
        /// <param name="colorLength">Length of one color in the data array</param>
        /// <param name="allowTransparency">False to force transaprency to 255</param>
        /// <returns>The color data at that position</returns>
        private static Color GetColor(Byte[] data, Int32 index, Int32 colorLength, Boolean allowTransparency)
        {
            Int32 offset = index * colorLength;
            if (offset >= data.Length)
                return Color.Empty;
            Int32 alpha = 255;
            Int32 blue;
            Int32 green;
            Int32 red;
            switch (colorLength)
            {
                case 2:
                    //8421 ==bin==> 1000010000100001 ==split==> 10000 10000 10000 1 ==dec==> 16 16 16 1 ==x8==> 128 128 128 1
                    Int32 val = (data[offset] << 8) + data[offset + 1];
                    if (allowTransparency)
                        alpha = (val & 1) * 255;
                    blue = ((val >> 1) & 31) * 8;
                    green = ((val >> 6) & 31) * 8;
                    red = ((val >> 11) & 31) * 8;
                    break;
                case 4:
                    //00333333 == a=00, R=33, G=33, B=33
                    if (allowTransparency)
                        alpha = data[offset + 0];
                    blue = data[offset + 1];
                    green = data[offset + 2];
                    red = data[offset + 3];
                    break;
                default:
                    return Color.Empty;
            }
            return Color.FromArgb(alpha, red, green, blue);
        }

        public Bitmap GetBitmap()
        {
            PixelFormat pf = this.GetPixelFormat();
            Int32 stride = Image.GetPixelFormatSize(pf) * this.Width;
            stride = (stride / 8) + ((stride % 8) > 0 ? 1 : 0);
            return ImageUtils.BuildImage(this.ImageData, this.Width, this.Height, stride, pf, this.GetColorPalette());
        }

        private void ReadHeader(Byte[] headerBytes)
        {
            if (headerBytes.Length < 10)
                return;
            this.DataOffset = ArrayUtils.GetBEIntFromByteArray(headerBytes, 0);
            this.PaletteOffset = ArrayUtils.GetBEIntFromByteArray(headerBytes, 4);
            this.Width = ArrayUtils.GetBEShortFromByteArray(headerBytes, 8);
            this.Height = ArrayUtils.GetBEShortFromByteArray(headerBytes, 10);
            this.ReadBytesPerColor = headerBytes[12];
            this.BytesPerColor = this.ReadBytesPerColor;
            this.ColorFormat = headerBytes[13];
            this.ReadColorsInPalette = ArrayUtils.GetBEShortFromByteArray(headerBytes, 14);
        }
    }
}