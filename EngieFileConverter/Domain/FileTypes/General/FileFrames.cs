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

        public override int Width { get { return this.m_LoadedImage == null ? this.CheckCommonWidth() : this.m_LoadedImage.Width; } }
        public override int Height { get { return this.m_LoadedImage == null ? this.CheckCommonHeight() : this.m_LoadedImage.Height; } }

        public override string ShortTypeName { get { return "Frames"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return (this.BaseType == null ? String.Empty : this.BaseType + " ") + "Frames"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions { get { return new string[0]; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override string[] DescriptionsForExtensions { get { return null; } }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new FileTypeSaveException("This is not a real file format to save. How did you even get here?");
        }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette.</summary>
        public override bool FramesHaveCommonPalette { get { return this.m_CommonPalette; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return this.m_LoadedImage != null; } }
        public override int BitsPerPixel { get { return this.m_BitsPerPixel != -1 ? this.m_BitsPerPixel : base.BitsPerPixel; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask { get { return this.m_TransparencyMask; } }

        /// <summary>Amount of colors in the palette that is contained inside the image. 0 if the image itself does not contain a palette, even if it generates one.</summary>
        public override bool NeedsPalette { get { return this.m_NeedsPalette; } }

        /// <summary>
        /// Avoid using this for adding frames: use AddFrame instead.
        /// </summary>
        public List<SupportedFileType> FramesList { get; private set; }

        public string BaseType { get; private set; }
        public Type EmbeddedType { get; private set; }
        public bool FromFileRange { get; private set; }


        /// <summary>Creates a new FileFrames object</summary>
        public FileFrames()
        {
            FramesList = new List<SupportedFileType>();
        }

        /// <summary>Creates a new FileFrames object</summary>
        /// <param name="fromFileRange">Sets whether this file was created from a range of files.</param>
        public FileFrames(bool fromFileRange)
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

        protected bool m_CommonPalette;
        protected bool m_NeedsPalette;
        protected FileClass m_InputFileClass = FileClass.None;
        protected int m_BitsPerPixel;
        protected bool[] m_TransparencyMask;

        private int CheckCommonWidth()
        {
            int nrOfFrames = this.FramesList.Count;
            int width = 0;
            for (int i = 0; i < nrOfFrames; ++i)
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

        private int CheckCommonHeight()
        {
            int nrOfFrames = this.FramesList.Count;
            int height = 0;
            for (int i = 0; i < nrOfFrames; ++i)
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

        public void SetCommonPalette(bool commonPalette)
        {
            this.m_CommonPalette = commonPalette;
        }

        public void SetBitsPerPixel(int bitsPerColor)
        {
            this.m_BitsPerPixel = bitsPerColor;
        }

        public void SetNeedsPalette(bool needsPalette)
        {
            this.m_NeedsPalette = needsPalette;
        }

        public void SetPalette(Color[] palette)
        {
            this.m_Palette = palette;
        }

        public void SetTransparencyMask(bool[] transparencyMask)
        {
            this.m_TransparencyMask = transparencyMask;
        }

        public void SetFrameInputClass(FileClass supportedFrameTypes)
        {
            this.m_InputFileClass = supportedFrameTypes;
        }

        public void SetFrameInputClassFromBpp(int bpp)
        {
            switch (bpp)
            {
                case 1: this.m_InputFileClass = FileClass.Image1Bit; break;
                case 4: this.m_InputFileClass = FileClass.Image4Bit; break;
                case 8: this.m_InputFileClass = FileClass.Image8Bit; break;
                default: this.m_InputFileClass = FileClass.ImageHiCol; break;
            }
        }

        public static string[] GetFrameFilesRange(string path, out string baseName)
        {
            baseName = path;
            string ext = Path.GetExtension(path);
            string folder = Path.GetDirectoryName(path);
            string name = Path.GetFileName(path);
            Regex framesCheck = new Regex("^(.*?)(\\d+)" + Regex.Escape(ext) + "$");
            Match m = framesCheck.Match(name);
            if (!m.Success)
                return null;
            string namepart = m.Groups[1].Value;
            string numpart = m.Groups[2].Value;
            string numpartFormat = "D" + numpart.Length;
            ulong filenum;
            try
            {
                filenum = ulong.Parse(numpart);
            }
            catch (OverflowException)
            {
                return null;
            }
            ulong num = filenum;
            ulong minNum = filenum;
            while (File.Exists(Path.Combine(folder, namepart + num.ToString(numpartFormat) + ext)))
            {
                minNum = num;
                if (num == 0)
                    break;
                num--;
            }
            num = filenum;
            ulong maxNum = filenum;
            while (File.Exists(Path.Combine(folder, namepart + num.ToString(numpartFormat) + ext)))
            {
                maxNum = num;
                if (num == ulong.MaxValue)
                    break;
                num++;
            }
            // Only one frame; not a range. Abort.
            if (maxNum == minNum)
                return null;
            string frName = namepart;
            if (frName.Length == 0)
            {
                string minNameStr = minNum.ToString(numpartFormat);
                string maxNameStr = maxNum.ToString(numpartFormat);
                int index = 0;
                while (index < minNameStr.Length && minNameStr[index] == maxNameStr[index])
                    index++;
                frName = minNameStr.Substring(0, index);
            }
            else if (frName.EndsWith("-") && frName.Length > 1)
                frName = frName.Substring(0, frName.Length - 1);
            frName = frName.Trim();
            if (frName.Length == 0)
                frName = new string(Enumerable.Repeat('#', numpartFormat.Length).ToArray());
            baseName = Path.Combine(folder, frName + ext);
            ulong fullRange = maxNum - minNum + 1;
            string[] allNames = new string[fullRange];
            for (ulong i = 0; i < fullRange; ++i)
                allNames[i] = Path.Combine(folder, namepart + (minNum + i).ToString(numpartFormat) + ext);
            return allNames;
        }

        public static FileFrames CheckForFrames(string path, SupportedFileType currentType, out string minName, out string maxName, out bool hasEmptyFrames)
        {
            string baseName;
            minName = null;
            maxName = null;
            hasEmptyFrames = false;
            string[] frameNames = GetFrameFilesRange(path, out baseName);
            // No file or only one file; not a range. Abort.
            int nrOfFrames;
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
                for (int i = 0; i < nrOfFrames; ++i)
                {
                    string framePath = frameNames[i];
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
            bool commonPalette = pal != null && pal.Length > 0 && !currentType.NeedsPalette;
            FileClass frameTypes = FileClass.None;
            bool nullPalette = currentType.NeedsPalette || pal == null;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                string currentFrame = frameNames[i];
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
                    byte[] fileData = File.ReadAllBytes(currentFrame);
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
        public static SupportedFileType PasteImageOnFrames(SupportedFileType framesContainer, Bitmap image, Point pasteLocation, int[] framesRange, bool keepIndices)
        {
            bool singleImage = (framesContainer.Frames == null || framesContainer.Frames.Length == 0) && framesContainer.GetBitmap() != null;
            int pasteBpp = Image.GetPixelFormatSize(image.PixelFormat);
            if (pasteBpp > 8)
                pasteBpp = 32;
            Color[] imPalette = pasteBpp > 8 ? null : image.Palette.Entries;
            bool[] imPalTrans = imPalette == null ? null : imPalette.Select(c => c.A == 0).ToArray();
            bool[] imTransMask = null;
            int imWidth = image.Width;
            int imHeight = image.Height;
            byte[] imData = null;
            int imStride = imWidth;
            // check if all frames have the same palette.
            bool equalPal = singleImage || framesContainer.FramesHaveCommonPalette;
            Color[] framePal = null;
            int frameBpp = 0;
            SupportedFileType[] frames = singleImage ? new SupportedFileType[] { framesContainer } : framesContainer.Frames;
            int nrOfFrames = frames.Length;
            // Explicitly test if all frames have the same color depth and palette.
            if (!equalPal)
            {
                bool isEqual = true;
                for (int i = 0; i < nrOfFrames; ++i)
                {
                    SupportedFileType frame = frames[i];
                    // Skip empty frames.
                    if (frame == null || frame.GetBitmap() == null)
                        continue;
                    int curFrameBpp = frame.BitsPerPixel;
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
            string name = String.Empty;
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
                bool[] transMask = framesContainer.TransparencyMask == null ? null : ArrayUtils.CloneArray(framesContainer.TransparencyMask);
                newfile.SetTransparencyMask(transMask);
            }
            framesRange = framesRange.Distinct().OrderBy(x => x).ToArray();
            int framesToHandle = framesRange.Length;
            int nextPasteFrameIndex = 0;

            for (int i = 0; i < nrOfFrames; ++i)
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
                    int curFrameBpp = Image.GetPixelFormatSize(frBm.PixelFormat);
                    nextPasteFrameIndex++;
                    int frWidth = frBm.Width;
                    int frHeight = frBm.Height;

                    if ((frBm.PixelFormat & PixelFormat.Indexed) == 0)
                    {
                        Bitmap tempBm = new Bitmap(frBm);
                        using (Graphics g = Graphics.FromImage(tempBm))
                            g.DrawImage(image, pastePos);
                        if (frBm.PixelFormat != PixelFormat.Format32bppArgb)
                        {
                            byte[] drawBytes = ImageUtils.GetImageData(tempBm, out imStride, frBm.PixelFormat);
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
                        int frBpp = Image.GetPixelFormatSize(frBm.PixelFormat);
                        int frStride;
                        byte[] frData = ImageUtils.GetImageData(frBm, out frStride);
                        if (frBpp != 8)
                            frData = ImageUtils.ConvertTo8Bit(frData, frWidth, frHeight, 0, frBpp, true, ref frStride);
                        // determine whether the image to paste needs to be re-matched to the palette.
                        bool regenImage = false;
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
                        bool[] transGuide = null;
                        if (pasteBpp <= 8)
                        {
                            bool keepInd = keepIndices && pasteBpp <= frBpp;
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
                                int maskStride = imStride;
                                byte[] transMask1 = ImageUtils.Convert32BitToPaletted(imData, imWidth, imHeight, 8, true, palTrans, ref maskStride);
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
        public static FileFrames CutImageIntoFrames(Bitmap image, string imagePath, int frameWidth, int frameHeight, int frames, Color? cropColor, int? cropIndex, int matchBpp, Color[] matchPalette, bool cloneSource, bool needsPalette)
        {
            Bitmap editImage = cloneSource ? ImageUtils.CloneImage(image) : image;
            Bitmap[] framesArr = ImageUtils.ImageToFrames(editImage, frameWidth, frameHeight, cropColor, cropIndex, matchBpp, matchPalette, 0, frames - 1);
            if (cloneSource)
                editImage.Dispose();
            bool isMatched = matchBpp > 0 && matchBpp <= 8 && matchPalette != null;
            int bpp = isMatched ? matchBpp : Image.GetPixelFormatSize(image.PixelFormat);
            Color[] imPalette = isMatched ? matchPalette : bpp > 8 ? null : image.Palette.Entries;
            bool indexed = isMatched || bpp <= 8;
            FileFrames newfile = new FileFrames();
            newfile.SetFileNames(imagePath);
            newfile.SetCommonPalette(indexed);
            newfile.SetNeedsPalette(indexed && needsPalette);
            newfile.SetBitsPerPixel(bpp);
            newfile.SetFrameInputClassFromBpp(bpp);
            newfile.SetPalette(imPalette);
            newfile.SetTransparencyMask(null);
            for (int i = 0; i < framesArr.Length; ++i)
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

        public static int[][] CheckForMaskFrames(SupportedFileType input, out int srcTransIndex)
        {
            Dictionary<ushort, List<int>> twoColImages = new Dictionary<ushort, List<int>>();
            int[] imageFrames;
            int[] maskFrames;
            HashSet<byte> foundColors = new HashSet<byte>();
            SupportedFileType[] frames = input.Frames;
            int nrOfFrames = frames.Length;
            byte[][] frameData = new byte[nrOfFrames][];
            bool sameFrameSizes = true;
            int prevWidth = -1;
            int prevHeight = -1;
            for (int i = 0; i < nrOfFrames; ++i)
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
                byte[] imageData = ImageUtils.GetImageData(frame.GetBitmap(), PixelFormat.Format8bppIndexed);
                frameData[i] = imageData;
                foundColors.Clear();
                for (int j = 0; j < imageData.Length; ++j)
                {
                    if (foundColors.Contains(imageData[i]))
                        continue;
                    foundColors.Add(imageData[i]);
                    if (foundColors.Count > 2)
                        break;
                }
                if (foundColors.Count == 2)
                {
                    byte[] cols = foundColors.ToArray();
                    Array.Sort(cols);
                    ushort keyVal = (ushort)(cols[0] << 8 | cols[1]);
                    List<int> curFrames;
                    if (!twoColImages.TryGetValue(keyVal, out curFrames))
                    {
                        curFrames = new List<int>();
                        twoColImages[keyVal] = curFrames;
                    }
                    curFrames.Add(i);
                }
            }
            // Get the color pair for which the amount of found images is the largest.
            int maxTwoCol = twoColImages.Select(x => x.Value.Count).Max();
            KeyValuePair<ushort, List<int>> detectedColor = twoColImages.Where(x => x.Value.Count == maxTwoCol).First();

            List<int> maskFramesList = detectedColor.Value;
            maskFramesList.Sort();
            // List found. Check how it relates to other frames.
            bool isBefore = true;
            List<int> correspondingFramesBefore = new List<int>();
            bool isAfter = true;
            List<int> correspondingFramesAfter = new List<int>();
            List<int> correspondingFramesBeforeBatch = new List<int>();
            List<int> correspondingFramesAfterBatch = new List<int>();
            for (int i = 0; i < maskFramesList.Count; ++i)
            {
                int frameNr = maskFramesList[i];
                if (frameNr > 0 && !maskFramesList.Contains(frameNr - 1))
                {
                    correspondingFramesBefore.Add(frameNr);
                    continue;
                }
                isBefore = false;
                break;
            }
            for (int i = maskFramesList.Count - 1; i >= 0; --i)
            {
                int frameNr = maskFramesList[i];
                if (frameNr + 1 < maskFramesList.Count && !maskFramesList.Contains(frameNr + 1))
                {
                    correspondingFramesAfter.Add(frameNr);
                    continue;
                }
                isAfter = false;
                break;
            }
            // Try ranges
            bool includesFirst = false;
            bool includesLast = false;
            List<int[]> batches = new List<int[]>();
            for (int i = 0; i < maskFramesList.Count; ++i)
            {
                List<int> batch = new List<int>();
                batch.Add(maskFramesList[i]);
                while (i + 1 < maskFramesList.Count && maskFramesList[i] + 1 == maskFramesList[i + 1])
                {
                    i++;
                    batch.Add(maskFramesList[i]);
                }
                batches.Add(batch.ToArray());
            }
            // Only check ranges if they aren't singular anyway.
            bool hasRanges = batches.Any(b => b.Length > 1);
            // Can't be 'before' the actual image frames it masks if it includes the last frame
            bool isBeforeBatch = hasRanges && !batches.Last().Contains(nrOfFrames - 1);
            // Can't be 'after' the actual image frames it masks if it includes the first frame
            bool isAfterBatch = hasRanges && !batches.First().Contains(0);
            if (isBeforeBatch)
            {
                for (int i = 0; i < batches.Count; ++i)
                {
                    int[] batch = batches[i];
                    // Batches are never empty.
                    int first = batch[0];
                    int maskRangeAmount = batch.Length;
                    for (int j = 0; j < maskRangeAmount; ++j)
                    {
                        int frame = batch[j] - maskRangeAmount;
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
                for (int i = batches.Count - 1; i >= 0; --i)
                {
                    int[] batch = batches[i];
                    // Batches are never empty.
                    int first = batch[0];
                    int maskRangeAmount = batch.Length;
                    for (int j = maskRangeAmount - 1; j >= 0; --j)
                    {
                        int frame = batch[j] - maskRangeAmount;
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
            int[] correctFramesBefore = null;
            int[] correctFramesAfter = null;
            int[] correctFramesBatchBefore = null;
            int[] correctFramesBatchAfter = null;
            if (!sameFrameSizes)
            {
                int[] matchCount = new int[4];
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
            imageFrames = new int[0];
            maskFrames = new int[0];
            srcTransIndex = 0;
            return new int[][] { imageFrames, maskFrames };
        }

        public static FileFrames ApplyTransparencyMask(SupportedFileType input, int[] imageFrames, int[] maskFrames, int srcTransIndex, int resTransIndex, bool keepOtherFrames)
        {
            if (imageFrames.Length != maskFrames.Length)
                throw new ArgumentException("Amount of mask frames does not equal amount of image frames.", "maskFrames");
            Array.Sort(imageFrames);
            Array.Sort(maskFrames);
            int[] origImageFrames = new int[imageFrames.Length];
            Array.Copy(imageFrames, origImageFrames, imageFrames.Length);
            int[] origMaskFrames = new int[maskFrames.Length];
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
                for (int i = 0; i < imageFrames.Length; ++i)
                {
                    imagesToProcess[i] = input.Frames[imageFrames[i]];
                    masksToProcess[i] = input.Frames[maskFrames[i]];
                    imageFrames[i] = i;
                    maskFrames[i] = i;
                }
            }
            Bitmap[] outputBm = new Bitmap[imageFrames.Length];
            for (int i = 0; i < imageFrames.Length; ++i)
            {
                SupportedFileType src = imagesToProcess[imageFrames[i]];
                SupportedFileType mask = masksToProcess[maskFrames[i]];
                Bitmap srcBm = src.GetBitmap();
                Bitmap maskBm = mask.GetBitmap();
                if (srcBm == null || srcBm == null)
                    throw new ArgumentException("Empty frames are not supported for this operation.", "input");
                int frWidth = srcBm.Width;
                int frHeight = srcBm.Height;
                if (frWidth != maskBm.Width || frHeight != maskBm.Height)
                    throw new ArgumentException(String.Format("Dimensions don't match on frame {0}, mask frame {1}.", origImageFrames[i], origMaskFrames[i]), "input");
                PixelFormat srcPf = srcBm.PixelFormat;
                PixelFormat maskPf = maskBm.PixelFormat;
                if (((srcPf | maskPf) & PixelFormat.Indexed) == 0)
                    throw new ArgumentException("All frames need to be indexed.", "input");
                Color[] pal = srcBm.Palette.Entries;
                //Boolean maskHiCol = (maskBm.PixelFormat | PixelFormat.Indexed) == 0;
                byte[] imageData = ImageUtils.GetImageData(srcBm, PixelFormat.Format8bppIndexed);
                byte[] maskData = ImageUtils.GetImageData(maskBm, PixelFormat.Format8bppIndexed);
                int linePos = 0;
                byte resTrans = (byte)resTransIndex;
                for (int y = 0; y < frHeight; ++y)
                {
                    int pos = linePos;
                    for (int x = 0; x < frWidth; ++x)
                    {
                        if (maskData[pos] == srcTransIndex)
                            imageData[pos] = resTrans;
                        pos++;
                    }
                    linePos += frWidth;
                }
                int origBpp = Image.GetPixelFormatSize(srcPf);
                int finalBpp = origBpp;
                while (origBpp < 8 && resTrans >= Math.Pow(2, finalBpp))
                {
                    finalBpp <<= 1;
                }
                if (finalBpp == 2)
                    finalBpp <<= 1;
                if (finalBpp < 8)
                    imageData = ImageUtils.ConvertFrom8Bit(imageData, frWidth, frHeight, finalBpp, true);
                PixelFormat resultFormat = ImageUtils.GetIndexedPixelFormat(finalBpp);
                for (int c = 0; c < pal.Length; ++c)
                    pal[c] = Color.FromArgb(c == resTransIndex ? 0 : 255, pal[c]);
                outputBm[i] = ImageUtils.BuildImage(imageData, frWidth, frHeight, frWidth, resultFormat, pal, Color.Black);
                imagesToProcess[imageFrames[i]] = null;
            }
            // Remove mask frames
            if (keepOtherFrames && imagesToProcess.Length != imageFrames.Length * 2)
            {
                List<SupportedFileType> images = new List<SupportedFileType>(imagesToProcess);
                for (int i = maskFrames.Length-1; i >= 0; --i)
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
                bool[] trans = new bool[resTransIndex + 1];
                trans[resTransIndex] = true;
                newfile.SetTransparencyMask(trans);
            }
            int[] imageFramesNew = new int[imageFrames.Length];
            for (int i = 0; i < imagesToProcess.Length; ++i)
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
            for (int i = 0; i < imageFrames.Length; ++i)
            {
                Bitmap masked = outputBm[i];
                SupportedFileType orig = input.Frames[origImageFrames[i]];
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(newfile, newfile, masked, input.LoadedFile, i);
                bool isCga = orig.BitsPerPixel == 2 && resTransIndex < 4;
                framePic.SetBitsPerColor(isCga ? 2 : Image.GetPixelFormatSize(masked.PixelFormat));
                framePic.SetNeedsPalette(input.NeedsPalette);
                imagesToProcess[imageFramesNew[i]] = framePic;                    
            }
            for (int i = 0; i < imagesToProcess.Length; ++i)
                newfile.AddFrame(imagesToProcess[i]);
            return null;
        }
    }
}
