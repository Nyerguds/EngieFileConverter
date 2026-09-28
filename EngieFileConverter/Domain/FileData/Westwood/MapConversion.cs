using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nyerguds.FileData.Westwood
{
    public static class MapConversion
    {
        public static readonly Dictionary<int, TileInfo> TILEINFO_TD = ReadTileInfoTd();
        public static readonly Dictionary<int, TileInfo> TILEINFO_RA = ReadTileInfoRa();
        public static readonly Dictionary<string, StructInfo> STRUCTUREINFO = ReadStructInfo(EngieFileConverter.Properties.Resources.structs, "structs.ini", "Structures");
        public static readonly Dictionary<string, StructInfo> TERRAININFO = ReadStructInfo(EngieFileConverter.Properties.Resources.terrain, "terrain.ini", "Terrain");
        public static readonly Dictionary<int, CnCMapCell> DESERT_MAPPING = LoadMapping("th_desert.nms", EngieFileConverter.Properties.Resources.th_desert);
        public static readonly Dictionary<int, CnCMapCell> TEMPERATE_MAPPING = LoadMapping("th_temperate.nms", EngieFileConverter.Properties.Resources.th_temperate);
        public static readonly Dictionary<int, CnCMapCell> DESERT_MAPPING_REVERSED = LoadReverseMapping(DESERT_MAPPING);
        public static readonly Dictionary<int, CnCMapCell> TEMPERATE_MAPPING_REVERSED = LoadReverseMapping(TEMPERATE_MAPPING);

        private static Dictionary<int, TileInfo> ReadTileInfoTd()
        {
            string file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2.ini");
            string tilesetsData2;
            if (File.Exists(file))
                tilesetsData2 = File.ReadAllText(file);
            else
                tilesetsData2 = EngieFileConverter.Properties.Resources.tilesets2;
            return ReadTileInfo(tilesetsData2, 0xFF);
        }

        private static Dictionary<int, TileInfo> ReadTileInfoRa()
        {
            string file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2ra.ini");
            string tilesetsData2;
            if (File.Exists(file))
                tilesetsData2 = File.ReadAllText(file);
            else
                tilesetsData2 = EngieFileConverter.Properties.Resources.tilesets2ra;
            return ReadTileInfo(tilesetsData2, 0xFFFF);
        }

        private static Dictionary<int, TileInfo> ReadTileInfo(string tilesFile, int maxId)
        {
            IniFile tilesetsFile2 = new IniFile(null, tilesFile, true, IniFile.ENCODING_DOS_US, true);
            Dictionary<int, TileInfo> tileInfo2 = new Dictionary<int, TileInfo>();
            //tilesets2.ini - new loading code
            for (int currentId = 0; currentId < maxId; ++currentId)
            {
                string sectionName = tilesetsFile2.GetStringValue("TileSets", currentId.ToString(), null);
                if (sectionName == null)
                    continue;
                if (sectionName.StartsWith("WC"))
                {

                }
                TileInfo info = new TileInfo();
                info.TileName = sectionName;
                int width = tilesetsFile2.GetIntValue(sectionName, "X", 1);
                int height = tilesetsFile2.GetIntValue(sectionName, "Y", 1);
                info.Width = width;
                info.Height = height;
                info.PrimaryHeightType = GeneralUtils.TryParseEnum(tilesetsFile2.GetStringValue(sectionName, "PrimaryType", null), TerrainTypeEnh.Clear, true);
                int cells = width * height;
                char[] types = new char[cells];
                for (int y = 0; y < height; ++y)
                {
                    string typechars = tilesetsFile2.GetStringValue(sectionName, "Terrain" + y, String.Empty);
                    int len = typechars.Length;
                    for (int x = 0; x < width; ++x)
                        types[y * width + x] = x >= len ? '?' : typechars[x];
                }
                TerrainTypeEnh[] typedCells = new TerrainTypeEnh[cells];
                for (int i = 0; i < cells; ++i)
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
                        case 'V':
                            typedCells[i] = TerrainTypeEnh.River;
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
                        case 'F':
                            typedCells[i] = TerrainTypeEnh.CliffFace;
                            break;
                        case 'P':
                            typedCells[i] = TerrainTypeEnh.CliffPlateau;
                            break;
                        case 'L':
                            typedCells[i] = TerrainTypeEnh.CliffPlateauwater;
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

        private static Dictionary<string, StructInfo> ReadStructInfo(string structsResource, string structsFilename, string listSection)
        {
            string file = Path.Combine(GeneralUtils.GetApplicationPath(), structsFilename);
            string fileGrids = Path.Combine(GeneralUtils.GetApplicationPath(), "grids.ini");
            string structData;
            if (File.Exists(file))
                structData = File.ReadAllText(file);
            else
                structData = structsResource;
            IniFile structsFile = new IniFile(null, structData, true, IniFile.ENCODING_DOS_US, true);

            string gridsData = null;
            if (File.Exists(fileGrids))
                gridsData = File.ReadAllText(fileGrids);
            else
                gridsData = EngieFileConverter.Properties.Resources.grids;
            IniFile gridsFile = gridsData == null ? null : new IniFile(null, gridsData, true, IniFile.ENCODING_DOS_US, true);

            Dictionary<string, StructInfo> structs = new Dictionary<string, StructInfo>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, string> structsList = structsFile.GetSectionContent(listSection);
            Regex dimRegex = new Regex("^\\s*(\\d+)\\s*x\\s*(\\d+)\\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            int curId = 0;
            string curIdStr = curId.ToString();
            while (structsList.ContainsKey(curIdStr))
            {
                string structName = structsList[curIdStr];
                Dictionary<string, string> structInfo = structsFile.GetSectionContent(structName);
                string occupy;
                int width = 1;
                if (gridsFile != null && structInfo.ContainsKey("OccupyList"))
                {
                    occupy = GetOccupyList(gridsFile, structInfo["OccupyList"], out width);
                }
                else
                {
                    occupy = String.Empty;
                }
                bool[] occupyList = new bool[occupy.Length];
                for (int i = 0; i < occupy.Length; ++i)
                {
                    char cell = occupy[i];
                    occupyList[i] = isAlphabetChar(cell);
                }
                string dimensions;
                if (!structInfo.TryGetValue("Dimensions", out dimensions))
                    dimensions = "1x1";
                Match dimMatch = dimRegex.Match(dimensions);
                if (dimMatch.Success)
                {
                    StructInfo si = new StructInfo();
                    si.StructName = structName;
                    si.Width = width;
                    si.Height = Int32.Parse(dimMatch.Groups[2].Value);
                    si.OccupyList = occupyList;
                    si.HasBib = structsFile.GetBoolValue(structName, "HasBib", false);
                    structs.Add(structName, si);
                }
                curId++;
                curIdStr = curId.ToString();
            }
            return structs;
        }

        private static string GetOccupyList(IniFile gridsFile, string gridName, out int width)
        {
            Dictionary<string, string> grid = gridsFile.GetSectionContent(gridName);
            int curId = 0;
            string curIdStr = curId.ToString();
            List<string> lines = new List<string>();
            while (grid.ContainsKey(curIdStr))
            {
                lines.Add(grid[curIdStr]);
                curId++;
                curIdStr = curId.ToString();
            }
            StringBuilder fullGrid = new StringBuilder();
            width = lines.Max(ln => ln.Length);
            foreach (string line in lines)
            {
                int add = width - line.Length;
                fullGrid.Append(line);
                if (add > 0)
                    fullGrid.Append(new string('-', add));
            }
            return fullGrid.ToString();
        }

        private static bool isAlphabetChar(char ch)
        {
            return ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'));
        }

        private static Dictionary<int, CnCMapCell> LoadMapping(string filename, byte[] internalFallback)
        {
            string file = Path.Combine(GeneralUtils.GetApplicationPath(), filename);
            byte[] mappingBytes = File.Exists(file) ? File.ReadAllBytes(file) : internalFallback;
            return LoadMapping(mappingBytes, out _);
        }

        private static Dictionary<int, CnCMapCell> LoadReverseMapping(Dictionary<int, CnCMapCell> mapping)
        {
            Dictionary<int, CnCMapCell> newmapping = new Dictionary<int, CnCMapCell>();
            List<CnCMapCell> errorcells;
            Dictionary<int, CnCMapCell[]> mapping2 = GetReverseMapping(mapping, out errorcells);
            foreach (int val in mapping2.Keys)
                newmapping.Add(val, mapping2[val][0]);
            return newmapping;
        }


        private static Dictionary<int, CnCMapCell> LoadMapping(byte[] fileData, out string[] errors)
        {
            List<string> errorMessages = new List<string>();
            Dictionary<int, CnCMapCell> n64MapValues = new Dictionary<int, CnCMapCell>();
            Dictionary<int, CnCMapCell> reverseValues = new Dictionary<int, CnCMapCell>();
            using (MemoryStream ms = new MemoryStream(fileData))
            {
                int amount = (int)ms.Length / 4;
                if (ms.Length != amount * 4)
                    throw new ArgumentException("file size must be divisible by 4.", "fileData");
                byte[] buffer = new byte[4];
                for (int i = 0; i < amount; ++i)
                {
                    if (ms.Read(buffer, 0, 4) == 4)
                    {
                        CnCMapCell N64cell = new CnCMapCell(buffer[0], buffer[1], false);
                        CnCMapCell PCcell = new CnCMapCell(buffer[2], buffer[3], false);
                        if (n64MapValues.ContainsKey(N64cell.ValueTD))
                        {
                            n64MapValues.Clear();
                            throw new ApplicationException("File contains duplicate entries.");
                        }
                        if (reverseValues.ContainsKey(PCcell.ValueTD))
                            errorMessages.Add(String.Format("Value {0} - {1} - PC value {1} already mapped on N64 value {2}", N64cell.ToString(), PCcell.ToString(), reverseValues[PCcell.ValueTD].ToString()));
                        else
                            reverseValues.Add(PCcell.ValueTD, N64cell);
                        n64MapValues.Add(N64cell.ValueTD, PCcell);
                    }
                }
            }
            errors = errorMessages.ToArray();
            return n64MapValues;
        }

        public static byte[] SaveMapping(Dictionary<int, CnCMapCell> mapping)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                List<int> keys = new List<int>(mapping.Keys);
                keys.Sort();
                int keyCount = keys.Count;
                for (int i = 0; i < keyCount; ++i)
                {
                    int key = keys[i];
                    CnCMapCell n64Cell = new CnCMapCell(key);
                    CnCMapCell pcCell = mapping[key];
                    ms.WriteByte((byte)n64Cell.TemplateType);
                    ms.WriteByte(n64Cell.Icon);
                    ms.WriteByte((byte)pcCell.TemplateType);
                    ms.WriteByte(pcCell.Icon);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static CnCMap ConvertMap(CnCMap map, Dictionary<int, CnCMapCell> mapping, byte? defaultHigh, byte? defaultLow, bool toN64, out List<CnCMapCell> errorcells)
        {
            byte highByte = defaultHigh.GetValueOrDefault(0xFF);
            byte lowByte = defaultLow.GetValueOrDefault((byte)(toN64 ? 0xFF : 0x00));
            CnCMap newmap = new CnCMap(map.GetAsBytes(), false);
            if (toN64)
            {
                CleanUpMapClearTerrain(newmap);
                // Prevents snow cells from being seen as as errors. They are a known anomaly that can be removed gracefully.
                RemoveSnow(newmap);
            }
            errorcells = new List<CnCMapCell>();
            for (int i = 0; i < CnCMap.LENGTH_TD; ++i)
            {
                int cellvalue = newmap[i].ValueTD;
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
                    newmap[i] = new CnCMapCell(highByte, lowByte, false);
                }
            }
            return newmap;
        }

        private static Dictionary<int, CnCMapCell[]> GetReverseMapping(Dictionary<int, CnCMapCell> mapping, out List<CnCMapCell> errorcells)
        {
            Dictionary<int, CnCMapCell[]> newmapping = new Dictionary<int, CnCMapCell[]>();
            errorcells = new List<CnCMapCell>();
            foreach (int mapval in mapping.Keys)
            {
                CnCMapCell cell = mapping[mapval];
                if (!newmapping.ContainsKey(cell.ValueTD))
                    newmapping.Add(cell.ValueTD, new CnCMapCell[] { new CnCMapCell(mapval) });
                else
                {
                    CnCMapCell[] orig = newmapping[cell.ValueTD];
                    CnCMapCell[] arr = new CnCMapCell[orig.Length + 1];
                    Array.Copy(orig, arr, orig.Length);
                    arr[orig.Length] = new CnCMapCell(mapval);
                    newmapping[cell.ValueTD] = arr;
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
        public static TerrainTypeEnh[] SimplifyMap(CnCMap mapData, Dictionary<int, TileInfo> tileInfo)
        {
            int emptyType = mapData.IsRaType ? 0xFFFF : 0xFF;
            TerrainTypeEnh[] simplifiedMap = new TerrainTypeEnh[mapData.Cells.Length];
            for (int i = 0; i < mapData.Cells.Length; ++i)
            {
                CnCMapCell cell = mapData.Cells[i];
                TerrainTypeEnh terrain = TerrainTypeEnh.Clear;
                if (cell.TemplateType != emptyType)
                {
                    TileInfo info;
                    if (tileInfo.TryGetValue(cell.TemplateType, out info))
                    {
                        if (info.TypedCells.Length > cell.Icon)
                            terrain = info.TypedCells[cell.Icon];
                        else
                            terrain = info.PrimaryHeightType;
                    }
                    else throw new ArgumentException("Unknown terrain data encountered.", "mapData");
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
            for (int i = 0; i < CnCMap.LENGTH_TD; ++i)
            {
                CnCMapCell cell = map.Cells[i];
                if (cell.TemplateType == 0 // XCC
                    || (cell.TemplateType == 0xFF && cell.Icon == 0xFF)) // cncmap
                {
                    cell.TemplateType = 0xFF;
                    cell.Icon = 0x00;
                }
            }
        }

        /// <summary>
        /// Replaces snow with clear terrain.
        /// </summary>
        /// <param name="map">Removes snow from a map, since the N64 version can't handle it.</param>
        public static void RemoveSnow(CnCMap map)
        {
            for (int i = 0; i < CnCMap.LENGTH_TD; ++i)
            {
                CnCMapCell cell = map.Cells[i];
                TileInfo tileInfo;
                if (!TILEINFO_TD.TryGetValue(cell.TemplateType, out tileInfo))
                    continue;
                if (tileInfo.TypedCells.Length <= cell.Icon)
                    continue;
                if (tileInfo.TypedCells[cell.Icon] != TerrainTypeEnh.Snow)
                    continue;
                cell.TemplateType = 0xFF;
                cell.Icon = 0x00;
            }
        }
    }
}
