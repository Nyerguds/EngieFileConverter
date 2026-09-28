using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileFramesWwCpsAmi4 : FileImgWwCps
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        protected SupportedFileType[] m_FramesList;
        protected bool m_CommonPal;

        public override string IdCode { get { return "WwCpsAmi4"; } }
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public override bool FramesHaveCommonPalette { get { return this.m_CommonPal; } }
        public override bool HasCompositeFrame { get { return true; } }


        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Westwood Amiga 4-Frames CPS"; } }
        public override string[] FileExtensions { get { return new string[] { "cps" }; } }
        public override string LongTypeName { get { return "Westwood Amiga 4-Frames CPS File"; } }
        public override int BitsPerPixel { get { return 8; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {

            byte[] imageData = GetImageData(fileData, 0, fileData.Length, filename, true, false,
                out int compression, out Color[] palette, out CpsVersion cpsVersion, out int colorDepth, out int width, out int height);
            this.CompressionType = compression;
            this.CpsVersion = cpsVersion;
            m_ColorFormat = colorDepth;
            m_Width = width;
            m_Height = height;
            if (palette == null)
                throw new FileTypeLoadException("Cannot identify palette.");
            this.m_LoadedPalette = filename;
            this.m_Palette = palette;
            int images = this.m_Palette.Length / 32;
            int frWidth = 160;
            int frHeight = 96;
            this.m_FramesList = new SupportedFileType[images];
            Color[] firstFramePal = null;
            bool equalPalettes = true;
            byte[] testFrame = ImageUtils.CopyFrom8bpp(imageData, 320, 200, 320, new Rectangle(0, 192, 320, 8));
            bool expandBottomFrames = testFrame.Any(p => p != 0);
            for (int i = 0; i < images; ++i)
            {
                int index = i * 160;
                int offsetX = index % 320;
                int offsetY = index / 320 * frHeight;
                Color[] framePal = new Color[32];
                Array.Copy(this.m_Palette, i * 32, framePal, 0, 32);
                if (firstFramePal == null)
                    firstFramePal= framePal;
                else if (equalPalettes)
                    equalPalettes = framePal.SequenceEqual(firstFramePal);
                int usedFrHeight = offsetY == 0 || !expandBottomFrames ? frHeight : 104;
                byte[] frame = ImageUtils.CopyFrom8bpp(imageData, 320, 200, 320, new Rectangle(offsetX, offsetY, frWidth, usedFrHeight));
                Bitmap curFrImg = ImageUtils.BuildImage(frame, frWidth, usedFrHeight, frWidth, PixelFormat.Format8bppIndexed, framePal, null);
                curFrImg.Palette = ImageUtils.GetPalette(framePal);

                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, filename, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetNeedsPalette(this.NeedsPalette);
                this.m_FramesList[i] = framePic;
            }
            this.m_CommonPal = equalPalettes;
            try
            {
                byte[] combinedImageData = equalPalettes ? imageData : this.GetCombinedImage(imageData, images);
                if (equalPalettes)
                    this.m_Palette = firstFramePal;
                this.m_LoadedImage = ImageUtils.BuildImage(combinedImageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                this.m_LoadedImage.Palette = ImageUtils.GetPalette(this.m_Palette);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FileTypeLoadException("Cannot construct image from read data.", e);
            }
            this.SetExtraInfo(null);
            this.SetFileNames(filename);
        }

        protected byte[] GetCombinedImage(byte[] imageData, int amigaPalCount)
        {
            // New array.
            byte[] adjustedData = new byte[imageData.Length];
            // EOB2 4-frames image
            int frHeight = 96;
            // Test if the image data exceeds the normal bottom and goes into the 8-pixel strîp below.
            byte[] bottomstrip = ImageUtils.CopyFrom8bpp(imageData, 320, 200, 320, new Rectangle(0, 192, 320, 8));
            bool expandBottomFrames = bottomstrip.Any(p => p != 0);
            // Combine images by making each one use a different 32-color slice of the palette.
            for (int i = 0; i < amigaPalCount; ++i)
            {
                int palOffset = i * 32;
                int index = i * 160;
                int offsetX = index % 320;
                int offsetXEnd = offsetX + 160;
                int offsetY = index / 320 * 96;
                int usedFrHeight = offsetY == 0 || !expandBottomFrames ? frHeight : 104;
                int offsetYEnd = offsetY + usedFrHeight;

                int offsetRow = offsetY * 320 + offsetX;
                for (int y = offsetY; y < offsetYEnd; ++y)
                {
                    int offset = offsetRow;
                    for (int x = offsetX; x < offsetXEnd; ++x)
                    {
                        adjustedData[offset] = (byte)(imageData[offset] + palOffset);
                        offset++;
                    }
                    offsetRow += 320;
                }
            }
            return adjustedData;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            this.PerformPreliminaryChecks(fileToSave);
            FileFramesWwCpsAmi4 cps = fileToSave as FileFramesWwCpsAmi4;
            int compression = cps != null ? cps.CompressionType : 4;
            return new Option[]
            {
                new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave);
            // Save options
            int compressionType;
            Int32.TryParse(Option.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);

            // Extract frames + frame-specific checks
            int nrOfFrames = fileToSave.Frames.Length;
            Color[] fullPalette = new Color[nrOfFrames*32];
            byte[] imageData = new byte[64000];
            for (int c = 0; c < fullPalette.Length; ++c)
                fullPalette[c] = Color.Black;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap frImage = frame.GetBitmap();
                Color[] frColors = frImage.Palette.Entries;
                if (frImage.PixelFormat != PixelFormat.Format8bppIndexed || frColors.Length == 0)
                    throw new ArgumentException("Frame " + i + " is not 8-bit indexed.", "fileToSave");
                int width = frImage.Width;
                int height = frImage.Height;
                int curMax = i < 2 ? 96 : 104;
                if (width > 160 && height > curMax)
                    throw new ArgumentException("Frame " + i + " does not fit in 160×" + curMax + ".", "fileToSave");
                int stride;
                byte[] frData = ImageUtils.GetImageData(frImage, out stride, true);
                if (frData.Any(p => p >= 32))
                    throw new ArgumentException("Pixels in frame " + i + " exceed index 32 on the palette.", "fileToSave");
                Array.Copy(frColors, 0, fullPalette, i * 32, Math.Min(frColors.Length, 32));

                int index = i * 160;
                int offsetX = index % 320;
                int offsetY = index / 320 * 96;
                ImageUtils.PasteOn8bpp(imageData, 320, 200, 320, frData, width, height, width, new Rectangle(offsetX, offsetY, width, height), null, true);
            }
            return SaveCps(imageData, fullPalette, nrOfFrames, compressionType, CpsVersion.AmigaEob2);
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] {fileToSave};
            if (frames == null || frames.Length == 0)
                throw new ArgumentException(ERR_FRAMES_NEEDED, "fileToSave");
            int nrOfFrames = frames.Length;
            if (nrOfFrames > 4)
                throw new ArgumentException("This type can only save up to four frames.", "fileToSave");
            return frames;
        }
    }
}