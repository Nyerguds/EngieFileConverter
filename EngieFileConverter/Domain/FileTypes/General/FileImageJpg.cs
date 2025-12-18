using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageJpg : FileImage
    {
        public override FileClass FileClass { get { return FileClass.ImageHiCol; } }

        public override String ShortTypeName { get { return "JPEG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "JPEG"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "jpg", "jpeg" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] {this.ShortTypeDescription, this.ShortTypeDescription }; } }
        protected override String MimeType { get { return "jpg"; } }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return new SaveOption[]
            {
                new SaveOption("QUA", SaveOptionType.Number, "Save quality (%)", "1,100", "100"),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            Int32 quality;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "QUA"), out quality);
            quality = Math.Max(1, Math.Min(quality, 100));
            Bitmap image = fileToSave.GetBitmap();
            image = ImageUtils.CloneImage(image);
            using (MemoryStream ms = new MemoryStream())
            {
                // What a mess just to have non-crappy jpeg. Scratch that; jpeg is always crappy.
                ImageCodecInfo jpegEncoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                EncoderParameters encparams = new EncoderParameters(1);
                encparams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                image.Save(ms, jpegEncoder, encparams);
                return ms.ToArray();
            }
        }
    }
}
