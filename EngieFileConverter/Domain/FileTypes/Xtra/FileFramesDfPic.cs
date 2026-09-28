using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// DaisyField pictures, from the game SeXoniX.
    /// </summary>
    /// <remarks>
    /// A big thanks to CTPAX-X Team for giving me the hint that
    /// this was a simple XOR operation, and not a remapping.
    /// </remarks>
    public class FileFramesDfPic : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return 320; } }
        public override int Height { get { return 200; } }
        public override string IdCode { get { return "DflPic"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "DaisyField Pictures"; } }
        public override string[] FileExtensions { get { return new string[] { "pic" }; } }
        public override string LongTypeName { get { return "DaisyField Pictures File"; } }
        public override bool NeedsPalette { get { return true; } }
        public override bool FramesHaveCommonPalette { get { return false; } }
        public override int BitsPerPixel { get { return 8; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }


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
            const int palSize = 0x300;
            int frameSize = this.Width * this.Height;
            int frameDataSize = frameSize + palSize;
            int fileDataLength = fileData.Length;
            if (fileDataLength % frameDataSize != 0)
                throw new FileTypeLoadException("Not a DaisyField PIC file.");
            int nrOfFrames = fileDataLength / frameDataSize;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            int readIndex = 0;
            for (int f = 0; f < nrOfFrames; ++f)
            {
                int palReadIndex = readIndex;
                int imgReadIndex = readIndex + palSize;
                byte[] framePalData = new byte[palSize];
                Array.Copy(fileData, palReadIndex, framePalData, 0, palSize);
                byte[] frameData = new byte[frameSize];
                Array.Copy(fileData, imgReadIndex, frameData, 0, frameSize);
                for (int i = 0; i < palSize; ++i)
                {
                    byte curVal = framePalData[i];
                    // All values are between 0x40 and 0x80;
                    if (curVal < 0x40 || curVal >= 0x80)
                        throw new FileTypeLoadException("Not a DaisyField PIC file.");
                    framePalData[i] = (byte)(curVal ^ 0x55);
                }
                Color[] frPalette = ColorUtils.ReadSixBitPalette(framePalData, 0);
                for (int i = 0; i < frameSize; ++i)
                    frameData[i] = (byte)(frameData[i] ^ 0x55);
                Bitmap curFrImg = ImageUtils.BuildImage(frameData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, frPalette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, f);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                this.m_FramesList[f] = framePic;
                readIndex += frameDataSize;
            }
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames;
            if (frames == null || (nrOfFrames = frames.Length) == 0)
                throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new FileTypeSaveException(ERR_FRAMES_EMPTY);
                if (frame.BitsPerPixel != 8)
                    throw new FileTypeSaveException(String.Format(ERR_BPP_INPUT_EXACT, 8));
                if (frame.Width != 320 || frame.Height != 200)
                    throw new FileTypeSaveException(String.Format(ERR_DIMENSIONS_INPUT, 320, 200));
            }
            const int palSize = 0x300;
            int frameSize = this.Width * this.Height;
            int frameDataSize = frameSize + palSize;
            int fileDataLength = nrOfFrames * frameDataSize;
            byte[] outBytes = new byte[fileDataLength];
            int writeOffset = 0;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap fr = frame.GetBitmap();
                byte[] sixBitCols = ColorUtils.GetSixBitPaletteData(fr.Palette.Entries);
                for (int j = 0; j < palSize; ++j)
                    sixBitCols[j] = (byte)(sixBitCols[j] ^ 0x55);
                Array.Copy(sixBitCols, 0, outBytes, writeOffset, palSize);
                writeOffset += palSize;
                int stride;
                byte[] imageBytes = ImageUtils.GetImageData(fr, out stride, true);
                for (int j = 0; j < frameSize; ++j)
                    imageBytes[j] = (byte)(imageBytes[j] ^ 0x55);
                Array.Copy(imageBytes, 0, outBytes, writeOffset, frameSize);
                writeOffset += frameSize;
            }
            return outBytes;
        }

    }

}