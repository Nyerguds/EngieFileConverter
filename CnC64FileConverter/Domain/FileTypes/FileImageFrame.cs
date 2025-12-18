using Nyerguds.ImageManipulation;
using System;
using System.Drawing;
using System.IO;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImageFrame : FileImagePng
    {
        protected Int32 m_BitsPerColor= -1;
        protected Int32 m_ColorsInPalette = 0;
        public override String ShortTypeName { get { return "ImageFrm"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Frame"; } }
        public override Int32 BitsPerColor { get { return m_BitsPerColor != -1   ? m_BitsPerColor : base.BitsPerColor; } }
        public override Int32 ColorsInPalette { get { return m_ColorsInPalette != - 1? m_ColorsInPalette : base.ColorsInPalette; } }
        public override SupportedFileType PreferredExportType { get { return new FileImagePng(); } }
        
        public void SetBitsPerColor(Int32 bitsPerColor) { m_BitsPerColor = bitsPerColor; }
        public void SetColorsInPalette(Int32 colorsInPalette) { m_ColorsInPalette = colorsInPalette; }

        protected String sourcePath;
        protected String frameName;
        
        public void SetFrameFileName(String frameName)
        {
            this.frameName = frameName;
            UpdateNames();
        }

        public override void SetFileNames(String path)
        {
            sourcePath = path;
            UpdateNames();
        }

        protected void UpdateNames()
        {
            if (this.frameName != null && this.sourcePath != null)
            {
                LoadedFileName = Path.GetFileNameWithoutExtension(sourcePath) + "-" + frameName + Path.GetExtension(sourcePath);
                LoadedFile = Path.Combine(Path.GetDirectoryName(sourcePath), LoadedFileName);
            }
            else if (this.frameName != null)
            {
                base.SetFileNames(this.frameName);
            }
            else if (this.sourcePath != null)
            {
                base.SetFileNames(this.sourcePath);
            }
        }
    }
}
