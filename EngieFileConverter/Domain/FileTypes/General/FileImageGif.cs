using System;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageGif : FileImage
    {
        public override FileClass InputFileClass { get { return FileClass.ImageIndexed; } }
        public override String ShortTypeName { get { return "GIF"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "CompuServe GIF image"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "gif" }; } }
        protected override String MimeType { get { return "gif"; } }

        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions
        {
            get { return new String[] {this.ShortTypeDescription }; }
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException(FILE_EMPTY);
            return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ImageFormat.Gif);
        }

        /*/
        // animated gif frames splitting code. Doesn't work because the .net framework insists animated gif is 32bpp ARGB. All my WTF.

        public override SupportedFileType[] Frames { get { return m_TilesList == null ? null : m_TilesList.Cast<SupportedFileType>().ToArray(); } }

        /// <summary>See this as a data type containing frames.</summary>
        public override Boolean ContainsFrames { get { return m_isAnimatedGif; } }
        protected Boolean m_isAnimatedGif = false;
        protected Byte[][] m_rawframes;
        protected FileImageGif[] m_TilesList;
        protected Color[] m_Palette;

        public override Color[] GetColors()
        {
            return m_Palette == null ? new Color[0] : m_Palette.ToArray();
        }

        public FileImageGif() {}
        protected FileImageGif(Bitmap image, String filename)
        {
            this.m_LoadedImage = image;
            this.m_isAnimatedGif = false;
            this.LoadedFileName = filename;
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadedFileName = Path.GetFileName(filename);
            String partName = Path.GetFileNameWithoutExtension(filename);
            String ext = Path.GetExtension(filename);
            using (MemoryStream ms = new MemoryStream(fileData))
            using (Bitmap loadedImage = new Bitmap(ms))
            {
                m_Palette = loadedImage.Palette.Entries;
                Int32 length = loadedImage.GetFrameCount(FrameDimension.Time);
                if (length > 1)
                {
                    Bitmap[] tilesList = ImageUtils.GetFramesFromAnimatedGIF(loadedImage);
                    this.m_isAnimatedGif = true;
                    Int32 tempStride;
                    this.m_rawframes = new Byte[tilesList.Length][];
                    this.m_TilesList = new FileImageGif[tilesList.Length];
                    for (Int32 i = 0; i < m_rawframes.Length; ++i)
                    {
                        using (Bitmap frame = new Bitmap(tilesList[i]))
                        {
                            m_Palette = frame.Palette.Entries;
                            this.m_rawframes[i] = ImageUtils.ConvertTo8Bit(ImageUtils.GetImageData(frame, out tempStride), frame.Width, frame.Height, 0, 8, true, ref tempStride);
                            this.m_TilesList[i] = new FileImageGif(tilesList[i], partName + "_" + i + ext);
                        }
                    }
                    BuildFullImage();
                }
                else
                {
                    m_LoadedImage = ImageUtils.CloneImage(loadedImage);
                    this.m_isAnimatedGif = false;
                }
                m_ColsInPal = loadedImage.Palette.Entries.Length;
            }
        }

        protected override void BuildFullImage()
        {
            if (m_TilesList.Length == 0)
                return;
            Int32 nrOftiles = this.m_rawframes.Length;
            Int32 width = m_TilesList[0].Width;
            Int32 height = m_TilesList[0].Height;
            this.m_LoadedImage = ImageUtils.Tile8BitImages(this.m_rawframes, width, height, width, nrOftiles, m_Palette, this.m_CompositeFrameTilesWidth);
        }
        //*/
    }
}
