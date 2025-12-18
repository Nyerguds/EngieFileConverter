using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CnC64FileConverter.Domain.FileTypes
{
    public abstract class SupportedFileType : FileTypeBroadcaster
    {
        protected Bitmap m_LoadedImage;
        protected Color[] m_BackupPalette = null;
        protected Int32 m_CompositeFrameTilesWidth = 1;

        /// <summary>Very short code name for this type.</summary>
        public virtual String ShortTypeName { get { return FileExtensions.Length > 0 ? FileExtensions[0].ToUpper() : this.GetType().Name; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public abstract String ShortTypeDescription { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        public abstract String[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public virtual String[] DescriptionsForExtensions { get { return Enumerable.Repeat(this.ShortTypeDescription, this.FileExtensions.Length).ToArray(); } }
        /// <summary>Width of the file (if applicable). Normally the same as GetBitmap().Width</summary>
        public virtual Int32 Width { get { return m_LoadedImage == null ? 0 : m_LoadedImage.Width; } }
        /// <summary>Height of the file (if applicable). Normally the same as GetBitmap().Height</summary>
        public virtual Int32 Height { get { return m_LoadedImage == null ? 0 : m_LoadedImage.Height; } }
        /// <summary>Amount of colors in the palette that is contained inside the image. 0 if the image itself does not contain a palette, even if it generates one.</summary>
        public virtual Int32 ColorsInPalette { get { return this.m_LoadedImage == null? 0 : m_LoadedImage.Palette.Entries.Length; } }
        /// <summary>Type for quick-converting this type.</summary>
        public virtual SupportedFileType PreferredExportType { get { return new FileImagePng(); } }
        /// <summary>Full path of the loaded file.</summary>
        public String LoadedFile { get; protected set; }
        /// <summary>Display string to show on the UI which file was loaded (no path).</summary>
        public String LoadedFileName { get; protected set; }
        public virtual Int32 BitsPerColor { get { return m_LoadedImage == null ? 0 : Image.GetPixelFormatSize(m_LoadedImage.PixelFormat); } }

        /// <summary>Enables frame controls on the UI.</summary>
        public virtual Boolean ContainsFrames { get { return false; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public virtual SupportedFileType[] Frames { get { return null; } }
        /// <summary>If the type supports frames, this determines whether an overview-frame is available as index '-1'. If not, index 0 is accessed directly.</summary>
        public virtual Boolean RenderCompositeFrame { get { return true; } }
        /// <summary>Sets the width of the composite frame image, and re-renders the image with the given width.</summary>
        public Int32 CompositeFrameTilesWidth
        {
            get { return this.m_CompositeFrameTilesWidth; }
            set
            {
                this.m_CompositeFrameTilesWidth = value;
                if (this.ContainsFrames)
                    BuildFullImage();
            }
        }
        protected virtual void BuildFullImage() { }

        public abstract void LoadFile(Byte[] fileData);
        public abstract void LoadFile(String filename);
        public abstract void SaveAsThis(SupportedFileType fileToSave, String savePath);

        protected void SetFileNames(String path)
        {
            LoadedFile = path;
            LoadedFileName = Path.GetFileName(path);
        }

        public virtual Color[] GetColors()
        {
            if (m_LoadedImage == null)
                return new Color[0];
            return m_LoadedImage.Palette.Entries.ToArray();
        }

        public virtual void SetColors(Color[] palette)
        {
            if (palette == null || palette.Length == 0)
                return;
            // Override this in types that don't have a palette, like grayscale N64 images.
            // This function should only be called from UI "if (BitsPerColor != 0 && !FileHasPalette)"
            if (this.BitsPerColor == 0)
                throw new NotSupportedException("This image does not support palettes.");
            // Not gonna execute this check, since this basic function can't actually set the palette.
            // else if (this.FileHasPalette)
            Int32 pfs = Image.GetPixelFormatSize(this.m_LoadedImage.PixelFormat);
            if (pfs > 8 || (this.m_LoadedImage.PixelFormat & PixelFormat.Indexed) == 0
                || this.m_LoadedImage.Palette.Entries.Length == 0)
                throw new NotSupportedException("This image has no palette.");
            ColorPalette cp = this.m_LoadedImage.Palette;
            if (m_BackupPalette == null)
                m_BackupPalette = GetColors();
            for (Int32 i = 0; i < cp.Entries.Length; i++)
            {
                if (palette.Length > i)
                    cp.Entries[i] = palette[i];
                else
                    cp.Entries[i] = Color.Empty;
            }
            m_LoadedImage.Palette = cp;
        }

        public virtual void ResetColors()
        {
            SetColors(m_BackupPalette);
        }

        public virtual Boolean ColorsChanged()
        {
            if (this.BitsPerColor == 0)
                return false;
            Int32 pfs = Image.GetPixelFormatSize(this.m_LoadedImage.PixelFormat);
            if (pfs > 8 || (this.m_LoadedImage.PixelFormat & PixelFormat.Indexed) == 0
                || this.m_LoadedImage.Palette.Entries.Length == 0)
                return false;
            Color[] cols = GetColors();
            // assume there's no palette, or no backup was ever made
            if (cols == null || m_BackupPalette == null)
                return false;
            return !GetColors().SequenceEqual(m_BackupPalette);
        }

        public virtual Bitmap GetBitmap()
        {
            return m_LoadedImage;
        }

        private static Type[] m_supportedOpenTypes =
        {
            typeof(FileImgN64),
            typeof(FileMapN64),
            typeof(FileMapPc),
            typeof(FileImage),
            typeof(FileImgLCW),
            typeof(FileTilesN64Bpp4),
            typeof(FileTilesN64Bpp8),
            typeof(FileTilesetPC),
            typeof(FilePalettePc),
            typeof(FilePaletteN64),
        };

        private static Type[] m_autoDetectTypes =
        {
            typeof(FileImgN64Gray),
            typeof(FileImgN64Standard),
            typeof(FileMapN64),
            typeof(FileMapPc),
            typeof(FileMapPcFromIni),
            typeof(FileMapN64FromIni),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImgLCW),
            typeof(FileTilesN64Bpp4),
            typeof(FileTilesN64Bpp8),
            typeof(FileTilesetPC),
            typeof(FilePalettePc),
            typeof(FilePaletteN64Pa8),
            typeof(FilePaletteN64Pa4),
        };
        private static Type[] m_supportedSaveTypes =
        {
            typeof(FileImgN64Standard),
            typeof(FileImgN64Jap),
            typeof(FileImgN64Gray),
            typeof(FileMapN64),
            typeof(FileMapPc),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImgLCW),
            typeof(FileTilesetPC),
            typeof(FilePalettePc),
            typeof(FilePaletteN64Pa4),
            typeof(FilePaletteN64Pa8),
        };

        static SupportedFileType()
        {
            CheckTypes(SupportedOpenTypes);
            CheckTypes(SupportedSaveTypes);
            CheckTypes(AutoDetectTypes);
        }

        private static void CheckTypes(Type[] types)
        {
            Type imgType = typeof(SupportedFileType);
            foreach (Type t in types)
                if (!t.IsSubclassOf(imgType))
                    throw new Exception("Entries in autoDetectTypes list must all be FontFile classes!");
        }

        public static Type[] SupportedOpenTypes { get { return m_supportedOpenTypes.ToArray(); } }
        public static Type[] SupportedSaveTypes { get { return m_supportedSaveTypes.ToArray(); } }
        public static Type[] AutoDetectTypes { get { return m_autoDetectTypes.ToArray(); } }
        
        public static SupportedFileType LoadImageAutodetect(String path, SupportedFileType[] possibleTypes, out List<FileTypeLoadException> loadErrors)
        {
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (possibleTypes == null)
                possibleTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, path);
            foreach (SupportedFileType typeObj in possibleTypes)
            {
                try
                {
                    typeObj.LoadFile(path);
                    return typeObj;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = typeObj.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            foreach (Type type in SupportedFileType.AutoDetectTypes)
            {
                Boolean knownType = false;
                foreach (SupportedFileType typeObj in possibleTypes)
                {
                    if (typeObj.GetType() == type)
                    {
                        knownType = true;
                        break;
                    }
                }
                if (knownType)
                    continue;
                SupportedFileType objInstance = null;
                try
                {
                    objInstance = (SupportedFileType)Activator.CreateInstance(type);
                }
                catch { /* Ignore; programmer error. */ }
                if (objInstance == null)
                    continue;
                try
                {
                    objInstance.LoadFile(path);
                    return objInstance;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = objInstance.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            return null;
        }
    }

}