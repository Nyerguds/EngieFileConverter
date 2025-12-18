using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
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
        protected Boolean m_CommonPal;

        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public override Boolean FramesHaveCommonPalette { get { return this.m_CommonPal; } }
        public override Boolean HasCompositeFrame { get { return true; } }


        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Westwood Amiga Frames CPS"; } }
        public override String[] FileExtensions { get { return new String[] { "cps" }; } }
        public override String ShortTypeDescription { get { return "Westwood Amiga Frames CPS File"; } }
        public override Int32 ColorsInPalette { get { return this.HasPalette ? this.m_Palette.Length : 0; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            Int32 compression;
            Color[] palette;
            CpsVersion cpsVersion;
            Byte[] imageData = GetImageData(fileData, 0, filename, true, false, out compression, out palette, out cpsVersion);
            this.CompressionType = compression;
            this.CpsVersion = cpsVersion;
            this.HasPalette = true;
            if (palette == null)
                throw new FileTypeLoadException("Cannot identify palette!");
            m_Palette = palette;
            Int32 images = m_Palette.Length / 32;
            Int32 frWidth = 160;
            Int32 frHeight = 96;
            this.m_FramesList = new SupportedFileType[images];
            Color[] firstFramePal = null;
            Boolean equalPalettes = true;
            Byte[] testFrame = ImageUtils.CopyFrom8bpp(imageData, 320, 200, 320, new Rectangle(0, 192, 320, 8));
            Boolean expandBottomFrames = testFrame.Any(p => p != 0);
            for (Int32 i = 0; i < images; ++i)
            {
                Int32 index = i * 160;
                Int32 offsetX = index % 320;
                Int32 offsetY = index / 320 * frHeight;
                Color[] framePal = new Color[32];
                Array.Copy(m_Palette, i * 32, framePal, 0, 32);
                if (firstFramePal == null)
                    firstFramePal= framePal;
                else if (equalPalettes)
                    equalPalettes = framePal.SequenceEqual(firstFramePal);
                Int32 usedFrHeight = offsetY == 0 || !expandBottomFrames ? frHeight : 104;
                Byte[] frame = ImageUtils.CopyFrom8bpp(imageData, 320, 200, 320, new Rectangle(offsetX, offsetY, frWidth, usedFrHeight));
                Bitmap curFrImg = ImageUtils.BuildImage(frame, frWidth, usedFrHeight, frWidth, PixelFormat.Format8bppIndexed, framePal, null);
                curFrImg.Palette = BitmapHandler.GetPalette(framePal);

                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFileFrame(this, this, curFrImg, filename, i);
                framePic.SetBitsPerColor(this.BitsPerPixel);
                framePic.SetFileClass(this.FrameInputFileClass);
                framePic.SetColorsInPalette(this.ColorsInPalette);
                this.m_FramesList[i] = framePic;
            }
            m_CommonPal = equalPalettes;
            try
            {
                Byte[] combinedImageData = equalPalettes ? imageData : GetCombinedImage(imageData, images);
                if (equalPalettes)
                    this.m_Palette = firstFramePal;
                this.m_LoadedImage = ImageUtils.BuildImage(combinedImageData, this.Width, this.Height, this.Width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                this.m_LoadedImage.Palette = BitmapHandler.GetPalette(this.m_Palette);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new FileTypeLoadException("Cannot construct image from read data!", e);
            }
            this.SetExtraInfo();
            this.SetFileNames(filename);
        }

        protected Byte[] GetCombinedImage(Byte[] imageData, Int32 amigaPalCount)
        {
            // New array.
            Byte[] adjustedData = new Byte[imageData.Length];
            // EOB2 4-frames image
            Int32 frHeight = 96;
            // Test if the image data exceeds the normal bottom and goes into the 8-pixel strîp below.
            Byte[] bottomstrip = ImageUtils.CopyFrom8bpp(imageData, 320, 200, 320, new Rectangle(0, 192, 320, 8));
            Boolean expandBottomFrames = bottomstrip.Any(p => p != 0);
            // Combine images by making each one use a different 32-colour slice of the palette.
            for (Int32 i = 0; i < amigaPalCount; ++i)
            {
                Int32 palOffset = i * 32;
                Int32 index = i * 160;
                Int32 offsetX = index % 320;
                Int32 offsetXEnd = offsetX + 160;
                Int32 offsetY = index / 320 * 96;
                Int32 usedFrHeight = offsetY == 0 || !expandBottomFrames ? frHeight : 104;
                Int32 offsetYEnd = offsetY + usedFrHeight;

                Int32 offsetRow = offsetY * 320 + offsetX;
                for (Int32 y = offsetY; y < offsetYEnd; ++y)
                {
                    Int32 offset = offsetRow;
                    for (Int32 x = offsetX; x < offsetXEnd; ++x)
                    {
                        adjustedData[offset] = (Byte)(imageData[offset] + palOffset);
                        offset++;
                    }
                    offsetRow += 320;
                }
            }
            return adjustedData;
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            FileFramesWwCpsAmi4 cps = fileToSave as FileFramesWwCpsAmi4;
            Int32 compression = cps != null ? cps.CompressionType : 4;
            return new SaveOption[]
            {
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new NotSupportedException("File to save is empty!");
            SupportedFileType[] frames = fileToSave.Frames;
            if (!fileToSave.IsFramesContainer || frames == null || frames.Length == 0)
                throw new NotSupportedException("File to save has no frames!");
            if (frames.Length > 4)
                throw new NotSupportedException("This type can only save up to four frames!");
            
            // Save options
            Int32 compressionType;
            Int32.TryParse(SaveOption.GetSaveOptionValue(saveOptions, "CMP"), out compressionType);            

            // Extract frames + frame-specific checks
            Int32 nrOfFrames = fileToSave.Frames.Length;
            Color[] fullPalette = new Color[nrOfFrames*32];
            Byte[] imageData = new Byte[64000];
            for (Int32 c = 0; c < fullPalette.Length; ++c)
                fullPalette[c] = Color.Black;
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap frImage = frame.GetBitmap();
                Color[] frColors = frImage.Palette.Entries;
                if (frImage.PixelFormat != PixelFormat.Format8bppIndexed || frColors.Length == 0)
                    throw new NotSupportedException("Frame " + i + " is not 8-bit indexed!");
                Int32 width = frImage.Width;
                Int32 height = frImage.Height;
                Int32 curMax = i < 2 ? 96 : 104;
                if (width > 160 && height > curMax)
                    throw new NotSupportedException("Frame " + i + " does not fit in 160×" + curMax + "!");
                Int32 stride;
                Byte[] frData = ImageUtils.GetImageData(frImage, out stride, true);
                if (frData.Any(p => p >= 32))
                    throw new NotSupportedException("Pixels in frame " + i + " exceed index 32 on the palette!");
                Array.Copy(frColors, 0, fullPalette, i * 32, Math.Min(frColors.Length, 32));

                Int32 index = i * 160;
                Int32 offsetX = index % 320;
                Int32 offsetY = index / 320 * 96;
                ImageUtils.PasteOn8bpp(imageData, 320, 200, 320, frData, width, height, width, new Rectangle(offsetX, offsetY, width, height), null, true);
            }
            return SaveCps(imageData, fullPalette, nrOfFrames, compressionType, CpsVersion.AmigaEob2);
        }
    }
}