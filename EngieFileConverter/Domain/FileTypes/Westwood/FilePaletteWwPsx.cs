using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FilePaletteWwPsx : SupportedFileType
    {
        public override string IdCode { get { return "WwPalPsx"; } }
        protected SupportedFileType[] m_FramesList;
        public override FileClass FileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit | FileClass.FrameSet; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "WW PSX Pal"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return "Westwood PSX palette"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions { get { return new string[] { "pal" }; } }
        public override int Width { get { return 16; } }
        public override int Height { get { return this.m_Height; } }
        public override bool NeedsPalette { get { return this.m_Palette == null; } }
        public override int BitsPerPixel { get { return 16; } }
        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return this.m_FramesList != null; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override bool HasCompositeFrame { get { return true; } }
        public override bool[] TransparencyMask { get { return new bool[0]; } }
        public override bool FramesHaveCommonPalette { get { return false; } }

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public override bool CanSave { get { return false; } }

        protected int m_Height;

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
            int len = fileData.Length;
            if (len == 0)
                throw new FileTypeLoadException("File is empty.");
            if (len % 32 != 0)
                throw new FileTypeLoadException("Incorrect file size: not a multiple of 32.");
            int palSize = len / 2;
            this.m_Height = palSize / 16;
            int nrOfPalettes = (this.m_Height + 15) / 16;
            //Format seems to be 16 bit LE ABGR.
            PixelFormatter pf = new PixelFormatter(2, 0x8000, 0x1F, 0x3E0, 0x7C00, true);
            Color[][] palettes = new Color[nrOfPalettes][];

            int curHeight = 0;
            for (int i = 0; i < nrOfPalettes; ++i)
            {
                int curPalRows = Math.Min(this.m_Height - curHeight, 16);
                int offset = i << 9; // = (* 256 * 16bit);
                // Check starting bytes. All PSX palettes start with a 00 00 color.
                for (int j = 0; j < curPalRows; ++j)
                    if (ArrayUtils.ReadUInt16FromByteArrayLe(fileData, offset + (j << 5)) != 0)
                        throw new FileTypeLoadException("Incorrect data: PSX palettes always start with a transparent value.");
                palettes[i] = pf.GetColorRange(fileData, offset, 16 * curPalRows);
                curHeight += curPalRows;
            }
            this.m_FramesList = new SupportedFileType[nrOfPalettes];
            int stride = 16;
            byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (byte) x).ToArray();

            int remainingHeight = this.m_Height;
            for (int i = 0; i < nrOfPalettes; ++i)
            {
                if (remainingHeight < 16)
                    imageData = imageData.Take(16 * remainingHeight).ToArray();
                Bitmap paletteImage = ImageUtils.BuildImage(imageData, 16, Math.Min(remainingHeight, 16), stride, PixelFormat.Format8bppIndexed, palettes[i], Color.Empty);
                if (remainingHeight < 16)
                    paletteImage.Palette = ImageUtils.GetPalette(palettes[i]);
                // Make frame
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, paletteImage, sourcePath, i);
                frame.SetBitsPerColor(8);
                frame.SetFileClass(this.FrameInputFileClass);
                frame.SetExtraInfo("");
                this.m_FramesList[i] = frame;
                remainingHeight -= 16;
            }
            byte[] fileData2 = ArrayUtils.CloneArray(fileData);
            PixelFormatter.ReorderBits(fileData2, 16, this.m_Height, 32, pf, PixelFormatter.Format16BitArgb1555Le);
            Bitmap fullImage = ImageUtils.BuildImage(fileData2, 16, this.m_Height, 32, PixelFormat.Format16bppArgb1555, null, null);
            this.m_LoadedImage = fullImage;
            this.ExtraInfo = "Contains " + nrOfPalettes + " color palette" + (nrOfPalettes != 1 ? "s" : String.Empty);
            this.m_Palette = null;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotImplementedException();
            /*/
            Color[] cols = this.CheckInputForColors(fileToSave, true);
            if (cols.Length % 256 != 0)
                throw new ArgumentException("PSX palettes must be 256 colors.", "fileToSave");
            Byte[] outBytes = new Byte[cols.Length * 2];
            PixelFormatter pf = FileImgWwCps.Format16BitRgbX444Be;
            for (Int32 i = 0; i < cols.Length; ++i)
                pf.WriteColor(outBytes, i << 1, cols[i]);
            return outBytes;
            //*/
        }
    }
}
