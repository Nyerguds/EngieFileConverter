using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nyerguds.Util;
using Nyerguds.Util.UI;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileFrames : FileImage
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
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
        public override Boolean FramesHaveCommonPalette { get { return this._commonPalette; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        
        /// <summary>
        /// Avoid using this for adding frames: use AddFrame instead.
        /// </summary>
        public List<SupportedFileType> FramesList = new List<SupportedFileType>();

        public String BaseType { get; private set; }

        protected Boolean _commonPalette;
        /// <summary>
        /// Adds a frame to the list, setting its FrameParent property to this object.
        /// </summary>
        /// <param name="frame"></param>
        public void AddFrame(SupportedFileType frame)
        {
            frame.FrameParent = this;
            this.FramesList.Add(frame);
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
            framesContainer._commonPalette = pal != null && currentType.ColorsInPalette > 0;
            foreach (String currentFrame in frameNames)
            {
                if (new FileInfo(currentFrame).Length == 0)
                {
                    hasEmptyFrames = true;
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(framesContainer, currentType.ShortTypeName, null, currentFrame, -1);
                    frame.SetBitsPerColor(currentType.BitsPerPixel);
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
                    frame.LoadFileFrame(framesContainer, frameFile.ShortTypeName, frameFile.GetBitmap(), currentFrame, -1);
                    frame.SetBitsPerColor(frameFile.BitsPerPixel);
                    frame.SetColorsInPalette(frameFile.ColorsInPalette);
                    framesContainer.AddFrame(frame);
                    if (framesContainer._commonPalette)
                        framesContainer._commonPalette = frameFile.GetColors() != null && frameFile.ColorsInPalette > 0 && pal.SequenceEqual(frameFile.GetColors());
                }
                catch (FileTypeLoadException)
                {
                    // One of the files in the sequence cannot be loaded as the same type. Abort.
                    return null;
                }
            }
            return framesContainer;
        }
    }
}
