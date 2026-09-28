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

        public override string IdCode { get { return "IgGx2"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Interactive Girls GX2 file"; } }
        public override string[] FileExtensions { get { return new string[] { "gx2" }; } }
        public override string LongTypeName { get { return "Interactive Girls GX2 image file"; } }
        public override int BitsPerPixel { get { return this.m_BitPerPixel; } }
        protected int m_BitPerPixel;

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            int dataLen = fileData.Length;
            if (dataLen < 0x1B)
                throw new FileTypeLoadException("Too short to be an " + this.LongTypeName + ".");
            uint magic1 = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0x00);
            //UInt16 headsize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x04);
            byte bpp = fileData[0x06];
            ushort width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x07);
            ushort height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x09);
            //UInt16 aspectX = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0B);
            //UInt16 aspectY = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0D);
            //Byte unknown1 = fileData[0x0F];
            //UInt16 subhsize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x10);
            uint magic2 = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0x12);
            //UInt16 unknown2 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x16);
            //Byte unknown3 = fileData[0x18];
            //UInt16 unknown4 = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x19);
            if (width == 0 || height == 0)
                throw new FileTypeLoadException("Dimensions cannot be 0.");
            if (magic1 != 0x01325847 || magic2 != 0x58465053)
                throw new FileTypeLoadException("Not an " + this.LongTypeName + ".");
            int palCols = bpp > 8 ? 0 : (1 << bpp);
            int palSize = palCols * 3;
            this.m_BitPerPixel = bpp;
            if (dataLen < 0x1B + palSize)
                throw new FileTypeLoadException("Too short to be an " + this.LongTypeName + ".");
            byte[] pal = new byte[palSize];
            Array.Copy(fileData, 0x1B, pal, 0, palSize);
            this.m_Palette = ColorUtils.ReadEightBitPalette(pal, 0, palCols);
            int dataOffs = 0x1B + palSize;
            byte[] frameDataUncompr = RleCompressionHighBitRepeat.RleDecode(fileData, (uint)dataOffs, null, true);
            if (frameDataUncompr == null)
                throw new FileTypeLoadException("RLE decompression failed.");
            byte[] frameData;
            try
            {
                frameData = IgcBitMaskCompression.BitMaskDecompress(frameDataUncompr, width, height);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException("Bit mask decompression failed: " + GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
            this.m_LoadedImage = ImageUtils.BuildImage(frameData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            if (fileToSave.BitsPerPixel != 8)
                throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 8);
            if (fileToSave.Width > 320 || fileToSave.Height > 200)
                throw new FileTypeSaveException(ERR_DIMENSIONS_TOO_LARGE);

            ushort width = (ushort)fileToSave.Width;
            ushort height = (ushort)fileToSave.Height;
            int stride;
            byte[] imageData = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            byte[] palette = ColorUtils.GetEightBitPaletteData(fileToSave.GetColors(), true);
            try
            {
                imageData = IgcBitMaskCompression.BitMaskCompress(imageData, stride, height);
                imageData = RleCompressionHighBitRepeat.RleEncode(imageData);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true));
            }
            byte[] data = new byte[imageData.Length + palette.Length + 0x1B];
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