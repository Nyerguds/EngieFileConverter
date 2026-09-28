using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Nyerguds.FileData.Bloodlust;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesExec : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override string IdCode { get { return "ExSpr"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Executioner Sprite"; } }
        public override string[] FileExtensions { get { return new string[] { "vol" }; } }
        public override string LongTypeName { get { return "Executioner Sprite File"; } }
        public override bool NeedsPalette { get { return true; } }
        public override int BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        protected SupportedFileType[] m_FramesList;

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
                transMask[0xFF] = true;
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
            int headersSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            if (headersSize == 0 || headersSize % 0x0D != 0)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            int frames = headersSize / 0x0D;
            int headerEnd = headersSize + 2;
            if (fileData.Length < headerEnd)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            // Frames are always a 4 byte header, and can not be 0x0. So minimum 1x1, so, 5 bytes.
            if (fileData.Length < frames * 5)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            int curDataStart = headerEnd;
            for (int i = 0; i < frames; ++i)
            {
                int curHeaderPos = 2 + (0x0D * i);
                // Check header - derived from previous block's size
                int width;
                int height;
                string error = this.TestHeaderData(fileData, curDataStart, out width, out height);
                if (error != null)
                    throw new FileTypeLoadException(error);
                // Check current header
                //if (fileData[curHeaderPos + 0x0C] != 0)
                //    throw new FileTypeLoadException(ERR_BADHEADERDATA);
                int curBlockSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curHeaderPos);
                int frameNr = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curHeaderPos + 0x02);
                int posX = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x04);
                int posY = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x06);
                int posXMirr = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x08);
                int posYMirr = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x0A);
                int zpos = fileData[curHeaderPos + 0x0C];
                int ptr = curDataStart;
                if (fileData.Length < curDataStart)
                    throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                bool success;
                byte[] mask = null;
                byte[] imageData = ExecutionersCompression.DecodeChunk(fileData, ref ptr, 0xFF, ref mask, 0x01, out success);
                Bitmap image = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
                if (imageData == null)
                    throw new FileTypeLoadException(ERR_DECOMPR);
                FileImageFrame sprite = new FileImageFrame();
                sprite.LoadFileFrame(this, this, image, sourcePath, 0);
                sprite.SetBitsPerColor(this.BitsPerPixel);
                sprite.SetFileClass(this.FrameInputFileClass);
                sprite.SetNeedsPalette(this.NeedsPalette);
                StringBuilder sb = new StringBuilder();
                sb.Append("Header: 13 bytes at offset ").Append(curHeaderPos)
                    .Append("\nData: ").Append(curBlockSize).Append(" bytes at offset ").Append(curDataStart)
                    .Append("\nSprite ID: ").Append(frameNr)
                    .Append("\nSprite position: ").Append(posX).Append(',').Append(posY)
                    .Append("\nMirrored position: ").Append(posXMirr).Append(',').Append(posYMirr)
                    .Append("\nZ-position: " + zpos);
                sprite.SetExtraInfo(sb.ToString());
                this.m_FramesList[i] = sprite;
                // Set sprite read position to end of current data.
                curDataStart += curBlockSize;
            }
            this.m_LoadedImage = null;
        }

        private string TestHeaderData(byte[] fileData, int curDataStart, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (fileData.Length < curDataStart + 4)
                return ERR_SIZE_TOO_SMALL_IMAGE;
            if (fileData[curDataStart] != 0x10 || fileData[curDataStart + 3] != 0xFF)
                return ERR_BAD_HEADER_DATA;
            width = fileData[curDataStart + 1];
            height = fileData[curDataStart + 2];
            if (width == 0 || height == 0)
                return ERR_DIM_ZERO;
            return null;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Nyerguds.Util.Option[] saveOptions)
        {
            throw new NotImplementedException();
        }
    }
}