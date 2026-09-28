using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics2d;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileIcon : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.Image; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return this.m_MaxWidth; } }
        public override int Height { get { return this.m_MaxHeight; } }
        protected int m_MaxWidth;
        protected int m_MaxHeight;
        public override string IdCode { get { return "Ico"; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public override bool FramesHaveCommonPalette { get { return false; } }

        public override string ShortTypeName { get { return "Icon"; } }
        public override string LongTypeName { get { return "Icon file"; } }
        public override string[] FileExtensions { get { return new string[] { "ico" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override string[] DescriptionsForExtensions { get { return new string[] { "Windows Icon" }; } }


        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override bool ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            HeaderParseException hpe;
            try
            {
                const int hdrSize = 6;
                if (fileData.Length < hdrSize)
                    throw new HeaderParseException("Not long enough for header.");
                ushort hdrReserved = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
                ushort hdrType = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
                ushort hdrNumberOfImages = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
                //ICONDIR hdr = ArrayUtils.StructFromByteArray<ICONDIR>(fileData);
                if (hdrReserved != 0)
                    throw new HeaderParseException("Invalid values in header.");
                if (hdrType != 1 && hdrType != 2)
                    throw new HeaderParseException("Invalid values in header.");
                uint nrOfImages = hdrNumberOfImages;
                int indexItemSize = 16;// Marshal.SizeOf(typeof (ICONDIRENTRY));
                if (fileData.Length < hdrSize + nrOfImages * indexItemSize)
                    throw new HeaderParseException("Not long enough for images index.");
                if (nrOfImages == 0)
                    throw new HeaderParseException("No images in given icon.");
                int offset = hdrSize;
                List<SupportedFileType> frames = new List<SupportedFileType>();
                for (int i = 0; i < nrOfImages; ++i)
                {
                    // 0 image width (is 0 for "256")
                    byte dirEntryWidth = fileData[offset];
                    // 1 image height
                    byte dirEntryHeight = fileData[offset + 1];
                    // 2 number of colors
                    //Byte dirEntryPaletteLength = fileData[offset + 2];
                    // 3 reserved
                    //Byte dirEntryReserved = fileData[offset + 3];
                    // 4-5 color planes
                    //UInt16 dirEntryColorPlanes = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 4);
                    // 6-7 bits per pixel
                    //UInt16 dirEntryBitsPerPixel = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 6);
                    // 8-11 size of image data
                    uint dirEntryImageLength = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, offset + 8);
                    // 12-15 offset of image data
                    uint dirEntryImageOffset = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, offset + 12);

                    //ICONDIRENTRY info = ArrayUtils.ReadStructFromByteArray<ICONDIRENTRY>(fileData, offset);
                    uint imageOffset = dirEntryImageOffset;
                    uint imageLength = dirEntryImageLength;
                    if (imageOffset + imageLength > fileData.Length)
                        throw new HeaderParseException("Bad header data: offset and length for image " + i + " do not fit in file.");
                    string type = MimeTypeDetector.GetMimeType(fileData, (int)imageOffset)[0];
                    Bitmap bmp;
                    int frWidth = dirEntryWidth == 0 ? 0x100 : dirEntryWidth;
                    int frHeight = dirEntryHeight == 0 ? 0x100 : dirEntryHeight;
                    PixelFormat originalPixelFormat = PixelFormat.Undefined;
                    if (frWidth == 0 || frHeight == 0)
                        throw new HeaderParseException("Icon dimensions cannot be zero.");
                    if ("png".Equals(type))
                        bmp = this.GetBmp<FileImagePng>(fileData, imageOffset, imageLength);
                    else if ("bmp".Equals(type))
                        bmp = this.GetBmp<FileImageBmp>(fileData, imageOffset, imageLength);
                    else
                    {
                        bmp = DibHandler.ImageFromDib(fileData, (int)imageOffset, (int)imageLength, 0, true, false, out originalPixelFormat);
                        type = "dib";
                    }
                    if (bmp == null)
                        throw new HeaderParseException("Can't detect internal type.");
                    //throw new HeaderParseException("Unsupported image type " + ("dat".Equals(type) ? String.Empty : "\"" + type + "\" ") + "in frame " + i + ".");
                    if ((bmp.Width != 256 && bmp.Width != frWidth) || (bmp.Height != 256 && bmp.Height != frHeight))
                        throw new HeaderParseException("Image " + i + " in icon does not match header information.");
                    this.m_MaxHeight = Math.Max(this.m_MaxHeight, frHeight);
                    this.m_MaxWidth = Math.Max(this.m_MaxWidth, frWidth);
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this, bmp, sourcePath, i);
                    FileClass fc;
                    switch (Image.GetPixelFormatSize(bmp.PixelFormat))
                    {
                        case 1: fc = FileClass.Image1Bit; break;
                        case 4: fc = FileClass.Image4Bit; break;
                        case 8: fc = FileClass.Image8Bit; break;
                        default: fc = FileClass.ImageHiCol; break;
                    }
                    framePic.SetFileClass(fc);
                    StringBuilder extraInfo = new StringBuilder()
                        .AppendFormat("Format: {0}", type.ToUpper());
                    if (originalPixelFormat != PixelFormat.Undefined)
                        extraInfo.AppendFormat("\nOriginal pixel format: {0} bpp", Image.GetPixelFormatSize(originalPixelFormat));
                    extraInfo.AppendFormat("\nOffset: {0}, length: {1}", dirEntryImageOffset, dirEntryImageLength);
                    framePic.SetExtraInfo(extraInfo.ToString());
                    frames.Add(framePic);
                    offset += indexItemSize;
                }
                this.m_FramesList = frames.ToArray();
                return;
            }
            catch (HeaderParseException ex)
            {
                hpe = ex;
            }
            try
            {
                using (MemoryStream ms = new MemoryStream(fileData))
                using (Icon ic = new Icon(ms))
                using (Bitmap bm = ic.ToBitmap())
                {
                    this.m_LoadedImage = ImageUtils.CloneImage(bm);
                    this.m_MaxHeight = bm.Height;
                    this.m_MaxWidth = bm.Width;
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this, ImageUtils.CloneImage(bm), sourcePath, -1);
                    framePic.SetFileNames(sourcePath);
                    framePic.SetBitsPerColor(this.BitsPerPixel);
                    FileClass fc;
                    switch (this.BitsPerPixel)
                    {
                        case 1: fc = FileClass.Image1Bit; break;
                        case 4: fc = FileClass.Image4Bit; break;
                        case 8: fc = FileClass.Image8Bit; break;
                        default: fc = FileClass.ImageHiCol; break;
                    }
                    framePic.SetFileClass(fc);
                    this.m_FramesList = new SupportedFileType[1];
                    this.m_FramesList[0] = framePic;
                    this.m_LoadedImage = ImageUtils.CloneImage(bm);
                }
            }
            catch
            {
                // Image moading failed here too. Release original exception.
                throw new FileTypeLoadException(hpe.Message);
            }

        }

        private Bitmap GetBmp<T>(byte[] data, uint offset, uint length) where T : FileImage, new()
        {
            byte[] frameData = new byte[length];
            Array.Copy(data, offset, frameData, 0, length);

            using (T frameImg = new T())
            {
                frameImg.LoadFile(frameData);
                return ImageUtils.CloneImage(frameImg.GetBitmap());
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            Bitmap bmToSave = fileToSave.GetBitmap();
            int w = bmToSave.Width;
            int h = bmToSave.Height;
            bool addSq = w != h;
            bool addInc = Math.Max(w, h) < 256;
            List<Option> opts = new List<Option>();
            if (addSq)
                opts.Add(new Option("SQR", OptionInputType.Boolean, "Pad image to square format", "1"));
            if (addInc)
            {
                opts.Add(new Option("INC", OptionInputType.Boolean, "Include formats larger than source image", "1"));
                opts.Add(new Option("PIX", OptionInputType.Boolean, "Use pixel zoom for larger images", "0"));
            }
            // The character filter specifically disallows "-", so no actual ranges can be given.
            opts.Add(new Option("SIZ", OptionInputType.String, "Included sizes: (Comma separated, max 256)", "0123456789, " + Environment.NewLine, "16, 24, 32, 48, 64, 96, 128, 192, 256"));
            return opts.ToArray();
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            bool makeSquare = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "SQR"));
            bool upscale = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "INC"));
            bool pixelZoom = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PIX"));
            string includedSizesStr = Option.GetSaveOptionValue(saveOptions, "SIZ");
            // The character filter specifically disallows "-", so no actual ranges can be given.
            int[] sizes = GeneralUtils.GetRangedNumbers(includedSizesStr);
            if (sizes.Length == 0)
                throw new FileTypeSaveException("The icon needs to contain at least one image.");
            for (int i = 0; i < sizes.Length; ++i)
            {
                if (sizes[i] == 0)
                    throw new FileTypeSaveException("0 is not a valid icon size.");
                if (sizes[i] > 0x100)
                    throw new FileTypeSaveException(ERR_DIMENSIONS_TOO_HIGH_SIZE, 256);
            }
            Bitmap bm = fileToSave.GetBitmap();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ConvertToIcon(bm, ms, makeSquare, upscale, pixelZoom, sizes);
                    return ms.ToArray();
                }
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
        }


        /// <summary>
        /// Converts an image to a icon (ico) with all the sizes windows likes
        /// </summary>
        /// <param name="inputBitmap">The input bitmap.</param>
        /// <param name="output">The output stream.</param>
        /// <param name="makeSquare">True to pad the top and bottom of the icons with transparency to make the saved icons square.</param>
        /// <param name="upscale">True to also save the image in sizes larger than the original image.</param>
        /// <param name="pixelZoom">Use pixel scaling for resizing to sizes larger than the original image.</param>
        /// <param name="sizes">Icon sizes to be included.</param>
        /// <returns>True if the the icon was succesfully generated.</returns>
        public static bool ConvertToIcon(Bitmap inputBitmap, Stream output, bool makeSquare, bool upscale, bool pixelZoom, int[] sizes)
        {
            if (inputBitmap == null)
                throw new ArgumentNullException("inputBitmap", "Input bitmap cannot be null.");
            if (output == null)
                throw new ArgumentNullException("output", "Output stream cannot be null.");
            if (sizes == null)
                throw new ArgumentNullException("sizes", "Icon sizes cannot be null.");
            if (sizes.Length == 0)
                throw new ArgumentException("Need at least one icon size.", "sizes");

            List<byte[]> images = new List<byte[]>();
            List<byte> widths = new List<byte>();
            List<byte> heights = new List<byte>();
            int maxDim = Math.Max(inputBitmap.Width, inputBitmap.Height);
            // Generate bitmaps for all the sizes and toss them in streams
            int sizesLen = sizes.Length;
            for (int i = 0; i < sizesLen; ++i)
            {
                int size = sizes[i];
                if (size > 0x100)
                    throw new ArgumentException(String.Format(ERR_DIMENSIONS_TOO_HIGH_SIZE, 256));

                if (!upscale && size > maxDim)
                    continue;
                int width = size;
                int height = size;
                if (inputBitmap.Width <= inputBitmap.Height)
                    width = (int) (((double) inputBitmap.Width / inputBitmap.Height) * size);
                else
                    height = (int) (((double) inputBitmap.Height / inputBitmap.Width) * size);
                // These are 0 for "256"
                byte saveWidth = (byte) (Math.Min(makeSquare ? size : width, 0x100) & 0xFF);
                byte saveHeight = (byte) (Math.Min(makeSquare ? size : height, 0x100) & 0xFF);
                bool skip = false;
                int imgCount = images.Count;
                for (int si = 0; si < imgCount; ++si)
                {
                    if (widths[si] == saveWidth && heights[si] == saveHeight)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip)
                    continue;
                widths.Add(saveWidth);
                heights.Add(saveHeight);
                // Always use smooth resize for smaller images.
                using (Bitmap newBitmap = ImageUtils.ResizeImage(inputBitmap, width, height, makeSquare, size < maxDim || !pixelZoom))
                    images.Add(GetPngData(newBitmap));
            }
            try
            {
                ConvertImagesToIco(images.ToArray(), output);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static Icon ConvertImagesToIco(Image[] images)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int nrOfImages = images.Length;
                byte[][] pngImages = new byte[nrOfImages][];
                for (int i = 0; i < nrOfImages; ++i)
                    pngImages[i] = GetPngData(images[i]);
                ConvertImagesToIco(pngImages, ms);
                ms.Position = 0;
                return new Icon(ms);
            }
        }

        public static byte[] ConvertImagesToIcoBytes(Image[] images)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int nrOfImages = images.Length;
                byte[][] pngImages = new byte[nrOfImages][];
                for (int i = 0; i < nrOfImages; ++i)
                    pngImages[i] = GetPngData(images[i]);
                ConvertImagesToIco(pngImages, ms);
                return ms.ToArray();
            }
        }

        public static void ConvertImagesToIco(byte[][] pngImages, Stream output)
        {
            if (pngImages == null)
                throw new ArgumentNullException("pngImages");
            int imgCount = pngImages.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given.", "pngImages");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images.", "pngImages");
            using (BinaryWriter iconWriter = new BinaryWriter(new NonDisposingStream(output)))
            {
                byte[][] frameBytes = new byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((short)0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((short)1);
                // 4-5 number of images
                iconWriter.Write((short)imgCount);
                // Calculate header size for first image data offset.
                int offset = 6 + (16 * imgCount);
                for (int i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    byte[] frameData = pngImages[i];
                    int width = frameData[19] | frameData[18] << 8 | frameData[17] << 16 | frameData[16] << 24;
                    int height = frameData[23] | frameData[22] << 8 | frameData[21] << 16 | frameData[20] << 24;
                    if (width > 256 || height > 256)
                        throw new ArgumentException("Image " + i + "is too large.", "pngImages");
                    // Get the color depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    int bpp;
                    byte colDepth = frameData[24];
                    byte colType = frameData[25];
                    // I think .Net saving only supports color types 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2: bpp = 3 * colDepth; break; // RGB
                        case 6: bpp = 4 * colDepth; break; // ARGB
                        default: bpp = colDepth; break; // Indexed & greyscale
                    }
                    byte colors;
                    if (bpp > 8)
                        colors = 0;
                    else
                    {
                        int plteOffset = PngHandler.FindPngChunk(frameData, "PLTE");
                        if (plteOffset == -1) // Should never happen...
                            throw new ArgumentException("Cannot convert image " + i + ".");
                        // Value 0 is interpreted as 256, so the cast reducing 256 to 0 is no problem.
                        colors = (byte)(PngHandler.GetPngChunkDataLength(frameData, plteOffset) / 3);
                    }
                    frameBytes[i] = frameData;
                    int imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width. Value 0 is interpreted as 256, so the cast reducing 256 to 0 is no problem.
                    iconWriter.Write((byte)width);
                    // 1 image height. Value 0 is interpreted as 256, so the cast reducing 256 to 0 is no problem.
                    iconWriter.Write((byte)height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((byte)0);
                    // 4-5 color planes
                    iconWriter.Write((short)0);
                    // 6-7 bits per pixel
                    iconWriter.Write((short)bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (int i = 0; i < imgCount; ++i)
                {
                    // Write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(frameBytes[i]);
                }
                iconWriter.Flush();
            }
        }

        private static byte[] GetPngData(Image bitmap)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                data = ms.ToArray();
            }
            return data;
        }
    }

    public enum ImageScaleMode
    {
        Pad,
        Stretch
    }
}
