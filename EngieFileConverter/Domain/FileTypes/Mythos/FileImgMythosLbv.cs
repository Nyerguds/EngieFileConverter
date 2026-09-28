using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    /// <summary>
    /// This type is ridiculously simple; it's a raw 320x200 array of 8-bit data, followed by a 6-bit color palette.
    /// The combination of exact file size and the fact the last 0x300 bytes all need to be below 0x40 makes detection
    /// fairly reliable, though, so I'm keeping this in the autodetect logic.
    /// </summary>
    public class FileImgMythosLbv : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override int Width { get { return 320; } }
        public override int Height { get { return 200; } }

        public override string IdCode { get { return "MythLbv"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Mythos LBV Image"; } }
        public override string[] FileExtensions { get { return new string[] { "lbv" }; } }
        public override string LongTypeName { get { return "Mythos LBV Image"; } }
        public override bool NeedsPalette { get { return false; } }
        public override int BitsPerPixel { get{ return 8; } }

        const int imageLen = 320 * 200;
        const int palLen = 3 * 256;

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData)
        {
            int datalen = fileData.Length;
            if (datalen != imageLen + palLen)
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            byte[] imageData = new byte[imageLen];
            Array.Copy(fileData, imageData, imageLen);
            byte[] sixBitPalette = new byte[palLen];
            Array.Copy(fileData, imageLen, sixBitPalette, 0, palLen);

            try
            {
                this.m_Palette = ColorUtils.ReadSixBitPalette(fileData, imageLen);
            }
            catch (ArgumentException arex)
            {
                throw new FileTypeLoadException("Invalid palette.", arex);
            }
            Bitmap image = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.m_LoadedImage = image;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap image = this.PerformPreliminaryChecks(fileToSave);
            byte[] imageData = ImageUtils.GetImageData(image, true);
            byte[] fullData = new byte[imageLen + palLen];
            Array.Copy(imageData, fullData, imageLen);
            byte[] sixBitPalette = ColorUtils.GetSixBitPaletteData(fileToSave.GetColors());
            Array.Copy(sixBitPalette, 0, fullData, imageLen, palLen);
            return fullData;
        }

        private Bitmap PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
            if (image.Width != 320 || image.Height != 200)
                throw new ArgumentException("This format can only save 320×200 images.", "fileToSave");
            return image;
        }

    }
}