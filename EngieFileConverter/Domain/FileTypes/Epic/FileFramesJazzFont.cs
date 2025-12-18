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

        public override String IdCode { get { return "JazzFont"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Jazz Font"; } }
        public override String[] FileExtensions { get { return new String[] { "000" }; } }
        public override String LongTypeName { get { return "Jazz Uncompressed Font "; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask
        {
            get
            {
                Boolean[] transMask = new Boolean[0x100];
                transMask[0xFE] = true;
                return transMask;
            }
        }

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
            if (fileData.Length < 2)
                throw new FileTypeLoadException(ERR_NOHEADER);
            UInt32 symbols = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 2, true);
            Int32 offset = 2;
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            this.m_FramesList = new SupportedFileType[symbols];
            for (Int32 i = 0; i < symbols; ++i)
            {
                Int32 dataOffset = offset;
                if (offset + 8 > fileData.Length)
                    throw new FileTypeLoadException(ERR_SIZETOOSMALL);
                Int32 stride = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset, 2, true);
                Int32 width = stride * 4;
                Int32 height = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 2, 2, true);
                Int32 size = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 4, 2, true);
                Int32 empty = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, offset + 6, 2, true);
                if (stride * height != size)
                    throw new FileTypeLoadException("Image data size does not match width and height.");
                offset += 8;
                if (empty != 0)
                    throw new FileTypeLoadException("Reserved bytes don't match");
                if (fileData.Length < offset + size)
                    throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);

                Byte[] symbol = new Byte[size * 4];
                for (Int32 j = 0; j < 4; ++j)
                {
                    for (Int32 k = 0; k < size; k++)
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            SupportedFileType[] frames = this.PerformPreliminaryChecks(fileToSave);
            Int32 length = frames.Length;
            Int32[] frWidths = new Int32[length];
            Int32[] frHeights = new Int32[length];
            Byte[][] framesData = new Byte[length][];
            Int32 fullSize = 2;
            for (Int32 i = 0; i < length; ++i)
            {
                SupportedFileType frame = frames[i];
                Bitmap bm;
                if (frame == null || frame.Width == 0 || frame.Height == 0 || (bm = frame.GetBitmap()) == null)
                {
                    framesData[i] = new Byte[0];
                    continue;
                }
                Byte[] frameData = ImageUtils.GetImageData(bm, true);
                Int32 width = frame.Width;
                Int32 height = frame.Height;
                if (width % 4 != 0)
                {
                    Int32 newWidth = (width + 3) / 4 * 4;
                    Byte[] newFrameData = new Byte[height * newWidth];
                    ImageUtils.PasteOn8bpp(newFrameData, newWidth, height, newWidth, frameData, width, height, width, new Rectangle(0, 0, width, height), null, true);
                    frameData = newFrameData;
                    width = newWidth;
                }
                framesData[i] = frameData;
                frWidths[i] = width;
                frHeights[i] = height;
                fullSize += 8 + frameData.Length;
            }
            Byte[] fileData = new Byte[fullSize];
            ArrayUtils.WriteIntToByteArray(fileData, 0, 2, true, (UInt64)length);
            Int32 offset = 2;
            for (Int32 i = 0; i < length; ++i)
            {
                Int32 width = frWidths[i] / 4;
                Int32 height = frHeights[i];
                Int32 size = width * height;
                ArrayUtils.WriteIntToByteArray(fileData, offset, 2, true, (UInt64)width);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 2, 2, true, (UInt64)height);
                ArrayUtils.WriteIntToByteArray(fileData, offset + 4, 2, true, (UInt64)size);
                //ArrayUtils.WriteIntToByteArray(fileData, offset + 6, 2, true, 0);
                offset += 8;
                Byte[] symbolData = framesData[i];
                for (Int32 j = 0; j < 4; ++j)
                {
                    for (Int32 k = 0; k < size; k++)
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
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            SupportedFileType[] frames = fileToSave.IsFramesContainer ? fileToSave.Frames : new SupportedFileType[] { fileToSave };
            Int32 nrOfFrames = frames == null ? 0 : frames.Length;
            if (nrOfFrames == 0)
                throw new ArgumentException(ERR_NO_FRAMES, "fileToSave");
            for (Int32 i = 0; i < nrOfFrames; ++i)
            {
                SupportedFileType frame = frames[i];
                if (frame == null || frame.GetBitmap() == null)
                    continue;
                if (frame.BitsPerPixel != 8)
                    throw new ArgumentException(String.Format(ERR_INPUT_XBPP, 8), "fileToSave");
            }
            return frames;
        }
    }
}