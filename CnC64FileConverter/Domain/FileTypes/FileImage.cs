using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.IO;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImage : SupportedFileType
    {
        public override String ShortTypeName { get { return "Image"; } }
        public override String ShortTypeDescription { get { return "Image file"; } }
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

        public FileImage() { }

        public void LoadFile(Bitmap image, String filename)
        {
            m_LoadedImage = image;
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            String filename = "test." + FileExtensions[0];
            return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ref filename);
        }
    }
}
