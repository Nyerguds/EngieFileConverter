using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesWwShpBr : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return this.m_Width; } }
        public override int Height { get { return this.m_Height; } }
        protected int m_Width;
        protected int m_Height;
        public override string IdCode { get { return "WwShpBr"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood BR Shape"; } }
        public override string[] FileExtensions { get { return new string[] { "shp" }; } }
        public override string LongTypeName { get { return "Westwood Shape File - Blade Runner"; } }
        public override bool NeedsPalette { get { return false; } }
        public override int BitsPerPixel { get { return 16; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }

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
            int fileLen = fileData.Length;
            if (fileData.Length < 0x04)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            if (fileData[2] != 0 || fileData[3] != 0)
                throw new FileTypeLoadException("Too many frames.");
            uint nrOfFrames = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0);
            if (nrOfFrames == 0)
                throw new FileTypeLoadException("Not a BR SHP file.");
            int readOffset = 4;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            for (int i = 0; i < nrOfFrames; ++i)
            {
                if (readOffset + 0x0C >= fileLen)
                    throw new FileTypeLoadException("Not a BR SHP file.");
                int frWidth =  ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset);
                int frHeight = ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset + 4);
                int frSize =   ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset + 8);
                if (frWidth <= 0 || frHeight <= 0 || frWidth * frHeight * 2 != frSize)
                    throw new FileTypeLoadException("Not a BR SHP file.");
                readOffset += 0x0C;
                if (readOffset + frSize > fileLen)
                    throw new FileTypeLoadException("Not a BR SHP file.");
                byte[] frameData = new byte[frSize];
                Array.Copy(fileData, readOffset, frameData, 0, frSize);
                for (int b = 1; b < frSize; b += 2)
                {
                    byte val = frameData[b];
                    frameData[b] = (val & 0x80) == 0 ? (byte)(val | 0x80) : (byte)(val & 0x7F);
                }
                Bitmap curFrImg = ImageUtils.BuildImage(frameData, frWidth, frHeight, frWidth * 2, PixelFormat.Format16bppArgb1555, null, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(FileClass.ImageHiCol);
                framePic.SetNeedsPalette(false);
                framePic.SetExtraInfo("Data: " + frSize + " bytes @ 0x" + readOffset.ToString("X"));
                this.m_FramesList[i] = framePic;
                readOffset += frSize;
            }
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            return null;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave);
            uint nrOfFrames = (uint)frames.Length;
            byte[][] framesData = new byte[nrOfFrames][];
            uint[] frameWidths = new uint[nrOfFrames];
            uint[] frameHeights = new uint[nrOfFrames];
            uint fullSize = 4;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                Bitmap frame = frames[i].GetBitmap();
                int stride;
                byte[] frameData = ImageUtils.GetImageData(frame, out stride, PixelFormat.Format16bppArgb1555, true);
                uint frSize = (uint)frameData.Length;
                for (int b = 1; b < frSize; b += 2)
                {
                    byte val = frameData[b];
                    frameData[b] = (val & 0x80) == 0 ? (byte)(val | 0x80) : (byte)(val & 0x7F);
                }
                framesData[i] = frameData;
                frameWidths[i] = (uint)frame.Width;
                frameHeights[i] = (uint)frame.Height;
                fullSize += 0x0C + frSize;
            }
            byte[] fileData = new byte[fullSize];
            ArrayUtils.WriteUInt32ToByteArrayLe(fileData, 0, nrOfFrames);
            int writeOffset = 4;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                byte[] frameData = framesData[i];
                uint frSize = (uint) frameData.Length;
                ArrayUtils.WriteUInt32ToByteArrayLe(fileData, writeOffset, frameWidths[i]);
                ArrayUtils.WriteUInt32ToByteArrayLe(fileData, writeOffset + 4, frameHeights[i]);
                ArrayUtils.WriteUInt32ToByteArrayLe(fileData, writeOffset + 8, frSize);
                writeOffset += 0x0C;
                Array.Copy(frameData,0,fileData, writeOffset, frSize);
                writeOffset += frameData.Length;
            }
            return fileData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_FRAMES_EMPTY, "fileToSave");
            }
            return frames;
        }

    }
}