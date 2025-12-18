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
        public override String ShortTypeName { get { return "N64ImgGray"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String ShortTypeDescription { get { return "C&C64 paletteless image"; } }

        public override void SetColors(Color[] palette)
        {
            Int32 paletteLength = 1 << this.BitsPerColor;
            Color[] pal = new Color[paletteLength];
            for (Int32 i = 0; i < paletteLength; i++)
            {
                if (i < palette.Length)
                    pal[i] = palette[i];
                else
                    pal[i] = Color.Empty;
            }
            this.m_palette = pal;
            if (m_LoadedImage == null)
                return;
            ColorPalette imagePal = this.m_LoadedImage.Palette;
            Int32 entries = imagePal.Entries.Length;
            for (Int32 i = 0; i < entries; i++)
            {
                if (i < palette.Length)
                    imagePal.Entries[i] = palette[i];
                else
                    imagePal.Entries[i] = Color.Empty;
            }
            this.m_LoadedImage.Palette = imagePal;
        }

        public override void LoadFile(Byte[] fileData)
        {
            base.LoadFile(fileData);
            if (this.m_palette == null || this.ColorsInPalette == this.m_palette.Length)
                throw new FileTypeLoadException("This is not a grayscale paletteless IMG file.");
        }

        public override void LoadFile(String filename)
        {
            base.LoadFile(filename);
            if (this.m_palette == null || this.ColorsInPalette == this.m_palette.Length)
                throw new FileTypeLoadException("This is not a grayscale paletteless IMG file.");
        }

        public void LoadImage(Bitmap img, String displayFileName, String fullFilePath)
        {
            hdrBytesPerColor = 4;
            hdrReadBytesPerColor = 4;
            hdrColorFormat = 1;
            hdrColorsInPalette = 0;
            this.m_palette = PaletteUtils.GenerateGrayPalette(8, false, false);
            this.m_LoadedImage = ImageUtils.ConvertToPalettedGrayscale(img);
            this.LoadedFile = fullFilePath;
            this.LoadedFileName = displayFileName;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            return SaveImg(fileToSave.GetBitmap(), 0, true);
        }
    }
}
