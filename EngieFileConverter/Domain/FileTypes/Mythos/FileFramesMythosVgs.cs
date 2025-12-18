using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Mythos;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesMythosVgs: SupportedFileType
    {
        public const Byte TransparentIndex = 0xFF;
        protected const String PAL_IDENTIFIER = "VGA palette";
        protected const String X_OFFSET = "X-offset: ";
        protected const String Y_OFFSET = "Y-offset: ";
        protected const String COMPRESSION = "Compression: ";
        protected const String CHUNKS = "Chunks: ";

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }

        public override FileClass FileClass { get { return this.m_fileClass; } }
        private FileClass m_fileClass = FileClass.FrameSet;
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "MythVgs"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Mythos Visage"; } }
        public override String[] FileExtensions { get { return new String[] { "vgs", "lbv", "all" }; } }
        public override String LongTypeName { get { return "Mythos Visage Frames file"; } }
        public override Boolean NeedsPalette { get { return this.m_LoadedPalette == null; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public Int32 CompressionType { get; set; }
        protected String[] compressionTypes = new String[] { "No compression", "Flag-based RLE", "Collapsed transparency" };
        protected List<SupportedFileType> m_FramesList = new List<SupportedFileType>();
        protected String m_LoadedPalette;
        protected Int32 m_Width;
        protected Int32 m_Height;

        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return PaletteUtils.MakePalTransparencyMask(8, TransparentIndex); } }

        // template.
        //public override SaveOption[] GetPostLoadInitOptions()
        //{
        //    return base.GetPostLoadInitOptions();
        //}

        //public override void PostLoadInit(SaveOption[] loadOptions)
        //{
        //
        //}

        public override void LoadFile(Byte[] fileData)
        {
            List<Point> framesXY;
            this.LoadFromFileData(fileData, null, false, false, false, out framesXY, false);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.SetFileNames(filename);
            List<Point> framesXY;
            this.LoadFromFileData(fileData, filename, false, false, false, out framesXY, false);
        }

        /// <summary>
        /// Loads the VGS/VDA file.
        /// </summary>
        /// <param name="fileData">File data in the VGS/VDA file.</param>
        /// <param name="sourcePath">Path of the source file.</param>
        /// <param name="asPalette">True to only read a single frame and treat it as just a palette. Will give load exceptions if the file does not start with a palettes, or is longer than just that palette.</param>
        /// <param name="paletteStrict">True if an exception should be thrown if loading as palette and the file contains more than a palette.</param>
        /// <param name="asVideo">True to load the header bytes according to VDA header format, and store the X and Y offsets of all frames in the framesXY list instead of applying them to the images.</param>
        /// <param name="framesPoints">List of frame X and Y offsets. Only filled in when asVideo is set to true.</param>
        /// <param name="forFrameTest">True abort after reading the first frame, so it can be tested to be a full 320×200 VDA start frame.</param>
        protected void LoadFromFileData(Byte[] fileData, String sourcePath, Boolean asPalette, Boolean paletteStrict, Boolean asVideo, out List<Point> framesPoints, Boolean forFrameTest)
        {
            Int32 fileDataLen = fileData.Length;
            if (fileDataLen < 0x8)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 offset = 0;
            Boolean[] transMask = this.TransparencyMask;
            this.m_FramesList.Clear();
            framesPoints = asVideo ? new List<Point>() : null;
            this.m_LoadedPalette = null;
            this.m_Palette = null;
            this.CompressionType = -1;
            // Read data
            while (offset < fileDataLen)
            {
                if (forFrameTest && this.m_FramesList.Count > 0)
                    return;
                String extraInfo = String.Empty;
                if (offset + 4 > fileDataLen)
                    throw new FileTypeLoadException("Bad header data.");
                Int32 frameWidth = ArrayUtils.ReadInt16FromByteArrayLe(fileData, offset + 0) + 1;
                Int32 frameHeight = ArrayUtils.ReadInt16FromByteArrayLe(fileData, offset + 2) + 1;
                if ((frameWidth < 0 || frameHeight < 0) || (!(frameWidth == 0 && frameHeight == 0) && (frameWidth == 0 || frameHeight == 0)))
                    throw new FileTypeLoadException("Bad header data.");
                Int32 skipLen;
                Byte comprByte;
                Int32 xOffset;
                Int32 yOffset;
                //Int32 extraByte;
                if (!asVideo)
                {
                    //extraByte = fileData[offset + 4];
                    comprByte = fileData[offset + 5];
                    xOffset = fileData[offset + 6];
                    yOffset = fileData[offset + 7];
                }
                else
                {
                    comprByte = fileData[offset + 4];
                    xOffset = fileData[offset + 5] | (fileData[offset + 6] << 8);
                    yOffset = fileData[offset + 7];
                    // frames can be parsed into a video
                }
                Boolean compressed = (asVideo && comprByte == 2) || (!asVideo && comprByte == 1);
                if (!compressed && comprByte != 0)
                        throw new FileTypeLoadException("Unknown compression type: " + comprByte);
                offset += 8;
                Int32 dataLen = frameWidth * frameHeight;
                Byte[] imageData = new Byte[dataLen];
                if (compressed)
                {
                    skipLen = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset) - 8;
                }
                else
                {
                    skipLen = dataLen;
                }
                if (fileData.Length < offset + skipLen)
                    throw new FileTypeLoadException("header references offset outside file data.");
                Int32 frameCompression = 0;
                if (compressed)
                {
                    UInt32 endOffset = (UInt32)(offset + skipLen);
                    try
                    {
                        imageData = MythosCompression.FlagRleDecode(fileData, (UInt32)offset, endOffset, dataLen, true);
                        if (imageData != null)
                        {
                            frameCompression = 1;
                            if (this.CompressionType == -1)
                                this.CompressionType = 1;
                        }
                        else
                        {
                            imageData = MythosCompression.CollapsedTransparencyDecode(fileData, (UInt32)offset, endOffset, dataLen, frameWidth, TransparentIndex, true);
                            if (imageData != null)
                            {
                                frameCompression = 2;
                                if (this.CompressionType == -1)
                                    this.CompressionType = 2;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new FileTypeLoadException(ERR_DECOMPR, e);
                    }
                    if (imageData == null)
                        throw new FileTypeLoadException(ERR_DECOMPR);
                }
                else
                {
                    Array.Copy(fileData, offset, imageData, 0, dataLen);
                }
                // Detect palette on first frame
                if (this.m_LoadedPalette == null && this.m_FramesList.Count == 0 && (imageData.Length == PAL_IDENTIFIER.Length + 0x301))
                {
                    Byte[] compare = new Byte[PAL_IDENTIFIER.Length];
                    Array.Copy(fileData, offset, compare, 0, PAL_IDENTIFIER.Length);
                    // "VGA palette" followed by byte 0x1A identifies as palette.
                    if (compare.SequenceEqual(Encoding.ASCII.GetBytes(PAL_IDENTIFIER)) && imageData[PAL_IDENTIFIER.Length] == 0x1A)
                    {
                        // Palette found! Extract palette and skip frame so it doesn't get added to the list.
                        try
                        {
                            this.m_Palette = ColorUtils.ReadSixBitPalette(imageData, PAL_IDENTIFIER.Length + 1);
                        }
                        catch (ArgumentException e)
                        {
                            throw new FileTypeLoadException(e.Message, e);
                        }
                        PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, transMask);
                        this.m_LoadedPalette = sourcePath;
                        this.ExtraInfo =  "Palette loaded from \"" + PAL_IDENTIFIER + "\" frame.";
                        offset += skipLen;
                        if (asPalette)
                        {
                            if (offset == fileData.Length || !paletteStrict)
                                return;
                            throw new FileTypeLoadException("Not a single palette file.");
                        }
                        continue;
                    }
                    if (asPalette)
                        throw new FileTypeLoadException("Not a palette file.");
                }
                else if (asPalette)
                    throw new FileTypeLoadException("Not a palette file.");
                // safe to execute this now the palette has been handled; otherwise there would be a ghost X and Y for the palette entry.
                if (asVideo)
                {
                    framesPoints.Add(new Point(xOffset, yOffset));
                }
                else if (xOffset > 0 || yOffset > 0 && !asVideo)
                {
                    Int32 newWidth = frameWidth + xOffset;
                    Int32 newHeight = frameHeight + yOffset;
                    Byte[] adjustedData = new Byte[newWidth * newHeight];
                    for (Int32 i = 0; i < adjustedData.Length; ++i)
                        adjustedData[i] = TransparentIndex;
                    ImageUtils.PasteOn8bpp(adjustedData, newWidth, newHeight, newWidth, imageData, frameWidth, frameHeight, frameWidth,
                        new Rectangle(xOffset, yOffset, frameWidth, frameHeight), transMask, true);
                    imageData = adjustedData;
                    frameWidth = newWidth;
                    frameHeight = newHeight;
                }
                if (xOffset > 0)
                    extraInfo += X_OFFSET + xOffset + (asVideo ? " (not applied)" : String.Empty) + "\n";
                if (yOffset > 0)
                    extraInfo += Y_OFFSET + yOffset + (asVideo ? " (not applied)" : String.Empty) + "\n";
                if (this.m_Palette == null)
                {
                    if (sourcePath != null && !sourcePath.EndsWith(".pal", StringComparison.InvariantCultureIgnoreCase))
                    {
                        SupportedFileType palette = CheckForPalette<FileFramesMythosPal>(sourcePath);
                        if (palette == null)
                            palette = CheckForPalette<FilePalette6Bit>(sourcePath);
                        if (palette == null)
                            palette = CheckForPalette<FilePalette8Bit>(sourcePath);
                        if (palette != null)
                        {
                            this.m_Palette = PaletteUtils.ApplyPalTransparencyMask(palette.GetColors(), transMask);
                            this.m_LoadedPalette = palette.LoadedFile;
                            this.ExtraInfo = "Palette loaded from \"" + Path.GetFileName(palette.LoadedFile) + "\".";
                        }
                    }
                    if (this.m_Palette == null)
                        this.m_Palette = PaletteUtils.GenerateGrayPalette(8, transMask, false);

                }
                Bitmap curImage = ImageUtils.BuildImage(imageData, frameWidth, frameHeight, frameWidth, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, curImage, sourcePath, this.m_FramesList.Count);
                frame.SetNeedsPalette(this.m_LoadedPalette == null);
                frame.SetColors(this.m_Palette, this);
                extraInfo += COMPRESSION + this.compressionTypes[frameCompression];
                frame.SetExtraInfo(extraInfo.TrimEnd('\n'));
                this.m_FramesList.Add(frame);
                offset += skipLen;
            }
            if (this.CompressionType == -1)
                this.CompressionType = 0;
            if (this.ExtraInfo == null)
                this.ExtraInfo = String.Empty;
            if (this.ExtraInfo.Length > 0)
                this.ExtraInfo += Environment.NewLine;
            this.ExtraInfo += "Used compression: " + this.compressionTypes[this.CompressionType] + ".";
            if (offset != fileData.Length)
                throw new FileTypeLoadException("Image load failed.");
            if (this.m_LoadedPalette != null && this.m_FramesList.Count == 0)
                throw new FileTypeLoadException("this is a palette-only VGS! Load it as palette.");
            if (!asVideo && this.m_LoadedPalette != null && this.m_FramesList.Count == 1 && this.m_FramesList[0].Width == 320 && this.m_FramesList[0].Height == 200)
            {
                this.m_fileClass = FileClass.Image8Bit;
                this.m_LoadedImage = this.m_FramesList[0].GetBitmap();
                this.m_FramesList.Clear();
                this.m_Width = this.m_LoadedImage.Width;
                this.m_Height = this.m_LoadedImage.Height;

                this.ExtraInfo += Environment.NewLine + "Treated as single full-screen image.";
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 compression = 0;
            Boolean hasXOpt = false;
            Boolean hasYOpt = false;
            Boolean hasPal = false;
            FileFramesMythosVgs fileVgs = fileToSave as FileFramesMythosVgs;
            if (fileVgs != null)
            {
                hasPal = fileVgs.m_LoadedPalette != null;
                compression = fileVgs.CompressionType;
                SupportedFileType[] frames = fileVgs.Frames;
                Int32 nrOfFrames = frames.Length;
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    SupportedFileType frame = frames[i];
                    String[] extraInfo = frame.ExtraInfo.Split(new Char[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
                    // Kind of dirty but whatev.
                    Int32 extraInfoLines = extraInfo.Length;
                    for (Int32 j = 0; j < extraInfoLines; ++j)
                    {
                        String info = extraInfo[j];
                        if (info.StartsWith(X_OFFSET))
                            hasXOpt = true;
                        else if (info.StartsWith(Y_OFFSET))
                            hasYOpt = true;
                        if (hasXOpt && hasYOpt)
                            break;
                    }
                    if (hasXOpt && hasYOpt)
                        break;
                }
            }
            if (compression < 0 || compression > this.compressionTypes.Length)
                compression = 0;
            return new Option[]
            {
                new Option("PAL", OptionInputType.Boolean, "Save palette into file as first frame", hasPal ? "1" : "0"),
                new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new Option("OPX", OptionInputType.Boolean, "Optimize empty horizontal space to X-offsets", hasXOpt ? "1" : "0"),
                new Option("OPY", OptionInputType.Boolean, "Optimize empty vertical space to Y-offsets", hasYOpt ? "1" : "0"),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave);
            Int32 nrOfFrames = frames.Length;
            Boolean asPaletted = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PAL"));
            Boolean optimiseX = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "OPX"));
            Boolean optimiseY = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "OPY"));
            Int32 compressionType;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (compressionType < 0 || compressionType > 2)
                compressionType = 0;

            Boolean paletteOnly = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PALONLY"));
            if (paletteOnly)
            {
                asPaletted = true;
                compressionType = 0;
            }
            Color[] palette = null;
            if (asPaletted)
                palette = CheckInputForColors(fileToSave, this.BitsPerPixel, false);
            Int32 actualLen = paletteOnly ? 0 : nrOfFrames;
            if (asPaletted)
                actualLen++;
            Byte[][] frameData = new Byte[actualLen][];
            Boolean[] compressed = new Boolean[actualLen];
            Int32[] widths = new Int32[actualLen];
            Int32[] heighths = new Int32[actualLen];
            Byte[] xOffsets = new Byte[actualLen];
            Byte[] yOffsets = new Byte[actualLen];
            if (asPaletted)
            {
                // Don't think this can happen at this point.
                if (palette == null)
                    throw new ArgumentException("There is no palette available in the given frames.", "fileToSave");
                // Add extra frame with palette header to serve as internal palette.
                Byte[] frameBytes = new Byte[780];
                Array.Copy(Encoding.ASCII.GetBytes(PAL_IDENTIFIER), 0, frameBytes, 0, PAL_IDENTIFIER.Length);
                frameBytes[PAL_IDENTIFIER.Length] = 0x1A;
                Byte[] colorBytes = ColorUtils.GetSixBitPaletteData(palette);
                Array.Copy(colorBytes, 0, frameBytes, PAL_IDENTIFIER.Length + 1, colorBytes.Length);
                frameData[0] = frameBytes;
                compressed[0] = false;
                widths[0] = 390;
                heighths[0] = 2;
                xOffsets[0] = 0;
                yOffsets[0] = 0;
            }
            for (Int32 i = asPaletted ? 1 : 0; i < actualLen; ++i)
            {
                Int32 index = i;
                if (asPaletted)
                    --index;
                SupportedFileType frame = frames[index];
                Int32 stride;
                Byte[] frameBytes = ImageUtils.GetImageData(frame.GetBitmap(), out stride, true);
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                Int32 xOffset = 0;
                Int32 yOffset = 0;
                widths[i] = width;
                heighths[i] = height;
                if (optimiseX)
                    frameBytes = ImageUtils.OptimizeXWidth(frameBytes, ref width, height, ref xOffset, true, TransparentIndex, 0xFF, true);
                if (optimiseY)
                    frameBytes = ImageUtils.OptimizeYHeight(frameBytes, width, ref height, ref yOffset, true, TransparentIndex, 0xFF, true);
                Byte[] compressedBytes = null;
                if (compressionType > 0)
                {
                    try
                    {
                        if (compressionType == 1)
                            compressedBytes = MythosCompression.FlagRleEncode(frameBytes, 0xFE, width, 8);
                        else if (compressionType == 2)
                            compressedBytes = MythosCompression.CollapsedTransparencyEncode(frameBytes, TransparentIndex, width, 8);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
                    }
                }
                frameData[i] = compressedBytes ?? frameBytes;
                compressed[i] = compressedBytes != null;
                widths[i] = width;
                heighths[i] = height;
                xOffsets[i] = (Byte)xOffset;
                yOffsets[i] = (Byte)yOffset;
            }
            Int32 dataSum = 0;
            for (Int32 i = 0; i < actualLen; ++i)
                dataSum += frameData[i].Length;
            Byte[] finalData = new Byte[actualLen * 8 + dataSum];
            Int32 offset = 0;
            for (Int32 i = 0; i < actualLen; ++i)
            {
                ArrayUtils.WriteUInt16ToByteArrayLe(finalData, offset + 0, (UInt16)(widths[i] - 1));
                ArrayUtils.WriteUInt16ToByteArrayLe(finalData, offset + 2, (UInt16)(heighths[i] - 1));
                //finalData[offset + 4] = 0x00;
                finalData[offset + 5] = (Byte)(compressed[i] ? 1 : 0);
                finalData[offset + 6] = xOffsets[i];
                finalData[offset + 7] = yOffsets[i];
                offset += 8;
                Byte[] curSymbolData = frameData[i];
                Array.Copy(curSymbolData, 0, finalData, offset, curSymbolData.Length);
                offset += curSymbolData.Length;
            }
            return finalData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames == null ? 0 : frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_FRAMES_EMPTY, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
            }
            return frames;
        }

    }
}

