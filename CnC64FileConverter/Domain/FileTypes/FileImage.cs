using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;

namespace CnC64FileConverter.Domain.FileTypes
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

        public override Color[] GetColors()
        {
            if (m_LoadedImage == null)
                return new Color[0];
            Color[] col1 = m_LoadedImage.Palette.Entries;
            Color[] col2 = new Color[m_ColsInPal];
            Array.Copy(col1, col2, Math.Min(col1.Length, ColorsInPalette));
            return col2;
        }

        protected Int32 m_ColsInPal;

        public FileImage() { }

        public void LoadImage(Bitmap image, Int32 colors, String displayfilename)
        {
            m_LoadedImage = image;
            m_ColsInPal = colors;
            LoadedFileName = displayfilename;
        }
        
        public override void LoadFile(Byte[] fileData)
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

        public override void LoadFile(String filename)
        {
            try
            {
                m_LoadedImage = BitmapHandler.LoadBitmap(filename, out m_ColsInPal);
                 SetFileNames(filename);
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
