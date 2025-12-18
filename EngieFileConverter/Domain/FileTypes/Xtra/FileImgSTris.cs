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

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "SexTris image"; } }
        public override String[] FileExtensions { get { return new String[] { "sex" }; } }
        public override String ShortTypeDescription { get { return "SexTris image file"; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public override Int32 ColorsInPalette { get { return this.m_PaletteLoaded ? base.ColorsInPalette : 0; } }
        protected Boolean m_PaletteLoaded;

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
            //0004   Byte         Padding zero (if value at #2 is 0x90)
            //0005   UInt16       File size after the current point.
            //0007   Byte         Seemed like the image height on some files, but didn't match on others.
            //0008   Byte[0x300]  6-bit Palette (if value at #2 is 0x90)
            //0308   Byte         Padding zero (if value at #2 is 0x90)
            //0309   Byte[]       Image data
            //       Byte[]       fixed footer? (if value at #2 is 0x90)
            
            if (fileData.Length < 0x309)
                throw new FileTypeLoadException("Too short to be an " + this.ShortTypeName + ".");
            Int32 magic01 = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            Int32 magic02 = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            // Size: the amount of data that follows after the read value (so after offset 7).
            Int32 size = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 5, 2, true);
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
                for (Int32 i = 0; i < endLen; i++)
                    if (endbytes[i] != this.m_EndSequence[i])
                        throw new FileTypeLoadException("End sequence does not match!");
            }
            else if (magic02 == 0xA0)
            {
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
                this.m_PaletteLoaded = false;
                width = 320;
                height = size / width;
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
            throw new NotImplementedException();
        }

    }

}