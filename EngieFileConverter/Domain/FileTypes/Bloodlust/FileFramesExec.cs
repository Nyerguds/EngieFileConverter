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

        public override String IdCode { get { return "ExSpr"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Executioner Sprite"; } }
        public override String[] FileExtensions { get { return new String[] { "vol" }; } }
        public override String LongTypeName { get { return "Executioner Sprite File"; } }
        public override Boolean NeedsPalette { get { return true; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        protected SupportedFileType[] m_FramesList;

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
                transMask[0xFF] = true;
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
                throw new FileTypeLoadException(ERR_NO_HEADER);
            Int32 headersSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 0);
            if (headersSize == 0 || headersSize % 0x0D != 0)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            Int32 frames = headersSize / 0x0D;
            Int32 headerEnd = headersSize + 2;
            if (fileData.Length < headerEnd)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            // Frames are always a 4 byte header, and can not be 0x0. So minimum 1x1, so, 5 bytes.
            if (fileData.Length < frames * 5)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            this.m_FramesList = new SupportedFileType[frames];
            this.m_Palette = PaletteUtils.GenerateGrayPalette(8, this.TransparencyMask, false);
            Int32 curDataStart = headerEnd;
            for (Int32 i = 0; i < frames; ++i)
            {
                Int32 curHeaderPos = 2 + (0x0D * i);
                // Check header - derived from previous block's size
                Int32 width;
                Int32 height;
                String error = this.TestHeaderData(fileData, curDataStart, out width, out height);
                if (error != null)
                    throw new FileTypeLoadException(error);
                // Check current header
                //if (fileData[curHeaderPos + 0x0C] != 0)
                //    throw new FileTypeLoadException(ERR_BADHEADERDATA);
                Int32 curBlockSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curHeaderPos);
                Int32 frameNr = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, curHeaderPos + 0x02);
                Int32 posX = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x04);
                Int32 posY = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x06);
                Int32 posXMirr = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x08);
                Int32 posYMirr = ArrayUtils.ReadInt16FromByteArrayLe(fileData, curHeaderPos + 0x0A);
                Int32 zpos = fileData[curHeaderPos + 0x0C];
                Int32 ptr = curDataStart;
                if (fileData.Length < curDataStart)
                    throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                Boolean success;
                Byte[] mask = null;
                Byte[] imageData = ExecutionersCompression.DecodeChunk(fileData, ref ptr, 0xFF, ref mask, 0x01, out success);
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

        private String TestHeaderData(Byte[] fileData, Int32 curDataStart, out Int32 width, out Int32 height)
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