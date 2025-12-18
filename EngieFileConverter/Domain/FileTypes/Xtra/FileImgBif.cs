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

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "BIF image"; } }
        public override String[] FileExtensions { get { return new String[] { "bif" }; } }
        public override String ShortTypeDescription { get { return "BIF image file"; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override Int32 ColorsInPalette { get { return this.m_PaletteLoaded ? base.ColorsInPalette : 0; } }
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
                throw new FileTypeLoadException("Too short to be a " + ShortTypeName + ".");
            Int32 width = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            Int32 height = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            Int32 imgLength = width * height;
            if (dataLength < imgLength + 4)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeName + ".");
            // Only accept if all the rest is 00
            Int32 padding = dataLength - 4 - imgLength;
            if (padding > 0)
                for (Int32 i = imgLength + 4; i < dataLength; i++)
                    if (fileData[i] != 0)
                        throw new FileTypeLoadException("Not a " + ShortTypeName + ".");
            String paletteFilename = Path.GetFileNameWithoutExtension(sourcePath) + ".pal";
            String palettePath = sourcePath == null ? null : Path.Combine(Path.GetDirectoryName(sourcePath), paletteFilename);
            List<String> extraInfo = new List<String>();
            if (palettePath != null && File.Exists(palettePath) && new FileInfo(palettePath).Length == 0x300)
            {
                m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPaletteFile(palettePath));
                m_PaletteLoaded = true;
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
            Byte[] bifData = new Byte[imageBytes.Length + 4];
            ArrayUtils.WriteIntToByteArray(bifData, 0, 2, true, (UInt16)width);
            ArrayUtils.WriteIntToByteArray(bifData, 2, 2, true, (UInt16)height);
            Array.Copy(imageBytes, 0, bifData, 4, imageBytes.Length);
            return bifData;
        }

    }

}