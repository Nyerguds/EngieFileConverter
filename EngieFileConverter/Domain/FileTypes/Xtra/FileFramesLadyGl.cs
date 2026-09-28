using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesLadyGl : SupportedFileType
    {
        public override FileClass FileClass { get { return this.m_LoadedImage == null ? FileClass.FrameSet :   FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override string IdCode { get { return "LadyGl"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "LadyLove GL archive"; } }
        public override string[] FileExtensions { get { return new string[] { "gl", "glt" }; } }
        public override string LongTypeName { get { return "LadyLove GL archive"; } }
        public override int BitsPerPixel { get { return 8; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        public override bool FramesHaveCommonPalette { get { return this.m_CommonPalette; } }
        private bool m_CommonPalette = false;
        public override bool NeedsPalette { get { return this.m_Palette == null; } }

        public override bool CanSave { get { return false; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            const int nameLen = 0x0D;
            const int entryLen = 0x0D + 8;
            byte[] archiveData = null;
            byte[] tableData = null;
            if (sourcePath == null)
                throw new FileTypeLoadException("Need path to identify this type.");
            string basePath = Path.GetDirectoryName(sourcePath);
            string baseName = Path.Combine(basePath, Path.GetFileNameWithoutExtension(sourcePath));
            string ext = Path.GetExtension(sourcePath);
            if (".GLT".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                tableData = fileData;
            else if (".GL".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                archiveData = fileData;
            else
            {
                if (File.Exists(baseName + ".GL"))
                    tableData = fileData;
                else if (File.Exists(baseName + ".GLT"))
                    archiveData = fileData;
            }
            bool isTable = tableData != null;
            if (archiveData == null && tableData == null)
                throw new FileTypeLoadException("Cannot find accompanying file.");
            if (archiveData == null && File.Exists(baseName + ".GL"))
                archiveData = File.ReadAllBytes(baseName + ".GL");
            if (tableData == null && File.Exists(baseName + ".GLT"))
                tableData = File.ReadAllBytes(baseName + ".GLT");
            if (archiveData == null || tableData == null)
                throw new FileTypeLoadException("Cannot find accompanying file.");

            int tableOffset = 0;
            int tableLength = tableData.Length;
            int dataLength = archiveData.Length;
            if (tableLength % entryLen != 0)
                throw new FileTypeLoadException("Table data does not exact amount of entries.");
            List<SupportedFileType> frames = new List<SupportedFileType>();
            int frameNr = 0;
            Color[] firstPalette = null;
            Color[] lastPalette = null;
            string lastPalFrame = null;
            List<int[]> contentOverlapCheck = new List<int[]>();
            while (tableOffset < tableLength)
            {
                byte[] nameBuf = new byte[nameLen];
                Array.Copy(tableData, tableOffset, nameBuf, 0, nameLen);
                string fileName = new string(nameBuf.TakeWhile(b => b != 0).Select(c => (char) (c <= 0x20 || c > 0x7F ? 0 : c)).ToArray());
                if (fileName.Contains('\0'))
                    throw new FileTypeLoadException("Non-ascii characters in internal filename.");
                string[] nameSplit = fileName.Split('.');
                int actualNameLen = fileName.Length;
                if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                    throw new FileTypeLoadException("Internal filename does not match DOS 8.3 format.");
                string framePath = Path.Combine(basePath, fileName);
                int fileOffset = ArrayUtils.ReadInt32FromByteArrayLe(tableData, tableOffset + nameLen);
                int fileLength = ArrayUtils.ReadInt32FromByteArrayLe(tableData, tableOffset + nameLen + 4);
                tableOffset += entryLen;
                if (fileOffset < 0 || fileLength < 0)
                    throw new FileTypeLoadException("Bad data in table.");
                int fileEnd = fileOffset + fileLength;

                for (int i = 0; i < frameNr; ++i)
                {
                    int[] prevFrameLen = contentOverlapCheck[i];
                    int prevStart = prevFrameLen[0];
                    int prevEnd = prevFrameLen[1];
                    if ((fileOffset >= prevStart && fileOffset < prevEnd) || (fileEnd >= prevStart && fileEnd < prevEnd))
                        throw new FileTypeLoadException("Overlapping files in table.");
                }
                contentOverlapCheck.Add(new int[] {fileOffset, fileEnd});
                if (dataLength < fileEnd)
                    throw new FileTypeLoadException("Internal file does not fit in archive.");
                try
                {
                    FileImgLadyTme tmeFrame = new FileImgLadyTme();
                    tmeFrame.LoadFromFileData(archiveData, framePath, fileOffset, fileLength);
                    if (!tmeFrame.NeedsPalette)
                    {
                        lastPalette = tmeFrame.GetColors();
                        lastPalFrame = fileName + " (frame " + frameNr + ")";
                        if (firstPalette == null)
                            firstPalette = lastPalette;
                    }
                    else if (lastPalette != null)
                        tmeFrame.OverridePalette(lastPalette, "Colors inherited from " + lastPalFrame);
                    frames.Add(tmeFrame);
                }
                catch (FileTypeLoadException)
                {
                    FileImageFrame emptyFrame = new FileImageFrame();
                    emptyFrame.SetFileNames(framePath);
                    emptyFrame.SetBitsPerColor(8);
                    emptyFrame.SetExtraInfo("Not a TME image file.");
                    frames.Add(emptyFrame);
                }
                frameNr++;
            }
            if (frameNr == 0)
                throw new FileTypeLoadException("No frames found.");
            this.m_FramesList = frames.ToArray();
            this.m_CommonPalette = true;
            this.m_Palette = null;
            if (firstPalette != null)
            {
                Color[] firstPal = firstPalette;
                for (int i = 1; i < frameNr; ++i)
                {
                    FileImgLadyTme sft = frames[i] as FileImgLadyTme;
                    if (sft == null)
                        continue;
                    if (sft.NeedsPalette || !firstPal.SequenceEqual(sft.GetColors()))
                    {
                        this.m_CommonPalette = false;
                        break;
                    }
                }
                if (this.m_CommonPalette)
                {
                    this.m_Palette = firstPal;
                }
            }
            this.LoadedFile = sourcePath;
            string curPath = Path.GetFileNameWithoutExtension(sourcePath);
            if (isTable)
                curPath = curPath + ".GL/" + Path.GetExtension(sourcePath).TrimStart('.');
            else
                curPath = curPath + Path.GetExtension(sourcePath) + "/GLT";
            this.LoadedFileName = curPath;
        }


        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotSupportedException();
        }

    }
}