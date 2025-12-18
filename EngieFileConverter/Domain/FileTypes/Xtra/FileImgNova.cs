using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using Nyerguds.FileData.EmotionalPictures;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

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

        public override String IdCode { get { return "CgspNova"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Nova image"; } }
        public override String[] FileExtensions { get { return new String[] { "ppp" }; } }
        public override String ShortTypeDescription { get { return "Nova image file"; } }
        public override Boolean NeedsPalette { get { return !this.m_PaletteLoaded; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override SupportedFileType[] Frames { get { return this.m_Frames; } }

        protected Boolean m_PaletteLoaded;
        protected SupportedFileType[] m_Frames;

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
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            // Check if first 3 bytes are "NOV". 4th could possibly be mangled by compression, but first 3 can't be.
            // This gives a decent check before going to the heavy step of decompressing the whole thing.
            if (ArrayUtils.ReadIntFromByteArray(fileData, 0, 3, true) != 0x564F4E)
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
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            // The final check on the "NOVA" string at the start.
            if (ArrayUtils.ReadUInt32FromByteArrayLe(fileDataUnc, 0) != 0x41564F4E)
                throw new FileTypeLoadException("Not a " + this.ShortTypeName + ".");
            Int32 width = ArrayUtils.ReadUInt16FromByteArrayLe(fileDataUnc, 4);
            Int32 height = ArrayUtils.ReadUInt16FromByteArrayLe(fileDataUnc, 6);
            String paletteFilename = Path.GetFileNameWithoutExtension(sourcePath) + ".pal";
            String palettePath = sourcePath == null ? null : Path.Combine(Path.GetDirectoryName(sourcePath), paletteFilename);
            List<String> extraInfo = new List<String>();
            if (palettePath != null && File.Exists(palettePath) && new FileInfo(palettePath).Length == 0x300)
            {
                this.m_Palette = ColorUtils.ReadFromSixBitPaletteFile(palettePath);
                this.m_PaletteLoaded = true;
                extraInfo.Add("Palette loaded from " + paletteFilename);
            }
            Int32 imageSize = width * height;
            if (imageSize + 8 != len)
                throw new FileTypeLoadException("File size does not match.");
            Byte[] imageData = new Byte[imageSize];
            Array.Copy(fileDataUnc, 8, imageData, 0, imageSize);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            Int32 cinemaFrames =  this.CheckForCinemaFrames(sourcePath);
            if (cinemaFrames > 0)
                extraInfo.Add(cinemaFrames + " cinema frames found.");
            this.ExtraInfo = String.Join("\n", extraInfo.ToArray());
            this.SetFileNames(sourcePath);
        }

        private Int32 CheckForCinemaFrames(String sourcePath)
        {
            if (sourcePath == null)
                return 0;
            String path = Path.GetDirectoryName(sourcePath);
            String filename = Path.GetFileName(sourcePath);
            String filenameBase = Path.GetFileNameWithoutExtension(sourcePath);
            Regex nameRegex = new Regex("^([a-z]+)(\\d)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Match m = nameRegex.Match(filenameBase);
            if (!m.Success)
                return 0;
            String baseName = m.Groups[1].Value;
            String baseNum = m.Groups[2].Value;

            List<SupportedFileType> frames = new List<SupportedFileType>();
            Color[] greyPal = PaletteUtils.GenerateGrayPalette(4, null, false);
            for (Int32 i = 1; i <= 9; ++i)
            {
                String cinemaFilename = baseName + "." + baseNum + "_" + i;
                String cinemaFullName = Path.Combine(path, cinemaFilename);
                FileInfo cinemaFrame = new FileInfo(cinemaFullName);
                if (!cinemaFrame.Exists || cinemaFrame.Length != 10240)
                    continue;
                Byte[] imageBytes = File.ReadAllBytes(cinemaFullName);
                Bitmap frameImg = ImageUtils.BuildImage(imageBytes, 160, 128, 80, PixelFormat.Format4bppIndexed, greyPal, null);
                //Headerless grayscale 160x128 4-bit images which show the striptease scenes.
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, frameImg, cinemaFullName, -1);
                framePic.SetBitsPerColor(4);
                framePic.SetFileClass(FileClass.Image4Bit);
                framePic.ExtraInfo = "Cinema frame " + i + " for " + filename;
                frames.Add(framePic);
            }
            if (frames.Count > 0)
                this.m_Frames = frames.ToArray();
            return frames.Count;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
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
            Byte[] imageData = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Byte[] novaData = new Byte[8 + imageData.Length];
            ArrayUtils.WriteInt32ToByteArrayLe(novaData, 0, 0x41564F4E);
            ArrayUtils.WriteInt16ToByteArrayLe(novaData, 4, width);
            ArrayUtils.WriteInt16ToByteArrayLe(novaData, 6, height);
            Array.Copy(imageData, 0, novaData, 8, imageData.Length);
            Byte[] compressBuffer = PppCompression.CompressPppRle(novaData);
            return compressBuffer;
        }
    }

}