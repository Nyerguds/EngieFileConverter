using Nyerguds.CCTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileImgCps : SupportedFileType
    {
        private static PixelFormatter SixteenBppFormatter = new PixelFormatter(2, 5, 10, 5, 5, 5, 0, 0, 0, true);
        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 200; } }
        protected Color[] m_palette;
        protected Boolean hasPalette;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "CPS"; } }
        public override String[] FileExtensions { get { return new String[] { "cps" }; } }
        public override String ShortTypeDescription { get { return "CPS Image file"; } }
        public override Int32 ColorsInPalette { get { return hasPalette? 256 : 0; } }
        public override Int32 BitsPerColor { get{ return 8; } }

        public FileImgCps() { }
        
        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData);
        }
        
        public override Color[] GetColors()
        {
            // ensures the UI can show the partial palette.
            return m_palette == null ? null : m_palette.ToArray();
        }
        
        public override void SetColors(Color[] palette)
        {
            if (this.m_backupPalette == null)
                this.m_backupPalette = GetColors();
            m_palette = palette;
            base.SetColors(palette);
        }        

        public override Boolean ColorsChanged()
        {
            // assume there's no palette, or no backup was ever made
            if (this.m_backupPalette == null)
                return false;
            return !m_palette.SequenceEqual(this.m_backupPalette);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData);
            SetFileNames(filename);
        }
                
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave)
        {
            return SaveCps(fileToSave.GetBitmap(), fileToSave.GetColors(), fileToSave.ColorsInPalette == 0 && fileToSave.GetColors().Length != 0);
        }


        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 2)
                throw new FileTypeLoadException("File is not long enough to be a valid CPS file.");
            Int32 fileSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            if (fileSize+2 != fileData.Length)
                throw new FileTypeLoadException("File size in header does not match!");
            Int32 four = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            if (four != 4)
                throw new FileTypeLoadException("Invalid values encountered in header.");
            Int32 bufferSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 4, 4, true);
            Int32 paletteLength = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            if (paletteLength > 0)
            {
                if (paletteLength != 0x300)
                    throw new FileTypeLoadException("Invalid palette length in header!");
                Byte[] pal = new Byte[paletteLength];
                Array.Copy(fileData, 10, pal, 0, paletteLength);
                SixBitColor[] palette;
                try
                {
                    palette = ColorUtils.ReadSixBitPalette(pal);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + ex.Message, ex);
                }
                catch (NotSupportedException ex2)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + ex2.Message, ex2);
                }
                m_palette = ColorUtils.GetEightBitColorPalette(palette);
                this.hasPalette = true;
            }
            if (this.m_palette == null)
                this.m_palette = this.m_palette = PaletteUtils.GenerateGrayPalette(this.BitsPerColor, false, false);
            Byte[] imageData = new Byte[bufferSize];
            Int32 dataOffset = 10 + paletteLength;
            try
            {
                CHRONOLIB.Compression.WWCompression.LcwUncompress(fileData, ref dataOffset, imageData);
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message);
            }
            try
            {
                //Int32 stride = ImageUtils.GetMinimumStride(this.Width, Image.GetPixelFormatSize(pf));
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, m_palette, null);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        protected static Byte[] SaveCps(Bitmap image, Color[] palette, Boolean asNoPalGray)
        {
            if (image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320x200 images can be saved as CPS!");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            // Removes any stride
            if (stride != image.Width)
                imageData = ImageUtils.ConvertTo8Bit(imageData, image.Width, image.Height, 0, 8, false, ref stride);
            Byte[] compressedData = CHRONOLIB.Compression.WWCompression.LcwCompress(imageData);
            Int32 dataLength = 10 + compressedData.Length;
            if (!asNoPalGray)
                dataLength += 0x300;
            Byte[] fullData = new Byte[dataLength];
            ArrayUtils.WriteIntToByteArray(fullData, 0, 2, true, (UInt32)(dataLength - 2));
            ArrayUtils.WriteIntToByteArray(fullData, 2, 2, true, (UInt32)4);
            ArrayUtils.WriteIntToByteArray(fullData, 4, 4, true, (UInt32)imageData.Length);
            ArrayUtils.WriteIntToByteArray(fullData, 8, 2, true, (UInt32)(asNoPalGray ? 0 : 0x300));
            Int32 offset = 10;
            if (!asNoPalGray)
            {
                if (palette.Length != 256)
                {
                    Color[] pal = Enumerable.Repeat(Color.Black, 256).ToArray();
                    Array.Copy(palette, 0, pal,0, Math.Min(palette.Length, 256));
                    palette = pal;
                }
                SixBitColor[] sixbitPal = ColorUtils.GetSixBitColorPalette(palette);
                Byte[] palData = ColorUtils.GetSixBitPaletteData(sixbitPal);
                Array.Copy(palData, 0, fullData, offset, palData.Length);
                offset += palData.Length;
            }
            Array.Copy(compressedData, 0, fullData, offset, compressedData.Length);
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
    }
}