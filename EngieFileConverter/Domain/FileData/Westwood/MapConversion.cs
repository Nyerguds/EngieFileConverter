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
        public static readonly Dictionary<Int32, TileInfo> TILEINFO_TD = ReadTileInfoTd();
        public static readonly Dictionary<Int32, TileInfo> TILEINFO_RA = ReadTileInfoRa();
        public static readonly Dictionary<String, StructInfo> STRUCTUREINFO = ReadStructInfo(EngieFileConverter.Properties.Resources.structs, "structs.ini", "Structures");
        public static readonly Dictionary<String, StructInfo> TERRAININFO = ReadStructInfo(EngieFileConverter.Properties.Resources.terrain, "terrain.ini", "Terrain");
        public static readonly Dictionary<Int32, CnCMapCell> DESERT_MAPPING = LoadMapping("th_desert.nms", EngieFileConverter.Properties.Resources.th_desert);
        public static readonly Dictionary<Int32, CnCMapCell> TEMPERATE_MAPPING = LoadMapping("th_temperate.nms", EngieFileConverter.Properties.Resources.th_temperate);
        public static readonly Dictionary<Int32, CnCMapCell> DESERT_MAPPING_REVERSED = LoadReverseMapping(DESERT_MAPPING);
        public static readonly Dictionary<Int32, CnCMapCell> TEMPERATE_MAPPING_REVERSED = LoadReverseMapping(TEMPERATE_MAPPING);

        private static Dictionary<Int32, TileInfo> ReadTileInfoTd()
        {
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2.ini");
            String tilesetsData2;
            if (File.Exists(file))
                tilesetsData2 = File.ReadAllText(file);
            else
                tilesetsData2 = EngieFileConverter.Properties.Resources.tilesets2;
            return ReadTileInfo(tilesetsData2, 0xFF);
        }

        private static Dictionary<Int32, TileInfo> ReadTileInfoRa()
        {
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2ra.ini");
            String tilesetsData2;
            if (File.Exists(file))
                tilesetsData2 = File.ReadAllText(file);
            else
                tilesetsData2 = EngieFileConverter.Properties.Resources.tilesets2ra;
            return ReadTileInfo(tilesetsData2, 0xFFFF);
        }

        private static Dictionary<Int32, TileInfo> ReadTileInfo(string tilesFile, int maxId)
        {
            IniFile tilesetsFile2 = new IniFile(null, tilesFile, true, IniFile.ENCODING_DOS_US, true);
            Dictionary<Int32, TileInfo> tileInfo2 = new Dictionary<Int32, TileInfo>();
            //tilesets2.ini - new loading code
            for (Int32 currentId = 0; currentId < maxId; ++currentId)
            {
                String sectionName = tilesetsFile2.GetStringValue("TileSets", currentId.ToString(), null);
                if (sectionName == null)
                    continue;
                if (sectionName.StartsWith("WC"))
                {

                }
                TileInfo info = new TileInfo();
                info.TileName = sectionName;
                Int32 width = tilesetsFile2.GetIntValue(sectionName, "X", 1);
                Int32 height = tilesetsFile2.GetIntValue(sectionName, "Y", 1);
                info.Width = width;
                info.Height = height;
                info.PrimaryHeightType = GeneralUtils.TryParseEnum(tilesetsFile2.GetStringValue(sectionName, "PrimaryType", null), TerrainTypeEnh.Clear, true);
                Int32 cells = width * height;
                Char[] types = new Char[cells];
                for (Int32 y = 0; y < height; ++y)
                {
                    String typechars = tilesetsFile2.GetStringValue(sectionName, "Terrain" + y, String.Empty);
                    Int32 len = typechars.Length;
                    for (Int32 x = 0; x < width; ++x)
                        types[y * width + x] = x >= len ? '?' : typechars[x];
                }
                TerrainTypeEnh[] typedCells = new TerrainTypeEnh[cells];
                for (Int32 i = 0; i < cells; ++i)
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

        private static Dictionary<String, StructInfo> ReadStructInfo(String structsResource, String structsFilename, String listSection)
        {
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), structsFilename);
            String fileGrids = Path.Combine(GeneralUtils.GetApplicationPath(), "grids.ini");
            String structData;
            if (File.Exists(file))
                structData = File.ReadAllText(file);
            else
                structData = structsResource;
            IniFile structsFile = new IniFile(null, structData, true, IniFile.ENCODING_DOS_US, true);

            String gridsData = null;
            if (File.Exists(fileGrids))
                gridsData = File.ReadAllText(fileGrids);
            else
                gridsData = EngieFileConverter.Properties.Resources.grids;
            IniFile gridsFile = gridsData == null ? null : new IniFile(null, gridsData, true, IniFile.ENCODING_DOS_US, true);

            Dictionary<String, StructInfo> structs = new Dictionary<String, StructInfo>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<String, String> structsList = structsFile.GetSectionContent(listSection);
            Regex dimRegex = new Regex("^\\s*(\\d+)\\s*x\\s*(\\d+)\\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Int32 curId = 0;
            String curIdStr = curId.ToString();
            while (structsList.ContainsKey(curIdStr))
            {
                String structName = structsList[curIdStr];
                Dictionary<String, String> structInfo = structsFile.GetSectionContent(structName);
                String occupy;
                Int32 width = 1;
                if (gridsFile != null && structInfo.ContainsKey("OccupyList"))
                    occupy = GetOccupyList(gridsFile, structInfo["OccupyList"], out width);
                else
                    occupy = String.Empty;
                
                Boolean[] occupyList = new Boolean[occupy.Length];
                for (Int32 i = 0; i < occupy.Length; ++i)
                {
                    Char cell = occupy[i];
                    occupyList[i] = isAlphabetChar(cell);
                }
                String dimensions;
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

        private static String GetOccupyList(IniFile gridsFile, String gridName, out Int32 width)
        {
            Dictionary<String, String> grid = gridsFile.GetSectionContent(gridName);
            Int32 curId = 0;
            String curIdStr = curId.ToString();
            List<String> lines = new List<String>();
            while (grid.ContainsKey(curIdStr))
            {
                lines.Add(grid[curIdStr]);
                curId++;
                curIdStr = curId.ToString();
            }
            StringBuilder fullGrid = new StringBuilder();
            width = lines.Max(ln => ln.Length);
            foreach (String line in lines)
            {
                Int32 add = width - line.Length;
                fullGrid.Append(line);
                if (add > 0)
                    fullGrid.Append(new String('-', add));
            }
            return fullGrid.ToString();
        }

        private static Boolean isAlphabetChar(Char ch)
        {
            return ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'));
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
                    throw new ArgumentException("file size must be divisible by 4.", "fileData");
                Byte[] buffer = new Byte[4];
                for (Int32 i = 0; i < amount; ++i)
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
                    ms.WriteByte((byte)n64Cell.TemplateType);
                    ms.WriteByte(n64Cell.Icon);
                    ms.WriteByte((byte)pcCell.TemplateType);
                    ms.WriteByte(pcCell.Icon);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static CnCMap ConvertMap(CnCMap map, Dictionary<Int32, CnCMapCell> mapping, Byte? defaultHigh, Byte? defaultLow, Boolean toN64, out List<CnCMapCell> errorcells)
        {
            Byte highByte = defaultHigh.GetValueOrDefault(0xFF);
            Byte lowByte = defaultLow.GetValueOrDefault((Byte)(toN64 ? 0xFF : 0x00));
            CnCMap newmap = new CnCMap(map.GetAsBytes(), false);
            if (toN64)
            {
                CleanUpMapClearTerrain(newmap);
                // Prevents snow cells from being seen as as errors. They are a known anomaly that can be removed gracefully.
                RemoveSnow(newmap);
            }
            errorcells = new List<CnCMapCell>();
            for (Int32 i = 0; i < CnCMap.LENGTH_TD; ++i)
            {
                Int32 cellvalue = newmap[i].ValueTD;
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

        private static Dictionary<Int32, CnCMapCell[]> GetReverseMapping(Dictionary<Int32, CnCMapCell> mapping, out List<CnCMapCell> errorcells)
        {
            Dictionary<Int32, CnCMapCell[]> newmapping = new Dictionary<Int32, CnCMapCell[]>();
            errorcells = new List<CnCMapCell>();
            foreach (Int32 mapval in mapping.Keys)
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
        public static TerrainTypeEnh[] SimplifyMap(CnCMap mapData, Dictionary<Int32, TileInfo> tileInfo)
        {
            int emptyType = mapData.IsRaType ? 0xFFFF : 0xFF;
            TerrainTypeEnh[] simplifiedMap = new TerrainTypeEnh[mapData.Cells.Length];
            for (Int32 i = 0; i < mapData.Cells.Length; ++i)
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
            for (Int32 i = 0; i < CnCMap.LENGTH_TD; ++i)
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
            for (Int32 i = 0; i < CnCMap.LENGTH_TD; ++i)
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
