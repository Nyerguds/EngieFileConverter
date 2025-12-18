using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
            SupportedFileType parentType = fileToSave.FrameParent;
            Boolean mainTypeIndexed = (fileToSave.FileClass & FileClass.ImageIndexed) != 0 || (fileToSave.IsFramesContainer && fileToSave.Frames.Any(f => f != null && (f.FileClass & FileClass.ImageIndexed) != 0));
            Boolean parTypeIndexed = parentType != null && ((parentType.FileClass & FileClass.ImageIndexed) != 0 || (parentType.Frames.Any(f => f != null && (f.FileClass & FileClass.ImageIndexed) != 0)));
            // Does not support indexed graphics; don't show the option at all.
            Boolean hasPaletteTransparency = false;
            Boolean mainTypeHasMask = fileToSave.TransparencyMask != null && fileToSave.TransparencyMask.Any(b => b);
            Boolean parentTypeHasMask = parentType != null && parentType.TransparencyMask != null && parentType.TransparencyMask.Any(b => b);
            if (mainTypeIndexed || parTypeIndexed)
            {
                Color[] pal = fileToSave.GetColors();
                hasPaletteTransparency = mainTypeHasMask || parentTypeHasMask || (pal != null && pal.Any(c => c.A != 255));
            }
            List<SaveOption> opts = new List<SaveOption>();
            if (hasPaletteTransparency)
                opts.Add(new SaveOption("NTP", SaveOptionType.Boolean, "Save indexed graphics without transparency (advised for editing)", mainTypeHasMask || parentTypeHasMask ? "1" : "0"));
            if (fileToSave.GetBitmap() != null && fileToSave.GetBitmap().PixelFormat == PixelFormat.Format4bppIndexed && fileToSave.GetColors().Length <= 4)
                opts.Add(new SaveOption("EXP", SaveOptionType.Boolean, "Expand palette of 4-bit image to 16 colours, to ensure re-saved versions do not get reduced to 2-bit png, so they can still be opened in Engie File Converter (advised for editing)", "1"));
            return opts.ToArray();
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            Color[] pal = fileToSave.GetColors();
            Bitmap bm = fileToSave.GetBitmap();
            Boolean noPalTransparency = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "NTP"));
            Boolean expandPal = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "EXP"))
                && pal != null && pal.Length <= 4 && bm.PixelFormat == PixelFormat.Format4bppIndexed;
            if (expandPal)
            {
                Int32 width = bm.Width;
                Int32 height = bm.Height;
                Int32 stride;
                // I could just modify the palette in the original image and set it back afterwards, but this seems cleaner.
                Byte[] imageData = ImageUtils.GetImageData(bm, out stride);
                using (Bitmap bm16 = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format4bppIndexed, pal, Color.Black))
                    return ImageUtils.GetPngImageData(bm16, 0, noPalTransparency);
            }
            return ImageUtils.GetPngImageData(bm, pal == null ? 0 : pal.Length, noPalTransparency);
        }
    }
}
