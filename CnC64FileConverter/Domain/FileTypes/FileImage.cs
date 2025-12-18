using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.IO;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImage : SupportedFileType
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
        public override SupportedFileType PreferredExportType { get { return new FileImagePng(); } }

        protected Int32 m_ColsInPal;

        public FileImage() { }

        public void LoadFile(Bitmap image, Int32 colors, String filename)
        {
            m_LoadedImage = image;
            m_ColsInPal = colors;
            SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            try
            {
                m_LoadedImage = BitmapHandler.LoadBitmap(fileData);
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
                m_LoadedImage = BitmapHandler.LoadBitmap(filename);
                SetFileNames(filename);
            }
            catch (Exception ex)
            {
                throw new FileTypeLoadException("Failed to load file as image!", ex);
            }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            String filename = "test." + FileExtensions[0];
            return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ref filename);
        }
    }
}
