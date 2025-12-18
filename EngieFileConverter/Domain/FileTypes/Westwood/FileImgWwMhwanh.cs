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
    /// Unsure what this format is, or what any of the data in its header really means. Found on the Japanese PSX ROM of C&amp;C1.
    /// </summary>
    class FileImgWwMhwanh: SupportedFileType
    {
        public override String IdCode { get { return "HsiRaw"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "HSI Raw"; } }
        public override String[] FileExtensions { get { return new String[] { "raw", "jap" }; } }
        public override String LongTypeName { get { return "ImageAlchemy HSI Raw Format"; } }
        public override FileClass FileClass { get { return this._IsHighCol ? FileClass.ImageHiCol : FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.ImageHiCol; } }
        public override Int32 Width { get { return this._Width; } }
        public override Int32 Height { get { return this._Height; } }

        public override Boolean CanSave { get { return false; } }

        protected Int32 _Version;
        protected Int32 _Width;
        protected Int32 _Height;
        protected Boolean _IsHighCol;

        public override void LoadFile(Byte[] fileData)
        {
            const Int32 headerSize = 0x20;
            if (fileData.Length < headerSize)
                throw new FileTypeLoadException("File is not long enough.");
            if (!fileData.Take(6).SequenceEqual(Encoding.ASCII.GetBytes("mhwanh")))
                throw new FileTypeLoadException("Header ID does not match.");
            this._Version = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x06);
            this._Width = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x08);
            this._Height = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x0A);
            Int32 paletteSize = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x0C);
            if (paletteSize < 0)
                paletteSize = 0;
            this._IsHighCol = paletteSize == 0;
            Int32 horizonalDpi = ArrayUtils.ReadInt16FromByteArrayBe(fileData, 0x0E);
            Int32 verticalDpi = ArrayUtils.ReadInt16FromByteArrayBe(fileData, 0x10);
            Int32 gamma = ArrayUtils.ReadInt16FromByteArrayBe(fileData, 0x12);
            Int32 compression = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x14);
            if (compression != 0)
                throw new FileTypeLoadException("HSI Raw format with compression is not supported.");
            Boolean hasAlpha = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x16) != 0;
            if (hasAlpha)
                throw new FileTypeLoadException("HSI Raw format with alpha channel is not supported.");
            /*/
            Int32 Reserved1 = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x18);
            Int32 Reserved2 = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x1A);
            Int32 Reserved3 = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x1C);
            Int32 Reserved4 = ArrayUtils.ReadUInt16FromByteArrayBe(fileData, 0x1E);
            //*/
            Int32 palDataLen = paletteSize * 3;
            Int32 imgDataLen = this._Width * this._Height;
            if (this._IsHighCol)
                imgDataLen *= 3;
            if (fileData.Length != headerSize + palDataLen + imgDataLen)
                throw new FileTypeLoadException("File length does not match.");
            this.m_Palette = null;;
            Int32 readOffs = headerSize;
            if (!this._IsHighCol)
            {
                this.m_Palette = new Color[paletteSize];
                for (Int32 i = 0; i < paletteSize; ++i)
                {
                    this.m_Palette[i] = Color.FromArgb(fileData[readOffs], fileData[readOffs + 1], fileData[readOffs + 2]);
                    readOffs += 3;
                }
            }
            Byte[] imageData = new Byte[imgDataLen];
            Array.Copy(fileData, headerSize + palDataLen, imageData, 0, imgDataLen);
            PixelFormat pf = this._IsHighCol ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed;
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, this._Width, this._Height, this._Width, pf, this.m_Palette, Color.Empty);
            this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
            this.ExtraInfo = "Version: " + this._Version + Environment.NewLine;
            if (horizonalDpi < 0 && verticalDpi < 0)
                this.ExtraInfo += "Aspect ratio: " + (-horizonalDpi) + "x" + (-verticalDpi);
            else
                this.ExtraInfo += "Horizontal DPI: " + horizonalDpi + Environment.NewLine + "Vertical DPI: " + verticalDpi;
            this.ExtraInfo += Environment.NewLine + "Gamma: " + gamma;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotSupportedException();
        }
    }
}
