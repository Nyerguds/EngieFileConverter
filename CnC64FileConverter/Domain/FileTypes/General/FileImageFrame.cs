using Nyerguds.ImageManipulation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImageFrame : FileImagePng
    {
        protected Int32 m_BitsPerColor= -1;
        protected Int32 m_ColorsInPalette = -1;
        protected Dictionary<String, Object> m_ExtraProps = new Dictionary<String, Object>();
        protected Boolean[] m_transparencyMask = null;
        public override String ShortTypeName { get { return "Frame"; } }

        public override FileClass InputFileClass { get { return FileClass.None; } }
        public Dictionary<String, Object> ExtraProps { get { return m_ExtraProps; }}

        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return (m_BaseType == null?  String.Empty : m_BaseType + " ") + "Frame"; } }
        public override Int32 BitsPerPixel { get { return m_BitsPerColor != -1   ? m_BitsPerColor : base.BitsPerPixel; } }
        public override Int32 ColorsInPalette { get { return m_ColorsInPalette != - 1 ? m_ColorsInPalette : base.ColorsInPalette; } }
        public override Boolean[] TransparencyMask { get { return m_transparencyMask; } }

        public void SetBitsPerColor(Int32 bitsPerColor) { this.m_BitsPerColor = bitsPerColor; }
        public void SetColorsInPalette(Int32 colorsInPalette) { this.m_ColorsInPalette = colorsInPalette; }
        public void SetTransparencyMask(Boolean[] transparencyMask) { this.m_transparencyMask = transparencyMask; }

        protected String sourcePath;
        protected String frameName;
        protected String m_BaseType;
        
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

        public void SetExtraInfo(String extraInfo)
        {
            this.ExtraInfo = extraInfo;
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
            else
            {
                LoadedFileName = null;
                LoadedFile = null;
            }
        }

        public void LoadFileFrame(SupportedFileType parent, String baseType, Bitmap image, String filename, Int32 frameNumber)
        {
            this.LoadFile(image, null);
            this.FrameParent = parent;
            this.m_BaseType = baseType;
            this.sourcePath = filename;
            // Set to -1 if it's actually loading from a frame file, so the automatic number adding is skipped.
            this.frameName = frameNumber >= 0 ? frameNumber.ToString("D5") : null;
            UpdateNames();
        }

    }
}
