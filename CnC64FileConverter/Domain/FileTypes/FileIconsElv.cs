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

    public class FileIconsElv : SupportedFileType
    {
        public override Int32 Width { get { return 48; } }
        public override Int32 Height { get { return 48; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "ElvIco"; } }
        public override String[] FileExtensions { get { return new String[] { "DAT" }; } }
        public override String ShortTypeDescription { get { return "Elvira ICON.DAT file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 1; } }
        protected FileImagePng[] m_TilesList = new FileImagePng[0];
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
        }
                
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            return SaveImg(fileToSave.GetBitmap());
        }

        protected void LoadFromFileData(Byte[] fileData)
        {
            if (fileData.Length < 0x48)
                throw new FileTypeLoadException("Not long enough.");
            if (fileData.Length % 0x48 != 0)
                throw new FileTypeLoadException("Not a multiple of 1-bit 24x24 tiles.");
            m_LoadedImage = ImageUtils.BuildImage(fileData, 24, fileData.Length / 3, 3, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);
        }

        protected Byte[] SaveImg(Bitmap image)
        {
            Bitmap hiColImage = ImageUtils.PaintOn32bpp(image, Color.Black);
            Int32 stride;
            Byte[] imageData32 = ImageUtils.GetImageData(hiColImage, out stride);
            return ImageUtils.Convert32bToGray(imageData32, image.Width, image.Height, 1, true, ref stride);
        }


    }
}