#if DEBUG
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Nyerguds.FileData.EmotionalPictures;

namespace EngieFileConverter.UI
{
    /// <summary>
    /// To anyone who sees this, hello, and welcome to Nyerguds's random experiments and test code! This code onbnly compiles in Debug mode,
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
            this.ListHighVerDotWriterFonts();
        }

        private void LoadTestFile(SupportedFileType loadImage)
        {
            this.ReloadWithDispose(loadImage, true, true);
        }

        private void LoadTestFile(Bitmap loadImage, String filename, String extraInfo)
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

        private void LoadTestFile(Bitmap loadImage, String extraInfo)
        {
            this.LoadTestFile(loadImage, ".\\image.png", extraInfo);
        }

        private void ViewInt33MouseCursors()
        {
            // Cursors data from the KORT.EXE of the King Arthur's K.O.R.T. game.
            Byte[] int33MouseCursorKort = new Byte[]
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
            // Cursors data from the INSTALL.EXE of the Exhumed game.
            Byte[] int33MouseCursorExhumed = new Byte[]
            {
                0x3F, 0xF8, 0x0F, 0xE0, 0x07, 0xC0, 0x83, 0x83, 0xC3, 0x87, 0xC3, 0x87, 0x87, 0xC3, 0x0F, 0xE1,
                0x1F, 0xF0, 0x03, 0x80, 0x03, 0x80, 0x03, 0x80, 0x3F, 0xF8, 0x3F, 0xF8, 0x3F, 0xF8, 0x3F, 0xF8,
                0x00, 0x00, 0xC0, 0x07, 0x70, 0x1C, 0x38, 0x38, 0x18, 0x30, 0x18, 0x30, 0x30, 0x18, 0x60, 0x0C,
                0xC0, 0x06, 0x80, 0x03, 0xF8, 0x3F, 0x80, 0x03, 0x80, 0x03, 0x80, 0x03, 0x80, 0x03, 0x00, 0x00,
            };

            //*/
            Byte[] int33MouseCursor = int33MouseCursorKort;
            /*/
            Byte[] int33MouseCursor = int33MouseCursorExhumed;
            //*/

            Color[] palette = new Color[4];
            palette[0] = Color.Black;
            palette[1] = Color.FromArgb(0, Color.Fuchsia);
            palette[2] = Color.White;
            palette[3] = Color.Red;
            Int32 frames = int33MouseCursor.Length / 64;
            Int32 fullWidth = frames * 16;
            Int32 fullHeight = 16;
            Int32 fullStride = frames * 16;
            Byte[] fullImage = new Byte[fullHeight * fullStride];
            FileFrames framesContainer = new FileFrames();
            for (Int32 i = 0; i < frames; ++i)
            {
                Int32 start = i * 64;
                Int32 start2 = start + 32;
                Byte[] curImage1 = new Byte[32];
                for (Int32 j = 0; j < 32; j += 2)
                {
                    curImage1[j] = int33MouseCursor[start + j + 1];
                    curImage1[j + 1] = int33MouseCursor[start + j];
                }
                Int32 stride1 = 2;
                curImage1 = ImageUtils.ConvertTo8Bit(curImage1, 16, 16, 0, 1, true, ref stride1);

                Byte[] curImage2 = new Byte[32];
                for (Int32 j = 0; j < 32; j += 2)
                {
                    curImage2[j] = int33MouseCursor[start2 + j + 1];
                    curImage2[j + 1] = int33MouseCursor[start2 + j];
                }
                Int32 stride2 = 2;
                curImage2 = ImageUtils.ConvertTo8Bit(curImage2, 16, 16, 0, 1, true, ref stride2);

                Byte[] imageFinal = new Byte[256];
                Int32 strideFinal = 16;
                for (Int32 j = 0; j < 256; ++j)
                {
                    imageFinal[j] = (Byte) ((curImage2[j] << 1) | curImage1[j]);
                }
                StringBuilder sb = new StringBuilder();
                using (MemoryStream ms = new MemoryStream(int33MouseCursor))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    ms.Position = 64 * i;
                    for (Int32 j = 0; j < 32; ++j)
                    {
                        if (j == 16)
                            sb.Append('\n');
                        UInt16 line = br.ReadUInt16();
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
            Boolean x4 = false;

            Bitmap composite;
            if (x4)
            {
                Int32 fullWidth4 = fullWidth * 4;
                Int32 fullHeight4 = fullHeight * 4;
                Int32 fullStride4 = fullStride * 4;
                Byte[] fullImage4 = new Byte[fullStride4 * fullHeight4];
                for (Int32 y = 0; y < fullHeight4; ++y)
                    for (Int32 x = 0; x < fullWidth4; ++x)
                        fullImage4[y * fullStride4 + x] = fullImage[y / 4 * fullStride + x / 4];
                fullImage4 = ImageUtils.ConvertFrom8Bit(fullImage4, fullWidth4, fullHeight4, 4, true, ref fullStride4);
                composite = ImageUtils.BuildImage(fullImage4, fullWidth4, fullHeight4, fullStride4, PixelFormat.Format4bppIndexed, palette, Color.Empty);
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
            Byte[] imageBytes =
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
            for (Int32 i = 0; i < 0x100; ++i)
                palette[i] = Color.FromArgb(i, i, i);
            using (Bitmap img = ImageUtils.BuildImage(imageBytes, 10, 20, 10, PixelFormat.Format8bppIndexed, palette, null))
                this.LoadTestFile(img);
        }

        private void CombineHue()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null)
                return;
            String path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String filenameImage = "nukesilo_nocol.png";
            String filenameColors = "nukesilo_rlt_resize.jpg";
            String pathImage = Path.Combine(path, filenameImage);
            String pathColors = Path.Combine(path, filenameColors);
            if (!File.Exists(pathImage) || !File.Exists(pathColors))
                return;

            // image data
            Bitmap im = ImageUtils.LoadBitmap(pathImage);
            // colour data
            Bitmap col = ImageUtils.LoadBitmap(pathColors);
            if (im.Width != col.Width || im.Height != col.Height)
                return;
            Int32 iStride;
            Byte[] imageData = ImageUtils.GetImageData(im, out iStride, PixelFormat.Format32bppArgb);
            Int32 cStride;
            Byte[] colorData = ImageUtils.GetImageData(col, out cStride, PixelFormat.Format32bppArgb);
            if (imageData.Length != colorData.Length || iStride != cStride)
                return;
            for (Int32 i = 0; i < imageData.Length; i += 4)
            {
                ColorHSL curPix = Color.FromArgb(ArrayUtils.ReadInt32FromByteArrayLe(imageData, i));
                ColorHSL curCol = Color.FromArgb(ArrayUtils.ReadInt32FromByteArrayLe(colorData, i));
                ColorHSL newCol = new ColorHSL(curCol.Hue, curCol.Saturation, curPix.Luminosity, curPix.Alpha);
                UInt32 val = (UInt32) ((Color) newCol).ToArgb();
                ArrayUtils.WriteInt32ToByteArrayLe(imageData, i, val);
            }
            using (Bitmap img = ImageUtils.BuildImage(imageData, im.Width, im.Height, iStride, PixelFormat.Format32bppArgb, null, null))
                this.LoadTestFile(img);
        }

        private void ColorPsx()
        {
            String filenameImage = "SCA01EA_cutout.BIN";
            if (!File.Exists(filenameImage))
                return;
            Byte[] imageData = File.ReadAllBytes(filenameImage);
            Color[] palette = PaletteUtils.GenerateRainbowPalette(8, -1, null, true, 0, 160, true);
            using (Bitmap img = ImageUtils.BuildImage(imageData, 2048, 128, 2048, PixelFormat.Format8bppIndexed, palette, null))
                this.LoadTestFile(img);
        }

        private void ExpandRAMap()
        {
            String file = "SCG01EA";
            String ext = ".INI";

            String finalFile = file + ext;
            if (!File.Exists(finalFile))
                return;
            IniFile ramap = new IniFile(finalFile);
            Int32 lineNr = 1;
            //String packedSection = "MapPack";
            String packedSection = "OverlayPack";
            Dictionary<String, String> sectionValues = ramap.GetSectionContent(packedSection);
            StringBuilder sb = new StringBuilder();
            while (sectionValues.ContainsKey(lineNr.ToString()))
            {
                sb.Append(sectionValues[lineNr.ToString()]);
                lineNr++;
            }
            Byte[] compressedMap = Convert.FromBase64String(sb.ToString());
            Int32 readPtr = 0;
            Int32 writePtr = 0;
            Byte[] mapFile = new Byte[128 * 128 * 3];

            while (readPtr + 4 <= compressedMap.Length)
            {
                UInt32 uLength = ArrayUtils.ReadUInt32FromByteArrayLe(compressedMap, readPtr);
                Int32 length = (Int32) (uLength & 0xDFFFFFFF);
                readPtr += 4;
                Byte[] dest = new Byte[8192];
                Int32 readPtr2 = readPtr;
                Int32 decompressed = Nyerguds.FileData.Westwood.WWCompression.LcwDecompress(compressedMap, ref readPtr2, dest, 0);
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
            Byte[] matrix =
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
            Int32 x = 120;
            Int32 y = 96;
            Byte pixel = ImageUtilsSO.GetIndexedPixel(bm, x, y);
            MessageBox.Show(this, "The index of pixel [" + x + "," + y + "] is " + pixel + ".", GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void ExtractInts()
        {
            String s = "some text here = 5\nanother text line here = 4 with random garbage\n7\nfoo bar 9";
            List<Int32> nums = UtilsSO.ExtractInts(s);
            String ints = String.Join(", ", nums.Select(i => i.ToString()).ToArray());
            MessageBox.Show(this, "The numbers are " + ints, GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DecompressPppStringsFiles()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null)
                return;
            String path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String[] files = {"ENGLISH", "FRENCH", "GERMAN", "ITALIAN", "SCROLL"};
            foreach (String filename in files)
            {
                String fullPath = Path.Combine(path, filename + ".PPP");
                if (!File.Exists(fullPath))
                    continue;
                Byte[] buff = File.ReadAllBytes(fullPath);

                Byte[] buffDec;
                try
                {
                    buffDec = PppCompression.DecompressPppRle(buff);
                }
                catch (ArgumentException)
                {
                    continue;
                }
                String uncPath = Path.Combine(path, filename + ".dat");
                File.WriteAllBytes(uncPath, buffDec);

                Int32 len = buffDec.Length;
                Int32 ptr = 0;
                List<String> stringsFile = new List<String>();
                // DOS-865: Nordic
                Encoding dosenc = Encoding.GetEncoding(865);
                while (ptr < len)
                {
                    Int32 strLen = buffDec[ptr];
                    ptr++;
                    Byte[] strBuffer = new Byte[strLen];
                    Array.Copy(buffDec, ptr, strBuffer, 0, Math.Min(strLen, len - ptr));
                    for (Int32 c = 0; c < strLen; ++c)
                        strBuffer[c] = (Byte) ((strBuffer[c] << 1) | (strBuffer[c] >> 7));
                    String curLine = dosenc.GetString(strBuffer);
                    stringsFile.Add(curLine);
                    ptr += strLen;
                }
                String fullFile = String.Join(Environment.NewLine, stringsFile.ToArray());

                String fullPath2 = Path.Combine(path, filename + ".txt");
                File.WriteAllText(fullPath2, fullFile, new UTF8Encoding(true) /* dosenc*/);
            }
        }

        private void PixelsToPalette()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.GetBitmap()) == null)
                return;
            String path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String name = Path.GetFileNameWithoutExtension(this.m_LoadedFile.LoadedFile);
            Byte[] imageData = ImageUtils.GetImageData(this.m_LoadedFile.GetBitmap(), PixelFormat.Format24bppRgb);
            Int32 palLen = Math.Min(0x300, imageData.Length / 3 * 3);
            for (Int32 i = 0; i < palLen; i += 3)
            {
                Byte b = imageData[i];
                imageData[i] = imageData[i + 2];
                imageData[i + 2] = b;
            }
            Byte[] paletteData = new Byte[0x300];
            Array.Copy(imageData, paletteData, Math.Min(0x300, imageData.Length));
            FilePalette8Bit pal = new FilePalette8Bit();
            pal.LoadFile(paletteData, Path.Combine(path, name + ".pal"));
            this.LoadTestFile(pal);
        }

        private void CompareMaps()
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.LoadedFile == null)
                return;
            String folder = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String folder106c = Path.Combine(folder, "106c");
            String folderOrig = Path.Combine(folder, "orig");
            if (!Directory.Exists(folderOrig) || !Directory.Exists(folder106c))
                return;
            String[] allMaps = Directory.GetFiles(folderOrig, "*.bin");
            Int32 nrOfMaps = allMaps.Length;
            FileFrames frames = new FileFrames();
            for (Int32 i = 0; i < nrOfMaps; ++i)
            {
                String fileOrig = allMaps[i];
                String fileName = Path.GetFileName(fileOrig);
                String file106c = Path.Combine(folder106c, fileName);
                Boolean fileOrigExists = File.Exists(fileOrig);
                Boolean file106cExists = File.Exists(file106c);

                FileImageFrame framePic = new FileImageFrame();
                String newPath = Path.Combine(folder, fileName);
                if (!fileOrigExists || !file106cExists)
                {
                    framePic.LoadFile(null, newPath);
                    framePic.SetExtraInfo("Compare file not found.");
                    frames.AddFrame(framePic);
                    continue;
                }
                Byte[] mapDataOrig = File.ReadAllBytes(fileOrig);
                Byte[] mapData106c = File.ReadAllBytes(file106c);

                Byte[] imageData;
                Int32 stride;
                Byte colIndex;
                Color[] colors;
                using (FileMapWwCc1Pc mapOrig = new FileMapWwCc1Pc())
                {
                    mapOrig.LoadFile(mapDataOrig, fileOrig);
                    Bitmap mappic = mapOrig.GetBitmap();
                    imageData = ImageUtils.GetImageData(mappic, out stride, true);
                    Color[] cols = mappic.Palette.Entries;
                    if (cols.Length < 256)
                    {
                        colIndex = (Byte) cols.Length;
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
                const Int32 mapSize = 64 * 64;
                List<Int32> affectedCells = new List<Int32>();
                for (Int32 c = 0; c < mapSize; ++c)
                {
                    Int32 mapOffs = c << 1;
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
            Byte[] contents;
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
            String folder = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String[] files = Directory.GetFiles(folder, "*.png");
            foreach (String file in files)
                this.FixPngAspectRatio(file);
        }

        private void FixPngAspectRatio(String path)
        {
            const String physChunkId = "pHYs";
            // Read bytes
            Byte[] pngBytes;
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
            Int32 physLoc = PngHandler.FindPngChunk(pngBytes, physChunkId);
            if (physLoc == -1)
                return;
            Byte[] pngChunk = PngHandler.GetPngChunkData(pngBytes, physLoc);
            if (pngChunk.Length != 9)
                return;
            UInt32 dimX = ArrayUtils.ReadUInt32FromByteArrayBe(pngChunk, 0);
            UInt32 dimY = ArrayUtils.ReadUInt32FromByteArrayBe(pngChunk, 4);
            if (dimX == dimY)
                return;

            // Fix segment
            ArrayUtils.WriteInt32ToByteArrayBe(pngChunk, 0, 0xEC3);
            ArrayUtils.WriteInt32ToByteArrayBe(pngChunk, 4, 0xEC3);
            PngHandler.WritePngChunk(pngBytes, physLoc, physChunkId, pngChunk);

            // Make backup
            String folder = Path.GetDirectoryName(path);
            String origFolder = Path.Combine(folder, "orig");
            if (!Directory.Exists(origFolder))
                Directory.CreateDirectory(origFolder);
            String backup = Path.Combine(origFolder, Path.GetFileName(path));
            File.Copy(path, backup);

            // Save changes
            File.WriteAllBytes(path, pngBytes);
        }

        private void MakeBorderIcon()
        {
            Int32 size = 34;
            Int32 borderSize = 2;
            PixelFormat pixelFormat = PixelFormat.Format1bppIndexed;
            Int32 stride = size;
            Byte[] pixels = new Byte[size * stride];

            Color[] palette = new Color[] {Color.Pink, Color.Green};

            Byte paintIndex = 1;
            borderSize = Math.Min(borderSize, size);

            // Horizontal: just fill the whole block.

            // Top line
            Int32 end = stride * borderSize;
            for (Int32 i = 0; i < end; ++i)
                pixels[i] = paintIndex;

            // Bottom line
            end = stride * size;
            for (Int32 i = stride * (size - borderSize); i < end; ++i)
                pixels[i] = paintIndex;

            // Vertical: Both loops are inside the same y loop. It only goes over
            // the space between the already filled top and bottom parts.
            Int32 lineStart = borderSize * stride;
            Int32 yEnd = size - borderSize;
            Int32 rightStart = size - borderSize;
            for (Int32 y = borderSize; y < yEnd; ++y)
            {
                // left line
                for (Int32 x = 0; x < borderSize; ++x)
                    pixels[lineStart + x] = paintIndex;
                // right line
                for (Int32 x = rightStart; x < size; ++x)
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
            this.LoadTestFile(bm2, m_LoadedFile.LoadedFile, "Detected blobs: " + blobs.Count);
        }

        private void IndexedToArgb()
        {
            Bitmap bm;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (bm = shownFile.GetBitmap()) == null || bm.PixelFormat != PixelFormat.Format8bppIndexed)
                return;
            Int32 stride;
            Int32 width = bm.Width;
            Int32 height = bm.Height;
            Byte[] data = ImageUtils.GetImageData(bm, out stride);
            Color[] cols = bm.Palette.Entries;
            ColorSixBit[] sbcp = ColorUtils.GetSixBitColorPalette(cols);

            Byte[] palette = ColorUtils.GetSixBitPaletteData(sbcp);

            // Used data:
            // Byte[] data = image data
            // Byte[] palette = 6-bit palette
            // Int32 stride = number of bytes on one line of the image
            // Int32 width = image width
            // Int32 height = image height

            for (Int32 t = 0; t < 0x300; ++t)
                palette[t] = (Byte) (palette[t] * 4);
            Int32 lineOffset = 0;
            Int32 lineOffsetQuad = 0;
            Int32 strideQuad = width * 4;
            Byte[] dataArgb = new Byte[strideQuad * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                Int32 outOffset = lineOffsetQuad;
                for (Int32 x = 0; x < width; ++x)
                {
                    // get colour index, then get the correct location in the palette array
                    // by multiplying it by 3 (the length of one full colour)
                    Int32 colIndex = data[offset++] * 3;
                    dataArgb[outOffset++] = palette[colIndex + 2]; // Blue
                    dataArgb[outOffset++] = palette[colIndex + 1]; // Green
                    dataArgb[outOffset++] = palette[colIndex]; // Red
                    dataArgb[outOffset++] = (colIndex == 0 ? (Byte) 0 : (Byte) 255); // Alpha: set to 0 for background black
                }
                lineOffset += stride;
                lineOffsetQuad += strideQuad;
            }
            Bitmap bm2 = ImageUtils.BuildImage(dataArgb, width, height, strideQuad, PixelFormat.Format32bppArgb, null, null);
            this.LoadTestFile(bm2);
        }

        private void WriteIcoFileFromFrames()
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0 || m_LoadedFile.LoadedFile == null)
                return;
            Int32 frameNr = this.m_LoadedFile.Frames.Length;
            List<Image> images = new List<Image>();
            for (Int32 i = 0; i < frameNr; ++i)
            {
                Bitmap bm = this.m_LoadedFile.Frames[i].GetBitmap();
                if (bm != null)
                    images.Add(bm);
            }
            Byte[] fileBytes = FileIcon.ConvertImagesToIcoBytes(images.ToArray());
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(m_LoadedFile.LoadedFile), "test.ico"), fileBytes);
        }

        private void ChromaKey()
        {
            Bitmap bm;
            SupportedFileType shownFile = this.GetShownFile();
            if (shownFile == null || (bm = shownFile.GetBitmap()) == null)
                return;
            Int32 stride;
            Byte[] imageData = ImageUtils.GetImageData(bm, out stride, PixelFormat.Format32bppArgb);
            Int32 width = bm.Width;
            Int32 height = bm.Height;

            Color chroma = Color.Aquamarine;
            Double chromaHue = chroma.GetHue();
            Double hueThreshold = 50.0;
            Double satThreshold = 0.2;
            Double briThreshold = 0.2;
            Int32 lineOffset = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offsetQuad = lineOffset - 4;
                for (Int32 x = 0; x < width; ++x)
                {
                    offsetQuad += 4;

                    Byte b = imageData[offsetQuad + 0];
                    Byte g = imageData[offsetQuad + 1];
                    Byte r = imageData[offsetQuad + 2];
                    Byte a = imageData[offsetQuad + 3];
                    Color c = Color.FromArgb(a, r, g, b);
                    Double cHue = c.GetHue();
                    Double cSat = c.GetSaturation();
                    Double cBri = c.GetBrightness();
                    Double hueDiff = Math.Min(Math.Abs(chromaHue - cHue), 360 - Math.Abs(chromaHue - cHue));

                    if (cSat < satThreshold || cBri < briThreshold || hueDiff > hueThreshold)
                        continue;
                    Byte grayVal = (Byte) Math.Min((r * 0.3) + (g * 0.59) + (b * 0.11), 255);
                    imageData[offsetQuad + 0] = grayVal;
                    imageData[offsetQuad + 1] = grayVal;
                    imageData[offsetQuad + 2] = grayVal;
                    imageData[offsetQuad + 3] = (Byte) Math.Min(((180 - hueDiff) * 255 / 360), 255);
                }
                lineOffset += stride;
            }
            lineOffset = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offsetQuad = lineOffset - 4;
                for (Int32 x = 0; x < width; ++x)
                {
                    offsetQuad += 4;
                    Byte b = imageData[offsetQuad + 0];
                    Byte g = imageData[offsetQuad + 1];
                    Byte r = imageData[offsetQuad + 2];
                    Color c = Color.FromArgb(r, g, b);
                    Double cHue = c.GetHue();
                    Double cSat = c.GetSaturation();
                    Double cBri = c.GetBrightness();

                    if (cHue >= 60 && cHue <= 130 && cSat >= 0.15 && cBri > 0.15)
                    {
                        if ((r * b) != 0 && (g * g) / (r * b) >= 1.5)
                        {
                            imageData[offsetQuad + 0] = (Byte) Math.Max(r * 1.4, 255);
                            imageData[offsetQuad + 1] = g;
                            imageData[offsetQuad + 2] = (Byte) Math.Max(b * 1.4, 255);
                        }
                        else
                        {
                            imageData[offsetQuad + 0] = (Byte) Math.Max(r * 1.2, 255);
                            imageData[offsetQuad + 1] = g;
                            imageData[offsetQuad + 2] = (Byte) Math.Max(b * 1.2, 255);
                        }
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
            Int32 width = bm.Width;
            Int32 height = bm.Height;
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

            Int32 width = image.Width;
            Int32 height = image.Height;
            BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Int32 stride = sourceData.Stride;
            Byte[] data = new Byte[stride * height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            image.UnlockBits(sourceData);

            Int32 lastWhiteLine = ImageUtilsSO.GetLastClearLine(data, stride, width, height, Color.White);
            if (lastWhiteLine == height - 1)
                MessageBox.Show(this, "Nothing touching the bottom edge!");
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
            Int32 width = image.Width;
            Int32 height = image.Height;
            Byte[] imageData = ImageUtilsSO.GetChannelBytes(image, 0);
            Int32 stride = width;
            Byte[] bayerData = ImageUtilsSO.BayerToRgb2x2Orig(imageData, ref width, ref height, ref stride, true, false);
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
            Int32 width = image.Width / 2;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] imgData1 = ImageUtils.GetImageData(image, out stride, true);
            Byte[] imgData2 = new Byte[width * height];
            Int32 readLineOffs = 0;
            Int32 writeLineOffs = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 readOffs = readLineOffs;
                Int32 readOffsEnd = readLineOffs + width * 2;
                for (; readOffs < readOffsEnd; readOffs += 2)
                {
                    Int32 value = ((imgData1[readOffs + 1] << 8) + imgData1[readOffs]) >> 4;
                    imgData2[writeLineOffs] = (Byte) value;
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
            Int32 width;
            Int32 height;
            SupportedFileType current = this.m_LoadedFile;
            if (current == null || !current.IsFramesContainer || current.Frames.Length < 2
                || current.Frames[0] == null || (picFootshape = current.Frames[0].GetBitmap()) == null
                || current.Frames[1] == null || (materialImage = current.Frames[1].GetBitmap()) == null
                || (width = picFootshape.Width) != materialImage.Width || (height = picFootshape.Height) != materialImage.Height)
                return;
            Int32 stride;
            // extract bytes of shape & alpha image
            Byte[] shapeImageBytes = ImageUtils.GetImageData(picFootshape, out stride, PixelFormat.Format32bppArgb);
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
            String testFilesPath = Path.GetFullPath(Path.Combine(GeneralUtils.GetApplicationPath(), "..\\..\\..\\..\\1_testdata"));
            String patternsFolder = Path.Combine(testFilesPath, "feet_patterns");
            //String materialsFolder = Path.Combine(testFilesPath, "feet_material");
            //foreach (String materialImagePath in Directory.GetFiles(materialsFolder))
            //    File.Delete(materialImagePath);
            String finalFolder = Path.Combine(testFilesPath, "feet_final");
            foreach (String finalFile in Directory.GetFiles(finalFolder))
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
            Int32 width = image.Width;
            Int32 height = image.Height;
            Int32 stride;
            Byte[] shapeImageBytes = ImageUtils.GetImageData(image, out stride, PixelFormat.Format32bppArgb);
            Bitmap blackImage = ImageUtilsSO.ExtractBlackImage(shapeImageBytes, width, height, stride);
            this.LoadTestFile(blackImage);
        }

        private void MakeTrans()
        {
            const Byte bgRedR = 0x96;
            const Byte bgRedG = 0x0b;
            const Byte bgRedB = 0x08;

            Bitmap img1Red;
            Bitmap img2Black;
            Int32 width;
            Int32 height;
            SupportedFileType current = this.m_LoadedFile;
            if (current == null || !current.IsFramesContainer || current.Frames.Length < 2
                || current.Frames[0] == null || (img1Red = current.Frames[0].GetBitmap()) == null
                || current.Frames[1] == null || (img2Black = current.Frames[1].GetBitmap()) == null
                || (width = img1Red.Width) != img2Black.Width || (height = img1Red.Height) != img2Black.Height)
                return;
            Int32 stride = ImageUtils.GetClassicStride(width, 32);
            Byte[] img1RedBytes = ImageUtils.GetImageData(img1Red, PixelFormat.Format32bppArgb);
            Byte[] img2BlackBytes = ImageUtils.GetImageData(img2Black, PixelFormat.Format32bppArgb);
            Int32 lineOffset = 0;
            const Int32 threshold = 160;
            const Int32 thresholdBlack = 5;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                for (Int32 x = 0; x < width; ++x)
                {
                    Byte b1r = img1RedBytes[offset];
                    Byte b2b = img2BlackBytes[offset];
                    Byte g1r = img1RedBytes[offset + 1];
                    Byte g2b = img2BlackBytes[offset + 1];
                    Byte r1r = img1RedBytes[offset + 2];
                    Byte r2b = img2BlackBytes[offset + 2];
                    Int32 diffB = Math.Abs(b1r - b2b);
                    Int32 diffG = Math.Abs(g1r - g2b);
                    Int32 diffR = Math.Abs(r1r - r2b);
                    if (b2b < thresholdBlack && g2b < thresholdBlack && r2b < thresholdBlack)
                        //if (diffR > threshold || diffG > threshold || diffB > threshold)
                        //if (diffR > threshold || diffG > threshold || diffB > threshold || (b2b < thresholdBlack && g2b < thresholdBlack && r2b < thresholdBlack))
                    {
                        //Int32 diffB1Red = Math.Abs(b1 - bgRedB);
                        //Int32 diffG1Red = Math.Abs(g1 - bgRedG);
                        Int32 diffR1Red = Math.Abs(r1r - bgRedR);
                        //img1RedBytes[offset + 0] = r1r;
                        //img1RedBytes[offset + 1] = r1r;
                        //img1RedBytes[offset + 2] = r1;
                        img1RedBytes[offset + 3] = (Byte) diffR1Red;
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
            String testFilesPath = Path.GetFullPath(Path.Combine(GeneralUtils.GetApplicationPath(), "..\\..\\..\\..\\1_testdata"));
            String patternsFolder = Path.Combine(testFilesPath, "feet_patterns");
            List<Bitmap> images = new List<Bitmap>();
            String[] files = Directory.GetFiles(patternsFolder);
            foreach (String imagePath in files)
                images.Add(new Bitmap(imagePath));
            Int32 width = images.First().Width; //all images in list have the same width so I take the first
            Int32 height = 0;
            for (Int32 i = 0; i < images.Count; ++i) //the list has 300 images.
            {
                height += images[i].Height;
            }
            Bitmap bitmap2 = new Bitmap(width, height);
            bitmap2.SetResolution(72, 72);
            using (Graphics g = Graphics.FromImage(bitmap2))
            {

                height = 0;
                for (Int32 i = 0; i < images.Count; ++i)
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
            if (this.m_LoadedFile == null || this.m_LoadedFile.Frames == null || this.m_LoadedFile.Frames.Length == 0 || m_LoadedFile.LoadedFile == null)
                return;
            List<String> originalNames = this.m_LoadedFile.Frames.Select(f => f.LoadedFile).Where(File.Exists).ToList();
            if (originalNames.Count == 0)
                return;
            String outPath = Path.GetDirectoryName(originalNames[0]);
            ImageUtilsSO.WriteImagesToIcons(originalNames, outPath);
        }

        private void ViewSTrisHidden()
        {
            // This image appears in a number of games. it may be the logo of some old adult warez distribution group.
            // The files are always the same: a 6-bit colour palette in plain text format, and a raw 320x200 8-bit data array
            // without header, inconspicuously named as if they are files of the game itself. The image is always the same too:
            // Eddie, the zombie mascot of the band "Iron Maiden", carrying a British flag as depicted on the album "The Trooper".

            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null || (!(this.m_LoadedFile is FileImgStris) && !(this.m_LoadedFile is FileFrames && ((FileFrames)m_LoadedFile).EmbeddedType == typeof(FileImgStris))))
                return;
            String path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String palFile = Path.Combine(path, "14.sex");
            String imgFile = Path.Combine(path, "15.sex");
            if (!File.Exists(palFile) || !File.Exists(imgFile))
                return;
            Byte[] palB = File.ReadAllBytes(palFile);
            for (Int32 i = 0; i < palB.Length; ++i)
                if (palB[i] != 0x0D && palB[i] != 0x0A && palB[i] != 0x20 && (palB[i] < '0' || palB[i] > '9'))
                    return;
            String palT = Encoding.ASCII.GetString(palB);
            Regex line = new Regex("\\s*(\\d+)\\s*(\\d\\d?)\\s*(\\d\\d?)\\s*(\\d\\d?)\\s*?\r\n");
            MatchCollection mc = line.Matches(palT);
            Color[] palette = new Color[256];
            foreach (Match m in mc)
            {
                Int32 index = Int32.Parse(m.Groups[1].Value);
                if (index >= 256)
                    continue;
                palette[index] = new ColorSixBit(Byte.Parse(m.Groups[2].Value), Byte.Parse(m.Groups[3].Value), Byte.Parse(m.Groups[4].Value));
            }
            Byte[] imgB = File.ReadAllBytes(imgFile);
            Bitmap image = ImageUtils.BuildImage(imgB, 320, 200, 320, PixelFormat.Format8bppIndexed, palette, Color.Black);
            this.LoadTestFile(image);
        }

        private void Ikegami()
        {
            Byte[] graphic =
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
            Byte[] conv1 = ImageUtils.ConvertTo8Bit(graphic, 8, 56, 0, 1, false);
            Byte[] conv2 = new Byte[conv1.Length];
            for (int i = 0; i < conv1.Length; ++i)
                conv2[(56 * (i%8)) + i / 8] = conv1[i];
            //Bitmap ikegami = ImageUtils.BuildImage(conv2, 56, 8, 56, PixelFormat.Format8bppIndexed, new Color[] { Color.Black, Color.White }, null);
            Byte[] conv3 = ImageUtils.ConvertFrom8Bit(conv2, 56, 8, 1, true);
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
            Byte[] dibdata = new Byte[]
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
            Bitmap cursorImage = DibHandler.ImageFromDib(dibdata, 0, 0, true, false, out pf);
            this.LoadTestFile(cursorImage);
        }

        private void ReplaceImagePalette()
        {
            if (this.m_LoadedFile == null || (this.m_LoadedFile.LoadedFile) == null || this.m_LoadedFile is FileFrames || this.m_LoadedFile.BitsPerPixel != 8)
                return;
            String filename = this.m_LoadedFile.LoadedFile;
            if (filename == null)
                return;
            Color[] newPalette = new Color[0x100];
            using (Bitmap bm = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                for (Int32 i = 0; i < 0x100; ++i)
                {
                    Color c = bm.Palette.Entries[i];
                    newPalette[i] = Color.FromArgb(c.R, c.G, c.B);
                }
            }
            Int32 stride;
            Int32 width;
            Int32 height;
            Color[] curPalette;
            Byte[] imageData;
            // This 'using' block is kept small; extract the data and then dispose everything.
            using (Bitmap image = ImageUtils.CloneImage(m_LoadedFile.GetBitmap()))
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
            Byte[] match = new Byte[curPalette.Length];
            for (Int32 i = 0; i < curPalette.Length; ++i)
                match[i] = (Byte)ColorUtils.GetClosestPaletteIndexMatch(curPalette[i], newPalette);
            // Go over the actual pixels in the image data and replace the colours.
            Int32 currentLineOffset = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = currentLineOffset;
                for (Int32 x = 0; x < width; ++x)
                {
                    // Replace index with index of the closest match found before for that colour.
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

        private void AutoRemapPalette()
        {
            if (this.m_LoadedFile == null || !(this.m_LoadedFile is FileFrames) || this.m_LoadedFile.Frames.Length < 2
                || this.m_LoadedFile.Frames.Any(f => f.BitsPerPixel != 8) || this.m_LoadedFile.Frames[0].Width != this.m_LoadedFile.Frames[1].Width
                || this.m_LoadedFile.Frames[0].Height != this.m_LoadedFile.Frames[1].Height)
                return;
            Int32 width = this.m_LoadedFile.Frames[0].Width;
            Int32 height = this.m_LoadedFile.Frames[0].Height;
            Byte[] imgBeta= ImageUtils.GetImageData(this.m_LoadedFile.Frames[0].GetBitmap(), true);
            Byte[] imgFinl = ImageUtils.GetImageData(this.m_LoadedFile.Frames[1].GetBitmap(), true);
            Dictionary<Byte, List<Int32>> differentIndices = new Dictionary<Byte, List<Int32>>();
            Dictionary<Int32, Int32> matchedIndices = new Dictionary<Int32, Int32>();
            Int32 lineOffset = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                for (Int32 x = 0; x < width; ++x)
                {
                    Byte indexBeta = imgBeta[offset];
                    Byte indexFinl = imgFinl[offset];
                    Int32 dictVal = indexBeta | (indexFinl << 16);
                    if (matchedIndices.ContainsKey(dictVal))
                        matchedIndices[dictVal]++;
                    else
                    {
                        matchedIndices[dictVal] = 1;
                        if (!differentIndices.ContainsKey(indexBeta))
                            differentIndices[indexBeta] = new List<Int32>();
                        differentIndices[indexBeta].Add(dictVal);
                    }
                    offset++;
                }
                lineOffset += width;
            }
            Byte[] indices = differentIndices.Keys.OrderBy(x => x).ToArray();
            Byte[] fullPaletteRemap = new Byte[0x100];
            Int32[] fullPaletteRemapAmount = new Int32[0x100];
            Boolean[] alreadyMatched = new Boolean[0x100];
            for (Int32 i = 0; i < indices.Length; ++i)
            {
                Byte indexBeta = indices[i];
                List<Int32> differentMatches = differentIndices[indexBeta];
                Int32 maxAmount = 0;
                Int32 maxIndex = -1;
                foreach (Int32 matchId in differentMatches)
                {
                    Int32 amount = matchedIndices[matchId];
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
                    fullPaletteRemap[indexBeta] = (Byte)maxIndex;
                    fullPaletteRemapAmount[indexBeta] = maxAmount;
                    alreadyMatched[indexBeta] = true;
                }
            }
            for (Int32 i = 0; i < 0x100; ++i)
            {
                if (!alreadyMatched[i])
                    fullPaletteRemap[i] = 0x01;
            }
            for (Int32 i = 0; i < imgBeta.Length; i++)
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
            Int32 stride;
            Byte[] imgData = ImageUtils.GetImageData(loadedBm, out stride);
            Int32 skipsize = m_LoadedFile.BitsPerPixel / 8;
            Int32 height = loadedBm.Height;
            Int32 width = loadedBm.Width;
            //ARGB = [BB GG RR AA]
            Int32 linePtr = 0;
            for (Int32 y = 0; y < height; y++)
            {
                Int32 ptr = linePtr;
                for (Int32 x = 0; x < width; ++x)
                {
                    Byte blue = imgData[ptr];
                    Byte red = imgData[ptr+2];
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
            SupportedFileType[] frames = this.m_LoadedFile.IsFramesContainer ? this.m_LoadedFile.Frames : new SupportedFileType[] {this.m_LoadedFile};
            Int32 nrOfFrames = frames.Length;
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

            String icoPath = "engie.ico";
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
                Byte[] icoFile = ImageUtilsSO.ConvertImagesToIco(includedSizes);
                File.WriteAllBytes(icoPath, icoFile);
            }
        }

        private void ExtractBitmaps()
        {
            String fileToHandle = "PCS0396S.MVB";
            if (!File.Exists(fileToHandle))
                return;
            String baseName = Path.GetFileNameWithoutExtension(fileToHandle);
            if (!Directory.Exists(baseName))
            {
                Directory.CreateDirectory(baseName);
            }
            else
            {
                String[] bmpFiles = Directory.GetFiles(baseName, "*.bmp", SearchOption.TopDirectoryOnly);
                for (Int32 i = 0; i < bmpFiles.Length; ++i)
                    File.Delete(bmpFiles[i]);
            }
            using (FileStream fs = new FileStream(fileToHandle, FileMode.Open))
            using(BinaryReader br = new BinaryReader(fs))
            {
                Int32 curBm = 0;
                Int64 len = fs.Length;                
                while (fs.Position + 0x13 < len)
                {
                    Int64 readStart = fs.Position;
                    Int32 cur = br.ReadByte();
                    if (cur != 0x42)
                        continue;
                    cur = br.ReadByte();
                    if (cur != 0x4D)
                    {
                        fs.Position = readStart + 1;
                        continue;
                    }
                    // Possible BMP point
                    UInt32 size = br.ReadUInt32();
                    UInt32 reserved = br.ReadUInt32();
                    UInt32 headerEnd = br.ReadUInt32();
                    if (reserved != 0 || headerEnd > size || size + readStart > len)
                    {
                        fs.Position = readStart + 1;
                        continue;
                    }
                    UInt32 headerSize = br.ReadUInt32();
                    if (headerEnd < headerSize + 14)
                    {
                        fs.Position = readStart + 1;
                        continue;
                    }
                    fs.Position = readStart;
                    Byte[] bmArr = br.ReadBytes((Int32) size);
                    String writeName = Path.Combine(baseName, String.Format("{0:000000}.bmp", curBm++));
                    File.WriteAllBytes(writeName, bmArr);
                }
            }
        }

        private void ShiftMap()
        {
            FileMapWwCc1Pc map;
            if (this.m_LoadedFile == null || String.IsNullOrEmpty(this.m_LoadedFile.LoadedFile) || !File.Exists(this.m_LoadedFile.LoadedFile) || (map = m_LoadedFile as FileMapWwCc1Pc) == null)
                return;
            String filename = map.LoadedFile;
            if (!filename.EndsWith(".bin", StringComparison.InvariantCultureIgnoreCase) && !filename.EndsWith(".int", StringComparison.InvariantCultureIgnoreCase))
                return;
            String writeName = filename.Substring(0, filename.Length - 4) + "1.ini";
            filename = filename.Substring(0, filename.Length - 3) + "ini";

            String text = File.ReadAllText(filename);
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
            String curFile = Path.GetFileName(this.m_LoadedFile.LoadedFile);
            String path = Path.GetDirectoryName(this.m_LoadedFile.LoadedFile);
            String[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            files = files.OrderBy(x=>x.ToUpperInvariant()).ToArray();
            StringBuilder sb = new StringBuilder("Found files:");
            Byte[] header = new Byte[0x0A];
            for (Int32 i = 0; i < files.Length; ++i)
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
                        sb.AppendLine().Append(fi.Name).Append(new String(' ', 12 - fi.Name.Length)).Append(": v").Append(header[5]).Append(", height 0x").Append(String.Format("{0:X02}", header[4]));
                    }
                }
            }
            MessageBox.Show(sb.ToString().Replace("\r\n", "\n"), "Info", MessageBoxButtons.OK);
        }

    }
}
#endif
