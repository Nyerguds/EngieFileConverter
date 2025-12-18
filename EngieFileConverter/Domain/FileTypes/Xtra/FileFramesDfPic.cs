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

        public override Int32 Width { get { return 320; } }
        public override Int32 Height { get { return 200; } }
        public override String IdCode { get { return "DflPic"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "DaisyField Pictures"; } }
        public override String[] FileExtensions { get { return new String[] { "pic" }; } }
        public override String LongTypeName { get { return "DaisyField Pictures File"; } }
        public override Boolean NeedsPalette { get { return true; } }
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
            const Int32 palSize = 0x300;
            Int32 frameSize = this.Width * this.Height;
            Int32 frameDataSize = frameSize + palSize;
            Int32 fileDataLength = fileData.Length;
            if (fileDataLength % frameDataSize != 0)
                throw new FileTypeLoadException("Not a DaisyField PIC file!");
            Int32 nrOfFrames = fileDataLength / frameDataSize;
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            Int32 readIndex = 0;
            for (Int32 f = 0; f < nrOfFrames; ++f)
            {
                Int32 palReadIndex = readIndex;
                Int32 imgReadIndex = readIndex + palSize;
                Byte[] framePalData = new Byte[palSize];
                Array.Copy(fileData, palReadIndex, framePalData, 0, palSize);
                Byte[] frameData = new Byte[frameSize];
                Array.Copy(fileData, imgReadIndex, frameData, 0, frameSize);
                for (Int32 i = 0; i < palSize; ++i)
                {
                    Byte curVal = framePalData[i];
                    // All values are between 0x40 and 0x80;
                    if (curVal < 0x40 || curVal >= 0x80)
                        throw new FileTypeLoadException("Not a DaisyField PIC file!");
                    framePalData[i] = (Byte)(curVal ^ 0x55);
                }
                Color[] frPalette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(framePalData, 0));
                for (Int32 i = 0; i < frameSize; ++i)
                    frameData[i] = (Byte)(frameData[i] ^ 0x55);
                Bitmap curFrImg = ImageUtils.BuildImage(frameData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, frPalette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, f);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                this.m_FramesList[f] = framePic;
                readIndex += frameDataSize;
            }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames;
            if (frames == null || (nrOfFrames = frames.Length) == 0)
                throw new ArgumentException(ERR_NO_FRAMES);
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    throw new ArgumentException(ERR_EMPTY_FRAMES, "fileToSave");
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
                if (frame.Width != 320 || frame.Height != 200)
                    throw new ArgumentException(String.Format(ERR_INPUT_DIMENSIONS, 320, 200), "fileToSave");
            }
            const Int32 palSize = 0x300;
            Int32 frameSize = this.Width * this.Height;
            Int32 frameDataSize = frameSize + palSize;
            Int32 fileDataLength = nrOfFrames * frameDataSize;
            Byte[] outBytes = new Byte[fileDataLength];
            Int32 writeOffset = 0;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap fr = frame.GetBitmap();
                Byte[] sixBitCols = ColorUtils.GetSixBitPaletteData(ColorUtils.GetSixBitColorPalette(fr.Palette.Entries));
                for (Int32 j = 0; j < palSize; ++j)
                    sixBitCols[j] = (Byte)(sixBitCols[j] ^ 0x55);
                Array.Copy(sixBitCols, 0, outBytes, writeOffset, palSize);
                writeOffset += palSize;
                Int32 stride;
                Byte[] imageBytes = ImageUtils.GetImageData(fr, out stride, true);
                for (Int32 j = 0; j < frameSize; ++j)
                    imageBytes[j] = (Byte)(imageBytes[j] ^ 0x55);
                Array.Copy(imageBytes, 0, outBytes, writeOffset, frameSize);
                writeOffset += frameSize;
            }
            return outBytes;
        }

    }

}