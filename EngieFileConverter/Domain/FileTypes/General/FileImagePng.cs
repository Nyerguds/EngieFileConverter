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
        public override string ShortTypeName { get { return "PNG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return "Portable Network Graphics"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions { get { return new string[] { "png" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override string[] DescriptionsForExtensions { get { return new string[] {this.LongTypeName }; } }
        protected override string MimeType { get { return "png"; } }
        
        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public void LoadFromFileData(byte[] fileData)
        {
            if (!PngHandler.IsPng(fileData))
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            base.LoadFile(fileData);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            SupportedFileType parentType = fileToSave.FrameParent;
            bool mainTypeIndexed = (fileToSave.FileClass & FileClass.ImageIndexed) != 0 || (fileToSave.IsFramesContainer && fileToSave.Frames.Any(f => f != null && (f.FileClass & FileClass.ImageIndexed) != 0));
            bool parTypeIndexed = parentType != null && ((parentType.FileClass & FileClass.ImageIndexed) != 0 || (parentType.Frames.Any(f => f != null && (f.FileClass & FileClass.ImageIndexed) != 0)));
            // Does not support indexed graphics; don't show the option at all.
            bool hasPaletteTransparency = false;
            bool mainTypeHasMask = fileToSave.TransparencyMask != null && fileToSave.TransparencyMask.Any(b => b);
            bool parentTypeHasMask = parentType != null && parentType.TransparencyMask != null && parentType.TransparencyMask.Any(b => b);
            if (mainTypeIndexed || parTypeIndexed)
            {
                Color[] pal = fileToSave.GetColors();
                hasPaletteTransparency = mainTypeHasMask || parentTypeHasMask || (pal != null && pal.Any(c => c.A != 255));
            }
            List<Option> opts = new List<Option>();
            if (hasPaletteTransparency)
                opts.Add(new Option("NTP", OptionInputType.Boolean, "Save indexed graphics without transparency (advised for editing)", mainTypeHasMask || parentTypeHasMask ? "1" : "0"));
            if (fileToSave.GetBitmap() != null && fileToSave.GetBitmap().PixelFormat == PixelFormat.Format4bppIndexed && fileToSave.GetColors().Length <= 4)
                opts.Add(new Option("EXP", OptionInputType.Boolean, "Expand palette of 4-bit image to 16 colors, to ensure re-saved versions do not get reduced to 2-bit png, so they can still be opened in Engie File Converter (advised for editing)", "1"));
            return opts.ToArray();
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            Color[] pal = fileToSave.GetColors();
            Bitmap bm = fileToSave.GetBitmap();
            bool noPalTransparency = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "NTP"));
            bool expandPal = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "EXP"))
                && pal != null && pal.Length <= 4 && bm.PixelFormat == PixelFormat.Format4bppIndexed;
            if (expandPal)
            {
                int width = bm.Width;
                int height = bm.Height;
                int stride;
                // I could just modify the palette in the original image and set it back afterwards, but this seems cleaner.
                byte[] imageData = ImageUtils.GetImageData(bm, out stride);
                using (Bitmap bm16 = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format4bppIndexed, pal, Color.Black))
                    return ImageUtils.GetPngImageData(bm16, 0, noPalTransparency);
            }
            return ImageUtils.GetPngImageData(bm, pal == null ? 0 : pal.Length, noPalTransparency);
        }
    }
}
