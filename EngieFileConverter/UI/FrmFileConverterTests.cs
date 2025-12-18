#if DEBUG
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EngieFileConverter.UI
{
    /// <summary>
    /// To anyone who sees this, hello! And welcome to Nyerguds's random experiments and test code! These are meant to be
    /// linked to the [Edit] -> [Test bed] menu item through the TsmiTestBed function. Typically, only one of them is called.
    /// I keep them all here because they often contain interesting code. Note that some code that is or was referenced here
    /// might be from the Domain\Utils\UtilsSO.cs or Domain\Utils\ImageUtils\ImageUtilsSO.cs class, which, like this one,
    /// does not compile in Release mode.
    /// </summary>
    partial class FrmFileConverter
    {
        private void ExecuteTestCode()
        {
            // any test code can be linked in here.
            //this.ViewKortExeIcons();
            //this.MatrixImage();
            //this.LoadByteArrayImage();
            //this.CombineHue();
            //this.CreateSierpinskiImage();
            //this.ColorPsx();
            //this.ExpandRAMap();
            //this.GetColourPixel();
            //this.ExtractInts();
        }

        private void ViewKortExeIcons()
        {
            // Icons data from inside the King Arthur's K.O.R.T. exe file.
            Byte[] oneBppImage = new Byte[] {
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
                0xA0, 0x04, 0xE0, 0x04, 0x00, 0x00, 0xAE, 0x6E, 0xA4, 0x4A, 0xE4, 0x4A, 0xA4, 0x6E, 0x00, 0x00 };

            Color[] palette = new Color[4];
            palette[0] = Color.Black;
            palette[1] = Color.FromArgb(0, Color.Fuchsia);
            palette[2] = Color.White;
            palette[3] = Color.Red;
            ColorPalette pal = BitmapHandler.GetPalette(palette);
            Int32 frames = oneBppImage.Length / 64;
            Int32 fullWidth = 16;
            Int32 fullHeight = frames * 16;
            Int32 fullStride = 16;
            Byte[] fullImage = new Byte[fullStride * fullHeight];
            FileFrames framesContainer = new FileFrames();
            for (Int32 i = 0; i < frames; ++i)
            {
                Int32 start = i * 64;
                Int32 start2 = start + 32;
                Byte[] curImage1 = new Byte[32];
                for (Int32 j = 0; j < 32; j += 2)
                {
                    curImage1[j] = oneBppImage[start + j + 1];
                    curImage1[j + 1] = oneBppImage[start + j];
                }
                Int32 stride1 = 2;
                curImage1 = ImageUtils.ConvertTo8Bit(curImage1, 16, 16, 0, 1, true, ref stride1);

                Byte[] curImage2 = new Byte[32];
                for (Int32 j = 0; j < 32; j += 2)
                {
                    curImage2[j] = oneBppImage[start2 + j + 1];
                    curImage2[j + 1] = oneBppImage[start2 + j];
                }
                Int32 stride2 = 2;
                curImage2 = ImageUtils.ConvertTo8Bit(curImage2, 16, 16, 0, 1, true, ref stride2);

                Byte[] imageFinal = new Byte[256];
                Int32 strideFinal = 16;
                for (Int32 j = 0; j < 256; ++j)
                {
                    imageFinal[j] = (Byte)((curImage2[j] << 1) | curImage1[j]);
                }
                ImageUtils.PasteOn8bpp(fullImage, fullWidth, fullHeight, fullWidth, imageFinal, 16, 16, strideFinal, new Rectangle(0, i * 16, 16, 16), null, true);
                imageFinal = ImageUtils.ConvertFrom8Bit(imageFinal, 16, 16, 4, true, ref strideFinal);
                Bitmap frameImage = ImageUtils.BuildImage(imageFinal, 16, 16, strideFinal, PixelFormat.Format4bppIndexed, palette, Color.Empty);
                frameImage.Palette = pal;
                
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(framesContainer, "Icon", frameImage, "Icon" + i.ToString("D3") + ".png", -1);
                frame.SetBitsPerColor(2);
                frame.SetFileClass(FileClass.Image4Bit);
                frame.SetColorsInPalette(4);
                framesContainer.AddFrame(frame);
            }
            fullImage = ImageUtils.ConvertFrom8Bit(fullImage, fullWidth, fullHeight, 4, true, ref fullStride);
            Bitmap composite = ImageUtils.BuildImage(fullImage, fullWidth, fullHeight, fullStride, PixelFormat.Format4bppIndexed, palette, Color.Empty);
            composite.Palette = pal;
            framesContainer.SetCompositeFrame(composite);
            framesContainer.SetBitsPerColor(2);
            framesContainer.SetPalette(pal.Entries);
            framesContainer.SetColorsInPalette(4);
            framesContainer.SetCommonPalette(true);
            LoadTestFile(framesContainer);
        }

        private void CreateSierpinskiImage()
        {
            // Replace this with something creating/loading a bitmap
            using (Bitmap sierpinskiImage = ImageUtilsSO.GetSierpinski(800, 800))
                LoadTestFile(sierpinskiImage);
        }

        private void LoadByteArrayImage()
        {
            Byte[] imageBytes = {0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 
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
                            0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80, 0xC2, 0x80 };
            Color[] palette = new Color[0x100];
            for (Int32 i = 0; i < 0x100; ++i)
                palette[i] = Color.FromArgb(i, i, i);
            // Replace this with something creating/loading a bitmap
            using (Bitmap img = ImageUtils.BuildImage(imageBytes, 10, 20, 10, PixelFormat.Format8bppIndexed, palette, null))
                LoadTestFile(img);
        }

        private void CombineHue()
        {
            String filenameImage = "tank-b.png";
            String filenameColors = "tank-c.png";
            if (!File.Exists(filenameImage) || !File.Exists(filenameColors))
                return;
            // image data
            Bitmap im = BitmapHandler.LoadBitmap(filenameImage);
            // colour data
            Bitmap col = BitmapHandler.LoadBitmap(filenameColors);
            if (im.Width != col.Width || im.Height != col.Height)
                return;
            Int32 iStride;
            Byte[] imageData = ImageUtils.GetImageData(im, out iStride, PixelFormat.Format32bppArgb);
            Int32 cStride;
            Byte[] colourData = ImageUtils.GetImageData(col, out cStride, PixelFormat.Format32bppArgb);
            if (imageData.Length != colourData.Length || iStride != cStride)
                return;
            for (Int32 i = 0; i < imageData.Length; i += 4)
            {
                ColorHSL curPix = Color.FromArgb((Int32)ArrayUtils.ReadIntFromByteArray(imageData, i, 4, true));
                ColorHSL curCol = Color.FromArgb((Int32)ArrayUtils.ReadIntFromByteArray(colourData, i, 4, true));
                ColorHSL newCol = new ColorHSL(curCol.Hue, curCol.Saturation, curPix.Luminosity);
                UInt32 val = (UInt32)((Color)newCol).ToArgb();
                ArrayUtils.WriteIntToByteArray(imageData, i, 4, true, val);
            }
            // Replace this with something creating/loading a bitmap
            using (Bitmap img = ImageUtils.BuildImage(imageData, im.Width, im.Height, iStride, PixelFormat.Format32bppArgb, null, null))
                LoadTestFile(img);
        }

        private void ColorPsx()
        {
            String filenameImage = "SCA01EA_cutout.BIN";
            if (!File.Exists(filenameImage))
                return;
            Byte[] imageData = File.ReadAllBytes(filenameImage);
            Color[] palette = PaletteUtils.GenerateRainbowPalette(8, -1, null, true, 0, 160, true);
            using (Bitmap img = ImageUtils.BuildImage(imageData, 2048, 128, 2048, PixelFormat.Format8bppIndexed, palette, null))
                LoadTestFile(img);
        }

        private void ExpandRAMap()
        {
            if (!File.Exists("SCA01EA.INI"))
                return;
            IniFile ramap = new IniFile("SCA01EA.INI");
            Int32 lineNr = 1;
            Dictionary<String, String> sectionValues = ramap.GetSectionContent("MapPack");
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
                UInt32 uLength = (UInt32)ArrayUtils.ReadIntFromByteArray(compressedMap, readPtr, 4, true);
                Int32 length = (Int32)(uLength & 0xDFFFFFFF);
                readPtr += 4;
                Byte[] dest = new Byte[8192];
                Int32 readPtr2 = readPtr;
                Int32 decompressed = Nyerguds.FileData.Westwood.WWCompression.LcwDecompress(compressedMap, ref readPtr2, dest, 0);
                Array.Copy(dest, 0, mapFile, writePtr, decompressed);
                readPtr += length;
                writePtr += decompressed;
            }
            File.WriteAllBytes("SCA01EA.MAP", mapFile);
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
            File.WriteAllBytes("SCA01EA_corrected.BIN", mapFile2);
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
                LoadTestFile(img);
        }
        
        private void LoadTestFile(Bitmap loadImage)
        {
            LoadTestFile(loadImage, ".\\image.png");
        }

        private void LoadTestFile(Bitmap loadImage, String filename)
        {
            FileImage fileImage = new FileImagePng();
            using (MemoryStream ms = new MemoryStream())
            {
                loadImage.Save(ms, ImageFormat.Png);
                fileImage.LoadFile(ms.ToArray(), filename);
            }
            LoadTestFile(fileImage);
        }
        
        private void LoadTestFile(SupportedFileType loadImage)
        {
            SupportedFileType oldFile = this.m_LoadedFile;
            this.m_LoadedFile = loadImage;
            this.AutoSetZoom();
            this.ReloadUi(true);
            if (oldFile != null)
                oldFile.Dispose();
        }

        private void GetColourPixel()
        {
            Bitmap bm;
            if(m_LoadedFile == null || (bm = m_LoadedFile.GetBitmap()) == null || (bm.PixelFormat & PixelFormat.Indexed) == 0)
                return;
            Int32 x = 120;
            Int32 y = 96;
            Byte pixel = ImageUtilsSO.GetIndexedPixel(bm, x, y);
            MessageBox.Show(this, "The index of pixel [" + x + "," + y + "] is " + pixel + ".", GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        
        private void ExtractInts()
        {
            String s = "some text here = 5\nanother text line here = 4 with random garbage\n7\nfoo bar 9";
            List<Int32> nums = UtilsSO.ExtractInts(s);
            String ints = String.Join(", ", nums.Select(i => i.ToString()).ToArray());
            MessageBox.Show(this, "The numbers are " + ints, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
#endif