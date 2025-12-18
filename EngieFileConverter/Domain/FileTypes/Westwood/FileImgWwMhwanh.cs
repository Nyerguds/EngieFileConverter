using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Unsure what this format is, or what any of the data in it s header really means. Found on the Japanese PSX ROM of C&C1
    /// </summary>
    class FileImgWwMhwanh: SupportedFileType
    {
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "HSI Raw"; } }
        public override String[] FileExtensions { get { return new String[] { "raw", "jap" }; } }
        public override String ShortTypeDescription { get { return "ImageAlchemy HSI Raw Format"; } }
        public override FileClass FileClass { get { return _IsHighCol ? FileClass.ImageHiCol : FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.ImageHiCol; } }
        public override Int32 Width { get { return this._Width; } }
        public override Int32 Height { get { return this._Height; } }
        public override Int32 ColorsInPalette { get { return _PaletteSize; } }

        protected Int32 _Version;
        protected Int32 _Width;
        protected Int32 _Height;
        protected Int32 _PaletteSize;
        protected Boolean _IsHighCol;

        public override void LoadFile(Byte[] fileData)
        {
            const Int32 headerSize = 0x20;
            if (fileData.Length < headerSize)
                throw new FileTypeLoadException("File is not long enough.");
            if (!fileData.Take(6).SequenceEqual(Encoding.ASCII.GetBytes("mhwanh")))
                throw new FileTypeLoadException("Header ID does not match.");
            _Version = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x06, 2, false);
            _Width = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x08, 2, false);
            _Height = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x0A, 2, false);
            _PaletteSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x0C, 2, false);
            if (_PaletteSize < 0)
                _PaletteSize = 0;
            _IsHighCol = _PaletteSize == 0;
            Int32 horizonalDpi = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 0x0E, 2, false);
            Int32 verticalDpi = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 0x10, 2, false);
            Int32 gamma = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, 0x12, 2, false);
            Int32 compression = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x14, 2, false);
            if (compression != 0)
                throw new FileTypeLoadException("HSI Raw format with compression is not supported.");
            Boolean hasAlpha = ArrayUtils.ReadIntFromByteArray(fileData, 0x16, 2, false) != 0;
            if (hasAlpha)
                throw new FileTypeLoadException("HSI Raw format with alpha channel is not supported.");
            /*/
            Int32 Reserved1 = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x18, 2, false);
            Int32 Reserved2 = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x1A, 2, false);
            Int32 Reserved3 = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x1C, 2, false);
            Int32 Reserved4 = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0x1E, 2, false);
            //*/
            Int32 palDataLen = _PaletteSize * 3;
            Int32 imgDataLen = _Width * _Height;
            if (_IsHighCol)
                imgDataLen *= 3;
            if (fileData.Length != headerSize + palDataLen + imgDataLen)
                throw new FileTypeLoadException("File length does not match.");
            m_Palette = _IsHighCol ? null : new Color[_PaletteSize];
            Int32 readOffs = headerSize;
            for (Int32 i = 0; i < _PaletteSize; ++i)
            {
                m_Palette[i] = Color.FromArgb(fileData[readOffs], fileData[readOffs + 1], fileData[readOffs + 2]);
                readOffs += 3;
            }
            Byte[] imageData = new Byte[imgDataLen]; 
            imageData = new Byte[imgDataLen];
            Array.Copy(fileData, headerSize + palDataLen, imageData, 0, imgDataLen);
            PixelFormat pf = _IsHighCol ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed;
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, _Width, _Height, _Width, pf, m_Palette, Color.Empty);
            this.m_LoadedImage.Palette = ImageUtils.GetPalette(m_Palette);
            this.ExtraInfo = "Version: " + _Version + Environment.NewLine;
            if (horizonalDpi < 0 && verticalDpi < 0)
                this.ExtraInfo += "Aspect ratio: : " + (-horizonalDpi) + "x" + (-verticalDpi);
            else
                this.ExtraInfo += "Horizontal DPI: " + horizonalDpi
                + Environment.NewLine + "Vertical DPI: " + verticalDpi;
            this.ExtraInfo +=
                  Environment.NewLine + "Gamma: " + gamma;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotImplementedException();
        }
    }
}
