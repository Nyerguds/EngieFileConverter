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
        public override String LongTypeName { get { return "Interactive Girls GX2 image file"; } }
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
                throw new FileTypeLoadException("Too short to be an " + this.LongTypeName + ".");
            UInt32 magic1 = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0x00);
            //UInt16 headsize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x04);
            Byte bpp = fileData[0x06];
            UInt16 width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x07);
            UInt16 height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x09);
            //UInt16 aspectX = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0B);
            //UInt16 aspectY = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0D);
            //Byte unknown1 = fileData[0x0F];
            //UInt16 subhsize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x10);
            UInt32 magic2 = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0x12);
            //UInt16 unknown2 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x16);
            //Byte unknown3 = fileData[0x18];
            //UInt16 unknown4 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x19);
            if (width == 0 || height == 0)
                throw new FileTypeLoadException("Dimensions cannot be 0.");
            if (magic1 != 0x01325847 || magic2 != 0x58465053)
                throw new FileTypeLoadException("Not an " + this.LongTypeName + ".");
            Int32 palCols = bpp > 8 ? 0 : (1 << bpp);
            Int32 palSize = palCols * 3;
            this.m_BitPerPixel = bpp;
            if (dataLen < 0x1B + palSize)
                throw new FileTypeLoadException("Too short to be an " + this.LongTypeName + ".");
            Byte[] pal = new Byte[palSize];
            Array.Copy(fileData, 0x1B, pal, 0, palSize);
            this.m_Palette = ColorUtils.ReadEightBitPalette(pal, 0, palCols);
            Int32 dataOffs = 0x1B + palSize;
            Byte[] frameDataUncompr = RleCompressionHighBitRepeat.RleDecode(fileData, (UInt32)dataOffs, null, true);
            if (frameDataUncompr == null)
                throw new FileTypeLoadException("RLE decompression failed.");
            Byte[] frameData;
            try
            {
                frameData = IgcBitMaskCompression.BitMaskDecompress(frameDataUncompr, width, height);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Bit mask decompression failed: " + GeneralUtils.RecoverArgExceptionMessage(ex, false), ex);
            }
            this.m_LoadedImage = ImageUtils.BuildImage(frameData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
            if (fileToSave.Width > 320 || fileToSave.Height > 200)
                throw new ArgumentException(ERR_IMAGE_TOO_LARGE, "fileToSave");

            UInt16 width = (UInt16)fileToSave.Width;
            UInt16 height = (UInt16)fileToSave.Height;
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Byte[] palette = ColorUtils.GetEightBitPaletteData(fileToSave.GetColors(), true);
            imageData = IgcBitMaskCompression.BitMaskCompress(imageData, stride, height);
            imageData = RleCompressionHighBitRepeat.RleEncode(imageData);
            Byte[] data = new Byte[imageData.Length + palette.Length + 0x1B];
            ArrayUtils.WriteInt32ToByteArrayLe(data, 0x00, 0x01325847); // magic
            ArrayUtils.WriteInt16ToByteArrayLe(data, 0x04, 0x19); // headsize
            data[0x06] = 0x08; // BPP
            ArrayUtils.WriteUInt16ToByteArrayLe(data, 0x07, width); // width
            ArrayUtils.WriteUInt16ToByteArrayLe(data, 0x09, height); // height
            ArrayUtils.WriteInt16ToByteArrayLe(data, 0x0B, 0x04); // xaspect
            ArrayUtils.WriteInt16ToByteArrayLe(data, 0x0D, 0x03); // yaspect
            data[0x0F] = 0x00; // unknown1
            ArrayUtils.WriteInt16ToByteArrayLe(data, 0x10, 0x09); // subhsize
            ArrayUtils.WriteInt32ToByteArrayLe(data, 0x12, 0x58465053); // shmagic
            ArrayUtils.WriteInt16ToByteArrayLe(data, 0x16, 0x0F); // unknown2
            data[0x18] = 0x00; // unknown3
            ArrayUtils.WriteInt16ToByteArrayLe(data, 0x19, 0x02); // unknown4
            Array.Copy(palette, 0, data, 0x1B, palette.Length);
            Array.Copy(imageData, 0, data, palette.Length + 0x1B, imageData.Length);
            return data;
        }

    }

}