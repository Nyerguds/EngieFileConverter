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
        public override Boolean IsFramesContainer { get { return true; } }
        /// <summary> This is a container-type that builds a full image from its frames to show on the UI, which means this type can be used as single-image source.</summary>
        public override Boolean HasCompositeFrame { get { return true; } }
        /// <summary>True if all frames in this frames container have a common palette. Defaults to True if the type is a frames container.</summary>
        public override Boolean FramesHaveCommonPalette { get { return true; } }

        /// <summary>Array of Booleans which defines for the palette which indices are transparent.</summary>
        public override Boolean[] TransparencyMask { get { return new Boolean[] { true }; } }
        protected Boolean[] m_TileUseList;
        private byte[] m_typesInfo;
        private int m_tilesWidth;
        private bool m_is1x1Multiple;

        public override void LoadFile(byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
            this.SetFileNames(filename);
        }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        private void LoadFromFileData(byte[] fileData, String sourcePath)
        {
            int[] widths;
            int[] heights;
            byte[] landTypesInfo;
            bool[] tileUseList;
            int tilesX;
            int tilesY;
            byte[][] raTmpData = GetRaTmpData(fileData, out widths, out heights, out landTypesInfo, out tileUseList, out tilesX, out tilesY);
            string hdrSize = tilesX + "×" + tilesY;
            tilesX = Math.Max(1, tilesX);
            m_tilesWidth = tilesX;
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
            m_is1x1Multiple = tilesX * tilesY == 1 && numIcons > 1;
            int widthX = tilesX;
            if (m_is1x1Multiple)
            {
                Double sqrt = Math.Sqrt(numIcons);
                widthX = (sqrt - Math.Floor(sqrt)) < 0.0001 ? (int)sqrt : (int)(sqrt + 1);
            }
            m_FramesList = new SupportedFileType[numIcons];
            String landTypes = LandTypesToString(typesInfo, 0);
            for (int i = 0; i < numIcons; ++i)
            {
                Bitmap frameImg = ImageUtils.BuildImage(raTmpData[i], tileX, tileY, tileX, PixelFormat.Format8bppIndexed, this.m_Palette, null);
                FileImageFrame frame = new FileImageFrame();
                frame.LoadFileFrame(this, this, frameImg, sourcePath, i);
                byte landType = m_is1x1Multiple ? typesInfo[0] : landTypes.Length <= i ? (byte)0 : typesInfo[i];
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

        public static byte[][] GetRaTmpData(byte[] fileData, out int[] widths, out int[] heights, out byte[] landTypesInfo, out Boolean[] tileUseList, out int headerWidth, out int headerHeight)
        {
            int fileLen = fileData.Length;
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
            int hdrSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x0C);
            // Offset of start of actual icon data. Generally always 0x20
            int hdrIconsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x10);
            // Offset of start of palette data. Probably always 0.
            int hdrPalettesPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x14);
            // Offset of remaps data. Dune II leftover of 4 bit to 8 bit translation tables.
            // Always seems to be 0x2C730FXX (with values differing for the lowest byte), which makes no sense as ptr.
            int hdrRemapsPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x18);
            // Offset of 'transparency flags'? Generally points to an empty array at the end of the file.
            int hdrTransFlagPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x1C);
            // Offset of 'color' map, indicating the terrain type for each type. This includes unused cells, which are usually indicated as 0.
            int hdrColorMapPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x20);
            // Offset of actual icon set definition, defining for each index which icon data to use. FF for none.
            int hdrMapPtr = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0x24);
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
            int tileSize = hdrWidth * hdrHeight;
            // Maps the available images onto the full iconset definition
            byte[] map = new byte[hdrCount];
            Array.Copy(fileData, hdrMapPtr, map, 0, hdrCount);
            landTypesInfo = new byte[Math.Max(1, headerWidth) * Math.Max(1, headerHeight)];
            if (hdrMapPtr + landTypesInfo.Length > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            Array.Copy(fileData, hdrColorMapPtr, landTypesInfo, 0, landTypesInfo.Length);
            // Get max index plus one for real images count. Nothing in the file header actually specifies this directly.
            int actualImages = map.Max(x => x == 0xff ? -1 : x) + 1;
            if (hdrTransFlagPtr + actualImages > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL);
            if (hdrIconsPtr + actualImages * tileSize > fileLen)
                throw new FileTypeLoadException(ERR_SIZE_TOO_SMALL_IMAGE);
            byte[] imagesIndex = new byte[actualImages];
            Array.Copy(fileData, hdrTransFlagPtr, imagesIndex, 0, actualImages);
            byte[][] tiles = new byte[hdrCount][];
            widths = new int[hdrCount];
            heights = new int[hdrCount];
            tileUseList = new bool[map.Length];
            for (int i = 0; i < map.Length; ++i)
            {
                byte dataIndex = map[i];
                bool used = dataIndex != 0xFF;
                tileUseList[i] = used;
                byte[] tileData = new byte[tileSize];
                if (used)
                {
                    int offset = hdrIconsPtr + dataIndex * tileSize;
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

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            PerformPreliminaryChecks(fileToSave, out int tiles, out int tileWidth, out bool hasFixedWidth, out byte[] landTypesArr);
            bool is1x1Multiple = tileWidth == -1;
            tileWidth = Math.Max(1, tileWidth);
            string landTypes = LandTypesToString(landTypesArr, tileWidth);
            string landTypesCheck = LandTypesToString(landTypesArr, 0).TrimEnd('X');
            // Only allow 1x1 if there are no empty tile gaps in the data.
            bool allow1x1 = is1x1Multiple || (landTypesCheck.Length > 0 && !landTypesCheck.Contains('X'));

            int nrOfOps = 2;
            if (allow1x1) nrOfOps++;
            Option[] opts = new Option[nrOfOps];
            int optind = 0;
            if (allow1x1)
            {
                opts[optind++] = new Option("1x1", OptionInputType.Boolean, "Save as 1x1 with multiple frames (only the first land type is used)", is1x1Multiple ? "1" : "0");
            }
            opts[optind++] = new Option("LND", OptionInputType.String, "Land types for all cells.\n" +
                "X: Unused, C: Clear, B: Beach, I: Rock\nR: Road, W: Water, V: River, H: Rough", "XxCcBbIiRrWwVvHh\r\n", landTypes);
            // Only asked if it's not a multi-tile image or a tileset file.
            opts[optind++] = new Option("WDT", OptionInputType.Number, "Width in tiles", "1," + tiles, tileWidth.ToString(),
                new EnableFilter("1x1", false, "1"),
                new EnableFilter("LND", hasFixedWidth, "1"));
            return opts;
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            byte[][] tilesData = PerformPreliminaryChecks(fileToSave, out int nrOfTiles, out int tilesWidth, out _, out _);
            string wOption = Option.GetSaveOptionValue(saveOptions, "WDT");
            if (wOption != null)
            {
                // If given, override.
                Int32.TryParse(wOption, out tilesWidth);
            }
            string landTypes = Option.GetSaveOptionValue(saveOptions, "LND");
            bool is1x1multiple = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "1x1"));
            // DATA GATHERED. Build icons map, remove duplicates.
            int saveNrOfTiles = nrOfTiles;
            int tilesHeight = is1x1multiple ? 1 : nrOfTiles / tilesWidth;
            if (!is1x1multiple)
            {
                if (nrOfTiles % tilesWidth != 0)
                {
                    tilesHeight = (nrOfTiles + tilesWidth - 1) / tilesWidth;
                    saveNrOfTiles = tilesWidth * tilesHeight;
                }
            }
            else
            {
                tilesWidth = 1;
            }
            byte[][] tempTiles = new byte[saveNrOfTiles][];
            byte[] finalIndices = new byte[saveNrOfTiles];
            int actualTiles = 0;
            for (int index = 0; index < saveNrOfTiles; ++index)
            {
                if (index >= nrOfTiles)
                {
                    finalIndices[index] = 0xFF;
                    continue;
                }
                byte[] tileData = tilesData[index];
                if (tileData == null)
                {
                    finalIndices[index] = 0xFF;
                    continue;
                }
                int foundIndex = -1;
                for (int i = 0; i < actualTiles; ++i)
                {
                    if (ArrayUtils.ArraysAreEqual(tempTiles[i], tileData))
                    {
                        foundIndex = i;
                        break;
                    }
                }
                if (foundIndex != -1)
                {
                    finalIndices[index] = (byte)foundIndex;
                }
                else
                {
                    finalIndices[index] = (byte)actualTiles;
                    tempTiles[actualTiles] = tileData;
                    actualTiles++;
                }
            }
            byte[] landsForIcons = LandTypesFromString(landTypes, tilesWidth * tilesHeight);
            if (is1x1multiple)
            {
                if (landsForIcons[0] == 3) landsForIcons[0] = 0;
            }
            else
            {
                for (int i = 0; i < saveNrOfTiles; ++i)
                {
                    if (finalIndices[i] == 0xFF)
                    {
                        landsForIcons[i] = 0;
                    }
                    else if (landsForIcons[i] == 0)
                    {
                        landsForIcons[i] = 3;
                    }
                }
            }
            // Order: (Header) , (data) ,  (actual frames index), (all tiles index) , (hdrColorMapPtr)
            int tileLength = 24 * 24;
            int size = 0x28;
            int hdrIconsPtr = size;
            size += actualTiles * tileLength;
            int hdrMapPtr = size;
            size += saveNrOfTiles;

            int hdrTransFlagPtr = size;
            size += actualTiles;

            int hdrColorMapPtr = size;
            size += landsForIcons.Length;
            byte[] finalData = new byte[size];

            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x00, 24); // Width
            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x02, 24); // Height
            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x04, (ushort)saveNrOfTiles);
            // ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x06, 0); // hdrCount
            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x08, (ushort)tilesWidth);
            ArrayUtils.WriteUInt16ToByteArrayLe(finalData, 0x0A, (ushort)tilesHeight);

            ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x0C, (ushort)size);
            ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x10, (ushort)hdrIconsPtr);
            // ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x014, 0); // indexPalette
            // Signature ;)
            ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x18, 0x49474E45);
            ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x1C, (ushort)hdrTransFlagPtr);
            ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x20, (ushort)hdrColorMapPtr);
            ArrayUtils.WriteUInt32ToByteArrayLe(finalData, 0x24, (ushort)hdrMapPtr);

            for (int i = 0; i < actualTiles; ++i)
                Array.Copy(tempTiles[i], 0, finalData, hdrIconsPtr + tileLength * i, tileLength);
            Array.Copy(finalIndices, 0, finalData, hdrMapPtr, finalIndices.Length);
            // Not done: write data to offset indexImages. Because, no one really knows what it does.
            Array.Copy(landsForIcons, 0, finalData, hdrColorMapPtr, landsForIcons.Length);
            return finalData;
        }

        private byte[][] PerformPreliminaryChecks(SupportedFileType fileToSave, out int nrOfTiles, out int tileWidth, out bool hasWidth, out byte[] landTypesArr)
        {
            nrOfTiles = 0;
            tileWidth = 0;
            hasWidth = false;
            landTypesArr = null;
            FileTilesetWwRA1 tilesetRa = fileToSave as FileTilesetWwRA1;
            if (tilesetRa != null)
            {
                nrOfTiles = tilesetRa.Frames.Length;
                tileWidth = tilesetRa.m_tilesWidth;
                if (tilesetRa.m_is1x1Multiple && tilesetRa.m_tilesWidth == 1)
                {
                    tileWidth = -1;
                }
                hasWidth = true;
                landTypesArr = tilesetRa.m_typesInfo;
            }
            Byte[][] framesData;
            if (!fileToSave.IsFramesContainer)
            {
                if (fileToSave.BitsPerPixel != 8)
                    throw new ArgumentException("Can only save 8 BPP images as this type.", "fileToSave");
                Bitmap bitmap = fileToSave.GetBitmap();
                if (bitmap == null || bitmap.Width % 24 != 0 || bitmap.Height % 24 != 0)
                    throw new ArgumentException("The file dimensions are not a multiple of 24×24.", "fileToSave");
                Int32 nrOfFramesX = bitmap.Width / 24;
                Int32 nrOfFramesY = bitmap.Height / 24;
                if (tilesetRa == null)
                {
                    nrOfTiles = nrOfFramesX * nrOfFramesY;
                    hasWidth = true;
                    tileWidth = nrOfFramesX;
                }
                if (landTypesArr == null)
                {
                    landTypesArr = Enumerable.Repeat(03, nrOfTiles).Select(b => (byte)b).ToArray();
                }
                framesData = new Byte[nrOfTiles][];
                if (nrOfTiles > 255)
                    throw new ArgumentException("Too many tiles in file.", "fileToSave");
                Int32 stride;
                Byte[] fullImageData = ImageUtils.GetImageData(bitmap, out stride);
                for (Int32 y = 0; y < nrOfFramesY; ++y)
                {
                    for (Int32 x = 0; x < nrOfFramesX; ++x)
                    {
                        Int32 index = y * nrOfFramesX + x;
                        byte[] frameData = ImageUtils.CopyFrom8bpp(fullImageData, bitmap.Width, bitmap.Height, stride, new Rectangle(x * 24, y * 24, 24, 24));
                        if (ArrayUtils.IsEmpty(frameData))
                        {
                            landTypesArr[index] = 0;
                            frameData = null;
                        }
                        framesData[index] = frameData;
                    }
                }
            }
            else
            {
                SupportedFileType[] frames = fileToSave.Frames;
                nrOfTiles = frames.Length;
                if (nrOfTiles > 255)
                    throw new ArgumentException("Too many tiles in file.", "fileToSave");
                framesData = new Byte[nrOfTiles][];
                if (landTypesArr == null)
                {
                    landTypesArr = Enumerable.Repeat(03, nrOfTiles).Select(b => (byte)b).ToArray();
                }
                for (Int32 i = 0; i < nrOfTiles; ++i)
                {
                    Bitmap bitmap;
                    SupportedFileType frame = frames[i];
                    if (frame == null || (bitmap = frame.GetBitmap()) == null)
                        continue;
                    if (frame.BitsPerPixel != 8)
                        throw new ArgumentException("Can only save 8 BPP images as this type.", "fileToSave");
                    if (bitmap.Width != 24 || bitmap.Height != 24)
                        throw new ArgumentException("All frames must be 24×24.", "fileToSave");
                    byte[] frameData = ImageUtils.GetImageData(bitmap, true);
                    if (ArrayUtils.IsEmpty(frameData))
                    {
                        landTypesArr[i] = 0;
                        frameData = null;
                    }
                    framesData[i] = frameData;
                }
                if (tilesetRa == null)
                {
                    Bitmap cmp = fileToSave.GetBitmap();
                    // Check if composite frame gives a full image with a viable width to use.
                    if (fileToSave.HasCompositeFrame && cmp != null && cmp.Width % 24 == 0 && cmp.Height % 24 == 0 && cmp.Width / 24 * cmp.Height / 24 == nrOfTiles)
                    {
                        hasWidth = true;
                        tileWidth = cmp.Width / 24;
                    }
                    else
                    {
                        double sqrt = Math.Sqrt(nrOfTiles);
                        tileWidth = (sqrt - Math.Floor(sqrt)) < 0.0001 ? (int)sqrt : (int)(sqrt + 1);
                        int attemptUp = tileWidth;
                        int attemptDn = tileWidth;
                        while (nrOfTiles % attemptUp != 0)
                        {
                            attemptUp++;
                        }
                        while (nrOfTiles % attemptDn != 0)
                        {
                            attemptDn--;
                        }
                        // Get closest value, preferring upwards.
                        if (attemptDn != 1 && attemptUp != nrOfTiles)
                        {
                            if (tileWidth - attemptDn < attemptUp - tileWidth)
                            {
                                tileWidth = attemptDn;
                            }
                            else
                            {
                                tileWidth = attemptUp;
                            }
                        }
                        else if (attemptUp != nrOfTiles)
                        {
                            tileWidth = attemptUp;
                        }
                        else if (attemptDn != 1)
                        {
                            tileWidth = attemptDn;
                        }
                    }
                }
            }
            return framesData;
        }

        private static readonly Dictionary<byte, char> LandTypeChars = new Dictionary<byte, char>
        {
            { 00, 'X' }, // Filler tile, or [Clear] terrain on 1x1 sets with multiple tiles.
            { 03, 'C' }, // [Clear] Normal clear terrain.
            { 06, 'B' }, // [Beach] Sandy beach. Can't be built on.
            { 08, 'I' }, // [Rock]  Impassable terrain.
            { 09, 'R' }, // [Road]  Units move faster on this terrain.
            { 10, 'W' }, // [Water] Ships can travel over this.
            { 11, 'V' }, // [River] Ships normally can't travel over this.
            { 14, 'H' }, // [Rough] Rough terrain. Can't be built on
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

        private static byte[] LandTypesFromString(string types, int arrLen)
        {
            types = types.Replace("\r", String.Empty).Replace("\n", String.Empty).Replace(" ", String.Empty).Replace("\t", String.Empty);
            byte[] arr = new byte[arrLen];
            char[] input = types.ToUpperInvariant().ToCharArray();
            int inputLen = input.Length;
            for (int i = 0; i < arrLen; ++i)
            {
                arr[i] = (byte)(i >= inputLen ? 0 : LandTypesValues.TryGetValue(input[i], out byte t) ? t : 0);
            }
            return arr;
        }

        private static string LandTypesToString(byte[] types, int width)
        {
            bool hasWidth = width > 0;
            int reallen = types.Length;
            int len = reallen;
            char[] output;
            if (!hasWidth)
            {
                output = new Char[len];
                for (int i = 0; i < len; ++i)
                {
                    output[i] = LandTypeChars.TryGetValue(types[i], out char t) ? t : 'X';
                }
            }
            else
            {
                int height = (len + width - 1) / width;
                // Add a spot for the line break
                int actualWidth = width + 1;
                // Full length minus the final line break
                len = actualWidth * height - 1;
                output = new Char[len];
                int index = 0;
                for (int i = 0; i < len; ++i)
                {
                    if ((i + 1) % actualWidth != 0)
                    {
                        byte val = index >= reallen ? (byte)0 : types[index];
                        output[i] = LandTypeChars.TryGetValue(val, out char t) ? t : 'X';
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
