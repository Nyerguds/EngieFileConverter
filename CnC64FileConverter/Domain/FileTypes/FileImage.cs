using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.ImageFile
{
    public class FileImage : N64FileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Image"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Image file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions
        {
            get { return new String[] { "png", "bmp", "gif", "jpg", "jpeg" }; }
        }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] { "Portable Network Graphics", "Bitmap", "CompuServe GIF image", "JPEG. Gods, why would you do that?", "JPEG. No seriously, why?" }; }
        }
        public override Int32 ColorsInPalette { get { return m_ColsInPal; } }
        public override N64FileType PreferredExportType { get { return new FileImgN64(); } }

        protected Int32 m_ColsInPal;

        public FileImage() { }

        public void LoadImage(Bitmap image, Int32 colors)
        {
            m_LoadedImage = image;
            m_ColsInPal = colors;
        }
        
        public override void LoadImage(Byte[] fileData)
        {
            try
            {
                m_LoadedImage = BitmapHandler.LoadBitmap(fileData, out m_ColsInPal);
            }
            catch (Exception ex)
            {
                throw new FileTypeLoadException("Failed to load file as image!", ex);
            }
        }

        public override void LoadImage(String filename)
        {
            try
            {
                m_LoadedImage = BitmapHandler.LoadBitmap(filename, out m_ColsInPal);
                LoadedFileName = filename;
            }
            catch (Exception ex)
            {
                throw new FileTypeLoadException("Failed to load file as image!", ex);
            }
        }

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            ImageUtils.SaveImage(fileToSave.GetBitmap(), savePath);
        }
    }
}
