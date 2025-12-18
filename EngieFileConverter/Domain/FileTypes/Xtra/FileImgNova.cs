using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.EmotionalPictures;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Image format of Cover Girl Strip Poker.
    /// Uses a 4-byte flag-based RLE compression.
    /// Does not compress repeating sequences of less than 5 bytes.
    /// </summary>
    public class FileImgNova : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Nova image"; } }
        public override String[] FileExtensions { get { return new String[] { "ppp" }; } }
        public override String ShortTypeDescription { get { return "Nova image file"; } }
        public override Int32 ColorsInPalette { get { return this.m_PaletteLoaded ? base.ColorsInPalette : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_PaletteLoaded;
        public Boolean StartPosSeven { get; private set; }
        
        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            if (fileData.Length < 5)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeName + ".");
            // 4th could possibly be mangled by compression, but first 3 can't be.
            // This gives a decent check before going to the heavy step of decompressing the whole thing.
            if (ArrayUtils.ReadIntFromByteArray(fileData, 0, 3, true) != 0x564F4E)
                throw new FileTypeLoadException("Not a " + ShortTypeName + ".");
            // Check 4th value too, in both bare and compressed variant, because, eh, why not.
            if (fileData[3] != 0x41 && (fileData[3] != 0xFF || fileData[4] != 0x41))
                throw new FileTypeLoadException("Not a " + this.ShortTypeName + ".");
            // Decompress flag-based RLE.
            Byte[] fileDataUnc;
            try
            {
                fileDataUnc = PppCompression.DecompressPppRle(fileData);
            }
            catch (ArgumentException)
            {
                throw new FileTypeLoadException("Decompression failed. Not a " + this.ShortTypeName + ".");
            }
            Int32 len = fileDataUnc.Length;
            if (len < 8)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeName + ".");
            // Kinda unnecessary, but whatever: the final check on the "NOVA" string at the start.
            if (ArrayUtils.ReadIntFromByteArray(fileDataUnc, 0, 4, true) != 0x41564F4E)
                throw new FileTypeLoadException("Not a " + ShortTypeName + ".");
            Int32 width = (Int32)ArrayUtils.ReadIntFromByteArray(fileDataUnc, 4, 2, true);
            Int32 height = (Int32)ArrayUtils.ReadIntFromByteArray(fileDataUnc, 6, 2, true);
            String paletteFilename = Path.GetFileNameWithoutExtension(sourcePath) + ".pal";
            String palettePath = sourcePath == null ? null : Path.Combine(Path.GetDirectoryName(sourcePath), paletteFilename);
            if (palettePath != null && File.Exists(palettePath) && new FileInfo(palettePath).Length == 0x300)
            {
                m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPaletteFile(palettePath));
                m_PaletteLoaded = true;
                this.ExtraInfo = "Palette loaded from " + paletteFilename;
            }
            Int32 imageSize = width * height;
            if (imageSize + 8 != len)
                throw new FileTypeLoadException("File size does not match.");
            Byte[] imageData = new Byte[imageSize];
            Array.Copy(fileDataUnc, 8, imageData, 0, imageSize);
            m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, m_Palette, null);
            this.SetFileNames(sourcePath);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("No source data given!");
            if (fileToSave.BitsPerPixel != 8)
                throw new NotSupportedException("This format needs an 8bpp image.");
            Int32 width = fileToSave.Width;
            Int32 height = fileToSave.Height;
            if (width > 0xFFFF || height > 0xFFFF)
                throw new NotSupportedException("The given image is too large.");
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Byte[] novaData = new Byte[8 + imageData.Length];
            ArrayUtils.WriteIntToByteArray(novaData, 0, 4, true, 0x41564F4E);
            ArrayUtils.WriteIntToByteArray(novaData, 4, 2, true, (UInt16)width);
            ArrayUtils.WriteIntToByteArray(novaData, 6, 2, true, (UInt16)height);
            Array.Copy(imageData, 0, novaData, 8, imageData.Length);
            Byte[] compressBuffer = PppCompression.CompressPppRle(novaData);
            return compressBuffer;
        }
    }

}