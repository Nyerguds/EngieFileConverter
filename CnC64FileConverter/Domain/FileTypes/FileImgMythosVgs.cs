using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImgMythosVgs: SupportedFileType
    {
        public const String PAL_IDENTIFIER = "VGA palette";
        public override Int32 Width { get { return 0; } }
        public override Int32 Height { get { return 0; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Mythos VGS"; } }
        public override String[] FileExtensions { get { return new String[] { "VGS" }; } }
        public override String ShortTypeDescription { get { return "Mythos frames file"; } }
        public override Int32 ColorsInPalette { get { return m_PaletteSet? this.m_Palette.Length : 0; } }
        public override Int32 BitsPerColor { get { return 8; } }
        protected List<SupportedFileType> m_FramesList = new List<SupportedFileType>();
        protected Boolean m_PaletteSet = false;

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }
        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            return new SaveOption[] { new SaveOption(SaveOptionType.Boolean, "Save palette into file", "0") }; 
        }

        public override void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            Color[] palette2 = new Color[256];
            Array.Copy(palette, palette2, Math.Min(256, palette.Length));
            for (Int32 i = palette.Length; i < 256; i++)
                palette2[i] = Color.Empty;
            palette2[255] = Color.FromArgb(0, palette[255]);
            base.SetColors(palette2, updateSource);
        }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFromFileData(fileData, null);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            LoadFromFileData(fileData, filename);
            SetFileNames(filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            // 01 00 06 00 00 00 00 01
            // W-1   H-1      CM X? Y
            if (fileData.Length < 0x8)
                throw new FileTypeLoadException("Not long enough for header.");
            Int32 offset = 0;
            m_FramesList.Clear();
            m_PaletteSet = false;
            m_Palette = null;
            // Read data
            while (offset < fileData.Length)
            {
                Int32 frameWidth = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset + 0, 2, true) + 1;
                Int32 frameHeight = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset + 2, 2, true) + 1;
                if ((frameWidth < 0 || frameHeight < 0) || (!(frameWidth == 0 && frameHeight == 0) && (frameWidth == 0 || frameHeight == 0)))
                    throw new FileTypeLoadException("Bad header data.");
                Int32 skipLen;
                Byte comprByte = fileData[offset + 5];
                Boolean compressed = comprByte != 0;
                Int32 xOffset = fileData[offset + 6];
                Int32 yOffset = fileData[offset + 7];
                offset += 8;
                Int32 dataLen = frameWidth * frameHeight;
                Byte[] imageData = new Byte[dataLen];
                if (compressed)
                {
                    if (comprByte != 1)
                        throw new FileTypeLoadException("Unknown compression type: " + comprByte);
                    skipLen = (Int16)ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true) - 8;
                }
                else
                {
                    skipLen = dataLen;
                }
                if (fileData.Length < offset + skipLen)
                    throw new FileTypeLoadException("header references offset outside file data.");

                if (compressed)
                {
                    // Is compressed. Doesn't actually work...
                    //Array.Copy(fileData, offset, imageData, 0, skipLen);
                    // TODO: LZW MAGIC! ...or not. Bah. Maybe it's just RLE?

                    // Draw a nice little "Nope" box instead...
                    for (int i = 0; i < imageData.Length; i++)
                        imageData[i] = 0;
                    Byte drawColor = 0xFF;
                    Int32 crossDim = Math.Min(frameHeight, frameWidth);
                    Int32 skipW = (frameWidth - crossDim) / 2;
                    Int32 skipH = (frameHeight - crossDim) / 2;
                    for (Int32 y = 0; y < frameHeight; y++)
                    {
                        for (Int32 x = 0; x < frameWidth; x++)
                            if (
                                (x - skipW == y - skipH) || // diagonal '\'
                                (crossDim - x + skipW - 1 == y - skipH) || // diagonal '/'
                                (x == 0) || // line left
                                (y == 0) || // line top
                                (x == frameWidth - 1) || // line right
                                (y == frameHeight - 1) // line bottom
                                )
                                imageData[y * frameWidth + x] = drawColor;
                    }
                }
                else
                {
                    Array.Copy(fileData, offset, imageData, 0, dataLen);
                    // Detect palette on first frame
                    if (!this.m_PaletteSet && this.m_FramesList.Count == 0 && (imageData.Length == PAL_IDENTIFIER.Length + 0x301))
                    {
                        Byte[] compare = new Byte[PAL_IDENTIFIER.Length];
                        Array.Copy(fileData, offset, compare, 0, PAL_IDENTIFIER.Length);
                        // "VGA palette" followed by byte 0x1A identifies as palette.
                        if (compare.SequenceEqual(Encoding.ASCII.GetBytes(PAL_IDENTIFIER)) && imageData[PAL_IDENTIFIER.Length] == 0x1A)
                        {
                            // Palette found! Extract palette and skip frame so it doesn't get added to the list.
                            Byte[] paletteData = new Byte[0x300];
                            Array.Copy(imageData, PAL_IDENTIFIER.Length + 1, paletteData, 0, 0x300);
                            this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(paletteData));
                            this.m_PaletteSet = true;
                            offset += skipLen;
                            continue;
                        }
                    }
                    if (xOffset > 0 || yOffset > 0)
                    {
                        Int32 newWidth = frameWidth + xOffset;
                        Int32 newHeight = frameHeight + yOffset;
                        Byte[] adjustedData = new Byte[newWidth * newHeight];
                        for (Int32 i = 0; i < adjustedData.Length; i++)
                            adjustedData[i] = 0xFF;
                        // Last color is seen as transparent on these images.
                        Color[] transparencyGuide = Enumerable.Repeat(Color.White, 256).ToArray();
                        transparencyGuide[255] = Color.Transparent;
                        ImageUtils.PasteOn8bpp(adjustedData, newWidth, newHeight, newWidth,
                            imageData, frameWidth, frameHeight, frameWidth,
                            new Rectangle(xOffset, yOffset, frameWidth, frameHeight), transparencyGuide, true);
                        imageData = adjustedData;
                        frameWidth = newWidth;
                        frameHeight = newHeight;
                    }
                }
                if (this.m_Palette == null)
                    this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
                Bitmap curImage = ImageUtils.BuildImage(imageData, frameWidth, frameHeight, frameWidth, PixelFormat.Format8bppIndexed, m_Palette, null);
                // TODO CALL FRAME CREATION

                FileImageFrameMythosVgs frame = new FileImageFrameMythosVgs();
                frame.LoadFileFrame(this, this.ShortTypeName, curImage, sourcePath, this.m_FramesList.Count);
                frame.SetColorsInPalette(this.m_PaletteSet ? this.m_Palette.Length : 0);
                frame.SetColors(this.m_Palette);

                this.m_FramesList.Add(frame);
                offset += skipLen;
            }
            if (offset != fileData.Length)
                throw new FileTypeLoadException("Image load failed.");
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            return SaveToBytes(fileToSave, saveOptions, dontCompress);
        }

        protected static Byte[] SaveToBytes(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave.Frames == null)
                throw new NotSupportedException("Mythos VGS saving for single frame is not supported!");
            if (fileToSave.Frames.Length == 0)
                throw new NotSupportedException("No frames found in source data!");
            Boolean asPaletted = GeneralUtils.IsTrueValue(saveOptions[0].SaveData, false);
            Color[] palette = null;
            foreach (SupportedFileType frame in fileToSave.Frames)
            {
                if (frame == null || frame.GetBitmap() == null)
                    throw new NotSupportedException("Mythos VGS can't handle empty frames!");
                Bitmap image = fileToSave.GetBitmap();
                if (image.PixelFormat != PixelFormat.Format8bppIndexed && image.PixelFormat != PixelFormat.Format4bppIndexed)
                    throw new NotSupportedException("Mythos VGS requires 8-bit frames!");
                // Take first frame's palette as colours.
                if (asPaletted && palette == null)
                    palette = image.Palette.Entries;
            }
            Int32 actualLen = fileToSave.Frames.Length;
            if (asPaletted)
                actualLen++;
            Byte[][] frameData = new Byte[actualLen][];
            Int32[] widths = new Int32[actualLen];
            Int32[] heighths = new Int32[actualLen];
            Byte[] xOffsets = new Byte[actualLen];
            Byte[] yOffsets = new Byte[actualLen];
            if (asPaletted)
            {
                // Don't think this can happen at this point.
                if (palette == null)
                    throw new NotSupportedException("There is no palette available in the given frames!");
                // Add extra frame with palette header to serve as internal palette.
                Byte[] frameBytes = new Byte[780];
                Array.Copy(Encoding.ASCII.GetBytes(PAL_IDENTIFIER), 0, frameBytes, 0, PAL_IDENTIFIER.Length);
                frameBytes[PAL_IDENTIFIER.Length] = 0x1A;
                Byte[] colorBytes = ColorUtils.GetSixBitPaletteData(ColorUtils.GetSixBitColorPalette(palette));
                Array.Copy(colorBytes, 0, frameBytes, PAL_IDENTIFIER.Length + 1, colorBytes.Length);
                frameData[0] = frameBytes;
                widths[0] = 390;
                heighths[0] = 2;
                xOffsets[0] = 0;
                yOffsets[0] = 0;
            }
            for (Int32 i = asPaletted ? 1 : 0; i < actualLen; i++)
            {
                SupportedFileType frame = fileToSave.Frames[asPaletted ? i - 1 : i];
                Int32 stride;
                frameData[i] = ImageUtils.GetImageData(frame.GetBitmap(), out stride);
                widths[i] = frame.Width;
                heighths[i] = frame.Height;                
                // Optimise Y offset here
                //yOffsets[i] = (Byte)frame.YOffset;
            }
            Byte[] finalData = new Byte[actualLen * 8 + frameData.Sum(sd => sd.Length)];
            Int32 offset = 0;
            for (Int32 i = 0; i < actualLen; i++)
            {
                ArrayUtils.WriteIntToByteArray(finalData, offset + 0, 2, true, (UInt32)(widths[i] - 1));
                ArrayUtils.WriteIntToByteArray(finalData, offset + 2, 2, true, (UInt32)(heighths[i]-1));
                finalData[offset + 6] = xOffsets[i];
                finalData[offset + 7] = yOffsets[i];
                offset += 8;
                Byte[] curSymbolData = frameData[i];
                Array.Copy(curSymbolData, 0, finalData, offset, curSymbolData.Length);
                offset += curSymbolData.Length;
            }
            return finalData;
        }
    }

    public class FileImageFrameMythosVgs: FileImageFrame
    {

        public override void SetColors(Color[] palette, SupportedFileType updateSource)
        {
            Color[] palette2 = new Color[256];
            Array.Copy(palette, palette2, Math.Min(256, palette.Length));
            for (Int32 i = palette.Length; i < 256; i++)
                palette2[i] = Color.Empty;
            palette2[255] = Color.FromArgb(0, palette[255]);
            base.SetColors(palette2, updateSource);
        }
    }
}

