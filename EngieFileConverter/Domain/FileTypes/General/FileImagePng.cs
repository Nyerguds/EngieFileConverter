using System;
using System.Drawing;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.Linq;

namespace EngieFileConverter.Domain.FileTypes
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

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            SupportedFileType parType = fileToSave.FrameParent;
            Boolean mainTypeIndexed = (fileToSave.FileClass & FileClass.ImageIndexed) != 0 || (fileToSave.IsFramesContainer && (fileToSave.FrameInputFileClass & FileClass.ImageIndexed) != 0);
            Boolean parTypeIndexed = parType != null && ((parType.FileClass & FileClass.ImageIndexed) != 0 || (parType.FrameInputFileClass & FileClass.ImageIndexed) != 0);
            // Does not support indexed graphics; don't show the option at all.
            if (!mainTypeIndexed && !parTypeIndexed)
                return new SaveOption[0];            
            Boolean mainTypeHasMask = fileToSave.TransparencyMask != null && fileToSave.TransparencyMask.Any(b => b);
            Boolean parTypeHasMask = parType != null && parType.TransparencyMask != null && parType.TransparencyMask.Any(b => b);
            Color[] pal = fileToSave.GetColors();
            if (!mainTypeHasMask && !parTypeHasMask && pal != null && pal.All(c => c.A == 255))
                return new SaveOption[0];
            // Default to true if the type has a specific forced transparent index; this kind of transparency is usually best removed for editing.
            return new SaveOption[]
            {
                new SaveOption("NTP", SaveOptionType.Boolean, "Save indexed graphics without transparency (advised for editing)", mainTypeHasMask || parTypeHasMask ? "1" : "0")
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Color[] pal = fileToSave.GetColors();
            Boolean noPalTransparency = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "NTP"));
            return BitmapHandler.GetPngImageData(fileToSave.GetBitmap(), pal == null ? 0 : pal.Length, noPalTransparency);
        }
    }
}
