using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFrames : FileImage
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | base.FileClass; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }

        public override Int32 Width { get { return this.m_LoadedImage == null ? this.CheckCommonWidth() : this.m_LoadedImage.Width; } }
        public override Int32 Height { get { return this.m_LoadedImage == null ? this.CheckCommonHeight() : this.m_LoadedImage.Height; } }

        public override String ShortTypeName { get { return "Frames"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String LongTypeName { get { return (this.BaseType == null ? String.Empty : this.BaseType + " ") + "Frames"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[0]; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return null; } }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new FileTypeSaveException("This is not a real file format to save. How did you even get here?");
        }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette.</summary>
        public override Boolean FramesHaveCommonPalette { get { return this.m_CommonPalette; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return this.m_LoadedImage != null; } }
        public override Int32 BitsPerPixel { get { return this.m_BitsPerPixel != -1 ? this.m_BitsPerPixel : base.BitsPerPixel; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return this.m_TransparencyMask; } }

        /// <summary>Amount of colors in the palette that is contained inside the image. 0 if the image itself does not contain a palette, even if it generates one.</summary>
        public override Boolean NeedsPalette { get { return this.m_NeedsPalette; } }

        /// <summary>
        /// Avoid using this for adding frames: use AddFrame instead.
        /// </summary>
        public List<SupportedFileType> FramesList { get; private set; }

        public String BaseType { get; private set; }
        public Type EmbeddedType { get; private set; }
        public Boolean FromFileRange { get; private set; }


        /// <summary>Creates a new FileFrames object</summary>
        public FileFrames()
        {
            FramesList = new List<SupportedFileType>();
        }

        /// <summary>Creates a new FileFrames object</summary>
        /// <param name="fromFileRange">Sets whether this file was created from a range of files.</param>
        public FileFrames(Boolean fromFileRange)
            : this()
        {
            this.FromFileRange = fromFileRange;
        }

        /// <summary>Creates a new FileFrames object</summary>
        /// <param name="framesSource">Source of the frames. Giving this does not copy any frames, it just inherits the "from file range" status.</param>
        public FileFrames(SupportedFileType framesSource)
            : this()
        {
            FileFrames framesFile = framesSource as FileFrames;
            this.FromFileRange = framesFile != null && framesFile.FromFileRange;
        }

        protected Boolean m_CommonPalette;
        protected Boolean m_NeedsPalette;
        protected FileClass m_InputFileClass = FileClass.None;
        protected Int32 m_BitsPerPixel;
        protected Boolean[] m_TransparencyMask;

        private Int32 CheckCommonWidth()
        {
            Int32 nrOfFrames = this.FramesList.Count;
            Int32 width = 0;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType fr = this.FramesList[i];
                Bitmap bm = fr.GetBitmap();
                if (bm == null)
                    return 0;
                if (width == 0)
                    width = bm.Width;
                else if (width != bm.Width)
                    return 0;
            }
            return width;
        }

        private Int32 CheckCommonHeight()
        {
            Int32 nrOfFrames = this.FramesList.Count;
            Int32 height = 0;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType fr = this.FramesList[i];
                Bitmap bm = fr.GetBitmap();
                if (bm == null)
                    return 0;
                if (height == 0)
                    height = bm.Height;
                else if (height != bm.Height)
                    return 0;
            }
            return height;
        }

        /// <summary>
        /// Adds a frame to the list, setting its FrameParent property to this object.
        /// </summary>
        /// <param name="frame">Frame to add.</param>
        public void AddFrame(SupportedFileType frame)
        {
            frame.FrameParent = this;
            this.FramesList.Add(frame);
        }

        public void SetCompositeFrame(Bitmap compositeBitmap)
        {
            this.m_LoadedImage = compositeBitmap;
        }

        public void SetCommonPalette(Boolean commonPalette)
        {
            this.m_CommonPalette = commonPalette;
        }

        public void SetBitsPerPixel(Int32 bitsPerColor)
        {
            this.m_BitsPerPixel = bitsPerColor;
        }

        public void SetNeedsPalette(Boolean needsPalette)
        {
            this.m_NeedsPalette = needsPalette;
        }

        public void SetPalette(Color[] palette)
        {
            this.m_Palette = palette;
        }

        public void SetTransparencyMask(Boolean[] transparencyMask)
        {
            this.m_TransparencyMask = transparencyMask;
        }

        public void SetFrameInputClass(FileClass supportedFrameTypes)
        {
            this.m_InputFileClass = supportedFrameTypes;
        }

        public void SetFrameInputClassFromBpp(Int32 bpp)
        {
            switch (bpp)
            {
                case 1: this.m_InputFileClass = FileClass.Image1Bit; break;
                case 4: this.m_InputFileClass = FileClass.Image4Bit; break;
                case 8: this.m_InputFileClass = FileClass.Image8Bit; break;
                default: this.m_InputFileClass = FileClass.ImageHiCol; break;
            }
        }

        public static String[] GetFrameFilesRange(String path, out String baseName)
        {
            baseName = path;
            String ext = Path.GetExtension(path);
            String folder = Path.GetDirectoryName(path);
            String name = Path.GetFileName(path);
            Regex framesCheck = new Regex("^(.*?)(\\d+)" + Regex.Escape(ext) + "$");
            Match m = framesCheck.Match(name);
            if (!m.Success)
                return null;
            String namepart = m.Groups[1].Value;
            String numpart = m.Groups[2].Value;
            String numpartFormat = "D" + numpart.Length;
            UInt64 filenum;
            try
            {
                filenum = UInt64.Parse(numpart);
            }
            catch (OverflowException)
            {
                return null;
            }
            UInt64 num = filenum;
            UInt64 minNum = filenum;
            while (File.Exists(Path.Combine(folder, namepart + num.ToString(numpartFormat) + ext)))
            {
                minNum = num;
                if (num == 0)
                    break;
                num--;
            }
            num = filenum;
            UInt64 maxNum = filenum;
            while (File.Exists(Path.Combine(folder, namepart + num.ToString(numpartFormat) + ext)))
            {
                maxNum = num;
                if (num == UInt64.MaxValue)
                    break;
                num++;
            }
            // Only one frame; not a range. Abort.
            if (maxNum == minNum)
                return null;
            String frName = namepart;
            if (frName.Length == 0)
            {
                String minNameStr = minNum.ToString(numpartFormat);
                String maxNameStr = maxNum.ToString(numpartFormat);
                Int32 index = 0;
                while (index < minNameStr.Length && minNameStr[index] == maxNameStr[index])
                    index++;
                frName = minNameStr.Substring(0, index);
            }
            else if (frName.EndsWith("-") && frName.Length > 1)
                frName = frName.Substring(0, frName.Length - 1);
            frName = frName.Trim();
            if (frName.Length == 0)
                frName = new String(Enumerable.Repeat('#', numpartFormat.Length).ToArray());
            baseName = Path.Combine(folder, frName + ext);
            UInt64 fullRange = maxNum - minNum + 1;
            String[] allNames = new String[fullRange];
            for (UInt64 i = 0; i < fullRange; ++i)
                allNames[i] = Path.Combine(folder, namepart + (minNum + i).ToString(numpartFormat) + ext);
            return allNames;
        }

        public static FileFrames CheckForFrames(String path, SupportedFileType currentType, out String minName, out String maxName, out Boolean hasEmptyFrames)
        {
            String baseName;
            minName = null;
            maxName = null;
            hasEmptyFrames = false;
            String[] frameNames = GetFrameFilesRange(path, out baseName);
            // No file or only one file; not a range. Abort.
            Int32 nrOfFrames;
            if (frameNames == null || (nrOfFrames = frameNames.Length) == 1)
                return null;
            if (currentType != null && currentType.IsFramesContainer)
                return null;
            minName = Path.GetFileName(frameNames[0]);
            maxName = Path.GetFileName(frameNames[nrOfFrames - 1]);

            FileFrames framesContainer = new FileFrames(true);
            framesContainer.SetFileNames(baseName);
            if (currentType == null)
            {
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    String framePath = frameNames[i];
                    if (new FileInfo(framePath).Length == 0)
                        continue;
                    SupportedFileType[] possibleTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(FileTypesFactory.AutoDetectTypes, framePath);
                    List<FileTypeLoadException> loadErrors;
                    currentType = FileTypesFactory.LoadFileAutodetect(framePath, possibleTypes, false, out loadErrors);
                    break;
                }
                // All frames are empty. Not gonna support that.
                if (currentType == null)
                    return null;
            }
            framesContainer.BaseType = currentType.ShortTypeName;
            Type type = currentType.GetType();
            framesContainer.EmbeddedType = type;
            Color[] pal = currentType.GetColors();
            // 'common palette' logic is started by setting it to True when there is a palette.
            Boolean commonPalette = pal != null && !currentType.NeedsPalette;
            FileClass frameTypes = FileClass.None;
            Boolean nullPalette = currentType.NeedsPalette || pal == null;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                String currentFrame = frameNames[i];
                if (new FileInfo(currentFrame).Length == 0)
                {
                    hasEmptyFrames = true;
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(framesContainer, currentType, null, currentFrame, -1);
                    frame.SetBitsPerColor(currentType.BitsPerPixel);
                    frame.SetFileClass(currentType.FileClass);
                    frame.SetNeedsPalette(currentType.NeedsPalette);
                    frame.SetExtraInfo("Empty file.");
                    framesContainer.AddFrame(frame);
                    continue;
                }
                try
                {
                    SupportedFileType frameFile = (SupportedFileType)Activator.CreateInstance(type);
                    Byte[] fileData = File.ReadAllBytes(currentFrame);
                    frameFile.LoadFile(fileData, currentFrame);
                    framesContainer.AddFrame(frameFile);
                    if (commonPalette)
                        commonPalette = frameFile.GetColors() != null && !frameFile.NeedsPalette && pal.SequenceEqual(frameFile.GetColors());
                    if (nullPalette)
                        nullPalette = frameFile.NeedsPalette;
                    frameTypes |= currentType.FileClass;
                }
                catch (FileTypeLoadException)
                {
                    // One of the files in the sequence cannot be loaded as the same type. Abort.
                    return null;
                }
            }
            framesContainer.SetCommonPalette(commonPalette || nullPalette);
            framesContainer.SetTransparencyMask(currentType.TransparencyMask);
            framesContainer.SetFrameInputClass(frameTypes);
            if (framesContainer.FramesHaveCommonPalette)
            {
                framesContainer.SetBitsPerPixel(currentType.BitsPerPixel);
                framesContainer.SetNeedsPalette(currentType.NeedsPalette);
                // Ensures the correct amount of colors is set for the container
                framesContainer.SetPalette(currentType.GetColors());
                framesContainer.SetColors(currentType.GetColors());
            }
            return framesContainer;
        }

        /// <summary>
        /// Pastes an image on a range of frames. Supports all pixel format combinations.
        /// If both the image to paste and the frame are indexed, and the bpp of the frame is at
        /// least as high as that of the paste image, then no palette matching will be performed.
        /// </summary>
        /// <param name="framesContainer">SupportedFileType object containing frames.</param>
        /// <param name="image">Image to paste onto the frames.</param>
        /// <param name="pasteLocation">Point at which to paste the image.</param>
        /// <param name="framesRange">Arra containing the indices to paste the image on.</param>
        /// <param name="keepIndices">If all involved images are indexed, and no overflow can occur, paste bare data indices when handling indexed types rather than matching image colors to a palette.</param>
        /// <returns>A new FileFrames object containing the edited frames.</returns>
        public static SupportedFileType PasteImageOnFrames(SupportedFileType framesContainer, Bitmap image, Point pasteLocation, Int32[] framesRange, Boolean keepIndices)
        {
            Boolean singleImage = (framesContainer.Frames == null || framesContainer.Frames.Length == 0) && framesContainer.GetBitmap() != null;
            Int32 pasteBpp = Image.GetPixelFormatSize(image.PixelFormat);
            if (pasteBpp > 8)
                pasteBpp = 32;
            Color[] imPalette = pasteBpp > 8 ? null : image.Palette.Entries;
            Boolean[] imPalTrans = imPalette == null ? null : imPalette.Select(c => c.A == 0).ToArray();
            Boolean[] imTransMask = null;
            Int32 imWidth = image.Width;
            Int32 imHeight = image.Height;
            Byte[] imData = null;
            Int32 imStride = imWidth;
            // check if all frames have the same palette.
            Boolean equalPal = singleImage || framesContainer.FramesHaveCommonPalette;
            Color[] framePal = null;
            Int32 frameBpp = 0;
            SupportedFileType[] frames = singleImage ? new SupportedFileType[] { framesContainer } : framesContainer.Frames;
            Int32 nrOfFrames = frames.Length;
            // Explicitly test if all frames have the same color depth and palette.
            if (!equalPal)
            {
                Boolean isEqual = true;
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    SupportedFileType frame = frames[i];
                    // Skip empty frames.
                    if (frame == null || frame.GetBitmap() == null)
                        continue;
                    Int32 curFrameBpp = frame.BitsPerPixel;
                    if (curFrameBpp > 8)
                    {
                        isEqual = false;
                        break;
                    }
                    if (frameBpp == 0)
                        frameBpp = curFrameBpp;
                    else if (curFrameBpp != frameBpp)
                    {
                        isEqual = false;
                        break;
                    }
                    if (framePal == null)
                        framePal = frame.GetColors();
                    else
                    {
                        Color[] curFrPal = frame.GetColors();
                        if (PaletteUtils.PalettesAreEqual(framePal, curFrPal, true))
                            continue;
                        isEqual = false;
                        break;
                    }
                }
                if (isEqual)
                    equalPal = true;
            }
            else
                framePal = framesContainer.GetColors();
            if (!equalPal)
            {
                framePal = null;
                frameBpp = 0;
            }
            Rectangle pastePos = new Rectangle(pasteLocation, new Size(imWidth, imHeight));
            String name = String.Empty;
            if (framesContainer.LoadedFile != null)
                name = framesContainer.LoadedFile;
            else if (framesContainer.LoadedFileName != null)
                name = framesContainer.LoadedFileName;
            FileFrames newfile = null;
            if (!singleImage)
            {
                newfile = new FileFrames(framesContainer);
                newfile.SetFileNames(name);
                newfile.SetCommonPalette(equalPal);
                newfile.SetBitsPerPixel(framesContainer.BitsPerPixel);
                newfile.SetNeedsPalette(framesContainer.NeedsPalette);
                newfile.SetPalette(equalPal ? framePal : null);
                Boolean[] transMask = framesContainer.TransparencyMask == null ? null : ArrayUtils.CloneArray(framesContainer.TransparencyMask);
                newfile.SetTransparencyMask(transMask);
            }
            framesRange = framesRange.Distinct().OrderBy(x => x).ToArray();
            Int32 framesToHandle = framesRange.Length;
            Int32 nextPasteFrameIndex = 0;

            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null)
                {
                    newfile.AddFrame(null);
                    continue;
                }
                Bitmap frBm = frame.GetBitmap();
                Bitmap newBm;
                // List is sorted. This is more efficient than "contains" every time.
                if (nextPasteFrameIndex < framesToHandle && i == framesRange[nextPasteFrameIndex] && frBm != null)
                {
                    Int32 curFrameBpp = Image.GetPixelFormatSize(frBm.PixelFormat);
                    nextPasteFrameIndex++;
                    Int32 frWidth = frBm.Width;
                    Int32 frHeight = frBm.Height;

                    if ((frBm.PixelFormat & PixelFormat.Indexed) == 0)
                    {
                        Bitmap tempBm = new Bitmap(frBm);
                        using (Graphics g = Graphics.FromImage(tempBm))
                            g.DrawImage(image, pastePos);
                        if (frBm.PixelFormat != PixelFormat.Format32bppArgb)
                        {
                            Byte[] drawBytes = ImageUtils.GetImageData(tempBm, out imStride, frBm.PixelFormat);
                            newBm = ImageUtils.BuildImage(drawBytes, frWidth, frHeight, imStride, frBm.PixelFormat, null, null);
                            tempBm.Dispose();
                        }
                        else
                        {
                            newBm = tempBm;
                        }
                    }
                    else
                    {
                        Color[] frPalette = frBm.Palette.Entries;
                        Int32 frBpp = Image.GetPixelFormatSize(frBm.PixelFormat);
                        Int32 frStride;
                        Byte[] frData = ImageUtils.GetImageData(frBm, out frStride);
                        if (frBpp != 8)
                            frData = ImageUtils.ConvertTo8Bit(frData, frWidth, frHeight, 0, frBpp, true, ref frStride);
                        // determine whether the image to paste needs to be re-matched to the palette.
                        Boolean regenImage = false;
                        if (imData == null)
                            regenImage = true;
                        else if (!equalPal || curFrameBpp != frameBpp)
                        {
                            if (framePal == null)
                            {
                                regenImage = true;
                                framePal = frPalette;
                            }
                            else
                            {
                                regenImage = !PaletteUtils.PalettesAreEqual(framePal, frPalette, true);
                                if (regenImage)
                                    framePal = frame.GetColors();
                            }
                            if (curFrameBpp != frameBpp)
                            {
                                regenImage = true;
                            }
                        }
                        Boolean[] transGuide = null;
                        if (pasteBpp <= 8)
                        {
                            Boolean keepInd = keepIndices && pasteBpp <= frBpp;
                            if (regenImage)
                            {
                                transGuide = frPalette.Select(col => col.A != 0xFF).ToArray();
                                imData = ImageUtils.GetImageData(image, out imStride);
                                imData = ImageUtils.ConvertTo8Bit(imData, imWidth, imHeight, 0, pasteBpp, true, ref imStride);
                                if (!keepInd)
                                {
                                    imTransMask = imData.Select(px => imPalTrans[px]).ToArray();
                                    imData = ImageUtils.Match8BitDataToPalette(imData, imPalette, frPalette);
                                }
                            }
                            if (keepInd)
                                transGuide = imPalTrans;
                        }
                        else
                        {
                            if (regenImage)
                            {
                                imData = ImageUtils.GetImageData(image, out imStride, PixelFormat.Format32bppArgb);
                                // Create transparency mask to determine which pieces on the image are transparent and should be ignored for the paste.
                                Color[] palTrans = new Color[] { Color.Transparent, Color.Gray };
                                Int32 maskStride = imStride;
                                Byte[] transMask1 = ImageUtils.Convert32BitToPaletted(imData, imWidth, imHeight, 8, true, palTrans, ref maskStride);
                                imTransMask = transMask1.Select(b => b == 0).ToArray();
                                // Get actual image data
                                imData = ImageUtils.Convert32BitToPaletted(imData, imWidth, imHeight, 8, true, frPalette, ref imStride);
                            }
                        }
                        // Paste using the transparency image mask.
                        frData = ImageUtils.PasteOn8bpp(frData, frWidth, frHeight, frStride, imData, imWidth, imHeight, imStride, pastePos, transGuide, true, imTransMask);
                        frData = ImageUtils.ConvertFrom8Bit(frData, frWidth, frHeight, frBpp, true, ref frStride);
                        newBm = ImageUtils.BuildImage(frData, frWidth, frHeight, frStride, ImageUtils.GetIndexedPixelFormat(frBpp), frPalette, null);
                    }
                    frameBpp = curFrameBpp;
                }
                else
                {
                    newBm = frBm == null ? null : ImageUtils.CloneImage(frBm);
                }
                // single image.
                if (newfile == null)
                {
                    FileImagePng result = new FileImagePng();
                    result.LoadFile(newBm, name);
                    return result;
                }
                FileImageFrame frameCombined = new FileImageFrame();
                frameCombined.LoadFileFrame(newfile, frame.LongTypeName, newBm, name, i);
                frameCombined.SetBitsPerColor(frame.BitsPerPixel);
                frameCombined.SetFileClass(frame.FileClass);
                frameCombined.SetNeedsPalette(frame.NeedsPalette);
                frameCombined.SetExtraInfo(frame.ExtraInfo);
                newfile.AddFrame(frameCombined);
            }
            return newfile;
        }

        /// <summary>
        /// Cuts an image into frames and returns it as <see cref="FileFrames"/> object.
        /// </summary>
        /// <param name="image">Source image.</param>
        /// <param name="imagePath">Path the image was loaded from, to set the frame names.</param>
        /// <param name="frameWidth">Width of the cut out frames.</param>
        /// <param name="frameHeight">Height of the cut out frames.</param>
        /// <param name="frames">Upper limit to the amount of frames to generate.</param>
        /// <param name="cropColor">Color to trim away for cropping frames, if the source is high-color.</param>
        /// <param name="cropIndex">Color index to trim away for cropping frames, if the source is indexed.</param>
        /// <param name="matchBpp">Bits per pixel for the palette to match. 0 for no palette matching.</param>
        /// <param name="matchPalette">Palette to match. Only used if <see cref="matchBpp"/> is not 0.</param>
        /// <param name="cloneSource">True to clone the source image, to prevent conflicts in multithreaded use.</param>
        /// <param name="needsPalette">True to mark the frames object and its frame as needing an external palette.</param>
        /// <returns>A <see cref="FileFrames"/> object that contains the cut-out frames.</returns>
        public static FileFrames CutImageIntoFrames(Bitmap image, String imagePath, Int32 frameWidth, Int32 frameHeight, Int32 frames, Color? cropColor, Int32? cropIndex, Int32 matchBpp, Color[] matchPalette, Boolean cloneSource, Boolean needsPalette)
        {
            Bitmap editImage = cloneSource ? ImageUtils.CloneImage(image) : image;
            Bitmap[] framesArr = ImageUtils.ImageToFrames(editImage, frameWidth, frameHeight, cropColor, cropIndex, matchBpp, matchPalette, 0, frames - 1);
            if (cloneSource)
                editImage.Dispose();
            Boolean isMatched = matchBpp > 0 && matchBpp <= 8 && matchPalette != null;
            Int32 bpp = isMatched ? matchBpp : Image.GetPixelFormatSize(image.PixelFormat);
            Color[] imPalette = isMatched ? matchPalette : bpp > 8 ? null : image.Palette.Entries;
            Boolean indexed = isMatched || bpp <= 8;
            FileFrames newfile = new FileFrames();
            newfile.SetFileNames(imagePath);
            newfile.SetCommonPalette(indexed);
            newfile.SetNeedsPalette(indexed && needsPalette);
            newfile.SetBitsPerPixel(bpp);
            newfile.SetFrameInputClassFromBpp(bpp);
            newfile.SetPalette(imPalette);
            newfile.SetTransparencyMask(null);
            for (Int32 i = 0; i < framesArr.Length; ++i)
            {
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(newfile, newfile, framesArr[i], imagePath, i);
                framePic.SetBitsPerColor(bpp);
                framePic.SetNeedsPalette(indexed && needsPalette);
                newfile.AddFrame(framePic);
            }
            if (indexed)
                newfile.SetColors(imPalette);
            return newfile;
        }

        public static Int32[][] CheckForMaskFrames(SupportedFileType input, out Int32 srcTransIndex)
        {
            Dictionary<UInt16, List<Int32>> twoColImages = new Dictionary<UInt16, List<Int32>>();
            Int32[] imageFrames;
            Int32[] maskFrames;
            HashSet<Byte> foundColors = new HashSet<Byte>();
            SupportedFileType[] frames = input.Frames;
            Int32 nrOfFrames = frames.Length;
            Byte[][] frameData = new Byte[nrOfFrames][];
            Boolean sameFrameSizes = true;
            Int32 prevWidth = -1;
            Int32 prevHeight = -1;
            for (Int32 i = 0; i < nrOfFrames; i++)
            {
                SupportedFileType frame = frames[i];
                if (sameFrameSizes)
                {
                    if (prevWidth != -1 && prevHeight != -1 && (frame.Width != prevWidth || frame.Height != prevHeight))
                        sameFrameSizes = false;
                    prevWidth = frame.Width;
                    prevHeight = frame.Height;
                }
                if (frame.BitsPerPixel > 8)
                    throw new ArgumentException("All frames need to be indexed for this conversion.", "input");
                Byte[] imageData = ImageUtils.GetImageData(frame.GetBitmap(), PixelFormat.Format8bppIndexed);
                frameData[i] = imageData;
                foundColors.Clear();
                for (Int32 j = 0; j < imageData.Length; ++j)
                {
                    if (foundColors.Contains(imageData[i]))
                        continue;
                    foundColors.Add(imageData[i]);
                    if (foundColors.Count > 2)
                        break;
                }
                if (foundColors.Count == 2)
                {
                    Byte[] cols = foundColors.ToArray();
                    Array.Sort(cols);
                    UInt16 keyVal = (UInt16)(cols[0] << 8 | cols[1]);
                    List<Int32> curFrames;
                    if (!twoColImages.TryGetValue(keyVal, out curFrames))
                    {
                        curFrames = new List<Int32>();
                        twoColImages[keyVal] = curFrames;
                    }
                    curFrames.Add(i);
                }
            }
            // Get the color pair for which the amount of found images is the largest.
            Int32 maxTwoCol = twoColImages.Select(x => x.Value.Count).Max();
            KeyValuePair<UInt16, List<Int32>> detectedColor = twoColImages.Where(x => x.Value.Count == maxTwoCol).First();

            List<Int32> maskFramesList = detectedColor.Value;
            maskFramesList.Sort();
            // List found. Check how it relates to other frames.
            Boolean isBefore = true;
            List<Int32> correspondingFramesBefore = new List<Int32>();
            Boolean isAfter = true;
            List<Int32> correspondingFramesAfter = new List<Int32>();
            List<Int32> correspondingFramesBeforeBatch = new List<Int32>();
            List<Int32> correspondingFramesAfterBatch = new List<Int32>();
            for (Int32 i = 0; i < maskFramesList.Count; ++i)
            {
                Int32 frameNr = maskFramesList[i];
                if (frameNr > 0 && !maskFramesList.Contains(frameNr - 1))
                {
                    correspondingFramesBefore.Add(frameNr);
                    continue;
                }
                isBefore = false;
                break;
            }
            for (Int32 i = maskFramesList.Count - 1; i >= 0; --i)
            {
                Int32 frameNr = maskFramesList[i];
                if (frameNr + 1 < maskFramesList.Count && !maskFramesList.Contains(frameNr + 1))
                {
                    correspondingFramesAfter.Add(frameNr);
                    continue;
                }
                isAfter = false;
                break;
            }
            // Try ranges
            Boolean includesFirst = false;
            Boolean includesLast = false;
            List<Int32[]> batches = new List<Int32[]>();
            for (Int32 i = 0; i < maskFramesList.Count; ++i)
            {
                List<Int32> batch = new List<Int32>();
                batch.Add(maskFramesList[i]);
                while (i + 1 < maskFramesList.Count && maskFramesList[i] + 1 == maskFramesList[i + 1])
                {
                    i++;
                    batch.Add(maskFramesList[i]);
                }
                batches.Add(batch.ToArray());
            }
            // Only check ranges if they aren't singular anyway.
            Boolean hasRanges = batches.Any(b => b.Length > 1);
            // Can't be 'before' the actual image frames it masks if it includes the last frame
            Boolean isBeforeBatch = hasRanges && !batches.Last().Contains(nrOfFrames - 1);
            // Can't be 'after' the actual image frames it masks if it includes the first frame
            Boolean isAfterBatch = hasRanges && !batches.First().Contains(0);
            if (isBeforeBatch)
            {
                for (Int32 i = 0; i < batches.Count; ++i)
                {
                    Int32[] batch = batches[i];
                    // Batches are never empty.
                    Int32 first = batch[0];
                    Int32 maskRangeAmount = batch.Length;
                    for (Int32 j = 0; j < maskRangeAmount; ++j)
                    {
                        Int32 frame = batch[j] - maskRangeAmount;
                        if (frame > 0 && !maskFramesList.Contains(frame))
                        {
                            correspondingFramesBeforeBatch.Add(frame);
                            continue;
                        }
                        isBeforeBatch = false;
                        break;
                    }
                    if (!isBeforeBatch)
                        break;
                }
            }
            if (isAfterBatch)
            {
                for (Int32 i = batches.Count - 1; i >= 0; --i)
                {
                    Int32[] batch = batches[i];
                    // Batches are never empty.
                    Int32 first = batch[0];
                    Int32 maskRangeAmount = batch.Length;
                    for (Int32 j = maskRangeAmount - 1; j >= 0; --j)
                    {
                        Int32 frame = batch[j] - maskRangeAmount;
                        if (frame > 0 && !maskFramesList.Contains(frame))
                        {
                            correspondingFramesAfterBatch.Add(frame);
                            continue;
                        }
                        isAfterBatch = false;
                        break;
                    }
                    if (!isAfterBatch)
                        break;
                }
            }
            // Now, check which of the detected choices is most likely. If multiple match, check image contents for overlap with different indices.
            // First check: see if frame sizes are the same. Ignore this if all sizes are the same.
            Int32[] correctFramesBefore = null;
            Int32[] correctFramesAfter = null;
            Int32[] correctFramesBatchBefore = null;
            Int32[] correctFramesBatchAfter = null;
            if (!sameFrameSizes)
            {
                Int32[] matchCount = new Int32[4];
                if (isBefore)
                {
                    correctFramesBefore = maskFramesList.Where(f => frames[maskFramesList[f]].Width == frames[correspondingFramesBefore[f]].Width
                                                                && frames[maskFramesList[f]].Height == frames[correspondingFramesBefore[f]].Height).ToArray();
                    if (correctFramesBefore.Length == 0)
                        isBefore = false;
                }
                if (isAfter)
                {
                    correctFramesAfter = maskFramesList.Where(f => frames[maskFramesList[f]].Width == frames[correspondingFramesAfter[f]].Width
                                                                && frames[maskFramesList[f]].Height == frames[correspondingFramesAfter[f]].Height).ToArray();
                    if (correctFramesAfter.Length == 0)
                        isAfter = false;
                }
                if (isBeforeBatch)
                {
                    correctFramesBatchBefore = maskFramesList.Where(f => frames[maskFramesList[f]].Width == frames[correspondingFramesBeforeBatch[f]].Width
                                                                && frames[maskFramesList[f]].Height == frames[correspondingFramesBeforeBatch[f]].Height).ToArray();
                    if (correctFramesBatchBefore.Length == 0)
                        isBeforeBatch = false;
                }
                if (isAfterBatch)
                {
                    correctFramesBatchAfter = maskFramesList.Where(f => frames[maskFramesList[f]].Width == frames[correspondingFramesAfterBatch[f]].Width
                                                                && frames[maskFramesList[f]].Height == frames[correspondingFramesAfterBatch[f]].Height).ToArray();
                    if (correctFramesBatchAfter.Length == 0)
                        isAfterBatch = false;
                }
            }
            // BIG FAT TODO
            imageFrames = new Int32[0];
            maskFrames = new Int32[0];
            srcTransIndex = 0;
            return new Int32[][] { imageFrames, maskFrames };
        }

        public static FileFrames ApplyTransparencyMask(SupportedFileType input, Int32[] imageFrames, Int32[] maskFrames, Int32 srcTransIndex, Int32 resTransIndex, Boolean keepOtherFrames)
        {
            if (imageFrames.Length != maskFrames.Length)
                throw new ArgumentException("Amount of mask frames does not equal amount of image frames.", "maskFrames");
            Array.Sort(imageFrames);
            Array.Sort(maskFrames);
            Int32[] origImageFrames = new Int32[imageFrames.Length];
            Array.Copy(imageFrames, origImageFrames, imageFrames.Length);
            Int32[] origMaskFrames = new Int32[maskFrames.Length];
            Array.Copy(maskFrames, origMaskFrames, maskFrames.Length);
            SupportedFileType[] imagesToProcess;
            SupportedFileType[] masksToProcess;
            if (keepOtherFrames)
            {
                imagesToProcess = input.Frames;
                masksToProcess = input.Frames;
            }
            else
            {
                imagesToProcess = new SupportedFileType[imageFrames.Length];
                masksToProcess = new SupportedFileType[maskFrames.Length];
                for (Int32 i = 0; i < imageFrames.Length; ++i)
                {
                    imagesToProcess[i] = input.Frames[imageFrames[i]];
                    masksToProcess[i] = input.Frames[maskFrames[i]];
                    imageFrames[i] = i;
                    maskFrames[i] = i;
                }
            }
            Bitmap[] outputBm = new Bitmap[imageFrames.Length];
            for (Int32 i = 0; i < imageFrames.Length; ++i)
            {
                SupportedFileType src = imagesToProcess[imageFrames[i]];
                SupportedFileType mask = masksToProcess[maskFrames[i]];
                Bitmap srcBm = src.GetBitmap();
                Bitmap maskBm = mask.GetBitmap();
                if (srcBm == null || srcBm == null)
                    throw new ArgumentException("Empty frames are not supported for this operation.", "input");
                Int32 frWidth = srcBm.Width;
                Int32 frHeight = srcBm.Height;
                if (frWidth != maskBm.Width || frHeight != maskBm.Height)
                    throw new ArgumentException(String.Format("Dimensions don't match on frame {0}, mask frame {1}.", origImageFrames[i], origMaskFrames[i]), "input");
                PixelFormat srcPf = srcBm.PixelFormat;
                PixelFormat maskPf = maskBm.PixelFormat;
                if (((srcPf | maskPf) & PixelFormat.Indexed) == 0)
                    throw new ArgumentException("All frames need to be indexed.", "input");
                Color[] pal = srcBm.Palette.Entries;
                //Boolean maskHiCol = (maskBm.PixelFormat | PixelFormat.Indexed) == 0;
                Byte[] imageData = ImageUtils.GetImageData(srcBm, PixelFormat.Format8bppIndexed);
                Byte[] maskData = ImageUtils.GetImageData(maskBm, PixelFormat.Format8bppIndexed);
                Int32 linePos = 0;
                Byte resTrans = (Byte)resTransIndex;
                for (int y = 0; y < frHeight; y++)
                {
                    Int32 pos = linePos;
                    for (int x = 0; x < frWidth; y++)
                    {
                        if (maskData[pos] == srcTransIndex)
                            imageData[pos] = resTrans;
                        pos++;
                    }
                    linePos += frWidth;
                }
                Int32 origBpp = Image.GetPixelFormatSize(srcPf);
                Int32 finalBpp = origBpp;
                while (origBpp < 8 && resTrans >= Math.Pow(2, finalBpp))
                {
                    finalBpp <<= 1;
                }
                if (finalBpp == 2)
                    finalBpp <<= 1;
                if (finalBpp < 8)
                    imageData = ImageUtils.ConvertFrom8Bit(imageData, frWidth, frHeight, finalBpp, true);
                PixelFormat resultFormat = ImageUtils.GetIndexedPixelFormat(finalBpp);
                for (Int32 c = 0; c < pal.Length; c++)
                    pal[c] = Color.FromArgb(c == resTransIndex ? 0 : 255, pal[c]);
                outputBm[i] = ImageUtils.BuildImage(imageData, frWidth, frHeight, frWidth, resultFormat, pal, Color.Black);
                imagesToProcess[imageFrames[i]] = null;
            }
            // Remove mask frames
            if (keepOtherFrames && imagesToProcess.Length != imageFrames.Length * 2)
            {
                List<SupportedFileType> images = new List<SupportedFileType>(imagesToProcess);
                for (Int32 i = maskFrames.Length-1; i >= 0; --i)
                    images.RemoveAt(i);
                imagesToProcess = images.ToArray();
            }
            // Recreate the whole thing as new frames file.
            FileFrames newfile = new FileFrames(input);
            newfile.SetFileNames(input.LoadedFile);
            newfile.SetCommonPalette(input.FramesHaveCommonPalette);
            newfile.SetNeedsPalette(input.NeedsPalette);
            newfile.SetBitsPerPixel(input.BitsPerPixel);
            newfile.SetFrameInputClassFromBpp(input.BitsPerPixel);
            newfile.SetPalette(input.GetColors());
            // Adapt to new transparency?
            if (imagesToProcess.Length == imageFrames.Length) {
                Boolean[] trans = new Boolean[resTransIndex + 1];
                trans[resTransIndex] = true;
                newfile.SetTransparencyMask(trans);
            }
            Int32[] imageFramesNew = new Int32[imageFrames.Length];
            for (Int32 i = 0; i < imagesToProcess.Length; ++i)
            {
                SupportedFileType frame = imagesToProcess[i];
                if (frame == null)
                {
                    imageFramesNew[imageFramesNew.Length - 1] = i;
                    continue;
                }
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(newfile, newfile, frame.GetBitmap(), input.LoadedFile, i);
                framePic.SetBitsPerColor(frame.BitsPerPixel);
                framePic.SetNeedsPalette(input.NeedsPalette);
                imagesToProcess[i] = framePic;
            }
            for (Int32 i = 0; i < imageFrames.Length; ++i)
            {
                Bitmap masked = outputBm[i];
                SupportedFileType orig = input.Frames[origImageFrames[i]];
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(newfile, newfile, masked, input.LoadedFile, i);
                Boolean isCga = orig.BitsPerPixel == 2 && resTransIndex < 4;
                framePic.SetBitsPerColor(isCga ? 2 : Image.GetPixelFormatSize(masked.PixelFormat));
                framePic.SetNeedsPalette(input.NeedsPalette);
                imagesToProcess[imageFramesNew[i]] = framePic;                    
            }
            for (Int32 i = 0; i < imagesToProcess.Length; ++i)
                newfile.AddFrame(imagesToProcess[i]);
            return null;
        }
    }
}
