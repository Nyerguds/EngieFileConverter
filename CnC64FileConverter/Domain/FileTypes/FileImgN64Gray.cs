using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.ImageFile
{
    
    public class FileImgN64Gray: FileImgN64
    {
        public override String ShortTypeName { get { return "N64ImgGray"; } }
        public override String[] FileExtensions { get { return new String[] { "img" }; } }
        public override String ShortTypeDescription { get { return "C&C64 paletteless image"; } }


        public override void LoadImage(Byte[] fileData)
        {
            base.LoadImage(fileData);
            if (this.FileHasPalette || this.ColorsInPalette == 0)
                throw new FileTypeLoadException("This is not a grayscale paletteless IMG file.");
        }

        public override void LoadImage(String filename)
        {
            base.LoadImage(filename);
            if (this.FileHasPalette || this.ColorsInPalette == 0)
                throw new FileTypeLoadException("This is not a grayscale paletteless IMG file.");
        }
        
        public void LoadImage(Bitmap img)
        {
            hdrBytesPerColor = 4;
            hdrReadBytesPerColor = 4;
            hdrColorFormat = 1;
            hdrColorsInPalette = 0;
            this.palette = ColorUtils.GenerateGrayPalette(8);
            this.m_LoadedImage = ImageUtils.ConvertToGrayscale(img);
        }

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            SaveImg(fileToSave.GetBitmap(), savePath, true);
        }
    }
}
