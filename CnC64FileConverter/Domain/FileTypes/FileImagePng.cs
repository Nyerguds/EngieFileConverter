using Nyerguds.ImageManipulation;
using System;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImagePng : FileImage
    {
        public override String ShortTypeName { get { return "ImagePNG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription
        {
            get { return "Portable Network Graphics"; }
        }

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

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            BitmapHandler.SaveAsPng(fileToSave.GetBitmap(), savePath, fileToSave.ColorsInPalette);
        }
    }
}
