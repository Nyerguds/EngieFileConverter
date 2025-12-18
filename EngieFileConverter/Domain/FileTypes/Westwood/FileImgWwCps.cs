using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.GameData.Compression;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgWwCps : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width = 320;
        protected Int32 m_Height = 200;
        protected Boolean hasPalette;
        protected Int32 CompressionType { get; set; }
        protected String[] compressionTypes = new String[] {"No compression", "LZW-12", "LZW-14", "RLE", "LCW"};

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood CPS"; } }
        public override String[] FileExtensions { get { return new String[] {"cps"}; } }
        public override String ShortTypeDescription { get { return "Westwood CPS File"; } }
        public override Int32 ColorsInPalette { get { return this.hasPalette ? 256 : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < 10)
                throw new FileTypeLoadException("File is not long enough to be a valid CPS file.");
            Int32 fileSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            Int32 compression = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            if (compression < 0 || compression > 4)
                throw new FileTypeLoadException("Unknown compression type " + compression);

            this.ExtraInfo = "Compression: " + this.compressionTypes[compression];
            if (!((compression != 0 || compression != 4) && fileSize == fileData.Length) && !((compression == 0 || compression == 4) && fileSize + 2 == fileData.Length))
                throw new FileTypeLoadException("File size in header does not match!");
            Int32 bufferSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 4, 4, true);
            Int32 paletteLength = (Int16) ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            if (paletteLength > 0)
            {
                if (paletteLength != 0x300)
                    throw new FileTypeLoadException("Invalid palette length in header!");
                Byte[] pal = new Byte[paletteLength];
                Array.Copy(fileData, 10, pal, 0, paletteLength);
                ColorSixBit[] palette;
                try
                {
                    palette = ColorUtils.ReadSixBitPaletteFile(pal);
                }
                catch (ArgumentException ex)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + ex.Message, ex);
                }
                catch (NotSupportedException ex2)
                {
                    throw new FileTypeLoadException("Could not load CPS palette: " + ex2.Message, ex2);
                }
                this.m_Palette = ColorUtils.GetEightBitColorPalette(palette);
                this.hasPalette = true;
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(this.BitsPerPixel, null, false);
            Byte[] imageData;
            Int32 dataOffset = 10 + paletteLength;
            this.CompressionType = compression;
            try
            {
                switch (compression)
                {
                    case 0:
                        imageData = new Byte[bufferSize];
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
                        imageData = WestwoodRle.RleDecode(fileData, (UInt32) dataOffset, null, bufferSize, false, true);
                        break;
                    case 4:
                        imageData = new Byte[bufferSize];
                        WWCompression.LcwDecompress(fileData, ref dataOffset, imageData, 0);
                        break;
                    default:
                        throw new FileTypeLoadException("Unsupported compression format, " + compression);
                }
                if (imageData == null)
                    throw new FileTypeLoadException("Error decompressing image.");
                this.ExtraInfo = "Compression: " + this.compressionTypes[this.CompressionType] + "\nIncludes palette: " + (this.hasPalette ? "yes" : "no");
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading image data: " + e.Message, e);
            }
            try
            {
                this.m_LoadedImage = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            }
            catch (IndexOutOfRangeException)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!");
            }
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = fileToSave != null && !(fileToSave is FileImage) && fileToSave.ColorsInPalette != 0;
            Int32 compression = (fileToSave is FileImgWwCps) ? ((FileImgWwCps)fileToSave).CompressionType : 4;
            return new SaveOption[]
            {
                new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", (hasColors ? 1 : 0).ToString()),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Boolean asPaletted = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            return SaveCps(fileToSave.GetBitmap(), fileToSave.GetColors(), asPaletted, compressionType);
        }
        public static Byte[] SaveCps(Bitmap image, Color[] palette, Boolean withPalette, Int32 compressionType)
        {
            if (image.Width != 320 || image.Height != 200 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 320x200 images can be saved as CPS!");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(image, out stride, true);
            Byte[] compressedData;
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
                    compressedData = WestwoodRle.RleEncode(imageData);
                    break;
                case 4:
                    compressedData = WWCompression.LcwCompress(imageData);
                    break;
                default:
                    throw new NotSupportedException("Unknown compression type given.");
            }
            Int32 dataLength = 10 + compressedData.Length;
            if (withPalette)
                dataLength += 0x300;
            Byte[] fullData = new Byte[dataLength];
            ArrayUtils.WriteIntToByteArray(fullData, 0, 2, true, (UInt32) (dataLength - (compressionType == 0 || compressionType == 4 ? 2 : 0)));
            ArrayUtils.WriteIntToByteArray(fullData, 2, 2, true, (UInt32) compressionType);
            ArrayUtils.WriteIntToByteArray(fullData, 4, 4, true, (UInt32) imageData.Length);
            ArrayUtils.WriteIntToByteArray(fullData, 8, 2, true, (UInt32) (withPalette ? 0x300 : 0));
            Int32 offset = 10;
            if (withPalette)
            {
                if (palette.Length != 256)
                {
                    Color[] pal = Enumerable.Repeat(Color.Black, 256).ToArray();
                    Array.Copy(palette, 0, pal, 0, Math.Min(palette.Length, 256));
                    palette = pal;
                }
                ColorSixBit[] sixbitPal = ColorUtils.GetSixBitColorPalette(palette);
                Byte[] palData = ColorUtils.GetSixBitPaletteData(sixbitPal);
                Array.Copy(palData, 0, fullData, offset, palData.Length);
                offset += palData.Length;
            }
            Array.Copy(compressedData, 0, fullData, offset, compressedData.Length);
            return fullData;
        }
    }
}