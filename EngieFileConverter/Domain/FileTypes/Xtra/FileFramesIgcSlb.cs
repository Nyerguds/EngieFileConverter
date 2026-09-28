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

        public override int Width { get { return 0; } }
        public override int Height { get { return 0; } }
        public override string IdCode { get { return null; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Interactive Girls archive"; } }
        public override string[] FileExtensions { get { return new string[] { "slb", "m3" }; } }
        public override string LongTypeName { get { return "Interactive Girls SLB archive"; } }
        public override bool NeedsPalette { get { return true; } }
        public override bool FramesHaveCommonPalette { get { return false; } }
        public override int BitsPerPixel { get { return 8; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }

        public override bool CanSave { get { return false; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            int fileDataLength = fileData.Length;
            if (fileDataLength < 8 || ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0) != 0)
                throw new FileTypeLoadException("Not an IGC SLB file.");

            int readOffs =4;
            List<int> offsetsList = new List<int>();
            List<bool> offsetsListValid = new List<bool>();
            int minOffs = Int32.MaxValue;
            int indexOffs;
            do
            {
                indexOffs = ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffs);
                minOffs = Math.Min(minOffs, indexOffs);
                if (indexOffs > fileDataLength || indexOffs == 0 || indexOffs < minOffs)
                    throw new FileTypeLoadException("Not an IGC SLB file.");
                bool isImage = indexOffs + 0x1B <= fileDataLength
                        && 0x01325847 == ArrayUtils.ReadUInt32FromByteArrayLe(fileData, indexOffs)
                        && 0x58465053 == ArrayUtils.ReadUInt32FromByteArrayLe(fileData, indexOffs + 0x12);
                offsetsList.Add(indexOffs);
                offsetsListValid.Add(isImage);
                readOffs += 4;
            } while (readOffs < minOffs && indexOffs < fileDataLength);

            if (offsetsList.Count == 0)
                throw new FileTypeLoadException("Not an ICG SLB file.");
            int nrOfFrames = offsetsList.Count - 1;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            string basePath = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath));
            List<string> dataIndices = new List<string>();
            for (int i = 0; i < nrOfFrames; ++i)
            {
                int readStart = offsetsList[i];
                if (readStart < readOffs)
                    throw new FileTypeLoadException("Not an ICG SLB file.");
                int readEnd = offsetsList[i + 1];
                int frameSize = readEnd - readStart;
                string curName = basePath + "-" + i.ToString("D5");
                if (!offsetsListValid[i])
                {
                    FileImageFrame emptyFramePic = new FileImageFrame();
                    emptyFramePic.LoadFileFrame(this, this, null, curName + ".dat", -1);
                    emptyFramePic.SetBitsPerColor(this.BitsPerPixel);
                    emptyFramePic.SetFileClass(this.FrameInputFileClass);
                    emptyFramePic.SetNeedsPalette(this.NeedsPalette);
                    string extraInfo = "Data file: " + frameSize + " bytes";
                    if (frameSize > 2 && ArrayUtils.ReadUInt16FromByteArrayLe(fileData, readStart) == 0x7E7C)
                        extraInfo += "\nDetected as script file (text)";
                    emptyFramePic.SetExtraInfo(extraInfo);
                    this.m_FramesList[i] = emptyFramePic;
                    dataIndices.Add(i.ToString());
                    continue;
                }
                byte[] imageData = new byte[frameSize];
                Array.Copy(fileData, readStart, imageData, 0, imageData.Length);
                FileImgIgcGx2 frame = new FileImgIgcGx2();
                frame.LoadFile(imageData, curName + ".gx2");
                this.m_FramesList[i] = frame;
                if (dataIndices.Count > 0)
                    this.ExtraInfo = "Non-image entries: " + String.Join(", ", dataIndices.ToArray()) + ".";
            }
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotSupportedException();
        }

    }

}