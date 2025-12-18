using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CnC64FileConverter.Domain.FileTypes
{
    public abstract class SupportedFileType : FileTypeBroadcaster
    {
        protected Bitmap m_LoadedImage;
        protected Color[] m_Palette = null;
        protected Color[] m_BackupPalette = null;
        protected Int32 m_CompositeFrameTilesWidth = 1;
        public SupportedFileType FrameParent { get; set; }

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

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public virtual SupportedFileType[] Frames { get { return null; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public virtual Boolean IsFramesContainer { get { return Frames != null; } }
        /// <summary>
        /// This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source, and can normally also be saved from a single-image source.
        /// This setting should be ignored for types that are not set to IsFramesContainer.
        /// </summary>
        public virtual Boolean HasCompositeFrame { get { return false; } }

        /// <summary>Sets the width of the composite frame image, and re-renders the image with the given width.</summary>
        public Int32 CompositeFrameTilesWidth
        {
            get { return this.m_CompositeFrameTilesWidth; }
            set
            {
                this.m_CompositeFrameTilesWidth = value;
                if (this.Frames != null)
                    BuildFullImage();
            }
        }
        protected virtual void BuildFullImage() { }

        public abstract void LoadFile(Byte[] fileData);
        public abstract void LoadFile(String filename);

        public virtual void LoadFile(SupportedFileType file)
        {
            Byte[] thisBytes = this.SaveToBytesAsThis(file, new SaveOption[0], true);
            LoadFile(thisBytes);
        }

        /// <summary>
        /// Get specific options for saving a file to this format. Can be made to depend on the input file and the output path.
        /// </summary>
        /// <param name="fileToSave">The opened file that is being saved.</param>
        /// <param name="targetFileName">The target file path.</param>
        /// <returns>The list of options. Leave empty if no options are needed. Returning null will give a general "cannot save as this type" message.</returns>
        public virtual SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName) { return new SaveOption[0]; }

        public virtual void SaveAsThis(SupportedFileType fileToSave, String savePath, SaveOption[] saveOptions, Boolean dontCompress)
        {
            Byte[] data = this.SaveToBytesAsThis(fileToSave, saveOptions, dontCompress);
            File.WriteAllBytes(savePath, data);
        }

        public abstract Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress);

        public virtual void SetFileNames(String path)
        {
            LoadedFile = path;
            LoadedFileName = Path.GetFileName(path);
        }

        public virtual Color[] GetColors()
        {
            if (m_LoadedImage == null && this.m_Palette == null)
                return new Color[0];
            Color[] col1 = this.m_LoadedImage == null ? this.m_Palette : m_LoadedImage.Palette.Entries;
            Color[] col2 = new Color[col1.Length];
            Array.Copy(col1, col2, col1.Length);
            return col2;
        }

        public virtual void SetColors(Color[] palette)
        {
            this.SetColors(palette, null);
        }

        /// <summary>
        /// Sets the colour palette for this object, its frames, or if it is a frame, all frames in its parent.
        /// </summary>
        /// <param name="palette">New colour palette</param>
        /// <param name="updateSource">The object that requested the update. If this equals the current object's FrameParent, no request for a parent update will be done. If it equals this object, the operation aborts immediately. If it equals one of the frames contained inside this, that frame will be skipped.</param>
        public virtual void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            if (updateSource == this)
                return;
            if (palette == null || palette.Length == 0)
                return;
            // Override this in types that don't have a palette, like grayscale N64 images.
            // This function should only be called from UI "if (BitsPerColor != 0 && !FileHasPalette)"
            if (this.BitsPerColor != 0)
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
                this.m_Palette = pal;
                if (m_LoadedImage != null)
                {
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
            }
            // Logic if this is a frame: call for a colour replace in the parent so all frames get affected.
            // Skip this step if the FrameParent is the source, since that means some other frame already started this.
            if (this.FrameParent != null && this.FrameParent != updateSource)
                FrameParent.SetColors(palette, this);
            // Logic for crame container: call a colour replace on all frames, giving the current object as source.
            // Only execute this if the current object has frames. Skip the source frame.
            if (this.Frames != null && this.Frames != null)
                foreach (SupportedFileType frame in this.Frames)
                    if (frame != null && frame != updateSource)
                        frame.SetColors(palette, this);
        }

        public virtual void ResetColors()
        {
            SetColors(this.m_BackupPalette, null);
        }

        public virtual Boolean ColorsChanged()
        {
            if (this.BitsPerColor == 0 || this.BitsPerColor > 8)
                return false;
            if (this.m_BackupPalette == null)
                return false;
            if (m_Palette == null)
                return true;
            return !GetColors().SequenceEqual(this.m_BackupPalette);
        }

        public virtual Bitmap GetBitmap()
        {
            return m_LoadedImage;
        }

        private static Type[] m_supportedOpenTypes =
        {
            typeof(FileImgN64),
            typeof(FileMapCc1N64),
            typeof(FileMapCc1Pc),
            typeof(FileMapRa1PC),
            typeof(FileImage),
            typeof(FileImgCps),
            typeof(FileTilesN64Bpp4),
            typeof(FileTilesN64Bpp8),
            typeof(FileTilesetCc1PC),
            typeof(FilePaletteWwPc),
            typeof(FilePaletteN64),
            typeof(FileAdvVgaFrames),
            typeof(FileAdvIcons),
            //typeof(FileImgDynBmp),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FilePaletteDyn),
            typeof(FileImgKort),
            typeof(FileImgKortBmp),
            typeof(FileImgKotB),
            typeof(FileImgMythosVgs),
        };

        private static Type[] m_autoDetectTypes =
        {
            typeof(FileImgN64Gray),
            typeof(FileImgN64Standard),
            typeof(FileMapCc1N64),
            typeof(FileMapCc1Pc),
            typeof(FileMapPcFromIni),
            typeof(FileMapN64FromIni),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImgCps0),
            typeof(FileImgCps0c),
            //typeof(FileImgCps1),
            //typeof(FileImgCps1c),
            //typeof(FileImgCps2),
            //typeof(FileImgCps2c),
            typeof(FileImgCps3),
            typeof(FileImgCps3c),
            typeof(FileImgCps4),
            typeof(FileImgCps4c),
            typeof(FileImgWwLcw),
            typeof(FileTilesN64Bpp4),
            typeof(FileTilesN64Bpp8),
            typeof(FileTilesetCc1PC),
            typeof(FilePaletteWwPc),
            typeof(FilePaletteN64Pa8),
            typeof(FilePaletteN64Pa4),
            typeof(FileAdvVgaFrames),
            typeof(FileImgKort),
            typeof(FileImgKortBmp),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            //typeof(FileImgDynBmp),
            typeof(FilePaletteDyn),
            typeof(FileImgMythosVgs),
            typeof(FileImgKotB),
            typeof(FileAdvIcons),
        };
        private static Type[] m_supportedSaveTypes =
        {
            typeof(FileImgN64Standard),
            typeof(FileImgN64Jap),
            typeof(FileImgN64Gray),
            typeof(FileMapCc1N64),
            typeof(FileMapCc1Pc),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImgCps0),
            typeof(FileImgCps0c),
            //typeof(FileImgCps1),
            //typeof(FileImgCps1c),
            //typeof(FileImgCps2),
            //typeof(FileImgCps2c),
            typeof(FileImgCps3),
            typeof(FileImgCps3c),
            typeof(FileImgCps4),
            typeof(FileImgCps4c),
            typeof(FileImgWwLcw),
            typeof(FileTilesetCc1PC),
            typeof(FilePaletteWwPc),
            typeof(FilePaletteN64Pa4),
            typeof(FilePaletteN64Pa8),
            typeof(FileAdvVgaFrames),
            typeof(FileAdvIcons),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FilePaletteDyn),
            typeof(FileImgKort),
            typeof(FileImgKortBmp),
            typeof(FileImgMythosVgs),
            typeof(FileImgKotB),
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