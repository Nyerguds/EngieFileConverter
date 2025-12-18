using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgLadyTme : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "LadyTme"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "LadyLove TME Image"; } }
        public override String[] FileExtensions { get { return new String[] { "tme" }; } }
        public override String LongTypeName { get { return "LadyLove TME Image file"; } }
        public override Boolean NeedsPalette { get { return this.m_Palette == null; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public override Boolean CanSave { get { return false; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, 0, fileData == null ? 0 : fileData.Length);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename, 0, fileData == null ? 0 : fileData.Length);
        }

        public void LoadFromFileData(Byte[] fileData, String sourcePath, Int32 frameStart, Int32 frameLength)
        {
            if (frameStart < 0 || frameLength < 6 || frameStart + frameLength > fileData.Length)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            Int32 width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, frameStart + 0);
            Int32 height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, frameStart + 2);
            Int32 colors = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, frameStart + 4);
            Int32 imgLength = width * height;
            Int32 dataStart = 6 + 3 * colors;
            Int32 frameDataLength = dataStart + imgLength;
            if (frameLength < frameDataLength)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeName + ".");
            if (width == 0)
                throw new FileTypeLoadException("Width cannot be 0");
            if (height == 0)
                throw new FileTypeLoadException("Height cannot be 0");
            if (width % 4 != 0)
                throw new FileTypeLoadException("Width must be divisible by 4.");
            Int32 imgChunkLength = imgLength / 4;
            Int32 imgChunkLength2 = imgChunkLength * 2;
            Int32 imgChunkLength3 = imgChunkLength * 3;
            Boolean noCol = false;
            Color[] pal;
            if (colors > 0)
            {
                try
                {
                    pal = ColorUtils.ReadSixBitPalette(fileData, frameStart + 6, colors);
                }
                catch (ArgumentException)
                {
                    throw new FileTypeLoadException("Palette data is not 6-bit.");
                }
            }
            else
            {
                pal = PaletteUtils.GenerateGrayPalette(8, null, false);
                noCol = true;
            }
            Byte[] imageData = new Byte[imgLength];
            Int32 dataPtr = frameStart + dataStart;
            Int32 outPtr = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                for (Int32 x = 0; x < width; x += 4)
                {
                    imageData[outPtr++] = fileData[dataPtr];
                    imageData[outPtr++] = fileData[dataPtr + imgChunkLength];
                    imageData[outPtr++] = fileData[dataPtr + imgChunkLength2];
                    imageData[outPtr++] = fileData[dataPtr + imgChunkLength3];
                    dataPtr++;
                }
            }
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, pal, null);

            if (!noCol)
            {
                this.m_Palette = pal;
                colors = pal.Length;
                if (colors < 256)
                    this.m_LoadedImage.Palette = ImageUtils.GetPalette(pal, colors);
            }
            this.SetFileNames(sourcePath);
        }

        public void OverridePalette(Color[] pal, String source)
        {
            this.m_Palette = pal;
            this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette, pal.Length);
            if (String.IsNullOrEmpty(this.ExtraInfo))
                this.ExtraInfo = source;
            else
                this.ExtraInfo += '\n' + source;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotImplementedException();
        }
    }
}