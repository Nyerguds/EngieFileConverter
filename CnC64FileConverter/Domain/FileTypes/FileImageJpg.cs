using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.ImageFile
{
    public class FileImageJpg : FileImage
    {
        public override String ShortTypeName { get { return "ImageJPG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription
        {
            get { return "JPEG. Gods, why would you do that?"; }
        }

        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions
        {
            get { return new String[] { "jpg", "jpeg" }; }
        }

        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] { ShortTypeDescription, ShortTypeDescription }; }
        }

    }
}
