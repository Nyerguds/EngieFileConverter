using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using EngieFileConverter.Domain.FileTypes;

namespace EngieFileConverter.Domain.HeightMap
{
    public class HeightMapGenerator
    {
        private const Int32 BASE_HEIGHT = 0x40;
        private const Int32 ROCKS_HEIGHT_DIFF = 0x20;
        private const Int32 BEACH_HEIGHT_DIFF = -0x10;
        private const Int32 WATER_HEIGHT_DIFF = -0x20;
        private static readonly Color[] LEVEL_COLORS = Enumerable.Range(0, 5).Select(x => Color.FromArgb(Math.Min(0xFF, BASE_HEIGHT*x), Math.Min(0xFF, BASE_HEIGHT*x), Math.Min(0xFF, BASE_HEIGHT*x))).ToArray();
        private static readonly Rectangle FULL_MAP = new Rectangle(0, 0, 64, 64);

        public static FileImagePng GeneratePlateauImage64x64(FileMapWwCc1Pc mapFile, String suffix)
        {
            return GenerateHeightMapImage(mapFile, null, suffix);
        }

        public static FileImagePng GenerateHeightMapImage64x64(FileMapWwCc1Pc mapFile, Bitmap plateauLevelsImage, String suffix)
        {
            return GenerateHeightMapImage(mapFile, plateauLevelsImage, suffix);
        }

        private static FileImagePng GenerateHeightMapImage(FileMapWwCc1Pc map, Bitmap plateauLevelsImage, String returnNameSuffix)
        {
            // if a plateau levels image is given, this whole function switches to generating a more detailed image based on both the map and the plateaus.
            Boolean generatebasicCliffs = plateauLevelsImage == null;
            String loadedPath = map.LoadedFile;
            String baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            String iniFileName = baseFileName + ".ini";
            String pngFileName = baseFileName + (returnNameSuffix ?? String.Empty) + ".png";
            IniFile mapInfo = !File.Exists(iniFileName) ? null : new IniFile(iniFileName, IniFile.ENCODING_DOS_US);

            TerrainTypeEnh[] simpleMap = MapConversion.SimplifyMap(map.Map, MapConversion.TILEINFO_TD);

            Int32 mapStartX = 1;
            Int32 mapStartY = 1;
            // ends are exclusive
            Int32 mapEndX = 63;
            Int32 mapEndY = 63;
            if (mapInfo != null)
            {
                mapStartX = mapInfo.GetIntValue("Map", "X", 1);
                mapStartY = mapInfo.GetIntValue("Map", "Y", 1);
                mapEndX = mapStartX + mapInfo.GetIntValue("Map", "Width", 62);
                mapEndY = mapStartY + mapInfo.GetIntValue("Map", "Height", 62);
            }
            mapStartX = Math.Min(63, Math.Max(1, mapStartX));
            mapStartY = Math.Min(63, Math.Max(1, mapStartY));
            mapEndX = Math.Min(63, Math.Max(1, mapEndX));
            mapEndY = Math.Min(63, Math.Max(1, mapEndY));
            Rectangle area = new Rectangle(mapStartX, mapStartY, mapEndX - mapStartX, mapEndY - mapStartY);

            ExpandOutsideBorders(simpleMap, 64, area);

            Byte[] eightBitPlateauData;
            if (!generatebasicCliffs)
            {
                if (plateauLevelsImage.Width != 64 || plateauLevelsImage.Height != 64)
                    throw new ArgumentException("Plateau levels image needs to be 64x64!", "plateauLevelsImage");
                Int32 plateauStride;
                Byte[] plateauData = ImageUtils.GetImageData(ImageUtils.PaintOn32bpp(plateauLevelsImage, Color.Black), out plateauStride);
                eightBitPlateauData = ImageUtils.Convert32BitToPaletted(plateauData, plateauLevelsImage.Width, plateauLevelsImage.Height, 8, false, LEVEL_COLORS, ref plateauStride);
                EnhancePlateaus(eightBitPlateauData, simpleMap, FULL_MAP);
            }
            else
            {
                eightBitPlateauData = null;
            }
            Int32 mapSize = 64*64;
            Byte[] heightMapData = new Byte[mapSize];
            for (Int32 i = 0; i < mapSize; ++i)
            {
                Int32 val = BASE_HEIGHT;
                if (eightBitPlateauData != null)
                    val = val/2*eightBitPlateauData[i];
                TerrainTypeEnh cur = simpleMap[i];
                switch (cur)
                {
                    case TerrainTypeEnh.Clear:
                    case TerrainTypeEnh.Road:
                    case TerrainTypeEnh.Smudge:
                    case TerrainTypeEnh.Snow:
                        break;
                    case TerrainTypeEnh.Water:
                        if (!generatebasicCliffs)
                            val += WATER_HEIGHT_DIFF;
                        break;
                    case TerrainTypeEnh.Rock:
                        if (!generatebasicCliffs)
                            val += ROCKS_HEIGHT_DIFF;
                        break;
                    case TerrainTypeEnh.Beach:
                        if (!generatebasicCliffs)
                            val += BEACH_HEIGHT_DIFF;
                        break;
                    case TerrainTypeEnh.CliffFace:
                        if (generatebasicCliffs)
                            val += BASE_HEIGHT;
                        break;
                    case TerrainTypeEnh.CliffPlateau:
                        if (generatebasicCliffs || eightBitPlateauData == null)
                            val += BASE_HEIGHT;
                        break;
                }
                heightMapData[i] = (Byte) Math.Min(Math.Max(0, val), 255);
            }
            Color[] palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            if (generatebasicCliffs)
            {
                heightMapData = ImageUtils.Match8BitDataToPalette(heightMapData, palette, LEVEL_COLORS);
                palette = LEVEL_COLORS;
            }
            Bitmap bm = ImageUtils.BuildImage(heightMapData, 64, 64, 64, PixelFormat.Format8bppIndexed, palette, Color.Empty);
            // Force palette length
            bm.Palette = ImageUtils.GetPalette(palette);
            FileImagePng returnImg = new FileImagePng();
            returnImg.LoadFile(bm, pngFileName);
            return returnImg;
        }

        private static void EnhancePlateaus(Byte[] eightBitPlateauData, TerrainTypeEnh[] simpleMap, Rectangle area)
        {
            Byte[] eightBitPlateauCopy = new Byte[eightBitPlateauData.Length];
            Array.Copy(eightBitPlateauData, 0, eightBitPlateauCopy, 0, eightBitPlateauData.Length);
            // Fill all cliff faces with the lower ground
            for (Int32 y = area.Y; y < area.Bottom; ++y)
            {
                for (Int32 x = area.X; x < area.Right; ++x)
                {
                    Int32 offset = y*64 + x;
                    // Lower cliff faces to the nearby ground level, so the transition from cliff plateau to ground becomes a steep cliff in the game.
                    if (simpleMap[offset] == TerrainTypeEnh.CliffFace)
                    {
                        Int32 newHeight = FindNearbyLowerGround(x, y, eightBitPlateauCopy);
                        eightBitPlateauData[offset] = (Byte) Math.Max(0, newHeight);
                    }
                }
            }
            // This code creates a slope where a terrain difference occurs.
            Boolean[] reduce = new Boolean[eightBitPlateauData.Length];
            for (Int32 y = area.Y; y < area.Bottom; ++y)
            {
                for (Int32 x = area.X; x < area.Right; ++x)
                {
                    Int32 height = eightBitPlateauData[y*64 + x];
                    Byte[] heightsAround = GetNeighbouringData(eightBitPlateauData, 64, x, y, area, (Byte) 0xFF, false);
                    TerrainTypeEnh type = simpleMap[y*64 + x];
                    TerrainTypeEnh[] typesAround = GetNeighbouringData(simpleMap, 64, x, y, area, TerrainTypeEnh.Unused, false);
                    // 0 1 2
                    // 3   4
                    // 5 6 7
                    Boolean reduceThis = false;
                    if (type == TerrainTypeEnh.Clear || type == TerrainTypeEnh.Road || type == TerrainTypeEnh.Snow || type == TerrainTypeEnh.Smudge)
                    {
                        Int32 nrOfTypesAround = typesAround.Length;
                        for (Int32 i = 0; i < nrOfTypesAround; ++i)
                        {
                            if ((typesAround[i] == TerrainTypeEnh.Clear || typesAround[i] == TerrainTypeEnh.Road || typesAround[i] == TerrainTypeEnh.Snow || typesAround[i] == TerrainTypeEnh.Smudge) && heightsAround[i] < height)
                            {
                                reduceThis = true;
                                break;
                            }
                        }
                    }
                    reduce[y*64 + x] = reduceThis;
                }
            }
            for (Int32 y = 0; y < 64; ++y)
            {
                for (Int32 x = 0; x < 64; ++x)
                {
                    Int32 val2 = eightBitPlateauData[y*64 + x]*2;
                    if (reduce[y*64 + x])
                        val2 -= 1;
                    eightBitPlateauData[y*64 + x] = (Byte) Math.Max(0, val2);
                }
            }
        }

        private static Byte FindNearbyLowerGround(Int32 x, Int32 y, Byte[] eightBitPlateauData)
        {
            Int32 offset = y*64 + x;
            Byte currentLevel = eightBitPlateauData[offset];
            Byte[] heightsAround = GetNeighbouringData<Byte>(eightBitPlateauData, 64, x, y, FULL_MAP, 0xFF, true);
            // Priority to directly adjacent cells.
            for (int i = 1; i < 9; i += 2)
            {
                Byte newLevel;
                if ((newLevel = heightsAround[i]) != 0xFF && newLevel < currentLevel)
                    return newLevel;
            }
            // If those fail, check corners.
            // This will technically check the center too (since it checks 0,2,[4],6,8) but that will fail anyway,
            // since it gets filled in with the actual cell, whose type is always CliffFace.
            for (int i = 0; i < 9; i += 2)
            {
                Byte newLevel;
                if ((newLevel = heightsAround[i]) != 0xFF && newLevel < currentLevel)
                    return newLevel;
            }
            // Not found: possibly an impassably narrow valley between two mountain ranges. Default to old behaviour
            return (Byte)Math.Max(0, currentLevel - 1);
        }

        /// <summary>
        /// Gathers data from the eight neighbouring positions of an array seen as 2-dimensional area with a given width, into an array of eight elements (nine if asNine is enabled).
        /// </summary>
        /// <typeparam name="T">Type of the map data.</typeparam>
        /// <param name="mapdata">Map data.</param>
        /// <param name="mapWidth">Map width</param>
        /// <param name="x">X-coordinate of the cell to check</param>
        /// <param name="y">Y-coordinate of the cell to check</param>
        /// <param name="area">The usable area on the map</param>
        /// <param name="invalidValue">Value indicating cells that fell outside the map border.</param>
        /// <param name="asNine">True to put the data (and invalidity data) in an array of 9 elements instead of 8, with the value of the point to check including as the center. This means direct edges and corners can be checked more easily in a for-loop.</param>
        /// <returns>An array containing the data of the eight neighbouring positions around the given coordinate.</returns>
        private static T[] GetNeighbouringData<T>(T[] mapdata, Int32 mapWidth, Int32 x, Int32 y, Rectangle area, T invalidValue, Boolean asNine)
        {
            Boolean[] dataInvalid;
            return GetNeighbouringData<T>(mapdata, mapWidth, x, y, area, invalidValue, out dataInvalid, asNine);
        }

        /// <summary>
        /// Gathers data from the eight neighbouring positions of an array seen as 2-dimensional area with a given width, into an array of eight elements (nine if asNine is enabled).
        /// </summary>
        /// <typeparam name="T">Type of the map data.</typeparam>
        /// <param name="mapdata">Map data.</param>
        /// <param name="mapWidth">Map width</param>
        /// <param name="x">X-coordinate of the cell to check</param>
        /// <param name="y">Y-coordinate of the cell to check</param>
        /// <param name="area">The usable area on the map</param>
        /// <param name="invalidValue">Value indicating cells that fell outside the map border.</param>
        /// <param name="dataInvalid">An array telling which indices were out of bounds of the usable rectangle given by <see cref="area"/> area.</param>
        /// <param name="asNine">True to put the data (and invalidity data) in an array of 9 elements instead of 8, with the value of the point to check including as the center. This means direct edges and corners can be checked more easily in a for-loop.</param>
        /// <returns>An array containing the data of the eight neighbouring positions around the given coordinate.</returns>
        private static T[] GetNeighbouringData<T>(T[] mapdata, Int32 mapWidth, Int32 x, Int32 y, Rectangle area, T invalidValue, out Boolean[] dataInvalid, Boolean asNine)
        {
            dataInvalid = new Boolean[asNine ? 9 : 8];
            T[] typesAround = Enumerable.Repeat(invalidValue, asNine ? 9 : 8).ToArray();
            Int32[] offsetsAround = new Int32[] { 0, 1, 2, 3, asNine ? 5 : 4, asNine ? 6 : 5, asNine ? 7 : 6, asNine ? 8 : 7 };
            // AsNine:
            // 0 1 2
            // 3[4]5
            // 6 7 8
            // Else:
            // 0 1 2
            // 3 - 4
            // 5 6 7
            if (x == area.X)
            {
                dataInvalid[offsetsAround[0]] = true;
                dataInvalid[offsetsAround[3]] = true;
                dataInvalid[offsetsAround[5]] = true;
            }
            if (x == area.Right - 1)
            {
                dataInvalid[offsetsAround[2]] = true;
                dataInvalid[offsetsAround[4]] = true;
                dataInvalid[offsetsAround[7]] = true;
            }
            if (y == area.Y)
            {
                dataInvalid[offsetsAround[0]] = true;
                dataInvalid[offsetsAround[1]] = true;
                dataInvalid[offsetsAround[2]] = true;
            }
            if (y == area.Bottom - 1)
            {
                dataInvalid[offsetsAround[5]] = true;
                dataInvalid[offsetsAround[6]] = true;
                dataInvalid[offsetsAround[7]] = true;
            }
            if (!dataInvalid[offsetsAround[0]]) typesAround[offsetsAround[0]] = mapdata[(y - 1) * mapWidth + (x - 1)];
            if (!dataInvalid[offsetsAround[1]]) typesAround[offsetsAround[1]] = mapdata[(y - 1) * mapWidth + x];
            if (!dataInvalid[offsetsAround[2]]) typesAround[offsetsAround[2]] = mapdata[(y - 1) * mapWidth + (x + 1)];
            if (!dataInvalid[offsetsAround[3]]) typesAround[offsetsAround[3]] = mapdata[y * mapWidth + (x - 1)];
            if (asNine)                          typesAround[4]                = mapdata[y * mapWidth + x];
            if (!dataInvalid[offsetsAround[4]]) typesAround[offsetsAround[4]] = mapdata[y * mapWidth + (x + 1)];
            if (!dataInvalid[offsetsAround[5]]) typesAround[offsetsAround[5]] = mapdata[(y + 1) * mapWidth + (x - 1)];
            if (!dataInvalid[offsetsAround[6]]) typesAround[offsetsAround[6]] = mapdata[(y + 1) * mapWidth + x];
            if (!dataInvalid[offsetsAround[7]]) typesAround[offsetsAround[7]] = mapdata[(y + 1) * mapWidth + (x + 1)];
            return typesAround;
        }

        /// <summary>
        /// Expands the outside borders to the same types that are at the edges, to avoid getting awkward downward cuts in the height map at the map edges.
        /// </summary>
        /// <param name="simpleMap">Map simplified to terrain types</param>
        /// <param name="mapWidth">Map width</param>
        /// <param name="area">The usable area on the map</param>
        private static void ExpandOutsideBorders(TerrainTypeEnh[] simpleMap, Int32 mapWidth, Rectangle area)
        {
            for (Int32 y = area.Y; y < area.Right; ++y)
            {
                Int32 line = y*mapWidth;
                // duplicate leftmost cell of inner map to left edge
                TerrainTypeEnh htt = simpleMap[line + area.X];
                // Rocks are small objects. Check if it ends just outside the map.
                if (htt == TerrainTypeEnh.Rock)
                {
                    if (area.X > 0)
                        htt = simpleMap[line + area.X - 1];
                    else
                        htt = TerrainTypeEnh.Clear;
                }
                for (Int32 x = 0; x < area.X; ++x)
                    simpleMap[line + x] = htt;
                // duplicate rightmost cell of inner map to right edge
                htt = simpleMap[line + area.Right - 1];
                // Rocks are small objects. Check if it ends just outside the map.
                if (htt == TerrainTypeEnh.Rock)
                {
                    if (area.X > 0)
                        htt = simpleMap[line + area.X - 1];
                    else
                        htt = TerrainTypeEnh.Clear;
                }
                for (Int32 x = area.Right; x < mapWidth; ++x)
                    simpleMap[line + x] = htt;
            }
            // duplicate top row
            TerrainTypeEnh[] curRow = new TerrainTypeEnh[mapWidth];
            Int32 copyStartTop = area.Y * mapWidth;
            Array.Copy(simpleMap, copyStartTop, curRow, 0, mapWidth);
            for (Int32 i = 0; i < mapWidth; ++i)
            {
                // Rocks are small objects. Check if it ends just outside the map.
                if (curRow[i] == TerrainTypeEnh.Rock)
                {
                    if (area.Y > 0)
                        curRow[i] = simpleMap[copyStartTop - mapWidth + i];
                    else
                        curRow[i] = TerrainTypeEnh.Clear;
                }
            }
            for (Int32 y = 0; y < area.Y; ++y)
                Array.Copy(curRow, 0, simpleMap, y * mapWidth, mapWidth);

            // duplicate bottom row
            Int32 copyStartBottom = (area.Bottom - 1) * mapWidth;
            Array.Copy(simpleMap, copyStartBottom, curRow, 0, mapWidth);
            for (Int32 i = 0; i < mapWidth; ++i)
            {
                // Rocks are small objects. Check if it ends just outside the map.
                if (curRow[i] == TerrainTypeEnh.Rock)
                {
                    if (area.Y < 63)
                        curRow[i] = simpleMap[copyStartBottom + mapWidth + i];
                    else
                        curRow[i] = TerrainTypeEnh.Clear;
                }
            }
            for (Int32 y = area.Bottom; y < mapWidth; ++y)
                Array.Copy(curRow, 0, simpleMap, y * mapWidth, mapWidth);
        }
        
        public static Bitmap GenerateHeightMapImage65x65(Bitmap heightMap64x64)
        {
            // This does not stretch the image. It paints the 64x64 image on a (0.5,0.5) pixel offset into a 65x65 image.
            if (heightMap64x64.Width != 64 || heightMap64x64.Height != 64)
                throw new ArgumentException("Can only convert 64x64 images!", "heightMap64x64");
            heightMap64x64 = ImageUtils.ConvertToPalettedGrayscale(heightMap64x64);
            Bitmap bp = new Bitmap(65, 65, PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bp))
            {
                using (SolidBrush br = new SolidBrush(LEVEL_COLORS[1]))
                    gr.FillRectangle(br, new Rectangle(0, 0, 65, 65));
                // simple fill of outer edge by stretching image.
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.DrawImage(heightMap64x64, new Rectangle(0,0,65,65));
                // actual centering of the image
                gr.InterpolationMode = InterpolationMode.Bilinear;
                gr.DrawImage(heightMap64x64, 0.5f, 0.5f);
            }
            return bp;
        }
    }
}