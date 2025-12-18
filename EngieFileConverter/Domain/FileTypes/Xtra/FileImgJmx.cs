using System;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// ImageLine JMX format (PornTris, Mozaik, PornPipe)
    /// Fairly simple: 6-bit palette, Int16 width, Int16 height, image data.
    /// Identical to BIF format, only with a palette added at the start rather than in a separate file.
    /// </summary>
    public class FileImgJmx : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "ImlJmx"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "ImageLine JMX image"; } }
        public override String[] FileExtensions { get { return new String[] { "jmx" }; } }
        public override String ShortTypeDescription { get { return "ImageLine JMX image file"; } }
        public override Int32 BitsPerPixel { get { return 8; } }

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
            Int32 dataLength = fileData.Length;
            if (dataLength < 0x304)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeName + ".");
            Int32 width = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x300, 2, true);
            Int32 height = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x302, 2, true);
            Int32 imgLength = width * height;
            if (dataLength != 0x304 + imgLength)
                throw new FileTypeLoadException("File size does not match header information.");
            try
            {
                m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(fileData, 0, 256));
            }
            catch (ArgumentException)
            {
                throw new FileTypeLoadException("Palette data is not 6-bit!");
            }
            Byte[] imageData = new Byte[imgLength];
            Array.Copy(fileData, 0x304, imageData, 0, imgLength);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
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
            Byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Byte[] jmxData = new Byte[imageBytes.Length + 0x304];
            Byte[] palette = ColorUtils.GetSixBitPaletteData(ColorUtils.GetSixBitColorPalette(fileToSave.GetColors()));
            Array.Copy(palette, 0, jmxData, 0, palette.Length);
            ArrayUtils.WriteIntToByteArray(jmxData, 0x300, 2, true, (UInt16)width);
            ArrayUtils.WriteIntToByteArray(jmxData, 0x302, 2, true, (UInt16)height);
            Array.Copy(imageBytes, 0, jmxData, 0x304, imageBytes.Length);
            return jmxData;
        }

    }

}