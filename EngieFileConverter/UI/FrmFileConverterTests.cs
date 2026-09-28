#if DEBUG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Nyerguds.FileData.EmotionalPictures;
// ReSharper disable UnusedMember.Local

namespace EngieFileConverter.UI
{
    /// <summary>
    /// To anyone who sees this, hello, and welcome to Nyerguds's random experiments and test code! This code only compiles in Debug mode,
    /// and is linked to the [Edit] -> [Test bed] menu item (tsmiTestBed) through the TsmiTestBedClick function. Typically, only one of the
    /// below functions is called. I keep them all here because they often contain interesting code, but I don't want to pollute the main
    /// source file of FrmFileConverter with them.
    /// Note that some code that is referenced here might be from the Domain\Utils\UtilsSO.cs or Domain\Utils\ImageUtils\ImageUtilsSO.cs
    /// class, which, like this one, do not compile in Release mode.
    /// </summary>
    partial class FrmFileConverter
    {
        private void ExecuteTestCode()
        {
            // any test code can be linked in here.
            //this.ViewInt33MouseCursors();
            //this.MatrixImage();
            //this.LoadByteArrayImage();
            //this.CombineHue();
            //this.CreateSierpinskiImage();
            //this.ColorPsx();
            //this.ExpandRAMap();
            //this.GetColorPixel();
            //this.ExtractInts();
            //this.DecompressPppStringsFiles();
            //this.PixelsToPalette();
            //this.GetIco();
            //this.FixPngAspectRatio();
            //this.MakeBorderIcon();
            //this.DetectBlobs();
            //this.IndexedToArgb();
            //this.WriteIcoFileFromFrames();
            //this.ChromaKey();
            //this.ChromaKey2();
            //this.TestSplit();
            //this.BayerGridtoGray();
            //this.BuildBayer();
            //this.Reduce12Bit();
            //this.CombineImages();
            //this.MakePatterns();
            //this.ExtractBlack();
            //this.MakeTrans();
            //this.CombineVertical();
            //this.ConvertToIcons();
            //this.ViewSTrisHidden();
            //this.Ikegami();
            //this.SplitTwoColor();
            //this.LoadToClip();
            //this.ReplaceImagePalette();
            //this.AutoRemapPalette();
            //this.SwapColors();
            //this.MakeIcons();
            //this.ExtractBitmaps();
            //this.ShiftMap();
            //this.ListHighVerDotWriterFonts();
            //this.CountPixels();
            //this.ExpandGif();
            //this.DecryptDat();
            //this.ExecuteThreaded(this.ReduceRPlace, false, false, false, "Reducing palettes...");
            //this.ExecuteThreaded(this.GetDataFromImage, false, false, false, "Getting image...");
            //this.ExecuteThreaded(this.InvertIndices, false, false, false, "Getting image...");
            //this.ExecuteThreaded(() => this.CorrectHue(this.m_LoadedFile, Color.FromArgb(0x00, 0xFF, 0x00).GetHue(), 15.0, 0.2), false, false, false, "Matching to hue...");
            //this.ExecuteThreaded(() => this.CorrectAlpha(this.m_LoadedFile), false, false, false, "Applying alpha...");
            //this.ExecuteThreaded(() => this.ConvertDiff(this.m_LoadedFile), false, false, false, "Removing differences...");
            //this.ExecuteThreaded(() => this.Fix6BitPalette(this.m_LoadedFile), false, false, false, "Fixing palette...");
            this.SwapPaletteColors();
        }

        private void LoadTestFile(SupportedFileType loadImage)
        {
            this.ReloadWithDispose(loadImage, true, true, true);
        }

        private void LoadTestFile(Bitmap loadImage, string filename, string extraInfo)
        {
            if (!filename.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                filename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".png");

            FileImagePng fileImage = new FileImagePng();
            using (MemoryStream ms = new MemoryStream())
            {
                loadImage.Save(ms, ImageFormat.Png);

                fileImage.LoadFile(ms.ToArray(), filename);
                if (extraInfo != null)
                    fileImage.ExtraInfo = extraInfo;
            }
            this.LoadTestFile(fileImage);
        }

        private void LoadTestFile(Bitmap loadImage)
        {
            this.LoadTestFile(loadImage, ".\\image.png", null);
        }

        private void LoadTestFile(Bitmap loadImage, string extraInfo)
        {
            this.LoadTestFile(loadImage, ".\\image.png", extraInfo);
        }

        private void ViewInt33MouseCursors()
        {
            //*/
            // Cursors data from the KORT.EXE of the King Arthur's K.O.R.T. game.
            byte[] int33MouseCursorKort = new byte[]
            {
                0xFF, 0x1F, 0xFF, 0x0F, 0xFF, 0x07, 0xFF, 0x03, 0xFF, 0x01, 0xFF, 0x00, 0x7F, 0x00, 0x3F, 0x00,
                0x1F, 0x00, 0x3F, 0x00, 0xFF, 0x01, 0xFF, 0x01, 0xFF, 0xE0, 0xFF, 0xF0, 0xFF, 0xF8, 0xFF, 0xF8,
                0x00, 0x00, 0x00, 0x40, 0x00, 0x60, 0x00, 0x70, 0x00, 0x78, 0x00, 0x7C, 0x00, 0x7E, 0x00, 0x7F,
                0x80, 0x7F, 0x00, 0x7C, 0x00, 0x4C, 0x00, 0x06, 0x00, 0x06, 0x00, 0x03, 0x00, 0x03, 0x00, 0x00,

                0xF0, 0xFF, 0xE0, 0xFF, 0xC0, 0xFF, 0x81, 0xFF, 0x03, 0xFF, 0x07, 0x06, 0x0F, 0x00, 0x1F, 0x00,
                0x3F, 0x80, 0x7F, 0xC0, 0xFF, 0xE0, 0xFF, 0xF1, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0x00, 0x00, 0x06, 0x00, 0x0C, 0x00, 0x18, 0x00, 0x30, 0x00, 0x60, 0x00, 0xC0, 0x70, 0x80, 0x39,
                0x00, 0x1F, 0x00, 0x0E, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                0x1F, 0xF0, 0x0F, 0xE0, 0x07, 0xC0, 0x03, 0x80, 0x41, 0x04, 0x61, 0x0C, 0x81, 0x03, 0x81, 0x03,
                0x81, 0x03, 0x61, 0x0C, 0x41, 0x04, 0x03, 0x80, 0x07, 0xC0, 0x0F, 0xE0, 0x10, 0xF0, 0xFF, 0xFF,
                0x00, 0x00, 0xC0, 0x07, 0x20, 0x09, 0x10, 0x11, 0x08, 0x21, 0x04, 0x40, 0x04, 0x40, 0x3C, 0x78,
                0x04, 0x40, 0x04, 0x40, 0x08, 0x21, 0x10, 0x11, 0x20, 0x09, 0xC0, 0x07, 0x00, 0x00, 0x00, 0x00,

                0xFF, 0xF3, 0xFF, 0xE1, 0xFF, 0xE1, 0xFF, 0xE1, 0xFF, 0xE1, 0x49, 0xE0, 0x00, 0xE0, 0x00, 0x80,
                0x00, 0x00, 0x00, 0x00, 0xFC, 0x07, 0xF8, 0x07, 0xF9, 0x9F, 0xF1, 0x8F, 0x03, 0xC0, 0x00, 0xE0,
                0x00, 0x0C, 0x00, 0x12, 0x00, 0x12, 0x00, 0x12, 0x00, 0x12, 0xB6, 0x13, 0x49, 0x12, 0x49, 0x72,
                0x49, 0x92, 0x01, 0x90, 0x01, 0x90, 0x01, 0x80, 0x02, 0x40, 0x02, 0x40, 0x04, 0x20, 0xF8, 0x1F,

                0xFF, 0x8F, 0xFF, 0x07, 0xFF, 0x03, 0xFF, 0x01, 0xFB, 0x80, 0x71, 0xC0, 0x31, 0xE0, 0x11, 0xF0,
                0x01, 0xF8, 0x03, 0xFC, 0x07, 0xFE, 0x03, 0xFF, 0x01, 0xF8, 0x20, 0xF0, 0x70, 0xF8, 0xF9, 0xFF,
                0x00, 0x00, 0x00, 0x70, 0x00, 0x78, 0x00, 0x5C, 0x00, 0x2E, 0x04, 0x17, 0x84, 0x0B, 0xC4, 0x05,
                0xEC, 0x02, 0x78, 0x01, 0xB0, 0x00, 0x68, 0x00, 0xD4, 0x00, 0x8A, 0x07, 0x04, 0x00, 0x00, 0x00,

                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0x00, 0x00, 0x6A, 0x69, 0x4C, 0x49, 0x4C, 0x49, 0x6A, 0x6D, 0x00, 0x00, 0xE0, 0x0E, 0xA0, 0x04,
                0xA0, 0x04, 0xE0, 0x04, 0x00, 0x00, 0xAE, 0x6E, 0xA4, 0x4A, 0xE4, 0x4A, 0xA4, 0x6E, 0x00, 0x00,
            };
            /*/
            // Cursors data from the INSTALL.EXE of the Exhumed game.
            Byte[] int33MouseCursorExhumed = new Byte[]
            {
                0x3F, 0xF8, 0x0F, 0xE0, 0x07, 0xC0, 0x83, 0x83, 0xC3, 0x87, 0xC3, 0x87, 0x87, 0xC3, 0x0F, 0xE1,
                0x1F, 0xF0, 0x03, 0x80, 0x03, 0x80, 0x03, 0x80, 0x3F, 0xF8, 0x3F, 0xF8, 0x3F, 0xF8, 0x3F, 0xF8,
                0x00, 0x00, 0xC0, 0x07, 0x70, 0x1C, 0x38, 0x38, 0x18, 0x30, 0x18, 0x30, 0x30, 0x18, 0x60, 0x0C,
                0xC0, 0x06, 0x80, 0x03, 0xF8, 0x3F, 0x80, 0x03, 0x80, 0x03, 0x80, 0x03, 0x80, 0x03, 0x00, 0x00,
            };
            /*/
            byte[] int33MouseCursor = int33MouseCursorKort;
            /*/
            Byte[] int33MouseCursor = int33MouseCursorExhumed;
            //*/

            Color[] palette = new Color[4];
            palette[0] = Color.Black;
            palette[1] = Color.FromArgb(0, Color.Fuchsia);
            palette[2] = Color.White;
            palette[3] = Color.Red;
            int frames = int33MouseCursor.Length / 64;
            int fullWidth = frames * 16;
            int fullHeight = 16;
            int fullStride = frames * 16;
            byte[] fullImage = new byte[fullHeight * fullStride];
            FileFrames framesContainer = new FileFrames();
            for (int i = 0; i < frames; ++i)
            {
                int start = i * 64;
                int start2 = start + 32;
                byte[] curImage1 = new byte[32];
                for (int j = 0; j < 32; j += 2)
                {
                    curImage1[j] = int33MouseCursor[start + j + 1];
                    curImage1[j + 1] = int33MouseCursor[start + j];
                }
                int stride1 = 2;
                curImage1 = ImageUtils.ConvertTo8Bit(curImage1, 16, 16, 0, 1, true, ref stride1);

                byte[] curImage2 = new byte[32];
                for (int j = 0; j < 32; j += 2)
                {
                    curImage2[j] = int33MouseCursor[start2 + j + 1];
                    curImage2[j + 1] = int33MouseCursor[start2 + j];
                }
                int stride2 = 2;
                curImage2 = ImageUtils.ConvertTo8Bit(curImage2, 16, 16, 0, 1, true, ref stride2);

                byte[] imageFinal = new byte[256];
                int strideFinal = 16;
                for (int j = 0; j < 256; ++j)
                {
                    imageFinal[j] = (byte)((curImage2[j] << 1) | curImage1[j]);
                }
                StringBuilder sb = new StringBuilder();
                using (MemoryStream ms = new MemoryStream(int33MouseCursor))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    ms.Position = 64 * i;
                    for (int j = 0; j < 32; ++j)
                    {
                        if (j == 16)
                            sb.Append('\n');
                        ushort line = br.ReadUInt16();
                        sb.AppendFormat(" {0:X04} ", line).Append(Convert.ToString(line, 2).PadLeft(16, '0').Replace("0", "_").Replace("1", "X")).Append("\n");
                    }
                }

                ImageUtils.PasteOn8bpp(fullImage, fullWidth, fullHeight, fullStride, imageFinal, 16, 16, strideFinal, new Rectangle(i * 16, 0, 16, 16), null, true);
                imageFinal = ImageUtils.ConvertFrom8Bit(imageFinal, 16, 16, 4, true, ref strideFinal);
                Bitmap frameImage = ImageUtils.BuildImage(imageFinal, 16, 16, strideFinal, PixelFormat.Format4bppIndexed, palette, Color.Empty);
                frameImage.Palette = ImageUtils.GetPalette(palette);

                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(framesContainer, "Cursor", frameImage, "Cursor" + i.ToString("D3") + ".png", -1);
                frame.SetBitsPerColor(2);
                frame.SetFileClass(FileClass.Image4Bit);
                frame.SetNeedsPalette(false);
                framesContainer.AddFrame(frame);
                frame.SetExtraInfo(sb.ToString().TrimEnd('\n'));
            }
            int factor = 4;
            Bitmap composite;
            if (factor != 1)
            {
                int fullWidthF = fullWidth * factor;
                int fullHeightF = fullHeight * factor;
                int fullStrideF = fullHeightF;
                byte[] fullImageF = new byte[fullStrideF * fullHeightF];
                int readLine = 0;
                int writeLine = 0;
                int prevWriteLine = 0;
                for (int y = 1; y <= fullHeightF; ++y)
                {
                    int curWriteLine = writeLine;
                    if (y % factor == 0)
                    {
                        int curReadLine = readLine;
                        for (int x = 0; x < fullWidthF; ++x)
                            fullImageF[curWriteLine + x] = fullImage[curReadLine + x / 4];
                        readLine += fullStride;
                    }
                    else
                    {
                        Array.Copy(fullImageF, prevWriteLine, fullImageF, curWriteLine, fullStrideF);
                    }
                    prevWriteLine = writeLine;
                    writeLine += fullStrideF;
                }
                fullImageF = ImageUtils.ConvertFrom8Bit(fullImageF, fullWidthF, fullHeightF, 4, true, ref fullStrideF);
                composite = ImageUtils.BuildImage(fullImageF, fullWidthF, fullHeightF, fullStrideF, PixelFormat.Format4bppIndexed, palette, Color.Empty);
            }
            else
            {
                fullImage = ImageUtils.ConvertFrom8Bit(fullImage, fullWidth, fullHeight, 4, true, ref fullStride);
                composite = ImageUtils.BuildImage(fullImage, fullWidth, fullHeight, fullStride, PixelFormat.Format4bppIndexed, palette, Color.Empty);
            }
            composite.Palette = ImageUtils.GetPalette(palette);
            framesContainer.SetCompositeFrame(composite);
            framesContainer.SetBitsPerPixel(2);
            framesContainer.SetCommonPalette(true);
            framesContainer.SetPalette(palette);
            this.LoadTestFile(framesContainer);
        }

        private void CreateSierpinskiImage()
        {
            using (Bitmap sierpinskiImage = ImageUtilsSO.GetSierpinski(800, 800))
                this.LoadTestFile(sierpinskiImage);
        }

        private void LoadByteArrayImage()
        {
            byte[] imageBytes =
            {
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80,
                0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80
            };
            Color[] palette = new Color[0x100];
            for (int i = 0; i < 0x100; ++i)
                palette[i] = Color.FromArgb(i, i, i);
            using (Bitmap img = ImageUtils.BuildImage(imageBytes, 10, 20, 10, PixelFormat.Format8bppIndexed, palette, null))
                this.LoadTestFile(img);
        }

        private void CombineHue()
        {
            if (this.m_LoadedFile == null || !this.m_LoadedFile.IsFramesContainer || this.m_LoadedFile.Frames.Length < 2)
                return;

            // image data
            Bitmap im = this.m_LoadedFile.Frames[0].GetBitmap();
            // color data
            Bitmap col = this.m_LoadedFile.Frames[1].GetBitmap();
            if (im == null || col == null || im.Width != col.Width || im.Height != col.Height)
                return;
            int iStride;
            byte[] imageData = ImageUtils.GetImageData(im, out iStride, PixelFormat.Format32bppArgb);
            int cStride;
            byte[] colorData = ImageUtils.GetImageData(col, out cStride, PixelFormat.Format32bppArgb);
            if (imageData.Length != colorData.Length || iStride != cStride)
                return;
            bool isGray = true;
            for (int i = 0; i < imageData.Length; i += 4)
            {
                byte first = imageData[i];
                if (first != imageData[i + 1] || first != imageData[i + 2])
                {
                    isGray = false;
                    break;
                }
            }
            if (!isGray)
            {
                byte[] tmp = imageData;
                int tmpStride = iStride;
                imageData = colorData;
                iStride = cStride;
                colorData = tmp;
                cStride = tmpStride;
            }
            for (int i = 0; i < imageData.Length; i += 4)
            {
                Color curPix = Color.FromArgb(ArrayUtils.ReadInt32FromByteArrayLe(imageData, i));
                Color curCol = Color.FromArgb(ArrayUtils.ReadInt32FromByteArrayLe(colorData, i));
                ColorToHSV(curPix, out _, out _, out double value);
                ColorToHSV(curCol, out double hue, out double sat, out double _);
                // Color newCol = new ColorHSL(curCol.GetHue(), curCol.GetSaturation(), curPix.GetBrightness(), curPix.A);
                Color newCol = ColorFromHSV(hue, sat, value);
                uint val = (uint)newCol.ToArgb();
                ArrayUtils.WriteUInt32ToByteArrayLe(imageData, i, val);
            }
            using (Bitmap img = ImageUtils.BuildImage(imageData, im.Width, im.Height, iStride, PixelFormat.Format32bppArgb, null, null))
                this.LoadTestFile(img);
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        private void ColorPsx()
        {
            string filenameImage = "SCA01EA_cutout.BIN";
            if (!File.Exists(filenameImage))
                return;
            byte[] imageData = File.ReadAllBytes(filenameImage);
            Color[] palette = PaletteUtils.GenerateRainbowPalette(8, -1, null, true, 0, 240, true);
            using (Bitmap img = ImageUtils.BuildImage(imageData, 2048, 128, 2048, PixelFormat.Format8bppIndexed, palette, null))
                this.LoadTestFile(img);
        }

        private void ExpandRAMap()
        {
            string file = "SCG01EA";
            string ext = ".INI";

            string finalFile = file + ext;
            if (!File.Exists(finalFile))
                return;
            IniFile ramap = new IniFile(finalFile);
            int lineNr = 1;
            //String packedSection = "MapPack";
            string packedSection = "OverlayPack";
            Dictionary<string, string> sectionValues = ramap.GetSectionContent(packedSection);
            StringBuilder sb = new StringBuilder();
            while (sectionValues.ContainsKey(lineNr.ToString()))
            {
                sb.Append(sectionValues[lineNr.ToString()]);
                lineNr++;
            }
            byte[] compressedMap = Convert.FromBase64String(sb.ToString());
            int readPtr = 0;
            int writePtr = 0;
            byte[] mapFile = new byte[128 * 128 * 3];

            while (readPtr + 4 <= compressedMap.Length)
            {
                uint uLength = ArrayUtils.ReadUInt32FromByteArrayLe(compressedMap, readPtr);
                int length = (int)(uLength & 0xDFFFFFFF);
                readPtr += 4;
                byte[] dest = new byte[8192];
                int readPtr2 = readPtr;
                int decompressed = Nyerguds.FileData.Westwood.WWCompression.LcwDecompress(compressedMap, ref readPtr2, dest, 0);
                Array.Copy(dest, 0, mapFile, writePtr, decompressed);
                readPtr += length;
                writePtr += decompressed;
            }
            File.WriteAllBytes(file + "." + packedSection, mapFile);
            /*/
            // Align from 24 to 32 bit
            Byte[] mapFile2 = new Byte[128 * 128 * 16];
            writePtr = 0;
            for (Int32 i = 0; i < mapFile.Length; i += 3)
            {
                writePtr += 8;
                mapFile2[writePtr++] = mapFile[i];
                mapFile2[writePtr++] = mapFile[i + 1];
                mapFile2[writePtr++] = mapFile[i + 2];
                writePtr += 5;
            }
            File.WriteAllBytes("SCA01EA_expanded16.BIN", mapFile2);
            //*/
        }

        private void MatrixImage()
        {
            byte[] matrix =
            {
                0x00, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C, 0x0E,
                0x10, 0x12, 0xFF, 0x16, 0x18, 0xFF, 0x1C, 0x1E,
                0x20, 0x22, 0xFF, 0x26, 0x28, 0xFF, 0x2C, 0x2E,
                0x30, 0x32, 0x34, 0x36, 0x38, 0x3A, 0x3C, 0x3E,
                0x40, 0xFF, 0x44, 0x46, 0x48, 0x4A, 0xFF, 0x4E,
                0x50, 0x52, 0xFF, 0x56, 0x58, 0xFF, 0x5C, 0x5E,
                0x60, 0x62, 0x64, 0xFF, 0xFF, 0x6A, 0x6C, 0x6E,
                0x70, 0x72, 0x74, 0x76, 0x78, 0x7A, 0x7C, 0x7E,
            };
            using (Bitmap img = ImageUtils.BuildImage(matrix, 8, 8, 8, PixelFormat.Format8bppIndexed, PaletteUtils.GenerateGrayPalette(8, null, false), null))
                this.LoadTestFile(img);
        }

        private void GetColorPixel()
        {
            Bitmap bm;
            if (this.m_LoadedFile == null || (bm = this.m_LoadedFile.GetBitmap()) == null || (bm.PixelFormat & PixelFormat.Indexed) == 0)
                return;
            int x = 120;
            int y = 96;
            byte pixel = ImageUtilsSO.GetIndexedPixel(bm, x, y);
            MessageBox.Show(this, "The index of pixel [" + x + "," + y + "] is " + pixel + ".", GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void ExtractInts()
        {
            string s = "some text here = 5\nanother text line here = 4 with random garbage\n7\nfoo bar 9";
            List<int> nums = UtilsSO.ExtractInts(s);
            string ints = String.Join(", ", nums.Select(i => i.ToString()).ToArray());
            MessageBox.Show(this, "The numbers are " + ints, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DecompressPppStringsFiles()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null)
                return;
            string path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            string[] files = { "ENGLISH", "FRENCH", "GERMAN", "ITALIAN", "SCROLL" };
            foreach (string filename in files)
            {
                string fullPath = Path.Combine(path, filename + ".PPP");
                if (!File.Exists(fullPath))
                    continue;
                byte[] buff = File.ReadAllBytes(fullPath);

                byte[] buffDec;
                try
                {
                    buffDec = PppCompression.DecompressPppRle(buff);
                }
                catch (ArgumentException)
                {
                    continue;
                }
                string uncPath = Path.Combine(path, filename + ".dat");
                File.WriteAllBytes(uncPath, buffDec);

                int len = buffDec.Length;
                int ptr = 0;
                List<string> stringsFile = new List<string>();
                // DOS-865: Nordic
                Encoding dosenc = Encoding.GetEncoding(865);
                while (ptr < len)
                {
                    int strLen = buffDec[ptr];
                    ptr++;
                    byte[] strBuffer = new byte[strLen];
                    Array.Copy(buffDec, ptr, strBuffer, 0, Math.Min(strLen, len - ptr));
                    for (int c = 0; c < strLen; ++c)
                        strBuffer[c] = (byte)((strBuffer[c] << 1) | (strBuffer[c] >> 7));
                    string curLine = dosenc.GetString(strBuffer);
                    stringsFile.Add(curLine);
                    ptr += strLen;
                }
                string fullFile = String.Join(Environment.NewLine, stringsFile.ToArray());

                string fullPath2 = Path.Combine(path, filename + ".txt");
                File.WriteAllText(fullPath2, fullFile, new UTF8Encoding(true) /* dosenc*/);
            }
        }

        private void PixelsToPalette()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.GetBitmap()) == null)
                return;
            string path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            string name = Path.GetFileNameWithoutExtension(this.m_LoadedFile.LoadedFile);
            byte[] imageData = ImageUtils.GetImageData(this.m_LoadedFile.GetBitmap(), PixelFormat.Format24bppRgb);
            int palLen = Math.Min(0x300, imageData.Length / 3 * 3);
            for (int i = 0; i < palLen; i += 3)
            {
                byte b = imageData[i];
                imageData[i] = imageData[i + 2];
                imageData[i + 2] = b;
            }
            byte[] paletteData = new byte[0x300];
            Array.Copy(imageData, paletteData, Math.Min(0x300, imageData.Length));
            FilePalette8Bit pal = new FilePalette8Bit();
            pal.LoadFile(paletteData, Path.Combine(path, name + ".pal"));
            this.LoadTestFile(pal);
        }

        private void CompareMaps()
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.LoadedFile == null)
                return;
            string folder = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            string folder106c = Path.Combine(folder, "106c");
            string folderOrig = Path.Combine(folder, "orig");
            if (!Directory.Exists(folderOrig) || !Directory.Exists(folder106c))
                return;
            string[] allMaps = Directory.GetFiles(folderOrig, "*.bin");
            int nrOfMaps = allMaps.Length;
            FileFrames frames = new FileFrames();
            for (int i = 0; i < nrOfMaps; ++i)
            {
                string fileOrig = allMaps[i];
                string fileName = Path.GetFileName(fileOrig);
                string file106c = Path.Combine(folder106c, fileName);
                bool fileOrigExists = File.Exists(fileOrig);
                bool file106cExists = File.Exists(file106c);

                FileImageFrame framePic = new FileImageFrame();
                string newPath = Path.Combine(folder, fileName);
                if (!fileOrigExists || !file106cExists)
                {
                    framePic.LoadFile(null, newPath);
                    framePic.SetExtraInfo("Compare file not found.");
                    frames.AddFrame(framePic);
                    continue;
                }
                byte[] mapDataOrig = File.ReadAllBytes(fileOrig);
                byte[] mapData106c = File.ReadAllBytes(file106c);

                byte[] imageData;
                int stride;
                byte colIndex;
                Color[] colors;
                using (FileMapWwCc1Pc mapOrig = new FileMapWwCc1Pc())
                {
                    mapOrig.LoadFile(mapDataOrig, fileOrig);
                    Bitmap mappic = mapOrig.GetBitmap();
                    imageData = ImageUtils.GetImageData(mappic, out stride, true);
                    Color[] cols = mappic.Palette.Entries;
                    if (cols.Length < 256)
                    {
                        colIndex = (byte)cols.Length;
                        colors = new Color[colIndex + 1];
                        Array.Copy(cols, colors, colIndex);
                    }
                    else
                    {
                        colors = cols;
                        colIndex = 0x20;
                    }
                }
                colors[colIndex] = Color.Red;
                const int mapSize = 64 * 64;
                List<int> affectedCells = new List<int>();
                for (int c = 0; c < mapSize; ++c)
                {
                    int mapOffs = c << 1;
                    if (mapDataOrig[mapOffs] == mapData106c[mapOffs] && mapDataOrig[mapOffs + 1] == mapData106c[mapOffs + 1])
                        continue;
                    imageData[c] = colIndex;
                    affectedCells.Add(c);
                }
                Bitmap bm = ImageUtils.BuildImage(imageData, 64, 64, stride, PixelFormat.Format8bppIndexed, colors, null);
                framePic.LoadFile(bm, newPath);
                if (affectedCells.Count > 0)
                    framePic.SetExtraInfo("Changed cells: " + String.Join(", ", affectedCells.Select(c => c.ToString()).ToArray()));
                frames.AddFrame(framePic);
            }
            if (frames.Frames.Length > 0)
                this.LoadTestFile(frames);
        }


        private void GetIco()
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType[] frames = this.m_LoadedFile.Frames;
            Bitmap curBm = this.m_LoadedFile.GetBitmap();
            List<Image> images = new List<Image>();
            if (frames != null && frames.Length > 0)
            {
                foreach (SupportedFileType frame in frames)
                {
                    Bitmap frImg = frame.GetBitmap();
                    if (frImg == null || frImg.Width > 256 || frImg.Height > 256)
                        continue;
                    images.Add(frImg);
                }
            }
            else if (curBm != null && curBm.Width <= 256 && curBm.Height <= 256)
            {
                images.Add(curBm);
            }
            if (images.Count == 0)
                return;
            byte[] contents;
            // Set program icon to this, as quick test.
            this.Icon = ImageUtilsSO.ConvertImagesToIco(images.ToArray(), out contents);
            // Content of Images comes from loaded file, so don't dispose them. The LoadTestFile function will take care of that.
            FileIcon ic = new FileIcon();
            ic.LoadFile(contents, Path.Combine(Path.GetDirectoryName(this.m_LoadedFile.LoadedFile), Path.GetFileNameWithoutExtension(this.m_LoadedFile.LoadedFile) + ".ico"));
            this.LoadTestFile(ic);
        }

        private void FixPngAspectRatio()
        {
            if (this.m_LoadedFile == null || String.IsNullOrEmpty(this.m_LoadedFile.LoadedFile) || !File.Exists(this.m_LoadedFile.LoadedFile))
                return;
            string folder = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            string[] files = Directory.GetFiles(folder, "*.png");
            foreach (string file in files)
                this.FixPngAspectRatio(file);
        }

        private void FixPngAspectRatio(string path)
        {
            const string physChunkId = "pHYs";
            // Read bytes
            byte[] pngBytes;
            try
            {
                pngBytes = File.ReadAllBytes(path);
            }
            catch
            {
                return; /* Not dealing with this. Just abort. */
            }
            // Checks
            if (!PngHandler.IsPng(pngBytes))
                return;
            int physLoc = PngHandler.FindPngChunk(pngBytes, physChunkId);
            if (physLoc == -1)
                return;
            byte[] pngChunk = PngHandler.GetPngChunkData(pngBytes, physLoc);
            if (pngChunk.Length != 9)
                return;
            uint dimX = ArrayUtils.ReadUInt32FromByteArrayBe(pngChunk, 0);
            uint dimY = ArrayUtils.ReadUInt32FromByteArrayBe(pngChunk, 4);
            if (dimX == dimY)
                return;

            // Fix segment
            ArrayUtils.WriteInt32ToByteArrayBe(pngChunk, 0, 0xEC3);
            ArrayUtils.WriteInt32ToByteArrayBe(pngChunk, 4, 0xEC3);
            PngHandler.WritePngChunk(pngBytes, physLoc, physChunkId, pngChunk);

            // Make backup
            string folder = Path.GetDirectoryName(path);
            string origFolder = Path.Combine(folder, "orig");
            if (!Directory.Exists(origFolder))
                Directory.CreateDirectory(origFolder);
            string backup = Path.Combine(origFolder, Path.GetFileName(path));
            File.Copy(path, backup);

            // Save changes
            File.WriteAllBytes(path, pngBytes);
        }

        private void MakeBorderIcon()
        {
            int size = 34;
            int borderSize = 2;
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed;
            int stride = size;
            byte[] pixels = new byte[size * stride];

            Color[] palette = new Color[] { Color.Pink, Color.Green };

            byte paintIndex = 1;
            borderSize = Math.Min(borderSize, size);

            // Horizontal: just fill the whole block.

            // Top line
            int end = stride * borderSize;
            for (int i = 0; i < end; ++i)
                pixels[i] = paintIndex;

            // Bottom line
            end = stride * size;
            for (int i = stride * (size - borderSize); i < end; ++i)
                pixels[i] = paintIndex;

            // Vertical: Both loops are inside the same y loop. It only goes over
            // the space between the already filled top and bottom parts.
            int lineStart = borderSize * stride;
            int yEnd = size - borderSize;
            int rightStart = size - borderSize;
            for (int y = borderSize; y < yEnd; ++y)
            {
                // left line
                for (int x = 0; x < borderSize; ++x)
                    pixels[lineStart + x] = paintIndex;
                // right line
                for (int x = rightStart; x < size; ++x)
                    pixels[lineStart + x] = paintIndex;
                lineStart += stride;
            }

            if (pixelFormat == PixelFormat.Format1bppIndexed)
                pixels = ImageUtils.ConvertFrom8Bit(pixels, size, size, 1, true, ref stride);

            Bitmap bm2 = ImageUtils.BuildImage(pixels, size, size, stride, pixelFormat, palette, Color.Black);
            this.LoadTestFile(bm2);
        }

        private void DetectBlobs()
        {
            Bitmap bm;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (bm = shownFile.GetBitmap()) == null)
                return;
            List<List<Point>> blobs = BlobDetection.FindBlobs(bm, true, 0.5f, -1, true);
            Bitmap bm2 = new Bitmap(bm);
            foreach (List<Point> blob in blobs)
            {
                Point center = BlobDetection.GetBlobCenter(blob);
                foreach (Point p in blob)
                    bm2.SetPixel(p.X, p.Y, Color.Blue);
                bm2.SetPixel(center.X - 1, center.Y, Color.Red);
                bm2.SetPixel(center.X, center.Y, Color.Red);
                bm2.SetPixel(center.X + 1, center.Y, Color.Red);
                bm2.SetPixel(center.X, center.Y - 1, Color.Red);
                bm2.SetPixel(center.X, center.Y + 1, Color.Red);
            }
            this.LoadTestFile(bm2, this.m_LoadedFile.LoadedFile, "Detected blobs: " + blobs.Count);
        }

        private void IndexedToArgb()
        {
            Bitmap bm;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (bm = shownFile.GetBitmap()) == null || bm.PixelFormat != PixelFormat.Format8bppIndexed)
                return;
            int stride;
            int width = bm.Width;
            int height = bm.Height;
            byte[] data = ImageUtils.GetImageData(bm, out stride);
            Color[] cols = bm.Palette.Entries;
            byte[] palette = ColorUtils.GetSixBitPaletteData(cols);

            // Used data:
            // Byte[] data = image data
            // Byte[] palette = 6-bit palette
            // Int32 stride = number of bytes on one line of the image
            // Int32 width = image width
            // Int32 height = image height

            for (int t = 0; t < 0x300; ++t)
                palette[t] = (byte)(palette[t] * 4);
            int lineOffset = 0;
            int lineOffsetQuad = 0;
            int strideQuad = width * 4;
            byte[] dataArgb = new byte[strideQuad * height];
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                int outOffset = lineOffsetQuad;
                for (int x = 0; x < width; ++x)
                {
                    // get color index, then get the correct location in the palette array
                    // by multiplying it by 3 (the length of one full color)
                    int colIndex = data[offset++] * 3;
                    dataArgb[outOffset++] = palette[colIndex + 2]; // Blue
                    dataArgb[outOffset++] = palette[colIndex + 1]; // Green
                    dataArgb[outOffset++] = palette[colIndex]; // Red
                    dataArgb[outOffset++] = (colIndex == 0 ? (byte)0 : (byte)255); // Alpha: set to 0 for background black
                }
                lineOffset += stride;
                lineOffsetQuad += strideQuad;
            }
            Bitmap bm2 = ImageUtils.BuildImage(dataArgb, width, height, strideQuad, PixelFormat.Format32bppArgb, null, null);
            this.LoadTestFile(bm2);
        }

        private void WriteIcoFileFromFrames()
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0 || this.m_LoadedFile.LoadedFile == null)
                return;
            int frameNr = this.m_LoadedFile.Frames.Length;
            List<Image> images = new List<Image>();
            for (int i = 0; i < frameNr; ++i)
            {
                Bitmap bm = this.m_LoadedFile.Frames[i].GetBitmap();
                if (bm != null)
                    images.Add(bm);
            }
            byte[] fileBytes = FileIcon.ConvertImagesToIcoBytes(images.ToArray());
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(this.m_LoadedFile.LoadedFile), "test.ico"), fileBytes);
        }

        private void ChromaKey()
        {
            Bitmap bm;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (bm = shownFile.GetBitmap()) == null)
                return;
            int stride;
            byte[] imageData = ImageUtils.GetImageData(bm, out stride, PixelFormat.Format32bppArgb);
            int width = bm.Width;
            int height = bm.Height;

            Color chroma = Color.Aquamarine;
            double chromaHue = chroma.GetHue();
            double hueThreshold = 50.0;
            double satThreshold = 0.2;
            double briThreshold = 0.2;
            int lineOffset = 0;
            for (int y = 0; y < height; ++y)
            {
                int offsetQuad = lineOffset - 4;
                for (int x = 0; x < width; ++x)
                {
                    offsetQuad += 4;

                    byte b = imageData[offsetQuad + 0];
                    byte g = imageData[offsetQuad + 1];
                    byte r = imageData[offsetQuad + 2];
                    byte a = imageData[offsetQuad + 3];
                    Color c = Color.FromArgb(a, r, g, b);
                    double cHue = c.GetHue();
                    double cSat = c.GetSaturation();
                    double cBri = c.GetBrightness();
                    double hueDiff = Math.Min(Math.Abs(chromaHue - cHue), 360 - Math.Abs(chromaHue - cHue));

                    if (cSat < satThreshold || cBri < briThreshold || hueDiff > hueThreshold)
                        continue;
                    byte grayVal = (byte)Math.Min((r * 0.3) + (g * 0.59) + (b * 0.11), 255);
                    imageData[offsetQuad + 0] = grayVal;
                    imageData[offsetQuad + 1] = grayVal;
                    imageData[offsetQuad + 2] = grayVal;
                    imageData[offsetQuad + 3] = (byte)Math.Min(((180 - hueDiff) * 255 / 360), 255);
                }
                lineOffset += stride;
            }
            lineOffset = 0;
            for (int y = 0; y < height; ++y)
            {
                int offsetQuad = lineOffset - 4;
                for (int x = 0; x < width; ++x)
                {
                    offsetQuad += 4;
                    byte b = imageData[offsetQuad + 0];
                    byte g = imageData[offsetQuad + 1];
                    byte r = imageData[offsetQuad + 2];
                    Color c = Color.FromArgb(r, g, b);
                    double cHue = c.GetHue();
                    double cSat = c.GetSaturation();
                    double cBri = c.GetBrightness();
                    if (cHue < 60 || cHue > 130 || cSat < 0.15 || cBri <= 0.15)
                        continue;
                    //if (cHue >= 60 && cHue <= 130 && cSat >= 0.15 && cBri > 0.15)
                    int rb = r * b;
                    int gsq = g * g;
                    if (rb != 0 && gsq / rb >= 1.5)
                    {
                        imageData[offsetQuad + 0] = (byte)Math.Max(r * 1.4, 255);
                        imageData[offsetQuad + 1] = g;
                        imageData[offsetQuad + 2] = (byte)Math.Max(b * 1.4, 255);
                    }
                    else
                    {
                        imageData[offsetQuad + 0] = (byte)Math.Max(r * 1.2, 255);
                        imageData[offsetQuad + 1] = g;
                        imageData[offsetQuad + 2] = (byte)Math.Max(b * 1.2, 255);
                    }
                }
            }
            Bitmap bmNew = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format32bppArgb, null, null);
            this.LoadTestFile(bmNew);
        }

        private void ChromaKey2()
        {
            Bitmap bm;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (bm = shownFile.GetBitmap()) == null)
                return;
            Color low_color = Color.Green;
            Color high_color = Color.White;
            ImageAttributes imageAttr = new ImageAttributes();
            imageAttr.SetColorKey(low_color, high_color);

            // Make the result image.
            int width = bm.Width;
            int height = bm.Height;
            Bitmap bmNew = new Bitmap(width, height);

            // Process the image.
            using (Graphics gr = Graphics.FromImage(bmNew))
            {
                // Fill with magenta.
                //gr.Clear(Color.Magenta);

                // Copy the original image onto the result
                // image while using the ImageAttributes.
                Rectangle dest_rect = new Rectangle(0, 0, width, height);
                gr.DrawImage(bm, dest_rect, 0, 0, width, height, GraphicsUnit.Pixel, imageAttr);
            }
            this.LoadTestFile(bmNew);
        }

        private void TestSplit()
        {
            Bitmap image;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (image = shownFile.GetBitmap()) == null)
                return;

            int width = image.Width;
            int height = image.Height;
            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = sourceData.Stride;
            byte[] data = new byte[stride * height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            image.UnlockBits(sourceData);

            int lastWhiteLine = ImageUtilsSO.GetLastClearLine(data, stride, width, height, Color.White);
            if (lastWhiteLine == height - 1)
                MessageBox.Show(this, "Nothing touching the bottom edge.");
            else
                MessageBox.Show(this, "Last full white line is " + lastWhiteLine);
        }

        private void BayerGridtoGray()
        {
            Bitmap image;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (image = shownFile.GetBitmap()) == null)
                return;
            Bitmap bmNew = ImageUtilsSO.BayerGridToGray(image, true, false);
            this.LoadTestFile(bmNew);
        }

        private void BuildBayer()
        {
            Bitmap image;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (image = shownFile.GetBitmap()) == null)
                return;
            int width = image.Width;
            int height = image.Height;
            byte[] imageData = ImageUtilsSO.GetChannelBytes(image, 0);
            int stride = width;
            byte[] bayerData = ImageUtilsSO.BayerToRgb2x2Orig(imageData, ref width, ref height, ref stride, true, false);
            Bitmap bmNew;
            using (Bitmap bmBay = ImageUtils.BuildImage(bayerData, width, height, stride, PixelFormat.Format24bppRgb, null, null))
            {
                bmNew = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppRgb);
                using (Graphics g = Graphics.FromImage(bmNew))
                {
                    g.InterpolationMode = InterpolationMode.Bilinear;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(bmBay, 0, 0, image.Width, image.Height);
                    g.DrawImage(bmBay, 0.5f, 0.5f, width, height);
                }
            }
            this.LoadTestFile(bmNew);
        }

        private void Reduce12Bit()
        {
            Bitmap image;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (image = shownFile.GetBitmap()) == null || image.PixelFormat != PixelFormat.Format8bppIndexed || image.Width % 2 != 0)
                return;
            int width = image.Width / 2;
            int height = image.Height;
            int stride;
            byte[] imgData1 = ImageUtils.GetImageData(image, out stride, true);
            byte[] imgData2 = new byte[width * height];
            int readLineOffs = 0;
            int writeLineOffs = 0;
            for (int y = 0; y < height; ++y)
            {
                int readOffs = readLineOffs;
                int readOffsEnd = readLineOffs + width * 2;
                for (; readOffs < readOffsEnd; readOffs += 2)
                {
                    int value = ((imgData1[readOffs + 1] << 8) + imgData1[readOffs]) >> 4;
                    imgData2[writeLineOffs] = (byte)value;
                    writeLineOffs++;
                }
                readLineOffs += stride;
            }
            Bitmap bm2 = ImageUtils.BuildImage(imgData2, width, height, width, PixelFormat.Format8bppIndexed, PaletteUtils.GenerateGrayPalette(8, null, false), null);
            this.LoadTestFile(bm2);
        }

        private void CombineImages()
        {
            Bitmap picFootshape;
            Bitmap materialImage;
            int width;
            int height;
            SupportedFileType current = this.m_LoadedFile;
            if (current == null || !current.IsFramesContainer || current.Frames.Length < 2
                || current.Frames[0] == null || (picFootshape = current.Frames[0].GetBitmap()) == null
                || current.Frames[1] == null || (materialImage = current.Frames[1].GetBitmap()) == null
                || (width = picFootshape.Width) != materialImage.Width || (height = picFootshape.Height) != materialImage.Height)
                return;
            int stride;
            // extract bytes of shape & alpha image
            byte[] shapeImageBytes = ImageUtils.GetImageData(picFootshape, out stride, PixelFormat.Format32bppArgb);
            // combine
            using (Bitmap blackImage = ImageUtilsSO.ExtractBlackImage(shapeImageBytes, width, height, stride))
            {
                Bitmap result = ImageUtilsSO.ApplyAlphaToImage(shapeImageBytes, width, height, stride, materialImage);
                // paint black lines image onto alpha-adjusted pattern image.
                using (Graphics g = Graphics.FromImage(result))
                    g.DrawImage(blackImage, 0, 0);
                this.LoadTestFile(result);
            }
        }

        private void MakePatterns()
        {
            SupportedFileType shownFile = this.GetShownFile();
            Bitmap image;
            if (shownFile == null || (image = shownFile.GetBitmap()) == null || image.PixelFormat != PixelFormat.Format32bppArgb)
                return;
            string testFilesPath = Path.GetFullPath(Path.Combine(GeneralUtils.GetApplicationPath(), "..\\..\\..\\..\\1_testdata"));
            string patternsFolder = Path.Combine(testFilesPath, "feet_patterns");
            //String materialsFolder = Path.Combine(testFilesPath, "feet_material");
            //foreach (String materialImagePath in Directory.GetFiles(materialsFolder))
            //    File.Delete(materialImagePath);
            string finalFolder = Path.Combine(testFilesPath, "feet_final");
            foreach (string finalFile in Directory.GetFiles(finalFolder))
                File.Delete(finalFile);
            //ImageUtilsSO.TilePatterns(patternsFolder, image.Width, image.Height, materialsFolder);
            ImageUtilsSO.BakeImages(shownFile.LoadedFile, patternsFolder, finalFolder);
        }

        private void ExtractBlack()
        {
            SupportedFileType shownFile = this.GetShownFile();
            Bitmap image;
            if (shownFile == null || (image = shownFile.GetBitmap()) == null)
                return;
            int width = image.Width;
            int height = image.Height;
            int stride;
            byte[] shapeImageBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format32bppArgb);
            Bitmap blackImage = ImageUtilsSO.ExtractBlackImage(shapeImageBytes, width, height, stride);
            this.LoadTestFile(blackImage);
        }

        private void MakeTrans()
        {
            const byte bgRedR = 0x96;
            const byte bgRedG = 0x0b;
            const byte bgRedB = 0x08;

            Bitmap img1Red;
            Bitmap img2Black;
            int width;
            int height;
            SupportedFileType current = this.m_LoadedFile;
            if (current == null || !current.IsFramesContainer || current.Frames.Length < 2
                || current.Frames[0] == null || (img1Red = current.Frames[0].GetBitmap()) == null
                || current.Frames[1] == null || (img2Black = current.Frames[1].GetBitmap()) == null
                || (width = img1Red.Width) != img2Black.Width || (height = img1Red.Height) != img2Black.Height)
                return;
            int stride = ImageUtils.GetClassicStride(width, 32);
            byte[] img1RedBytes = ImageUtils.GetImageData(img1Red, PixelFormat.Format32bppArgb);
            byte[] img2BlackBytes = ImageUtils.GetImageData(img2Black, PixelFormat.Format32bppArgb);
            int lineOffset = 0;
            const int threshold = 160;
            const int thresholdBlack = 5;
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                for (int x = 0; x < width; ++x)
                {
                    byte b1r = img1RedBytes[offset];
                    byte b2b = img2BlackBytes[offset];
                    byte g1r = img1RedBytes[offset + 1];
                    byte g2b = img2BlackBytes[offset + 1];
                    byte r1r = img1RedBytes[offset + 2];
                    byte r2b = img2BlackBytes[offset + 2];
                    int diffB = Math.Abs(b1r - b2b);
                    int diffG = Math.Abs(g1r - g2b);
                    int diffR = Math.Abs(r1r - r2b);
                    if (b2b < thresholdBlack && g2b < thresholdBlack && r2b < thresholdBlack)
                    //if (diffR > threshold || diffG > threshold || diffB > threshold)
                    //if (diffR > threshold || diffG > threshold || diffB > threshold || (b2b < thresholdBlack && g2b < thresholdBlack && r2b < thresholdBlack))
                    {
                        //Int32 diffB1Red = Math.Abs(b1 - bgRedB);
                        //Int32 diffG1Red = Math.Abs(g1 - bgRedG);
                        int diffR1Red = Math.Abs(r1r - bgRedR);
                        //img1RedBytes[offset + 0] = r1r;
                        //img1RedBytes[offset + 1] = r1r;
                        //img1RedBytes[offset + 2] = r1;
                        img1RedBytes[offset + 3] = (byte)diffR1Red;
                    }
                    offset += 4;
                }
                lineOffset += stride;
            }
            Bitmap bm2 = ImageUtils.BuildImage(img1RedBytes, width, height, stride, PixelFormat.Format32bppArgb, null, null);
            this.LoadTestFile(bm2);
        }

        private void CombineVertical()
        {
            string testFilesPath = Path.GetFullPath(Path.Combine(GeneralUtils.GetApplicationPath(), "..\\..\\..\\..\\1_testdata"));
            string patternsFolder = Path.Combine(testFilesPath, "feet_patterns");
            List<Bitmap> images = new List<Bitmap>();
            string[] files = Directory.GetFiles(patternsFolder);
            foreach (string imagePath in files)
                images.Add(new Bitmap(imagePath));
            int width = images.First().Width; //all images in list have the same width so I take the first
            int height = 0;
            for (int i = 0; i < images.Count; ++i) //the list has 300 images.
            {
                height += images[i].Height;
            }
            Bitmap bitmap2 = new Bitmap(width, height);
            bitmap2.SetResolution(72, 72);
            using (Graphics g = Graphics.FromImage(bitmap2))
            {

                height = 0;
                for (int i = 0; i < images.Count; ++i)
                {
                    Bitmap image = images[i];
                    image.SetResolution(72, 72);
                    g.DrawImage(image, 0, height);
                    height += image.Height;
                }
            }
            foreach (Bitmap image in images)
                image.Dispose();
            //bitmap2.Save(Path.Combine(testFilesPath, "testCombine.png"), ImageFormat.Png);
            this.LoadTestFile(bitmap2);
        }

        private void ConvertToIcons()
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0 || this.m_LoadedFile.LoadedFile == null)
                return;
            List<string> originalNames = this.m_LoadedFile.Frames.Select(f => f.LoadedFile).Where(File.Exists).ToList();
            if (originalNames.Count == 0)
                return;
            string outPath = Path.GetDirectoryName(originalNames[0]);
            ImageUtilsSO.WriteImagesToIcons(originalNames, outPath);
        }

        private void ViewSTrisHidden()
        {
            // This image appears in a number of games. it may be the logo of some old adult warez distribution group.
            // The files are always the same: a 6-bit color palette in plain text format, and a raw 320x200 8-bit data array
            // without header, inconspicuously named as if they are files of the game itself. The image is always the same too:
            // Eddie, the zombie mascot of the band "Iron Maiden", carrying a British flag as depicted on the album "The Trooper".

            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null || (!(this.m_LoadedFile is FileImgStris) && !(this.m_LoadedFile is FileFrames && ((FileFrames)this.m_LoadedFile).EmbeddedType == typeof(FileImgStris))))
                return;
            string path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            string palFile = Path.Combine(path, "14.sex");
            string imgFile = Path.Combine(path, "15.sex");
            if (!File.Exists(palFile) || !File.Exists(imgFile))
                return;
            byte[] palB = File.ReadAllBytes(palFile);
            for (int i = 0; i < palB.Length; ++i)
                if (palB[i] != 0x0D && palB[i] != 0x0A && palB[i] != 0x20 && (palB[i] < '0' || palB[i] > '9'))
                    return;
            string palT = Encoding.ASCII.GetString(palB);
            Regex line = new Regex("\\s*(\\d+)\\s*(\\d\\d?)\\s*(\\d\\d?)\\s*(\\d\\d?)\\s*?\r\n");
            MatchCollection mc = line.Matches(palT);
            Color[] palette = new Color[256];
            foreach (Match m in mc)
            {
                int index = Int32.Parse(m.Groups[1].Value);
                if (index >= 256)
                    continue;
                byte[] cols = new byte[] { byte.Parse(m.Groups[2].Value), byte.Parse(m.Groups[3].Value), byte.Parse(m.Groups[4].Value) };
                palette[index] = PixelFormatter.Format6BitVgaPal.GetColor(cols, 0);
            }
            byte[] imgB = File.ReadAllBytes(imgFile);
            Bitmap image = ImageUtils.BuildImage(imgB, 320, 200, 320, PixelFormat.Format8bppIndexed, palette, Color.Black);
            this.LoadTestFile(image);
        }

        private void Ikegami()
        {
            byte[] graphic =
            {
                0x00, 0x00, 0x41, 0x7F, 0x7F, 0x41, 0x00, 0x00,
                0x00, 0x7F, 0x7F, 0x18, 0x3C, 0x76, 0x63, 0x41,
                0x00, 0x00, 0x7F, 0x7F, 0x49, 0x49, 0x49, 0x41,
                0x00, 0x1C, 0x3E, 0x63, 0x41, 0x49, 0x79, 0x79,
                0x00, 0x7C, 0x7E, 0x13, 0x11, 0x13, 0x7E, 0x7C,
                0x00, 0x7F, 0x7F, 0x0E, 0x1C, 0x0E, 0x7F, 0x7F,
                0x00, 0x00, 0x41, 0x7F, 0x7F, 0x41, 0x00, 0x00
            };
            //Bitmap ikegami = ImageUtils.BuildImage(graphic, 8, 56, 1, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);
            byte[] conv1 = ImageUtils.ConvertTo8Bit(graphic, 8, 56, 0, 1, false);
            byte[] conv2 = new byte[conv1.Length];
            for (int i = 0; i < conv1.Length; ++i)
                conv2[(56 * (i % 8)) + i / 8] = conv1[i];
            //Bitmap ikegami = ImageUtils.BuildImage(conv2, 56, 8, 56, PixelFormat.Format8bppIndexed, new Color[] { Color.Black, Color.White }, null);
            byte[] conv3 = ImageUtils.ConvertFrom8Bit(conv2, 56, 8, 1, true);
            Bitmap ikegami = ImageUtils.BuildImage(conv3, 56, 8, 7, PixelFormat.Format1bppIndexed, new Color[] { Color.Black, Color.White }, null);


            this.LoadTestFile(ikegami);
        }

        private void SplitTwoColor()
        {
            SupportedFileType shownFile = this.GetShownFile();
            Bitmap image;
            if (shownFile == null || (image = shownFile.GetBitmap()) == null)
                return;
            Bitmap newBm = ImageUtilsSO.ReduceToTwoColorFade(image, true, true);
            this.LoadTestFile(newBm);
        }

        private void LoadToClip()
        {
            byte[] dibdata = new byte[]
            {
                // header
                0x28, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x80, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // palette
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x80, 0x80, 0x00,
                0x80, 0x00, 0x00, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x80, 0x00, 0x00, 0x80, 0x80, 0x80, 0x00,
                0xC0, 0xc0, 0xc0, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00,
                0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00,
                // XOR mask
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x88, 0x88, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x07, 0xFF, 0x88, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x7F, 0xF8, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x7F, 0x80, 0x07, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x07, 0xF8, 0x00, 0x07, 0xFF, 0x7F, 0xF7, 0xF8, 0x0F, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x07, 0x70, 0x00, 0x07, 0xF8, 0x0F, 0x80, 0xF8, 0x0F, 0x80, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x0F, 0x80, 0xF8, 0x07, 0x70, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x0F, 0x80, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x0F, 0x80, 0x77, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x07, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x77, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // AND mask
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xF8, 0x1F, 0xFF,
                0xFF, 0xF0, 0x0F, 0xFF,
                0xFF, 0xF0, 0x0F, 0xFF,
                0xFF, 0xE0, 0x03, 0xFF,
                0xFF, 0xc0, 0x03, 0xFF,
                0xFF, 0x80, 0x01, 0xFF,
                0xFF, 0x80, 0x01, 0xFF,
                0xFF, 0x00, 0x01, 0xFF,
                0xFF, 0x00, 0x00, 0xFF,
                0xFE, 0x00, 0x00, 0xFF,
                0xFE, 0x00, 0x00, 0xFF,
                0xFc, 0x00, 0x00, 0xFF,
                0xFc, 0x20, 0x00, 0xFF,
                0xF8, 0x60, 0x00, 0xFF,
                0xF8, 0xE0, 0x00, 0xFF,
                0xFF, 0xE0, 0x01, 0xFF,
                0xFF, 0xE0, 0x07, 0xFF,
                0xFF, 0xE0, 0x0F, 0xFF,
                0xFF, 0xE0, 0x7F, 0xFF,
                0xFF, 0xE1, 0xFF, 0xFF,
                0xFF, 0xE1, 0xFF, 0xFF,
                0xFF, 0xE1, 0xFF, 0xFF,
                0xFF, 0xE1, 0xFF, 0xFF,
                0xFF, 0xF3, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF
            };
            PixelFormat pf;
            Bitmap cursorImage = DibHandler.ImageFromDib(dibdata, 0, 0, 0, true, false, out pf);
            this.LoadTestFile(cursorImage);
        }

        private void ReplaceImagePalette()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null || this.m_LoadedFile is FileFrames || this.m_LoadedFile.BitsPerPixel != 8)
                return;
            string filename = this.m_LoadedFile.LoadedFile;
            if (filename == null)
                return;
            Color[] newPalette = new Color[0x100];
            using (Bitmap bm = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                for (int i = 0; i < 0x100; ++i)
                {
                    Color c = bm.Palette.Entries[i];
                    newPalette[i] = Color.FromArgb(c.R, c.G, c.B);
                }
            }
            int stride;
            int width;
            int height;
            Color[] curPalette;
            byte[] imageData;
            // This 'using' block is kept small; extract the data and then dispose everything.
            using (Bitmap image = ImageUtils.CloneImage(this.m_LoadedFile.GetBitmap()))
            //using (Bitmap image = new Bitmap(filename))
            {
                if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                    return;
                width = image.Width;
                height = image.Height;
                curPalette = image.Palette.Entries;
                imageData = ImageUtils.GetImageData(image, out stride);
            }
            // Make remap table to translate from old palette indices to new ones.
            byte[] match = new byte[curPalette.Length];
            for (int i = 0; i < curPalette.Length; ++i)
                match[i] = (byte)ColorUtils.GetClosestPaletteIndexMatch(curPalette[i], newPalette);
            // Go over the actual pixels in the image data and replace the colors.
            int currentLineOffset = 0;
            for (int y = 0; y < height; ++y)
            {
                int offset = currentLineOffset;
                for (int x = 0; x < width; ++x)
                {
                    // Replace index with index of the closest match found before for that color.
                    imageData[offset] = match[imageData[offset]];
                    // Increase offset on this line
                    offset++;
                }
                // Increase to start of next line
                currentLineOffset += stride;
            }
            /*/
            using (Bitmap newbm = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, newPalette, Color.Black))
            {
                // Old bitmap is already disposed, so there is no issue saving to the same filename now
                newbm.Save(filename, ImageFormat.Bmp);
            }
            //*/
            Bitmap newbm = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, newPalette, Color.Black);
            this.LoadTestFile(newbm);
        }

        private void ReversePalette()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null || this.m_LoadedFile is FileFrames || this.m_LoadedFile.BitsPerPixel != 8)
                return;
            Color[] newPalette = new Color[0x100];
            byte[] remap = new byte[0x100];
            Color[] oldPalette = m_LoadedFile.GetColors();

            for (int i = 0; i < 0x100; ++i)
            {
                byte inverse = (byte)(0xFF - i);
                newPalette[i] = oldPalette[inverse];
                remap[i] = inverse;
            }
            int stride;
            int width;
            int height;
            Color[] curPalette;
            byte[] imageData;
            // This 'using' block is kept small; extract the data and then dispose everything.
            using (Bitmap image = ImageUtils.CloneImage(this.m_LoadedFile.GetBitmap()))
            {
                if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                    return;
                width = image.Width;
                height = image.Height;
                curPalette = image.Palette.Entries;
                imageData = ImageUtils.GetImageData(image, out stride);
            }
            // Go over the actual pixels in the image data and replace the colors.
            int currentLineOffset = 0;
            for (int y = 0; y < height; ++y)
            {
                int offset = currentLineOffset;
                for (int x = 0; x < width; ++x)
                {
                    // Replace index with index of the closest match found before for that color.
                    imageData[offset] = remap[imageData[offset]];
                    // Increase offset on this line
                    offset++;
                }
                // Increase to start of next line
                currentLineOffset += stride;
            }
            Bitmap newbm = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, newPalette, Color.Black);
            this.LoadTestFile(newbm);
        }

        private void AutoRemapPalette()
        {
            if (!(this.m_LoadedFile is FileFrames) || this.m_LoadedFile.Frames.Length < 2
                || this.m_LoadedFile.Frames.Any(f => f.BitsPerPixel != 8) || this.m_LoadedFile.Frames[0].Width != this.m_LoadedFile.Frames[1].Width
                || this.m_LoadedFile.Frames[0].Height != this.m_LoadedFile.Frames[1].Height)
                return;
            int width = this.m_LoadedFile.Frames[0].Width;
            int height = this.m_LoadedFile.Frames[0].Height;
            byte[] imgBeta = ImageUtils.GetImageData(this.m_LoadedFile.Frames[0].GetBitmap(), true);
            byte[] imgFinl = ImageUtils.GetImageData(this.m_LoadedFile.Frames[1].GetBitmap(), true);
            Dictionary<byte, List<int>> differentIndices = new Dictionary<byte, List<int>>();
            Dictionary<int, int> matchedIndices = new Dictionary<int, int>();
            int lineOffset = 0;
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                for (int x = 0; x < width; ++x)
                {
                    byte indexBeta = imgBeta[offset];
                    byte indexFinl = imgFinl[offset];
                    int dictVal = indexBeta | (indexFinl << 16);
                    if (matchedIndices.ContainsKey(dictVal))
                        matchedIndices[dictVal]++;
                    else
                    {
                        matchedIndices[dictVal] = 1;
                        if (!differentIndices.ContainsKey(indexBeta))
                            differentIndices[indexBeta] = new List<int>();
                        differentIndices[indexBeta].Add(dictVal);
                    }
                    offset++;
                }
                lineOffset += width;
            }
            byte[] indices = differentIndices.Keys.OrderBy(x => x).ToArray();
            byte[] fullPaletteRemap = new byte[0x100];
            int[] fullPaletteRemapAmount = new int[0x100];
            bool[] alreadyMatched = new bool[0x100];
            for (int i = 0; i < indices.Length; ++i)
            {
                byte indexBeta = indices[i];
                List<int> differentMatches = differentIndices[indexBeta];
                int maxAmount = 0;
                int maxIndex = -1;
                foreach (int matchId in differentMatches)
                {
                    int amount = matchedIndices[matchId];
                    if (amount > maxAmount)
                    {
                        maxAmount = amount;
                        maxIndex = (matchId >> 16 & 0xFF);
                    }
                }
                if (maxIndex == -1)
                    continue;
                // end result: most occurring match for this beta index
                // Use only most occurring one.
                if (fullPaletteRemapAmount[maxIndex] < maxAmount)
                {
                    fullPaletteRemap[indexBeta] = (byte)maxIndex;
                    fullPaletteRemapAmount[indexBeta] = maxAmount;
                    alreadyMatched[indexBeta] = true;
                }
            }
            for (int i = 0; i < 0x100; ++i)
            {
                if (!alreadyMatched[i])
                    fullPaletteRemap[i] = 0x01;
            }
            for (int i = 0; i < imgBeta.Length; ++i)
            {
                imgBeta[i] = fullPaletteRemap[imgBeta[i]];
            }
            Bitmap newBm = ImageUtils.BuildImage(imgBeta, width, height, width, PixelFormat.Format8bppIndexed, this.m_LoadedFile.Frames[1].GetColors(), null);
            this.LoadTestFile(newBm);
        }

        private void SwapColors()
        {
            Bitmap loadedBm;
            if (this.m_LoadedFile == null || (loadedBm = this.m_LoadedFile.GetBitmap()) == null || this.m_LoadedFile.BitsPerPixel <= 8)
                return;
            int stride;
            byte[] imgData = ImageUtils.GetImageData(loadedBm, out stride);
            int skipsize = this.m_LoadedFile.BitsPerPixel / 8;
            int height = loadedBm.Height;
            int width = loadedBm.Width;
            //ARGB = [BB GG RR AA]
            int linePtr = 0;
            for (int y = 0; y < height; ++y)
            {
                int ptr = linePtr;
                for (int x = 0; x < width; ++x)
                {
                    byte blue = imgData[ptr];
                    byte red = imgData[ptr + 2];
                    imgData[ptr] = red;
                    imgData[ptr + 2] = blue;
                    ptr += skipsize;
                }
                linePtr += stride;
            }
            Bitmap newBm = ImageUtils.BuildImage(imgData, width, height, stride, loadedBm.PixelFormat, null, null);
            this.LoadTestFile(newBm);
        }


        private void MakeIcons()
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType[] frames = this.m_LoadedFile.IsFramesContainer ? this.m_LoadedFile.Frames : new SupportedFileType[] { this.m_LoadedFile };
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                return;
            Bitmap[] bm = frames.Select(fr => fr.GetBitmap()).OrderBy(b => b.Width).ToArray();

            Bitmap source16 = bm.FirstOrDefault(b => b.Width >= 16) ?? bm.Last();
            Bitmap source32 = bm.FirstOrDefault(b => b.Width >= 32) ?? bm.Last();
            Bitmap source48 = bm.FirstOrDefault(b => b.Width >= 48) ?? bm.Last();
            Bitmap source64 = bm.FirstOrDefault(b => b.Width >= 64) ?? bm.Last();
            Bitmap source96 = bm.FirstOrDefault(b => b.Width >= 96) ?? bm.Last();
            Bitmap source128 = bm.FirstOrDefault(b => b.Width >= 128) ?? bm.Last();
            Bitmap source192 = bm.FirstOrDefault(b => b.Width >= 192) ?? bm.Last();
            Bitmap source256 = bm.FirstOrDefault(b => b.Width >= 256) ?? bm.Last();

            string icoPath = "engie.ico";
            InterpolationMode scalingMode = InterpolationMode.HighQualityBicubic;
            using (Bitmap resize16 = source16.Resize(16, 16, scalingMode))
            using (Bitmap resize32 = source32.Resize(32, 32, scalingMode))
            using (Bitmap resize48 = source48.Resize(48, 48, scalingMode))
            using (Bitmap resize64 = source64.Resize(64, 64, scalingMode))
            using (Bitmap resize96 = source96.Resize(96, 96, scalingMode))
            using (Bitmap resize128 = source128.Resize(128, 128, scalingMode))
            using (Bitmap resize192 = source192.Resize(192, 192, scalingMode))
            using (Bitmap resize256 = source256.Resize(256, 256, scalingMode))
            {
                Image[] includedSizes = new Image[]
                {resize16, resize32, resize48, resize64, resize96, resize128, resize192, resize256};
                byte[] icoFile = ImageUtilsSO.ConvertImagesToIco(includedSizes);
                File.WriteAllBytes(icoPath, icoFile);
            }
        }

        private void ExtractBitmaps()
        {
            string fileToHandle = "PCS0396S.MVB";
            if (!File.Exists(fileToHandle))
                return;
            string baseName = Path.GetFileNameWithoutExtension(fileToHandle);
            if (!Directory.Exists(baseName))
            {
                Directory.CreateDirectory(baseName);
            }
            else
            {
                string[] bmpFiles = Directory.GetFiles(baseName, "*.bmp", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < bmpFiles.Length; ++i)
                    File.Delete(bmpFiles[i]);
            }
            using (FileStream fs = new FileStream(fileToHandle, FileMode.Open))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int curBm = 0;
                long len = fs.Length;
                while (fs.Position + 0x13 < len)
                {
                    long readStart = fs.Position;
                    int cur = br.ReadByte();
                    if (cur != 0x42)
                        continue;
                    cur = br.ReadByte();
                    if (cur != 0x4D)
                    {
                        fs.Position = readStart + 1;
                        continue;
                    }
                    // Possible BMP point
                    uint size = br.ReadUInt32();
                    uint reserved = br.ReadUInt32();
                    uint headerEnd = br.ReadUInt32();
                    if (reserved != 0 || headerEnd > size || size + readStart > len)
                    {
                        fs.Position = readStart + 1;
                        continue;
                    }
                    uint headerSize = br.ReadUInt32();
                    if (headerEnd < headerSize + 14)
                    {
                        fs.Position = readStart + 1;
                        continue;
                    }
                    fs.Position = readStart;
                    byte[] bmArr = br.ReadBytes((int)size);
                    string writeName = Path.Combine(baseName, String.Format("{0:000000}.bmp", curBm++));
                    File.WriteAllBytes(writeName, bmArr);
                }
            }
        }

        private void ShiftMap()
        {
            FileMapWwCc1Pc map;
            if (this.m_LoadedFile == null || String.IsNullOrEmpty(this.m_LoadedFile.LoadedFile) || !File.Exists(this.m_LoadedFile.LoadedFile) || (map = this.m_LoadedFile as FileMapWwCc1Pc) == null)
                return;
            string filename = map.LoadedFile;
            if (!filename.EndsWith(".bin", StringComparison.InvariantCultureIgnoreCase) && !filename.EndsWith(".int", StringComparison.InvariantCultureIgnoreCase))
                return;
            string writeName = filename.Substring(0, filename.Length - 4) + "1.ini";
            filename = filename.Substring(0, filename.Length - 3) + "ini";

            string text = File.ReadAllText(filename);
            Regex infRegex = new Regex("^(\\d+=[^,\\r\\n]+,[^,\\r\\n]+,\\d+,)(\\d+)(,\\d+,[^,\\r\\n]+,\\d+,[^,\\r\\n]+[\\r\\n])", RegexOptions.Multiline | RegexOptions.Singleline);
            Regex strRegex = new Regex("^(\\d+=[^,\\r\\n]+,[^,\\r\\n]+,\\d+,)(\\d+)(,\\d+,[^,\\r\\n]+[\\r\\n])", RegexOptions.Multiline | RegexOptions.Singleline);
            Regex uniRegex = new Regex("^(\\d+=[^,\\r\\n]+,[^,\\r\\n]+,\\d+,)(\\d+)(,\\d+,[^,\\r\\n]+,[^,\\r\\n]+[\\r\\n])", RegexOptions.Multiline | RegexOptions.Singleline);
            Regex ovrRegex = new Regex("^(\\d+)(=[^,\\r\\n]+[\\r\\n])", RegexOptions.Multiline | RegexOptions.Singleline);
            Regex terRegex = new Regex("^(\\d+)(=[^,\\r\\n]+,[^,\\r\\n]+[\\r\\n])", RegexOptions.Multiline | RegexOptions.Singleline);
            MatchCollection mci = infRegex.Matches(text);
            text = infRegex.Replace(text, m => m.Groups[1].Value + (Int32.Parse(m.Groups[2].Value) - 64) + m.Groups[3].Value);
            MatchCollection mcs = strRegex.Matches(text);
            text = strRegex.Replace(text, m => m.Groups[1].Value + (Int32.Parse(m.Groups[2].Value) - 64) + m.Groups[3].Value);
            MatchCollection mcu = uniRegex.Matches(text);
            text = uniRegex.Replace(text, m => m.Groups[1].Value + (Int32.Parse(m.Groups[2].Value) - 64) + m.Groups[3].Value);
            MatchCollection mco = ovrRegex.Matches(text);
            text = ovrRegex.Replace(text, m => (Int32.Parse(m.Groups[1].Value) - 64) + m.Groups[2].Value);
            MatchCollection mct = terRegex.Matches(text);
            text = terRegex.Replace(text, m => (Int32.Parse(m.Groups[1].Value) - 64) + m.Groups[2].Value);
            File.WriteAllText(writeName, text);
        }

        private void ListHighVerDotWriterFonts()
        {
            if (this.m_LoadedFile == null || String.IsNullOrEmpty(this.m_LoadedFile.LoadedFile) || !File.Exists(this.m_LoadedFile.LoadedFile))
                return;
            string curFile = Path.GetFileName(this.m_LoadedFile.LoadedFile);
            string path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            files = files.OrderBy(x => x.ToUpperInvariant()).ToArray();
            StringBuilder sb = new StringBuilder("Found files:");
            byte[] header = new byte[0x0A];
            for (int i = 0; i < files.Length; ++i)
            {
                FileInfo fi = new FileInfo(files[i]);
                if (curFile.Equals(fi.Name, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (fi.Length >= header.Length)
                {
                    using (FileStream fs = fi.OpenRead())
                    {
                        fs.Read(header, 0, header.Length);
                    }
                    if (header[5] != 0)
                    {
                        sb.AppendLine().Append(fi.Name).Append(new string(' ', 12 - fi.Name.Length)).Append(": v").Append(header[5]).Append(", height 0x").Append(String.Format("{0:X02}", header[4]));
                    }
                }
            }
            MessageBox.Show(sb.ToString().Replace("\r\n", "\n"), "Info", MessageBoxButtons.OK);
        }

        private void CountPixels()
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType[] frames = this.m_LoadedFile.IsFramesContainer ? this.m_LoadedFile.Frames : new SupportedFileType[] { this.m_LoadedFile };
            int nrOfFrames = frames.Length;
            if (nrOfFrames == 0)
                return;
            double[] results = new double[nrOfFrames];
            const int amountInSlice = 144;
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm;
                if (frame == null || (bm = frame.GetBitmap()) == null)
                    return;
                int height = bm.Height;
                int width = bm.Width;
                int stride;
                byte[] dataArgb = ImageUtils.GetImageData(bm, out stride, PixelFormat.Format32bppArgb);
                int lineOffset = 0;
                int blueCount = 0;
                int redCount = 0;
                int greenCount = 0;
                for (int y = 0; y < height; ++y)
                {
                    int offset = lineOffset;
                    for (int x = 0; x < width; ++x)
                    {
                        int blu = dataArgb[offset++]; // Blue
                        int grn = dataArgb[offset++]; // Green
                        int red = dataArgb[offset++]; // Red
                        int alp = dataArgb[offset++]; // Alpha
                        if (blu > 192 && red < 64 && grn < 64)
                            blueCount++;
                        if (blu < 64 && red > 192 && grn < 64)
                            redCount++;
                        if (blu < 64 && red < 64 && grn > 192)
                            greenCount++;
                    }
                    lineOffset += stride;
                }
                int total = blueCount + redCount + greenCount;
                double multiplier = (total * 1.0) / (blueCount * 1.0);
                double result = amountInSlice * multiplier;
                MessageBox.Show(this,
                    "Scan results for image " + (i + 1) + ":\n" +
                    "\nRed: " + redCount +
                    "\nGreen: " + greenCount +
                    "\nBlue: " + blueCount +
                    "\nTotal: " + total +
                    "\nAmount in blue: " + amountInSlice +
                    "\nMultiplier (total / blue): " + multiplier +
                    "\nTotal amount: " + amountInSlice + " * " + multiplier + " = " + result, GetTitle());
                results[i] = result;
            }
            int min = (int)results.Min();
            int max = (int)results.Max() + 1;
            int average = min + (max - min) / 2;
            int averageErr1 = average - amountInSlice;
            int averageErr2 = average + amountInSlice;
            Random rnd = new Random((int)(DateTime.Now.Ticks & 0xFFFFFFFF));
            int randomBetween = rnd.Next(averageErr1, averageErr2);
            MessageBox.Show(this,
                "Final results:\n" +
                "\nMinimum for given blue areas: " + min +
                "\nMaximum for given blue areas: " + max +
                "\nAverage between these two: " + average +
                "\nMinimum for random: average minus amount in slice: " + average + " - " + amountInSlice + " = " + averageErr1 +
                "\nMaximum for random: average plus amount in slice: " + average + " + " + amountInSlice + " = " + averageErr2 +
                "\nRandom value between these: " + randomBetween, GetTitle());
        }

        private void ExpandGif()
        {
            const int footerHeight = 30;
            const string text = "Hello, World!";
            const string fontFamily = "Arial";
            const int fontSize = 15;
            Color fontColor = Color.Black;

            Bitmap img;
            if (this.m_LoadedFile == null || (img = this.m_LoadedFile.GetBitmap()) == null || this.m_LoadedFile.BitsPerPixel != 8)
                return;
            int width = img.Width;
            int height = img.Height;
            int newHeight = height + footerHeight;
            Color[] pal = img.Palette.Entries;
            byte[] fullImage = new byte[width * newHeight];
            byte[] origImageData = ImageUtils.GetImageData(img, true);
            Array.Copy(origImageData, fullImage, origImageData.Length);
            byte[] commImageData;
            using (Bitmap bitmapComment = new Bitmap(width, footerHeight))
            {
                using (Graphics graphicImage = Graphics.FromImage(bitmapComment))
                using (Font font = new Font(fontFamily, fontSize))
                using (Brush brush = new SolidBrush(fontColor))
                {
                    graphicImage.Clear(Color.White);
                    graphicImage.DrawString(text, font, new SolidBrush(Color.Black), 0, footerHeight / 6);
                }
                int stride;
                byte[] commImageData32 = ImageUtils.GetImageData(bitmapComment, out stride);
                commImageData = ImageUtils.Convert32BitToPaletted(commImageData32, width, footerHeight, 8, true, pal, ref stride);
            }
            Array.Copy(commImageData, 0, fullImage, origImageData.Length, commImageData.Length);

            Bitmap newBm = ImageUtils.BuildImage(fullImage, width, newHeight, width, PixelFormat.Format8bppIndexed, pal, null);
            this.LoadTestFile(newBm);
        }

        private void DecryptDat()
        {
            if (this.m_LoadedFile == null)
                return;
            string path = this.m_LoadedFile.LoadedFile;
            if (String.IsNullOrEmpty(path) || !File.Exists(path))
                return;
            path = Path.GetDirectoryName(path);
            string[] files = { "OPTIONS.DAT", "OWNER.DAT", "WEAPONS.DAT" };
            foreach (string file in files)
            {
                string readPath = Path.Combine(path, file);
                byte[] fBytes = File.ReadAllBytes(readPath);
                for (int i = 0; i < fBytes.Length; ++i)
                {
                    fBytes[i] = (byte)(fBytes[i] - 120);
                }
                File.WriteAllBytes(readPath + ".txt", fBytes);
            }
        }

        private SupportedFileType ReduceRPlace()
        {
            const string palName = "0-pal.png";
            if (this.m_LoadedFile == null)
                return null;
            string path = this.m_LoadedFile.LoadedFile;
            if (String.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            path = Path.GetDirectoryName(path);
            string addPath = Path.Combine(path, "reduced");
            Color[] pal;
            string palPath = Path.Combine(path, palName);
            if (!File.Exists(palPath))
                return null;
            if (!Directory.Exists(addPath))
                Directory.CreateDirectory(addPath);
            using (FileImagePng palfile = new FileImagePng())
            {
                palfile.LoadFile(palPath);
                if (palfile.BitsPerPixel != 8)
                    return null;
                pal = palfile.GetColors();
                if (pal.Length == 0)
                    return null;
            }
            string[] files = Directory.GetFiles(path, "*.png");
            Regex dateFile = new Regex("\\d{10}\\.png");
            for (int i = 0; i < files.Length; ++i)
            {
                string filename = files[i];
                string name = Path.GetFileName(filename);
                if (palName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (!dateFile.IsMatch(name))
                    continue;

                byte[] newImg;
                string newName = Path.Combine(addPath, name);
                if (File.Exists(newName))
                    continue;
                using (FileImagePng png = new FileImagePng())
                {
                    try { png.LoadFile(filename); }
                    catch (FileTypeLoadException) { continue; }
                    Bitmap image = png.GetBitmap();
                    Bitmap[] result = ImageUtils.ImageToFrames(image, image.Width, image.Height, null, null, 8, pal, 0, 0);
                    if (result == null || result.Length == 0)
                        continue;
                    newImg = ImageUtils.GetPngImageData(result[0], pal.Length, true);
                    for (int j = 0; j < result.Length; ++j)
                    {
                        try { result[j].Dispose(); }
                        catch { /* Ignore */ }
                    }
                }
                File.WriteAllBytes(newName, newImg);
            }
            return null;
        }

        private SupportedFileType GetDataFromImage()
        {
            Bitmap loadedBm;
            if (this.m_LoadedFile == null || (loadedBm = this.m_LoadedFile.GetBitmap()) == null || this.m_LoadedFile.BitsPerPixel <= 8)
                return null;
            string path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile ?? ".");
            string filename = "image.jpg";
            string newPath = Path.Combine(path, filename);
            Color[] matchPalette = new Color[] { Color.Black, Color.White, Color.Gray };
            Bitmap[] result = ImageUtils.ImageToFrames(loadedBm, loadedBm.Width, loadedBm.Height, null, null, 8, matchPalette, 0, 0);
            if (result.Length == 0)
                return null;
            Bitmap bwImg = result[0];
            byte[] imgData = ImageUtils.GetImageData(bwImg, true);
            for (int i = 0; i < result.Length; ++i)
                result[i].Dispose();
            int length;
            for (length = 0; length < imgData.Length; ++length)
                if (imgData[length] > 1)
                    break;
            byte[] imgDataTrimmed = new byte[length];
            Array.Copy(imgData, imgDataTrimmed, length);
            byte[] byteData = ImageUtils.ConvertFrom8Bit(imgDataTrimmed, length, 1, 1, true);
            File.WriteAllBytes(newPath, byteData);
            FileImageJpg image = new FileImageJpg();
            image.LoadFile(byteData, filename);
            return image;
        }

        private SupportedFileType InvertIndices()
        {
            Bitmap loadedBm;
            if (this.m_LoadedFile == null || (loadedBm = this.m_LoadedFile.GetBitmap()) == null || this.m_LoadedFile.BitsPerPixel != 8)
                return null;
            byte[] imgData = ImageUtils.GetImageData(loadedBm, true);
            for (int i = 0; i < imgData.Length; ++i)
                imgData[i] = (byte)(255 - imgData[i]);
            Color[] palette = loadedBm.Palette.Entries.Reverse().ToArray();

            byte[] data;
            using (Bitmap newBm = ImageUtils.BuildImage(imgData, loadedBm.Width, loadedBm.Height, loadedBm.Width, PixelFormat.Format8bppIndexed, palette, null))
            using (MemoryStream ms = new MemoryStream())
            {
                newBm.Save(ms, ImageFormat.Png);
                data = ms.ToArray();
            }
            FileImagePng image = new FileImagePng();
            image.LoadFile(data, this.m_LoadedFile.LoadedFile);
            return image;
        }

        private SupportedFileType CorrectHue(SupportedFileType fileToProcess, double targetHue, double hueThreshold, double satMinimum)
        {
            if (fileToProcess == null)
                return null;
            bool container = fileToProcess.IsFramesContainer;
            SupportedFileType[] frames = container ? fileToProcess.Frames : new SupportedFileType[] { fileToProcess };
            FileFrames framesContainer = new FileFrames();
            //const Double hueThreshold = 15.0;
            //const Double satThreshold = 0.2;
            double hueLo = targetHue - hueThreshold;
            double hueHi = targetHue + hueThreshold;
            for (int i = 0; i < frames.Length; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap frImg = frame.GetBitmap();
                Bitmap newImg = null;
                if (frImg != null)
                {
                    byte[] imageContents = ImageUtils.GetImageData(frImg, PixelFormat.Format32bppRgb);
                    int width = frImg.Width;
                    int height = frImg.Height;
                    int stride = width * 4;
                    int lineIndex = 0;
                    for (int y = 0; y < height; ++y)
                    {
                        int lineEndIndex = lineIndex + stride;
                        for (int offs = lineIndex; offs < lineEndIndex; offs += 4)
                        {
                            int curCol = ArrayUtils.ReadInt32FromByteArrayLe(imageContents, offs);
                            ColorHSL cur = Color.FromArgb(curCol);
                            double hue = cur.Hue;
                            double sat = cur.Saturation;
                            if (hue > hueLo && hue < hueHi && sat > satMinimum)
                            {
                                ColorHSL colFixed = new ColorHSL(targetHue, sat, cur.Luminosity);
                                curCol = ((Color)colFixed).ToArgb();
                                ArrayUtils.WriteInt32ToByteArrayLe(imageContents, offs, curCol);
                            }
                        }
                        lineIndex = lineEndIndex;
                    }
                    newImg = ImageUtils.BuildImage(imageContents, width, height, stride, PixelFormat.Format32bppRgb, null, null);
                }
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFile(newImg, frame.LoadedFile);
                if (!container)
                    return framePic;
                framesContainer.AddFrame(framePic);
            }
            return framesContainer;
        }

        private SupportedFileType CorrectAlpha(SupportedFileType fileToProcess)
        {
            if (fileToProcess == null || fileToProcess.IsFramesContainer || fileToProcess.BitsPerPixel != 32)
                return null;
            Bitmap toProcess = fileToProcess.GetBitmap();
            int stride;
            byte[] dataArgb = ImageUtils.GetImageData(toProcess, out stride, PixelFormat.Format32bppRgb, true);
            int width = toProcess.Width;
            int height = toProcess.Height;
            int lineOffset = 0;
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                for (int x = 0; x < width; ++x)
                {
                    int alpInt = dataArgb[offset + 3]; // Alpha
                    if (alpInt != 0 && alpInt != 255)
                    {
                        double blu = dataArgb[offset + 0] / 255.0; // Blue
                        double grn = dataArgb[offset + 1] / 255.0; // Green
                        double red = dataArgb[offset + 2] / 255.0; // Red
                        double alp = alpInt / 255.0; // Alpha
                        blu = 1.0 - (1.0 - blu) / alp;
                        grn = 1.0 - (1.0 - grn) / alp;
                        red = 1.0 - (1.0 - red) / alp;
                        dataArgb[offset + 0] = (byte)(blu * 255); // Blue
                        dataArgb[offset + 1] = (byte)(grn * 255); // Green
                        dataArgb[offset + 2] = (byte)(red * 255); // Red
                    }
                    offset += 4;
                }
                lineOffset += stride;
            }
            byte[] data;
            using (Bitmap newBm = ImageUtils.BuildImage(dataArgb, width, height, stride, PixelFormat.Format32bppArgb, null, null))
            using (MemoryStream ms = new MemoryStream())
            {
                newBm.Save(ms, ImageFormat.Png);
                data = ms.ToArray();
            }
            FileImagePng image = new FileImagePng();
            image.LoadFile(data, this.m_LoadedFile.LoadedFile);
            return image;
        }

        private SupportedFileType ConvertDiff(SupportedFileType fileToProcess)
        {
            if (fileToProcess == null || fileToProcess.BitsPerPixel != 8)
                return null;
            bool container = fileToProcess.IsFramesContainer;
            SupportedFileType[] frames = container ? fileToProcess.Frames : new SupportedFileType[] { fileToProcess };
            FileFrames framesContainer = new FileFrames();
            string floor = Path.Combine(Path.GetDirectoryName(fileToProcess.LoadedFile), "floor.png");
            if (!File.Exists(floor))
                return null;
            byte[] floorContents;
            using (Bitmap floorBm = new Bitmap(floor))
            {
                if (floorBm.PixelFormat != PixelFormat.Format8bppIndexed)
                    return null;
                floorContents = ImageUtils.GetImageData(floorBm, PixelFormat.Format8bppIndexed, true);
            }

            for (int i = 0; i < frames.Length; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap frImg = frame.GetBitmap();
                Bitmap newImg = null;
                if (frImg != null)
                {
                    byte[] imageContents = ImageUtils.GetImageData(frImg, PixelFormat.Format8bppIndexed, true);
                    int width = frImg.Width;
                    int height = frImg.Height;
                    int stride = width;
                    if (imageContents.Length == floorContents.Length)
                    {
                        int lineIndex = 0;
                        for (int y = 0; y < height; ++y)
                        {
                            int lineEndIndex = lineIndex + stride;
                            for (int offs = lineIndex; offs < lineEndIndex; ++offs)
                            {
                                if (imageContents[offs] == floorContents[offs])
                                {
                                    imageContents[offs] = 0;
                                }
                            }
                            lineIndex = lineEndIndex;
                        }
                    }
                    newImg = ImageUtils.BuildImage(imageContents, width, height, stride, PixelFormat.Format8bppIndexed, frImg.Palette.Entries, null);
                }
                FileImageFrame framePic = new FileImageFrame();
                framePic.LoadFile(newImg, frame.LoadedFile);
                if (!container)
                    return framePic;
                framesContainer.AddFrame(framePic);
            }
            return framesContainer;
        }

        private SupportedFileType Fix6BitPalette(SupportedFileType fileToProcess)
        {
            if (fileToProcess == null || fileToProcess.BitsPerPixel != 4 || fileToProcess.GetBitmap() == null)
                return null;
            Color[] palette = fileToProcess.GetBitmap().Palette.Entries.ToArray();
            byte[] pal = new byte[0x300];
            int index = 0;
            for (int i = 0; i < palette.Length; ++i)
            {
                Color palCol = palette[i];
                pal[index++] = (byte)(palCol.R >> 2);
                pal[index++] = (byte)(palCol.G >> 2);
                pal[index++] = (byte)(palCol.B >> 2);
            }
            FilePalette6Bit palsix = new FilePalette6Bit();
            palsix.LoadFile(pal, Path.ChangeExtension(fileToProcess.LoadedFile, ".pal"));
            return palsix;
        }

        private void SwapPaletteColors()
        {
            Bitmap loadedBm;
            if (this.m_LoadedFile == null || (loadedBm = this.m_LoadedFile.GetBitmap()) == null || this.m_LoadedFile.BitsPerPixel > 8)
                return;
            Color[] pal = this.m_LoadedFile.GetColors();
            for (int c = 0; c < pal.Length; ++c)
            {
                Color col = pal[c];
                pal[c] = Color.FromArgb(col.B, col.R, col.G);
            }
            this.ExecuteThreaded(() => this.SetToPalette(m_LoadedFile, pal), true, false, false, "Setting different palette");
        }
    }
}
#endif
