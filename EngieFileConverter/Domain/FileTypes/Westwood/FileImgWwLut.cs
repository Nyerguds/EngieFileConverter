using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgWwLut : SupportedFileType
    {
        const int LutDimensions = 64;
        const int LutPixelLength = 3;
        const int LutLineLength = 64 * LutPixelLength;
        const int LutSize = LutLineLength * LutDimensions;
        const int LutMaxBrightness = 15;

        public override FileClass FileClass { get { return FileClass.ImageHiCol; } }
        public override FileClass InputFileClass { get { return FileClass.Image; } }

        public override String IdCode { get { return "WwLut"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood Chrono LUT"; } }
        public override String[] FileExtensions { get { return new String[] { "lut" }; } }
        public override String LongTypeName { get { return "Westwood Chrono Vortex Lookup Table"; } }
        public override Int32 BitsPerPixel { get { return 32; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length != LutSize)
            {
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            }
            int writeStride = LutDimensions * 3;
            byte[] imageData = new byte[LutDimensions * writeStride];
            int index = 0;
            for (int i = 0; i < LutSize; i += LutPixelLength)
            {
                int x = index % LutDimensions;
                int y = index / LutDimensions;
                index++;
                int write = writeStride * y + x * 3;
                int pixX = fileData[i + 0];
                int pixY = fileData[i + 1];
                int bri = fileData[i + 2];
                // boundaries check. Pretty much all we can do.
                if (pixX >= LutDimensions || pixY >= LutDimensions || bri > LutMaxBrightness)
                {
                    throw new FileTypeLoadException(ERR_BAD_IMAGE_DATA);
                }
                // Abusing 6BitVgaPal formatter since it stretches 0-63 values to 0-255
                byte[] imgdata = PixelFormatter.Format6BitVgaPal.GetColorComponents(fileData, i);
                int valX = imgdata[PixelFormatter.ColR];
                int valY = imgdata[PixelFormatter.ColG];
                int valDark = (15 - bri) * 255 / 15;
                // ARGB = B,G,R,A
                imageData[write + 0] = (byte)valY; // Blue
                imageData[write + 1] = (byte)valDark; // Green
                imageData[write + 2] = (byte)valX; // Red
                // imageData[write + 3] = 255;
            }
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, LutDimensions, LutDimensions, writeStride, PixelFormat.Format24bppRgb, null, null);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave.IsFramesContainer && !fileToSave.HasCompositeFrame)
            {
                throw new FileTypeSaveException(ERR_ONLY_FRAMES);
            }
            Bitmap bitmap = fileToSave.GetBitmap();
            if (bitmap == null)
            {
                throw new FileTypeSaveException(ERR_NO_IMAGE);
            }
            if (bitmap.Width != LutDimensions || bitmap.Height != LutDimensions)
            {
                throw new FileTypeSaveException(String.Format(ERR_DIMENSIONS_INPUT, LutDimensions, LutDimensions));
            }
            byte[] inputData = ImageUtils.GetImageData(bitmap, out int stride, PixelFormat.Format24bppRgb);
            byte[] saveData = new byte[LutDimensions * LutLineLength];
            int inIndexRow = 0;
            int outIndex = 0;
            for (int y = 0; y < LutDimensions; ++y)
            {
                int inIndex = inIndexRow;
                for (int x = 0; x < LutDimensions; ++x)
                {
                    byte b = inputData[inIndex++];
                    byte g = inputData[inIndex++];
                    byte r = inputData[inIndex++];
                    // Abusing the Format6BitVgaPal formatter since it stretches 0-63 values to 0-255
                    byte[] components = new byte[] { Byte.MaxValue, r, g, b };
                    PixelFormatter.Format6BitVgaPal.WriteColorComponents(components, 0, components);
                    // Invert green factor; brighter green = added darkness.
                    int bri = LutMaxBrightness - (g * LutMaxBrightness / Byte.MaxValue);
                    saveData[outIndex++] = components[0]; // Red component in 6-bit 'palette'
                    saveData[outIndex++] = components[2]; // Blue component in 6-bit 'palette'
                    saveData[outIndex++] = (byte)bri;
                }
                inIndexRow += stride;
            }
            return saveData;
        }
    }
}