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

        public override String IdCode { get { return "LadyGl"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "LadyLove GL archive"; } }
        public override String[] FileExtensions { get { return new String[] { "gl", "glt" }; } }
        public override String LongTypeName { get { return "LadyLove GL archive"; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        public override Boolean FramesHaveCommonPalette { get { return this.m_CommonPalette; } }
        private Boolean m_CommonPalette = false;
        public override Boolean NeedsPalette { get { return this.m_Palette == null; } }

        public override Boolean CanSave { get { return false; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            const Int32 nameLen = 0x0D;
            const Int32 entryLen = 0x0D + 8;
            Byte[] archiveData = null;
            Byte[] tableData = null;
            if (sourcePath == null)
                throw new FileTypeLoadException("Need path to identify this type.");
            String basePath = Path.GetDirectoryName(sourcePath);
            String baseName = Path.Combine(basePath, Path.GetFileNameWithoutExtension(sourcePath));
            String ext = Path.GetExtension(sourcePath);
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
            Boolean isTable = tableData != null;
            if (archiveData == null && tableData == null)
                throw new FileTypeLoadException("Cannot find accompanying file.");
            if (archiveData == null && File.Exists(baseName + ".GL"))
                archiveData = File.ReadAllBytes(baseName + ".GL");
            if (tableData == null && File.Exists(baseName + ".GLT"))
                tableData = File.ReadAllBytes(baseName + ".GLT");
            if (archiveData == null || tableData == null)
                throw new FileTypeLoadException("Cannot find accompanying file.");

            Int32 tableOffset = 0;
            Int32 tableLength = tableData.Length;
            Int32 dataLength = archiveData.Length;
            if (tableLength % entryLen != 0)
                throw new FileTypeLoadException("Table data does not exact amount of entries.");
            List<SupportedFileType> frames = new List<SupportedFileType>();
            Int32 frameNr = 0;
            Color[] firstPalette = null;
            Color[] lastPalette = null;
            String lastPalFrame = null;
            List<Int32[]> contentOverlapCheck = new List<Int32[]>();
            while (tableOffset < tableLength)
            {
                Byte[] nameBuf = new Byte[nameLen];
                Array.Copy(tableData, tableOffset, nameBuf, 0, nameLen);
                String fileName = new String(nameBuf.TakeWhile(b => b != 0).Select(c => (Char) (c <= 0x20 || c > 0x7F ? 0 : c)).ToArray());
                if (fileName.Contains('\0'))
                    throw new FileTypeLoadException("Non-ascii characters in internal filename.");
                String[] nameSplit = fileName.Split('.');
                Int32 actualNameLen = fileName.Length;
                if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                    throw new FileTypeLoadException("Internal filename does not match DOS 8.3 format.");
                String framePath = Path.Combine(basePath, fileName);
                Int32 fileOffset = ArrayUtils.ReadInt32FromByteArrayLe(tableData, tableOffset + nameLen);
                Int32 fileLength = ArrayUtils.ReadInt32FromByteArrayLe(tableData, tableOffset + nameLen + 4);
                tableOffset += entryLen;
                if (fileOffset < 0 || fileLength < 0)
                    throw new FileTypeLoadException("Bad data in table.");
                Int32 fileEnd = fileOffset + fileLength;

                for (Int32 i = 0; i < frameNr; ++i)
                {
                    Int32[] prevFrameLen = contentOverlapCheck[i];
                    Int32 prevStart = prevFrameLen[0];
                    Int32 prevEnd = prevFrameLen[1];
                    if ((fileOffset >= prevStart && fileOffset < prevEnd) || (fileEnd >= prevStart && fileEnd < prevEnd))
                        throw new FileTypeLoadException("Overlapping files in table.");
                }
                contentOverlapCheck.Add(new Int32[] {fileOffset, fileEnd});
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
                for (Int32 i = 1; i < frameNr; ++i)
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
            String curPath = Path.GetFileNameWithoutExtension(sourcePath);
            if (isTable)
                curPath = curPath + ".GL/" + Path.GetExtension(sourcePath).TrimStart('.');
            else
                curPath = curPath + Path.GetExtension(sourcePath) + "/GLT";
            this.LoadedFileName = curPath;
        }


        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotSupportedException();
        }

    }
}