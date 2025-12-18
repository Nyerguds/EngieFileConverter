using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Dynamix;

namespace CnC64FileConverter.Domain.FileTypes
{

    public class FileImgDynBmp : SupportedFileType
    {
        public override Int32 Width { get { return hdrWidth; } }
        public override Int32 Height { get { return hdrHeight; } }
        protected Int32 hdrWidth;
        protected Int32 hdrHeight;

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Dynamix Bmp"; } }
        public override String[] FileExtensions { get { return new String[] { "bmp" }; } }
        public override String ShortTypeDescription { get { return "Dynamix BMP animations file"; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 8; } }
        protected SupportedFileType[] m_FramesList = new SupportedFileType[0];

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList.ToArray(); } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return false; } }

        public FileImgDynBmp() { }

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

        public override Boolean ColorsChanged()
        {
            return false;
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            DynamixChunk mainChunk = DynamixChunk.ReadChunk(fileData, "BMP");
            if (mainChunk == null || mainChunk.Address != 0 || mainChunk.DataLength + 8 != fileData.Length)
                throw new FileTypeLoadException("BMP chunk not found: not a valid Dynamix BMP file header.");
            Byte[] data = mainChunk.Data;
            DynamixChunk infChunk = DynamixChunk.ReadChunk(data, "INF");
            if (infChunk == null)
                throw new FileTypeLoadException("INF chunk not found: not a valid Dynamix BMP file header.");
            Byte[] frameInfo = infChunk.Data;
            Int32 frames = (Int32)ArrayUtils.ReadIntFromByteArray(frameInfo, 0, 2, true);
            Int32[] widths = new Int32[frames];
            Int32[] heights = new Int32[frames];
            Int32 fullDataSize = 0;
            for (Int32 i = 0; i < frames; i++)
            {
                widths[i] = (Int32)ArrayUtils.ReadIntFromByteArray(frameInfo, 2+(i * 4), 2, true);
                heights[i] = (Int32)ArrayUtils.ReadIntFromByteArray(frameInfo, 4+(i * 4), 2, true);
                fullDataSize += (widths[i] * heights[i]);
            }
            this.hdrWidth = widths.Max();
            this.hdrHeight = heights.Max();
            DynamixChunk vqtChunk = DynamixChunk.ReadChunk(data, "VQT");
            if (vqtChunk == null)
                throw new FileTypeLoadException("VQT chunk not found: not a valid Dynamix BMP file header.");
            Byte[] frameData = vqtChunk.Data;
            Byte[] frameData2;
            //frameData2 = Compression.DynamixCompression.RleDecode(frameData, fullDataSize);
            //frameData2= Compression.DynamixCompression.LzwDecode(frameData, fullDataSize);
            frameData2 = new Byte[fullDataSize]; Array.Copy(frameData, 0, frameData2, 0, Math.Min(frameData.Length, fullDataSize));
            //File.WriteAllBytes("test.bm", frameData2);
            Int32 offset = 0;
            this.m_FramesList = new SupportedFileType[frames];
            for (Int32 i = 0; i < frames; i++)
            {
                Int32 curSize = widths[i] * heights[i];
                Byte[] image = new Byte[curSize];
                Array.Copy(frameData2, offset, image, 0, curSize);
                offset += curSize;
                this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
                Bitmap frameImage = ImageUtils.BuildImage(image, widths[i], heights[i], widths[i], PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this.ShortTypeName, frameImage, sourcePath, i);
                frame.SetBitsPerColor(this.BitsPerColor);
                frame.SetColorsInPalette(0);
                this.m_FramesList[i] = frame;
                //if (m_LoadedImage == null)
                //    m_LoadedImage = curImage;
            }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions, Boolean dontCompress)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            throw new NotSupportedException("Saving to this type is not supported.");
        }


    }
}