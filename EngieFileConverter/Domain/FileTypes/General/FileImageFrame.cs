using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageFrame : FileImagePng
    {
        protected Int32 m_BitsPerColor= -1;
        protected Int32 m_ColorsInPalette = -1;
        protected Dictionary<String, Object> m_ExtraProps = new Dictionary<String, Object>();
        //protected Boolean[] m_transparencyMask = null;
        public override String ShortTypeName { get { return "Frame"; } }

        public override FileClass InputFileClass { get { return FileClass.None; } }
        public Dictionary<String, Object> ExtraProps { get { return this.m_ExtraProps; }}

        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return m_Description ?? ((this.m_BaseType == null ? String.Empty : this.m_BaseType + " ") + "Frame"); } }
        public override Int32 BitsPerPixel { get { return this.m_BitsPerColor != -1   ? this.m_BitsPerColor : base.BitsPerPixel; } }
        public override FileClass FileClass  { get { return this.m_FileClass ?? base.FileClass; } }
        public override Int32 ColorsInPalette { get { return this.m_ColorsInPalette != - 1 ? this.m_ColorsInPalette : base.ColorsInPalette; } }
        //public override Boolean[] TransparencyMask { get { return this.m_transparencyMask; } }

        public void SetBitsPerColor(Int32 bitsPerColor) { this.m_BitsPerColor = bitsPerColor; }
        public void SetFileClass(FileClass? fileClass) { this.m_FileClass = fileClass; }
        
        public void SetColorsInPalette(Int32 colorsInPalette) { this.m_ColorsInPalette = colorsInPalette; }
        //public void SetTransparencyMask(Boolean[] transparencyMask) { this.m_transparencyMask = transparencyMask; }

        protected String sourcePath;
        protected String frameName;
        protected String m_BaseType;
        protected String m_Description;
        protected FileClass? m_FileClass;
        
        public void SetFrameFileName(String frameName)
        {
            this.frameName = frameName;
            this.UpdateNames();
        }

        public override void SetFileNames(String path)
        {
            this.sourcePath = path;
            this.UpdateNames();
        }

        public void SetExtraInfo(String extraInfo)
        {
            this.ExtraInfo = extraInfo;
        }

        protected void UpdateNames()
        {
            if (this.frameName != null && this.sourcePath != null)
            {
                this.LoadedFileName = Path.GetFileNameWithoutExtension(this.sourcePath) + "-" + this.frameName + Path.GetExtension(this.sourcePath);
                this.LoadedFile = Path.Combine(Path.GetDirectoryName(this.sourcePath), this.LoadedFileName);
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
                this.LoadedFileName = null;
                this.LoadedFile = null;
            }
        }

        /// <summary>
        /// Loads a frame as frame type of a parent class.
        /// </summary>
        /// <param name="parent">Parent object which contains this frame.</param>
        /// <param name="typeParent">Parent type. The final description will be this type's ShortTypeName with "frame" added behind it.</param>
        /// <param name="image">The image.</param>
        /// <param name="filename">The filename this is loaded from.</param>
        /// <param name="frameNumber">The frame number.</param>
        public void LoadFileFrame(SupportedFileType parent, SupportedFileType typeParent, Bitmap image, String filename, Int32 frameNumber)
        {
            this.LoadFile(image, null);
            this.FrameParent = parent;
            this.m_BaseType = typeParent is FileFrames ? null : typeParent.ShortTypeName;
            this.sourcePath = filename;
            // Set to -1 if it's actually loading from a frame file, so the automatic number adding is skipped.
            this.frameName = frameNumber >= 0 ? frameNumber.ToString("D5") : null;
            this.UpdateNames();
        }


        /// <summary>
        /// Loads a frame with specific set type description
        /// </summary>
        /// <param name="parent">Parent object which contains this frame.</param>
        /// <param name="shortTypeDescription">short type description.</param>
        /// <param name="image">The image.</param>
        /// <param name="filename">The filename this is loaded from.</param>
        /// <param name="frameNumber">The frame number.</param>
        public void LoadFileFrame(SupportedFileType parent, String shortTypeDescription, Bitmap image, String filename, Int32 frameNumber)
        {
            this.LoadFile(image, null);
            this.FrameParent = parent;
            this.m_Description = shortTypeDescription;
            this.sourcePath = filename;
            // Set to -1 if it's actually loading from a frame file, so the automatic number adding is skipped.
            this.frameName = frameNumber >= 0 ? frameNumber.ToString("D5") : null;
            this.UpdateNames();
        }

    }
}
