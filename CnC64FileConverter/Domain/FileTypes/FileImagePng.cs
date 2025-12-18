using Nyerguds.ImageManipulation;
using System;
using System.Drawing;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImagePng : FileImage
    {
        public override String ShortTypeName { get { return "PNG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription
        {
            get { return "Portable Network Graphics"; }
        }
        public override SupportedFileType PreferredExportType { get { return new FileImgN64(); } }

        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions
        {
            get { return new String[] { "png" }; }
        }

        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] { ShortTypeDescription }; }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Color[] pal = fileToSave.GetColors();
            return BitmapHandler.GetPngImageData(fileToSave.GetBitmap(), pal == null ? 0 : pal.Length);
        }
    }
}
