using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;

namespace CnC64FileConverter.Domain.ImageFile
{
    public abstract class N64FileType : FileTypeBroadcaster
    {
        /// <summary>Very short code name for this type.</summary>
        public virtual String ShortTypeName { get { return FileExtensions.Length > 0 ? FileExtensions[0].ToUpper() : this.GetType().Name; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public abstract String ShortTypeDescription { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        public abstract String[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public virtual String[] DescriptionsForExtensions { get { return Enumerable.Repeat(this.ShortTypeDescription, this.FileExtensions.Length).ToArray(); } }
        /// <summary>Is this file format treated as an image with a color palette?</summary>
        public virtual Boolean FileHasPalette { get { return loadedImage.PixelFormat == PixelFormat.Format8bppIndexed || loadedImage.PixelFormat == PixelFormat.Format4bppIndexed; } }
        /// <summary>Width of the file (if applicable). Normally the same as GetBitmap().Width</summary>
        public virtual Int32 Width { get { return loadedImage.Width; } }
        /// <summary>Height of the file (if applicable). Normally the same as GetBitmap().Height</summary>
        public virtual Int32 Height { get { return loadedImage.Height; } }
        /// <summary>Amount of colors in the palette.</summary>
        public virtual Int32 ColorsInPalette { get { return this.FileHasPalette ? loadedImage.Palette.Entries.Length : 0; } }
        public virtual N64FileType PreferredExportType { get { return new FileImagePng(); } }
        
        public abstract void LoadImage(Byte[] fileData);
        public abstract void LoadImage(String filename);
        public abstract void SaveAsThis(N64FileType fileToSave, String savePath);

        public virtual Int32 GetBitsPerColor()
        {
            return Image.GetPixelFormatSize(loadedImage.PixelFormat);
        }

        public virtual Color[] GetColors()
        {
            if (!this.FileHasPalette)
                return new Color[0];
            Color[] col1 = loadedImage.Palette.Entries;
            Color[] col2 = new Color[ColorsInPalette];
            Array.Copy(col1, col2, Math.Min(col1.Length, ColorsInPalette));
            return col2;
        }

        public virtual Bitmap GetBitmap()
        {
            return loadedImage;
        }

        protected Bitmap loadedImage;

        private static Type[] m_supportedOpenTypes =
        {
            typeof(FileImgN64),
            typeof(FileMapN64),
            typeof(FileMapPc),
            typeof(FileImage),
            typeof(FileTilesN64Bpp4),
            typeof(FileTilesN64Bpp8),
            typeof(FilePalettePc),
            typeof(FilePaletteN64),
        };

        private static Type[] m_autoDetectTypes =
        {
            typeof(FileImgN64Gray),
            typeof(FileImgN64Basic1),
            typeof(FileImgN64Basic2),
            typeof(FileMapN64),
            typeof(FileMapPc),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileTilesN64Bpp4),
            typeof(FileTilesN64Bpp8),
            typeof(FilePalettePc),
            typeof(FilePaletteN64Pa8),
            typeof(FilePaletteN64Pa4),
        };
        private static Type[] m_supportedSaveTypes =
        {
            typeof(FileImgN64Basic1),
            typeof(FileImgN64Basic2),
            typeof(FileImgN64Gray),
            typeof(FileMapN64),
            typeof(FileMapPc),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FilePalettePc),
            typeof(FilePaletteN64Pa4),
            typeof(FilePaletteN64Pa8),
        };
                
        public static Type[] SupportedOpenTypes { get { return m_supportedOpenTypes.ToArray(); } }
        public static Type[] SupportedSaveTypes { get { return m_supportedSaveTypes.ToArray(); } }
        public static Type[] AutoDetectTypes { get { return m_autoDetectTypes.ToArray(); } }
        
        public static N64FileType LoadImageAutodetect(String path, N64FileType[] possibleTypes, out List<FileTypeLoadException> loadErrors)
        {
            Type imgType = typeof(N64FileType);
            foreach (Type t in N64FileType.AutoDetectTypes)
                if (!t.IsSubclassOf(imgType))
                    throw new Exception("Entries in autoDetectTypes list must all be FontFile classes!");
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (possibleTypes == null)
                possibleTypes = FileDialogGenerator.IdentifyByExtension<N64FileType>(N64FileType.AutoDetectTypes, path);
            foreach (N64FileType typeObj in possibleTypes)
            {
                try
                {
                    typeObj.LoadImage(path);
                    return typeObj;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = typeObj.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            foreach (Type type in N64FileType.AutoDetectTypes)
            {
                Boolean knownType = false;
                foreach (N64FileType typeObj in possibleTypes)
                {
                    if (typeObj.GetType() == type)
                    {
                        knownType = true;
                        break;
                    }
                }
                if (knownType)
                    continue;
                N64FileType objInstance = null;
                try
                {
                    objInstance = (N64FileType)Activator.CreateInstance(type);
                }
                catch { /* Ignore; programmer error. */ }
                if (objInstance == null)
                    continue;
                try
                {
                    objInstance.LoadImage(path);
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