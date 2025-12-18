using Nyerguds.ImageManipulation;
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
        public override String ShortTypeDescription { get { return (BaseType == null ? String.Empty : BaseType + " ") + "Frames"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[0]; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return null; } }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotSupportedException("This is not a real file format to save. How did you even get here?");
        }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        
        /// <summary>
        /// Avoid using this for adding frames: use AddFrame instead.
        /// </summary>
        public List<SupportedFileType> FramesList = new List<SupportedFileType>();

        public String BaseType { get; private set; }

        /// <summary>
        /// Adds a frame to the list, setting its FrameParent property to this object.
        /// </summary>
        /// <param name="frame"></param>
        public void AddFrame(SupportedFileType frame)
        {
            frame.FrameParent = this;
            this.FramesList.Add(frame);
        }

        public static FileFrames CheckForFrames(String path, SupportedFileType currentType, out String minName, out String maxName, out Boolean hasEmptyFrames)
        {
            minName = null;
            maxName = null;
            hasEmptyFrames = false;
            // The type is a frames container, and is not guaranteed to have a bitmap
            if (currentType != null && currentType.IsFramesContainer && !currentType.HasCompositeFrame)
                return null;
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
            String curName;
            while (File.Exists(Path.Combine(folder, curName = namepart + num.ToString(numpartFormat) + ext)))
            {
                minNum = num;
                minName = curName;
                if (num == 0)
                    break;
                num--;
            }
            num = filenum;
            UInt64 maxNum = filenum;
            while (File.Exists(Path.Combine(folder, curName = namepart + num.ToString(numpartFormat) + ext)))
            {
                maxNum = num;
                maxName = curName;
                if (num == UInt64.MaxValue)
                    break;
                num++;
            }
            // Only one frame; not a range. Abort.
            if (maxNum == minNum)
                return null;
            FileFrames framesContainer = new FileFrames();
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
            framesContainer.SetFileNames(Path.Combine(folder, frName + ext));
            if (currentType == null)
            {
                for (num = minNum; num <= maxNum; num++)
                {
                    String framePath = Path.Combine(folder, namepart + num.ToString(numpartFormat) + ext);
                    if (new FileInfo(framePath).Length == 0)
                        continue;
                    SupportedFileType[] possibleTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, framePath);
                    List<FileTypeLoadException> loadErrors;
                    currentType = SupportedFileType.LoadFileAutodetect(framePath, possibleTypes, out loadErrors, false);
                    break;
                }
                // All frames are empty. Not gonna support that.
                if (currentType == null)
                    return null;
            }
            framesContainer.BaseType = currentType.ShortTypeName;
            for (num = minNum; num <= maxNum; num++)
            {
                String currentFrame = Path.Combine(folder, namepart + num.ToString(numpartFormat) + ext);
                if (new FileInfo(currentFrame).Length == 0)
                {
                    hasEmptyFrames = true;
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(framesContainer, currentType.ShortTypeName, null, currentFrame, -1);
                    frame.SetBitsPerColor(currentType.BitsPerColor);
                    frame.SetColorsInPalette(currentType.ColorsInPalette);
                    framesContainer.AddFrame(frame);
                    continue;
                }
                try
                {
                    SupportedFileType frameFile = (SupportedFileType)Activator.CreateInstance(currentType.GetType());
                    frameFile.LoadFile(currentFrame);
                    FileImageFrame frame = new FileImageFrame();
                    frame.LoadFileFrame(framesContainer, frameFile.ShortTypeName, frameFile.GetBitmap(), currentFrame, -1);
                    frame.SetBitsPerColor(frameFile.BitsPerColor);
                    frame.SetColorsInPalette(frameFile.ColorsInPalette);
                    framesContainer.AddFrame(frame);
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
