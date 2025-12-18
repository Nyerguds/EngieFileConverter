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

        public override Int32 Width { get { return this.m_Width; } }
        public override Int32 Height { get { return this.m_Height; } }
        protected Int32 m_Width;
        protected Int32 m_Height;
        public override String IdCode { get { return "WwShpBr"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood BR Shape"; } }
        public override String[] FileExtensions { get { return new String[] { "shp" }; } }
        public override String LongTypeName { get { return "Westwood Shape File - Blade Runner"; } }
        public override Boolean NeedsPalette { get { return false; } }
        public override Int32 BitsPerPixel { get { return 16; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

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
            Int32 fileLen = fileData.Length;
            if (fileData.Length < 0x04)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            if (fileData[2] != 0 || fileData[3] != 0)
                throw new FileTypeLoadException("Too many frames.");
            UInt32 nrOfFrames = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0);
            if (nrOfFrames == 0)
                throw new FileTypeLoadException("Not a BR SHP file.");
            Int32 readOffset = 4;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                if (readOffset + 0x0C >= fileLen)
                    throw new FileTypeLoadException("Not a BR SHP file.");
                Int32 frWidth =  ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset);
                Int32 frHeight = ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset + 4);
                Int32 frSize =   ArrayUtils.ReadInt32FromByteArrayLe(fileData, readOffset + 8);
                if (frWidth <= 0 || frHeight <= 0 || frWidth * frHeight * 2 != frSize)
                    throw new FileTypeLoadException("Not a BR SHP file.");
                readOffset += 0x0C;
                if (readOffset + frSize > fileLen)
                    throw new FileTypeLoadException("Not a BR SHP file.");
                Byte[] frameData = new Byte[frSize];
                Array.Copy(fileData, readOffset, frameData, 0, frSize);
                for (Int32 b = 1; b < frSize; b += 2)
                {
                    Byte val = frameData[b];
                    frameData[b] = (val & 0x80) == 0 ? (Byte)(val | 0x80) : (Byte)(val & 0x7F);
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

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return null;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave);
            UInt32 nrOfFrames = (UInt32)frames.Length;
            Byte[][] framesData = new Byte[nrOfFrames][];
            UInt32[] frameWidths = new UInt32[nrOfFrames];
            UInt32[] frameHeights = new UInt32[nrOfFrames];
            UInt32 fullSize = 4;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Bitmap frame = frames[i].GetBitmap();
                Int32 stride;
                Byte[] frameData = ImageUtils.GetImageData(frame, out stride, PixelFormat.Format16bppArgb1555, true);
                UInt32 frSize = (UInt32)frameData.Length;
                for (Int32 b = 1; b < frSize; b += 2)
                {
                    Byte val = frameData[b];
                    frameData[b] = (val & 0x80) == 0 ? (Byte)(val | 0x80) : (Byte)(val & 0x7F);
                }
                framesData[i] = frameData;
                frameWidths[i] = (UInt32)frame.Width;
                frameHeights[i] = (UInt32)frame.Height;
                fullSize += 0x0C + frSize;
            }
            Byte[] fileData = new Byte[fullSize];
            ArrayUtils.WriteUInt32ToByteArrayLe(fileData, 0, nrOfFrames);
            Int32 writeOffset = 4;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Byte[] frameData = framesData[i];
                UInt32 frSize = (UInt32) frameData.Length;
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
            Int32 nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_FRAMES_EMPTY, "fileToSave");
            }
            return frames;
        }

    }
}