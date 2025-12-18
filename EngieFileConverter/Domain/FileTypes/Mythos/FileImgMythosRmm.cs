using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    /// <summary>
    /// DO NOT ENABLE THIS! It will match on any file larger than 44928 bytes on which the 768
    /// bytes on the position at 44628 bytes from the end don't have values higher than 64!
    ///
    /// This was only added for easy extraction of the scene images from the rmm format.
    /// </summary>
    public class FileImgMythosRmm : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        private const int _width = 320;
        private const int _height = 138;
        public override Int32 Width { get { return _width; } }
        public override Int32 Height { get { return _height; } }

        public override String IdCode { get { return "MythRmm"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Mythos RMM Image"; } }
        public override String[] FileExtensions { get { return new String[] { "rmm" }; } }
        public override String ShortTypeDescription { get { return "Mythos RMM Scene Image"; } }
        public override Boolean NeedsPalette { get { return false; } }
        public override Int32 BitsPerPixel { get{ return 8; } }
        public override bool CanSave { get { return false; } }

        const int imageLen = _width * _height;
        const int palLen = 0x300;

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            Int32 datalen = fileData.Length;
            Int32 fullLen = imageLen + palLen;
            if (datalen < imageLen + palLen)
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            Byte[] imageData = new Byte[imageLen];
            Array.Copy(fileData, datalen - imageLen, imageData, 0, imageLen);
            try
            {
                this.m_Palette = ColorUtils.ReadSixBitPaletteAsEightBit(fileData, datalen - fullLen, 256);
            }
            catch (ArgumentException arex)
            {
                throw new FileTypeLoadException("Invalid palette.", arex);
            }
            Bitmap image = ImageUtils.BuildImage(imageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.m_LoadedImage = image;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotSupportedException();
        }

    }
}