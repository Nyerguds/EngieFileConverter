using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageBmp : FileImage
    {
        private readonly String LOAD_ERROR = "Not a bitmap file.";
        public override String ShortTypeName { get { return "Bitmap"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Bitmap Image"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        protected override String MimeType { get { return "bmp"; } }

        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] {this.ShortTypeDescription }; }
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            // Implemented manually because it's already in the clipboard code, and GDI+ returns 32bpp for 16bpp bitmaps.
            // General specs: http://www.dragonwins.com/domains/getteched/bmp/bmpfileformat.htm
            Int32 dataLen = fileData.Length;
            if (dataLen < 18 || fileData[0] != 0x42 || fileData[1] != 0x4D)
                throw new FileTypeLoadException(LOAD_ERROR);
            UInt32 size = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x02, 4, true);
            UInt32 reserved = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x06, 4, true);
            Int32 headerEnd = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x0A, 4, true);
            if (size != dataLen || reserved != 0 || dataLen < headerEnd)
                throw new FileTypeLoadException(LOAD_ERROR);
            Int32 headerSize = (Int32) ArrayUtils.ReadIntFromByteArray(fileData, 0x0E, 4, true);
            if (headerEnd < headerSize + 14)
                throw new FileTypeLoadException(LOAD_ERROR);
            try
            {
                if (headerSize == 40)
                    this.m_LoadedImage = DibHandler.ImageFromDib(fileData, 14, headerEnd);
                else if (headerSize == 124)
                    this.m_LoadedImage = DibHandler.ImageFromDib5(fileData, 14, headerEnd, false);
                else
                {
                    // Attempt loading through the framework
                    using (MemoryStream ms = new MemoryStream(fileData))
                    using (Bitmap loadedImage = new Bitmap(ms))
                        this.m_LoadedImage = ImageUtils.CloneImage(loadedImage);
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading bitmap: " + e);
            }
            StringBuilder sbExtrainfo = new StringBuilder();
            Int32 version = 0;
            switch (headerSize)
            {
                case 40:
                    version = 1;
                    break;
                case 52:
                    version = 2;
                    break;
                case 56:
                    version = 3;
                    break;
                case 108:
                    version = 4;
                    break;
                case 124:
                    version = 5;
                    break;
            }
            if (version != -1)
                sbExtrainfo.Append("Bitmap version: ").Append(version);
            else
                sbExtrainfo.Append("Unknown bitmap version (header size: ").Append(headerSize).Append(")");
            if (Image.GetPixelFormatSize(m_LoadedImage.PixelFormat) == 16)
            {
                sbExtrainfo.Append("\nBits per color: ");
                switch (m_LoadedImage.PixelFormat)
                {
                    case PixelFormat.Format16bppRgb555:
                        sbExtrainfo.Append("R5 G5 B5");
                        break;
                    case PixelFormat.Format16bppArgb1555:
                        sbExtrainfo.Append("A1 R5 G5 B5");
                        break;
                    case PixelFormat.Format16bppRgb565:
                        sbExtrainfo.Append("R5 G6 B5");
                        break;
                }
            }
            this.ExtraInfo = sbExtrainfo.ToString();
            this.SetFileNames(filename);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException(FILE_EMPTY);
            return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ImageFormat.Bmp);
        }

    }
}
