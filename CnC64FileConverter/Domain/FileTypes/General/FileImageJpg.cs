using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Nyerguds.ImageManipulation;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImageJpg : FileImage
    {
        public override FileClass FileClass { get { return FileClass.ImageHiCol; } }

        public override String ShortTypeName { get { return "JPEG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription
        {
            get { return "JPEG"; }
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

        protected override void CheckSpecificFileType(String filename)
        {
            this.CheckSpecificFileType(filename, "jpg");
        }

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
                throw new NotSupportedException("File to save is empty!");
            Int32 quality;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "QUA"), out quality);
            quality = Math.Max(1, Math.Min(quality, 100));
            Bitmap image = fileToSave.GetBitmap();
            image = ImageUtils.CloneImage(image);
            using (MemoryStream ms = new MemoryStream())
            {
                // What a mess just to have non-crappy jpeg. Scratch that; jpeg is always crappy.
                ImageCodecInfo jpegEncoder = null;
                Guid formatId = ImageFormat.Jpeg.Guid;
                foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
                {
                    if (codec.FormatID == formatId)
                    {
                        jpegEncoder = codec;
                        break;
                    }
                }
                System.Drawing.Imaging.Encoder qualityEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters encparams = new EncoderParameters(1);
                encparams.Param[0] = new EncoderParameter(qualityEncoder, quality);
                image.Save(ms, jpegEncoder, encparams);
                return ms.ToArray();
            }
        }
    }
}
