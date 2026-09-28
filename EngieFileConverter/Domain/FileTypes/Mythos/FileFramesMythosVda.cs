using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.FileData.Mythos;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesMythosVda : FileFramesMythosVgs
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override string IdCode { get { return "MythVda"; } }
        public override string ShortTypeName { get { return "Mythos Visage Animation"; } }
        public override string LongTypeName { get { return "Mythos Visage Animation file"; } }
        public override string[] FileExtensions { get { return new string[] { "vda", "vdx" }; } }
        public override bool[] TransparencyMask { get { return (!this._isFramed || (this._noFirstFrame && !this._isChained)) ? base.TransparencyMask : new bool[0]; } }

        private const ushort FrameEnd = 0xFFFF;
        private const ushort AnimEnd = 0xFFFE;

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        /// <summary>Indicates that the first drawn chunk does not match the criteria for a full-screen frame.</summary>
        private bool _noFirstFrame;
        /// <summary>Indicates that the first frame was loaded from a previous file.</summary>
        private bool _isChained;
        /// <summary>Indicates that a frames definition file is found, and the frames are constructed.</summary>
        private bool _isFramed;

        public override List<string> GetFilesToLoadMissingData(string originalPath)
        {
            // No missing data.
            if (!this._noFirstFrame)
                return null;

            // Wrong file. Switch to the VDA one.
            if (originalPath.EndsWith(".VDX", StringComparison.InvariantCultureIgnoreCase))
            {
                originalPath = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + ".VDA");
                if (!File.Exists(originalPath))
                    return null;
            }
            // If a single png file of the same name is found it overrides normal chaining.
            string pngName = this.TestForPngStartFrame(originalPath);
            if (pngName != null)
                return new List<string>() { pngName };
            string baseName;
            // Call file range detection algorithm already in place on FileFrames class.
            string[] frameNames = FileFrames.GetFrameFilesRange(originalPath, out baseName);
            if (frameNames == null)
                return null;
            originalPath = Path.GetFullPath(originalPath);
            // The function from FileFrames returns the whole range, which might be too much. Find the actual file we started from.
            int index = Array.FindIndex(frameNames, t => String.Equals(t, originalPath, StringComparison.InvariantCultureIgnoreCase));
            // Check previous files until finding one with an initial frame.
            List<string> chain = new List<string>();
            for (int i = index - 1; i >= 0; i--)
            {
                string curName = frameNames[i];
                byte[] testBytesVda = File.ReadAllBytes(curName);
                string vdxPath = Path.Combine(Path.GetDirectoryName(curName), Path.GetFileNameWithoutExtension(curName) + ".VDX");
                byte[] testBytesVdx = File.ReadAllBytes(vdxPath);
                // Test for obvious indications that the file is a valid VDX
                if (!this.CheckForVdx(testBytesVdx))
                    return null;
                // Can't get last frame if there is no VDX file. Abort immediately.
                if (!File.Exists(vdxPath))
                    return null;
                // Clean up used images after check.
                using (FileFramesMythosVda testFile = new FileFramesMythosVda())
                {
                    // Check if first frame in VDX is frame 0. If not, all frames will need to be loaded. This is normally 0 though.
                    bool startsWithFrameZero = (ArrayUtils.ReadUInt16FromByteArrayLe(testBytesVdx, 0) & 0x7FFF) == 0;
                    List<Point> framesXY;
                    try
                    {
                        // If VDX starts with frame zero, load with the "forFrameTest" option so it aborts after reading that first frame.
                        testFile.LoadFromFileData(testBytesVda, curName, false, false, true, out framesXY, startsWithFrameZero);
                    }
                    catch (FileLoadException)
                    {
                        // can't load one of the chained files as VDA file. Abort.
                        return null;
                    }
                    // VDA files always have a palette.
                    if (testFile.m_LoadedPalette == null)
                        return null;
                    int badPalMatches = 0;
                    Color[] testPal = testFile.GetColors();
                    for (int p = 0; p < 256; ++p)
                        if (testPal[p] != this.m_Palette[p])
                            badPalMatches++;
                    // Check if palette matches. Some small changes will be ignored since they happen in the Serrated Scalpel files.
                    if (badPalMatches > 8)
                        return null;
                    SupportedFileType firstFrame = testFile.Frames.FirstOrDefault();
                    // No frames; could be a palette-only VGS file.
                    if (firstFrame == null)
                        return null;
                    // Check if the frame is complete, which would mean the end point of the chaining was reached.
                    if (firstFrame.Width == 320 && firstFrame.Height == 200 && framesXY[0].X == 0 && framesXY[0].Y == 0)
                    {
                        // Frame is OK. Check amount of chunks in the first frame defined in the VDX file, to see if it may be multi-chunk after all.
                        bool noFirstFrame;
                        // Call using the testFirstFrame option to abort after performing the "noFirstFrame" check.
                        // Technically this check is incomplete; if the first referenced frame is not frame #0 it fails.
                        // But the first referenced frame should always be frame 0... even my VDX optimisation only changes the VDA coordinates, not order.
                        try
                        {
                            this.BuildAnimationFromChunks(originalPath, testBytesVdx, testFile.m_FramesList, framesXY, null, true, out noFirstFrame);
                        }
                        catch (FileLoadException)
                        {
                            return null;
                        }
                        if (!noFirstFrame)
                        {
                            // Confirmed as first frame.
                            chain.Add(curName);
                            chain.Reverse();
                            return chain;
                        }
                    }
                    // End point not reached; current file also needs a first frame. Store current file and continue chaining back.
                    chain.Add(curName);

                    // Test for png. png is also end point.
                    string pngChained = this.TestForPngStartFrame(curName);
                    if (pngChained != null)
                    {
                        chain.Add(pngChained);
                        chain.Reverse();
                        return chain;
                    }
                }
            }
            return null;
        }

        private string TestForPngStartFrame(string originalPath)
        {
            string pngName = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + ".PNG");
            if (File.Exists(pngName))
            {
                try
                {
                    using (FileImagePng pngFile = new FileImagePng())
                    {
                        pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                        Bitmap image = pngFile.GetBitmap();
                        if (image.Width == 320 && image.Height == 200 && image.PixelFormat == PixelFormat.Format8bppIndexed)
                            return pngName;
                    }
                }
                catch (FileLoadException)
                {
                    // ignore; continue with normal load
                }
            }
            return null;
        }

        public override void ReloadFromMissingData(byte[] fileData, string originalPath, List<string> loadChain)
        {
            byte[] lastFrameData = null;
            string lastFrameInfo = String.Empty;
            string firstName = loadChain.First();
            int lastIndex = loadChain.Count - 1;
            bool fromPng = false;
            for (int i = 0; i <= lastIndex; ++i)
            {
                string chainFilePath = loadChain[i];
                try
                {
                    if (i == 0 && chainFilePath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        lastFrameData = this.GetFrameDataFromPng(firstName, ref lastFrameInfo);
                        if (lastFrameData != null)
                        {
                            fromPng = lastIndex == 0;
                            continue;
                        }
                    }
                    byte[] chainFileBytes = File.ReadAllBytes(chainFilePath);
                    using (FileFramesMythosVda chainFile = new FileFramesMythosVda())
                    {
                        chainFile.LoadFile(chainFileBytes, chainFilePath, lastFrameData);
                        int lastFrIndex = chainFile.Frames.Length - 1;
                        if (lastFrIndex < 0)
                            return;
                        SupportedFileType lastFrame = chainFile.m_FramesList[lastFrIndex];
                        lastFrameData = this.Get320x200FrameData(lastFrame);
                        // Maybe use exception? Should never happen though.
                        if (lastFrameData == null)
                            return;
                        lastFrameInfo = lastFrame.ExtraInfo;
                    }
                }
                catch { return; } // can't load as VDA file. Abort.
            }
            this.LoadFile(fileData, originalPath, lastFrameData);
            if (lastFrameData != null)
            {
                this.ExtraInfo += "\nData chained from " + Path.GetFileName(firstName);

                if (String.IsNullOrEmpty(lastFrameInfo))
                    lastFrameInfo = String.Empty;
                else
                    lastFrameInfo += "\n";
                FileImageFrame first = this.m_FramesList[0] as FileImageFrame;
                if (first != null)
                    first.SetExtraInfo((lastFrameInfo + (fromPng ? "PNG loaded as base frame" : "Loaded from previous file")).TrimStart('\n'));
            }
        }

        private byte[] GetFrameDataFromPng(string pngName, ref string lastFrameInfo)
        {
            byte[] lastFrameData = null;
            if (File.Exists(pngName))
            {
                try
                {
                    using (FileImageFrame pngFile = new FileImageFrame())
                    {
                        // Uses specific PNG loading from its superclass, since
                        // FileImageFrame inherits from png and still contains its mime type.
                        pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                        pngFile.LoadFileFrame(null, new FileImagePng().LongTypeName, pngFile.GetBitmap(), pngName, -1);
                        lastFrameData = this.Get320x200FrameData(pngFile);

                        if (lastFrameData != null)
                            lastFrameInfo = pngFile.ExtraInfo;
                    }
                }
                catch { /* can't load as png file. Abort.*/ }
            }
            return lastFrameData;
        }

        protected byte[] Get320x200FrameData(SupportedFileType loadedFrame)
        {
            if (loadedFrame == null)
                return null;
            Bitmap lastFrameImage = loadedFrame.GetBitmap();
            if (lastFrameImage == null || lastFrameImage.Width != 320 || lastFrameImage.Height != 200 || lastFrameImage.PixelFormat != PixelFormat.Format8bppIndexed)
                return null;
            int stride;
            // stride collapse is probably not needed... 320 is divisible by 4.
            return ImageUtils.GetImageData(lastFrameImage, true);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFile(fileData, filename, null);
        }

        public void LoadFile(byte[] fileData, string filename, byte[] initialFrameData)
        {
            byte[] vdaBytes;
            byte[] vdxBytes;
            string vdaName;
            string vdxName;
            this.GetLoadFileInfo(fileData, filename, out vdaBytes, out vdxBytes, out vdaName, out vdxName);
            if (vdaBytes == null)
                throw new FileTypeLoadException("Cannot load a VDA video from a VDX alone.");

            if (vdaName != null)
            {
                this.SetFileNames(vdaName.ToUpper());
                if (vdxBytes != null && vdxName != null)
                    this.LoadedFileName += "/" + Path.GetExtension(vdxName).TrimStart('.').ToUpper();
            }
            List<Point> framesXY;
            this._isFramed = vdxBytes != null;
            this._isChained = initialFrameData != null;
            this.LoadFromFileData(vdaBytes, vdaName, false, false, true, out framesXY, false);
            this.m_Palette = PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, null);
            int chunks = this.m_FramesList.Count;
            if (this._isFramed)
            {
                bool noFirstFrame;
                List<SupportedFileType> framesList = this.BuildAnimationFromChunks(vdaName, vdxBytes, this.m_FramesList, framesXY, initialFrameData, false, out noFirstFrame);
                this._noFirstFrame = noFirstFrame;
                // Apply transparency mask.
                // Give parent ref so the SetColors mechanism thinks this is an update coming from the parent and will not loop over the parent's frames.
                this.SetColors(this.m_Palette, this);
                this.m_BackupPalette = null;
                this.m_FramesList = framesList;
            }
            if (!this._isFramed)
                this.ExtraInfo = "VDX file missing; showing raw chunks\n" + this.ExtraInfo;
            this.ExtraInfo += "\nChunks: " + chunks;
        }

        private void GetLoadFileInfo(byte[] fileData, string filename, out byte[] vdaBytes, out byte[] vdxBytes, out string vdaName, out string vdxName)
        {
            vdxBytes = null;
            vdaName = null;
            vdxName = null;
            if (filename != null)
            {
                bool isVda = filename.EndsWith(".VDA", StringComparison.InvariantCultureIgnoreCase);
                bool isVdx = filename.EndsWith(".VDx", StringComparison.InvariantCultureIgnoreCase);
                string vdaNm = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".VDA");
                string vdxNm = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".VDx");
                if (isVda)
                {
                    vdaName = filename;
                    vdaBytes = fileData;
                    vdxName = vdxNm;
                    if (File.Exists(vdxName))
                        vdxBytes = File.ReadAllBytes(vdxName);
                }
                else if (isVdx)
                {
                    vdaName = vdaNm;
                    vdaBytes = File.Exists(vdaName) ? File.ReadAllBytes(vdaNm) : null;
                    vdxName = filename;
                    vdxBytes = fileData;
                }
                else
                {
                    bool hasVda = File.Exists(vdaNm);
                    bool hasVdx = File.Exists(vdxNm);
                    if (hasVda && hasVdx)
                    {
                        bool dataIsVdx = this.CheckForVdx(fileData);
                        vdaName = dataIsVdx ? vdaNm : filename;
                        vdxName = dataIsVdx ? filename : vdxNm;
                        vdaBytes = dataIsVdx ? File.ReadAllBytes(vdaNm) : fileData;
                        vdxBytes = dataIsVdx ? fileData : File.ReadAllBytes(vdxNm);
                    }
                    else if (hasVda && this.CheckForVdx(fileData))
                    {
                        vdaName = vdaNm;
                        vdaBytes = File.ReadAllBytes(vdaNm);
                        vdxBytes = fileData;
                        vdxName = filename;
                    }
                    else
                    {
                        vdaName = filename;
                        vdaBytes = fileData;
                        vdxBytes = hasVdx ? File.ReadAllBytes(vdxNm) : null;
                    }
                }
            }
            else
            {
                if (this.CheckForVdx(fileData))
                    throw new FileTypeLoadException("Can't load a video from .VDX file without filename.");
                vdaBytes = fileData;
            }
        }

        /// <summary>
        /// USes the vdx data to builds the animation from the vdachunks.
        /// </summary>
        /// <param name="sourcePath">Source path to save into the produced frame objects.</param>
        /// <param name="framesInfo">Frames info bytes from the vdx file.</param>
        /// <param name="allChunks">List of chunks to use to produce the frames.</param>
        /// <param name="framesXY">The X and Y coordinates of the chunks in <see cref="allChunks"/>.</param>
        /// <param name="initialFrameData">Frame data for a missing first frame, loaded from a previous file.</param>
        /// <param name="noFirstFrame">Returns whether a missing first frame was detected.</param>
        /// <param name="testFirstFrame">Only test whether a missing first frame was detected, and immediately return the result.</param>
        /// <returns>The constructed frames, or null in <see cref="testFirstFrame"/> mode.</returns>
        private List<SupportedFileType> BuildAnimationFromChunks(string sourcePath, byte[] framesInfo, List<SupportedFileType> allChunks, List<Point> framesXY, byte[] initialFrameData, bool testFirstFrame, out bool noFirstFrame)
        {
            noFirstFrame = initialFrameData != null;
            List<SupportedFileType> framesList = new List<SupportedFileType>();
            int offset = 0;
            int imageWidth = 320;
            int imageHeight = 200;
            int imageStride = 320;
            int arraySize = imageWidth * imageHeight;
            if (initialFrameData != null && initialFrameData.Length != arraySize)
                throw new FileTypeLoadException("Bad start frame data length.");
            byte[] imageData = initialFrameData == null ? null : ArrayUtils.CloneArray(initialFrameData);

            bool[] pasteTransMask = base.TransparencyMask;
            bool[] imageTransMask = pasteTransMask;
            if (initialFrameData != null)
            {
                // starting frame
                Bitmap curImage = ImageUtils.BuildImage(imageData, imageWidth, imageHeight, imageStride, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, curImage, sourcePath, framesList.Count);
                frame.SetNeedsPalette(this.m_LoadedPalette == null);
                // Give parent ref so the SetColors mechanism thinks this is an update coming from the parent and will not loop over the parent's frames.
                frame.SetColors(this.m_Palette, this);
                frame.SetFileClass(this.FrameInputFileClass);
                frame.SetExtraInfo(CHUNKS + 1);
                framesList.Add(frame);
            }
            int chunks = 0;
            while (offset + 2 <= framesInfo.Length)
            {
                ushort curVal = ArrayUtils.ReadUInt16FromByteArrayLe(framesInfo, offset);
                if (curVal == AnimEnd)
                    break;
                if (curVal == FrameEnd)
                {
                    // No chunks at all specified for the very first frame. Could happen in a continued animation starting with a pause I guess?
                    if (imageData == null)
                    {
                        noFirstFrame = true;
                        if (testFirstFrame)
                            return null;
                        imageData = new byte[arraySize];
                        for (int i = 0; i < arraySize; ++i)
                            imageData[i] = TransparentIndex;
                    }
                    if (testFirstFrame)
                        return null;
                    if (framesList.Count == 0)
                    {
                        if (!noFirstFrame || initialFrameData != null)
                            imageTransMask = null;
                        PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, imageTransMask);
                    }
                    Bitmap curImage = ImageUtils.BuildImage(imageData, imageWidth, imageHeight, imageStride, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                    // TEST
                    //imageData = null;
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(this, this, curImage, sourcePath, framesList.Count);
                    frame.SetNeedsPalette(this.m_LoadedPalette == null);
                    // Give parent ref so the SetColors mechanism thinks this is an update coming from the parent and will not loop over the parent's frames.
                    frame.SetColors(this.m_Palette, this);
                    frame.SetFileClass(this.FrameInputFileClass);
                    frame.SetExtraInfo(CHUNKS + chunks);
                    framesList.Add(frame);
                    chunks = 0;
                    offset += 2;
                }
                else
                {
                    // Since the First frame has no transparent holes, it can't be chunked, so the first frame should always be one chunk.
                    // More than 0 chunks here means this section was looped once already, so the first frame contains multiple chunks.
                    if (testFirstFrame && chunks > 0)
                    {
                        noFirstFrame = true;
                        return null;
                    }
                    int frameNumber = curVal & 0x7FFF;
                    if (allChunks.Count <= frameNumber)
                        throw new FileLoadException("Video frames file references more frames than available in graphics file.");
                    int xOffset;
                    int yOffset;
                    if ((curVal & 0x8000) != 0)
                    {
                        if (offset + 6 >= framesInfo.Length)
                            throw new FileLoadException("Illegal data order in video frames file.");
                        xOffset = ArrayUtils.ReadUInt16FromByteArrayLe(framesInfo, offset + 2);
                        yOffset = ArrayUtils.ReadUInt16FromByteArrayLe(framesInfo, offset + 4);
                        offset += 4;
                    }
                    else
                    {
                        if (framesXY.Count < frameNumber)
                            throw new FileLoadException("Video frames file references more frames than available in the graphics file.");
                        xOffset = framesXY[frameNumber].X;
                        yOffset = framesXY[frameNumber].Y;
                    }
                    Bitmap currentImage = allChunks[frameNumber].GetBitmap();
                    int stride;
                    int width = currentImage.Width;
                    int height = currentImage.Height;
                    byte[] currentFrameData = ImageUtils.GetImageData(currentImage, out stride, true);
                    if (imageData == null)
                    {
                        if (xOffset == 0 && yOffset == 0 && width == 320 && height == 200)
                        {
                            imageData = currentFrameData;
                            // To skip paint operation.
                            currentFrameData = null;
                        }
                        else
                        {
                            noFirstFrame = true;
                            if (testFirstFrame)
                                return null;
                            imageData = Enumerable.Repeat(TransparentIndex, arraySize).ToArray();
                        }
                    }
                    if (!noFirstFrame && chunks > 1 && framesList.Count == 0)
                    {
                        noFirstFrame = true;
                        if (testFirstFrame)
                            return null;
                    }
                    // first frame built from multiple chunks means no base image.
                    if (currentFrameData != null)
                    {
                        if (imageWidth < xOffset + width || imageHeight < yOffset + height)
                            throw new FileLoadException("Illegal data in video frames file: paint coordinates out of bounds.");
                        ImageUtils.PasteOn8bpp(imageData, imageWidth, imageHeight, imageStride, currentFrameData, width, height, stride, new Rectangle(xOffset, yOffset, width, height), pasteTransMask, true);
                    }
                    chunks++;
                    offset += 2;
                }
            }
            return framesList;
        }

        /// <summary>
        /// Checks if the given bytes contain valid VDX data. The actual check is to see if the length
        /// is divisible by 2, and the data ends on the "frame end" and "animation end" markers.
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns></returns>
        protected bool CheckForVdx(byte[] fileData)
        {
            if (fileData.Length < 4 || fileData.Length % 2 != 0)
                return false;
            // Last two blocks should be FFFF and FFFE.
            ushort lastFrameEnd = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, fileData.Length - 4);
            ushort animationEnd = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, fileData.Length - 2);
            if (lastFrameEnd == FrameEnd && animationEnd == AnimEnd)
                return true;
            return false;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            Color[] palette;
            this.PerformPreliminaryChecks(fileToSave, out palette);
            int compression = 0;
            bool noFirstFrame = false;
            FileFramesMythosVgs fileVgs = fileToSave as FileFramesMythosVgs;
            if (fileVgs != null)
                compression = fileVgs.CompressionType;
            if (compression < 0 || compression > this.compressionTypes.Length)
                compression = 0;
            FileFramesMythosVda fileVda = fileToSave as FileFramesMythosVda;
            if (fileVda != null)
            {
                if (fileVda._noFirstFrame && !fileVda._isChained)
                    throw new FileTypeSaveException("A " + this.LongTypeName + " without initial frame cannot be re-saved correctly. Reload it with the missing start added (either as vda or as png) before saving it.");
                noFirstFrame = fileVda._noFirstFrame;
            }
            return new Option[]
            {
                new Option("OPT", OptionInputType.ChoicesList, "Optimisation:", "Save simple cropped diff frames,Optimise to chunks", "1"),
                new Option("CH8", OptionInputType.Boolean, "Chunks: include diagonal neighbours in chunk flood fill detection", null, "1", new EnableFilter("OPT", true, "1")),
                new Option("CHR", OptionInputType.Boolean, "Chunks: merge chunks with overlapping rectangle bounds", null, "1", new EnableFilter("OPT", true, "1")),
                new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new Option("CUT", OptionInputType.Boolean, "Leave off the first frame (save differences without initial state)", noFirstFrame? "1" : "0"),
            };
        }

        /// <summary>
        /// Saves the given file as this type.
        /// </summary>
        /// <param name="fileToSave">The input file to convert.</param>
        /// <param name="savePath">The path to save to.</param>
        /// <param name="saveOptions">Extra options for customising the save process. Request the list from GetSaveOptions.</param>
        public override void SaveAsThis(SupportedFileType fileToSave, string savePath, Option[] saveOptions)
        {
            string vdaName;
            string vdxName;
            if (savePath.EndsWith(".VDX", StringComparison.InvariantCultureIgnoreCase))
            {
                vdaName = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".vda");
                vdxName = savePath;
            }
            else // No explicit check on VDA.
            {
                vdaName = savePath;
                vdxName = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".vdx");
            }
            byte[] vdxFile;
            byte[] data = this.SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
            File.WriteAllBytes(vdaName, data);
            if (vdxFile != null)
                File.WriteAllBytes(vdxName, vdxFile);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // dummy function; this should never be used since it saves without vdx file.
            byte[] vdxFile;
            return this.SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
        }

        public byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions, out byte[] vdxFile)
        {
            Color[] palette;
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave, out palette);
            int nrOfFrames = frames.Length;
            bool useChunks = Int32.Parse(Option.GetSaveOptionValue(saveOptions, "OPT")) == 1;
            bool chunkDiag = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CH8"));
            bool chunkRects = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CHR"));
            bool cutfirstFrame = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "CUT"));
            int compressionType;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (compressionType < 0 || compressionType > 2)
                compressionType = 0;
            Bitmap origImage = frames[0].GetBitmap();
            // Forcing this to 320x200 for now.
            int origWidth = 320;
            int origHeight = 200;
            int fullImageStride;
            byte[] previousImageData = ImageUtils.GetImageData(origImage, out fullImageStride, true);
            bool[] previousImageNonTransIndex = previousImageData.Select(b => b != TransparentIndex).ToArray();
            int previousImageStride = fullImageStride;
            List<List<VideoChunk>> saveFrames = new List<List<VideoChunk>>();

            if (!cutfirstFrame)
            {
                VideoChunk chunk = new VideoChunk(previousImageData, new Rectangle(0, 0, origWidth, origHeight));
                saveFrames.Add(new List<VideoChunk>() { chunk });
            }
            for (int i = 1; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                int stride;
                Bitmap currentImage = frame.GetBitmap();
                byte[] imageData = ImageUtils.GetImageData(currentImage, out stride, true);
                byte[] imageDataOpt = ArrayUtils.CloneArray(imageData);
                int prevOffs = 0;
                int frameOffs = 0;
                for (int y = 0; y < origHeight; ++y)
                {
                    int curFrameOffs = frameOffs;
                    int curPrevOffs = prevOffs;
                    for (int x = 0; x < origWidth; ++x)
                    {
                        if (imageData[curFrameOffs] == TransparentIndex)
                        {
                            if (previousImageNonTransIndex[curPrevOffs])
                                throw new ArgumentException("adding pixels of color #255 on new locations after frame 0 is not supported.", "fileToSave");
                        }
                        if (imageData[curFrameOffs] == previousImageData[curPrevOffs])
                            imageDataOpt[curFrameOffs] = TransparentIndex;
                        curFrameOffs++;
                        curPrevOffs++;
                    }
                    frameOffs += stride;
                    prevOffs += previousImageStride;
                }
                if (!useChunks)
                {
                    // optimize diff frame by cropping it.
                    int xOffset = 0;
                    int yOffset = 0;
                    int newWidth = origWidth;
                    int newHeight = origHeight;
                    imageDataOpt = ImageUtils.OptimizeXWidth(imageDataOpt, ref newWidth, newHeight, ref xOffset, true, TransparentIndex, 0xFF, true);
                    imageDataOpt = ImageUtils.OptimizeYHeight(imageDataOpt, newWidth, ref newHeight, ref yOffset, true, TransparentIndex, 0xFFFF, true);
                    VideoChunk chunk = new VideoChunk(imageDataOpt, new Rectangle(xOffset, yOffset, newWidth, newHeight));
                    saveFrames.Add(new List<VideoChunk>() {chunk});
                }
                else
                {
                    List<bool[,]> inBlobs;
                    bool[,] fullBlobs;
                    Func<byte[], int, int, bool> clearsThreshold = (bytes, y, x) => bytes[y * stride + x] != TransparentIndex;
                    List<List<Point>> blobs = BlobDetection.FindBlobs(imageDataOpt, origWidth, origHeight, clearsThreshold, chunkDiag, true, out inBlobs, out fullBlobs);
                    if (chunkRects)
                        BlobDetection.MergeBlobs(blobs, origWidth, origHeight, null, 0);

                    List<VideoChunk> frameChunks = new List<VideoChunk>();
                    int blobsCount = blobs.Count;
                    for (int b = 0; b < blobsCount; ++b)
                    {
                        List<Point> blob = blobs[b];
                        bool[,] inBlob = inBlobs[b];
                        Rectangle rect = BlobDetection.GetBlobBounds(blob);
                        byte[] img = ImageUtils.CopyFrom8bpp(imageDataOpt, origWidth, origHeight, stride, rect);
                        if (!chunkRects)
                        {
                            // Remove pixels from the rectangle that are not part of the blob.
                            int lineIndex = 0;
                            int rectW = rect.Width;
                            int rectX = rect.X;
                            int rectY = rect.Y;
                            int maxH = rectY + rect.Height;
                            int maxW = rectX + rectW;
                            for (int y = rectY; y < maxH; ++y)
                            {
                                int byteIndex = lineIndex;
                                for (int x = rectX; x < maxW; ++x)
                                {
                                    if (!inBlob[y, x])
                                        img[byteIndex] = TransparentIndex;
                                    byteIndex++;
                                }
                                lineIndex += rectW;
                            }
                        }
                        VideoChunk chunk = new VideoChunk(img, rect);
                        frameChunks.Add(chunk);
                    }
                    saveFrames.Add(frameChunks);
                }
                previousImageData = imageData;
                previousImageNonTransIndex = previousImageData.Select(b => b != TransparentIndex).ToArray();
                previousImageStride = stride;
            }
            // Add unique chunks to a single list, and add all rects used for each unique chunk to the rect.
            List<VideoChunk> finalChunks = new List<VideoChunk>();
            List<List<Rectangle>> allImageRects = new List<List<Rectangle>>();
            int framesCount = saveFrames.Count;
            for (int i = 0; i < framesCount; ++i)
            {
                List<VideoChunk> frameChunks = saveFrames[i];
                int frameChunksCount = frameChunks.Count;
                for (int j = 0; j < frameChunksCount; ++j)
                {
                    VideoChunk frameChunk = frameChunks[j];
                    // Find which index in the already-added chunks equals the current chunk.
                    // This can only match one entry since this mechanism makes sure only uniques are put in that final list.
                    int[] found = Enumerable.Range(0, finalChunks.Count).Where(c => frameChunk.Equals(finalChunks[c])).ToArray();
                    if (found.Length > 0)
                    {
                        // Earlier match was found; treat as copy. Add this one's rectangle to the 'allImageRects' list of the found index.
                        int index = found[0];
                        allImageRects[index].Add(frameChunk.ImageRect);
                        frameChunk.FinalIndex = index;
                    }
                    else
                    {
                        // Copy to new chunk! Otherwise later messing with the ImageRect will modify one of the frames.
                        // Image data can be set by reference since these are the final unique entries.
                        VideoChunk finalFrameChunk = new VideoChunk(frameChunk.ImageData, frameChunk.ImageRect);
                        frameChunk.FinalIndex = finalChunks.Count;
                        finalChunks.Add(finalFrameChunk);
                        allImageRects.Add(new List<Rectangle>() {finalFrameChunk.ImageRect});
                        if (finalChunks.Count > 0x7FFD)
                            throw new ArgumentException("Chunk count exceeds " + 0x7FFD + ".", "fileToSave");
                    }
                    // clear this so it can get cleaned up on the copied chunks. It's no longer needed anyway; the reference to the final frame is set.
                    frameChunk.ImageData = null;
                }
            }
            // Set ImageRect to the most occurring image rect in the group. This minimises the use of the 3-byte offset-reassigning command in the vdx file.
            int finalChunksCount = finalChunks.Count;
            for (int i = 0; i < finalChunksCount; ++i)
                finalChunks[i].ImageRect = allImageRects[i].GroupBy(r => r).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            // BinaryWriter specs say it writes UInt16 as little-endian, meaning it is independent from system endianness.
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                for (int i = 0; i < framesCount; ++i)
                {
                    List<VideoChunk> frameChunks = saveFrames[i];
                    int frChunkCount = frameChunks.Count;
                    for (int j = 0; j < frChunkCount; ++j)
                    {
                        VideoChunk frameChunk = frameChunks[j];
                        ushort index = (ushort) frameChunk.FinalIndex;
                        VideoChunk baseChunk = finalChunks[index];
                        if (baseChunk.ImageRect == frameChunk.ImageRect)
                            bw.Write(index);
                        else
                        {
                            bw.Write((ushort) (index | 0x8000));
                            bw.Write((ushort) (frameChunk.ImageRect.X));
                            bw.Write((ushort) (frameChunk.ImageRect.Y));
                        }
                    }
                    bw.Write(FrameEnd);
                }
                bw.Write(AnimEnd);
                bw.Flush();
                vdxFile = ms.ToArray();
            }
            // Compress chunks
            if (compressionType > 0)
            {
                for (int i = 0; i < finalChunksCount; ++i)
                {
                    VideoChunk chunk = finalChunks[i];
                    byte[] compressedBytes = null;
                    try
                    {
                        if (compressionType == 1)
                            compressedBytes = MythosCompression.FlagRleEncode(chunk.ImageData, 0xFE, chunk.ImageRect.Width, 8);
                        else if (compressionType == 2)
                            compressedBytes = MythosCompression.CollapsedTransparencyEncode(chunk.ImageData, TransparentIndex, chunk.ImageRect.Width, 8);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new FileTypeSaveException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
                    }
                    if (compressedBytes != null && compressedBytes.Length < chunk.ImageData.Length)
                    {
                        chunk.ImageData = compressedBytes;
                        chunk.Compressed = true;
                    }
                }
            }
            // Add palette, the easy way.
            byte[] palData;
            using (FileFramesMythosPal pal = new FileFramesMythosPal())
            using(FilePalette8Bit inputPal = new FilePalette8Bit(palette))
                palData = pal.SaveToBytesAsThis(inputPal, null);
            // Full length: headers and data for all chunks.
            int fullLength = palData.Length + finalChunksCount * 0x08 + finalChunks.Sum(x => x.ImageData.Length);
            byte[] vdaFile = new byte[fullLength];
            palData.CopyTo(vdaFile, 0);
            int offset = palData.Length;
            for (int i = 0; i < finalChunksCount; ++i)
            {
                VideoChunk chunk = finalChunks[i];
                ArrayUtils.WriteUInt16ToByteArrayLe(vdaFile, offset + 0, (ushort)(chunk.ImageRect.Width - 1));
                ArrayUtils.WriteUInt16ToByteArrayLe(vdaFile, offset + 2, (ushort)(chunk.ImageRect.Height - 1));
                vdaFile[offset + 4] = (byte) (chunk.Compressed ? 0x02 : 0x00);
                ArrayUtils.WriteUInt16ToByteArrayLe(vdaFile, offset + 5, (ushort)(chunk.ImageRect.X));
                vdaFile[offset + 7] = (byte) (chunk.ImageRect.Y & 0xFF);
                offset += 8;
                byte[] chunkData = chunk.ImageData;
                int dataLen = chunkData.Length;
                Array.Copy(chunkData, 0, vdaFile, offset, dataLen);
                offset += dataLen;
            }
            return vdaFile;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave, out Color[] palette)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames == null ? 0 : frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            palette = fileToSave.GetColors();
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType sft = frames[i];
                if (sft.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
                if (sft.Width != 320 || sft.Height != 200)
                    throw new ArgumentException(String.Format(ERR_DIMENSIONS_INPUT, 320, 200), "fileToSave");
                if (palette == null || palette.Length == 0)
                    palette = sft.GetColors();
            }
            if (palette == null)
                throw new ArgumentException(ERR_COLORS_NEEDED, "fileToSave");
            return frames;
        }

        /// <summary>
        /// Contains the data for a chunk of VDA video data.
        /// </summary>
        private class VideoChunk: IEqualityComparer<VideoChunk>
        {
            public byte[] ImageData { get; set; }
            public Rectangle ImageRect { get; set; }
            public int FinalIndex { get; set; }
            public bool Compressed { get; set; }

            public VideoChunk(byte[] imageData, Rectangle imageRect)
            {
                this.ImageData = imageData;
                this.ImageRect = imageRect;
                this.FinalIndex = -1;
            }

            public bool Equals(VideoChunk x, VideoChunk y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;
                return x.ImageRect.Width == y.ImageRect.Width && x.ImageRect.Height == y.ImageRect.Height && x.ImageData.SequenceEqual(y.ImageData);
            }

            public int GetHashCode(VideoChunk obj)
            {
                byte[] imageBytes = new byte[this.ImageData.Length + 8];
                ArrayUtils.WriteInt32ToByteArrayLe(imageBytes, 0, this.ImageRect.Width);
                ArrayUtils.WriteInt32ToByteArrayLe(imageBytes, 4, this.ImageRect.Height);
                return (int)Crc32.ComputeChecksum(imageBytes);
            }

            public override bool Equals(object obj)
            {
                VideoChunk objVc = obj as VideoChunk;
                return objVc != null && this.Equals(this, objVc);
            }

            public override int GetHashCode()
            {
                return this.GetHashCode(this);
            }
        }
    }
}