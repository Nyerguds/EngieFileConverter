using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImage : SupportedFileType
    {

        public override FileClass FileClass
        {
            get
            {
                if (this.m_LoadedImage == null)
                    return FileClass.None;
                switch (this.m_LoadedImage.PixelFormat)
                {
                    case PixelFormat.Format1bppIndexed:
                        return FileClass.Image1Bit;
                    case PixelFormat.Format4bppIndexed:
                        return FileClass.Image4Bit;
                    case PixelFormat.Format8bppIndexed:
                        return FileClass.Image8Bit;
                    default:
                        return FileClass.ImageHiCol;
                }
            }
        }
        public override FileClass InputFileClass { get { return FileClass.Image; } }

        public override String IdCode { get { return null; } }
        public override String ShortTypeName { get { return "Image"; } }
        public override String LongTypeName { get { return "Image file"; } }
        public override String[] FileExtensions { get { return new String[] { "png", "bmp", "gif", "jpg", "jpeg" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] { "Portable Network Graphics", "Bitmap", "CompuServe GIF image", "JPEG", "JPEG" }; }
        }

        protected virtual String MimeType { get { return null; } }

        public FileImage() { }

        public void LoadFile(Bitmap image, String filename)
        {
            this.m_LoadedImage = image;
            this.SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            try
            {
                this.m_LoadedImage = ImageUtils.LoadBitmap(fileData);
            }
            catch (Exception ex)
            {
                throw new FileTypeLoadException("Failed to load file as image!", ex);
            }
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            try
            {
                this.CheckSpecificFileType(fileData, filename);
                this.m_LoadedImage = ImageUtils.LoadBitmap(fileData);
                if (PngHandler.IsPng(fileData) && fileData[25] == 0 && fileData[24] == 16)
                    this.ExtraInfo = "Downgraded from 16 bpp grayscale to 8 bpp.";
                this.SetFileNames(filename);
            }
            catch (Exception ex)
            {
                throw new FileTypeLoadException("Failed to load file as image!", ex);
            }
        }

        protected void CheckSpecificFileType(Byte[] fileData, String filename)
        {
            if (this.MimeType == null)
                return;
            String[] mimeType = MimeTypeDetector.GetMimeTypeFromExtension(this.MimeType);
            String[] dataMime = MimeTypeDetector.GetMimeType(fileData, 0);
            if (mimeType == null || dataMime == null || !mimeType[1].Equals(dataMime[1], StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("This is not a " + this.ShortTypeName + " image.");
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // General override in case an Image sub-type has no implementation of this.
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            String filename = "test." + this.FileExtensions[0];
            return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ref filename);
        }
    }
}
