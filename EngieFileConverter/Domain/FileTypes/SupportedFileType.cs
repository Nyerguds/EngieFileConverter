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
    public abstract class SupportedFileType : IFileTypeBroadcaster, IDisposable
    {
        #region Generic error messages
        // Input
        protected static readonly String ERR_FILE_TOO_SMALL = "File is not long enough to be of this type.";
        protected static readonly String ERR_BAD_SIZE = "Incorrect file size.";
        protected static readonly String ERR_DECOMPR = "Error decompressing file.";
        protected static readonly String ERR_DECOMPR_LEN = "Decompressed size does not match.";
        protected static readonly String ERR_DIM_ZERO = "Image dimensions can't be 0.";
        // Output
        protected static readonly String ERR_EMPTY_FILE = "File to save is empty!";
        protected static readonly String ERR_NO_FRAMES = "This format needs at least one frame.";
        protected static readonly String ERR_IMAGE_TOO_LARGE = "Image is too large to be saved into this format.";
        protected static readonly String ERR_EMPTY_FRAMES = "This format can't handle empty frames.";
        protected static readonly String ERR_FRAMES_DIFF = "This format needs all its frames to be the same size.";
        protected static readonly String ERR_FRAMES_BPPDIFF = "All frames must have the same color depth.";
        protected static readonly String ERR_1BPP_INPUT = "This format needs 1bpp input.";
        protected static readonly String ERR_4BPP_INPUT = "This format needs 4bpp input.";
        protected static readonly String ERR_8BPP_INPUT = "This format needs 8bpp input.";
        protected static readonly String ERR_NO_COL = "The given input contains no colors.";
        protected static readonly String ERR_UNKN_COMPR = "Unknown compression type \"{0}\".";
        protected static readonly String ERR_320x200 = "This format needs 320x200 input.";

        #endregion
        /// <summary>Main image in this loaded file. Can be left as null for an empty frame or the main entry of a frames container.</summary>
        protected Bitmap m_LoadedImage;
        /// <summary>Colour palette currently loaded into the image.</summary>
        protected Color[] m_Palette;
        /// <summary>Backup colour palette, to allow resetting the palette to its original state. Not used if NeedsPalette is set to return 'false'.</summary>
        protected Color[] m_BackupPalette;
        public SupportedFileType FrameParent { get; set; }

        /// <summary>General types applicable to this file type. Note that more specific types like 2-bit and 3-bit get rounded up to 4, since the actual image object in that case will be 4-bit.</summary>
        public abstract FileClass FileClass { get; }
        /// <summary>Types that are accepted as save input by this file type.</summary>
        public abstract FileClass InputFileClass { get; }
        /// <summary>Type to be accepted as frames. Override this for frame types.</summary>
        public virtual FileClass FrameInputFileClass { get { return FileClass.None; } }
        /// <summary>Unique identifier for this type. Use null for types that do not represent actual specific file types.</summary>
        public abstract String IdCode { get; }
        /// <summary>Very short code name for this type.</summary>
        public virtual String ShortTypeName { get { return this.FileExtensions.Length > 0 ? this.FileExtensions[0].ToUpper() : this.GetType().Name; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public abstract String ShortTypeDescription { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        public abstract String[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public virtual String[] DescriptionsForExtensions { get { return Enumerable.Repeat(this.ShortTypeDescription, this.FileExtensions.Length).ToArray(); } }
        /// <summary>True if this type can save. Defaults to true.</summary>
        public virtual Boolean CanSave { get { return true; } }
        /// <summary>Width of the file (if applicable). Normally the same as GetBitmap().Width</summary>
        public virtual Int32 Width { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Width; } }
        /// <summary>Height of the file (if applicable). Normally the same as GetBitmap().Height</summary>
        public virtual Int32 Height { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Height; } }
        /// <summary>True if the type contains no colours of its own, and needs an external palette to display its data. Only needs to be overridden if it return true.</summary>
        public virtual Boolean NeedsPalette { get { return false; } }
        /// <summary>Full path of the loaded file.</summary>
        public String LoadedFile { get; protected set; }
        /// <summary>Display string to show on the UI which file was loaded (no path).</summary>
        public String LoadedFileName { get; protected set; }
        /// <summary>Color depth of the file.</summary>
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
        public virtual String ExtraInfo { get; set; }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent. Null for no forced transparency.</summary>
        public virtual Boolean[] TransparencyMask { get { return null; } }

        /// <summary>
        /// Load a file from byte array. Note that the use of this function is discouraged, since many file types refer
        /// to accompanying files, like colour palettes, to complete the loaded data, and these cannot be detected without
        /// a file path.
        /// </summary>
        /// <param name="fileData">The data read from the file.</param>
        public abstract void LoadFile(Byte[] fileData);

        /// <summary>
        /// Load a file from byte array. The accompanying path is used to name the file on the UI, and to give the
        /// loading function the opportunity to load accompanying files from the same folder.
        /// </summary>
        /// <param name="fileData">The data read from the file.</param>
        /// <param name="filename">Original path the file was loaded from.</param>
        public virtual void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFile(fileData);
            this.SetFileNames(filename);
        }

        /// <summary>
        /// Some animation types are split into separate files, which often means the later files in the sequence
        /// rely on the previous ones to correctly construct their initial state.
        /// File types with that issue should override this function, to analyse which files need to be chained
        /// to get that state. This is a function used by the UI to ask for confirmation for the loading.
        /// This function is always called on loaded frame container types, and must return null or an empty chain
        /// to signal that there is no missing data.
        /// </summary>
        /// <param name="originalPath">Original path the file was loaded from.</param>
        /// <returns>The filenames in the required load chain, or null if there is no missing initial data.</returns>
        public virtual List<String> GetFilesToLoadMissingData(String originalPath) { return null; }

        /// <summary>
        /// Some animation types are split into separate files, which often means the later files in the sequence
        /// rely on the previous ones to correctly construct their initial state.
        /// This function will reload the file, using the files in the load chain to construct that initial state.
        /// </summary>
        /// <param name="fileData">Data of the original file </param>
        /// <param name="originalPath">Original path the file was loaded from.</param>
        /// <param name="loadChain">The sequence of files to load to get to the current file's initial state.</param>
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

        /// <summary>
        /// Gets the colours out of an image. Typically, this takes any specifically saved colours in m_Palette
        /// </summary>
        /// <returns></returns>
        public virtual Color[] GetColors()
        {
            return GetColorsInternal() ?? new Color[0];
        }

        protected Color[] GetColorsInternal()
        {
            if (this.BitsPerPixel == 0 || this.BitsPerPixel > 8)
                return null;
            if (this.m_Palette != null)
                return ArrayUtils.CloneArray(m_Palette);
            if (this.m_LoadedImage != null && (this.m_LoadedImage.PixelFormat & PixelFormat.Indexed) != 0)
                return this.m_LoadedImage.Palette.Entries;
            return null;
        }

        public virtual void SetColors(Color[] palette)
        {
            this.SetColors(palette, null);
        }

        public virtual void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            if (ReferenceEquals(updateSource, this))
                return;
            if (palette == null)
                return;
            Int32 paletteLength = palette.Length;
            if (paletteLength == 0)
                return;
            if (this.BitsPerPixel > 0 && this.BitsPerPixel <= 8)
            {
                Int32 maxLen = 1 << this.BitsPerPixel;
                // Palette length: never more than maxlen, in case of null it equals maxlen, if customised in image, take from image.
                Color[] origPal = GetColorsInternal();
                Int32 origPalLength = origPal == null ? maxLen : Math.Min(origPal.Length, maxLen);
                Color[] newPalette = new Color[origPalLength];
                for (Int32 i = 0; i < origPalLength; ++i)
                {
                    if (i < paletteLength)
                        newPalette[i] = palette[i];
                    else
                        newPalette[i] = Color.Empty;
                }
                Color[] testpal;
                if (this.m_BackupPalette == null && (testpal = this.GetColors()) != null && testpal.Length != 0 && !this.NeedsPalette)
                    this.m_BackupPalette = GetColorsInternal();
                this.m_Palette = newPalette;
                if (this.m_LoadedImage != null)
                    this.m_LoadedImage.Palette = ImageUtils.GetPalette(newPalette);
            }
            if (this.IsFramesContainer && !this.FramesHaveCommonPalette)
                return;
            // Logic if this is a frame: call for a colour replace in the parent so all frames get affected.
            // Skip this step if the FrameParent is the source, since that means some other frame already started this.
            if (this.FrameParent != null && !ReferenceEquals(this.FrameParent, updateSource) && this.FrameParent.FramesHaveCommonPalette)
                this.FrameParent.SetColors(palette, this);
            // Logic for frame container: call a colour replace on all frames, giving the current object as source.
            // Only execute this if the current object has frames. Skip the source frame.
            SupportedFileType[] frames = this.Frames;
            if (frames == null)
                return;
            Int32 nrOfFrames = frames.Length;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || ReferenceEquals(frame, updateSource))
                    continue;
                frame.SetColors(palette, this);
            }
        }

        public virtual void ResetColors()
        {
            // Should already be applied.
            //Color[] backup = ArrayUtils.CloneArray(this.m_BackupPalette);
            //PaletteUtils.ApplyPalTransparencyMask(backup, TransparencyMask);
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
        /// </summary>
        /// <param name="fileToSave">File to save.</param>
        /// <param name="targetBpp">Targeted bits per pixel.</param>
        /// <param name="expandToFullSize">Expand to full size.</param>
        /// <returns>The found colours in the input frames.</returns>
        public static Color[] CheckInputForColors(SupportedFileType fileToSave, Int32 targetBpp, Boolean expandToFullSize)
        {
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            Color[] palEntries = fileToSave.GetColors();
            // check frames
            if ((palEntries == null || palEntries.Length == 0) && fileToSave.IsFramesContainer && fileToSave.Frames != null)
            {
                SupportedFileType[] frames = fileToSave.Frames;
                // Find first palette in the frames.
                Int32 frLen = frames.Length;
                for (Int32 i = 0; i < frLen; ++i)
                {
                    palEntries = frames[i].GetColors();
                    if (palEntries != null && palEntries.Length > 0)
                        break;
                }
            }
            Int32 palLength;
            if (palEntries == null || (palLength = palEntries.Length) == 0)
                throw new ArgumentException("File to save has no color palette!", "fileToSave");
            // Relies on the current type's BPP setting.
            Int32 palSize = 1 << targetBpp;
            if (palEntries.Length == palSize || (!expandToFullSize && palLength < palSize))
                return palEntries;
            Color[] cols = new Color[palSize];
            Array.Copy(palEntries, cols, Math.Min(palLength, palSize));
            for (Int32 i = palLength; i < palSize; ++i)
                cols[i] = Color.Black;
            return cols;
        }

        private static Type[] m_autoDetectTypes =
        {
            typeof(FileImagePng),
            typeof(FileImageBmp),
            typeof(FileImageGif),
            typeof(FileImageJpg),
            typeof(FileImagePcx),
            typeof(FileImage),
            typeof(FileIcon),
            typeof(FileImgWwCps),
            typeof(FileImgWwCpsToon),
            typeof(FileFramesWwCpsAmi4),
            typeof(FileFramesWwWsa),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpLol1),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileFramesWwShpBr),
            typeof(FileFramesWwFntV3),
            typeof(FileFramesWwFntV4),
            typeof(FileFramesFntD2k),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
#if DEBUG
            typeof(FileImgWwMhwanh), // Experimental
#endif
            typeof(FileMapWwCc1Pc),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1PcFromIni),
            typeof(FileMapWwCc1N64FromIni),
            typeof(FileTilesWwCc1N64Bpp4),
            typeof(FileTilesWwCc1N64Bpp8),
            typeof(FileTilesetWwCc1PC),
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
            typeof(FileImgMythosLbv),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FilePaletteWwCc1N64Pa8),
            typeof(FilePaletteWwCc1N64Pa4),
            typeof(FileTblWwPal),
            typeof(FilePaletteWwAmiga),
#if DEBUG
            typeof(FilePaletteWwPsx), // Experimental
#endif
            typeof(FileFramesDfPic),
            typeof(FileFramesIgcSlb),
            typeof(FileFramesLadyGl),
            typeof(FileImgLadyTme),
            typeof(FileImgIgcGx2),
            typeof(FileImgIgcDmp),
            typeof(FileImgNova),
            typeof(FileImgStris),
            typeof(FileImgBif),
            typeof(FileImgJmx),
#if DEBUG
            typeof(FileImgMythosRmm), // Do not enable; too much chance on false positive.
#endif
            typeof(FileFramesAdvIco), // Put at the bottom because file size divisible by 0x120 is the only thing identifying this.
        };

        private static Type[] m_supportedOpenTypes =
        {
            typeof(FileImage),
            typeof(FileIcon),
            typeof(FileImgWwCps),
            typeof(FileImgWwCpsToon),
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpLol1),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileFramesWwShpBr),
            typeof(FileFramesWwFntV3),
            typeof(FileFramesWwFntV4),
            typeof(FileFramesFntD2k),
            typeof(FileFramesWwWsa),
            typeof(FileFramesWwCpsAmi4),
            typeof(FileImgWwLcw),
            typeof(FileImgWwN64),
            typeof(FileImgWwMhwanh),
            typeof(FileMapWwCc1N64),
            typeof(FileMapWwCc1Pc),
            //typeof(FileMapRa1PC),
            typeof(FileTilesWwCc1N64Bpp4),
            typeof(FileTilesWwCc1N64Bpp8),
            typeof(FileTilesetWwCc1PC),
            typeof(FilePalette6Bit),
            typeof(FilePalette8Bit),
            typeof(FilePaletteWwCc1N64Pa8),
            typeof(FilePaletteWwCc1N64Pa4),
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
            typeof(FilePaletteWwAmiga),
            typeof(FilePaletteWwPsx),
            typeof(FileFramesDfPic),
            typeof(FileFramesLadyGl),
            typeof(FileImgLadyTme),
            typeof(FileFramesIgcSlb),
            typeof(FileImgIgcGx2),
            typeof(FileImgIgcDmp),
            typeof(FileImgNova),
            typeof(FileImgStris),
            typeof(FileImgBif),
            typeof(FileImgJmx),
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
            typeof(FileFramesWwShpD2),
            typeof(FileFramesWwShpLol1),
            typeof(FileFramesWwShpCc),
            typeof(FileFramesWwShpTs),
            typeof(FileFramesWwShpBr),
            typeof(FileFramesWwFntV3),
            typeof(FileFramesWwFntV4),
            typeof(FileFramesFntD2k),
            typeof(FileFramesWwWsa),
            typeof(FileImgWwCps),
            typeof(FileImgWwCpsToon),
            typeof(FileFramesWwCpsAmi4),
            typeof(FileTilesetWwCc1PC),
            typeof(FileImgWwLcw),
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
            typeof(FilePaletteWwAmiga),
            //typeof(FilePaletteWwPsx),
            typeof(FileFramesDfPic),
            typeof(FileImgIgcDmp),
            typeof(FileImgIgcGx2),
            typeof(FileImgNova),
            typeof(FileImgBif),
            typeof(FileImgJmx),
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
            Int32 typesLength = types.Length;
            for (Int32 i = 0; i < typesLength; ++i)
            {
                Type t = types[i];
                if (!t.IsSubclassOf(sft))
                    throw new Exception("Entries in types list must all be SupportedFileType classes!");
            }
        }
#endif
        /// <summary>Lists all types that will appear in the Open File menu.</summary>
        public static Type[] SupportedOpenTypes { get { return ArrayUtils.CloneArray(m_supportedOpenTypes); } }
        /// <summary>Lists all types that can appear in the Save File menu.</summary>
        public static Type[] SupportedSaveTypes { get { return ArrayUtils.CloneArray(m_supportedSaveTypes); } }
        /// <summary
        /// >Lists all types that can be autodetected, in the order they will be detected. Note that as a first step,
        /// all types which contain the requested file's extension are filtered out and checked.
        /// </summary>
        public static Type[] AutoDetectTypes { get { return ArrayUtils.CloneArray(m_autoDetectTypes); } }

        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="path">File path to load.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static SupportedFileType LoadFileAutodetect(String path, SupportedFileType[] preferredTypes, Boolean onlyGivenTypes, out List<FileTypeLoadException> loadErrors)
        {
            Byte[] fileData = File.ReadAllBytes(path);
            return LoadFileAutodetect(fileData, path, preferredTypes, onlyGivenTypes, out loadErrors);
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
        public static SupportedFileType LoadFileAutodetect(Byte[] fileData, String path, SupportedFileType[] preferredTypes, Boolean onlyGivenTypes, out List<FileTypeLoadException> loadErrors)
        {
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (preferredTypes == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(AutoDetectTypes, path);
            else if (onlyGivenTypes)
            {
                // Try extension-filtering first, then the rest.
                SupportedFileType[] preferredTypesExt = FileDialogGenerator.IdentifyByExtension(preferredTypes, path);
                Int32 extLength = preferredTypesExt.Length;
                for (Int32 i = 0; i < extLength; ++i)
                {
                    SupportedFileType typeObj = preferredTypesExt[i];
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
            Int32 prefTypesLength = preferredTypes.Length;
            for (Int32 i = 0; i < prefTypesLength; ++i)
            {
                SupportedFileType typeObj = preferredTypes[i];
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
            Int32 autoTypesLength = AutoDetectTypes.Length;
            for (Int32 i = 0; i < autoTypesLength; ++i)
            {
                Type type = AutoDetectTypes[i];
                // Skip entries on the already-tried list.
                Boolean isPreferredType = false;
                for (Int32 j = 0; j < prefTypesLength; ++j)
                {
                    if (preferredTypes[j].GetType() != type)
                        continue;
                    isPreferredType = true;
                    break;
                }
                if (isPreferredType)
                    continue;
                SupportedFileType objInstance = null;
                try
                {
                    objInstance = (SupportedFileType) Activator.CreateInstance(type);
                }
                catch
                {
                    /* Ignore; programmer error. */
                }
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
                    // Removes any stored images.
                    objInstance.Dispose();
                }
            }
            return null;
        }

        public void Dispose()
        {
            SupportedFileType[] frames = this.Frames;
            if (this.IsFramesContainer && this.Frames != null)
            {
                Int32 nrOfFrames = this.Frames.Length;
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    SupportedFileType frame = this.Frames[i];
                    if (frame != null)
                        frame.Dispose();
                }
            }
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
        Image4Bit = Image1Bit << 1,
        Image8Bit = Image4Bit << 1,
        ImageIndexed = Image1Bit | Image4Bit | Image8Bit,
        ImageHiCol = Image8Bit << 1,
        Image = Image1Bit | Image4Bit | Image8Bit | ImageHiCol,
        FrameSet = ImageHiCol << 1,
        CcMap = FrameSet << 1,
        RaMap = CcMap << 1,
    }
}