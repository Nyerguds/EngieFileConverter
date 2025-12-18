using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.IO;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// 15 Move Hole files. Very simple format; int16 for width and height, and then the 8-bit image data.
    /// The end can be padded with zeroes.
    /// </summary>
    public class FileImgBif : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "15mhBif"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "BIF image"; } }
        public override String[] FileExtensions { get { return new String[] { "bif" }; } }
        public override String LongTypeName { get { return "BIF image file (15 Move Hole)"; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override Boolean NeedsPalette { get { return !this.m_PaletteLoaded; } }
        protected Boolean m_PaletteLoaded;

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
            if (dataLength < 4)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            Int32 width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            Int32 height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            if (width > Int16.MaxValue || height > Int16.MaxValue)
                throw new FileTypeLoadException("Not a " + this.ShortTypeName + ".");
            Int32 imgLength = width * height;
            if (dataLength < imgLength + 4)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            // Only accept if all the rest is 00
            Int32 padding = dataLength - 4 - imgLength;
            if (padding > 0)
                for (Int32 i = imgLength + 4; i < dataLength; ++i)
                    if (fileData[i] != 0)
                        throw new FileTypeLoadException("Not a " + this.ShortTypeName + ".");
            String paletteFilename = Path.GetFileNameWithoutExtension(sourcePath) + ".pal";
            String palettePath = sourcePath == null ? null : Path.Combine(Path.GetDirectoryName(sourcePath), paletteFilename);
            List<String> extraInfo = new List<String>();
            this.m_PaletteLoaded = false;
            if (palettePath != null && File.Exists(palettePath) && new FileInfo(palettePath).Length == 0x300)
            {
                try
                {
                    this.m_Palette = ColorUtils.ReadSixBitPaletteFile(palettePath, true);
                    this.m_PaletteLoaded = true;
                }
                catch (ArgumentException) { }
                if (this.m_PaletteLoaded)
                    extraInfo.Add("Palette loaded from " + paletteFilename);
            }
            if (padding > 0)
                extraInfo.Add("End padding: " + padding + " bytes");
            Byte[] imageData = new Byte[imgLength];
            Array.Copy(fileData, 4, imageData, 0, imgLength);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.ExtraInfo = String.Join("\n", extraInfo.ToArray());
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
            Byte[] bifData = new Byte[imageBytes.Length + 4];
            ArrayUtils.WriteUInt16ToByteArrayLe(bifData, 0, (UInt16)width);
            ArrayUtils.WriteUInt16ToByteArrayLe(bifData, 2, (UInt16)height);
            Array.Copy(imageBytes, 0, bifData, 4, imageBytes.Length);
            return bifData;
        }

    }

}