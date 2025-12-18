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

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileFrames : FileImage
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | base.FileClass; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }

        public override String ShortTypeName { get { return "Frames"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return (this.BaseType == null ? String.Empty : this.BaseType + " ") + "Frames"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[0]; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return null; } }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotSupportedException("This is not a real file format to save. How did you even get here?");
        }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette.</summary>
        public override Boolean FramesHaveCommonPalette { get { return this.m_CommonPalette; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return this.m_LoadedImage != null; } }
        public override Int32 BitsPerPixel { get { return this.m_BitsPerColor != -1 ? this.m_BitsPerColor : base.BitsPerPixel; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return this.m_TransparencyMask; } }

        /// <summary>Amount of colors in the palette that is contained inside the image. 0 if the image itself does not contain a palette, even if it generates one.</summary>
        public override Int32 ColorsInPalette { get { return this.m_ColorsInPalette == 0 ? (this.m_LoadedImage == null ? 0 : this.m_LoadedImage.Palette.Entries.Length) : m_ColorsInPalette; } }

        /// <summary>
        /// Avoid using this for adding frames: use AddFrame instead.
        /// </summary>
        public List<SupportedFileType> FramesList = new List<SupportedFileType>();

        public String BaseType { get; private set; }

        protected Boolean m_CommonPalette;
        protected Int32 m_ColorsInPalette;
        protected Int32 m_BitsPerColor;
        protected Boolean[] m_TransparencyMask;

        /// <summary>
        /// Adds a frame to the list, setting its FrameParent property to this object.
        /// </summary>
        /// <param name="frame"></param>
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

        public void SetBitsPerColor(Int32 bitsPerColor)
        {
            this.m_BitsPerColor = bitsPerColor;
        }

        public void SetColorsInPalette(Int32 colorsInPalette)
        {
            this.m_ColorsInPalette = colorsInPalette;
        }

        public void SetTransparencyMask(Boolean[] transparencyMask)
        {
            this.m_TransparencyMask = transparencyMask;
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
            UInt64 filenum = UInt64.Parse(numpart);
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
            if (frName.Length == 0)
                frName = new String(Enumerable.Repeat('#', numpartFormat.Length).ToArray());
            baseName = Path.Combine(folder, frName + ext);
            UInt64 fullRange = maxNum - minNum + 1;
            String[] allNames = new String[fullRange];
            for (UInt64 i = 0; i < fullRange; i++)
                allNames[i] = Path.Combine(folder, namepart + (minNum + i).ToString(numpartFormat) + ext);
            return allNames;
        }

        public static SupportedFileType CheckForFrames(String path, SupportedFileType currentType, out String minName, out String maxName, out Boolean hasEmptyFrames, out Boolean isChainedFramesType)
        {
            String baseName;

            minName = null;
            maxName = null;
            hasEmptyFrames = false;
            isChainedFramesType = false;
            String[] frameNames = GetFrameFilesRange(path, out baseName);
            // No file or only one file; not a range. Abort.
            if (frameNames == null || frameNames.Length == 1)
                return null;
            // not used for now...
            Boolean framesType = currentType != null && currentType.IsFramesContainer;
            SupportedFileType chainedFrames = null;//framesType ? currentType.ChainLoadFiles(ref frameNames, path) : null;
            isChainedFramesType = chainedFrames != null;
            minName = Path.GetFileName(frameNames[0]);
            maxName = Path.GetFileName(frameNames[frameNames.Length - 1]);
            if (framesType)
                return chainedFrames;

            FileFrames framesContainer = new FileFrames();
            framesContainer.SetFileNames(baseName);
            if (currentType == null)
            {
                foreach (String framePath in frameNames)
                {
                    if (new FileInfo(framePath).Length == 0)
                        continue;
                    SupportedFileType[] possibleTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(AutoDetectTypes, framePath);
                    List<FileTypeLoadException> loadErrors;
                    currentType = LoadFileAutodetect(framePath, possibleTypes, out loadErrors, false);
                    break;
                }
                // All frames are empty. Not gonna support that.
                if (currentType == null)
                    return null;
            }
            framesContainer.BaseType = currentType.ShortTypeName;
            Color[] pal = currentType.GetColors();
            framesContainer.m_CommonPalette = pal != null && currentType.ColorsInPalette > 0;
            foreach (String currentFrame in frameNames)
            {
                if (new FileInfo(currentFrame).Length == 0)
                {
                    hasEmptyFrames = true;
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(framesContainer, currentType, null, currentFrame, -1);
                    frame.SetBitsPerColor(currentType.BitsPerPixel);
                    frame.SetFileClass(currentType.FileClass);
                    frame.SetColorsInPalette(currentType.ColorsInPalette);
                    framesContainer.AddFrame(frame);
                    continue;
                }
                try
                {
                    SupportedFileType frameFile = (SupportedFileType)Activator.CreateInstance(currentType.GetType());
                    Byte[] fileData = File.ReadAllBytes(currentFrame);
                    frameFile.LoadFile(fileData, currentFrame);
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(framesContainer, frameFile, frameFile.GetBitmap(), currentFrame, -1);
                    frame.SetBitsPerColor(frameFile.BitsPerPixel);
                    frame.SetFileClass(frameFile.FileClass);
                    frame.SetColorsInPalette(frameFile.ColorsInPalette);
                    framesContainer.AddFrame(frame);
                    if (framesContainer.m_CommonPalette)
                        framesContainer.m_CommonPalette = frameFile.GetColors() != null && frameFile.ColorsInPalette > 0 && pal.SequenceEqual(frameFile.GetColors());
                }
                catch (FileTypeLoadException)
                {
                    // One of the files in the sequence cannot be loaded as the same type. Abort.
                    return null;
                }
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
        /// <param name="keepIndices">If all involved images are indexed, and no overflow can occur, paste bare data indices when handling indexed types rather than matching image colours to a palette.</param>
        /// <returns></returns>
        public static FileFrames PasteImageOnFrames(SupportedFileType framesContainer, Bitmap image, Point pasteLocation, Int32[] framesRange, Boolean keepIndices)
        {
            Int32 bpp = Image.GetPixelFormatSize(image.PixelFormat);
            Color[] imPalette = bpp > 8 ? null : image.Palette.Entries;
            Boolean[] imPalTrans = imPalette == null ? null : imPalette.Select(c => c.A == 0).ToArray();
            Boolean[] imTransMask = null;
            Int32 imWidth = image.Width;
            Int32 imHeight = image.Height;
            Byte[] imData = null;
            Int32 imStride = imWidth;
            Boolean equalPal = framesContainer.FramesHaveCommonPalette;

            Rectangle pastePos = new Rectangle(pasteLocation, new Size(imWidth, imHeight));
            String name = String.Empty;
            if (framesContainer.LoadedFile != null)
                name = framesContainer.LoadedFile;
            else if (framesContainer.LoadedFileName != null)
                name = framesContainer.LoadedFileName;
            FileFrames newfile = new FileFrames();
            newfile.SetFileNames(name);
            newfile.SetCommonPalette(true);
            newfile.SetBitsPerColor(framesContainer.BitsPerPixel);
            newfile.SetColorsInPalette(framesContainer.ColorsInPalette);
            Boolean[] transMask = framesContainer.TransparencyMask;
            newfile.SetTransparencyMask(transMask);
            framesRange = framesRange.Distinct().OrderBy(x => x).ToArray();
            Int32 framesToHandle = framesRange.Length;
            Int32 nextPasteFrameIndex = 0;

            for (Int32 i = 0; i < framesContainer.Frames.Length; i++)
            {
                SupportedFileType frame = framesContainer.Frames[i];
                Bitmap frBm = frame.GetBitmap();
                Bitmap newBm;
                // List is sorted. This is more efficient than "contains" every time.
                if (nextPasteFrameIndex < framesToHandle && i == framesRange[nextPasteFrameIndex])
                {
                    nextPasteFrameIndex++;
                    Int32 frWidth = frBm.Width;
                    Int32 freight = frBm.Height;

                    if ((frBm.PixelFormat & PixelFormat.Indexed) == 0)
                    {
                        newBm = new Bitmap(frBm.Width, frBm.Height, frBm.PixelFormat);
                        using (Graphics g = Graphics.FromImage(newBm))
                        {
                            g.DrawImage(frBm, 0, 0, frWidth, freight);
                            g.DrawImage(image, pastePos);
                        }
                    }
                    else
                    {
                        Color[] frPalette = frBm.Palette.Entries;
                        Boolean[] transGuide = null;
                        Int32 frBpp = Image.GetPixelFormatSize(frBm.PixelFormat);
                        Int32 frStride;
                        Byte[] frData = ImageUtils.GetImageData(frBm, out frStride);
                        frData = ImageUtils.ConvertTo8Bit(frData, frWidth, freight, 0, frBpp, false, ref frStride);
                        Boolean regenImage = imData == null || !equalPal;
                        if (bpp <= 8)
                        {
                            Boolean keepInd = keepIndices && bpp <= frBpp;
                            if (regenImage)
                            {
                                transGuide = frPalette.Select(col => col.A != 0xFF).ToArray();
                                imData = ImageUtils.GetImageData(image, out imStride);
                                imData = ImageUtils.ConvertTo8Bit(imData, imWidth, imHeight, 0, bpp, false, ref imStride);
                                if (!keepInd)
                                {
                                    imTransMask = imData.Select(px => imPalTrans[px]).ToArray();
                                    imData = ImageUtils.Match8BitDataToPalette(imData, imStride, imHeight, imPalette, frPalette);
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
                                // Create 'bit mask' to determine which pieces on the image are transparent and should be ignored for the paste.
                                Color[] palTrans = new Color[] { Color.Transparent, Color.Gray };
                                Int32 maskStride = imStride;
                                Byte[] transMask1 = ImageUtils.Convert32BitToPaletted(imData, imWidth, imHeight, 8, true, palTrans, ref maskStride);
                                imTransMask = transMask1.Select(b => b == 0).ToArray();
                                // Get actual image data
                                imData = ImageUtils.Convert32BitToPaletted(imData, imWidth, imHeight, 8, true, frPalette, ref imStride);
                            }
                        }
                        // Paste using the transparency image mask.
                        frData = ImageUtils.PasteOn8bpp(frData, frWidth, freight, frStride, imData, imWidth, imHeight, imStride, pastePos, transGuide, true, imTransMask);
                        frData = ImageUtils.ConvertFrom8Bit(frData, frWidth, freight, frBpp, false, ref frStride);
                        newBm = ImageUtils.BuildImage(frData, frWidth, freight, frStride, ImageUtils.GetIndexedPixelFormat(frBpp), frPalette, null);
                    }
                }
                else
                {
                    newBm = ImageUtils.CloneImage(frBm);
                }
                FileImageFrame frameCombined = new FileImageFrame();
                frameCombined.LoadFileFrame(newfile, frame.ShortTypeDescription, newBm, name, i);
                frameCombined.SetBitsPerColor(frame.BitsPerPixel);
                frameCombined.SetFileClass(frame.FileClass);
                frameCombined.SetColorsInPalette(frame.ColorsInPalette);
                frameCombined.SetTransparencyMask(transMask);
                frameCombined.SetExtraInfo(frame.ExtraInfo);
                newfile.AddFrame(frameCombined);
            }
            return newfile;
        }
    }
}
