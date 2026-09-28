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

        public override string IdCode { get { return "ImlJmx"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "ImageLine JMX image"; } }
        public override string[] FileExtensions { get { return new string[] { "jmx" }; } }
        public override string LongTypeName { get { return "ImageLine JMX image file"; } }
        public override int BitsPerPixel { get { return 8; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            int dataLength = fileData.Length;
            if (dataLength < 0x304)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            int width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x300);
            int height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x302);
            int imgLength = width * height;
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
            byte[] imageData = new byte[imgLength];
            Array.Copy(fileData, 0x304, imageData, 0, imgLength);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.SetFileNames(sourcePath);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
            int width = fileToSave.Width;
            int height = fileToSave.Height;
            if (width > 0xFFFF || height > 0xFFFF)
                throw new ArgumentException(ERR_DIMENSIONS_TOO_LARGE, "fileToSave");
            int stride;
            byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            byte[] jmxData = new byte[imageBytes.Length + 0x304];
            byte[] palette = ColorUtils.GetSixBitPaletteData(fileToSave.GetColors());
            Array.Copy(palette, 0, jmxData, 0, palette.Length);
            ArrayUtils.WriteUInt16ToByteArrayLe(jmxData, 0x300, (ushort)width);
            ArrayUtils.WriteUInt16ToByteArrayLe(jmxData, 0x302, (ushort)height);
            Array.Copy(imageBytes, 0, jmxData, 0x304, imageBytes.Length);
            return jmxData;
        }

    }

}