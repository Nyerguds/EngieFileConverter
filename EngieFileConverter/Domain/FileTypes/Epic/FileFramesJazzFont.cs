using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Epic;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{

    public class FileFramesJazzFont : SupportedFileType
    {

        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        protected SupportedFileType[] m_FramesList;

        public override string IdCode { get { return "JazzFont"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Jazz Font"; } }
        public override string[] FileExtensions { get { return new string[] { "000" }; } }
        public override string LongTypeName { get { return "Jazz Uncompressed Font "; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override bool[] TransparencyMask
        {
            get
            {
                bool[] transMask = new bool[0x100];
                transMask[0xFE] = true;
                return transMask;
            }
        }

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
            if (fileData.Length < 2)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            uint symbols = (uint)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            if (symbols == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            int offset = 2;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            this.m_FramesList = new SupportedFileType[symbols];
            for (int i = 0; i < symbols; ++i)
            {
                int dataOffset = offset;
                if (offset + 8 > fileData.Length)
                    throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                int stride = (int)ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true);
                int width = stride * 4;
                int height = (int)ArrayUtils.ReadIntFromByteArray(fileData, offset + 2, 2, true);
                int size = (int)ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 2, true);
                int empty = (int)ArrayUtils.ReadIntFromByteArray(fileData, offset + 6, 2, true);
                if (stride * height != size)
                    throw new FileTypeLoadException("Image data size does not match width and height.");
                offset += 8;
                if (empty != 0)
                    throw new FileTypeLoadException("Reserved bytes don't match");
                if (fileData.Length < offset + size)
                    throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);

                byte[] symbol = new byte[size * 4];
                for (int j = 0; j < 4; ++j)
                {
                    for (int k = 0; k < size; ++k)
                        symbol[k * 4 + j] = fileData[offset + k];
                    offset += size;
                }
                Bitmap symb = size == 0 ? null : ImageUtils.BuildImage(symbol, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, symb, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerPixel);
                frame.SetNeedsPalette(true);
                StringBuilder extraInfo = new StringBuilder();
                extraInfo.Append("Data offset: ").Append(dataOffset);
                extraInfo.Append('\n').Append("Data size: ").Append(size + 8);
                if (symb == null)
                    extraInfo.Append('\n').Append("Empty frame.\nInternal dimensions: ").Append(width).Append("x").Append(height);
                frame.SetExtraInfo(extraInfo.ToString());
                this.m_FramesList[i] = frame;
            }
            this.m_LoadedImage = null;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave);
            int length = frames.Length;
            int[] frWidths = new int[length];
            int[] frHeights = new int[length];
            byte[][] framesData = new byte[length][];
            int fullSize = 2;
            for (int i = 0; i < length; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm;
                if (frame == null || frame.Width == 0 || frame.Height == 0 || (bm = frame.GetBitmap()) == null)
                {
                    framesData[i] = new byte[0];
                    continue;
                }
                byte[] frameData = ImageUtils.GetImageData(bm, true);
                int width = frame.Width;
                int height = frame.Height;
                if (width % 4 != 0)
                {
                    int newWidth = (width + 3) / 4 * 4;
                    byte[] newFrameData = new byte[height * newWidth];
                    ImageUtils.PasteOn8bpp(newFrameData, newWidth, height, newWidth, frameData, width, height, width, new Rectangle(0, 0, width, height), null, true);
                    frameData = newFrameData;
                    width = newWidth;
                }
                framesData[i] = frameData;
                frWidths[i] = width;
                frHeights[i] = height;
                fullSize += 8 + frameData.Length;
            }
            byte[] fileData = new byte[fullSize];
            ArrayUtils.WriteIntToByteArray(fileData, 0, 2, true, (ulong)length);
            int offset = 2;
            for (int i = 0; i < length; ++i)
            {
                int width = frWidths[i] / 4;
                int height = frHeights[i];
                int size = width * height;
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (ulong)width);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 2, 2, true, (ulong)height);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 4, 2, true, (ulong)size);
                //ArrayUtils.WriteIntToByteArray(fileData, offset + 6, 2, true, 0);
                offset += 8;
                byte[] symbolData = framesData[i];
                for (int j = 0; j < 4; ++j)
                {
                    for (int k = 0; k < size; ++k)
                        fileData[offset + k] = symbolData[(k << 2) + j];
                    offset += size;
                }
            }
            return fileData;
        }

        private SupportedFileType[] PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null)
                throw new FileTypeSaveException(ERR_EMPTY_FILE);
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            int nrOfFrames = frames == null ? 0 : frames.Length;
            if (nrOfFrames == 0)
                throw new FileTypeSaveException(ERR_FRAMES_NEEDED);
            for (int i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    continue;
                if (frame.BitsPerPixel != 8)
                    throw new FileTypeSaveException(ERR_BPP_INPUT_EXACT, 8);
            }
            return frames;
        }
    }
}