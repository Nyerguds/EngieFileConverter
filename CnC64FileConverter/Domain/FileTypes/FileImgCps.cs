using Nyerguds.GameData.Westwood;
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

    public class FileImgCps0: FileImgCps
    {
        protected override Int32 CompressionType { get { return 0; } }
        protected override String Compressiondesc { get { return "Uncompressed"; } }
        protected override Boolean InternalColors { get { return false; } }
    }
    public class FileImgCps0c : FileImgCps
    {
        protected override Int32 CompressionType { get { return 0; } }
        protected override String Compressiondesc { get { return "Uncompressed - PAL"; } }
        protected override Boolean InternalColors { get { return true; } }
    }
    public class FileImgCps1 : FileImgCps
    {
        protected override Int32 CompressionType { get { return 1; } }
        protected override String Compressiondesc { get { return "LZW12"; } }
        protected override Boolean InternalColors { get { return false; } }
    }
    public class FileImgCps1c : FileImgCps
    {
        protected override Int32 CompressionType { get { return 1; } }
        protected override String Compressiondesc { get { return "LZW12 - PAL"; } }
        protected override Boolean InternalColors { get { return true; } }
    }
    public class FileImgCps2 : FileImgCps
    {
        protected override Int32 CompressionType { get { return 2; } }
        protected override String Compressiondesc { get { return "LZW14"; } }
        protected override Boolean InternalColors { get { return false; } }
    }
    public class FileImgCps2c : FileImgCps
    {
        protected override Int32 CompressionType { get { return 2; } }
        protected override String Compressiondesc { get { return "LZW14 - PAL"; } }
        protected override Boolean InternalColors { get { return true; } }
    }
    public class FileImgCps3 : FileImgCps
    {
        protected override Int32 CompressionType { get { return 3; } }
        protected override String Compressiondesc { get { return "CMV/RLE"; } }
        protected override Boolean InternalColors { get { return false; } }
    }
    public class FileImgCps3c : FileImgCps
    {
        protected override Int32 CompressionType { get { return 3; } }
        protected override String Compressiondesc { get { return "CMV/RLE - PAL"; } }
        protected override Boolean InternalColors { get { return true; } }
    }
    public class FileImgCps4 : FileImgCps
    {
        protected override Int32 CompressionType { get { return 4; } }
        protected override String Compressiondesc { get { return "LCW"; } }
        protected override Boolean InternalColors { get { return false; } }
    }
    public class FileImgCps4c : FileImgCps
    {
        protected override Int32 CompressionType { get { return 4; } }
        protected override String Compressiondesc { get { return "LCW - PAL"; } }
        protected override Boolean InternalColors { get { return true; } }
    }

    public class FileImgCps : SupportedFileType
    {
        private static PixelFormatter SixteenBppFormatter = new PixelFormatter(2, 5, 10, 5, 5, 5, 0, 0, 0, true);
        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 200; } }
        protected Boolean hasPalette;
        protected Int32 compressionType = -1;
        protected virtual Int32 CompressionType { get { return compressionType; } }
        protected virtual String Compressiondesc { get { return "Unspecified"; } }
        protected virtual Boolean InternalColors { get { return hasPalette; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "CPS"; } }
        public override String[] FileExtensions { get { return new String[] { "cps" }; } }
        public override String ShortTypeDescription { get { return "CPS Image file (" + this.Compressiondesc + ")"; } }
        public override Int32 ColorsInPalette { get { return hasPalette? 256 : 0; } }
        public override Int32 BitsPerColor { get{ return 8; } }


        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData, this.CompressionType);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData, this.CompressionType);
            SetFileNames(filename);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            return SaveCps(fileToSave.GetBitmap(), fileToSave.GetColors(), !this.InternalColors, this.CompressionType);
        }

        protected void LoadFromFileData(Byte[] fileData, Int32 comprType)
        {
            if (fileData.Length < 10)
                throw new FileTypeLoadException("File is not long enough to be a valid CPS file.");
            Int32 fileSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            Int32 compression = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            if (comprType != -1 && comprType != compression)
                throw new FileTypeLoadException("Not a CPS with compression " + comprType);


            if (!((compression != 0 || compression != 4) && fileSize == fileData.Length) && !((compression == 0 || compression == 4) && fileSize + 2 == fileData.Length))
                throw new FileTypeLoadException("File size in header does not match!");
            Int32 bufferSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 4, 4, true);
            Int32 paletteLength = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            if (paletteLength > 0)
            {
                hasPalette = true;
                if (!InternalColors)
                    throw new FileTypeLoadException("Not a CPS without internal palette.");
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
                m_Palette = ColorUtils.GetEightBitColorPalette(palette);
                this.hasPalette = true;
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerColor, false, false);
            Byte[] imageData = new Byte[bufferSize];
            Int32 dataOffset = 10 + paletteLength;
            try
            {
                switch (compression)
                {
                    case 0:
                        Array.Copy(fileData, dataOffset, imageData, 0, bufferSize);
                        break;
                    case 1:
                    case 2:
                        throw new NotImplementedException("Compression types 1 and 2 are not yet supported!");
                    case 3:
                        Nyerguds.GameData.Westwood.WWCompression.RleDecode(fileData, ref dataOffset, null, imageData, false);
                        break;
                    case 4:
                        Nyerguds.GameData.Westwood.WWCompression.LcwUncompress(fileData, ref dataOffset, imageData);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported compression format, " + compression);
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message);
            }
            try
            {
                //Int32 stride = ImageUtils.GetMinimumStride(this.Width, Image.GetPixelFormatSize(pf));
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, m_Palette, null);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        public static Byte[] SaveCps(Bitmap image, Color[] palette, Boolean asNoPalGray, Int32 compressionType)
        {
            if (image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320x200 images can be saved as CPS!");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride);
            // Removes any stride
            if (stride != image.Width)
                imageData = ImageUtils.ConvertTo8Bit(imageData, image.Width, image.Height, 0, 8, false, ref stride);

            Byte[] compressedData;
            switch (compressionType)
            {
                case 0:
                    compressedData = imageData;
                    break;
                case 1:
                case 2:
                    throw new NotImplementedException("Compression types 1 and 2 are not yet supported!");
                case 3:
                    compressedData = Nyerguds.GameData.Westwood.WWCompression.RleEncode(imageData);
                    break;
                case 4:
                    compressedData = Nyerguds.GameData.Westwood.WWCompression.LcwCompress(imageData);
                    break;
                default:
                    throw new NotSupportedException("Unknown compression type given.");
            }
            Int32 dataLength = 10 + compressedData.Length;
            if (!asNoPalGray)
                dataLength += 0x300;
            Byte[] fullData = new Byte[dataLength];
            ArrayUtils.WriteIntToByteArray(fullData, 0, 2, true, (UInt32)(dataLength - (compressionType == 0 || compressionType == 4 ? 2 : 0)));
            ArrayUtils.WriteIntToByteArray(fullData, 2, 2, true, (UInt32)compressionType);
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