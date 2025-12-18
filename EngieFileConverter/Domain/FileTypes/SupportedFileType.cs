using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;

namespace EngieFileConverter.Domain.FileTypes
{
    public abstract class SupportedFileType : FileTypeBroadcaster, IDisposable
    {
        protected Bitmap m_LoadedImage;
        protected Color[] m_Palette;
        protected Color[] m_BackupPalette;
        public SupportedFileType FrameParent { get; set; }

        /// <summary>General types applicable to this file type.</summary>
        public abstract FileClass FileClass { get; }
        /// <summary>Types that are accepted as save input by this file type.</summary>
        public abstract FileClass InputFileClass { get; }
        /// <summary>Type to be accepted as frames. Override this for frame types.</summary>
        public virtual FileClass FrameInputFileClass { get { return FileClass.None; } }
        /// <summary>Very short code name for this type.</summary>
        public virtual String ShortTypeName { get { return this.FileExtensions.Length > 0 ? this.FileExtensions[0].ToUpper() : this.GetType().Name; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public abstract String ShortTypeDescription { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        public abstract String[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public virtual String[] DescriptionsForExtensions { get { return Enumerable.Repeat(this.ShortTypeDescription, this.FileExtensions.Length).ToArray(); } }
        /// <summary>Width of the file (if applicable). Normally the same as GetBitmap().Width</summary>
        public virtual Int32 Width { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Width; } }
        /// <summary>Height of the file (if applicable). Normally the same as GetBitmap().Height</summary>
        public virtual Int32 Height { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Height; } }
        /// <summary>Amount of colors in the palette that is contained inside the image. 0 if the image itself does not contain a palette, even if it generates one.</summary>
        public virtual Int32 ColorsInPalette { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Palette.Entries.Length; } }
        /// <summary>Full path of the loaded file.</summary>
        public String LoadedFile { get; protected set; }
        /// <summary>Display string to show on the UI which file was loaded (no path).</summary>
        public String LoadedFileName { get; protected set; }
        /// <summary>Colour depth of the file.</summary>
        public virtual Int32 BitsPerPixel { get { return this.m_LoadedImage == null ? 0 : Image.GetPixelFormatSize(this.m_LoadedImage.PixelFormat); } }
        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public virtual SupportedFileType[] Frames { get { return null; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public virtual Boolean IsFramesContainer { get { return this.Frames != null; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public virtual Boolean FramesHaveCommonPalette { get { return this.IsFramesContainer; } }
        /// <summary>
        /// This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source, and can normally also be saved from a single-image source.
        /// This setting should be ignored for types that are not set to IsFramesContainer.
        /// </summary>
        public virtual Boolean HasCompositeFrame { get { return false; } }
        /// <summary>Extra info to be shown on the UI, like detected internal compression type in a loaded file.</summary>
        public virtual String ExtraInfo { get; protected set; }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent. Null for no forced transparency.</summary>
        public virtual Boolean[] TransparencyMask { get { return null; } }

        protected virtual void BuildFullImage() { }

        //public virtual SaveOption[] GetPostLoadInitOptions()
        //{
        //    return new SaveOption[0];
        //}

        //public virtual void PostLoadInit(SaveOption[] loadOptions) { }
        
        public abstract void LoadFile(Byte[] fileData);

        public virtual void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFile(fileData);
            this.SetFileNames(filename);
        }

        public virtual void LoadFile(SupportedFileType file)
        {
            Byte[] thisBytes = this.SaveToBytesAsThis(file, new SaveOption[0]);
            this.LoadFile(thisBytes);
        }

        public virtual List<String> GetFilesToLoadMissingData(String originalPath) { return null; }
        public virtual void ReloadFromMissingData(Byte[] fileData, String originalPath, List<String> loadChain) { }

        /// <summary>
        /// Get specific options for saving a file to this format. Can be made to depend on the input file and the output path.
        /// </summary>
        /// <param name="fileToSave">The opened file that is being saved.</param>
        /// <param name="targetFileName">The target file path.</param>
        /// <returns>The list of options. Leave empty if no options are needed. Returning null will give a general "cannot save as this type" message.</returns>
        public virtual SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName) { return new SaveOption[0]; }

        /// <summary>
        /// Saves the given file as this type.
        /// </summary>
        /// <param name="fileToSave">The input file to convert.</param>
        /// <param name="savePath">The path to save to.</param>
        /// <param name="saveOptions">Extra options for customising the save process. Request the list from GetSaveOptions.</param>
        public virtual void SaveAsThis(SupportedFileType fileToSave, String savePath, SaveOption[] saveOptions)
        {
            Byte[] data = this.SaveToBytesAsThis(fileToSave, saveOptions);
            File.WriteAllBytes(savePath, data);
        }

        /// <summary>
        /// Saves the given file as this type.
        /// </summary>
        /// <param name="fileToSave">The input file to convert.</param>
        /// <param name="saveOptions">Extra options for customising the save process. Request the list from GetSaveOptions.</param>
        /// <returns>The bytes of the file converted to this type.</returns>
        public abstract Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions);

        public virtual void SetFileNames(String path)
        {
            this.LoadedFile = path;
            this.LoadedFileName = Path.GetFileName(path);
        }

        public virtual Color[] GetColors()
        {
            if (this.m_LoadedImage == null && this.m_Palette == null)
                return new Color[0];
            Color[] col1 = this.m_LoadedImage == null ? this.m_Palette : this.m_LoadedImage.Palette.Entries;
            Color[] col2 = new Color[col1.Length];
            col1.CopyTo(col2, 0);
            return col2;
        }

        public virtual void SetColors(Color[] palette)
        {
            this.SetColors(palette, null);
        }

        public virtual void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            if (this.IsFramesContainer && !this.FramesHaveCommonPalette)
                return;
            if (ReferenceEquals(updateSource, this))
                return;
            if (palette == null || palette.Length == 0)
                return;
            Boolean isInternal = this.ColorsInPalette > 0;
            // Override this in types that don't have a palette, like grayscale N64 images.
            // This function should only be called from UI "if (BitsPerColor != 0 && !FileHasPalette)"
            if (this.BitsPerPixel != 0)
            {
                Int32 maxLen = 1 << this.BitsPerPixel;
                //Boolean[] transMask = this.TransparencyMask;
                //Int32 transMaskLen = transMask == null ? 0 : transMask.Length;
                // Palette length: never more than maxlen, in case of null it equals maxlen, if customised in image, take from image.
                Int32 paletteLength = Math.Min(maxLen, this.m_LoadedImage == null ? maxLen : this.m_LoadedImage.Palette.Entries.Length);
                Color[] pal = new Color[paletteLength];
                for (Int32 i = 0; i < paletteLength; i++)
                {
                    if (i < palette.Length)
                        pal[i] = palette[i];
                    else
                        pal[i] = Color.Empty;
                }
                Color[] testpal;
                if (this.m_BackupPalette == null && (testpal = this.GetColors()) != null && testpal.Length != 0 && isInternal)
                    this.m_BackupPalette = this.m_Palette == null ? (this.m_LoadedImage == null ? null : this.m_LoadedImage.Palette.Entries) : this.m_Palette.ToArray();
                this.m_Palette = pal;
                if (this.m_LoadedImage != null)
                {
                    ColorPalette imagePal = this.m_LoadedImage.Palette;
                    Int32 entries = imagePal.Entries.Length;
                    for (Int32 i = 0; i < entries; i++)
                    {
                        if (i < pal.Length)
                            imagePal.Entries[i] = pal[i];
                        else
                            imagePal.Entries[i] = Color.Empty;
                    }
                    this.m_LoadedImage.Palette = imagePal;
                }
            }
            // Logic if this is a frame: call for a colour replace in the parent so all frames get affected.
            // Skip this step if the FrameParent is the source, since that means some other frame already started this.
            if (this.FrameParent != null && !ReferenceEquals(this.FrameParent, updateSource) && this.FrameParent.FramesHaveCommonPalette)
                this.FrameParent.SetColors(palette, this);
            // Logic for frame container: call a colour replace on all frames, giving the current object as source.
            // Only execute this if the current object has frames. Skip the source frame.
            if (this.Frames == null)
                return;
            foreach (SupportedFileType frame in this.Frames.Where(frame => frame != null && !ReferenceEquals(frame, updateSource)))
                frame.SetColors(palette, this);
        }

        public virtual void ResetColors()
        {
            // Should already be applied.
            //Color[] backup = this.m_BackupPalette.ToArray();
            //PaletteUtils.ApplyTransparencyGuide(backup, TransparencyMask);
            this.SetColors(this.m_BackupPalette, null);
        }

        public virtual Boolean ColorsChanged()
        {
            if (this.BitsPerPixel == 0 || this.BitsPerPixel > 8)
                return false;
            if (this.m_BackupPalette == null)
                return false;
            Color[] colors = this.GetColors();
            if (colors == null)
                return false;
            return !this.m_BackupPalette.SequenceEqual(colors);
        }

        public virtual Bitmap GetBitmap()
        {
            return this.m_LoadedImage;
        }

        /// <summary>
        /// Palette types can use this to get the colour out of a SupportedFileType in their SaveToBytesAsThis routine.
        /// Relies on the current type's bits per pixel setting to get the final palette size.
        /// </summary>
        /// <param name="fileToSave">File to save.</param>
        /// <param name="expandToFullSize">Expand to full size.</param>
        /// <returns>The found colours in the input frames.</returns>
        protected Color[] CheckInputForColors(SupportedFileType fileToSave, Boolean expandToFullSize)
        {
            if (fileToSave == null)
                throw new NotSupportedException("File to save is empty!");
            Color[] palEntries = fileToSave.GetColors();
            // check frames
            if ((palEntries == null || palEntries.Length == 0) && fileToSave.IsFramesContainer && fileToSave.Frames != null)
            {
                SupportedFileType[] frames = fileToSave.Frames;
                // Find first palette in the frames.
                for (Int32 i = 0; i < frames.Length && (palEntries == null || palEntries.Length == 0); i++)
                    palEntries = frames[i].GetColors();
            }
            if (palEntries == null || palEntries.Length == 0)
                throw new NotSupportedException("File to save has no colour palette!");
            // Relies on the current type's BPP setting.
            Int32 palSize = 1 << this.BitsPerPixel;
            if (palEntries.Length == palSize || (!expandToFullSize && palEntries.Length < palSize))
                return palEntries;
            Color[] cols = new Color[palSize];
            Array.Copy(palEntries, cols, Math.Min(palEntries.Length, palSize));
            for (Int32 i = palEntries.Length; i < palSize; i++)
                cols[i] = Color.Black;
            return cols;
        }


        private static Type[] m_autoDetectTypes =
        {
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImage),
            typeof(FileIcon),
            typeof(FileImgWwCps),
            typeof(FileFramesWwWsa),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
            typeof(FileMapWwCc1Pc),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1PcFromIni),
            typeof(FileMapWwCc1N64FromIni),
            typeof(FileTilesWwCc1N64Bpp4),
            typeof(FileTilesWwCc1N64Bpp8),
            typeof(FileTilesetWwCc1PC),
            typeof(FilePalette6Bit),
            typeof(FilePaletteWwCc1N64Pa8),
            typeof(FilePaletteWwCc1N64Pa4),
            typeof(FileFramesAdvVga),
            typeof(FileImgKort),
            typeof(FileFramesKortBmp),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FileFramesDynBmp),
            typeof(FileImgDynBmpMtx),
            typeof(FilePaletteDyn),
            typeof(FileFramesMythosPal),
            typeof(FileFramesMythosVgs),
            typeof(FileFramesMythosVda),
            typeof(FileImgKotB),
            typeof(FileFramesAdvIco), // Put at the bottom because file size divisible by 0x120 is the only thing identifying this.
            typeof(FilePalette8Bit),
            typeof(FileTblWwPal),
        };

        private static Type[] m_supportedOpenTypes =
        {
            typeof(FileImage),
            typeof(FileIcon),
            typeof(FileImgWwCps),
            typeof(FileFramesWwWsa),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1Pc),
            //typeof(FileMapRa1PC),
            typeof(FileTilesWwCc1N64Bpp4),
            typeof(FileTilesWwCc1N64Bpp8),
            typeof(FileTilesetWwCc1PC),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FilePaletteWwCc1N64),
            typeof(FileTblWwPal),
            typeof(FileFramesAdvVga),
            typeof(FileFramesAdvIco),
            typeof(FileFramesDynBmp),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FilePaletteDyn),
            typeof(FileImgKort),
            typeof(FileFramesKortBmp),
            typeof(FileImgKotB),
            typeof(FileFramesMythosPal),
            typeof(FileFramesMythosVgs),
            typeof(FileFramesMythosVda),
        };

        private static Type[] m_supportedSaveTypes =
        {
            //typeof(FileImgN64Standard),
            //typeof(FileImgN64Jap),
            //typeof(FileImgN64Gray),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1Pc),
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileIcon),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FileImgWwCps),
            typeof(FileFramesWwWsa),
            typeof(FileImgWwLcw),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileTilesetWwCc1PC),
            typeof(FileImgWwN64),
            typeof(FilePaletteWwCc1N64Pa4),
            typeof(FilePaletteWwCc1N64Pa8),
            typeof(FileTblWwPal),
            typeof(FileFramesAdvVga),
            typeof(FileFramesAdvIco),
            typeof(FileImgDynScr),
            typeof(FileImgDynScrV2),
            typeof(FileFramesDynBmp),
            typeof(FileImgDynBmpMtx),
            typeof(FilePaletteDyn),
            typeof(FileImgKort),
            typeof(FileFramesKortBmp),
            typeof(FileFramesMythosVgs),
            typeof(FileFramesMythosVda),
            typeof(FileFramesMythosPal),
            typeof(FileImgKotB),
        };

#if DEBUG
        static SupportedFileType()
        {
            CheckTypes(SupportedOpenTypes);
            CheckTypes(SupportedSaveTypes);
            CheckTypes(AutoDetectTypes);
        }

        private static void CheckTypes(Type[] types)
        {
            // internal check for development.
            Type sft = typeof(SupportedFileType);
            foreach (Type t in types)
                if (!t.IsSubclassOf(sft))
                    throw new Exception("Entries in types list must all be SupportedFileType classes!");
        }
#endif
        /// <summary>Lists all types that will appear in the Open File menu.</summary>
        public static Type[] SupportedOpenTypes { get { return m_supportedOpenTypes.ToArray(); } }
        /// <summary>Lists all types that can appear in the Save File menu.</summary>
        public static Type[] SupportedSaveTypes { get { return m_supportedSaveTypes.ToArray(); } }
        /// <summary
        /// >Lists all types that can be autodetected, in the order they will be detected. Note that as a first step,
        /// all types which contain the requested file's extension are filtered out and checked.
        /// </summary>
        public static Type[] AutoDetectTypes { get { return m_autoDetectTypes.ToArray(); } }

        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="path">File path to load.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static SupportedFileType LoadFileAutodetect(String path, SupportedFileType[] preferredTypes, out List<FileTypeLoadException> loadErrors, Boolean onlyGivenTypes)
        {
            Byte[] fileData = File.ReadAllBytes(path);
            return LoadFileAutodetect(fileData, path, preferredTypes, out loadErrors, onlyGivenTypes);
        }

        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="fileData">File dat to load file from.</param>
        /// <param name="path">File path, used for extension filtering and file initialisation. Not for reading as bytes; fileData is used for that.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static SupportedFileType LoadFileAutodetect(Byte[] fileData, String path, SupportedFileType[] preferredTypes, out List<FileTypeLoadException> loadErrors, Boolean onlyGivenTypes)
        {
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (preferredTypes == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(AutoDetectTypes, path);
            else if (onlyGivenTypes)
            {
                // Try extension-filtering first, then the rest.
                SupportedFileType[] preferredTypesExt = FileDialogGenerator.IdentifyByExtension(preferredTypes, path);
                foreach (SupportedFileType typeObj in preferredTypesExt)
                {
                    try
                    {
                        typeObj.LoadFile(fileData, path);
                        return typeObj;
                    }
                    catch (FileTypeLoadException e)
                    {
                        e.AttemptedLoadedType = typeObj.ShortTypeName;
                        loadErrors.Add(e);
                    }
                    preferredTypes = preferredTypes.Where(tp => preferredTypesExt.All(tpe => tpe.GetType() != tp.GetType())).ToArray();
                }
            }
            foreach (SupportedFileType typeObj in preferredTypes)
            {
                try
                {
                    typeObj.LoadFile(fileData, path);
                    return typeObj;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = typeObj.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            if (onlyGivenTypes)
                return null;
            foreach (Type type in AutoDetectTypes)
            {
                // Skip entries on the already-tried list.
                if (preferredTypes.Any(x => x.GetType() == type))
                    continue;
                SupportedFileType objInstance = null;
                try { objInstance = (SupportedFileType) Activator.CreateInstance(type); }
                catch { /* Ignore; programmer error. */ }
                if (objInstance == null)
                    continue;
                try
                {
                    objInstance.LoadFile(fileData, path);
                    return objInstance;
                }
                catch (FileTypeLoadException e)
                {
                    // objInstance should not be disposed here since it never succeeded in initializing,
                    // and should not contain any loaded images at that point.
                    e.AttemptedLoadedType = objInstance.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (this.IsFramesContainer)
                foreach (SupportedFileType frame in this.Frames)
                    if (frame != null)
                        frame.Dispose();
            Bitmap bitmap = this.GetBitmap();
            if (bitmap != null)
            {
                try { bitmap.Dispose(); }
                catch (Exception) { /* Ignore */ }
            }
        }
    }

    [Flags]
    public enum FileClass
    {
        None = 0x00,
        Image1Bit = 0x01,
        Image4Bit = 0x02,
        Image8Bit = 0x04,
        ImageIndexed = 0x07,
        ImageHiCol = 0x08,
        Image = 0x0F,
        FrameSet = 0x10,
        CcMap = 0x20,
    }
}