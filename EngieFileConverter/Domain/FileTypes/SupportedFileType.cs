using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public abstract class SupportedFileType : IFileTypeBroadcaster, IDisposable
    {
        #region Generic error messages
        // Input
        protected const String ERR_FILE_TOO_SMALL = "File is not long enough to be of this type.";
        protected const String ERR_NO_HEADER = "File data too short to contain header.";
        protected const String ERR_BAD_HEADER = "Identifying bytes in header do not match.";
        protected const String ERR_BAD_HEADER_DATA = "Bad values in header.";
        protected const String ERR_BAD_SIZE = "Incorrect file size.";
        protected const String ERR_BAD_HEADER_SIZE = "File size in header does not match.";
        protected const String ERR_BAD_HEADER_PAL_SIZE = "Invalid palette length in header.";
        protected const String ERR_NO_IMAGE = "No image data found in file.";
        protected const String ERR_NO_FRAMES = "No frames found in file.";
        protected const String ERR_SIZE_TOO_SMALL = "File is too small.";
        protected const String ERR_SIZE_TOO_SMALL_IMAGE = "File is too small to contain the image data.";
        protected const String ERR_DECOMPR_ = "Error decompressing file";
        protected const String ERR_DECOMPR = ERR_DECOMPR_ + ".";
        protected const String ERR_DECOMPR_ERR = ERR_DECOMPR_ + ": {0}";
        protected const String ERR_DECOMPR_LEN = "Decompressed size does not match.";
        protected const String ERR_DIM_ZERO = "Image dimensions can't be 0.";
        protected const String ERR_MAKING_IMG_ = "Cannot construct image from read data";
        protected const String ERR_MAKING_IMG = ERR_MAKING_IMG_ + ".";
        protected const String ERR_MAKING_IMG_ERR = ERR_MAKING_IMG_ + ": {0}";
        // Output
        protected const String ERR_EMPTY_FILE = "File to save is empty.";
        protected const String ERR_DIMENSIONS_INPUT = "This format needs {0}×{1} input.";
        protected const String ERR_DIMENSIONS_TOO_WIDE = "Image width is too large to be saved into this format.";
        protected const String ERR_DIMENSIONS_TOO_HIGH = "Image height is too large to be saved into this format.";
        protected const String ERR_DIMENSIONS_TOO_LARGE = "Image is too large to be saved into this format.";
        protected const String ERR_DIMENSIONS_TOO_LARGE_MAX_DIM = " The maximum is {0} pixels.";
        protected const String ERR_DIMENSIONS_TOO_LARGE_MAX_SIZE = " The maximum is {0}×{1} pixels.";
        protected const String ERR_DIMENSIONS_TOO_WIDE_DIM = ERR_DIMENSIONS_TOO_WIDE + ERR_DIMENSIONS_TOO_LARGE_MAX_DIM;
        protected const String ERR_DIMENSIONS_TOO_HIGH_DIM = ERR_DIMENSIONS_TOO_HIGH + ERR_DIMENSIONS_TOO_LARGE_MAX_DIM;
        protected const String ERR_DIMENSIONS_TOO_HIGH_SIZE = ERR_DIMENSIONS_TOO_LARGE + ERR_DIMENSIONS_TOO_LARGE_MAX_SIZE;
        protected const String ERR_FRAMES_NEEDED = "This format needs at least one frame.";
        protected const String ERR_FRAMES_OVERFLOW = "This format can't handle more than {0} frames.";
        protected const String ERR_FRAMES_EMPTY = "This format can't handle empty frames.";
        protected const String ERR_FRAMES_SIZE_DIFF = "This format needs all its frames to be the same size.";
        protected const String ERR_FRAMES_BPP_DIFF = "All frames must have the same color depth.";
        protected const String ERR_BPP_INPUT_EXACT = "This format needs {0}bpp input.";
        protected const String ERR_BPP_INPUT_INDEXED = "This format needs indexed color input.";
        protected const String ERR_BPP_INPUT_4_8 = "This format needs 4bpp or 8bpp input.";
        protected const String ERR_BPP_LOW_INPUT = "This is a {0}bpp format. For higher bpp input, the values can only go from 0 to {1}.";
        protected const String ERR_COLORS_NEEDED = "The given input contains no colors.";
        protected const String ERR_UNKN_COMPR = "Unknown compression type.";
        protected const String ERR_UNKN_COMPR_X = "Unknown compression type \"{0}\".";
        protected const String ERR_BPP_DIMENSIONS = "Only {0}-bit {1}×{2} images can be saved as {3}.";
        protected const String ERR_COMPR_TOO_LARGE = "The content after compression exceeds {0} bytes; it is too large to be saved in this type.";
        protected String ErrFixedBppAndSize { get { return String.Format(ERR_BPP_DIMENSIONS, this.BitsPerPixel, this.Width, this.Height, ShortTypeName); } }

        #endregion
        /// <summary>Main image in this loaded file. Can be left as null for an empty frame or the main entry of a frames container.</summary>
        protected Bitmap m_LoadedImage;
        /// <summary>Color palette currently loaded into the image.</summary>
        protected Color[] m_Palette;
        /// <summary>Backup color palette, to allow resetting the palette to its original state. Not used if NeedsPalette is set to return 'false'.</summary>
        protected Color[] m_BackupPalette;
        public SupportedFileType FrameParent { get; set; }

        /// <summary>General types applicable to this file type. Note that more specific types like 2-bit and 3-bit get rounded up to 4, since the actual image object in that case will be 4-bit.</summary>
        public abstract FileClass FileClass { get; }
        /// <summary>Types that are accepted as save input by this file type.</summary>
        public abstract FileClass InputFileClass { get; }
        /// <summary>Type to be accepted as frames. Override this for frame types.</summary>
        public virtual FileClass FrameInputFileClass { get { return FileClass.None; } }
        /// <summary>Short unique identifier code for this type. Use null for types that do not represent actual specific file types.</summary>
        public abstract String IdCode { get; }
        /// <summary>Short name for this type.</summary>
        public virtual String ShortTypeName { get { return this.FileExtensions.Length > 0 ? this.FileExtensions[0].ToUpper() : this.GetType().Name; } }
        /// <summary>Longer name and description of the file type, for the UI and for the types dropdown in the "open file" dialog.</summary>
        public abstract String LongTypeName { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        public abstract String[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public virtual String[] DescriptionsForExtensions { get { return Enumerable.Repeat(this.LongTypeName, this.FileExtensions.Length).ToArray(); } }
        /// <summary>True if this type can save. Defaults to true.</summary>
        public virtual Boolean CanSave { get { return true; } }
        /// <summary>Width of the file (if applicable). Normally the same as GetBitmap().Width</summary>
        public virtual Int32 Width { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Width; } }
        /// <summary>Height of the file (if applicable). Normally the same as GetBitmap().Height</summary>
        public virtual Int32 Height { get { return this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Height; } }
        /// <summary>True if the type contains no colors of its own, and needs an external palette to display its data. Only needs to be overridden if it return true.</summary>
        public virtual Boolean NeedsPalette { get { return false; } }
        /// <summary>Full path of the loaded file.</summary>
        public String LoadedFile { get; protected set; }
        /// <summary>Display string to show on the UI which file was loaded (no path).</summary>
        public String LoadedFileName { get; protected set; }
        /// <summary>Color depth of the file. Note: "-2" switches the program to specific CGA-support 2-bit.</summary>
        public virtual Int32 BitsPerPixel { get { return this.m_LoadedImage == null ? 0 : Image.GetPixelFormatSize(this.m_LoadedImage.PixelFormat); } }
        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public virtual SupportedFileType[] Frames { get { return null; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false will not get an index -1 in the frames list.</summary>
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
        /// <summary>
        /// Array of Booleans which defines for the palette which indices are transparent. Null for no forced transparency.
        /// Note that this is only applied when a palette is loaded into the file from the UI; the class itself is responsible for the loaded file's initial color palette and transparency.
        /// </summary>
        public virtual Boolean[] TransparencyMask { get { return null; } }

        /// <summary>
        /// Load a file from file name.
        /// </summary>
        /// <param name="filename">Original path the file was loaded from.</param>
        public virtual void LoadFile(String filename)
        {

            Byte[] fileData = File.ReadAllBytes(filename);
            this.LoadFile(fileData, filename);
        }
        /// <summary>
        /// Load a file from byte array. Note that the use of this function is discouraged, since many file types refer
        /// to accompanying files, like color palettes, to complete the loaded data, and these cannot be detected without
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
        /// Some animation types are split into separate files, and this sometimes means the later files in the
        /// sequence rely on the a sequence of previous ones to correctly construct their initial state.
        /// File types with that issue should override this function, to analyse which files need to be chained
        /// to get that state. This is a function used by the UI to ask for confirmation for the loading.
        /// This function is always called on loaded frame container types, and must return null or an empty chain
        /// to signal that there is no missing data.
        /// </summary>
        /// <param name="originalPath">Original path the file was loaded from.</param>
        /// <returns>The filenames in the required load chain, or null if there is no missing initial data.</returns>
        public virtual List<String> GetFilesToLoadMissingData(String originalPath) { return null; }

        /// <summary>
        /// Some animation types are split into separate files, and this sometimes means the later files in the
        /// sequence rely on the a sequence of previous ones to correctly construct their initial state.
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
        public virtual Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName) { return new Option[0]; }

        /// <summary>
        /// Saves the given file as this type.
        /// </summary>
        /// <param name="fileToSave">The input file to convert.</param>
        /// <param name="savePath">The path to save to.</param>
        /// <param name="saveOptions">Extra options for customising the save process. Request the list from GetSaveOptions.</param>
        public virtual void SaveAsThis(SupportedFileType fileToSave, String savePath, Option[] saveOptions)
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
        public abstract Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions);

        public virtual void SetFileNames(String path)
        {
            this.LoadedFile = path;
            this.LoadedFileName = Path.GetFileName(path);
        }

        /// <summary>
        /// Gets the colors out of an image. Typically, this takes any specifically saved colors in m_Palette
        /// </summary>
        /// <returns></returns>
        public virtual Color[] GetColors()
        {
            return GetColorsInternal() ?? new Color[0];
        }

        protected Color[] GetColorsInternal()
        {
            if (!this.IsIndexed())
                return null;
            if (this.m_Palette != null)
                return ArrayUtils.CloneArray(m_Palette);
            if (this.m_LoadedImage != null && (this.m_LoadedImage.PixelFormat & PixelFormat.Indexed) != 0)
                return this.m_LoadedImage.Palette.Entries;
            return null;
        }

        protected Boolean IsIndexed()
        {
            Int32 bpp = Math.Abs(this.BitsPerPixel);
            return bpp > 0 && bpp <= 8;

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
            if (this.IsIndexed())
            {
                Int32 maxLen = 1 << Math.Abs(this.BitsPerPixel);
                // Palette length: never more than maxlen, in case of null it equals maxlen, if customised in image, take from image.
                Color[] origPal = GetColorsInternal();
                Int32 origPalLength = origPal == null ? maxLen : Math.Min(origPal.Length, maxLen);
                Color[] newPalette = new Color[origPalLength];
                // Do not apply transparency mask; that should be applied by the file itself, otherwise it can't be changed on the UI.
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
                {
                    try
                    {
                        this.m_LoadedImage.Palette = ImageUtils.GetPalette(newPalette);
                    }
                    catch { }
                }
            }
            if (this.IsFramesContainer && !this.FramesHaveCommonPalette)
                return;
            // Logic if this is a frame: call for a color replace in the parent so all frames get affected.
            // Skip this step if the FrameParent is the source, since that means some other frame already started this.
            if (this.FrameParent != null && !ReferenceEquals(this.FrameParent, updateSource) && this.FrameParent.FramesHaveCommonPalette)
                this.FrameParent.SetColors(palette, this);
            // Logic for frame container: call a color replace on all frames, giving the current object as source.
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
            if (!this.IsIndexed())
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
        /// Palette types can use this to get the color out of a SupportedFileType in their SaveToBytesAsThis routine.
        /// </summary>
        /// <param name="fileToSave">File to save.</param>
        /// <param name="targetBpp">Targeted bits per pixel.</param>
        /// <param name="expandToFullSize">Expand to full size.</param>
        /// <returns>The found colors in the input frames.</returns>
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
                throw new ArgumentException("File to save has no color palette.", "fileToSave");
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

        /// <summary>
        /// Finds an accompanying palette with the same filename, loads it into m_Palette, and adds "/.pal" to the loaded filename.
        /// </summary>
        /// <typeparam name="T">Type of the file to load for the palette.</typeparam>
        /// <param name="inputPath">Input path of the file</param>
        /// <returns>The palette, or null if none was found.</returns>
        protected T CheckForPalette<T>(String inputPath) where T : SupportedFileType, new()
        {
            T palette = null;

            String outputPath = Path.GetDirectoryName(inputPath);
            String palName = Path.GetFileNameWithoutExtension(inputPath) + ".pal";
            String[] files = Directory.GetFiles(outputPath, palName);
            if (files.Length == 0)
            {
                return null;
            }            
            try
            {
                String palFile = files[0];
                palette = new T();
                Byte[] palData = File.ReadAllBytes(palFile);
                palette.LoadFile(palData, palFile);
                this.m_Palette = palette.GetColors();
                this.LoadedFileName += "/" + (Path.GetExtension(palFile) ?? String.Empty).TrimStart('.');
                return palette;
            }
            catch
            {
                palette = null;
            }            
            return palette;
        }

        public static void TestFourBit(Bitmap bm, int frame)
        {
            GetFourBitData(bm, frame, true, false, out _);
        }

        public static byte[] GetFourBitData(Bitmap bm, int frame, bool throwErr, bool returnContent, out int stride)
        {
            stride = 0;
            switch (bm.PixelFormat)
            {
                case PixelFormat.Format4bppIndexed:
                    return returnContent ? ImageUtils.GetImageData(bm, out stride, true) : null;
                case PixelFormat.Format8bppIndexed:
                    byte[] imgData = ImageUtils.GetImageData(bm, true);
                    int dlen = imgData.Length;
                    for (int off = 0; off < dlen; ++off)
                    {
                        if (imgData[off] > 0x0F)
                        {
                            if (throwErr)
                                throw new FileTypeSaveException("Error in frame {2}: " + ERR_BPP_LOW_INPUT, 4, 15, frame);
                            else
                                return null;
                        }
                    }
                    return returnContent ? ImageUtils.ConvertFrom8Bit(imgData, bm.Width, bm.Height, 4, false, ref stride) : null;
                default:
                    if (throwErr)
                        throw new FileTypeSaveException("Error in frame {1}: " + ERR_BPP_INPUT_4_8, frame);
                    else
                        return null;
            }
        }

        /// <summary>
        /// Checks if this is either a single image, or a frames type where all images have the same bpp value,
        /// and returns the found bpp, or -1 if there's no single value. This operation ignores null-frames,
        /// but will return -1 if all frames are null frames.
        /// </summary>
        /// <returns></returns>
        public Int32 GetGlobalBpp()
        {
            Int32 bpp;
            if (!this.IsFramesContainer)
            {
                bpp = Math.Abs(this.BitsPerPixel);
            }
            else
            {
                SupportedFileType[] frames = this.Frames;
                Int32 len = frames.Length;
                bpp = -1;
                for (Int32 i = 0; i < len; ++i)
                {
                    SupportedFileType frame = frames[i];
                    if (frame == null)
                        continue;
                    Int32 frameBpp = Math.Abs(frame.BitsPerPixel);
                    if (bpp == -1)
                        bpp = Math.Abs(frameBpp);
                    else if (bpp != frameBpp)
                    {
                        bpp = -1;
                        break;
                    }
                }
            }
            return bpp;
        }

        public void Dispose()
        {
            SupportedFileType[] frames = this.Frames;
            if (this.IsFramesContainer && frames != null)
            {
                Int32 nrOfFrames = frames.Length;
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    SupportedFileType frame = frames[i];
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
        Image1Bit = 1 << 0,
        Image4Bit = 1 << 1,
        Image8Bit = 1 << 2,
        ImageIndexed = Image1Bit | Image4Bit | Image8Bit,
        ImageHiCol = 1 << 3,
        Image = Image1Bit | Image4Bit | Image8Bit | ImageHiCol,
        FrameSet = 1 << 4,
        CcMap = 1 << 5,
        RaMap = 1 << 6,
    }
}