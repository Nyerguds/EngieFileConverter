using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Interactive Girls frames files. Experimental; so far I can't figure out the compression.
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
        public override String ShortTypeName { get { return "IGC images"; } }
        public override String[] FileExtensions { get { return new String[] { "slb", "m3" }; } }
        public override String ShortTypeDescription { get { return "ICG SLB images File"; } }
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
            if(fileDataLength < 8 || ArrayUtils.ReadIntFromByteArray(fileData, 0, 4, true) != 0)
                throw new FileTypeLoadException("Not an ICG SLB file!");

            Int32 readOffs=4;
            List<Int32> offsetsList = new List<Int32>();
            do
            {
                Int32 indexOffs = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readOffs, 4, true);
                if (indexOffs == fileDataLength)
                {
                    offsetsList.Add(indexOffs);
                    break;
                }
                if (indexOffs + 0x1B > fileDataLength)
                    throw new FileTypeLoadException("Not an ICG SLB file!");
                if (0x01325847 != ArrayUtils.ReadIntFromByteArray(fileData, indexOffs, 4, true) ||
                    0x58465053 != ArrayUtils.ReadIntFromByteArray(fileData, indexOffs + 0x12, 4, true))
                    break;
                offsetsList.Add(indexOffs);
                readOffs += 4;
            } while (true);

            if (offsetsList.Count == 0)
                throw new FileTypeLoadException("Not an ICG SLB file!");
            Int32 nrOfFrames = offsetsList.Count - 1;
            
            this.m_FramesList = new SupportedFileType[nrOfFrames];
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                Int32 readStart = offsetsList[i];
                Int32 readEnd = offsetsList[i + 1];
                Byte bpp = fileData[readStart + 6];
                Int32 frWidth = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readStart + 7, 2, true);
                Int32 frHeight = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readStart + 9, 2, true);
                Int32 palLen = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, readStart + 0x0C, 2, true);

                Byte[] frPal = new Byte[palLen];
                Array.Copy(fileData, readStart + 0x1B, frPal, 0, palLen);
                Color[] frPalette = ColorUtils.ReadEightBitPalette(frPal, true);

                Int32 dataOffs = readStart + 0x1B + palLen;
                Int32 dataLen = readEnd - dataOffs;
                Byte[] frameDataCompr = new Byte[dataLen];
                Array.Copy(fileData, dataOffs, frameDataCompr, 0, dataLen);
                Int32 outputLen = frWidth * frHeight;
                Byte[] frameData = new Byte[outputLen];
                Array.Copy(frameDataCompr, 0, frameData, 0, Math.Min(dataLen, outputLen));
                /*/
                Int32 outptr = 0;
                for (Int32 c = 0; c < dataLen; ++c)
                {
                    Byte b = frameDataCompr[c];
                    if ((b & 0x80) != 0)
                    {
                        if (c + 1 == dataLen)
                            break;
                        Int32 curRepeatAmount = b & 0x7F;
                        Byte val = frameDataCompr[c++];
                        for (; curRepeatAmount > 0; --curRepeatAmount)
                        {

                            if (outptr == outputLen)
                                break;
                            frameData[outptr++] = val;
                        }
                    }
                    else
                    {
                        for (; b > 0; --b)
                        {
                            if (c == dataLen || outptr == outputLen)
                                break;
                            frameData[outptr++] = frameDataCompr[c++];
                        }
                    }
                }
                //if (frameData == null || frameData.Length == 0)
                //    throw new FileTypeLoadException("Not an ICG SLB file.");
                //Byte[] frameData2 = IgcRle.RleDecode(frameDataCompr, 0, null, false);
                //File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + "_frame" + i.ToString("000") + ".dat"), frameData2);
                //*/
                Bitmap curFrImg = ImageUtils.BuildImage(frameData, frWidth, frHeight, frWidth, PixelFormat.Format8bppIndexed, frPalette, null);
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, sourcePath, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetColorsInPalette(this.ColorsInPalette);
                framePic.SetExtraInfo("Compression has not yet been cracked! Sorry.");
                this.m_FramesList[i] = framePic;
            }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return null;
        }

    }

}