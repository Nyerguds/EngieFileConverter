using System;
using System.Drawing;
using Nyerguds.ImageManipulation;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImagePng: FileImage
    {
        public override String ShortTypeName { get { return "PNG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Portable Network Graphics"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "png" }; } }

        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] {this.ShortTypeDescription }; } }

        protected override String MimeType { get { return "png"; } }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Color[] pal = fileToSave.GetColors();
            return BitmapHandler.GetPngImageData(fileToSave.GetBitmap(), pal == null ? 0 : pal.Length);
        }
    }
}
