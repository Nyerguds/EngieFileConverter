using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileFramesWwWsa : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return this.m_Width; }  }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        protected String[] formats = new String[] { "Dune II v1.00", "Dune II v1.07", "C&C"}; //, "Monopoly" };
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood WSA"; } }
        public override String[] FileExtensions { get { return new String[] { "wsa" }; } }
        public override String ShortTypeDescription { get { return "Westwood Animation File"; } }
        public override Int32 ColorsInPalette { get { return this.m_HasPalette? 0x100 : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_HasPalette;
        protected WsaVersion m_Version = WsaVersion.Cnc;
        protected Boolean m_HasLoopFrame;
        protected Boolean m_DamagedLoopFrame;
        protected Boolean m_Continues;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }


        public override List<String> GetFilesToLoadMissingData(String originalPath)
        {
            if (!m_Continues)
                return null;
            // If a single png file of the same name is found it overrides normal chaining.
            String pngName = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + ".PNG");
            // Existence check + original case retrieve.
            String[] pngNames = Directory.GetFiles(Path.GetDirectoryName(pngName), Path.GetFileName(pngName));
            if (pngNames.Length > 0)
            {
                pngName = pngNames[0];
                try
                {
                    using (FileImagePng pngFile = new FileImagePng())
                    {
                        pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                        if (pngFile.Width == this.Width && pngFile.Height == this.Height)
                            return new List<String>() {pngName};
                    }
                }
                catch (FileLoadException)
                {
                    // ignore; continue with normal load
                }
            }
            String baseNameDummy;
            // check numeric ranges first; you never know
            String[] frameNames = FileFrames.GetFrameFilesRange(originalPath, out baseNameDummy);
            // If no frames were found, use usual WSA A-Z detect logic.
            if (frameNames == null)
            {
                String dir = Path.GetDirectoryName(originalPath);
                String nameNoExt = Path.GetFileNameWithoutExtension(originalPath);
                String ext = Path.GetExtension(originalPath);
                if (String.IsNullOrEmpty(nameNoExt))
                    return null;
                Char endChar = nameNoExt.ToLower().Last();
                // no alphabetic last character, or it is an 'A' and thus there can't be any previous chained files.
                if (endChar <= 'a' || endChar > 'z')
                    return null;
                String baseName = nameNoExt.Substring(0, nameNoExt.Length - 1);
                List<String> frameNamesList = new List<String>();
                String[] files = Directory.GetFiles(dir, baseName + '?' + ext);
                for (Char endch = endChar; endch >= 'a'; endch--)
                {
                    String curPath = Path.Combine(dir, baseName + endch + ext);
                    Int32 findex = Array.FindIndex(files, t => String.Equals(t, curPath, StringComparison.InvariantCultureIgnoreCase));
                    if (findex != -1)
                        frameNamesList.Add(files[findex]);
                    else
                        break;
                }
                frameNamesList.Reverse();
                frameNames = frameNamesList.ToArray();
            }
            List<String> chain = new List<String>();
            Int32 index = Array.FindIndex(frameNames.ToArray(), t => String.Equals(t, originalPath, StringComparison.InvariantCultureIgnoreCase));
            for (Int32 i = index - 1; i >= 0; i--)
            {
                String curName = frameNames[i];
                Byte[] testBytes = File.ReadAllBytes(curName);
                // Clean up used images after check. Probably not needed for WSA since the testContinue check makes it abort without actually storing any.
                using (FileFramesWwWsa testFrame = new FileFramesWwWsa())
                {
                    // Call with testContinue param to abort after confirming initial frame state.
                    try { testFrame.LoadFromFileData(testBytes, curName, m_Version, null, true); }
                    catch (HeaderParseException) { return null; }
                    if (testFrame.Width != this.Width || testFrame.Height != this.Height)
                        return null;
                    if (!testFrame.m_Continues)
                    {
                        chain.Add(curName);
                        chain.Reverse();
                        return chain;
                    }
                    chain.Add(curName);
                }
            }
            return null;
        }

        public override void ReloadFromMissingData(Byte[] fileData, String originalPath, List<String> loadChain)
        {
            Byte[] lastFrameData = null;
            String firstName = loadChain.FirstOrDefault();
            if (loadChain.Count == 1 && loadChain[0].EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
            {
                String pngName = loadChain[0];
                if(File.Exists(pngName))
                {
                    // Get actual name
                    try
                    {
                        // Extra frame is not used; always dispose image.
                        using (FileImageFrame pngFile = new FileImageFrame())
                        {
                            pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                            Bitmap bm = pngFile.GetBitmap();
                            if (bm.Width != this.Width || bm.Height != this.Height)
                                return;
                            Int32 stride;
                            lastFrameData = ImageUtils.GetImageData(bm, out stride);
                            lastFrameData = ImageUtils.CollapseStride(lastFrameData, bm.Width, bm.Height, 8, ref stride);
                            if (lastFrameData != null)
                                loadChain.Clear();
                        }
                    }
                    catch { return; }// can't load as png file. Abort.
                }
            }
            foreach (String chainFilePath in loadChain)
            {
                Byte[] chainFileBytes = File.ReadAllBytes(chainFilePath);
                using (FileFramesWwWsa chainFile = new FileFramesWwWsa())
                {
                    try { chainFile.LoadFromFileData(chainFileBytes, chainFilePath, m_Version, lastFrameData, false); }
                    catch { return; }
                    Int32 lastFrameIndex = chainFile.Frames.Length - 1;
                    if (lastFrameIndex  < 0)
                        return;
                    Bitmap lastFrame = chainFile.Frames[lastFrameIndex].GetBitmap();
                    if (lastFrame == null)
                        return;
                    Int32 stride;
                    Int32 width = lastFrame.Width;
                    Int32 height = lastFrame.Height;
                    if (width != this.Width || height != this.Height)
                        return;
                    lastFrameData = ImageUtils.GetImageData(lastFrame, out stride);
                    lastFrameData = ImageUtils.CollapseStride(lastFrameData, width, height, 8, ref stride);
                }
            }
            this.LoadFromFileData(fileData, originalPath, m_Version, lastFrameData, false);
            this.ExtraInfo = (this.ExtraInfo  + "\nData chained from " + Path.GetFileName(firstName)).TrimStart('\n');
        }
        
        
        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            // Loop over all versions.
            WsaVersion[] versions = Enum.GetValues(typeof (WsaVersion)).Cast<WsaVersion>().ToArray();
            Int32 lenmax = versions.Length - 1;
            for (Int32 i = 0; i <= lenmax; i++)
            {
                try
                {
                    this.LoadFromFileData(fileData, sourcePath, versions[i], null, false);
                    break;
                }
                catch (HeaderParseException)
                {
                    // Only catches the specific header file size check. If there are more items in the enum,
                    // continue the detection process. If it's the last one, just throw the exception.
                    // It subclasses FileTypeLoadException, so the global autodetect process will catch it.
                    if (i == lenmax)
                        throw;
                }
            }
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath, WsaVersion loadVersion, Byte[] continueData, Boolean testContinue)
        {
            this.m_Continues = false;
            Int32 datalen = fileData.Length;
            if (datalen < 14)
                throw new FileTypeLoadException("Bad header size.");
            UInt16 nrOfFrames = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            // Default for v3 (C&C). This is missing and shifted down 4 bytes for the D2 formats.
            // The D2-specific code corrects this later.
            UInt16 xPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 2, 2, true);
            UInt16 yPos = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 4, 2, true);
            UInt16 width = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 6, 2, true);
            UInt16 height = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, 8, 2, true);
            Int32 deltaOffs = 0x0A;
            UInt32 deltaBufferSize;
            UInt16 flags = 0;
            // If the type is Dune 2, the "width" value actually contains the buffer size, so it's practically impossible this is below 320.
            switch (loadVersion)
            {
                case WsaVersion.Dune2:
                case WsaVersion.Dune2v1:
                    width = xPos;
                    height = yPos;
                    xPos=0;
                    yPos=0;
                    // Compensate for missing X and Y offsets
                    deltaOffs -= 4;
                    this.m_Version = WsaVersion.Dune2;
                    deltaBufferSize = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, deltaOffs, 2, true);
                    // d2v1 has no flags, and can thus never contain a palette.
                    if (loadVersion == WsaVersion.Dune2v1)
                        deltaOffs -= 2;  // Decrease this to have data index offset correct later.
                    else
                        flags = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, deltaOffs + 2, 2, true);
                    break;
                case WsaVersion.Cnc:
                    deltaBufferSize = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, deltaOffs, 2, true);
                    flags = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, deltaOffs + 2, 2, true);
                    break;
                default:
                    // Might need specific poly handling here... but then I first need
                    // accurate checks to distinguish c&c from poly.
                    throw new FileTypeLoadException("Unknown WSA format!");
            }
            this.m_Version = loadVersion;
            String generalInfo = "Version: " + this.formats[(Int32) this.m_Version];
            if (this.m_Version != WsaVersion.Dune2 && this.m_Version != WsaVersion.Dune2v1)
                generalInfo += "\nX-offset = " + xPos + "\nY-offset = " + yPos;
            this.ExtraInfo = generalInfo;
            Int32 dataIndexOffset = deltaOffs + 4;
            Int32 paletteOffset = dataIndexOffset + (nrOfFrames + 2) * 4;
            this.m_HasPalette = (flags & 1) == 1;
            UInt32[] frameOffsets = new UInt32[nrOfFrames + 2];
            for (Int32 i = 0; i < nrOfFrames + 2; i++)
            {
                if (fileData.Length <= dataIndexOffset + 4)
                    throw new HeaderParseException("Data too short to contain frames info!");
                frameOffsets[i] = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, dataIndexOffset, 4, true);
                dataIndexOffset += 4;
            }
            this.m_HasLoopFrame = frameOffsets[nrOfFrames + 1] != 0;
            UInt32 endOffset = frameOffsets[nrOfFrames + (this.m_HasLoopFrame ? 1 : 0)];
            if (this.m_HasPalette)
                endOffset += 0x300;
            if (endOffset != fileData.Length)
                throw new HeaderParseException("Data size does not correspond with file size!");
            if (this.m_HasPalette)
            {
                if (fileData.Length < paletteOffset + 0x300)
                    throw new FileTypeLoadException("File is not long enough for color palette!");
                Byte[] pal = new Byte[0x300];
                Array.Copy(fileData, paletteOffset, pal, 0, 0x300);
                try
                {
                    this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPaletteFile(pal));
                }
                catch (NotSupportedException e)
                {
                    throw new FileTypeLoadException("Error loading color palette: " + e.Message, e);
                }
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            if (width == 0 || height == 0)
                throw new FileTypeLoadException("Invalid image dimensions!");
            this.m_Width = width;
            this.m_Height = height;
            this.m_FramesList = new SupportedFileType[nrOfFrames + 1];
            Byte[] frameData = new Byte[width * height];
            Byte[] frame0Data = new Byte[width * height];
            Byte[] xorData = new Byte[deltaBufferSize + 37];
            for (Int32 i = 0; i < nrOfFrames + 1; i++)
            {
                String specificInfo = String.Empty;
                UInt32 frameOffset = frameOffsets[i];
                UInt32 frameOffsetReal = frameOffset;
                if (this.m_HasPalette)
                    frameOffsetReal += 0x300;

                if (i == 0 && frameOffset == 0)
                {
                    this.m_Continues = true;
                    if (continueData != null)
                    {
                        if (continueData.Length != frameData.Length)
                            throw new FileTypeLoadException("Invalid size on substituted initial frame!");
                        Array.Copy(continueData, frameData, frameData.Length);
                        specificInfo = "\nLoaded from previous file";
                    }
                    else
                    {
                        Array.Clear(frameData, 0, frameData.Length);
                        specificInfo = "\nContinues from a previous file";
                        this.ExtraInfo += specificInfo;   
                    }                 
                }
                if (testContinue)
                    return;
                if (frameOffsetReal == fileData.Length)
                    break;
                Int32 refOff = (Int32)frameOffsetReal;
                Int32 uncLen;
                try
                {
                    uncLen = WWCompression.LcwDecompress(fileData, ref refOff, xorData);
                }
                catch (Exception ex)
                {
                    throw new FileTypeLoadException("LCW Decompression failed: " + ex.Message, ex);
                }
                try
                {
                    refOff = 0;
                    WWCompression.ApplyXorDelta(frameData, xorData, ref refOff, uncLen);
                }
                catch (Exception ex)
                {
                    throw new FileTypeLoadException("XOR Delta merge failed: " + ex.Message, ex);
                }
                if (i == 0)
                    Array.Copy(frameData, frame0Data, frameData.Length);
                Byte[] finalFrameData = frameData;
                Int32 finalWidth = width + xPos;
                Int32 finalHeight = height + yPos;
                if (xPos > 0 || yPos > 0)
                {
                    finalFrameData = ImageUtils.ChangeStride(frameData, width, height, finalWidth, true, 0);
                    finalFrameData = ImageUtils.ChangeHeight(finalFrameData, finalWidth, height, finalHeight, true, 0);
                }
                Bitmap curFrImg = ImageUtils.BuildImage(finalFrameData, finalWidth, finalHeight, finalWidth, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetColorsInPalette(this.ColorsInPalette);
                frame.SetExtraInfo(generalInfo + specificInfo);
                frame.SetTransparencyMask(this.TransparencyMask);
                this.m_FramesList[i] = frame;
            }
            this.m_DamagedLoopFrame = false;
            if (this.m_HasLoopFrame)
            {
                this.m_DamagedLoopFrame = !frameData.SequenceEqual(frame0Data);
                this.ExtraInfo += "\nHas loop frame";
                if (this.m_DamagedLoopFrame)
                    this.ExtraInfo += " (but doesn't match)";
            }
            if (!this.m_DamagedLoopFrame)
            {
                SupportedFileType[] newFramesList = new SupportedFileType[nrOfFrames];
                Array.Copy(this.m_FramesList, newFramesList, nrOfFrames);
                this.m_FramesList = newFramesList;
            }
            else
            {
                FileImageFrame frame = (FileImageFrame)this.m_FramesList[nrOfFrames];
                frame.SetExtraInfo(frame.ExtraInfo + "\nLoop frame (damaged?)");
            }
            if (this.m_FramesList.Length == 1)
                this.m_LoadedImage = this.m_FramesList[0].GetBitmap();
        }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = fileToSave != null && !(fileToSave is FileImage) && fileToSave.ColorsInPalette != 0;
            WsaVersion type = WsaVersion.Cnc;
            Boolean loop = true;
            Boolean trim = true;
            Boolean continues = false;
            Boolean ignoreLast = false;
            FileFramesWwWsa toSave = fileToSave as FileFramesWwWsa;
            if (toSave != null)
            {
                type = toSave.m_Version;
                loop = toSave.m_HasLoopFrame;
                if (type == WsaVersion.Dune2 || type == WsaVersion.Dune2v1)
                    trim = false;
                if (type == WsaVersion.Dune2v1)
                    hasColors = false;
                continues = toSave.m_Continues;
                ignoreLast = toSave.m_DamagedLoopFrame;
            }
            SaveOption[] opts = new SaveOption[ignoreLast ? 6 : 5];
            opts[1] = new SaveOption("TYPE", SaveOptionType.ChoicesList, "Type:", String.Join(",", this.formats), ((Int32)type).ToString());
            opts[0] = new SaveOption("PAL", SaveOptionType.Boolean, "Include palette (not supported for Dune II v1)", hasColors ? "1" : "0");
            opts[2] = new SaveOption("LOOP", SaveOptionType.Boolean, "Loop", null, loop ? "1" : "0");
            opts[3] = new SaveOption("CONT", SaveOptionType.Boolean, "Don't save initial frame", null, continues ? "1" : "0");
            opts[4] = new SaveOption("CROP", SaveOptionType.Boolean, "Crop to X and Y offsets (C&C type only)", null, trim ? "1" : "0");
            if (ignoreLast)
                opts[5] = new SaveOption("CUT", SaveOptionType.Boolean, "Ignore broken input loop frame", null, "1");
            return opts;
        }


        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");
            Int32 width = -1;
            Int32 height = -1;
            Color[] palette = null;
            foreach (SupportedFileType frame in fileToSave.Frames)
            {
                if (frame == null)
                    throw new NotSupportedException("WSA can't handle empty frames!");
                if (frame.BitsPerPixel != 8)
                    throw new NotSupportedException("Not all frames in input type are 8-bit images!");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new NotSupportedException("Not all frames in input type are the same size!");
                if (palette == null)
                    palette = frame.GetColors();
            }
            // Save options
            Int32 type;
            WsaVersion saveType;
            if (!Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "TYPE"), out type) || !Enum.IsDefined(typeof (WsaVersion), type))
                saveType = WsaVersion.Cnc;
            else
                saveType = (WsaVersion)type;

            Boolean asPaletted = saveType != WsaVersion.Dune2v1 && GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            Boolean loop = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "LOOP"));
            Boolean crop = saveType != WsaVersion.Dune2v1 && saveType != WsaVersion.Dune2 && GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CROP"));
            Boolean cut = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CUT"));
            Boolean continues = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CONT"));


            // Fetch and compress data
            Int32 readFrames = fileToSave.Frames.Length;
            Int32 writeFrames = readFrames;
            if (cut)
            {
                readFrames--;
                writeFrames--;
            }
            // Technically can't happen... I think.
            if (readFrames == 0)
                throw new NotSupportedException("No frames in source data!");
            if (loop)
                writeFrames++;
            Byte[][] framesDataUnc = new Byte[writeFrames][];
            Byte[] firstFrameData = continues ? new Byte[width * height] : null;
            for (Int32 i = 0; i < writeFrames; i++)
            {
                Byte[] frameDataRaw;
                if (i < readFrames)
                {
                    Bitmap bm = fileToSave.Frames[i].GetBitmap();
                    Int32 stride;
                    frameDataRaw = ImageUtils.GetImageData(bm, out stride);
                    frameDataRaw = ImageUtils.CollapseStride(frameDataRaw, width, height, 8, ref stride);
                }
                else
                    frameDataRaw = firstFrameData;
                if (firstFrameData == null)
                    firstFrameData = frameDataRaw;
                framesDataUnc[i] = frameDataRaw;
            }
            // crop logic.
            Int32 xOffset = 0;
            Int32 yOffset = 0;
            if (crop)
            {
                Int32 minYTrimStart = Int32.MaxValue;
                Int32 maxYTrimEnd = 0;
                Int32 minXTrimStart = Int32.MaxValue;
                Int32 maxXTrimEnd = 0;
                // No need to process the final frame if it's a copy of the first.
                Int32 checkEnd = writeFrames;
                if (loop)
                    checkEnd--;
                for (Int32 i = 0; i < checkEnd; i++)
                {
                    Int32 yOffs = 0;
                    Int32 trHeight = height;
                    ImageUtils.OptimizeYHeight(framesDataUnc[i], width, ref trHeight, ref yOffs, true, 0, 0xFFFF, false);
                    minYTrimStart = Math.Min(minYTrimStart, yOffs);
                    maxYTrimEnd = Math.Max(maxYTrimEnd, yOffs + trHeight);

                    Int32 trWidth = width;
                    Int32 xOffs = 0;
                    ImageUtils.OptimizeXWidth(framesDataUnc[i], ref trWidth, height, ref xOffs, true, 0, 0xFFFF, false);
                    minXTrimStart = Math.Min(minXTrimStart, xOffs);
                    maxXTrimEnd = Math.Max(maxXTrimEnd, xOffs + trWidth);
                }
                if ((minYTrimStart != Int32.MaxValue && maxYTrimEnd != 0 && minXTrimStart != Int32.MaxValue && maxXTrimEnd != 0)
                    && (minXTrimStart != 0 || maxXTrimEnd != width || minYTrimStart != 0 || maxYTrimEnd != height))
                {                    
                    xOffset = minXTrimStart;
                    yOffset = minYTrimStart;
                    Int32 newWidth = maxXTrimEnd - xOffset;
                    Int32 newheight = maxYTrimEnd - yOffset;
                    for (Int32 i = 0; i < writeFrames; i++)
                        framesDataUnc[i] = ImageUtils.CopyFrom8bpp(framesDataUnc[i], width, height, width, new Rectangle(xOffset, yOffset, newWidth, newheight));
                    width = newWidth;
                    height = newheight;
                }
            }
            Byte[][] framesData = new Byte[writeFrames][];
            Int32 deltaBufferSize = 0;
            Byte[] previousFrame = new Byte[width*height];
            for (Int32 i = 0; i < writeFrames; i++)
            {
                Byte[] currentFrame = framesDataUnc[i];
                Byte[] frameData = WWCompression.GenerateXorDelta(currentFrame, previousFrame);
                deltaBufferSize = Math.Max(frameData.Length, deltaBufferSize);
                frameData = WWCompression.LcwCompress(frameData);
                framesData[i] = frameData;
                previousFrame = currentFrame;
            }
            // To ensure the file size is correct
            if (continues && framesData.Length > 0)
                framesData[0] = new Byte[0];
            // I dunno lol just following specs.
            deltaBufferSize = Math.Max(0, deltaBufferSize - 37);
            Int32 headerSize = 14;
            if (saveType == WsaVersion.Dune2)
                headerSize -= 4;
            Int32 indexSize = (readFrames + 2) * 4;
            Int32 paletteSize = asPaletted ? 0x300 : 0;
            Int32 dataSize = framesData.Sum(x => x.Length);
            Int32 fileSize = headerSize + indexSize + paletteSize + dataSize;
            Byte[] fileData = new Byte[fileSize];
            Int32 curOffs = headerSize + indexSize;
            Int32[] frameOffsets = new Int32[readFrames + 2];
            // Initial offset. Set to 0 if there is no first frame.
            frameOffsets[0] = continues ? 0 : curOffs;
            for (Int32 i = 0; i < writeFrames; i++)
            {
                curOffs += framesData[i].Length;
                frameOffsets[i + 1] = curOffs;
            }
            // Write header
            Int32 offset = 0;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)readFrames);
            offset += 2;
            if (saveType == WsaVersion.Cnc)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)xOffset);
                offset += 2;
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)yOffset);
                offset += 2;
            }
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)width);
            offset += 2;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)height);
            offset += 2;
            ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)deltaBufferSize);
            offset += 2;
            if (saveType != WsaVersion.Dune2v1)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt32)(asPaletted ? 1 : 0));
                offset += 2;
            }
            foreach (Int32 frOffs in frameOffsets)
            {
                ArrayUtils.WriteIntToByteArray(fileData, offset, 4, true, (UInt32)frOffs);
                offset += 4;
            }
            if (asPaletted)
            {
                ColorSixBit[] sixBitPal = ColorUtils.GetSixBitColorPalette(palette);
                Byte[] sixBitPalBytes = ColorUtils.GetSixBitPaletteData(sixBitPal);
                Array.Copy(sixBitPalBytes, 0, fileData, offset, Math.Min(0x300, sixBitPalBytes.Length));
                offset += 0x300;
            }
            foreach (Byte[] frame in framesData)
            {
                Array.Copy(frame, 0, fileData, offset, frame.Length);
                offset += frame.Length;
            }
            return fileData;
        }
    }

    public enum WsaVersion
    {
        Dune2v1 = 0,
        Dune2 = 1,
        Cnc = 2,
        //Poly=3,
    }
}