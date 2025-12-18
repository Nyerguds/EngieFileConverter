using Nyerguds.Util;
using System;
using System.Collections.Generic;
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

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            SaveImg(fileToSave.GetBitmap(), savePath, true);
        }
    }
}
