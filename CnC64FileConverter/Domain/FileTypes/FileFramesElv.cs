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

    public class FileFramesElv : SupportedFileType
    {
        public override Int32 Width { get { return hdrWidth; } }
        public override Int32 Height { get { return hdrHeight; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "ElvImg"; } }
        public override String[] FileExtensions { get { return new String[] { "VGA" }; } }
        public override String ShortTypeDescription { get { return "Elvira VGA file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 4; } }
        protected FileImagePng[] m_TilesList = new FileImagePng[0];

        /// <summary>Enables frame controls on the UI.</summary>
        public override Boolean ContainsFrames { get { return m_TilesList.Length > 1; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return m_TilesList.Cast<SupportedFileType>().ToArray(); } }

        protected Color[] m_palette;

        public override Color[] GetColors()
        {
            // ensures the UI can show the partial palette.
            return m_palette == null ? null : m_palette.ToArray();
        }
        
        public override void SetColors(Color[] palette)
        {
            Int32 paletteLength = 1 << this.BitsPerColor;
            Color[] pal = new Color[paletteLength];
            for (Int32 i = 0; i < paletteLength; i++)
            {
                if (i < palette.Length)
                    pal[i] = Color.FromArgb(0xFF, palette[i]);
                else
                    pal[i] = Color.Empty;
            }
            this.m_palette = pal;
            if (m_LoadedImage == null)
                return;
            ColorPalette imagePal = this.m_LoadedImage.Palette;
            Int32 entries = imagePal.Entries.Length;
            for (Int32 i = 0; i < entries; i++)
            {
                if (i < palette.Length)
                    imagePal.Entries[i] = Color.FromArgb(0xFF, palette[i]);
                else
                    imagePal.Entries[i] = Color.Empty;
            }
            this.m_LoadedImage.Palette = imagePal;
            foreach (FileImagePng tile in this.m_TilesList)
            {
                if (tile !=null)
                    tile.SetColors(imagePal.Entries);
            }
        }

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
        }
        
        public override Boolean ColorsChanged()
        {
            return false;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave)
        {
            return SaveImg(fileToSave.GetBitmap());
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 16)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 headerEnd = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 8, 4, false);
            if (headerEnd < 0 || headerEnd > fileData.Length || headerEnd % 8 != 0)
                throw new FileTypeLoadException("Invalid header length.");
            List<Int32> offsets = new List<Int32>();
            List<Int32> widths = new List<Int32>();
            List<Int32> heights = new List<Int32>();
            List<Boolean> bitFlags = new List<Boolean>();
            Int32 lowestImageOffset = fileData.Length;
            Int32 offset = 0;
            Int32 currentOffset=fileData.Length;            
            try
            {
                while (offset + 8 < fileData.Length && offset < lowestImageOffset)
                {
                    currentOffset = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset, 4, false);
                    if (currentOffset < 0)
                        throw new FileTypeLoadException("Bad offset in header.");
                    offsets.Add(currentOffset);
                    Int32 imageHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 2, false);
                    bitFlags.Add((imageHeight & 0x8000) != 0);
                    imageHeight = imageHeight & 0x7FFF;
                    if (imageHeight < 0)
                        throw new FileTypeLoadException("Bad height in header.");
                    heights.Add(imageHeight);
                    Int32 imagewidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 6, 2, false);
                    if (imagewidth < 0)
                        throw new FileTypeLoadException("Bad width in header.");
                    widths.Add(imagewidth);
                    if (currentOffset != 0)
                        lowestImageOffset = Math.Min(lowestImageOffset, currentOffset);
                    offset+=8;
                }
            
                Int32 frames = offsets.Count;
                m_TilesList = new FileImagePng[frames];
                m_palette = PaletteUtils.GenerateGrayPalette(4, false, false);
                for (Int32 i = 0; i < frames; i++)
                {
                    Int32 imageOffset = offsets[i];
                    Int32 imageHeight = heights[i];
                    Int32 imageWidth = widths[i];
                    Bitmap curImage;

                    if (imageHeight == 0 && imageWidth == 0)
                    {
                        curImage = new Bitmap(1, 1, PixelFormat.Format4bppIndexed);
                        curImage.Palette = BitmapHandler.GetPalette(m_palette);
                    }
                    else if (!bitFlags[i])
                    {
                        Int32 dataStride = ImageUtils.GetMinimumStride(imageWidth, 4);
                        Int32 dataSize = imageHeight * dataStride;
                        if (imageOffset + dataSize > fileData.Length)
                            throw new FileTypeLoadException("Invalid data length.");
                        Byte[] data = new Byte[dataSize];
                        Array.Copy(fileData, imageOffset, data, 0, dataSize);
                        curImage = ImageUtils.BuildImage(data, imageWidth, imageHeight, dataStride, PixelFormat.Format4bppIndexed, m_palette, null);
                    }
                    else
                    {
                        curImage = new Bitmap(imageWidth, imageHeight, PixelFormat.Format4bppIndexed);
                        curImage.Palette = BitmapHandler.GetPalette(m_palette);
                    }
                    this.m_TilesList[i] = new FileImagePng();
                    this.m_TilesList[i].LoadFile(curImage, m_palette.Length, "frame" + i.ToString("000") + ".png");
                    if (m_LoadedImage == null && imageHeight != 0 && imageWidth != 0 && !bitFlags[i])
                    {
                        hdrWidth = imageWidth;
                        hdrHeight = imageHeight;
                        m_LoadedImage = curImage;
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        protected Byte[] SaveImg(Bitmap image)
        {
            return null;
        }


    }
}