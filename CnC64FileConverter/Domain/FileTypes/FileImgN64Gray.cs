using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileImgN64Gray: FileImgN64
    {
        public override String ShortTypeName { get { return "C&C64 IMG Gray"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String ShortTypeDescription { get { return "C&C N64 paletteless image"; } }

        public override void LoadFile(Byte[] fileData)
        {
            base.LoadFile(fileData);
            if (this.m_Palette == null || this.ColorsInPalette == this.m_Palette.Length)
                throw new FileTypeLoadException("This is not a grayscale paletteless IMG file.");
        }

        public override void LoadFile(String filename)
        {
            base.LoadFile(filename);
            if (this.m_Palette == null || this.ColorsInPalette == this.m_Palette.Length)
                throw new FileTypeLoadException("This is not a grayscale paletteless IMG file.");
        }

        public void LoadImage(Bitmap img, String displayFileName, String fullFilePath)
        {
            hdrBytesPerColor = 4;
            hdrReadBytesPerColor = 4;
            hdrColorFormat = 1;
            hdrColorsInPalette = 0;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            this.m_LoadedImage = ImageUtils.ConvertToPalettedGrayscale(img);
            this.LoadedFile = fullFilePath;
            this.LoadedFileName = displayFileName;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            return SaveImg(fileToSave.GetBitmap(), 0, true);
        }
    }
}
