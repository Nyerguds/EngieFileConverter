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
        public override String LongTypeName { get { return "ImageLine JMX image file"; } }
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
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            Int32 width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x300);
            Int32 height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x302);
            Int32 imgLength = width * height;
            if (dataLength != 0x304 + imgLength)
                throw new FileTypeLoadException("File size does not match header information.");
            try
            {
                this.m_Palette = ColorUtils.ReadSixBitPalette(fileData);
            }
            catch (ArgumentException)
            {
                throw new FileTypeLoadException("Palette data is not 6-bit.");
            }
            Byte[] imageData = new Byte[imgLength];
            Array.Copy(fileData, 0x304, imageData, 0, imgLength);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.SetFileNames(sourcePath);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
            Int32 width = fileToSave.Width;
            Int32 height = fileToSave.Height;
            if (width > 0xFFFF || height > 0xFFFF)
                throw new ArgumentException(ERR_IMAGE_TOO_LARGE, "fileToSave");
            Int32 stride;
            Byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Byte[] jmxData = new Byte[imageBytes.Length + 0x304];
            Byte[] palette = ColorUtils.GetSixBitPaletteData(fileToSave.GetColors());
            Array.Copy(palette, 0, jmxData, 0, palette.Length);
            ArrayUtils.WriteUInt16ToByteArrayLe(jmxData, 0x300, (UInt16)width);
            ArrayUtils.WriteUInt16ToByteArrayLe(jmxData, 0x302, (UInt16)height);
            Array.Copy(imageBytes, 0, jmxData, 0x304, imageBytes.Length);
            return jmxData;
        }

    }

}