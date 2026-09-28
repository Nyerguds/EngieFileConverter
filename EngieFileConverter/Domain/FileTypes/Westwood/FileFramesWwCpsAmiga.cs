using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwCpsAmiga : FileImgWwCps
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        protected SupportedFileType[] m_FramesList;

        public override string IdCode { get { return "WwCpsAmiF"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood Amiga CPS Frames File"; } }
        public override string[] FileExtensions { get { return new string[] { "cps" }; } }
        public override string LongTypeName { get { return "Westwood Amiga CPS Frames File"; } }
        public override int BitsPerPixel { get { return m_ColorFormat; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            int dataLen = fileData.Length;
            if (dataLen < 10)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            int fileSize = (int)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            int compression = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            if (compression > 4)
                throw new FileTypeLoadException(String.Format(ERR_UNKN_COMPR_X, compression));
            int bufferSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 4);
            int paletteLength = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 8);
            m_ColorFormat = 0;
            if (fileSize == dataLen)
            {
                // Decompress, see if multiple 8-byte headers are in there.
                byte[] dataBlock = DecompressData(fileData, 0, dataLen, paletteLength, bufferSize, compression, true);
                int startOffset = 0;
                List<int> listOffsets = new List<int>();
                List<int> listLengths = new List<int>();
                List<int> listWidths = new List<int>();
                List<int> listHeights = new List<int>();
                List<int> listPlanes = new List<int>();
                List<int> listStrides = new List<int>();
                while (bufferSize > startOffset)
                {
                    if (bufferSize < startOffset + 8)
                        throw new FileTypeLoadException(ERR_NO_HEADER);
                    int width = ArrayUtils.ReadInt16FromByteArrayBe(dataBlock, startOffset);
                    int height = ArrayUtils.ReadInt16FromByteArrayBe(dataBlock, startOffset + 2);
                    int planes = dataBlock[startOffset + 4];
                    int stride = ArrayUtils.ReadInt16FromByteArrayBe(dataBlock, startOffset + 6);
                    int length = stride * height * planes;
                    // Always seems to use strides aligned to 2-byte boundaries.
                    int calStride = (((width + 7) / 8 + 1) / 2) * 2;
                    if (stride != calStride || planes > 5 || width > 320 || height > 200)
                        throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                    listOffsets.Add(startOffset + 8);
                    listLengths.Add(length);
                    listWidths.Add(width);
                    listHeights.Add(height);
                    listPlanes.Add(planes);
                    listStrides.Add(stride);
                    startOffset += 8 + length;
                }
                if (startOffset != bufferSize)
                    throw new FileTypeLoadException(ERR_BAD_SIZE);
                // Confirmed: process data.
                m_FramesList = new SupportedFileType[listOffsets.Count];
                for (int i = 0; i < listOffsets.Count; ++i)
                {
                    int offset = listOffsets[i];
                    int length = listLengths[i];
                    int width = listWidths[i];
                    int height = listHeights[i];
                    int planes = listPlanes[i];
                    int stride = listStrides[i];
                    if (m_ColorFormat < planes)
                        m_ColorFormat = planes;
                    Color[] framePal = PaletteUtils.GenerateGrayPalette(planes, null, false);
                    byte[] imageData = ImageUtils.PlanarBlocksToLinear(dataBlock, offset, length, width, height, stride, planes);
                    stride = width;
                    PixelFormat pf = PixelFormat.Format8bppIndexed;
                    if (planes == 4)
                    {
                        imageData = ImageUtils.ConvertFrom8Bit(imageData, width, height, 4, true, ref stride);
                        pf = PixelFormat.Format4bppIndexed;
                    }
                    Bitmap curFrImg = ImageUtils.BuildImage(imageData, width, height, stride, pf, framePal, null);
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this, curFrImg, filename, i);
                    framePic.SetBitsPerColor(planes);
                    framePic.SetFileClass(this.FrameInputFileClass);
                    framePic.SetNeedsPalette(true);
                    m_FramesList[i] = framePic;
                }
            }
            else if (fileSize < dataLen)
            {
                int startOffset = 0;
                List<int> listOffsets = new List<int>();
                List<int> listSizes = new List<int>();
                while (startOffset < dataLen)
                {
                    int size = (int)ArrayUtils.ReadIntFromByteArray(fileData, startOffset, 2, true);
                    listOffsets.Add(startOffset);
                    listSizes.Add(size);
                    startOffset += size;
                }
                if (startOffset != dataLen)
                    throw new FileTypeLoadException(ERR_BAD_SIZE);
                m_FramesList = new SupportedFileType[listOffsets.Count];
                for (int i = 0; i < listOffsets.Count; ++i)
                {
                    int offset = listOffsets[i];
                    int size = listSizes[i];
                    byte[] imageData = GetImageData(fileData, offset, size, filename, false, false,
                        out int compr, out Color[] palette, out CpsVersion cpsVersion, out int colorDepth, out int width, out int height);
                    if (cpsVersion != CpsVersion.AmigaEob1)
                        throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
                    if (m_ColorFormat < colorDepth)
                        m_ColorFormat = colorDepth;
                    int stride = width;
                    PixelFormat pf = PixelFormat.Format8bppIndexed;
                    if (colorDepth == 4)
                    {
                        imageData = ImageUtils.ConvertFrom8Bit(imageData, width, height, 4, true, ref stride);
                        pf = PixelFormat.Format4bppIndexed;
                    }
                    Bitmap curFrImg = ImageUtils.BuildImage(imageData, width, height, stride, pf, palette, null);
                    if (palette.Length < 256)
                        curFrImg.Palette = ImageUtils.GetPalette(palette);
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this, curFrImg, filename, i);
                    framePic.SetBitsPerColor(colorDepth);
                    framePic.SetFileClass(this.FrameInputFileClass);
                    framePic.SetNeedsPalette(palette == null);
                    m_FramesList[i] = framePic;
                    // TODO process image data to 320x200 image.
                }
            }
            else
            {
                throw new FileTypeLoadException(ERR_BAD_SIZE);
            }
            //throw new FileTypeLoadException("Experimental.");
            /*/
            byte[] imageData = GetImageData(fileData, startOffset, fileData.Length - startOffset, filename, false, false,
                out int compression, out Color[] palette, out CpsVersion cpsVersion, out int colorDepth, out int width, out int height);
            m_ColorFormat = colorDepth;
            this.CompressionType = compression;
            this.CpsVersion = cpsVersion;
            this.SetFileNames(filename);
            this.SetExtraInfo(null);
            //*/
        }

    }
}