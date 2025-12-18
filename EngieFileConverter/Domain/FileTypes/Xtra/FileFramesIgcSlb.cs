using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Nyerguds.FileData.Compression;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Interactive Girls frames files. Actually an archive, so no writing support.
    /// </summary>
    public class FileFramesIgcSlb : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override Int32 Width { get { return 0; } }
        public override Int32 Height { get { return 0; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Interactive Girls images"; } }
        public override String[] FileExtensions { get { return new String[] { "slb", "m3" }; } }
        public override String ShortTypeDescription { get { return "Interactive Girls SLB images File"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Boolean FramesHaveCommonPalette { get { return false; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        
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
            Int32 fileDataLength = fileData.Length;
            if (fileDataLength < 8 || ArrayUtils.ReadIntFromByteArray(fileData, 0, 4, true) != 0)
                throw new FileTypeLoadException("Not an IGC SLB file!");

            Int32 readOffs=4;
            List<Int32> offsetsList = new List<Int32>();
            List<Boolean> offsetsListValid = new List<Boolean>();
            Int32 minOffs = Int32.MaxValue;
            Int32 indexOffs;
            do
            {
                indexOffs = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffs, 4, true);
                minOffs = Math.Min(minOffs, indexOffs);
                if (indexOffs > fileDataLength)
                    throw new FileTypeLoadException("Not an IGC SLB file!");
                Boolean isImage = indexOffs + 0x1B <= fileDataLength
                        && 0x01325847 == ArrayUtils.ReadIntFromByteArray(fileData, indexOffs, 4, true)
                        && 0x58465053 == ArrayUtils.ReadIntFromByteArray(fileData, indexOffs + 0x12, 4, true);
                offsetsList.Add(indexOffs);
                offsetsListValid.Add(isImage);
                readOffs += 4;
            } while (readOffs < minOffs && indexOffs < fileDataLength);

            if (offsetsList.Count == 0)
                throw new FileTypeLoadException("Not an ICG SLB file!");
            Int32 nrOfFrames = offsetsList.Count - 1;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            String basePath = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath));
            List<String> dataIndices = new List<String>();
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Int32 readStart = offsetsList[i];
                Int32 readEnd = offsetsList[i + 1];
                Int32 frameSize = readEnd - readStart;
                String curName = basePath + "-" + i.ToString("D5");
                if (!offsetsListValid[i])
                {
                    FileImageFrame emptyFramePic = new FileImageFrame();
                    emptyFramePic.LoadFileFrame(this, this, null, curName + ".dat", -1);
                    emptyFramePic.SetBitsPerColor(this.BitsPerPixel);
                    emptyFramePic.SetFileClass(this.FrameInputFileClass);
                    emptyFramePic.SetColorsInPalette(this.ColorsInPalette);
                    String extraInfo = "Data file: " + frameSize + " bytes";
                    if (frameSize > 2 && ArrayUtils.ReadIntFromByteArray(fileData, readStart, 2, true) == 0x7E7C)
                        extraInfo += "\nDetected as script file (text)";
                    emptyFramePic.SetExtraInfo(extraInfo);
                    this.m_FramesList[i] = emptyFramePic;
                    dataIndices.Add(i.ToString());
                    continue;
                }
                Byte[] imageData = new Byte[frameSize];
                Array.Copy(fileData, readStart, imageData, 0, imageData.Length);
                FileImgIgcGx2 frame = new FileImgIgcGx2();
                frame.LoadFile(imageData, curName + ".gx2");
                this.m_FramesList[i] = frame;
                if (dataIndices.Count > 0)
                    this.ExtraInfo = "Non-image entries: " + String.Join(", ", dataIndices.ToArray()) + ".";
                {
                }
            }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return null;
        }

    }

}