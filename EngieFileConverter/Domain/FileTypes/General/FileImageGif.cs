using System;
using System.Drawing.Imaging;
using System.Text;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImageGif : FileImage
    {
        public override FileClass InputFileClass { get { return FileClass.ImageIndexed; } }
        public override String ShortTypeName { get { return "GIF"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String LongTypeName { get { return "CompuServe GIF image"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "gif" }; } }
        protected override String MimeType { get { return "gif"; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] {this.LongTypeName }; } }



        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData);
            this.SetFileNames(filename);
        }

        public void LoadFromFileData(Byte[] fileData)
        {
            // Quick header identifying check
            Int32 dataLen = fileData.Length;
            
            // == Header ==
            if (dataLen < 0x0D)
                throw new FileTypeLoadException(ERR_NO_HEADER);
            if (fileData[0] != 'G' || fileData[1] != 'I' || fileData[3] != 'F' || fileData[4] != '8' || (fileData[5] != '7' && fileData[5] != '9') || fileData[6] != 'a')
                throw new FileTypeLoadException(ERR_BAD_HEADER);
            Boolean isVer87 = fileData[5] == '7';
            String version = isVer87 ? "87a" : "89a";

            // == Logical Screen Descriptor ==
            Int32 imageWidth = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 6);
            Int32 imageHeight = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 8);
            Byte colInfo = fileData[0x0A];
            Boolean hasPalette = (colInfo & 0x80) != 0;
            // Might indicate the original image came from a 6-bit color palette. OTherwise, useless.
            //Int32 colBpp = ((colInfo >> 8) & 7) + 1;
            // Indicates that the palette is sorted in order of importance. Seems useless; leave off when writing I guess.
            //Boolean sort = (colInfo & 8) != 0;
            // Defines both bit depth and palette length. Maximum is 8.
            Int32 palBpp = (colInfo & 7) + 1;
            // Background color. Fills full frame if the image data does not. (This is NOT the transparent color index)
            Byte bgcol = fileData[0x0B];
            // Pixel Aspect Ratio, as W/H, stored as [(PAR + 15) / 64 = W/H], so [W/H * 64 - 15 = PAR]. The value for 1:1 would technically be 49, but storing 0 will also default to that.
            // Specs define the range as 1:4 to 4:1, which would only be the range of 1 to 241. Value for 255 would be [270 / 64 = 4.21875].
            Int32 pixAspectRatio = fileData[0x0C];
            Int32 pixAspectX;
            Int32 pixAspectY;
            if (pixAspectRatio == 0)
            {
                pixAspectX = 1;
                pixAspectY = 1;
            }
            else if (pixAspectRatio == 38)
            {
                // 320x200 CRT pixels would be 5:6, which is 0.833333. The closest value for that is 38: [5/6 * 64 + 15 = 38.333...] and [(38 + 15) / 64 = 0.828125]
                // Maybe I should make this into a big switch-statement with more common ones?
                pixAspectX = 5;
                pixAspectY = 6;
            }
            else
            {
                pixAspectX = pixAspectRatio + 15;
                pixAspectY = 64;
                Int32 pixAspectDiv = GeneralUtils.HighestCommonDenominator(pixAspectX, pixAspectY);
                pixAspectX /= pixAspectDiv;
                pixAspectY /= pixAspectDiv;
            }
            // Double pixelAspectRatio = pixAspectRatio == 0 ? 0.0 : (pixAspectRatio + 15) / 64.0;
            String pixAspect = "Pixel aspect ratio: " + pixAspectX + ":" + pixAspectY;

            Int32 readIndex = 0x0D;
            
            // == Palette ==
            Int32 palLength = hasPalette ? 0 : (Int32)Math.Pow(2, palBpp);
            if (palLength > 0)
            {
                Int32 palReadLength = palLength * 3;
                if (dataLen < readIndex + palReadLength)
                    throw new FileTypeLoadException(ERR_NO_HEADER);
                ColorUtils.ReadEightBitPalette(fileData, readIndex, palReadLength);
                // Might be actual palette length? Not sure; find better specs than wikipedia.
                readIndex += palReadLength;
            }



            //base.LoadFile(fileData);
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
            return m_Palette == null ? new Color[0] : ArrayUtils.CloneArray(m_Palette);
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

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            return ImageUtils.GetSavedImageData(fileToSave.GetBitmap(), ImageFormat.Gif);
        }
    }
}
