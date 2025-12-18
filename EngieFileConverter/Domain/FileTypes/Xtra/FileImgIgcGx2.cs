using System;
using System.Drawing.Imaging;
using Nyerguds.FileData.Compression;
using Nyerguds.FileData.IGC;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Interactive Girls Club image files.
    /// </summary>
    public class FileImgIgcGx2 : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "IgGx2"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Interactive Girls GX2 file"; } }
        public override String[] FileExtensions { get { return new String[] { "gx2" }; } }
        public override String ShortTypeDescription { get { return "Interactive Girls GX2 image file"; } }
        public override Int32 BitsPerPixel { get { return this.m_BitPerPixel; } }
        protected Int32 m_BitPerPixel;
        
        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            Int32 dataLen = fileData.Length;
            if (dataLen < 0x1B)
                throw new FileTypeLoadException("Too short to be an " + this.ShortTypeDescription + "!");
            UInt32 magic1 = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x00, 4, true);
            //UInt16 headsize = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x04, 2, true);
            Byte bpp = (Byte) ArrayUtils.ReadIntFromByteArray(fileData, 0x06, 1, true);
            UInt16 width = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x07, 2, true);
            UInt16 height = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x09, 2, true);
            //UInt16 aspectX = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x0B, 2, true);
            //UInt16 aspectY = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0x0D, 2, true);
            //Byte unknown1 = (Byte) ArrayUtils.ReadIntFromByteArray(fileData, 0x0F, 1, true);
            //UInt16 subhsize = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x10, 2, true);
            UInt32 magic2 = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x12, 4, true);
            //UInt16 unknown2 = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x16, 2, true);
            //Byte unknown3 = (Byte) ArrayUtils.ReadIntFromByteArray(fileData, 0x18, 1, true);
            //UInt16 unknown4 = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, 0x19, 2, true);
            if (width == 0 || height == 0)
                throw new FileTypeLoadException("Dimensions cannot be 0!");
            if (magic1 != 0x01325847 || magic2 != 0x58465053)
                throw new FileTypeLoadException("Not an " + this.ShortTypeDescription + "!");
            Int32 palSize = bpp > 8 ? 0 : (1 << bpp) * 3;
            this.m_BitPerPixel = bpp;
            if (dataLen < 0x1B + palSize)
                throw new FileTypeLoadException("Too short to be an " + this.ShortTypeDescription + "!");
            Byte[] pal = new Byte[palSize];
            Array.Copy(fileData, 0x1B, pal, 0, palSize);
            this.m_Palette = ColorUtils.ReadEightBitPalette(pal, false);
            Int32 dataOffs = 0x1B + palSize;
            Byte[] frameDataUncompr = RleCompressionHighBitRepeat.RleDecode(fileData, (UInt32)dataOffs, null, true);
            if (frameDataUncompr == null)
                throw new FileTypeLoadException("RLE decompression failed!");
            Byte[] frameData;
            try
            {
                frameData = IgcBitMaskCompression.BitMaskDecompress(frameDataUncompr, width, height);
            }
            catch (ArgumentException e)
            {
                throw new FileTypeLoadException("Bit mask decompression failed: " + e.Message, e);
            }
            this.m_LoadedImage = ImageUtils.BuildImage(frameData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("No source data given!");
            if (fileToSave.BitsPerPixel != 8)
                throw new NotSupportedException("This format needs an 8bpp image.");
            if (fileToSave.Width > 320 || fileToSave.Height > 200)
                throw new NotSupportedException("The given image is too large.");

            UInt16 width = (UInt16)fileToSave.Width;
            UInt16 height = (UInt16)fileToSave.Height;
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Byte[] palette = ColorUtils.GetEightBitPaletteData(fileToSave.GetColors(), true);
            imageData = IgcBitMaskCompression.BitMaskCompress(imageData, stride, height);
            imageData = RleCompressionHighBitRepeat.RleEncode(imageData);
            Byte[] data = new Byte[imageData.Length + palette.Length + 0x1B];
            ArrayUtils.WriteIntToByteArray(data, 0x00, 4, true, 0x01325847); // magic
            ArrayUtils.WriteIntToByteArray(data, 0x04, 2, true, 0x19); // headsize
            ArrayUtils.WriteIntToByteArray(data, 0x06, 1, true, 0x08); // BPP
            ArrayUtils.WriteIntToByteArray(data, 0x07, 2, true, width); // width
            ArrayUtils.WriteIntToByteArray(data, 0x09, 2, true, height); // height
            ArrayUtils.WriteIntToByteArray(data, 0x0B, 2, true, 0x04); // xaspect
            ArrayUtils.WriteIntToByteArray(data, 0x0D, 2, true, 0x03); // yaspect
            ArrayUtils.WriteIntToByteArray(data, 0x0F, 1, true, 0x00); // unknown1
            ArrayUtils.WriteIntToByteArray(data, 0x10, 2, true, 0x09); // subhsize
            ArrayUtils.WriteIntToByteArray(data, 0x12, 4, true, 0x58465053); // shmagic
            ArrayUtils.WriteIntToByteArray(data, 0x16, 2, true, 0x0F); // unknown2
            ArrayUtils.WriteIntToByteArray(data, 0x18, 1, true, 0x00); // unknown3
            ArrayUtils.WriteIntToByteArray(data, 0x19, 2, true, 0x02); // unknown4
            Array.Copy(palette, 0, data, 0x1B, palette.Length);
            Array.Copy(imageData, 0, data, palette.Length + 0x1B, imageData.Length);
            return data;
        }

    }

}