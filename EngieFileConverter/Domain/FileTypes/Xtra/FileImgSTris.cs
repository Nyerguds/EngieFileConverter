using System;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Sextris files. Not sure about the format... it seems incomplete.
    /// </summary>
    public class FileImgStris : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "Sextris"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "SexTris image"; } }
        public override String[] FileExtensions { get { return new String[] { "sex" }; } }
        public override String ShortTypeDescription { get { return "SexTris image file"; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override Boolean NeedsPalette { get { return !this.m_PaletteLoaded; } }
        protected Boolean m_PaletteLoaded;

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public virtual Boolean CanSave { get { return false; } }

        protected static Byte[] DefPalette = {
                        0x1A, 0x1A, 0x1A, 0x3F, 0x26, 0x10, 0x00, 0x33, 0x33, 0x00, 0x3F, 0x3F,
                        0x3F, 0x27, 0x27, 0x3F, 0x17, 0x17, 0x3F, 0x08, 0x08, 0x33, 0x00, 0x00,
                        0x27, 0x27, 0x3F, 0x17, 0x18, 0x3F, 0x08, 0x09, 0x3F, 0x00, 0x01, 0x39,
                        0x3F, 0x27, 0x3F, 0x3F, 0x17, 0x3F, 0x3F, 0x00, 0x3F, 0x32, 0x00, 0x33,
                        0x3F, 0x3F, 0x2E, 0x3F, 0x3D, 0x08, 0x39, 0x36, 0x00, 0x33, 0x31, 0x00,
                        0x31, 0x3F, 0x10, 0x20, 0x33, 0x00, 0x1D, 0x2D, 0x00, 0x18, 0x27, 0x00,
                        0x34, 0x20, 0x14, 0x2D, 0x1C, 0x11, 0x28, 0x19, 0x0F, 0x24, 0x17, 0x0D,
                        0x00, 0x00, 0x00, 0x26, 0x26, 0x26, 0x36, 0x36, 0x36, 0x3F, 0x3F, 0x3F };
        
        protected readonly Byte[] m_EndSequence = new Byte[] { 
            0xE9, 0xEF, 0xDE, 0x33, 0xC9, 0x8E, 0x06, 0xBF,
            0x64, 0xBE, 0x20, 0x63, 0x8B, 0xFE, 0xAC, 0x3C,
            0x22, 0x75, 0x04, 0xFE, 0xC5, 0xEB, 0xF7, 0x3C,
            0x0D, 0x75, 0xF3, 0x51, 0x8B, 0xF7 };
        
        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
        }
        
        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            // Basic format for the 224x200 images. There are also 320x200 ones, without the palette or padding zeroes.
            //0000   UInt16       Fixed value 00FD
            //0002   UInt16       Fixed value 0090 or 00A0. Bit flags?
            //0004   Byte         Padding zero
            //0005   UInt16       File size after the current point.
            //0007   Byte         (if #2 is 0x90) Seemed like the image height on some files, but didn't match on others.
            //0008   Byte[0x300]  (if #2 is 0x90) 6-bit Palette
            //0308   Byte         (if #2 is 0x90) Padding zero
            //0309   Byte[]       Image data
            //       Byte[]       (if #2 is 0x90) fixed footer?
            
            if (fileData.Length < 0x309)
                throw new FileTypeLoadException("Too short to be an " + this.ShortTypeName + ".");
            Int32 magic01 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            Int32 magic02 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            // Size: the amount of data that follows after the read value (so after offset 7).
            Int32 size = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 5);
            if (magic01 != 0xFD || fileData.Length != size + 7)
                throw new FileTypeLoadException("Not an " + this.ShortTypeName + ".");
            Int32 width;
            Int32 height;
            Int32 start = 7;
            if (magic02 == 0x90)
            {
                width = 224;
                height = 200;
                // Padding byte before palette
                start ++;
                this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(fileData, start));
                this.m_PaletteLoaded = true;
                // Palette size
                start += 0x300;
                // Padding byte after palette
                start ++;
                Int32 dataEnd = start + width * height;
                Int32 endLen = this.m_EndSequence.Length;
                if (fileData.Length - dataEnd != endLen)
                    throw new FileTypeLoadException("End sequence does not match!");
                Byte[] endbytes = new Byte[endLen];
                Array.Copy(fileData, dataEnd, endbytes, 0, endLen);
                for (Int32 i = 0; i < endLen; ++i)
                    if (endbytes[i] != this.m_EndSequence[i])
                        throw new FileTypeLoadException("End sequence does not match!");
            }
            else if (magic02 == 0xA0)
            {
                Byte[] palArr = new Byte[0x300];
                Array.Copy(DefPalette, 0, palArr, 224 * 3, 32 * 3);
                this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(palArr, 0));
                this.m_PaletteLoaded = true;
                width = 320;
                height = size / width;
                this.ExtraInfo = "Using default palette.";
            }
            else
                throw new FileTypeLoadException("Not an " + this.ShortTypeName + ".");
            Int32 actualImageSize = height * width;
            if (actualImageSize > fileData.Length - start)
                throw new FileTypeLoadException("Not enough data for a " + width + "x" + height + " image.");
            Byte[] imageData = new Byte[actualImageSize];
            Array.Copy(fileData, start, imageData, 0, actualImageSize);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.SetFileNames(sourcePath);
        }
        
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Basic format for the 224x200 images. There are also 320x200 ones, without the palette or padding zeroes.
            //0000   UInt16       Fixed value 00FD
            //0002   UInt16       Fixed value 0090 or 00A0. Bit flags?
            //0004   Byte         Padding zero
            //0005   UInt16       File size after the current point.
            //0007   Byte         (if #2 is 0x90) Seemed like the image height on some files, but didn't match on others.
            //0008   Byte[0x300]  (if #2 is 0x90) 6-bit Palette
            //0308   Byte         (if #2 is 0x90) Padding zero
            //0309   Byte[]       Image data
            //       Byte[]       (if #2 is 0x90) fixed footer?
            throw new NotImplementedException();
        }

    }

}