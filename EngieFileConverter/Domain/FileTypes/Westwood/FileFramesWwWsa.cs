using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
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
        public override String IdCode { get { return "WwWsa"; } }
        protected String[] formats = new String[] { "Dune II v1.00", "Dune II v1.07", "Command & Conquer", "Monopoly" };
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood WSA"; } }
        public override String[] FileExtensions { get { return new String[] { "wsa" }; } }
        public override String LongTypeName { get { return "Westwood Animation File"; } }
        public override Boolean NeedsPalette { get { return !this.m_HasPalette; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_HasPalette;
        protected WsaVersion m_Version = WsaVersion.Cnc;
        protected Boolean m_HasLoopFrame;
        protected Boolean m_DamagedLoopFrame;
        protected Boolean m_Continues;
        protected Boolean[] m_TransMask = new Boolean[] { true };

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        public override Boolean[] TransparencyMask { get { return this.m_TransMask; } }

        public override List<String> GetFilesToLoadMissingData(String originalPath)
        {
            if (!this.m_Continues)
                return null;
            Int32 maxWidth = this.Width;
            Int32 maxHeight = this.Height;
            List<String> cpsName = this.FindOtherExtFile(originalPath, new FileImgWwCps(), maxWidth, maxHeight);
            if (cpsName != null)
                return cpsName;
            List<String> pngName = this.FindOtherExtFile(originalPath, new FileImagePng(), maxWidth, maxHeight);
            if (pngName != null)
                return pngName;
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
            Int32 index = Array.FindIndex(frameNames, t => String.Equals(t, originalPath, StringComparison.InvariantCultureIgnoreCase));
            String lastName = null;
            for (Int32 i = index - 1; i >= 0; i--)
            {
                lastName = frameNames[i];
                Byte[] testBytes = File.ReadAllBytes(lastName);
                // Clean up used images after check. Probably not needed for WSA since the testContinue check makes it abort without actually storing any.
                using (FileFramesWwWsa testFrame = new FileFramesWwWsa())
                {
                    // Call with testContinue param to abort after confirming initial frame state.
                    try { testFrame.LoadFromFileData(testBytes, lastName, this.m_Version, null, 0,0, true); }
                    catch (HeaderParseException) { return null; }
                    maxWidth = Math.Max(maxWidth, testFrame.Width);
                    maxHeight = Math.Max(maxHeight, testFrame.Height);
                    if (!testFrame.m_Continues)
                    {
                        if (testFrame.Width < maxWidth || testFrame.Height < maxHeight)
                            return null;
                        chain.Add(lastName);
                        chain.Reverse();
                        return chain;
                    }
                    chain.Add(lastName);
                }
            }
            // If this code is reached, the chain finished on a WSA that also continued.
            if (lastName == null)
                return null;
            List<String> addedName = this.FindOtherExtFile(lastName, new FileImgWwCps(), maxWidth, maxHeight);
            if (addedName == null)
                addedName = this.FindOtherExtFile(lastName, new FileImagePng(), maxWidth, maxHeight);
            if (addedName == null)
                return null;
            chain.Add(addedName.First());
            chain.Reverse();
            return chain;
        }

        private List<String> FindOtherExtFile(String originalPath, SupportedFileType checkType, Int32 maxWidth, Int32 maxHeight)
        {
            // If a single png file of the same name is found it overrides normal chaining.
            String fileName = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + "." + checkType.FileExtensions.First());
            // Existence check + original case retrieve.
            String[] fileNames = Directory.GetFiles(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
            if (fileNames.Length <= 0)
                return null;
            fileName = fileNames[0];
            try
            {
                checkType.LoadFile(File.ReadAllBytes(fileName), fileName);
                if (checkType.Width >= maxWidth && checkType.Height >= maxHeight && !checkType.IsFramesContainer)
                    return new List<String>() {fileName};
            }
            catch (FileLoadException)
            {
                // ignore; continue with normal load
            }
            finally
            {
                // Remove loaded file.
                checkType.Dispose();
            }
            return null;
        }

        public override void ReloadFromMissingData(Byte[] fileData, String originalPath, List<String> loadChain)
        {
            Int32 lastFrameWidth;
            Int32 lastFrameHeight;
            String firstName = loadChain.FirstOrDefault();
            if (firstName == null)
                return;
            Byte[] lastFrameData = this.LoadFromOtherExtFile(loadChain, new FileImgWwCps(), out lastFrameWidth, out lastFrameHeight);
            if (lastFrameData == null)
                lastFrameData = this.LoadFromOtherExtFile(loadChain, new FileImage(), out lastFrameWidth, out lastFrameHeight);
            Int32 loadChainLength = loadChain.Count;
            for (Int32 i = 0; i < loadChainLength; ++i)
            {
                String chainFilePath = loadChain[i];
                Byte[] chainFileBytes = File.ReadAllBytes(chainFilePath);
                using (FileFramesWwWsa chainFile = new FileFramesWwWsa())
                {
                    try
                    {
                        chainFile.LoadFromFileData(chainFileBytes, chainFilePath, this.m_Version, lastFrameData, lastFrameWidth, lastFrameHeight, false);
                    }
                    catch
                    {
                        // Chain file was not a WSA.
                        return;
                    }
                    Int32 lastFrameIndex = chainFile.Frames.Length - 1;
                    if (lastFrameIndex < 0)
                        return;
                    Bitmap lastFrame = chainFile.Frames[lastFrameIndex].GetBitmap();
                    if (lastFrame == null)
                        return;
                    Int32 stride;
                    lastFrameWidth = lastFrame.Width;
                    lastFrameHeight = lastFrame.Height;
                    if (lastFrameWidth < this.Width || lastFrameHeight < this.Height)
                        return;
                    lastFrameData = ImageUtils.GetImageData(lastFrame, out stride, true);
                }
            }
            this.LoadFromFileData(fileData, originalPath, this.m_Version, lastFrameData, lastFrameWidth, lastFrameHeight, false);
            //String sizeInfo = (lastFrameWidth > this.Width || lastFrameHeight > this.Height) ? ("Original size: " + this.Width + "x" + this.Height) : String.Empty;
            this.ExtraInfo = (this.ExtraInfo + "\nData chained from " + Path.GetFileName(firstName)).TrimStart('\n');
        }

        private Byte[] LoadFromOtherExtFile(List<String> loadChain, SupportedFileType checkType, out Int32 lastFrameWidth, out Int32 lastFrameHeight)
        {
            lastFrameWidth = 0;
            lastFrameHeight = 0;
            //if (loadChain.Count != 1)
            //  return null;
            String firstName = loadChain.First();
            if (!firstName.EndsWith("." + checkType.FileExtensions.First(), StringComparison.InvariantCultureIgnoreCase))
                return null;
            if (!File.Exists(firstName))
                return null;
            // Get actual name
            try
            {
                // Extra frame is not used; always dispose image.
                checkType.LoadFile(File.ReadAllBytes(firstName), firstName);
                Bitmap bm = checkType.GetBitmap();
                if (bm.Width < this.Width || bm.Height < this.Height)
                    return null;
                lastFrameHeight = bm.Height;
                Byte[] lastFrameData = ImageUtils.GetImageData(bm, out lastFrameWidth, true);
                if (lastFrameData != null)
                    loadChain.RemoveAt(0);
                if (!checkType.NeedsPalette)
                    this.m_TransMask = checkType.TransparencyMask;
                return lastFrameData;
            }
            catch
            {
                return null;
            }
            finally
            {
                checkType.Dispose();
            }
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
            // Loop over all versions, most recent first.
            WsaVersion[] versions = Enum.GetValues(typeof(WsaVersion)).Cast<WsaVersion>().Reverse().ToArray();
            Int32 lenmax = versions.Length - 1;
            for (Int32 i = 0; i <= lenmax; ++i)
            {
                try
                {
                    this.LoadFromFileData(fileData, sourcePath, versions[i], null, 0, 0, false);
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

        protected void LoadFromFileData(Byte[] fileData, String sourcePath, WsaVersion loadVersion, Byte[] continueData, Int32 continueWidth, Int32 continueHeight, Boolean testContinue)
        {
            this.m_Continues = false;
            Int32 datalen = fileData.Length;
            if (datalen < 14)
                throw new FileTypeLoadException("File is too small to contain header.");
            UInt16 nrOfFrames = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            if (nrOfFrames == 0)
                throw new FileTypeLoadException("WSA cannot contain 0 frames.");
            UInt16 xPos;
            UInt16 yPos;
            UInt16 xorWidth;
            UInt16 xorHeight;
            Int32 headerSize;
            UInt32 deltaBufferSize;
            UInt16 flags = 0;
            // If the type is Dune 2, the "width" value actually contains the buffer size, so it's practically impossible this is below 320.
            switch (loadVersion)
            {
                case WsaVersion.Dune2:
                case WsaVersion.Dune2v1:
                    headerSize = 0x0A;
                    xPos = 0;
                    yPos = 0;
                    xorWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
                    xorHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
                    deltaBufferSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
                    // d2v1 has no flags, and can thus never contain a palette.
                    if (loadVersion == WsaVersion.Dune2v1)
                        headerSize -= 2;
                    else
                        flags = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 8);
                    break;
                case WsaVersion.Cnc:
                case WsaVersion.Poly:
                    xPos = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
                    yPos = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
                    xorWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
                    xorHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 8);
                    Int32 buffSize = 2;
                    if (loadVersion == WsaVersion.Poly)
                        buffSize += 2;
                    deltaBufferSize = (UInt32) ArrayUtils.ReadIntFromByteArray(fileData, 0x0A, buffSize, true);
                    flags = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0x0A + buffSize);
                    headerSize = 0x0C + buffSize;
                    break;
                default:
                    throw new FileTypeLoadException("Unknown WSA format!");
            }
            if (xorWidth == 0 || xorHeight == 0)
                throw new HeaderParseException("Invalid image dimensions!");
            Boolean cropped = continueData != null && (continueWidth > xorWidth || continueHeight > xorHeight);
            Rectangle finalPos = new Rectangle(xPos, yPos, xorWidth, xorHeight);
            this.m_Version = loadVersion;
            StringBuilder generalInfo = new StringBuilder("Version: ").Append(this.formats[(Int32)loadVersion]);
            if (loadVersion != WsaVersion.Dune2 && loadVersion != WsaVersion.Dune2v1)
            {
                generalInfo.Append("\nFrame dimensions: ").Append(xorWidth).Append("×").Append(xorHeight);
                generalInfo.Append("\nFrame position: [").Append(xPos).Append(", ").Append(yPos).Append("]");
            }
            String extraInfo = generalInfo.ToString();
            Int32 dataIndexOffset = headerSize;
            Int32 paletteOffset = dataIndexOffset + (nrOfFrames + 2) * 4;
            this.m_HasPalette = (flags & 1) != 0;
            Boolean forceSixBitPal = loadVersion == WsaVersion.Poly && (flags & 2) != 0;
            UInt32[] frameOffsets = new UInt32[nrOfFrames + 2];
            for (Int32 i = 0; i < nrOfFrames + 2; ++i)
            {
                if (fileData.Length <= dataIndexOffset + 4)
                    throw new HeaderParseException("Data too short to contain frames info!");
                UInt32 curOffs = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, dataIndexOffset);
                frameOffsets[i] = curOffs;
                if (this.m_HasPalette)
                    curOffs +=300;
                if (curOffs > fileData.Length)
                    throw new HeaderParseException("Data too short to contain frames info!");
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
                    throw new HeaderParseException("File is not long enough for color palette!");
                Byte[] pal = new Byte[0x300];
                Array.Copy(fileData, paletteOffset, pal, 0, 0x300);
                try
                {
                    if (loadVersion != WsaVersion.Poly || forceSixBitPal)
                        this.m_Palette = ColorUtils.ReadSixBitPalette(fileData, paletteOffset);
                    else
                        this.m_Palette = ColorUtils.ReadEightBitPalette(fileData, paletteOffset, 0x100);
                    PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, this.TransparencyMask);
                }
                catch (ArgumentException ex)
                {
                    throw new HeaderParseException("Error loading color palette: " + GeneralUtils.RecoverArgExceptionMessage(ex, false), ex);
                }
            }
            if (this.m_Palette == null)
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            this.m_Width = Math.Max(continueWidth, xorWidth + xPos);
            this.m_Height = Math.Max(continueHeight, xorHeight + yPos);
            this.m_FramesList = new SupportedFileType[nrOfFrames + 1];
            Byte[] frameData = new Byte[xorWidth * xorHeight];
            Byte[] finalFrameData = null;
            Byte[] frame0Data = new Byte[xorWidth * xorHeight];
            deltaBufferSize += 37;
            Int32 xorWorstCase = Math.Max((Int32)deltaBufferSize, Math.Max(10000, xorWidth * xorHeight * 2));
            Byte[] xorData = new Byte[xorWorstCase];
            for (Int32 i = 0; i < nrOfFrames + 1; ++i)
            {
                String specificInfo = String.Empty;
                UInt32 frameOffset = frameOffsets[i];
                UInt32 frameOffsetReal = frameOffset;
                UInt32 frameEndOffset = frameOffsets[i + 1];
                if (this.m_HasPalette)
                {
                    frameOffsetReal += 0x300;
                    frameEndOffset += 0x300;
                }
                if (i == 0 && frameOffset == 0)
                {
                    this.m_Continues = true;
                    if (continueData != null)
                    {
                        if (cropped)
                            frameData = ImageUtils.CopyFrom8bpp(continueData, continueWidth, continueHeight, continueWidth, finalPos);
                        else
                            Array.Copy(continueData, frameData, frameData.Length);
                        specificInfo = "\nLoaded from previous file";
                    }
                    else
                    {
                        Array.Clear(frameData, 0, frameData.Length);
                        specificInfo = "\nContinues from a previous file";
                        extraInfo += specificInfo;
                    }
                }
                if (testContinue)
                    return;
                if (frameOffsetReal == fileData.Length)
                    break;
                if (frameOffset != 0)
                {
                    Int32 refOff = (Int32)frameOffsetReal;
                    Int32 uncLen;
                    Boolean bufferOverrun;
                    try
                    {
                        uncLen = WWCompression.LcwDecompress(fileData, ref refOff, xorData, (Int32)frameEndOffset);
                        //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(sourcePath), String.Format("input-xor-{0:000}.dat", i)), xorData);
                        bufferOverrun = uncLen > deltaBufferSize;
                    }
                    catch (Exception ex)
                    {
                        throw new HeaderParseException("LCW Decompression failed: " + ex.Message, ex);
                    }
                    specificInfo += "\nData: " + (refOff - frameOffsetReal) + (" bytes @ 0x") + frameOffsetReal.ToString("X");
                    if (bufferOverrun)
                        specificInfo += "\nFrame has buffer overrun";
                    try
                    {
                        refOff = 0;
                        WWCompression.ApplyXorDelta(frameData, xorData, ref refOff, uncLen);
                    }
                    catch (Exception ex)
                    {
                        throw new HeaderParseException("XOR Delta merge failed: " + ex.Message, ex);
                    }
                }
                if (i == 0)
                    Array.Copy(frameData, frame0Data, frameData.Length);
                if (this.m_Continues && cropped)
                {
                    if (finalFrameData == null)
                        finalFrameData = continueData;
                    ImageUtils.PasteOn8bpp(finalFrameData, continueWidth, continueHeight, continueWidth, frameData, xorWidth, xorHeight, xorWidth, finalPos, null, true);
                }
                else if (xPos > 0 || yPos > 0)
                {
                    finalFrameData = frameData;
                    //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(sourcePath), String.Format("input-crop-{0:000}.dat", i)), frameData);
                    finalFrameData = ImageUtils.ChangeStride(finalFrameData, xorWidth, xorHeight, this.m_Width, true, 0);
                    finalFrameData = ImageUtils.ChangeHeight(finalFrameData, this.m_Width, xorHeight, this.m_Height, true, 0);
                }
                else
                    finalFrameData = frameData;
                Bitmap curFrImg = ImageUtils.BuildImage(finalFrameData, this.m_Width, this.m_Height, this.m_Width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetFileClass(this.FrameInputFileClass);
                frame.SetNeedsPalette(this.NeedsPalette);
                frame.SetExtraInfo(specificInfo.TrimStart('\n'));
                this.m_FramesList[i] = frame;
            }
            this.m_DamagedLoopFrame = false;
            if (this.m_HasLoopFrame)
            {
                this.m_DamagedLoopFrame = !frameData.SequenceEqual(frame0Data);
                extraInfo += "\nHas loop frame";
                if (this.m_DamagedLoopFrame)
                    extraInfo += " (but doesn't match)";
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
            this.m_Version = loadVersion;
            this.ExtraInfo = extraInfo;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            // Preliminary checks
            Int32 width;
            Int32 height;
            Color[] palette;
            PerformPreliminaryChecks(fileToSave, out width, out height, out palette);
            Boolean hasColors = !fileToSave.NeedsPalette;
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
            Option[] opts = new Option[ignoreLast ? 7 : 6];
            opts[0] = new Option("TYPE", OptionInputType.ChoicesList, "Type:", String.Join(",", this.formats), ((Int32)type).ToString());
            opts[1] = new Option("PAL", OptionInputType.Boolean, "Include palette", null, hasColors ? "1" : "0", new EnableFilter("TYPE", false, "0"));
            opts[2] = new Option("PAL6", OptionInputType.Boolean, "Force 6-bit palette", null, "0", true, new EnableFilter("TYPE", true, "3"), new EnableFilter("PAL", true, "1"));
            opts[3] = new Option("LOOP", OptionInputType.Boolean, "Loop", null, loop ? "1" : "0");
            opts[4] = new Option("CONT", OptionInputType.Boolean, "Don't save initial frame", null, continues ? "1" : "0");
            opts[5] = new Option("CROP", OptionInputType.Boolean, "Crop to X and Y offsets", null, trim ? "1" : "0", new EnableFilter("TYPE", false, "0" ,"1"));
            if (ignoreLast)
                opts[6] = new Option("CUT", OptionInputType.Boolean, "Ignore broken input loop frame", null, "1");
            return opts;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // Preliminary checks
            Int32 width;
            Int32 height;
            Color[] palette;
            SupportedFileType[] frames = PerformPreliminaryChecks(fileToSave, out width, out height, out palette);
            Int32 nrOfFrames = frames.Length;

            // Save options
            Int32 type;
            WsaVersion saveType;
            if (!Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "TYPE"), out type) || !Enum.IsDefined(typeof (WsaVersion), type))
                saveType = WsaVersion.Cnc;
            else
                saveType = (WsaVersion)type;
            Boolean asPaletted = saveType != WsaVersion.Dune2v1 && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PAL"));
            Boolean sixBitPalOpt = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PAL6"));
            Boolean saveSixBitPal = saveType != WsaVersion.Poly || sixBitPalOpt;

            Boolean loop = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "LOOP"));
            Boolean crop = saveType != WsaVersion.Dune2v1 && saveType != WsaVersion.Dune2 && GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CROP"));
            Boolean cut = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CUT"));
            Boolean continues = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CONT"));

            // Fetch and compress data
            Int32 readFrames = nrOfFrames;
            Int32 writeFrames = nrOfFrames;
            if (cut)
            {
                readFrames--;
                writeFrames--;
            }
            // Technically can't happen... I think.
            if (readFrames == 0)
                throw new ArgumentException("No frames in source data!","fileToSave");
            if (loop)
                writeFrames++;
            Byte[][] framesDataUnc = new Byte[writeFrames][];
            Byte[] firstFrameData = continues ? new Byte[width * height] : null;
            for (Int32 i = 0; i < writeFrames; ++i)
            {
                Byte[] frameDataRaw;
                if (i < readFrames)
                {
                    Bitmap bm = frames[i].GetBitmap();
                    Int32 stride;
                    frameDataRaw = ImageUtils.GetImageData(bm, out stride, true);
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
                for (Int32 i = 0; i < checkEnd; ++i)
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
                    for (Int32 i = 0; i < writeFrames; ++i)
                    {
                        framesDataUnc[i] = ImageUtils.CopyFrom8bpp(framesDataUnc[i], width, height, width, new Rectangle(xOffset, yOffset, newWidth, newheight));
                        //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(fileToSave.LoadedFile), String.Format("output-crop-{0:000}.dat", i)), framesDataUnc[i]);
                    }
                    width = newWidth;
                    height = newheight;
                }
            }
            Byte[][] framesData = new Byte[writeFrames][];
            Int32 deltaBufferSize = 0;
            Byte[] previousFrame = new Byte[width*height];
            for (Int32 i = 0; i < writeFrames; ++i)
            {
                Byte[] currentFrame = framesDataUnc[i];
                Byte[] frameData = WWCompression.GenerateXorDelta(currentFrame, previousFrame);
                //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(fileToSave.LoadedFile), String.Format("output-xor-{0:000}.dat", i)), frameData);
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
            else if (saveType == WsaVersion.Poly)
                headerSize += 2;

            Int32 indexSize = (readFrames + 2) * 4;
            Int32 paletteSize = asPaletted ? 0x300 : 0;
            Int32 dataSize = framesData.Sum(x => x.Length);
            Int32 fileSize = headerSize + indexSize + paletteSize + dataSize;
            Byte[] fileData = new Byte[fileSize];
            Int32 curOffs = headerSize + indexSize;
            Int32 nrOfOffsets = readFrames + 2;
            Int32[] frameOffsets = new Int32[nrOfOffsets];
            // Initial offset. Set to 0 if there is no first frame.
            frameOffsets[0] = continues ? 0 : curOffs;
            for (Int32 i = 0; i < writeFrames; ++i)
            {
                curOffs += framesData[i].Length;
                frameOffsets[i + 1] = curOffs;
            }
            // Write header
            Int32 offset = 0;
            ArrayUtils.WriteUInt16ToByteArrayLe(fileData, offset, (UInt16)readFrames);
            offset += 2;
            if (saveType != WsaVersion.Dune2v1 && saveType != WsaVersion.Dune2)
            {
                ArrayUtils.WriteUInt16ToByteArrayLe(fileData, offset, (UInt16)xOffset);
                offset += 2;
                ArrayUtils.WriteUInt16ToByteArrayLe(fileData, offset, (UInt16)yOffset);
                offset += 2;
            }
            ArrayUtils.WriteUInt16ToByteArrayLe(fileData, offset, (UInt16)width);
            offset += 2;
            ArrayUtils.WriteUInt16ToByteArrayLe(fileData, offset, (UInt16)height);
            offset += 2;
            ArrayUtils.WriteIntToByteArray(fileData, offset, saveType == WsaVersion.Poly ? 4 : 2, true, (UInt32)deltaBufferSize);
            offset += saveType == WsaVersion.Poly ? 4 : 2;
            if (saveType != WsaVersion.Dune2v1)
            {
                UInt16 flags = 0;
                // Enable extra flag
                if (asPaletted)
                {
                    flags |= 1;
                    if (saveType == WsaVersion.Poly && sixBitPalOpt)
                        flags |= 2;
                }
                ArrayUtils.WriteUInt16ToByteArrayLe(fileData, offset, flags);
                offset += 2;
            }
            for (Int32 i = 0; i < nrOfOffsets; ++i)
            {
                ArrayUtils.WriteInt32ToByteArrayLe(fileData, offset, frameOffsets[i]);
                offset += 4;
            }
            if (asPaletted)
            {
                Byte[] palBytes;
                if (saveSixBitPal)
                    palBytes = ColorUtils.GetSixBitPaletteData(palette);
                else
                    palBytes = ColorUtils.GetEightBitPaletteData(palette, false);
                Array.Copy(palBytes, 0, fileData, offset, Math.Min(0x300, palBytes.Length));
                offset += 0x300;
            }
            for (Int32 i = 0; i < writeFrames; ++i)
            {
                Byte[] frame = framesData[i];
                Int32 frameLen = frame.Length;
                Array.Copy(frame, 0, fileData, offset, frameLen);
                offset += frameLen;
            }
            return fileData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out Int32 width, out Int32 height, out Color[] palette)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames;
            if (frames == null || (nrOfFrames = frames.Length) == 0)
                throw new ArgumentException(ERR_NEEDS_FRAMES, "fileToSave");
            width = -1;
            height = -1;
            palette = CheckInputForColors(fileToSave, 8, true);
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_EMPTY_FRAMES, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
                if (width == -1 && height == -1)
                {
                    width = frame.Width;
                    height = frame.Height;
                }
                else if (width != frame.Width || height != frame.Height)
                    throw new ArgumentException(ERR_FRAMES_DIFF, "fileToSave");
                if (palette == null || palette.Length == 0)
                    palette = frame.GetColors();
            }
            return frames;
        }

    }

    public enum WsaVersion
    {
        Dune2v1 = 0,
        Dune2 = 1,
        Cnc = 2,
        Poly = 3,
    }
}