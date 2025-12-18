using Nyerguds.GameData.Dynamix;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileElvVgaFrames : SupportedFileType
    {
        public override Int32 Width { get { return hdrWidth; } }
        public override Int32 Height { get { return hdrHeight; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "ElvFrm"; } }
        public override String[] FileExtensions { get { return new String[] { "VGA" }; } }
        public override String ShortTypeDescription { get { return "Elvira VGA file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 4; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return true; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>If the type supports frames, this determines whether an overview-frame is available as index '-1'. If not, index 0 is accessed directly.</summary>
        public override Boolean RenderCompositeFrame { get { return false; } }


        //public FileFramesElv() { }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData);
            SetFileNames(filename);
            // For FileImageFrame this combines the frame name and the given path.
            foreach (SupportedFileType frame in this.m_FramesList)
                frame.SetFileNames(filename);
            
        }

        public override Boolean ColorsChanged()
        {
            return false;
        }
        
        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 16)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 firstNonEmpty = 0;
            Int32 headerEnd;
            while ((headerEnd = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, firstNonEmpty * 8, 4, false)) == 0)
                firstNonEmpty++;
            if (headerEnd < 0 || headerEnd >= fileData.Length || headerEnd % 8 != 0)
                throw new FileTypeLoadException("Invalid header length.");
            List<Int32> offsets = new List<Int32>();
            List<Int32> widths = new List<Int32>();
            List<Int32> heights = new List<Int32>();
            List<Boolean> compressedFlags = new List<Boolean>();
            Int32 readOffset = 0;

            while (readOffset + 8 < fileData.Length && readOffset < headerEnd)
            {
                Int32 dataOffset = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffset, 4, false);
                if (dataOffset < 0 || (dataOffset != 0 && dataOffset < headerEnd))
                    throw new FileTypeLoadException("Bad offset in header.");
                offsets.Add(dataOffset);
                Int32 imageHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffset + 4, 2, false);
                compressedFlags.Add((imageHeight & 0x8000) != 0);
                imageHeight = imageHeight & 0x7FFF;
                if (imageHeight < 0)
                    throw new FileTypeLoadException("Bad height in header.");
                heights.Add(imageHeight);
                Int32 imagewidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffset + 6, 2, false);
                if (imagewidth < 0)
                    throw new FileTypeLoadException("Bad width in header.");
                widths.Add(imagewidth);
                readOffset += 8;
            }

            Int32 frames = offsets.Count;
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(4, false, false);
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 imageOffset = offsets[i];
                Int32 imageHeight = heights[i];
                Int32 imageWidth = widths[i];
                Boolean compressed = compressedFlags[i];
                Int32 dataEnd;
                Int32 skip = 0;
                // Skip any 0 entries following this one to get the actual offset following this one,
                // to determine the data length to read.
                while ((dataEnd = (i + skip + 1 < frames ? offsets[i + skip + 1] : fileData.Length)) == 0)
                    skip++;
                Int32 dataSize = dataEnd - imageOffset;
                Int32 dataStride = ImageUtils.GetMinimumStride(imageWidth, 4);
                Int32 neededDataSize = imageHeight * dataStride;
                Bitmap curImage;

                if ((imageHeight == 0 && imageWidth == 0) || imageOffset == 0 || dataSize == 0)
                {
                    curImage = null;
                }
                else if (!compressed)
                {
                    if (neededDataSize > dataSize)
                        throw new FileTypeLoadException("Invalid data length.");
                    Byte[] data = new Byte[neededDataSize];
                    Array.Copy(fileData, imageOffset, data, 0, neededDataSize);
                    curImage = ImageUtils.BuildImage(data, imageWidth, imageHeight, dataStride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                }
                else
                {
                    Byte[] data = new Byte[dataSize];
                    Array.Copy(fileData, imageOffset, data, 0, dataSize);

                    Int32 stride = ImageUtils.GetMinimumStride(imageWidth, 4);
                    Byte[] outbuff = new Byte[stride * imageHeight];
                    AgosCompression.RleDecode(data, null, null, outbuff, imageHeight, stride);
                    curImage = ImageUtils.BuildImage(outbuff, imageWidth, imageHeight, stride, PixelFormat.Format4bppIndexed, this.m_Palette, null);
                }
                FileImageFrame frame = new FileImageFrame();
                String name = i.ToString("D5");
                frame.LoadFile(curImage, this.m_Palette.Length, null);
                frame.SetFrameFileName(name);
                frame.FrameParent = this;
                if (curImage == null)
                    frame.SetBitsPerColor(4);
                this.m_FramesList[i] = frame;
            }
        }
        
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            if (!fileToSave.ContainsFrames)
                throw new NotSupportedException("Elvira VGA saving for single frame is not supported!");

            Int32 nrOfFr = fileToSave.Frames.Length;
            Byte[][] data = new Byte[nrOfFr][];
            Int32[] offsets = new Int32[nrOfFr];
            Int32[] widths = new Int32[nrOfFr];
            Int32[] heights = new Int32[nrOfFr];
            Boolean[] compressed = new Boolean[nrOfFr];
            Int32 offset = nrOfFr*8;
            for (Int32 i = 0; i < nrOfFr; i++)
            {
                SupportedFileType frame = fileToSave.Frames[i];
                Bitmap image = frame.GetBitmap();
                if (image == null)
                {
                    // Save empty frame
                    // This code is technically not needed since the arrays get initialised on these values.
                    data[i] = null;
                    widths[i] = 0;
                    heights[i] = 0;
                    compressed[i] = false;
                    offsets[i] = 0;
                }
                else if (frame.BitsPerColor != 4)
                    throw new NotSupportedException("Elvira VGA frames need to be 4 BPP files!");
                else
                {
                    Int32 width = image.Width;
                    Int32 height = image.Height;
                    Int32 stride;
                    Byte[] byteData = ImageUtils.GetImageData(image, out stride);
                    if (stride > ImageUtils.GetMinimumStride(width, 4))
                    {
                        // Collapse any extra bytes. Probably not needed, but you never know.
                        // Can't be arsed to loop over the lines myself, so I'm just using this:
                        byteData = ImageUtils.ConvertTo8Bit(byteData, width, height, 0, 4, false, ref stride);
                        byteData = ImageUtils.ConvertFrom8Bit(byteData, width, height, 4, false, ref stride);
                    }
                    data[i] = byteData;
                    compressed[i] = false;
                    if (!dontCompress)
                    {
                        Byte[] dataCompr = AgosCompression.RleEncode(byteData, height, stride);
                        if (dataCompr.Length < byteData.Length)
                        {
                            data[i] = dataCompr;
                            compressed[i] = true;
                        }
                    }
                    widths[i] = width;
                    heights[i] = height;
                    offsets[i] = offset;
                    offset += data[i].Length;
                }
            }
            Byte[] finalFile = new Byte[offset];
            for (Int32 i = 0; i < nrOfFr; i++)
            {
                Int32 indexOffset = i * 8;
                ArrayUtils.WriteIntToByteArray(finalFile, indexOffset, 4, false, (UInt32)offsets[i]);
                Int32 height = heights[i];
                if (compressed[i])
                    height |= 0x8000;
                ArrayUtils.WriteIntToByteArray(finalFile, indexOffset + 4, 2, false, (UInt32)height);
                ArrayUtils.WriteIntToByteArray(finalFile, indexOffset + 6, 2, false, (UInt32)widths[i]);
                if (data[i] != null)
                    Array.Copy(data[i], 0, finalFile, offsets[i], data[i].Length);
            }
            return finalFile;
        }
        
    }
}