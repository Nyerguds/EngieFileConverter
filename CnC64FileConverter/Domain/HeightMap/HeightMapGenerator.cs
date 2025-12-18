using Nyerguds.GameData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using CnC64FileConverter.Domain.FileTypes;

namespace CnC64FileConverter.Domain.HeightMap
{
    public class HeightMapGenerator
    {
        private const Int32 BASE_HEIGHT = 0x40;
        private const Int32 ROCKS_HEIGHT_DIFF = 0x20;
        private const Int32 BEACH_HEIGHT_DIFF = -0x10;
        private const Int32 WATER_HEIGHT_DIFF = -0x20;
        private static readonly Color[] LEVEL_COLORS = Enumerable.Range(0, 5).Select(x => Color.FromArgb(Math.Min(0xFF, BASE_HEIGHT * x), Math.Min(0xFF, BASE_HEIGHT * x), Math.Min(0xFF, BASE_HEIGHT * x))).ToArray();

        public static FileImagePng GeneratePlateauImage64x64(FileMapWwCc1Pc mapFile, String suffix)
        {
            return GenerateHeightMapImage64x64(mapFile, null, true, suffix);
        }

        public static FileImagePng GenerateHeightMapImage64x64(FileMapWwCc1Pc mapFile, Bitmap plateauLevelsImage, String suffix)
        {
            return GenerateHeightMapImage64x64(mapFile, plateauLevelsImage, false, suffix);
        }

        private static FileImagePng GenerateHeightMapImage64x64(FileMapWwCc1Pc map, Bitmap plateauLevelsImage, Boolean forPlateau, String returnNameSuffix)
        {
            String loadedPath = map.LoadedFile;
            String baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            String iniFileName = baseFileName + ".ini";
            String pngFileName = baseFileName + (returnNameSuffix ?? String.Empty) + ".png";
            IniFile mapInfo = !File.Exists(iniFileName) ? null : new IniFile(iniFileName, IniFile.ENCODING_DOS_US);

            if (forPlateau)
                plateauLevelsImage = null;
            TerrainTypeEnh[] simpleMap = MapConversion.SimplifyMap(map.Map);

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
            mapStartX = Math.Min(63, Math.Max(1,mapStartX));
            mapStartY = Math.Min(63, Math.Max(1,mapStartY));
            mapEndX =  Math.Min(63, Math.Max(1,mapEndX));
            mapEndY = Math.Min(63, Math.Max(1, mapEndY));

            ExpandOutsideBorders(simpleMap, mapStartX, mapStartY, mapEndX, mapEndY);

            Byte[] eightBitPlateauData;
            if (plateauLevelsImage != null)
            {
                if (plateauLevelsImage.Width != 64 || plateauLevelsImage.Height != 64)
                    throw new NotSupportedException("Plateau levels image needs to be 64x64!");
                Int32 plateauStride;
                Byte[] plateauData = ImageUtils.GetImageData(ImageUtils.PaintOn32bpp(plateauLevelsImage, Color.Black), out plateauStride);
                eightBitPlateauData = ImageUtils.Convert32BitToPaletted(plateauData, plateauLevelsImage.Width, plateauLevelsImage.Height, 8, false, LEVEL_COLORS, ref plateauStride);
                ReducePlateaus(eightBitPlateauData, simpleMap, 0, 0, 64, 64);
                ReducePlateausEnh(eightBitPlateauData, simpleMap, 0, 0, 64, 64);
            }
            else
            {
                eightBitPlateauData = null;
            }
            Byte[] heightMapData = new Byte[64 * 64];
            for (Int32 i = 0; i < heightMapData.Length; i++)
            {
                Int32 val = BASE_HEIGHT;
                if (eightBitPlateauData != null)
                    val = val / 2 * eightBitPlateauData[i];
                TerrainTypeEnh cur = simpleMap[i];
                switch (cur)
                {
                    case TerrainTypeEnh.Clear:
                    case TerrainTypeEnh.Road:
                    case TerrainTypeEnh.Smudge:
                    case TerrainTypeEnh.Snow:

                        break;
                    case TerrainTypeEnh.Water:
                        if (!forPlateau)
                            val += WATER_HEIGHT_DIFF;
                        break;
                    case TerrainTypeEnh.Rock:
                        if (!forPlateau)
                            val += ROCKS_HEIGHT_DIFF;
                        break;
                    case TerrainTypeEnh.Beach:
                        if (!forPlateau)
                            val += BEACH_HEIGHT_DIFF;
                        break;
                    case TerrainTypeEnh.CliffFace:
                        if (forPlateau)
                            val += BASE_HEIGHT;
                        break;
                    case TerrainTypeEnh.CliffPlateau:
                        if (forPlateau || eightBitPlateauData == null)
                            val += BASE_HEIGHT;
                        break;
                }
                heightMapData[i] = (Byte)Math.Min(Math.Max(0, val), 255);
            }
            Color[] palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            if (forPlateau)
            {
                heightMapData = ImageUtils.Match8BitDataToPalette(heightMapData, 64, 64, palette, LEVEL_COLORS);
                palette = LEVEL_COLORS;
            }
            Bitmap bm = ImageUtils.BuildImage(heightMapData, 64, 64, 64, PixelFormat.Format8bppIndexed, palette, Color.Empty);
            FileImagePng returnImg = new FileImagePng();
            returnImg.LoadFile(bm, pngFileName);
            return returnImg;
        }

        private static void ReducePlateaus(Byte[] eightBitPlateauData, TerrainTypeEnh[] simpleMap, Int32 mapStartX, Int32 mapStartY, Int32 mapEndX, Int32 mapEndY)
        {
            for (Int32 y = mapStartY; y < mapEndY; y++)
            {
                for (Int32 x = mapStartX; x < mapEndX; x++)
                {
                    Int32 offset = y * 64 + x;
                    if (simpleMap[offset] == TerrainTypeEnh.CliffFace)
                        eightBitPlateauData[offset] = (Byte)Math.Max(0, eightBitPlateauData[offset] - 1);
                }
            }
        }

        private static void ReducePlateausEnh(Byte[] eightBitPlateauData, TerrainTypeEnh[] simpleMap, Int32 mapStartX, Int32 mapStartY, Int32 mapEndX, Int32 mapEndY)
        {
            Boolean[] reduce = new Boolean[eightBitPlateauData.Length];
            for (Int32 y = mapStartY; y < mapEndY; y++)
            {
                for (Int32 x = mapStartX; x < mapEndX; x++)
                {
                    Int32 height = eightBitPlateauData[y * 64 + x];
                    Byte[] heightsAround = GetNeighbouringTypes(eightBitPlateauData, x, y, mapStartX, mapStartY, mapEndX, mapEndY, (Byte)1, (Byte)0xFF);
                    TerrainTypeEnh type = simpleMap[y * 64 + x];
                    TerrainTypeEnh[] typesAround = GetNeighbouringTypes(simpleMap, x, y, mapStartX, mapStartY, mapEndX, mapEndY, TerrainTypeEnh.Clear, TerrainTypeEnh.Unused);
                    // 0 1 2
                    // 3   4
                    // 5 6 7
                    Boolean reduceThis = false;
                    if (type == TerrainTypeEnh.Clear || type == TerrainTypeEnh.Road || type == TerrainTypeEnh.Snow || type == TerrainTypeEnh.Smudge)
                    {
                        for (Int32 i = 0; i < typesAround.Length; i++)
                        {
                            if ((typesAround[i] == TerrainTypeEnh.Clear || typesAround[i] == TerrainTypeEnh.Road || typesAround[i] == TerrainTypeEnh.Snow || typesAround[i] == TerrainTypeEnh.Smudge) && heightsAround[i] < height)
                            {
                                reduceThis = true;
                                break;
                            }
                        }
                    }
                    reduce[y * 64 + x] = reduceThis;
                }
            }
            for (Int32 y = 0; y < 64; y++)
            {
                for (Int32 x = 0; x < 64; x++)
                {
                    Int32 val2 = eightBitPlateauData[y * 64 + x] * 2;
                    if (reduce[y * 64 + x])
                        val2 -= 1;
                    eightBitPlateauData[y * 64 + x] = (Byte)Math.Max(0, val2);
                }
            }
        }

        private static T[] GetNeighbouringTypes<T>(T[] mapdata, Int32 x, Int32 y, Int32 mapStartX, Int32 mapStartY, Int32 mapEndX, Int32 mapEndY, T defaultValue, T invalidValue)
        {
            T[] typesAround = Enumerable.Repeat(defaultValue, 8).ToArray();
            // 0 1 2
            // 3   4
            // 5 6 7
            if (x == mapStartX)
            {
                typesAround[0] = invalidValue;
                typesAround[3] = invalidValue;
                typesAround[5] = invalidValue;
            }
            if (x == mapEndX - 1)
            {
                typesAround[2] = invalidValue;
                typesAround[4] = invalidValue;
                typesAround[7] = invalidValue;
            }
            if (y == mapStartY)
            {
                typesAround[0] = invalidValue;
                typesAround[1] = invalidValue;
                typesAround[2] = invalidValue;
            }
            if (y == mapEndY - 1)
            {
                typesAround[5] = invalidValue;
                typesAround[6] = invalidValue;
                typesAround[7] = invalidValue;
            }

            if (!typesAround[0].Equals(invalidValue)) typesAround[0] = mapdata[(y - 1) * 64 + (x - 1)];
            if (!typesAround[1].Equals(invalidValue)) typesAround[1] = mapdata[(y - 1) * 64 + x];
            if (!typesAround[2].Equals(invalidValue)) typesAround[2] = mapdata[(y - 1) * 64 + (x + 1)];

            if (!typesAround[3].Equals(invalidValue)) typesAround[3] = mapdata[y * 64 + (x - 1)];
            //T type = mapdata[y * 64 + x];
            if (!typesAround[4].Equals(invalidValue)) typesAround[4] = mapdata[y * 64 + (x + 1)];

            if (!typesAround[5].Equals(invalidValue)) typesAround[5] = mapdata[(y + 1) * 64 + (x - 1)];
            if (!typesAround[6].Equals(invalidValue)) typesAround[6] = mapdata[(y + 1) * 64 + x];
            if (!typesAround[7].Equals(invalidValue)) typesAround[7] = mapdata[(y + 1) * 64 + (x + 1)];

            return typesAround;
        }

        private static void ExpandOutsideBorders(TerrainTypeEnh[] simpleMap, Int32 mapStartX, Int32 mapStartY, Int32 mapEndX, Int32 mapEndY)
        {
            for (Int32 y = mapStartY; y < mapEndY; y++)
            {
                // duplicate leftmost cell of inner map to left edge
                TerrainTypeEnh htt = simpleMap[y * 64 + mapStartX];
                if (htt == TerrainTypeEnh.Rock)
                    htt = TerrainTypeEnh.Clear;
                for (Int32 x = 0; x < mapStartX; x++)
                    simpleMap[y * 64 + x] = htt;
                // duplicate rightmost cell of inner map to right edge
                htt = simpleMap[y * 64 + mapEndX - 1];
                if (htt == TerrainTypeEnh.Rock)
                    htt = TerrainTypeEnh.Clear;
                for (Int32 x = mapEndX; x < 64; x++)
                    simpleMap[y * 64 + x] = htt;
            }
            // duplicate top row
            TerrainTypeEnh[] curRow = new TerrainTypeEnh[64];
            Array.Copy(simpleMap, mapStartY * 64, curRow, 0, 64);
            for (Int32 i = 0; i < 64; i++)
                if (curRow[i] == TerrainTypeEnh.Rock)
                    curRow[i] = TerrainTypeEnh.Clear;
            for (Int32 y = 0; y < mapStartY; y++)
                Array.Copy(curRow, 0, simpleMap, y * 64, 64);

            // duplicate bottom row
            Array.Copy(simpleMap, (mapEndY - 1) * 64, curRow, 0, 64);
            for (Int32 i = 0; i < 64; i++)
                if (curRow[i] == TerrainTypeEnh.Rock)
                    curRow[i] = TerrainTypeEnh.Clear;
            for (Int32 y = mapEndY; y < 64; y++)
                Array.Copy(curRow, 0, simpleMap, y * 64, 64);
        }


        public static Bitmap GenerateHeightMapImage65x65(Bitmap heightMap64x64)
        {
            if (heightMap64x64.Width != 64 || heightMap64x64.Height != 64)
                throw new NotSupportedException("Can only convert 64x64 images!");
            heightMap64x64 = ImageUtils.ConvertToPalettedGrayscale(heightMap64x64);
            //Bitmap heightMap62x62 = ImageUtils.CloneImage(heightMap64x64, 1, 1, 62, 62);
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