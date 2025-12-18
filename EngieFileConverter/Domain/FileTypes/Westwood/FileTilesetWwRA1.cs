using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileTilesetWwRA1: SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.FrameSet | FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.Image8Bit; } }

        public override String IdCode { get { return "WwTmpRa"; } }
        public override String[] FileExtensions { get { return new String[] { "icn", "tem", "int", "sno" }; } }
        public override String ShortTypeName { get { return "RA1 Tileset"; } }
        public override String LongTypeName { get { return "Westwood Tileset File - RA1"; } }

        public override Int32 BitsPerPixel { get { return 8; } }
        public override Boolean NeedsPalette { get { return true; } }

        protected SupportedFileType[] m_FramesList;

        /// <summary>Retrieves the sub-frames inside this file.</summary>
        public override SupportedFileType[] Frames { get { return this.m_FramesList; } }
        /// <summary>
        /// See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false will not get an index -1 in the frames list.
        /// C&amp;C tileset files are bit of an edge case, though, since they contains no overall dimensions. Files with known tile names as filename get their X and Y from the tile info.
        /// </summary>
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public override Boolean FramesHaveCommonPalette { get { return true; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }
        protected Boolean[] m_TileUseList;
        private byte[] m_typesInfo;

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        private void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            int[] widths;
            int[] heights;
            byte[] landTypesInfo;
            bool[] tileUseList;
            int tilesX;
            int tilesY;
            Byte[][] raTmpData = GetRaTmpData(fileData, out widths, out heights, out landTypesInfo, out tileUseList, out tilesX, out tilesY);
            string hdrSize = tilesX + "×" + tilesY;
            tilesX = Math.Max(1, tilesX);
            tilesY = Math.Max(1, tilesY);
            int numIcons = raTmpData.Length;
            int tileX = widths[0];
            int tileY = heights[0];
            int hdrSizeNum = tilesX * tilesY;
            byte[] typesInfo = new byte[hdrSizeNum];
            Array.Copy(landTypesInfo, 0, typesInfo, 0, Math.Min(landTypesInfo.Length, hdrSizeNum));
            this.m_typesInfo = typesInfo;
            this.m_TileUseList = tileUseList;
            m_Palette = PaletteUtils.GenerateGrayPalette(8, TransparencyMask, false);
            bool is1x1Multiple = tilesX * tilesY == 1 && numIcons > 1;
            int widthX = tilesX;
            if (is1x1Multiple)
            {
                Double sqrt = Math.Sqrt(numIcons);
                widthX = (sqrt - Math.Floor(sqrt)) < 0.0001 ? (int)sqrt : (int)(sqrt + 1);
            }
            m_FramesList = new SupportedFileType[numIcons];
            String landTypes = LandTypesToString(typesInfo, 0);
            for (Int32 i = 0; i < numIcons; ++i)
            {
                Bitmap frameImg = ImageUtils.BuildImage(raTmpData[i], tileX, tileY, tileX, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImg, sourcePath, i);
                byte landType = is1x1Multiple ? typesInfo[0] : landTypes.Length <= i ? (byte)0 : typesInfo[i];
                string landTypeDesc = LandTypeDescriptions.TryGetValue(landType, out landTypeDesc) ? landTypeDesc : LandTypeDescriptions[0];
                frame.ExtraInfo = "Land type: " + landTypeDesc + "\nEmpty: " + (tileUseList[i] ? "no" : "yes");
                m_FramesList[i] = frame;
            }
            this.m_LoadedImage = ImageUtils.Tile8BitImages(raTmpData, tileX, tileY, tileX, raTmpData.Length, this.m_Palette, widthX);
            StringBuilder extraInfo = new StringBuilder();
            extraInfo.Append("Size in header:").Append(hdrSize).Append('\n');
            extraInfo.Append("Land types: ").Append(landTypes).Append('\n');
            extraInfo.Append("Used tiles: ").Append(new String(tileUseList.Select(b => b ? '1' : '0').ToArray()));
            this.ExtraInfo = extraInfo.ToString();
        }

        public static Byte[][] GetRaTmpData(Byte[] fileData, out int[] widths, out int[] heights, out byte[] landTypesInfo, out Boolean[] tileUseList, out int headerWidth, out int headerHeight)
        {
            Int32 fileLen = fileData.Length;
            if (fileLen < 0x28)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            Int16 hdrWidth = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x00);
            Int16 hdrHeight = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x02);
            // Amount of icons to form the full icon set. Not necessarily the same as the amount of actual icons.
            Int16 hdrCount = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x04);
            // Always 0
            Int16 hdrAllocated = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x06);
            // New in RA
            headerWidth = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x08);
            headerHeight = ArrayUtils.ReadInt16FromByteArrayLe(fileData, 0x0A);
            Int32 hdrSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0C);
            // Offset of start of actual icon data. Generally always 0x20
            Int32 hdrIconsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x10);
            // Offset of start of palette data. Probably always 0.
            Int32 hdrPalettesPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x14);
            // Offset of remaps data. Dune II leftover of 4 bit to 8 bit translation tables.
            // Always seems to be 0x2C730FXX (with values differing for the lowest byte), which makes no sense as ptr.
            Int32 hdrRemapsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x18);
            // Offset of 'transparency flags'? Generally points to an empty array at the end of the file.
            Int32 hdrTransFlagPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x1C);
            // Offset of 'color' map, indicating the terrain type for each type. This includes unused cells, which are usually indicated as 0.
            Int32 hdrColorMapPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x20);
            // Offset of actual icon set definition, defining for each index which icon data to use. FF for none.
            Int32 hdrMapPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x24);
            // File size check
            if (hdrSize != fileData.Length)
                throw new FileTypeLoadException(ERR_BAD_HEADER_SIZE);
            // Only allowing standard 24x24 size
            if (hdrHeight != 24 || hdrWidth != 24)
                throw new FileTypeLoadException("Only 24×24 pixel tiles are supported.");
            // Checking some normally hardcoded values
            if (hdrAllocated != 00 || hdrPalettesPtr != 0)
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            if (hdrCount == 0)
                throw new FileTypeLoadException(ERR_NO_FRAMES);
            // Checking if data is all inside the file
            if (hdrIconsPtr >= fileLen || (hdrMapPtr + hdrCount) > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            Int32 tileSize = hdrWidth * hdrHeight;
            // Maps the available images onto the full iconset definition
            Byte[] map = new Byte[hdrCount];
            Array.Copy(fileData, hdrMapPtr, map, 0, hdrCount);
            landTypesInfo = new Byte[Math.Max(1, headerWidth) * Math.Max(1, headerHeight)];
            if (hdrMapPtr + landTypesInfo.Length > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            Array.Copy(fileData, hdrColorMapPtr, landTypesInfo, 0, landTypesInfo.Length);
            // Get max index plus one for real images count. Nothing in the file header actually specifies this directly.
            Int32 actualImages = map.Max(x => x == 0xff ? -1 : x) + 1;
            if (hdrTransFlagPtr + actualImages > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            if (hdrIconsPtr + actualImages * tileSize > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            Byte[] imagesIndex = new Byte[actualImages];
            Array.Copy(fileData, hdrTransFlagPtr, imagesIndex, 0, actualImages);
            Byte[][] tiles = new Byte[hdrCount][];
            widths = new int[hdrCount];
            heights = new int[hdrCount];
            tileUseList = new Boolean[map.Length];
            for (Int32 i = 0; i < map.Length; ++i)
            {
                Byte dataIndex = map[i];
                Boolean used = dataIndex != 0xFF;
                tileUseList[i] = used;
                Byte[] tileData = new Byte[tileSize];
                if (used)
                {
                    Int32 offset = hdrIconsPtr + dataIndex * tileSize;
                    if ((offset + tileSize) > fileLen)
                        throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
                    Array.Copy(fileData, offset, tileData, 0, tileSize);
                }
                tiles[i] = tileData;
                widths[i] = hdrWidth;
                heights[i] = hdrHeight;
            }
            return tiles;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotImplementedException();
        }

        private static readonly Dictionary<byte, char> LandTypeChars = new Dictionary<byte, char>
        {
            { 00, 'X' }, // Filler tile, or [Clear] terrain on 1x1 sets with multiple tiles.
            { 03, 'C' }, // [Clear] Normal clear terrain.
            { 06, 'B' }, // [Beach] Sandy beach. Can''t be built on.
            { 08, 'I' }, // [Rock]  Impassable terrain.
            { 09, 'R' }, // [Road]  Units move faster on this terrain.
            { 10, 'W' }, // [Water] Ships can travel over this.
            { 11, 'V' }, // [River] Ships normally can''t travel over this.
            { 14, 'H' }, // [Rough] Rough terrain. Can''t be built on
        };

        private static readonly Dictionary<byte, string> LandTypeDescriptions = new Dictionary<byte, string>
        {
            { 00, "Empty / Clear" },
            { 03, "Clear" },
            { 06, "Beach" },
            { 08, "Rock" },
            { 09, "Road" },
            { 10, "Water" },
            { 11, "River" },
            { 14, "Rough" },
        };
        private static readonly Dictionary<char, byte> LandTypesValues = LandTypeChars.ToDictionary(x => x.Value, x => x.Key);

        private static Byte[] LandTypesFromString(string types, int arrLen)
        {
            types = types.Replace("\r", String.Empty).Replace("\n", String.Empty).Replace(" ", String.Empty).Replace("\t", String.Empty);
            arrLen = Math.Min(arrLen, types.Length);
            Byte[] arr = new Byte[arrLen];
            Char[] input = types.ToUpperInvariant().ToCharArray();
            int inputLen = input.Length;
            for (Int32 i = 0; i < input.Length; ++i)
            {
                arr[i] = (byte)(i >= inputLen ? 0 : LandTypesValues.TryGetValue(input[i], out byte t) ? t : 0);
            }
            return arr;
        }

        private static string LandTypesToString(Byte[] types, int width)
        {
            bool hasWidth = width > 0;
            int len = types.Length;
            if (hasWidth)
            {
                len += len / width;
                if (len % width == 0)
                    len--;
            }
            Char[] output = new Char[len];
            if (!hasWidth)
            {
                for (Int32 i = 0; i < len; ++i)
                {
                    output[i] = LandTypeChars.TryGetValue(types[i], out char t) ? t : 'X';
                }
            }
            else
            {
                int actualWidth = width + 1;
                int index = 0;
                for (Int32 i = 0; i < len; ++i)
                {
                    if ((i + 1) % actualWidth != 0)
                    {
                        output[i] = LandTypeChars.TryGetValue(types[index], out char t) ? t : 'X';
                        index++;
                    }
                    else
                    {
                        output[i] = '\n';
                    }
                }
            }
            return new string(output);
        }
    }
}
