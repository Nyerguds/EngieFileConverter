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
        public override String ShortTypeName { get { return "Mythos Visage Animation"; } }
        public override String ShortTypeDescription { get { return "Mythos Visage Animation file"; } }
        public override String[] FileExtensions { get { return new String[] { "vda", "vdx" }; } }
        public override Boolean[] TransparencyMask { get { return (!this._isFramed || (this._noFirstFrame && !this._isChained)) ? base.TransparencyMask : new Boolean[0]; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        private Boolean _noFirstFrame;
        private Boolean _isChained;
        private Boolean _isFramed;
        
        public override List<String> GetFilesToLoadMissingData(String originalPath)
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
            String pngName = TestForPngStartFrame(originalPath);
            if (pngName != null)
                return new List<String>() { pngName };
            String baseName;
            // Call file range detection algorithm already in place on FileFrames class.
            String[] frameNames = FileFrames.GetFrameFilesRange(originalPath, out baseName);
            if (frameNames == null)
                return null;
            originalPath = Path.GetFullPath(originalPath);
            // The function from FileFrames returns the whole range, which might be too much. Find the actual file we started from.
            Int32 index = Array.FindIndex(frameNames.ToArray(), t => String.Equals(t, originalPath, StringComparison.InvariantCultureIgnoreCase));
            // Check previous files until finding one with an initial frame.
            List<String> chain = new List<String>();
            for (Int32 i = index - 1; i >= 0; i--)
            {
                String curName = frameNames[i];
                Byte[] testBytesVda = File.ReadAllBytes(curName);
                String vdxPath = Path.Combine(Path.GetDirectoryName(curName), Path.GetFileNameWithoutExtension(curName) + ".VDX");
                Byte[] testBytesVdx = File.ReadAllBytes(vdxPath);
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
                    Boolean startsWithFrameZero = (ArrayUtils.ReadIntFromByteArray(testBytesVdx, 0, 2, true) & 0x7FFF) == 0;
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
                    if (!testFile.m_PaletteSet)
                        return null;
                    Int32 badPalMatches = 0;
                    Color[] testPal = testFile.GetColors();
                    for (Int32 p = 0; p < 256; ++p)
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
                        Boolean noFirstFrame;
                        // Call using the testFirstFrame option to abort after performing the "noFirstFrame" check.
                        // Technically this check is incomplete; if the first referenced frame is not frame #0 it fails.
                        // But the first referenced frame should always be frame 0... even my VDX optimisation only changes the VDA coordinates, not order.
                        try
                        {
                            this.BuildAnimationFromChunks(originalPath, testBytesVdx, testFile.m_FramesList, framesXY, null, out noFirstFrame, true);
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
                    String pngChained = TestForPngStartFrame(curName);
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

        private String TestForPngStartFrame(String originalPath)
        {
            String pngName = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + ".PNG");
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

        public override void ReloadFromMissingData(Byte[] fileData, String originalPath, List<String> loadChain)
        {
            Byte[] lastFrameData = null;
            String lastFrameInfo = String.Empty;
            String firstName = loadChain.First();
            Int32 lastIndex = loadChain.Count - 1;
            Boolean fromPng = false;
            for (Int32 i = 0; i <= lastIndex; ++i)
            {
                String chainFilePath = loadChain[i];
                try
                {
                    if (i == 0 && chainFilePath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        lastFrameData = GetFrameDataFromPng(firstName, ref lastFrameInfo);
                        if (lastFrameData != null)
                        {
                            fromPng = lastIndex == 0;
                            continue;
                        }
                    }
                    Byte[] chainFileBytes = File.ReadAllBytes(chainFilePath);
                    using (FileFramesMythosVda chainFile = new FileFramesMythosVda())
                    {
                        chainFile.LoadFile(chainFileBytes, chainFilePath, lastFrameData);
                        Int32 lastFrIndex = chainFile.Frames.Length - 1;
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

        private Byte[] GetFrameDataFromPng(String pngName, ref String lastFrameInfo)
        {
            Byte[] lastFrameData = null;
            if (File.Exists(pngName))
            {
                try
                {
                    using (FileImageFrame pngFile = new FileImageFrame())
                    {
                        // Uses specific PNG loading from its superclass, since
                        // FileImageFrame inherits from png and still contains its mime type.
                        pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                        pngFile.LoadFileFrame(null, new FileImagePng().ShortTypeDescription, pngFile.GetBitmap(), pngName, -1);
                        lastFrameData = this.Get320x200FrameData(pngFile);
                        
                        if (lastFrameData != null)
                            lastFrameInfo = pngFile.ExtraInfo;
                    }
                }
                catch { /* can't load as png file. Abort.*/ }
            }
            return lastFrameData;
        }

        protected Byte[] Get320x200FrameData(SupportedFileType loadedFrame)
        {
            if (loadedFrame == null)
                return null;
            Bitmap lastFrameImage = loadedFrame.GetBitmap();
            if (lastFrameImage == null || lastFrameImage.Width != 320 || lastFrameImage.Height != 200 || lastFrameImage.PixelFormat != PixelFormat.Format8bppIndexed)
                return null;
            Int32 stride;
            // stride collapse is probably not needed... 320 is divisible by 4.
            return ImageUtils.GetImageData(lastFrameImage, out stride, true);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFile(fileData, filename, null);
        }

        public void LoadFile(Byte[] fileData, String filename, Byte[] initialFrameData)
        {
            Byte[] vdaBytes;
            Byte[] vdxBytes;
            String vdaName;
            String vdxName;
            this.GetLoadFileInfo(fileData, filename, out vdaBytes, out vdxBytes, out vdaName, out vdxName);
            if (vdaBytes == null)
                throw new FileTypeLoadException("Cannot load a VDA video from a VDX alone!");

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
            Int32 chunks = this.m_FramesList.Count;
            if (this._isFramed)
            {
                Boolean noFirstFrame;
                List<SupportedFileType> framesList = this.BuildAnimationFromChunks(vdaName, vdxBytes, this.m_FramesList, framesXY, initialFrameData, out noFirstFrame, false);
                this._noFirstFrame = noFirstFrame;
                // Apply transparency mask.
                // Give parent ref so the SetColors mechanism thinks this is an update coming from the parent and will not loop over the parent's frames.
                this.SetColors(this.m_Palette, this);
                this.m_BackupPalette = null;
                this.m_FramesList = framesList;
            }
            if (!_isFramed)
                this.ExtraInfo = "VDX file missing; showing raw chunks\n" + this.ExtraInfo;
            this.ExtraInfo += "\nChunks: " + chunks;
        }

        private void GetLoadFileInfo(Byte[] fileData, String filename, out Byte[] vdaBytes, out Byte[] vdxBytes, out String vdaName, out String vdxName)
        {
            vdxBytes = null;
            vdaName = null;
            vdxName = null;
            if (filename != null)
            {
                Boolean isVda = filename.EndsWith(".VDA", StringComparison.InvariantCultureIgnoreCase);
                Boolean isVdx = filename.EndsWith(".VDx", StringComparison.InvariantCultureIgnoreCase);
                String vdaNm = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".VDA");
                String vdxNm = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".VDx");
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
                    Boolean hasVda = File.Exists(vdaNm);
                    Boolean hasVdx = File.Exists(vdxNm);
                    if (hasVda && hasVdx)
                    {
                        Boolean dataIsVdx = this.CheckForVdx(fileData);
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
                    throw new FileTypeLoadException("Can't load a video from .VDX file without filename!");
                vdaBytes = fileData;
            }
        }

        private List<SupportedFileType> BuildAnimationFromChunks(String sourcePath, Byte[] framesInfo, List<SupportedFileType> allChunks, List<Point> framesXY, Byte[] initialFrameData, out Boolean noFirstFrame, Boolean testFirstFrame)
        {
            noFirstFrame = initialFrameData != null;
            List<SupportedFileType> framesList = new List<SupportedFileType>();
            Int32 offset = 0;
            Int32 imageWidth = 320;
            Int32 imageHeight = 200;
            Int32 imageStride = 320;
            Int32 arraySize = imageWidth * imageHeight;
            if (initialFrameData != null && initialFrameData.Length != arraySize)
                throw new FileTypeLoadException("Bad start frame data length!");
            Byte[] imageData = initialFrameData == null ? null : initialFrameData.ToArray();

            Boolean[] pasteTransMask = base.TransparencyMask;
            Boolean[] imageTransMask = pasteTransMask;
            if (initialFrameData != null)
            {
                // starting frame
                Bitmap curImage = ImageUtils.BuildImage(imageData, imageWidth, imageHeight, imageStride, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, curImage, sourcePath, framesList.Count);
                frame.SetColorsInPalette(this.m_PaletteSet ? this.m_Palette.Length : 0);
                // Give parent ref so the SetColors mechanism thinks this is an update coming from the parent and will not loop over the parent's frames.
                frame.SetColors(this.m_Palette, this);
                frame.SetFileClass(this.FrameInputFileClass);
                frame.SetExtraInfo(CHUNKS + 1);
                framesList.Add(frame);
            }
            Int32 chunks = 0;
            while (offset + 2 <= framesInfo.Length)
            {
                UInt16 curVal = (UInt16)ArrayUtils.ReadIntFromByteArray(framesInfo, offset, 2, true);
                if (curVal == 0xFFFE)
                    break;
                if (curVal == 0xFFFF)
                {
                    // No chunks at all specified for the very first frame. Could happen in a continued animation starting with a pause I guess?
                    if (imageData == null)
                    {
                        noFirstFrame = true;
                        if (testFirstFrame)
                            return null;
                        imageData = new Byte[arraySize];
                        for (Int32 i = 0; i < arraySize; ++i)
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
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(this, this, curImage, sourcePath, framesList.Count);
                    frame.SetColorsInPalette(this.m_PaletteSet ? this.m_Palette.Length : 0);
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
                    if (testFirstFrame && chunks > 0)
                    {
                        noFirstFrame = true;
                        return null;
                    }
                    Int32 frameNumber = curVal & 0x7FFF;
                    if (allChunks.Count <= frameNumber)
                        throw new FileLoadException("Video frames file references more frames than available in graphics file!");
                    Int32 xOffset;
                    Int32 yOffset;
                    if ((curVal & 0x8000) != 0)
                    {
                        if (offset + 6 >= framesInfo.Length)
                            throw new FileLoadException("Illegal data order in video frames file!");
                        xOffset = (UInt16) ArrayUtils.ReadIntFromByteArray(framesInfo, offset + 2, 2, true);
                        yOffset = (UInt16) ArrayUtils.ReadIntFromByteArray(framesInfo, offset + 4, 2, true);
                        offset += 4;
                    }
                    else
                    {
                        if (framesXY.Count < frameNumber)
                            throw new FileLoadException("Video frames file references more frames than available in the graphics file!");
                        xOffset = framesXY[frameNumber].X;
                        yOffset = framesXY[frameNumber].Y;
                    }
                    Bitmap currentImage = allChunks[frameNumber].GetBitmap();
                    Int32 stride;
                    Int32 width = currentImage.Width;
                    Int32 height = currentImage.Height;
                    Byte[] currentFrameData = ImageUtils.GetImageData(currentImage, out stride, true);
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
                            throw new FileLoadException("Illegal data in video frames file: paint coordinates out of bounds!");
                        ImageUtils.PasteOn8bpp(imageData, imageWidth, imageHeight, imageStride, currentFrameData, width, height, stride, new Rectangle(xOffset, yOffset, width, height), pasteTransMask, true);
                    }
                    chunks++;
                    offset += 2;
                }
            }
            return framesList;
        }

        protected Boolean CheckForVdx(Byte[] fileData)
        {
            if (fileData.Length < 4 || fileData.Length % 2 != 0)
                return false;
            // Last two blocks should be FFFF and FFFE.
            UInt16 intl4 = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, fileData.Length - 4, 2, true);
            UInt16 intl2 = (UInt16)ArrayUtils.ReadIntFromByteArray(fileData, fileData.Length - 2, 2, true);
            if (intl4 == 0xFFFF && intl2 == 0xFFFE)
                return true;
            return false;
        }
        
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            if (fileToSave == null)
                return null;
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
             if (nrOfFrames == 0)
                throw new NotSupportedException("This format needs at least one frame.");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType sft = frames[i];
                if (sft.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8bpp images.");
                if (sft.Width != 320 || sft.Height != 200)
                    throw new NotSupportedException("This format needs 320x200 frames.");
            }
            SupportedFileType firstFrame = frames[0];
            Int32 firstFrameW = firstFrame.Width;
            Int32 firstFrameH = firstFrame.Height;
            for (Int32 i = 1; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame.Width != firstFrameW || frame.Height != firstFrameH)
                    throw new NotSupportedException("All frames should have the same dimensions.");
            }
            Int32 compression = 0;
            Boolean noFirstFrame = false;
            FileFramesMythosVgs fileVgs = fileToSave as FileFramesMythosVgs;
            if (fileVgs != null)
                compression = fileVgs.CompressionType;
            if (compression < 0 || compression > this.compressionTypes.Length)
                compression = 0;
            FileFramesMythosVda fileVda = fileToSave as FileFramesMythosVda;
            if (fileVda != null)
                noFirstFrame = fileVda._noFirstFrame;
            return new SaveOption[]
            {
                new SaveOption("OPT", SaveOptionType.ChoicesList, "Optimisation:", "Save simple cropped diff frames,Optimise to chunks", "1"),
                new SaveOption("CH8", SaveOptionType.Boolean, "Chunks: include diagonal neighbours in chunk flood fill detection", null, "1", new SaveEnableFilter("OPT", false, "1")),
                new SaveOption("CHR", SaveOptionType.Boolean, "Chunks: merge chunks with overlapping rectangle bounds", null, "1", new SaveEnableFilter("OPT", false, "1")),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new SaveOption("CUT", SaveOptionType.Boolean, "Leave off the first frame (save differences without initial state)", noFirstFrame? "1" : "0"),
            };
        }

        /// <summary>
        /// Saves the given file as this type.
        /// </summary>
        /// <param name="fileToSave">The input file to convert.</param>
        /// <param name="savePath">The path to save to.</param>
        /// <param name="saveOptions">Extra options for customising the save process. Request the list from GetSaveOptions.</param>
        public override void SaveAsThis(SupportedFileType fileToSave, String savePath, SaveOption[] saveOptions)
        {
            String vdaName;
            String vdxName;
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
            Byte[] vdxFile;
            Byte[] data = this.SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
            File.WriteAllBytes(vdaName, data);
            if (vdxFile != null)
                File.WriteAllBytes(vdxName, vdxFile);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // dummy function; this should never be used since it saves without vdx file.
            Byte[] vdxFile;
            return this.SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
        }

        public Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, out Byte[] vdxFile)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new NotSupportedException("No source data given!");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new NotSupportedException("This format needs at least one frame.");
            Color[] palette = fileToSave.GetColors();
            SupportedFileType palFile = fileToSave;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType sft = frames[i];
                if (sft.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8bpp images.");
                if (sft.Width != 320 || sft.Height != 200)
                    throw new NotSupportedException("This format needs 320x200 frames.");
                if (palette == null || palette.Length == 0)
                {
                    palette = sft.GetColors();
                    palFile = sft;
                }
            }
            if (palFile == null)
                throw new NotSupportedException("This format needs a color palette.");
            Boolean useChunks = Int32.Parse(SaveOption.GetSaveOptionValue(saveOptions, "OPT")) == 1;
            Boolean chunkDiag = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CH8"));
            Boolean chunkRects = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CHR"));
            Boolean cutfirstFrame = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CUT"));
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (compressionType < 0 || compressionType > 2)
                compressionType = 0;
            Bitmap origImage = frames[0].GetBitmap();
            // Forcing this to 320x200 for now.
            Int32 origWidth = 320;
            Int32 origHeight = 200;
            Int32 fullImageStride;
            Byte[] previousImageData = ImageUtils.GetImageData(origImage, out fullImageStride, true);
            Boolean[] previousImageNonTransIndex = previousImageData.Select(b => b != TransparentIndex).ToArray();
            Int32 previousImageStride = fullImageStride;
            List<List<VideoChunk>> saveFrames = new List<List<VideoChunk>>();

            if (!cutfirstFrame)
            {
                VideoChunk chunk = new VideoChunk(previousImageData, new Rectangle(0, 0, origWidth, origHeight));
                saveFrames.Add(new List<VideoChunk>() { chunk });
            }
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Int32 stride;
                Bitmap currentImage = frame.GetBitmap();
                Byte[] imageData = ImageUtils.GetImageData(currentImage, out stride, true);
                Byte[] imageDataOpt = imageData.ToArray();
                Int32 prevOffs = 0;
                Int32 frameOffs = 0;
                for (Int32 y = 0; y < origHeight; ++y)
                {
                    Int32 curFrameOffs = frameOffs;
                    Int32 curPrevOffs = prevOffs;
                    for (Int32 x = 0; x < origWidth; ++x)
                    {
                        if (imageData[curFrameOffs] == TransparentIndex)
                        {
                            if (previousImageNonTransIndex[curPrevOffs])
                                throw new NotSupportedException("adding pixels of color #255 on new locations after frame 0 is not supported!");
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
                    Int32 xOffset = 0;
                    Int32 yOffset = 0;
                    Int32 newWidth = origWidth;
                    Int32 newHeight = origHeight;
                    imageDataOpt = ImageUtils.OptimizeXWidth(imageDataOpt, ref newWidth, newHeight, ref xOffset, true, TransparentIndex, 0xFF, true);
                    imageDataOpt = ImageUtils.OptimizeYHeight(imageDataOpt, newWidth, ref newHeight, ref yOffset, true, TransparentIndex, 0xFFFF, true);
                    VideoChunk chunk = new VideoChunk(imageDataOpt, new Rectangle(xOffset, yOffset, newWidth, newHeight));
                    saveFrames.Add(new List<VideoChunk>() {chunk});
                }
                else
                {
                    List<Boolean[,]> inBlobs;
                    Boolean[,] fullBlobs;
                    Func<Byte[], Int32, Int32, Boolean> clearsThreshold = (bytes, y, x) => bytes[y * stride + x] != TransparentIndex;
                    List<List<Point>> blobs = BlobDetection.FindBlobs(imageDataOpt, origWidth, origHeight, clearsThreshold, chunkDiag, true, out inBlobs, out fullBlobs);
                    if (chunkRects)
                        BlobDetection.MergeBlobs(blobs, origWidth, origHeight, null, 0);

                    List<VideoChunk> frameChunks = new List<VideoChunk>();
                    Int32 blobsCount = blobs.Count;
                    for (Int32 b = 0; b < blobsCount; ++b)
                    {
                        List<Point> blob = blobs[b];
                        Boolean[,] inBlob = inBlobs[b];
                        Rectangle rect = BlobDetection.GetBlobBounds(blob);
                        Byte[] img = ImageUtils.CopyFrom8bpp(imageDataOpt, origWidth, origHeight, stride, rect);
                        if (!chunkRects)
                        {
                            // Remove pixels from the rectangle that are not part of the blob.
                            Int32 lineIndex = 0;
                            Int32 rectW = rect.Width;
                            Int32 rectX = rect.X;
                            Int32 rectY = rect.Y;
                            Int32 maxH = rectY + rect.Height;
                            Int32 maxW = rectX + rectW;
                            for (Int32 y = rectY; y < maxH; ++y)
                            {
                                Int32 byteIndex = lineIndex;
                                for (Int32 x = rectX; x < maxW; ++x)
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
            Int32 framesCount = saveFrames.Count;
            for (Int32 i = 0; i < framesCount; ++i)
            {
                List<VideoChunk> frameChunks = saveFrames[i];
                Int32 frameChunksCount = frameChunks.Count;
                for (Int32 j = 0; j < frameChunksCount; ++j)
                {
                    VideoChunk frameChunk = frameChunks[j];
                    // Find which index in the already-added chunks equals the current chunk.
                    // This can only match one entry since this mechanism makes sure only uniques are put in that final list.
                    Int32[] found = Enumerable.Range(0, finalChunks.Count).Where(c => frameChunk.Equals(finalChunks[c])).ToArray();
                    if (found.Length > 0)
                    {
                        // Earlier match was found; treat as copy. Add this one's rectangle to the 'allImageRects' list of the found index.
                        Int32 index = found[0];
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
                            throw new NotSupportedException("Chunk count exceeds " + 0x7FFD + "!");
                    }
                    // clear this so it can get cleaned up on the copied chunks. It's no longer needed anyway; the reference to the final frame is set.
                    frameChunk.ImageData = null;
                }
            }
            // Set ImageRect to the most occurring image rect in the group. This minimises the use of the 3-byte offset-reassigning command in the vdx file.
            Int32 finalChunksCount = finalChunks.Count;
            for (Int32 i = 0; i < finalChunksCount; ++i)
                finalChunks[i].ImageRect = allImageRects[i].GroupBy(r => r).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            // BinaryWriter specs say it writes UInt16 as little-endian, meaning it is independent from system endianness.
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                for (Int32 i = 0; i < framesCount; ++i)
                {
                    List<VideoChunk> frameChunks = saveFrames[i];
                    Int32 frChunkCount = frameChunks.Count;
                    for (Int32 j = 0; j < frChunkCount; ++j)
                    {
                        VideoChunk frameChunk = frameChunks[j];
                        UInt16 index = (UInt16) frameChunk.FinalIndex;
                        VideoChunk baseChunk = finalChunks[index];
                        if (baseChunk.ImageRect == frameChunk.ImageRect)
                            bw.Write(index);
                        else
                        {
                            bw.Write((UInt16) (index | 0x8000));
                            bw.Write((UInt16) (frameChunk.ImageRect.X));
                            bw.Write((UInt16) (frameChunk.ImageRect.Y));
                        }
                    }
                    bw.Write((UInt16) 0xFFFF);
                }
                bw.Write((UInt16)0xFFFE);
                bw.Flush();
                vdxFile = ms.ToArray();
            }
            // Compress chunks
            if (compressionType > 0)
            {
                for (Int32 i = 0; i < finalChunksCount; ++i)
                {
                    VideoChunk chunk = finalChunks[i];
                    Byte[] compressedBytes = null;
                    MythosCompression mc = new MythosCompression();
                    try
                    {
                        if (compressionType == 1)
                            compressedBytes = mc.FlagRleEncode(chunk.ImageData, 0xFE, chunk.ImageRect.Width, 8);
                        else if (compressionType == 2)
                            compressedBytes = mc.CollapsedTransparencyEncode(chunk.ImageData, TransparentIndex, chunk.ImageRect.Width, 8);
                    }
                    catch (OverflowException ex)
                    {
                        throw new NotSupportedException(ex.Message, ex);
                    }
                    if (compressedBytes != null && compressedBytes.Length < chunk.ImageData.Length)
                    {
                        chunk.ImageData = compressedBytes;
                        chunk.Compressed = true;
                    }
                }
            }
            // Add palette, the easy way.
            Byte[] palData;
            using (FileFramesMythosPal pal = new FileFramesMythosPal())
                palData = pal.SaveToBytesAsThis(palFile, null);
            // Full length: headers and data for all chunks.
            Int32 fullLength = palData.Length + finalChunksCount * 0x08 + finalChunks.Sum(x => x.ImageData.Length);
            Byte[] vdaFile = new Byte[fullLength];
            palData.CopyTo(vdaFile, 0);
            Int32 offset = palData.Length;
            for (Int32 i = 0; i < finalChunksCount; ++i)
            {
                VideoChunk chunk = finalChunks[i];
                ArrayUtils.WriteIntToByteArray(vdaFile, offset + 0, 2, true, (UInt16) (chunk.ImageRect.Width - 1));
                ArrayUtils.WriteIntToByteArray(vdaFile, offset + 2, 2, true, (UInt16) (chunk.ImageRect.Height - 1));
                vdaFile[offset + 4] = (Byte) (chunk.Compressed ? 0x02 : 0x00);
                ArrayUtils.WriteIntToByteArray(vdaFile, offset + 5, 2, true, (UInt16) (chunk.ImageRect.X));
                vdaFile[offset + 7] = (Byte) (chunk.ImageRect.Y & 0xFF);
                offset += 8;
                Byte[] chunkData = chunk.ImageData;
                Int32 dataLen = chunkData.Length;
                Array.Copy(chunkData, 0, vdaFile, offset, dataLen);
                offset += dataLen;
            }
            return vdaFile;
        }

        /// <summary>
        /// Contains the data for a chunk of VDA video data.
        /// </summary>
        private class VideoChunk: IEqualityComparer<VideoChunk>
        {
            public Byte[] ImageData { get; set; }
            public Rectangle ImageRect { get; set; }
            public Int32 FinalIndex { get; set; }
            public Boolean Compressed { get; set; }

            public VideoChunk(Byte[] imageData, Rectangle imageRect)
            {
                this.ImageData = imageData;
                this.ImageRect = imageRect;
                this.FinalIndex = -1;
            }

            public Boolean Equals(VideoChunk x, VideoChunk y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;
                return x.ImageRect.Width == y.ImageRect.Width && x.ImageRect.Height == y.ImageRect.Height && x.ImageData.SequenceEqual(y.ImageData);
            }

            public Int32 GetHashCode(VideoChunk obj)
            {
                Byte[] imageBytes = new Byte[this.ImageData.Length + 8];
                ArrayUtils.WriteIntToByteArray(imageBytes, 0, 4, true, (UInt32) this.ImageRect.Width);
                ArrayUtils.WriteIntToByteArray(imageBytes, 4, 4, true, (UInt32) this.ImageRect.Height);
                return (Int32)Crc32.ComputeChecksum(imageBytes);
            }

            public override Boolean Equals(Object obj)
            {
                VideoChunk objVc = obj as VideoChunk;
                return objVc != null && this.Equals(this, objVc);
            }

            public override Int32 GetHashCode()
            {
                return this.GetHashCode(this);
            }
        }
    }
}