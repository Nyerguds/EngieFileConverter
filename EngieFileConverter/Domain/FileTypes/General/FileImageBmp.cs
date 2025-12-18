using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Windows.Graphics2d;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageBmp : FileImage
    {
        public override String ShortTypeName { get { return "Bitmap"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String LongTypeName { get { return "Bitmap Image"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] { this.LongTypeName }; } }
        protected override String MimeType { get { return "bmp"; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public void LoadFromFileData(Byte[] fileData)
        {
            // Implemented manually because it's already in the clipboard code, and GDI+ returns 32bpp for 16bpp bitmaps.
            // General specs: http://www.dragonwins.com/domains/getteched/bmp/bmpfileformat.htm
            Int32 dataLen = fileData.Length;
            if (dataLen < 18 || fileData[0] != 0x42 || fileData[1] != 0x4D)
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            UInt32 size = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0x02);
            UInt32 reserved = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0x06);
            Int32 headerEnd = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0A);
            if (size != dataLen || reserved != 0 || dataLen < headerEnd)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            Int32 headerSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0E);
            String compression = null;
            if (headerEnd < headerSize + 14)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            try
            {
                if (headerSize == 40)
                    this.m_LoadedImage = DibHandler.ImageFromDib(fileData, 14, fileData.Length - 14, headerEnd, true);
                else if (headerSize == 124)
                    this.m_LoadedImage = DibHandler.ImageFromDib5(fileData, 14, fileData.Length - 14, headerEnd, false);
                if (this.m_LoadedImage == null)
                {
                    // Attempt loading through the framework to catch unsupported cases.
                    using (MemoryStream ms = new MemoryStream(fileData))
                    using (Bitmap loadedImage = new Bitmap(ms))
                        this.m_LoadedImage = ImageUtils.CloneImage(loadedImage);
                    if (this.m_LoadedImage != null)
                    {
                        // Success. Now try to figure out what went wrong.
                        if (headerSize == 40)
                        {
                            BITMAPINFOHEADER header = ArrayUtils.ReadStructFromByteArray<BITMAPINFOHEADER>(fileData, 14, Endianness.LittleEndian);
                            if (header.biCompression != BITMAPCOMPRESSION.BI_RGB && header.biCompression != BITMAPCOMPRESSION.BI_BITFIELDS)
                            {
                                compression = header.biCompression.ToString();
                                if (compression.StartsWith("BI_"))
                                    compression = compression.Substring(3);
                            }
                        }
                        else if (headerSize == 124)
                        {
                            BITMAPV5HEADER dib5Hdr = ArrayUtils.ReadStructFromByteArray<BITMAPV5HEADER>(fileData, 14, Endianness.LittleEndian);
                            if (dib5Hdr.bV5Compression != BITMAPCOMPRESSION.BI_RGB && dib5Hdr.bV5Compression != BITMAPCOMPRESSION.BI_BITFIELDS)
                            {
                                compression = dib5Hdr.bV5Compression.ToString();
                                if (compression.StartsWith("BI_"))
                                    compression = compression.Substring(3);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new FileTypeLoadException("Error loading bitmap: " + e);
            }
            if (this.m_LoadedImage == null)
            {
                throw new FileTypeLoadException("Error loading bitmap.");
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
            if (Image.GetPixelFormatSize(this.m_LoadedImage.PixelFormat) == 16)
            {
                sbExtrainfo.Append("\nBits per color: ");
                switch (this.m_LoadedImage.PixelFormat)
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
            if (compression != null)
                sbExtrainfo.Append("\nCompression: ").Append(compression);
            this.ExtraInfo = sbExtrainfo.ToString();
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            try
            {
                return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ImageFormat.Bmp);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
            catch (Exception ex)
            {
                if (ex is FileTypeSaveException)
                    throw;
                throw new FileTypeSaveException(ex.Message, ex);
            }
        }

    }
}
