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

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileIcon : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.Image; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_MaxWidth; } }
        public override Int32 Height { get { return this.m_MaxHeight; } }
        protected Int32 m_MaxWidth;
        protected Int32 m_MaxHeight;
        public override String IdCode { get { return "Ico"; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public override Boolean FramesHaveCommonPalette { get { return false; } }

        public override String ShortTypeName { get { return "Icon"; } }
        public override String ShortTypeDescription { get { return "Icon file"; } }
        public override String[] FileExtensions { get { return new String[] { "ico" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] { "Windows Icon" }; } }


        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            HeaderParseException hpe;
            try
            {
                const Int32 hdrSize = 6;
                if (fileData.Length < hdrSize)
                    throw new HeaderParseException("Not long enough for header.");
                UInt16 hdrReserved = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
                UInt16 hdrType = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
                UInt16 hdrNumberOfImages = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
                //ICONDIR hdr = ArrayUtils.StructFromByteArray<ICONDIR>(fileData);
                if (hdrReserved != 0)
                    throw new HeaderParseException("Invalid values in header.");
                if (hdrType != 1 && hdrType != 2)
                    throw new HeaderParseException("Invalid values in header.");
                UInt32 nrOfImages = hdrNumberOfImages;
                Int32 indexItemSize = 16;// Marshal.SizeOf(typeof (ICONDIRENTRY));
                if (fileData.Length < hdrSize + nrOfImages * indexItemSize)
                    throw new HeaderParseException("Not long enough for images index.");
                if (nrOfImages == 0)
                    throw new HeaderParseException("No images in given icon.");
                Int32 offset = hdrSize;
                this.m_FramesList = new SupportedFileType[nrOfImages];
                for (Int32 i = 0; i < nrOfImages; ++i)
                {
                    // 0 image width (is 0 for "256")
                    Byte dirEntryWidth = fileData[offset];
                    // 1 image height
                    Byte dirEntryHeight = fileData[offset + 1];
                    // 2 number of colors
                    //Byte dirEntryPaletteLength = fileData[offset + 2];
                    // 3 reserved
                    //Byte dirEntryReserved = fileData[offset + 3];
                    // 4-5 color planes
                    //UInt16 dirEntryColorPlanes = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 4);
                    // 6-7 bits per pixel
                    //UInt16 dirEntryBitsPerPixel = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + 6);
                    // 8-11 size of image data
                    UInt32 dirEntryImageLength = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, offset + 8);
                    // 12-15 offset of image data
                    UInt32 dirEntryImageOffset = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, offset + 12);

                    //ICONDIRENTRY info = ArrayUtils.ReadStructFromByteArray<ICONDIRENTRY>(fileData, offset);
                    UInt32 imageOffset = dirEntryImageOffset;
                    UInt32 imageLength = dirEntryImageLength;
                    if (imageOffset + imageLength > fileData.Length)
                        throw new HeaderParseException("Bad header data: offset and length for image " + i + " do not fit in file.");
                    Byte[] frameData = new Byte[imageLength];
                    Array.Copy(fileData, imageOffset, frameData, 0, imageLength);
                    String type = MimeTypeDetector.GetMimeType(frameData)[0];
                    Bitmap bmp;
                    Int32 frWidth = dirEntryWidth == 0 ? 0x100 : dirEntryWidth;
                    Int32 frHeight = dirEntryHeight == 0 ? 0x100 : dirEntryHeight;
                    PixelFormat originalPixelFormat = PixelFormat.Undefined;
                    if (frWidth == 0 || frHeight == 0)
                        throw new HeaderParseException("Icon dimensions cannot be zero.");
                    if ("png".Equals(type))
                        bmp = this.GetBmp<FileImagePng>(frameData);
                    else if ("bmp".Equals(type))
                        bmp = this.GetBmp<FileImageBmp>(frameData);
                    else
                    {
                        bmp = DibHandler.ImageFromDib(frameData, 0, 0, true, out originalPixelFormat);
                        if (bmp != null)
                            type = "dib";
                    }
                    if (bmp == null)
                        throw new HeaderParseException("Can't detect internal type!");
                    //throw new HeaderParseException("Unsupported image type " + ("dat".Equals(type) ? String.Empty : "\"" + type + "\" ") + "in frame " + i + ".");
                    if (bmp.Width != frWidth || bmp.Height != frHeight)
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
                    String extraInfo = "Format: " + type.ToUpper();
                    if (originalPixelFormat != PixelFormat.Undefined)
                        extraInfo += "\nOriginal pixel format: " + Image.GetPixelFormatSize(originalPixelFormat) + " bpp";
                    framePic.SetExtraInfo(extraInfo);
                    this.m_FramesList[i] = framePic;
                    offset += indexItemSize;
                }
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

        private Bitmap GetBmp<T>(Byte[] frameData) where T : FileImage, new()
        {
            using (T frameImg = new T())
            {
                frameImg.LoadFile(frameData);
                return ImageUtils.CloneImage(frameImg.GetBitmap());
            }
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Bitmap bmToSave = fileToSave.GetBitmap();
            Int32 w = bmToSave.Width;
            Int32 h = bmToSave.Height;
            Boolean addSq = w != h;
            Boolean addInc = Math.Max(w, h) < 256;
            List<SaveOption> opts = new List<SaveOption>();
            if (addSq)
                opts.Add(new SaveOption("SQR", SaveOptionType.Boolean, "Pad frames to square formats", null, "1"));
            if (addInc)
            {
                opts.Add(new SaveOption("INC", SaveOptionType.Boolean, "Include formats larger than source image", "1"));
                opts.Add(new SaveOption("PIX", SaveOptionType.Boolean, "Use pixel zoom for larger images", "0"));
            }
            opts.Add(new SaveOption("16", SaveOptionType.Boolean, "Include 16x16", "1"));
            opts.Add(new SaveOption("24", SaveOptionType.Boolean, "Include 24x24", "1"));
            opts.Add(new SaveOption("32", SaveOptionType.Boolean, "Include 32x32", "1"));
            opts.Add(new SaveOption("48", SaveOptionType.Boolean, "Include 48x48", "1"));
            opts.Add(new SaveOption("64", SaveOptionType.Boolean, "Include 64x64", "1"));
            opts.Add(new SaveOption("128", SaveOptionType.Boolean, "Include 128x128", "1"));
            opts.Add(new SaveOption("256", SaveOptionType.Boolean, "Include 256x256", "1"));
            return opts.ToArray();
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Boolean makeSquare = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "SQR"));
            Boolean upscale = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "INC"));
            Boolean pixelZoom = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PIX"));
            Int32[] sizes = new Int32[] { 16, 24, 32, 48, 64, 128, 256 };
            Int32 nrOfSizes = sizes.Length;
            List<Int32> lstSizes = new List<Int32>();
            for (Int32 i = 0; i < nrOfSizes; ++i)
            {
                Int32 curSize = sizes[i];
                Boolean included = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, curSize.ToString(CultureInfo.InvariantCulture)));
                if (included)
                    lstSizes.Add(curSize);
            }
            if (lstSizes.Count == 0)
                throw new ArgumentException("The icon needs to contain at least one image!", "fileToSave");
            Bitmap bm = fileToSave.GetBitmap();
            using (MemoryStream ms = new MemoryStream())
            {
                ConvertToIcon(bm, ms, makeSquare, upscale, pixelZoom, lstSizes.ToArray());
                return ms.ToArray();
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
        public static Boolean ConvertToIcon(Bitmap inputBitmap, Stream output, Boolean makeSquare, Boolean upscale, Boolean pixelZoom, Int32[] sizes)
        {
            if (inputBitmap == null)
                throw new ArgumentNullException("inputBitmap", "Input bitmap cannot be null.");
            if (output == null)
                throw new ArgumentNullException("output", "Output stream cannot be null.");
            if (sizes.Length == 0)
                throw new ArgumentNullException("sizes", "Need at least one icon size!");

            List<Byte[]> images = new List<Byte[]>();
            List<Byte> widths = new List<Byte>();
            List<Byte> heights = new List<Byte>();
            Int32 maxDim = Math.Max(inputBitmap.Width, inputBitmap.Height);
            // Generate bitmaps for all the sizes and toss them in streams
            Int32 sizesLen = sizes.Length;
            for (Int32 i = 0; i < sizesLen; ++i)
            {
                Int32 size = Math.Min(sizes[i], 0x100);
                if (!upscale && size > maxDim)
                    continue;
                Int32 width = size;
                Int32 height = size;
                if (inputBitmap.Width <= inputBitmap.Height)
                    width = (Int32) (((Double) inputBitmap.Width / inputBitmap.Height) * size);
                else
                    height = (Int32) (((Double) inputBitmap.Height / inputBitmap.Width) * size);
                // These are 0 for "256"
                Byte saveWidth = (Byte) (Math.Min(makeSquare ? size : width, 0x100) & 0xFF);
                Byte saveHeight = (Byte) (Math.Min(makeSquare ? size : height, 0x100) & 0xFF);
                Boolean skip = false;
                Int32 imgCount = images.Count;
                for (Int32 si = 0; si < imgCount; ++si)
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
                Int32 nrOfImages = images.Length;
                Byte[][] pngImages = new Byte[nrOfImages][];
                for (Int32 i = 0; i < nrOfImages; ++i)
                    pngImages[i] = GetPngData(images[i]);
                ConvertImagesToIco(pngImages, ms);
                ms.Position = 0;
                return new Icon(ms);
            }
        }

        public static Byte[] ConvertImagesToIcoBytes(Image[] images)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Int32 nrOfImages = images.Length;
                Byte[][] pngImages = new Byte[nrOfImages][];
                for (Int32 i = 0; i < nrOfImages; ++i)
                    pngImages[i] = GetPngData(images[i]);
                ConvertImagesToIco(pngImages, ms);
                return ms.ToArray();
            }
        }

        public static void ConvertImagesToIco(Byte[][] pngImages, Stream output)
        {
            if (pngImages == null)
                throw new ArgumentNullException("pngImages");
            Int32 imgCount = pngImages.Length;
            if (imgCount == 0)
                throw new ArgumentException("No images given!", "pngImages");
            if (imgCount > 0xFFFF)
                throw new ArgumentException("Too many images!", "pngImages");
            using (BinaryWriter iconWriter = new BinaryWriter(new NonDisposingStream(output)))
            {
                Byte[][] frameBytes = new Byte[imgCount][];
                // 0-1 reserved, 0
                iconWriter.Write((Int16)0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((Int16)1);
                // 4-5 number of images
                iconWriter.Write((Int16)imgCount);
                // Calculate header size for first image data offset.
                Int32 offset = 6 + (16 * imgCount);
                for (Int32 i = 0; i < imgCount; ++i)
                {
                    // Get image data
                    Byte[] frameData = pngImages[i];
                    Int32 width = frameData[19] | frameData[18] << 8;
                    Int32 height = frameData[23] | frameData[22] << 8;
                    if (width > 256 || frameData[16] != 0 || frameData[17] != 0 || height > 256 || frameData[20] != 0 || frameData[21] != 0)
                        throw new ArgumentException("Image too large!", "pngImages");
                    // Get the colour depth to save in the icon info. This needs to be
                    // fetched explicitly, since png does not support certain types
                    // like 16bpp, so it will convert to the nearest valid on save.
                    Int32 bpp;
                    Byte colDepth = frameData[24];
                    Byte colType = frameData[25];
                    // I think .Net saving only supports colour types 2, 3 and 6 anyway.
                    switch (colType)
                    {
                        case 2: bpp = 3 * colDepth; break; // RGB
                        case 6: bpp = 4 * colDepth; break; // ARGB
                        default: bpp = colDepth; break; // Indexed & greyscale
                    }
                    Byte colors;
                    if (bpp > 8)
                        colors = 0;
                    else
                    {
                        Int32 plteOffset = PngHandler.FindPngChunk(frameData, "PLTE");
                        if (plteOffset == -1) // Should never happen...
                            throw new ArgumentException("Cannot convert image " + i + "!");
                        // Value 0 is interpreted as 256, so the cast reducing 256 to 0 is no problem.
                        colors = (Byte)(PngHandler.GetPngChunkDataLength(frameData, plteOffset) / 3);
                    }
                    frameBytes[i] = frameData;
                    Int32 imageLen = frameData.Length;
                    // Write image entry
                    // 0 image width. Value 0 is interpreted as 256, so the cast reducing 256 to 0 is no problem.
                    iconWriter.Write((Byte)width);
                    // 1 image height. Value 0 is interpreted as 256, so the cast reducing 256 to 0 is no problem.
                    iconWriter.Write((Byte)height);
                    // 2 number of colors.
                    iconWriter.Write(colors);
                    // 3 reserved
                    iconWriter.Write((Byte)0);
                    // 4-5 color planes
                    iconWriter.Write((Int16)0);
                    // 6-7 bits per pixel
                    iconWriter.Write((Int16)bpp);
                    // 8-11 size of image data
                    iconWriter.Write(imageLen);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += imageLen;
                }
                for (Int32 i = 0; i < imgCount; ++i)
                {
                    // Write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(frameBytes[i]);
                }
                iconWriter.Flush();
            }
        }

        private static Byte[] GetPngData(Image bitmap)
        {
            Byte[] data;
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
