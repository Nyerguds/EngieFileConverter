using System;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImageBmp : FileImage
    {
        public override String ShortTypeName { get { return "BMP"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription
        {
            get { return "Bitmap Image"; }
        }

        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions
        {
            get { return new String[] { "bmp" }; }
        }

        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] { ShortTypeDescription }; }
        }

    }
}
