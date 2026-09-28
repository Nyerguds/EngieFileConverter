using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesInt33Cursor : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image4Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image4Bit | FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override int Width { get { return 16; } }
        public override int Height { get { return 16; } }
        public override string IdCode { get { return "Int33Cur"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "INT 33 Cursor"; } }
        public override string[] FileExtensions { get { return new string[] { "i33", "dat" }; } }
        public override string LongTypeName { get { return "INT 33 Graphics Pointer Shape"; } }
        public override bool NeedsPalette { get { return false; } }
        public override int BitsPerPixel { get { return 2; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary>
        /// This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source, and can normally also be saved from a single-image source.
        /// This setting should be ignored for types that are not set to IsFramesContainer.
        /// </summary>
        public override bool HasCompositeFrame { get { return true; } }
        /// <summary>Array of Booleans which defines for the palette which indices are transparent. Null for no forced transparency.</summary>
        public override bool[] TransparencyMask { get { return new bool[] { false, true, false, false }; } }


        public override void LoadFile(byte[] fileData, string filename)
        {
            LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(byte[] fileData)
        {
            LoadFromFileData(fileData, null);
        }

        private void LoadFromFileData(byte[] fileData, string filename)
        {
            if (fileData.Length == 0)
                throw new FileTypeLoadException("File is empty.");
            if (fileData.Length % 64 != 0)
                throw new FileTypeLoadException("INT 33 cursors are always data divisible by 64.");
            string shortName = filename == null ? "cursor" : Path.GetFileNameWithoutExtension(filename);
            Color[] palette = new Color[4];
            palette[0] = Color.Black;
            palette[1] = Color.FromArgb(0, Color.Fuchsia);
            palette[2] = Color.White;
            palette[3] = Color.Red;
            int frames = fileData.Length / 64;
            int fullWidth = frames * 16;
            int fullHeight = 16;
            int fullStride = frames * 16;
            byte[] fullImage = new byte[fullHeight * fullStride];
            this.m_FramesList = new SupportedFileType[frames];
            for (int i = 0; i < frames; ++i)
            {
                int start = i * 64;
                int start2 = start + 32;
                byte[] curImage1 = new byte[32];
                for (int j = 0; j < 32; j += 2)
                {
                    curImage1[j] = fileData[start + j + 1];
                    curImage1[j + 1] = fileData[start + j];
                }
                int stride1 = 2;
                curImage1 = ImageUtils.ConvertTo8Bit(curImage1, 16, 16, 0, 1, true, ref stride1);

                byte[] curImage2 = new byte[32];
                for (int j = 0; j < 32; j += 2)
                {
                    curImage2[j] = fileData[start2 + j + 1];
                    curImage2[j + 1] = fileData[start2 + j];
                }
                int stride2 = 2;
                curImage2 = ImageUtils.ConvertTo8Bit(curImage2, 16, 16, 0, 1, true, ref stride2);

                byte[] imageFinal = new byte[256];
                int strideFinal = 16;
                for (int j = 0; j < 256; ++j)
                {
                    imageFinal[j] = (byte)((curImage2[j] << 1) | curImage1[j]);
                }
                /*/
                StringBuilder sb = new StringBuilder();
                using (MemoryStream ms = new MemoryStream(fileData))
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
                //*/

                ImageUtils.PasteOn8bpp(fullImage, fullWidth, fullHeight, fullStride, imageFinal, 16, 16, strideFinal, new Rectangle(i * 16, 0, 16, 16), null, true);
                imageFinal = ImageUtils.ConvertFrom8Bit(imageFinal, 16, 16, 4, true, ref strideFinal);
                Bitmap frameImage = ImageUtils.BuildImage(imageFinal, 16, 16, strideFinal, PixelFormat.Format4bppIndexed, palette, Color.Empty);
                frameImage.Palette = ImageUtils.GetPalette(palette);

                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImage, shortName + i.ToString("D3") + ".dat", -1);
                frame.SetBitsPerColor(2);
                frame.SetFileClass(FileClass.Image4Bit);
                frame.SetNeedsPalette(false);
                //frame.SetExtraInfo(sb.ToString().TrimEnd('\n'));
                m_FramesList[i] = frame;
            }
            fullImage = ImageUtils.ConvertFrom8Bit(fullImage, fullWidth, fullHeight, 4, true, ref fullStride);
            Bitmap composite = ImageUtils.BuildImage(fullImage, fullWidth, fullHeight, fullStride, PixelFormat.Format4bppIndexed, palette, Color.Empty);

            composite.Palette = ImageUtils.GetPalette(palette);
            this.m_Palette = palette;
            this.m_LoadedImage = composite;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            Bitmap[] frames;

            if (!fileToSave.IsFramesContainer)
            {
                frames = new Bitmap[] {fileToSave.GetBitmap()};
            }
            else
            {
                SupportedFileType[] srcFrames = fileToSave.Frames;
                int len = srcFrames.Length;
                if (len == 0)
                    throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
                frames = new Bitmap[len];
                for (int i = 0; i < len; ++i)
                    frames[i] = srcFrames[i].GetBitmap();
            }
            int frLen = frames.Length;
            byte[][] frameBytes = new byte[frLen][];
            for (int i = 0; i < frLen; ++i)
            {
                Bitmap bm = frames[i];
                if (bm == null)
                    throw new FileTypeSaveException(ERR_FRAMES_EMPTY);
                if ((bm.PixelFormat & PixelFormat.Indexed) == 0)
                    throw new FileTypeSaveException(ERR_BPP_INPUT_INDEXED);
                if (bm.Width != 16 || bm.Height != 16)
                    throw new FileTypeSaveException(String.Format(ERR_DIMENSIONS_INPUT, 16, 16));
                int stride;
                byte[] origData = ImageUtils.GetImageData(bm, out stride);
                byte[] eightBitData = ImageUtils.ConvertTo8Bit(origData, 16, 16, 0, Image.GetPixelFormatSize(bm.PixelFormat), true);
                frameBytes[i] = eightBitData;
                for (int j = 0; j < eightBitData.Length; ++j)
                    if (eightBitData[i] > 3)
                        throw new FileTypeSaveException(String.Format(ERR_BPP_LOW_INPUT, 2, 3));
            }
            byte[] finalData8Bit = new byte[512 * frLen];
            for (int i = 0; i < frLen; ++i)
            {
                byte[] curImage = frameBytes[i];
                int curImageOffsAnd = i * 512;
                int curImageOffsXor = curImageOffsAnd + 256;
                for (int j = 0; j < 256; ++j)
                {
                    if ((curImage[j] & 1) != 0)
                        finalData8Bit[curImageOffsAnd + j] = 1;
                    if ((curImage[j] & 2) != 0)
                        finalData8Bit[curImageOffsXor + j] = 1;
                }
            }
            byte[] finalDataFlipped = ImageUtils.ConvertFrom8Bit(finalData8Bit, 16, 32 * frLen, 1, true);
            int finalLen = finalDataFlipped.Length;
            byte[] finalData = new byte[finalLen];
            for (int j = 0; j < finalLen; j += 2)
            {
                finalData[j] = finalDataFlipped[j + 1];
                finalData[j + 1] = finalDataFlipped[j];
            }
            return finalData;
        }
    }
}