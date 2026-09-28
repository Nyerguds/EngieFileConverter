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
        public override string ShortTypeName { get { return "JPEG"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return "JPEG"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions { get { return new string[] { "jpg", "jpeg" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override string[] DescriptionsForExtensions { get { return new string[] {this.LongTypeName, this.LongTypeName }; } }
        protected override string MimeType { get { return "jpg"; } }
        
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
            // Quick header identifying check
            int dataLen = fileData.Length;
            if (dataLen < 11 || fileData[0] != 0xFF || fileData[1] != 0xD8 || fileData[3] != 0xFF
                || fileData[6] != 0x4A || fileData[7] != 0x46 || fileData[8] != 0x49 || fileData[9] != 0x46 || fileData[10] != 0x00)
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            base.LoadFile(fileData);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            return new Option[]
            {
                new Option("QUA", OptionInputType.Number, "Save quality (%)", "1,100", "100"),
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            int quality;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "QUA"), out quality);
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
