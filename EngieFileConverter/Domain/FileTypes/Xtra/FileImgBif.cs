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

        public override string IdCode { get { return "15mhBif"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "BIF image"; } }
        public override string[] FileExtensions { get { return new string[] { "bif" }; } }
        public override string LongTypeName { get { return "BIF image file (15 Move Hole)"; } }
        public override int BitsPerPixel { get { return 8; } }
        public override bool NeedsPalette { get { return !this.m_PaletteLoaded; } }
        protected bool m_PaletteLoaded;

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
            if (dataLength < 4)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            int width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            int height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            if (width > short.MaxValue || height > short.MaxValue)
                throw new FileTypeLoadException("Not a " + this.ShortTypeName + ".");
            int imgLength = width * height;
            if (dataLength < imgLength + 4)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            // Only accept if all the rest is 00
            int padding = dataLength - 4 - imgLength;
            if (padding > 0)
                for (int i = imgLength + 4; i < dataLength; ++i)
                    if (fileData[i] != 0)
                        throw new FileTypeLoadException("Not a " + this.ShortTypeName + ".");
            string paletteFilename = Path.GetFileNameWithoutExtension(sourcePath) + ".pal";
            string palettePath = sourcePath == null ? null : Path.Combine(Path.GetDirectoryName(sourcePath), paletteFilename);
            List<string> extraInfo = new List<string>();
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
            byte[] imageData = new byte[imgLength];
            Array.Copy(fileData, 4, imageData, 0, imgLength);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.ExtraInfo = String.Join("\n", extraInfo.ToArray());
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
            byte[] bifData = new byte[imageBytes.Length + 4];
            ArrayUtils.WriteUInt16ToByteArrayLe(bifData, 0, (ushort)width);
            ArrayUtils.WriteUInt16ToByteArrayLe(bifData, 2, (ushort)height);
            Array.Copy(imageBytes, 0, bifData, 4, imageBytes.Length);
            return bifData;
        }

    }

}