using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    /// <summary>
    /// This type is ridiculously simple; it's a raw 320x200 array of 8-bit data, followed by a 6-bit colour palette.
    /// The combination of exact file size and the fact the last 0x300 bytes all need to be below 0x40 makes detection
    /// fairly reliable, though, so I'm keeping this in the autodetect logic.
    /// </summary>
    public class FileImgMythosLbv : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 200; } }

        public override String IdCode { get { return "MythLbv"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Mythos LBV Image"; } }
        public override String[] FileExtensions { get { return new String[] { "lbv" }; } }
        public override String ShortTypeDescription { get { return "Mythos LBV Image"; } }
        public override Boolean NeedsPalette { get { return false; } }
        public override Int32 BitsPerPixel { get{ return 8; } }

        const int imageLen = 320 * 200;
        const int palLen = 3 * 256;

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            Int32 datalen = fileData.Length;
            if (datalen != imageLen + palLen)
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            Byte[] imageData = new Byte[imageLen];
            Array.Copy(fileData, imageData, imageLen);
            Byte[] sixBitPalette = new Byte[palLen];
            Array.Copy(fileData, imageLen, sixBitPalette, 0, palLen);

            try
            {
                this.m_Palette = ColorUtils.ReadSixBitPaletteAsEightBit(fileData, imageLen, 256);
            }
            catch (ArgumentException arex)
            {
                throw new FileTypeLoadException("Invalid palette.", arex);
            }
            Bitmap image = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.m_LoadedImage = image;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Bitmap image = this.PerformPreliminaryChecks(fileToSave);
            Byte[] imageData = ImageUtils.GetImageData(image, true);
            Byte[] fullData = new Byte[imageLen + palLen];
            Array.Copy(imageData, fullData, imageLen);
            Byte[] sixBitPalette = ColorUtils.GetSixBitPaletteData(fileToSave.GetColors());
            Array.Copy(sixBitPalette, 0, fullData, imageLen, palLen);
            return fullData;
        }

        private Bitmap PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            Bitmap image;
            if (fileToSave == null || (image = fileToSave.GetBitmap()) == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
            if (image.Width != 320 || image.Height != 200)
                throw new ArgumentException("This format can only save 320×200 images.", "fileToSave");
            return image;
        }

    }
}