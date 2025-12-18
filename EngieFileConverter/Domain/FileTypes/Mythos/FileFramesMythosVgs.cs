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

        public override Int32 Width { get { return m_Width; } }
        public override Int32 Height { get { return m_Height; } }

        public override FileClass FileClass { get { return this.m_fileClass; } }
        private FileClass m_fileClass = FileClass.FrameSet;
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Mythos Visage"; } }
        public override String[] FileExtensions { get { return new String[] { "vgs", "lbv", "all" }; } }
        public override String ShortTypeDescription { get { return "Mythos Visage frames file"; } }
        public override Int32 ColorsInPalette { get { return this.m_PaletteSet ? this.m_Palette.Length : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        public Int32 CompressionType { get; set; }
        protected String[] compressionTypes = new String[] { "No compression", "Flag-based RLE", "Collapsed transparency" };
        protected List<SupportedFileType> m_FramesList = new List<SupportedFileType>();
        protected Boolean m_PaletteSet;
        protected Int32 m_Width = 0;
        protected Int32 m_Height = 0;

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
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
        /// <param name="framesXY">List of frame X and Y offsets. Only filled in when asVideo is set to true.</param>
        /// <param name="forFrameTest">True abort after reading the first frame, so it can be tested to be a full 320×200 VDA start frame.</param>
        protected void LoadFromFileData(Byte[] fileData, String sourcePath, Boolean asPalette, Boolean paletteStrict, Boolean asVideo, out List<Point> framesXY, Boolean forFrameTest)
        {
            if (fileData.Length < 0x8)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 offset = 0;
            Boolean[] transMask = this.TransparencyMask;
            this.m_FramesList.Clear();
            framesXY = asVideo ? new List<Point>() : null;
            this.m_PaletteSet = false;
            this.m_Palette = null;
            this.CompressionType = -1;
            // Read data
            while (offset < fileData.Length)
            {
                if (forFrameTest && this.m_FramesList.Count > 0)
                    return;
                String extraInfo = String.Empty;
                Int32 frameWidth = (Int16) ArrayUtils.ReadIntFromByteArray(fileData, offset + 0, 2, true) + 1;
                Int32 frameHeight = (Int16) ArrayUtils.ReadIntFromByteArray(fileData, offset + 2, 2, true) + 1;
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
                    skipLen = (UInt16) ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true) - 8;
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
                        MythosCompression mc = new MythosCompression();
                        imageData = mc.FlagRleDecode(fileData, (UInt32)offset, endOffset, dataLen, true);
                        if (imageData != null)
                        {
                            frameCompression = 1;
                            if (this.CompressionType == -1)
                                this.CompressionType = 1;
                        }
                        else
                        {
                            imageData = mc.CollapsedTransparencyDecode(fileData, (UInt32)offset, endOffset, dataLen, frameWidth, TransparentIndex, true);
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
                        throw new FileTypeLoadException("Cannot decompress VGS file!", e);
                    }
                    if (imageData == null)
                        throw new FileTypeLoadException("Cannot decompress VGS file!");
                }
                else
                {
                    Array.Copy(fileData, offset, imageData, 0, dataLen);
                }
                // Detect palette on first frame
                if (!this.m_PaletteSet && this.m_FramesList.Count == 0 && (imageData.Length == PAL_IDENTIFIER.Length + 0x301))
                {
                    Byte[] compare = new Byte[PAL_IDENTIFIER.Length];
                    Array.Copy(fileData, offset, compare, 0, PAL_IDENTIFIER.Length);
                    // "VGA palette" followed by byte 0x1A identifies as palette.
                    if (compare.SequenceEqual(Encoding.ASCII.GetBytes(PAL_IDENTIFIER)) && imageData[PAL_IDENTIFIER.Length] == 0x1A)
                    {
                        // Palette found! Extract palette and skip frame so it doesn't get added to the list.
                        Byte[] paletteData = new Byte[0x300];
                        Array.Copy(imageData, PAL_IDENTIFIER.Length + 1, paletteData, 0, 0x300);
                        this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPaletteFile(paletteData));
                        PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, transMask);
                        this.m_PaletteSet = true;
                        this.ExtraInfo =  "Palette loaded from \"" + PAL_IDENTIFIER + "\" frame.";
                        offset += skipLen;
                        if (asPalette)
                        {
                            if (offset == fileData.Length || !paletteStrict)
                                return;
                            throw new FileTypeLoadException("Not a single palette file!");
                        }
                        continue;
                    }
                    if (asPalette)
                        throw new FileTypeLoadException("Not a palette file!");
                }
                else if (asPalette)
                    throw new FileTypeLoadException("Not a palette file!");
                // safe to execute this now the palette has been handled; otherwise there would be a ghost X and Y for the palette entry.
                if (asVideo)
                {
                    framesXY.Add(new Point(xOffset, yOffset));
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
                        String palName = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + ".PAL");
                        if (File.Exists(palName))
                        {
                            Byte[] palData = File.ReadAllBytes(palName);
                            // Try loading the file as different palette types.
                            Type[] paletteTypes = {typeof(FileFramesMythosPal), typeof(FilePalette6Bit), typeof(FilePalette8Bit)};
                            Int32 nrOfPalTypes = paletteTypes.Length;
                            for (Int32 i = 0; i < nrOfPalTypes; ++i)
                            {
                                if (this.m_Palette != null)
                                    break;
                                try
                                {
                                    SupportedFileType palFile = (SupportedFileType)Activator.CreateInstance(paletteTypes[i]);
                                    palFile.LoadFile(palData, palName);
                                    this.m_Palette = palFile.GetColors();
                                }
                                catch
                                {
                                    // ignore.
                                }
                            }
                            if (this.m_Palette != null)
                            {
                                PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, transMask);
                                this.m_PaletteSet = true;
                                this.ExtraInfo = "Palette loaded from \"" + Path.GetFileName(palName) + "\".";
                                this.LoadedFileName += "/PAL";
                            }
                        }
                    }
                    if (this.m_Palette == null)
                        this.m_Palette = PaletteUtils.GenerateGrayPalette(8, transMask, false);
                    
                }
                Bitmap curImage = ImageUtils.BuildImage(imageData, frameWidth, frameHeight, frameWidth, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, curImage, sourcePath, this.m_FramesList.Count);
                frame.SetColorsInPalette(this.m_PaletteSet ? this.m_Palette.Length : 0);
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
            if (this.m_PaletteSet && this.m_FramesList.Count == 0)
                throw new FileTypeLoadException("this is a palette-only VGS! Load it as palette.");
            if (!asVideo && this.m_PaletteSet && this.m_FramesList.Count == 1 && this.m_FramesList[0].Width == 320 && this.m_FramesList[0].Height == 200)
            {
                this.m_fileClass = FileClass.Image8Bit;
                this.m_LoadedImage = this.m_FramesList[0].GetBitmap();
                this.m_FramesList.Clear();
                m_Width = m_LoadedImage.Width;
                m_Height = m_LoadedImage.Height;

                this.ExtraInfo += Environment.NewLine + "Treated as single full-screen image.";
            }
        }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            Int32 compression = 0;
            Boolean hasXOpt = false;
            Boolean hasYOpt = false;
            Boolean hasPal = false;
            FileFramesMythosVgs fileVgs = fileToSave as FileFramesMythosVgs;
            if (fileVgs != null)
            {
                hasPal = fileVgs.m_PaletteSet;
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
            return new SaveOption[]
            {
                new SaveOption("PAL", SaveOptionType.Boolean, "Save palette into file as first frame", hasPal ? "1" : "0"),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new SaveOption("OPX", SaveOptionType.Boolean, "Optimize empty horizontal space to X-offsets", hasXOpt ? "1" : "0"),
                new SaveOption("OPY", SaveOptionType.Boolean, "Optimize empty vertical space to Y-offsets", hasYOpt ? "1" : "0"),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new NotSupportedException("No source data given!");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] {fileToSave};
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new NotSupportedException("This format needs at least one frame.");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType sft = frames[i];
                if (sft == null || sft.GetBitmap() == null)
                    throw new NotSupportedException("Mythos VGS can't handle empty frames!");
                if (sft.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8bpp images.");
                if (sft.Width != 320 || sft.Height != 200)
                    throw new NotSupportedException("This format needs 320x200 frames.");
            }
            Boolean asPaletted = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            Boolean optimiseX = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "OPX"));
            Boolean optimiseY = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "OPY"));
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (compressionType < 0 || compressionType > 2)
                compressionType = 0;

            Boolean paletteOnly = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PALONLY"));
            if (paletteOnly)
            {
                asPaletted = true;
                compressionType = 0;
            }
            Color[] palette = null;
            if (asPaletted)
                palette = this.CheckInputForColors(fileToSave, true);
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
                    throw new NotSupportedException("There is no palette available in the given frames!");
                // Add extra frame with palette header to serve as internal palette.
                Byte[] frameBytes = new Byte[780];
                Array.Copy(Encoding.ASCII.GetBytes(PAL_IDENTIFIER), 0, frameBytes, 0, PAL_IDENTIFIER.Length);
                frameBytes[PAL_IDENTIFIER.Length] = 0x1A;
                Byte[] colorBytes = ColorUtils.GetSixBitPaletteData(ColorUtils.GetSixBitColorPalette(palette));
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
                    MythosCompression mc = new MythosCompression();
                    try
                    {
                        if (compressionType == 1)
                            compressedBytes = mc.FlagRleEncode(frameBytes, 0xFE, width, 8);
                        else if (compressionType == 2)
                            compressedBytes = mc.CollapsedTransparencyEncode(frameBytes, TransparentIndex, width, 8);
                    }
                    catch (OverflowException ex)
                    {
                        throw new NotSupportedException(ex.Message, ex);
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
                ArrayUtils.WriteIntToByteArray(finalData, offset + 0, 2, true, (UInt32)(widths[i] - 1));
                ArrayUtils.WriteIntToByteArray(finalData, offset + 2, 2, true, (UInt32)(heighths[i]-1));
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
    }
}

