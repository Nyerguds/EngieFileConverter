using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;
using Windows.Graphics2d;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileIcon : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.Image; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_MaxWidth; } }
        public override Int32 Height { get { return this.m_MaxHeight; } }
        protected Int32 m_MaxWidth;
        protected Int32 m_MaxHeight;
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
            HeaderParseException hpe = null;
            try
            {
                Int32 hdrSize = Marshal.SizeOf(typeof (ICONDIR));
                if (fileData.Length < hdrSize)
                    throw new HeaderParseException("Not long enough for header.");
                ICONDIR hdr = ArrayUtils.StructFromByteArray<ICONDIR>(fileData);
                if (hdr.Reserved != 0)
                    throw new HeaderParseException("Invalid values in header.");
                UInt32 nrOfImages = hdr.NumberOfImages;
                Int32 indexItemSize = Marshal.SizeOf(typeof (ICONDIRENTRY));
                if (fileData.Length < hdrSize + nrOfImages*indexItemSize)
                    throw new HeaderParseException("Not long enough for images index.");
                if (nrOfImages == 0)
                    throw new HeaderParseException("No images in given icon.");
                Int32 offset = hdrSize;
                this.m_FramesList = new SupportedFileType[nrOfImages];
                for (Int32 i = 0; i < nrOfImages; i++)
                {
                    ICONDIRENTRY info = ArrayUtils.ReadStructFromByteArray<ICONDIRENTRY>(fileData, offset);
                    UInt32 imageOffset = info.ImageOffset;
                    UInt32 imageLength = info.ImageLength;
                    if (imageOffset + imageLength > fileData.Length)
                        throw new HeaderParseException("Bad header data: offset and length for image " + i + " do not fit in file.");
                    Byte[] frameData = new Byte[imageLength];
                    Array.Copy(fileData, imageOffset, frameData, 0, imageLength);
                    String type = MimeTypeDetector.GetMimeType(frameData)[0];
                    Bitmap bmp;
                    Int32 frWidth = info.Width == 0 ? 0x100 : info.Width;
                    Int32 frHeight = info.Height == 0 ? 0x100 : info.Height;
                    if (frWidth == 0 || frHeight == 0)
                        throw new HeaderParseException("Icon dimensions cannot be zero.");
                    if ("png".Equals(type))
                        bmp = this.GetBmp<FileImagePng>(frameData);
                    else if ("bmp".Equals(type))
                        bmp = this.GetBmp<FileImageBmp>(frameData);
                    else
                    {
                        bmp = DibHandler.ImageFromDib(frameData);
                        type = "dib";
                    }
                    //throw new HeaderParseException("Unsupported image type " + ("dat".Equals(type) ? String.Empty : "\"" + type + "\" ") + "in frame " + i + ".");
                    if (bmp.Width != frWidth || bmp.Height != frHeight)
                        throw new HeaderParseException("Image " + i + " in icon does not match header information.");
                    this.m_MaxHeight = Math.Max(this.m_MaxHeight, frHeight);
                    this.m_MaxWidth = Math.Max(this.m_MaxWidth, frWidth);
                    FileImageFrame framePic = new FileImageFrame();
                    framePic.LoadFileFrame(this, this.ShortTypeName, bmp, sourcePath, i);
                    framePic.SetExtraInfo("Format: " + type.ToUpper());
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
                    framePic.LoadFileFrame(this, this.ShortTypeName, ImageUtils.CloneImage(bm), sourcePath, -1);
                    framePic.SetFileNames(sourcePath);
                    framePic.SetBitsPerColor(this.BitsPerPixel);
                    framePic.SetColorsInPalette(this.ColorsInPalette);
                    framePic.SetTransparencyMask(this.TransparencyMask);
                    //framePic.SetExtraInfo();
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
            using (T frameImgPng = new T())
            {
                frameImgPng.LoadFile(frameData);
                return ImageUtils.CloneImage(frameImgPng.GetBitmap());
            }
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Bitmap bmToSave = fileToSave.GetBitmap();
            Int32 w = bmToSave.Width;
            Int32 h = bmToSave.Height;
            Boolean addSq = w != h;
            Boolean addInc = Math.Max(w, h) < 256;
            //Boolean makeSquare, Boolean upscale, Boolean smooth
            List<SaveOption> opts = new List<SaveOption>();
            if (addSq)
                opts.Add(new SaveOption("SQR", SaveOptionType.Boolean, "Pad frames to square formats", null, "1"));
            if (addInc)
            {
                opts.Add(new SaveOption("INC", SaveOptionType.Boolean, "Include formats larger than source image", "1"));
                opts.Add(new SaveOption("PIX", SaveOptionType.Boolean, "Use pixel zoom for larger images", "0"));
            }
            return opts.ToArray();
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            Boolean makeSquare = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "SQR"));
            Boolean upscale = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "INC"));
            Boolean pixelZoom = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PIX"));
            Bitmap bm = fileToSave.GetBitmap();
            using (MemoryStream ms = new MemoryStream())
            {
                ConvertToIcon(bm, ms, makeSquare, upscale, pixelZoom);
                return ms.ToArray();
            }
        }


        /// <summary>
        /// Converts a PNG image to a icon (ico) with all the sizes windows likes
        /// </summary>
        /// <param name="inputBitmap">The input bitmap</param>
        /// <param name="output">The output stream</param>
        /// <param name="makeSquare">True to pad the top and bottom of the icons with transparency to make the saved icons square.</param>
        /// <param name="upscale">True to also save the image in sizes larger than the original image.</param>
        /// <param name="pixelZoom">Use pixel scaling for resizing to sizes larger than the original image.</param>
        /// <returns>Wether or not the icon was succesfully generated</returns>
        public static Boolean ConvertToIcon(Bitmap inputBitmap, Stream output, Boolean makeSquare, Boolean upscale, Boolean pixelZoom)
        {
            if (inputBitmap == null)
                throw new ArgumentNullException("inputBitmap", "Input bitmap cannot be null.");
            if (output == null)
                throw new ArgumentNullException("output", "Output stream cannot be null.");
            
            List<Byte[]> images = new List<Byte[]>();
            Int32[] sizes = new Int32[] {16, 32, 48, 256 };
            List<Byte> widths = new List<Byte>();
            List<Byte> heights = new List<Byte>();
            List<Int32> bpp = new List<Int32>();
            Int32 maxDim = Math.Max(inputBitmap.Width, inputBitmap.Height);
            // Generate bitmaps for all the sizes and toss them in streams
            for (Int32 i = 0; i < sizes.Length; i++)
            {
                Int32 size = Math.Min(sizes[i], 0x100);
                if (!upscale && size > maxDim)
                    continue;
                Int32 width = size;
                Int32 height = size;
                if (inputBitmap.Width <= inputBitmap.Height)
                    width = (Int32) (((Double) inputBitmap.Width/inputBitmap.Height)*size);
                else
                    height = (Int32) (((Double) inputBitmap.Height/inputBitmap.Width)*size);
                // These are 0 for "256"
                Byte saveWidth = (Byte)(Math.Min(makeSquare ? size : width, 0x100) & 0xFF);
                Byte saveHeight = (Byte)(Math.Min(makeSquare ? size : height, 0x100) & 0xFF);
                Boolean skip = false;
                for (Int32 si = 0; si < images.Count; si++)
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
                {
                    images.Add(GetPngData(newBitmap));
                    bpp.Add(Image.GetPixelFormatSize(newBitmap.PixelFormat));
                }
            }
            using (BinaryWriter iconWriter = new BinaryWriter(output))
            {
                Int32 offset = 0;

                // 0-1 reserved, 0
                iconWriter.Write((Byte) 0);
                iconWriter.Write((Byte) 0);
                // 2-3 image type, 1 = icon, 2 = cursor
                iconWriter.Write((Int16) 1);
                // 4-5 number of images
                iconWriter.Write((Int16)images.Count);
                offset += 6 + (16 * images.Count);

                for (Int32 i = 0; i < images.Count; i++)
                {
                    // image entry 1
                    // 0 image width
                    iconWriter.Write(widths[i]); // is 0 for "256"
                    // 1 image height
                    iconWriter.Write(heights[i]);
                    // 2 number of colors
                    iconWriter.Write((Byte) 0);
                    // 3 reserved
                    iconWriter.Write((Byte) 0);
                    // 4-5 color planes
                    iconWriter.Write((Int16) 0);
                    // 6-7 bits per pixel
                    iconWriter.Write((Int16)bpp[i]);
                    // 8-11 size of image data
                    iconWriter.Write(images[i].Length);
                    // 12-15 offset of image data
                    iconWriter.Write(offset);
                    offset += images[i].Length;
                }
                foreach (Byte[] img in images)
                {
                    // write image data
                    // png data must contain the whole png data file
                    iconWriter.Write(img);
                }
                iconWriter.Flush();
            }
            return true;
        }

        private static Byte[] GetPngData(Bitmap bitmap)
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
