using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Nyerguds.GameData.Mythos;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileFramesMythosVda : FileFramesMythosVgs
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        public override String ShortTypeName { get { return "Mythos Visage Animation"; } }
        public override String ShortTypeDescription { get { return "Mythos Visage Animation file"; } }
        public override String[] FileExtensions { get { return new String[] { "vda", "vdx" }; } }
        public override Boolean[] TransparencyMask { get { return null; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public Boolean NoFirstFrame { get; set; }

        private String vdaLoadName;
        private String vdxLoadName;
        
        /*/
        public override FileFrames ChainLoadFiles(ref String[] fileNames, String originalPath)
        {
            // strategy: check original file for palette and start frame (meaning, no transparency and full 320x200).
            //-If it has no start frame, check files before it. Continue looking while the files have the same palette, and no start frame.
            // If palettes differ or no file with an actual start frame was found, return null.
            //-If it has a palette (or one with a palette was found by the logic above),
            // load more files after it, until the end or until finding another one with a palette.
            // If no frames without palette were found
            //-Change frameNames to the collection of actually used fules.

            return null;
        }
        //*/

        public override List<String> GetFilesToLoadMissingData(String originalPath)
        {
            // No missing data.
            if (!NoFirstFrame)
                return null;
            
            // Wrong file. Switch to the VDA one.
            if (originalPath.EndsWith(".VDX", StringComparison.InvariantCultureIgnoreCase))
            {
                originalPath = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + ".VDA");
                if (!File.Exists(originalPath))
                    return null;
            }
            // If a single png file of the same name is found it overrides normal chaining.
            String pngName = Path.Combine(Path.GetDirectoryName(originalPath), Path.GetFileNameWithoutExtension(originalPath) + ".PNG");
            if (File.Exists(pngName))
            {
                try
                {
                    using (FileImagePng pngFile = new FileImagePng())
                    {
                        pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                        if (pngFile.Width == 320 && pngFile.Height == 200)
                            return new List<String>() {pngName};
                    }
                }
                catch (FileLoadException)
                {
                    // ignore; continue with normal load
                }
            }
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
                    for (Int32 p = 0; p < 256; p++)
                        if (testPal[p] != m_Palette[p])
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
                }
            }
            return null;
        }

        public override void ReloadFromMissingData(Byte[] fileData, String originalPath, List<String> loadChain)
        {
            Byte[] lastFrameData = null;
            SupportedFileType lastFrame = null;
            String firstName = loadChain.First();
            if (loadChain.Count == 1 && loadChain[0].EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
            {
                String pngName = loadChain[0];
                if (File.Exists(pngName))
                {
                    try
                    {
                        
                        FileImageFrame pngFile = new FileImageFrame();
                        // Uses specific PNG loading from its superclass, since
                        // FileImageFrame inherits from png and still contains its mime type.
                        pngFile.LoadFile(File.ReadAllBytes(pngName), pngName);
                        pngFile.LoadFileFrame(null, new FileImagePng().ShortTypeDescription, pngFile.GetBitmap(), pngName, -1);
                        lastFrameData = Get320x200FrameData(pngFile);
                        if (lastFrameData != null)
                        {
                            lastFrame = pngFile;
                            loadChain.Clear();
                        }
                        else
                        {
                            pngFile.Dispose();
                        }
                    }
                    catch { return; }// can't load as png file. Abort.
                }
            }
            Int32 lastIndex = loadChain.Count - 1;
            for (Int32 i = 0; i <= lastIndex; i++)
            {
                String chainFilePath = loadChain[i];
                try
                {
                    Byte[] chainFileBytes = File.ReadAllBytes(chainFilePath);
                    using (FileFramesMythosVda chainFile = new FileFramesMythosVda())
                    {
                        chainFile.LoadFile(chainFileBytes, chainFilePath, lastFrameData);
                        Int32 lastFrIndex = chainFile.Frames.Length - 1;
                        if (lastFrIndex < 0)
                            return;
                        lastFrame = chainFile.m_FramesList[lastFrIndex];
                        lastFrameData = Get320x200FrameData(lastFrame);
                        // Maybe use exception? Should never happen though.
                        if (lastFrameData == null)
                            return;
                        // Replace extracted frame to exclude it from cleanup when chainfile gets disposed..
                        if (i == lastIndex)
                            chainFile.m_FramesList[lastFrIndex] = new FileImageFrame();
                    }
                }
                catch { return; } // can't load as VDA file. Abort.
            }
            this.LoadFile(fileData, originalPath, lastFrameData);
            if (lastFrame != null)
            {
                this.ExtraInfo += "\nData chained from " + Path.GetFileName(firstName);
                FileImageFrame last = lastFrame as FileImageFrame;
                if (last != null)
                    last.SetExtraInfo((last.ExtraInfo + "\nLoaded from previous file").TrimStart('\n'));
                this.m_FramesList.Insert(0, lastFrame);
            }
        }

        protected Byte[] Get320x200FrameData(SupportedFileType loadedFrame)
        {
            if (loadedFrame.Width != 320 || loadedFrame.Height != 200)
                return null;
            Bitmap lastFrameImage = loadedFrame.GetBitmap();
            if (lastFrameImage.PixelFormat != PixelFormat.Format8bppIndexed)
                return null;
            Int32 width = lastFrameImage.Width;
            Int32 height = lastFrameImage.Height;
            Int32 stride;
            Byte[] frameData = ImageUtils.GetImageData(lastFrameImage, out stride);
            return ImageUtils.CollapseStride(frameData, width, height, 8, ref stride);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            LoadFile(fileData, filename, null);
        }

        public void LoadFile(Byte[] fileData, String filename, Byte[] initialFrameData)
        {
            Byte[] vdaBytes;
            Byte[] vdxBytes;
            String vdaName;
            String vdxName;
            GetLoadFileInfo(fileData, filename, out vdaBytes, out vdxBytes, out vdaName, out vdxName);
            vdaLoadName = vdaName;
            vdxLoadName = vdxName;

            if (vdaName != null)
            {
                this.SetFileNames(vdaName.ToUpper());
                if (vdxBytes != null && vdxName != null)
                    this.LoadedFileName += "/" + Path.GetExtension(vdxName).TrimStart('.').ToUpper();
            }
            List<Point> framesXY;
            this.LoadFromFileData(vdaBytes, vdaName, false, false, true, out framesXY, false);
            this.m_Palette = PaletteUtils.ApplyTransparencyGuide(this.m_Palette, null);
            if (vdxBytes != null)
            {
                Boolean noFirstFrame;
                List<SupportedFileType> framesList = this.BuildAnimationFromChunks(vdaName, vdxBytes, this.m_FramesList, framesXY, initialFrameData, out noFirstFrame, false);
                this.NoFirstFrame = noFirstFrame;
                this.m_FramesList = framesList;
            }
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
                    vdxBytes = File.ReadAllBytes(vdxName);
                }
                else if (isVdx)
                {
                    vdaName = vdaNm;
                    vdaBytes = File.ReadAllBytes(vdaNm);
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
            noFirstFrame = false;
            List<SupportedFileType> framesList = new List<SupportedFileType>();
            Int32 offset = 0;
            Int32 imageWidth = 320;
            Int32 imageHeight = 200;
            Int32 imageStride = 320;
            Int32 arraySize = imageWidth * imageHeight;
            if (initialFrameData != null && initialFrameData.Length != arraySize)
                throw new FileTypeLoadException("Bad start frame data length!");
            Byte[] imageData = initialFrameData == null ? null : initialFrameData.ToArray();
            Boolean[] transMask = this.CreateTransparencyMask();
            Int32 chunks = 0;
            while (offset + 2 <= framesInfo.Length)
            {
                UInt16 curVal = (UInt16)ArrayUtils.ReadIntFromByteArray(framesInfo, offset, 2, true);
                if (curVal == 0xFFFE)
                    break;
                if (curVal == 0xFFFF)
                {
                    // No chunks at all specified for the very first frame. Could happen in a continued animation I guess?
                    if (imageData == null)
                    {
                        noFirstFrame = true;
                        if (testFirstFrame)
                            return null;
                        imageData = Enumerable.Repeat(TransparentIndex, arraySize).ToArray();
                    }
                    if (testFirstFrame)
                        return null;
                    if (noFirstFrame && framesList.Count == 0)
                    {
                        PaletteUtils.ApplyTransparencyGuide(this.m_Palette, transMask);
                    }
                    Bitmap curImage = ImageUtils.BuildImage(imageData, imageWidth, imageHeight, imageStride, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(this, this, curImage, sourcePath, framesList.Count);
                    frame.SetColorsInPalette(this.m_PaletteSet ? this.m_Palette.Length : 0);
                    frame.SetTransparencyMask(noFirstFrame ? transMask : null);
                    frame.SetColors(this.m_Palette, this);
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
                    Byte[] currentFrameData = ImageUtils.GetImageData(currentImage, out stride);
                    currentFrameData = ImageUtils.CollapseStride(currentFrameData, width, height, 8, ref stride);
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
                        ImageUtils.PasteOn8bpp(imageData, imageWidth, imageHeight, imageStride, currentFrameData, width, height, stride, new Rectangle(xOffset, yOffset, width, height), transMask, true);
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
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                if (fileToSave.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8-bit images!");
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            else
            {
                if (fileToSave.Frames.Length == 0)
                    throw new NotSupportedException("This format needs at least one frame.");
                if (fileToSave.Frames.Any(x => x.BitsPerPixel != 8))
                    throw new NotSupportedException("All frames in the target file must be 8-bit images!");
            }
            foreach(SupportedFileType sft in fileToSave.Frames)
                if (sft.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8bpp images.");
            Int32 firstFrameW = fileToSave.Frames[0].Width;
            Int32 firstFrameH = fileToSave.Frames[0].Height;
            foreach (SupportedFileType frame in fileToSave.Frames.Skip(1))
            {
                if (frame.Width != firstFrameW || frame.Height != firstFrameH)
                    throw new NotSupportedException("All frames should have the same dimensions.");
            }
            Int32 compression = 0;
            FileFramesMythosVgs fileVgs = fileToSave as FileFramesMythosVgs;
            if (fileVgs != null)
                compression = fileVgs.CompressionType;
            if (compression < 0 || compression > this.compressionTypes.Length)
                compression = 0;
            return new SaveOption[]
            {
                new SaveOption("OPT", SaveOptionType.ChoicesList, "Optimisation:", "Save simple cropped diff frames,Optimise to chunks (grouped per rectangle),Optimise to chunks (no grouping)", "1"),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString()),
                new SaveOption("CUT", SaveOptionType.Boolean, "Leave off the first frame (difference frames only)", "0"),
            };
        }

        private enum OptimiseMethods
        {
            DiffFrames = 0,
            ChunksGrouped = 1,
            ChunksFragmented = 2,
            // not yet implemented
            ChunksBordered = 3
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
                vdaName = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".VDA");
                vdxName = savePath;
            }
            else // No explicit check on VDA.
            {
                vdaName = savePath;
                vdxName = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".VDX");
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
            return SaveToBytesAsThis(fileToSave, saveOptions, out vdxFile);
        }

        public Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, out Byte[] vdxFile)
        {
            // todo check on frame count
            if (!fileToSave.IsFramesContainer || fileToSave.Frames == null)
            {
                if (fileToSave.BitsPerPixel != 8)
                    throw new NotSupportedException("This format needs 8-bit images!");
                FileFrames frameSave = new FileFrames();
                frameSave.AddFrame(fileToSave);
                fileToSave = frameSave;
            }
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");
            if (fileToSave.Frames.Any(x => x.BitsPerPixel != 8))
                throw new NotSupportedException("All frames in the target file must be 8-bit images!");

            OptimiseMethods optimisation = (OptimiseMethods)Int32.Parse(SaveOption.GetSaveOptionValue(saveOptions, "OPT"));
            Boolean cutfirstFrame = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "CUT"));
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);
            if (compressionType < 0 || compressionType > 2)
                compressionType = 0;
            Bitmap origImage = fileToSave.Frames[0].GetBitmap();
            Int32 origWidth = origImage.Width;
            Int32 origHeight = origImage.Height;
            Int32 fullImageStride;
            Byte[] previousImageData = ImageUtils.GetImageData(origImage, out fullImageStride);
            Int32 previousImageWidth = origWidth;
            Int32 previousImageHeight = origHeight;
            Int32 previousImageStride = fullImageStride;
            List<List<VideoChunk>> frames = new List<List<VideoChunk>>();

            Int32 totalChunks = 0;
            if (!cutfirstFrame)
            {
                VideoChunk chunk = new VideoChunk(previousImageData, new Rectangle(0, 0, origWidth, origHeight));
                frames.Add(new List<VideoChunk>() { chunk });
                totalChunks++;
            }
            foreach (SupportedFileType frame in fileToSave.Frames.Skip(1))
            {
                Int32 stride;
                Bitmap currentImage = frame.GetBitmap();
                Int32 width = currentImage.Width;
                Int32 height = currentImage.Height;
                Byte[] imageData = ImageUtils.GetImageData(currentImage, out stride);
                Byte[] imageDataOpt = imageData.ToArray();
                Int32 prevOffs = 0;
                Int32 frameOffs = 0;
                Int32 maxWidth = Math.Min(width, previousImageWidth);
                Int32 maxHeight = Math.Min(height, previousImageHeight);
                for (Int32 y = 0; y < maxHeight; y++)
                {
                    Int32 curFrameOffs = frameOffs;
                    Int32 curPrevOffs = prevOffs;
                    for (Int32 x = 0; x < maxWidth; x++)
                    {
                        if (imageData[curFrameOffs] == previousImageData[curPrevOffs])
                            imageDataOpt[curFrameOffs] = TransparentIndex;
                        curFrameOffs++;
                        curPrevOffs++;
                    }
                    frameOffs += stride;
                    prevOffs += previousImageStride;
                }
                if (optimisation == OptimiseMethods.DiffFrames)
                {
                    // optimize diff frame by cropping it.
                    Int32 xOffset = 0;
                    Int32 yOffset = 0;
                    Int32 newWidth = width;
                    Int32 newHeight = height;
                    imageDataOpt = ImageUtils.CollapseStride(imageDataOpt, newWidth, newHeight, 8, ref stride);
                    imageDataOpt = ImageUtils.OptimizeXWidth(imageDataOpt, ref newWidth, newHeight, ref xOffset, true, TransparentIndex, 0xFF, true);
                    imageDataOpt = ImageUtils.OptimizeYHeight(imageDataOpt, newWidth, ref newHeight, ref yOffset, true, TransparentIndex, 0xFFFF, true);
                    VideoChunk chunk = new VideoChunk(imageDataOpt, new Rectangle(xOffset, yOffset, newWidth, newHeight));
                    frames.Add(new List<VideoChunk>() { chunk });
                    totalChunks++;
                    if (totalChunks >= 0x7FFF)
                        throw new NotSupportedException("Chunk count exceeds " + 0x7FFF + "!");
                }
                else
                {
                    List<Boolean[,]> inBlobs;
                    List<List<Point>> blobs = ImageUtils.FindBlobs(imageDataOpt, width, height, (bytes, y, x) => bytes[y * stride + x] != TransparentIndex, true, true, out inBlobs);
                    if (optimisation == OptimiseMethods.ChunksGrouped)
                        ImageUtils.MergeBlobs(blobs, width, height, null, 0);

                    List<VideoChunk> frameChunks = new List<VideoChunk>();
                    for (Int32 i = 0; i < blobs.Count; i++)
                    {
                        List<Point> blob = blobs[i];
                        Boolean[,] inBlob = inBlobs[i];
                        Rectangle rect = ImageUtils.GetBlobBounds(blob);
                        Byte[] img = ImageUtils.CopyFrom8bpp(imageDataOpt, width, height, stride, rect);
                        if (optimisation == OptimiseMethods.ChunksFragmented)
                        {
                            // Remove pixels from the rectangle that are not part of the chunk.
                            Int32 lineIndex = 0;
                            Int32 maxH = rect.Y + rect.Height;
                            Int32 maxW = rect.X + rect.Width;
                            for (Int32 y = rect.Y; y < maxH; y++)
                            {
                                Int32 byteIndex = lineIndex;
                                for (Int32 x = rect.X; x < maxW; x++)
                                {
                                    if (!inBlob[y, x])
                                        img[byteIndex] = TransparentIndex;
                                    byteIndex++;
                                }
                                lineIndex += rect.Width;
                            }
                        }
                        VideoChunk chunk = new VideoChunk(img, rect);
                        frameChunks.Add(chunk);
                        totalChunks++;
                        if (totalChunks >= 0x7FFF)
                            throw new NotSupportedException("Chunk count exceeds " + 0x7FFF + "!");
                    }
                    frames.Add(frameChunks);
                }
                previousImageData = imageData;
                previousImageWidth = width;
                previousImageHeight = height;
                previousImageStride = stride;
            }
            // Add unique chunks to a single list, and add all rects used for each unique chunk to the rect.
            List<VideoChunk> chunks = new List<VideoChunk>();
            List<List<Rectangle>> allImageRects = new List<List<Rectangle>>();
            foreach (List<VideoChunk> frameChunks in frames)
            {
                foreach (VideoChunk frameChunk in frameChunks)
                {
                    Int32[] found = Enumerable.Range(0, chunks.Count).Where(c => frameChunk.Equals(chunks[c])).ToArray();
                    if (found.Length > 0)
                    {
                        Int32 index = found[0];
                        VideoChunk foundChunk = chunks[index];
                        allImageRects[index].Add(frameChunk.ImageRect);
                        frameChunk.FinalIndex = index;
                    }
                    else
                    {
                        // copy chunk! Otherwise later messing with the ImageRect will modify one of the frames.
                        VideoChunk finalFrameChunk = new VideoChunk(frameChunk.ImageData, frameChunk.ImageRect);
                        frameChunk.FinalIndex = chunks.Count;
                        chunks.Add(finalFrameChunk);
                        allImageRects.Add(new List<Rectangle>() { finalFrameChunk.ImageRect });
                    }
                    // clear this so it can get cleaned up. It's no longer needed anyway; the reference to the final frame is set.
                    frameChunk.ImageData = null;
                }
            }
            // Set ImageRect to the most occurring image rect in the group. This minimises the use of the 3-byte offset-reassigning command in the vdx file.
            for (Int32 i = 0; i < chunks.Count; i++)
                chunks[i].ImageRect = allImageRects[i].GroupBy(r => r).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            List<UInt16> writeValues = new List<UInt16>();
            foreach (List<VideoChunk> frameChunks in frames)
            {
                foreach (VideoChunk frameChunk in frameChunks)
                {
                    UInt16 index = (UInt16)frameChunk.FinalIndex;
                    VideoChunk baseChunk = chunks[index];
                    if (baseChunk.ImageRect == frameChunk.ImageRect)
                        writeValues.Add(index);
                    else
                    {
                        writeValues.Add((UInt16)(index | 0x8000));
                        writeValues.Add((UInt16)(frameChunk.ImageRect.X));
                        writeValues.Add((UInt16)(frameChunk.ImageRect.Y));
                    }
                }
                writeValues.Add(0xFFFF);
            }
            writeValues.Add(0xFFFE);

            vdxFile = new Byte[writeValues.Count * 2];
            for (Int32 i = 0; i < writeValues.Count; i++)
                ArrayUtils.WriteIntToByteArray(vdxFile, i << 1, 2, true, writeValues[i]);
            // Compress chunks
            foreach (VideoChunk chunk in chunks)
            {
                Byte[] compressedBytes = null;
                if (compressionType > 0)
                {
                    MythosCompression mc = new MythosCompression();
                    if (compressionType == 1)
                        compressedBytes = mc.FlagRleEncode(chunk.ImageData, 0xFE, chunk.ImageRect.Width, 8);
                    else if (compressionType == 2)
                        compressedBytes = mc.CollapsedTransparencyEncode(chunk.ImageData, TransparentIndex, chunk.ImageRect.Width, 8);
                }
                if (compressedBytes != null && compressedBytes.Length < chunk.ImageData.Length)
                {
                    chunk.ImageData = compressedBytes;
                    chunk.Compressed = true;
                }
            }
            // Add palette
            FileFramesMythosPal pal = new FileFramesMythosPal();
            Byte[] palData = pal.SaveToBytesAsThis(fileToSave, null);

            // Full length: headers and data for all chunks.
            Int32 fullLength = palData.Length + chunks.Count * 0x08 + chunks.Sum(x => x.ImageData.Length);
            Byte[] vdmFile = new Byte[fullLength];
            Array.Copy(palData, 0, vdmFile, 0, palData.Length);
            Int32 offset = palData.Length;
            foreach (VideoChunk chunk in chunks)
            {
                ArrayUtils.WriteIntToByteArray(vdmFile, offset + 0, 2, true, (UInt16) (chunk.ImageRect.Width - 1));
                ArrayUtils.WriteIntToByteArray(vdmFile, offset + 2, 2, true, (UInt16) (chunk.ImageRect.Height - 1));
                vdmFile[offset + 4] = (Byte) (chunk.Compressed ? 0x02 : 0x00);
                ArrayUtils.WriteIntToByteArray(vdmFile, offset + 5, 2, true, (UInt16)(chunk.ImageRect.X));
                vdmFile[offset + 7] = (Byte) (chunk.ImageRect.Y & 0xFF);
                offset += 8;
                Byte[] chunkData = chunk.ImageData;
                Int32 dataLen = chunkData.Length;
                Array.Copy(chunkData, 0, vdmFile, offset, dataLen);
                offset += dataLen;
            }
            return vdmFile;
        }

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
                Byte[] imageBytes = new Byte[ImageData.Length + 8];
                ArrayUtils.WriteIntToByteArray(imageBytes, 0, 4, true, (UInt32) ImageRect.Width);
                ArrayUtils.WriteIntToByteArray(imageBytes, 4, 4, true, (UInt32) ImageRect.Height);
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