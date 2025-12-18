using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Nyerguds.FileData.Westwood
{
    public static class MapConversion
    {
        public static readonly Dictionary<Int32, TileInfo> TILEINFO = ReadTileInfo();
        public static readonly Dictionary<Int32, CnCMapCell> DESERT_MAPPING = LoadMapping("th_desert.nms", EngieFileConverter.Properties.Resources.th_desert);
        public static readonly Dictionary<Int32, CnCMapCell> TEMPERATE_MAPPING = LoadMapping("th_temperate.nms", EngieFileConverter.Properties.Resources.th_temperate);
        public static readonly Dictionary<Int32, CnCMapCell> DESERT_MAPPING_REVERSED = LoadReverseMapping(DESERT_MAPPING);
        public static readonly Dictionary<Int32, CnCMapCell> TEMPERATE_MAPPING_REVERSED = LoadReverseMapping(TEMPERATE_MAPPING);

        private static Dictionary<Int32, TileInfo> ReadTileInfo()
        {
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2.ini");
            String tilesetsData2;
            if (File.Exists(file))
                tilesetsData2 = File.ReadAllText(file);
            else
                tilesetsData2 = EngieFileConverter.Properties.Resources.tilesets2;

            IniFile tilesetsFile2 = new IniFile(null, tilesetsData2, true, IniFile.ENCODING_DOS_US, true);
            Dictionary<Int32, TileInfo> tileInfo2 = new Dictionary<Int32, TileInfo>();
            //tilesets2.ini - new loading code
            for (Int32 currentId = 0; currentId < 0xFF; ++currentId)
            {
                String sectionName = tilesetsFile2.GetStringValue("TileSets", currentId.ToString(), null);
                if (sectionName == null)
                    continue;
                TileInfo info = new TileInfo();
                info.TileName = sectionName;
                info.Width = tilesetsFile2.GetIntValue(sectionName, "X", 1);
                info.Height = tilesetsFile2.GetIntValue(sectionName, "Y", 1);
                info.PrimaryHeightType = GeneralUtils.TryParseEnum(tilesetsFile2.GetStringValue(sectionName, "PrimaryType", null), TerrainTypeEnh.Clear, true);
                Char[] types = new Char[info.Width * info.Height];
                for (Int32 y = 0; y < info.Height; ++y)
                {
                    String typechars = tilesetsFile2.GetStringValue(sectionName, "Terrain" + y, String.Empty);
                    Int32 len = typechars.Length;
                    for (Int32 x = 0; x < info.Width; ++x)
                        types[y * info.Width + x] = x >= len ? '?' : typechars[x];
                }
                TerrainTypeEnh[] typedCells = new TerrainTypeEnh[types.Length];
                for (Int32 i = 0; i < types.Length; ++i)
                {
                    switch (types[i])
                    {
                        case '?':
                            typedCells[i] = info.PrimaryHeightType;
                            break;
                        case '_':
                            typedCells[i] = TerrainTypeEnh.Unused;
                            break;
                        case 'C':
                            typedCells[i] = TerrainTypeEnh.Clear;
                            break;
                        case 'W':
                            typedCells[i] = TerrainTypeEnh.Water;
                            break;
                        case 'I':
                            typedCells[i] = TerrainTypeEnh.Rock;
                            break;
                        case 'B':
                            typedCells[i] = TerrainTypeEnh.Beach;
                            break;
                        case 'R':
                            typedCells[i] = TerrainTypeEnh.Road;
                            break;
                        case 'P':
                            typedCells[i] = TerrainTypeEnh.CliffPlateau;
                            break;
                        case 'F':
                            typedCells[i] = TerrainTypeEnh.CliffFace;
                            break;
                        case 'M':
                            typedCells[i] = TerrainTypeEnh.Smudge;
                            break;
                        case 'S':
                            typedCells[i] = TerrainTypeEnh.Snow;
                            break;
                        default:
                            if (Regex.IsMatch(types[i].ToString(), "^[a-zA-Z]$"))
                                typedCells[i] = info.PrimaryHeightType;
                            else
                                typedCells[i] = TerrainTypeEnh.Unused;
                            break;
                    }
                }
                info.TypedCells = typedCells;
                tileInfo2.Add(currentId, info);
            }
            return tileInfo2;
        }

        private static Dictionary<Int32, CnCMapCell> LoadMapping(String filename, Byte[] internalFallback)
        {
            String[] errors;
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), filename);
            Byte[] mappingBytes;
            if (File.Exists(file))
                mappingBytes = File.ReadAllBytes(file);
            else
                mappingBytes = internalFallback;
            return LoadMapping(mappingBytes, out errors);
        }

        private static Dictionary<Int32, CnCMapCell> LoadReverseMapping(Dictionary<Int32, CnCMapCell> mapping)
        {
            Dictionary<Int32, CnCMapCell> newmapping = new Dictionary<Int32, CnCMapCell>();
            List<CnCMapCell> errorcells;
            Dictionary<Int32, CnCMapCell[]> mapping2 = GetReverseMapping(mapping, out errorcells);
            foreach (Int32 val in mapping2.Keys)
                newmapping.Add(val, mapping2[val][0]);
            return newmapping;
        }


        private static Dictionary<Int32, CnCMapCell> LoadMapping(Byte[] fileData, out String[] errors)
        {
            List<String> errorMessages = new List<String>();
            Dictionary<Int32, CnCMapCell> n64MapValues = new Dictionary<Int32, CnCMapCell>();
            Dictionary<Int32, CnCMapCell> reverseValues = new Dictionary<Int32, CnCMapCell>();
            using (MemoryStream ms = new MemoryStream(fileData))
            {
                Int32 amount = (Int32)ms.Length / 4;
                if (ms.Length != amount * 4)
                    throw new ArgumentException("file size must be divisible by 4!", "fileData");
                Byte[] buffer = new Byte[4];
                for (Int32 i = 0; i < amount; ++i)
                {
                    if (ms.Read(buffer, 0, 4) == 4)
                    {
                        CnCMapCell N64cell = new CnCMapCell(buffer[0], buffer[1]);
                        CnCMapCell PCcell = new CnCMapCell(buffer[2], buffer[3]);
                        if (n64MapValues.ContainsKey(N64cell.Value))
                        {
                            n64MapValues.Clear();
                            throw new ApplicationException("File contains duplicate entries!");
                        }
                        if (reverseValues.ContainsKey(PCcell.Value))
                            errorMessages.Add(String.Format("Value {0} - {1} - PC value {1} already mapped on N64 value {2}", N64cell.ToString(), PCcell.ToString(), reverseValues[PCcell.Value].ToString()));
                        else
                            reverseValues.Add(PCcell.Value, N64cell);
                        n64MapValues.Add(N64cell.Value, PCcell);
                    }
                }
            }
            errors = errorMessages.ToArray();
            return n64MapValues;
        }

        public static Byte[] SaveMapping(Dictionary<Int32, CnCMapCell> mapping)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                List<Int32> keys = new List<Int32>(mapping.Keys);
                keys.Sort();
                Int32 keyCount = keys.Count;
                for (Int32 i = 0; i < keyCount; ++i)
                {
                    Int32 key = keys[i];
                    CnCMapCell n64Cell = new CnCMapCell(key);
                    CnCMapCell pcCell = mapping[key];
                    ms.WriteByte(n64Cell.HighByte);
                    ms.WriteByte(n64Cell.LowByte);
                    ms.WriteByte(pcCell.HighByte);
                    ms.WriteByte(pcCell.LowByte);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static CnCMap ConvertMap(CnCMap map, Dictionary<Int32, CnCMapCell> mapping, Byte? defaultHigh, Byte? defaultLow, Boolean toN64, out List<CnCMapCell> errorcells)
        {
            Byte highByte = defaultHigh.GetValueOrDefault(0xFF);
            Byte lowByte = defaultLow.GetValueOrDefault((Byte)(toN64 ? 0xFF : 0x00));
            CnCMap newmap = new CnCMap(map.GetAsBytes());
            if (toN64)
            {
                CleanUpMapClearTerrain(newmap);
                // Prevents snow cells from being seen as as errors. They are a known anomaly that can be removed gracefully.
                RemoveSnow(newmap);
            }
            errorcells = new List<CnCMapCell>();
            for (Int32 i = 0; i < CnCMap.LENGTH; ++i)
            {
                Int32 cellvalue = newmap[i].Value;
                if ((!toN64 && cellvalue == 0xFFFF) || (toN64 && cellvalue == 0xFF00))
                {
                    newmap[i] = new CnCMapCell(toN64 ? 0xFFFF : 0xFF00);
                }
                else if (mapping.ContainsKey(cellvalue))
                {
                    newmap[i] = mapping[cellvalue];
                }
                else
                {
                    errorcells.Add(new CnCMapCell(cellvalue));
                    newmap[i] = new CnCMapCell(highByte, lowByte);
                }
            }
            return newmap;
        }

        private static Dictionary<Int32, CnCMapCell[]> GetReverseMapping(Dictionary<Int32, CnCMapCell> mapping, out List<CnCMapCell> errorcells)
        {
            Dictionary<Int32, CnCMapCell[]> newmapping = new Dictionary<Int32, CnCMapCell[]>();
            errorcells = new List<CnCMapCell>();
            foreach (Int32 mapval in mapping.Keys)
            {
                CnCMapCell cell = mapping[mapval];
                if (!newmapping.ContainsKey(cell.Value))
                    newmapping.Add(cell.Value, new CnCMapCell[] { new CnCMapCell(mapval) });
                else
                {
                    CnCMapCell[] orig = newmapping[cell.Value];
                    CnCMapCell[] arr = new CnCMapCell[orig.Length + 1];
                    Array.Copy(orig, arr, orig.Length);
                    arr[orig.Length] = new CnCMapCell(mapval);
                    newmapping[cell.Value] = arr;
                    if (!errorcells.Contains(cell))
                        errorcells.Add(cell);
                }
            }
            return newmapping;
        }

        /// <summary>
        /// Simplifies a map to an array of terrain types. This uses the enhanced terrain types which show to which side cliffs are facing.
        /// </summary>
        /// <param name="mapData">Map data.</param>
        /// <returns>The map data simplified to terrain types.</returns>
        public static TerrainTypeEnh[] SimplifyMap(CnCMap mapData)
        {
            TerrainTypeEnh[] simplifiedMap = new TerrainTypeEnh[64 * 64];
            for (Int32 i = 0; i < mapData.Cells.Length; ++i)
            {
                CnCMapCell cell = mapData.Cells[i];
                TerrainTypeEnh terrain = TerrainTypeEnh.Clear;
                if (cell.HighByte != 0xFF)
                {
                    TileInfo info;
                    if (TILEINFO.TryGetValue(cell.HighByte, out info))
                    {
                        if (info.TypedCells.Length > cell.LowByte)
                            terrain = info.TypedCells[cell.LowByte];
                        else
                            terrain = info.PrimaryHeightType;
                    }
                    else throw new NotSupportedException("Unknown terrain data encountered.");
                }
                simplifiedMap[i] = terrain;
            }
            return simplifiedMap;
        }

        /// <summary>
        /// Cleans up wrongly saved blank terrain cells (either as 00XX or as FFFF)
        /// by replacing them by the real default FF00 terrain.
        /// </summary>
        /// <param name="map">The map to fix.</param>
        public static void CleanUpMapClearTerrain(CnCMap map)
        {
            for (Int32 i = 0; i < CnCMap.LENGTH; ++i)
            {
                CnCMapCell cell = map.Cells[i];
                if (cell.HighByte == 0 // XCC
                    || (cell.HighByte == 0xFF && cell.LowByte == 0xFF)) // cncmap
                {
                    cell.HighByte = 0xFF;
                    cell.LowByte = 0x00;
                }
            }
        }

        /// <summary>
        /// Replaces snow with clear terrain.
        /// </summary>
        /// <param name="map">Removes snow from a map, since the N64 version can't handle it.</param>
        public static void RemoveSnow(CnCMap map)
        {
            for (Int32 i = 0; i < CnCMap.LENGTH; ++i)
            {
                CnCMapCell cell = map.Cells[i];
                TileInfo tileInfo;
                if (!TILEINFO.TryGetValue(cell.HighByte, out tileInfo))
                    continue;
                if (tileInfo.TypedCells.Length <= cell.LowByte)
                    continue;
                if (tileInfo.TypedCells[cell.LowByte] != TerrainTypeEnh.Snow)
                    continue;
                cell.HighByte = 0xFF;
                cell.LowByte = 0x00;
            }
        }
    }
}
