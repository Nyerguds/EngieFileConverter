using Nyerguds.Ini;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.ImageManipulation;
using System.Text.RegularExpressions;
using Nyerguds.Util;
using Nyerguds.CCTypes;
using CnC64FileConverter.Domain.Utils;

namespace CnC64FileConverter.Domain.ImageFile
{
    public class FileMapPc : N64FileType
    {
        protected static readonly Regex HEXREGEX = new Regex("^[0-9A-F]+h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX1 = new Regex("^D\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX2 = new Regex("^FORD[1-2]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX3 = new Regex("^BRIDGE[1-4]D?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex MAPREGEX = new Regex("^SC[GB]\\d{2}\\d*[EWX][A-E]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected static readonly Dictionary<Int32, TileInfo> TILEINFO = ReadTileInfo();
        protected static readonly Dictionary<Int32, CnCMapCell> DESERT_MAPPING = LoadMapping("th_desert.nms", global::CnC64FileConverter.Properties.Resources.th_desert);
        protected static readonly Dictionary<Int32, CnCMapCell> TEMPERATE_MAPPING = LoadMapping("th_temperate.nms", global::CnC64FileConverter.Properties.Resources.th_temperate);
        protected static readonly Dictionary<Int32, CnCMapCell> DESERT_MAPPING_REVERSED = LoadReverseMapping(DESERT_MAPPING);
        protected static readonly Dictionary<Int32, CnCMapCell> TEMPERATE_MAPPING_REVERSED = LoadReverseMapping(TEMPERATE_MAPPING);
        
        protected static Color[] paletteTemperate = new Color[]
        {
            Color.Black,                      //Unused = 0
            Color.FromArgb(0x35, 0x44, 0x35), //Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), //Water = 2
            Color.FromArgb(0x60, 0x60, 0x60), //Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), //Beach = 4
            Color.FromArgb(0x5E, 0x55, 0x44), //Road = 5
        };
        protected static Color[] paletteDesert = new Color[]
        {
            Color.Black,                      //Unused = 0
            Color.FromArgb(0x88, 0x5E, 0x46), //Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), //Water = 2
            Color.FromArgb(0x60, 0x60, 0x60), //Rock = 3
            Color.FromArgb(0xE3, 0xB5, 0x49), //Beach = 4
            Color.FromArgb(0xAB, 0x81, 0x55), //Road = 5
        };
        protected static Color[] paletteSnow = new Color[]
        {
            Color.Black,                      //Unused = 0
            Color.FromArgb(0xC8, 0xC8, 0xC8), //Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), //Water = 2
            Color.FromArgb(0x60, 0x60, 0x60), //Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), //Beach = 4
            Color.FromArgb(0x92, 0x8A, 0x80), //Road = 5
        };
        
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "PCMap"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "PC C&C map file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "bin" }; } }
        /// <summary>Is this file format treated as an image with a color palette?</summary>
        public override Boolean FileHasPalette { get { return false; } }
        public override Int32 Width { get { return 64; } }
        public override Int32 Height { get { return 64; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override N64FileType PreferredExportType { get { return new FileMapN64(); } }

        public Byte[] PCMapData { get; protected set; }
        public Byte[] N64MapData { get; protected set; }

        public FileMapPc() { }
        
        public override void LoadImage(Byte[] fileData)
        {
            PCMapData = fileData;
            Theater theater = (Theater)0xFF;
            N64MapData = IdentifyTheaterAndConvert(fileData, ref theater, true, null);
            loadedImage = ReadMapAsImage(fileData, theater);
        }

        public override void LoadImage(String filename)
        {
            loadedImage = ReadMapAsImage(filename, (Theater)0xFF);
        }

        public override Int32 GetBitsPerColor()
        {
            return 0;
        }

        public override Color[] GetColors()
        {
            return null;
        }
        
        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            if (fileToSave is FileMapPc)
                File.WriteAllBytes(savePath, ((FileMapPc)fileToSave).PCMapData);
            else
                throw new NotSupportedException();
        }

        protected Bitmap ReadMapAsImage(String filename, Theater theater)
        {
            theater = GetTheaterFromIni(filename, theater);
            PCMapData = File.ReadAllBytes(filename);
            if (PCMapData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            N64MapData = IdentifyTheaterAndConvert(PCMapData, ref theater, false, filename);
            return ReadMapAsImage(PCMapData, theater);
        }

        protected Theater GetTheaterFromIni(String filename, Theater defaultTheater)
        {
            Theater theater = defaultTheater;
            try
            {
                String inipath = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".ini";
                if (File.Exists(inipath))
                {
                    IniFile inifile = new IniFile(inipath);
                    String th = inifile.GetStringValue("Map", "Theater", null);
                    theater = GeneralUtils.TryParseEnum(th, Theater.Desert, true);
                }
            }
            catch { /* ignore */ }
            return theater;
        }

        protected Bitmap ReadMapAsImage(Byte[] fileData, Theater theater)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            Byte[] imageData = new Byte[64 * 64];

            TerrainType[] simplifiedMap = SimplifyMap(fileData);
            Color[] palette;
            switch(theater)
            {
                case Theater.Desert:
                    palette = paletteDesert;
                    break;
                //case Theater.Jungle:
                //    palette = paletteTemperate;
                //    break;
                case Theater.Temperate:
                default:
                    palette = paletteTemperate;
                    break;
                case Theater.Winter:
                    palette = paletteTemperate;
                    break;
                case Theater.Snow:
                    palette = paletteSnow;
                    break;
            }

            for (Int32 i = 0; i < simplifiedMap.Length; i ++)
            {
                imageData[i] = (Byte)simplifiedMap[i];
            }
            return ImageUtils.BuildImage(imageData, 64, 64, 64, PixelFormat.Format8bppIndexed, palette, Color.Black);
        }

        protected TerrainType[] SimplifyMap(Byte[] mapData)
        {
            TerrainType[] simplifiedMap = new TerrainType[64*64];
            for (Int32 i = 0; i < simplifiedMap.Length; i ++)
            {
                Byte hiByte = mapData[i * 2];
                Byte loByte = mapData[i * 2 + 1];
                TerrainType terrain = TerrainType.Clear;
                if (hiByte == 0xFF)
                {
                    if (loByte == 0xFF)
                        throw new FileTypeLoadException("Bad format for clear map terrain!");   
                }
                else
                {
                    TileInfo info;
                    if (TILEINFO.TryGetValue(hiByte, out info))
                    {
                        if (info.TypedCells.Length > loByte)
                            terrain = info.TypedCells[loByte];
                        else
                            terrain = info.PrimaryType;
                    }
                    else throw new FileTypeLoadException("Unknown terrain data encountered.");
                }             
                simplifiedMap[i] = terrain;
            }
            return simplifiedMap;
        }

        protected static List<Int32> GetCellsList(String inidata, Char separator)
        {
            List<Int32> secCellsInt = new List<Int32>();
            if (!String.IsNullOrEmpty(inidata) && !"none".Equals(inidata.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                String[] cells = inidata.Split(separator);
                foreach (String cell in cells)
                {
                    String trimmedCell = cell.Trim();
                    Boolean isHex = HEXREGEX.IsMatch(trimmedCell);
                    if (isHex)
                    {
                        trimmedCell = trimmedCell.Substring(0, trimmedCell.Length - 1);
                        secCellsInt.Add(Int32.Parse(trimmedCell, NumberStyles.HexNumber));
                        continue;
                    }
                    if (GeneralUtils.IsNumeric(trimmedCell))
                        secCellsInt.Add(Int32.Parse(trimmedCell));
                }
            }
            return secCellsInt;
        }

        protected Byte[] IdentifyTheaterAndConvert(Byte[] fileData, ref Theater theater, Boolean toPC, String sourceFile)
        {;
            Dictionary<Int32, CnCMapCell> mappingDes = toPC ? DESERT_MAPPING : DESERT_MAPPING_REVERSED;
            Dictionary<Int32, CnCMapCell> mappingTem = toPC ? TEMPERATE_MAPPING : TEMPERATE_MAPPING_REVERSED;

            if (theater != (Theater)0xFF)
            {
                Dictionary<Int32, CnCMapCell> mapping = null;
                switch (theater)
                {
                    case Theater.Desert:
                        mapping = mappingDes;
                        break;
                    case Theater.Temperate:
                    case Theater.Winter:
                    case Theater.Snow:
                        mapping = mappingTem;
                        break;
                }
                if (mapping != null)
                {
                    List<CnCMapCell> errorcells;
                    fileData = ConvertMap(fileData, mapping, toPC, out errorcells);
                }
                else
                    theater = (Theater)0xFF;
            }
            if (theater == (Theater)0xFF)
            {
                List<CnCMapCell> errorcellsTemperate;
                Byte[] dataTemperate = ConvertMap(fileData, mappingTem, toPC, out errorcellsTemperate);
                List<CnCMapCell> errorcellsDesert;
                Byte[] dataDesert = ConvertMap(fileData, mappingDes, toPC, out errorcellsDesert);
                // Technically maps have more chance of being desert when they're Nod maps, so if the number of errors
                // is the same on each, and a filename is given, check the SC[G/B]##EA format of the filename.
                if (errorcellsTemperate.Count > errorcellsDesert.Count
                    || (errorcellsTemperate.Count == errorcellsDesert.Count && sourceFile != null && MAPREGEX.IsMatch(sourceFile = Path.GetFileNameWithoutExtension(sourceFile)) && sourceFile[2] == 'B'))
                {
                    fileData = dataDesert;
                    theater = Theater.Desert;
                }
                else
                {
                    // if sourceFile += null, use temperate as fallback if the amount of errors is equal
                    fileData = dataTemperate;
                    theater = Theater.Temperate;
                }
            }
            return fileData;
        }

        protected Byte[] ConvertMap(Byte[] mapData, Dictionary<Int32, CnCMapCell> mapping, Boolean toPC, out List<CnCMapCell> errorcells)
        {
            CnCMap map = new CnCMap(mapData);
            map = MapConversion.ConvertMap(map, mapping, null, null, !toPC, out errorcells);
            return map.GetAsBytes();
        }

        protected static Dictionary<Int32, CnCMapCell> LoadMapping(String filename, Byte[] internalFallback)
        {
            String[] errors;
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), filename);
            Byte[] mappingBytes;
            if (File.Exists(file))
                mappingBytes = File.ReadAllBytes(file);
            else
                mappingBytes = internalFallback;
            return MapConversion.LoadMapping(mappingBytes, out errors);
        }

        protected static Dictionary<Int32, CnCMapCell> LoadReverseMapping(Dictionary<Int32, CnCMapCell> mapping)
        {
            Dictionary<Int32, CnCMapCell> newmapping = new Dictionary<Int32, CnCMapCell>();
            List<CnCMapCell> errorcells;
            Dictionary<Int32, CnCMapCell[]> mapping2 = MapConversion.GetReverseMapping(mapping, out errorcells);
            foreach (Int32 val in mapping2.Keys)
                newmapping.Add(val, mapping2[val][0]);
            return newmapping;
        }
        
        protected static Dictionary<Int32, TileInfo> ReadTileInfo()
        {
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2.ini");
            String tilesetsData2;
            if (File.Exists(file))
                tilesetsData2 = File.ReadAllText(file);
            else
                tilesetsData2 = global::CnC64FileConverter.Properties.Resources.tilesets2;

            IniFile tilesetsFile2 = new IniFile(null, tilesetsData2, true, IniFile.ENCODING_DOS_US, true);
            Dictionary<Int32, TileInfo> tileInfo2 = new Dictionary<Int32, TileInfo>();
            //tilesets2.ini - new loading code
            for (Int32 currentId = 0; currentId < 0xFF; currentId++)
            {
                String sectionName = tilesetsFile2.GetStringValue("TileSets", currentId.ToString(), null);
                if (sectionName == null)
                    continue;
                TileInfo info = new TileInfo();
                info.TileName = sectionName;
                info.Width = tilesetsFile2.GetIntValue(sectionName, "X", 1);
                info.Height = tilesetsFile2.GetIntValue(sectionName, "Y", 1);
                info.PrimaryType = GeneralUtils.TryParseEnum(tilesetsFile2.GetStringValue(sectionName, "PrimaryType", null), TerrainType.Clear, true);
                Char[] types = new Char[info.Width * info.Height];
                for (Int32 y = 0; y < info.Height; y++)
                {
                    Int32 offset = y * info.Width;
                    String typechars = tilesetsFile2.GetStringValue(sectionName, "Terrain" + y, String.Empty);
                    Int32 len = typechars.Length;
                    for (Int32 x = 0; x < info.Width; x++)
                    {
                        types[y * info.Width + x] = x >= len ? '?' : typechars[x]; ;
                    }
                }
                TerrainType[] typedCells = new TerrainType[types.Length];
                for (Int32 i = 0; i < types.Length; i++)
                {
                    switch (types[i])
                    {
                        case '_':
                            typedCells[i] = TerrainType.Unused;
                            break;
                        case 'C':
                            typedCells[i] = TerrainType.Clear;
                            break;
                        case 'W':
                            typedCells[i] = TerrainType.Water;
                            break;
                        case 'I':
                            typedCells[i] = TerrainType.Rock;
                            break;
                        case 'B':
                            typedCells[i] = TerrainType.Beach;
                            break;
                        case 'R':
                            typedCells[i] = TerrainType.Road;
                            break;
                        default:
                            if (Regex.IsMatch(types[i].ToString(),"^[a-zA-Z]$"))
                                typedCells[i] = info.PrimaryType;
                            else
                                typedCells[i] = TerrainType.Unused;
                            break;
                    }
                }
                info.TypedCells = typedCells;
                tileInfo2.Add(currentId, info);
            }
            return tileInfo2;
        }

        /*/
        protected static Dictionary<Int32, TileInfo> ReadTileInfoOld()
        {
            //tilesets.ini - old loading code
            String file = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets.ini");
            String tilesetsData;
            if (File.Exists(file))
                tilesetsData = File.ReadAllText(file);
            else
                tilesetsData = global::CnC64FileConverter.Properties.Resources.tilesets;
            IniFile tilesetsFile = new IniFile(null, tilesetsData, true, IniFile.ENCODING_DOS_US, true);
            Dictionary<Int32, TileInfo> tileInfo = new Dictionary<Int32, TileInfo>();
            for (Int32 currentId = 0; currentId < 0xFF; currentId++)
            {
                String sectionName = tilesetsFile.GetStringValue("TileSets", currentId.ToString(), null);
                if (sectionName == null)
                    continue;
                TileInfo info = new TileInfo();
                info.TileName = sectionName;
                info.SecondaryTypeCells = GetCellsList(tilesetsFile.GetStringValue(sectionName, "SecondaryTypeCells", null), ',');
                info.SecondaryType = GeneralUtils.TryParseEnum(tilesetsFile.GetStringValue(sectionName, "SecondaryType", null), TerrainType.Clear, true);
                info.Width = tilesetsFile.GetIntValue(sectionName, "X", 1);
                info.Height = tilesetsFile.GetIntValue(sectionName, "Y", 1);
                info.PrimaryType = GeneralUtils.TryParseEnum(tilesetsFile.GetStringValue(sectionName, "PrimaryType", null), TerrainType.Clear, true);
                info.NameID = tilesetsFile.GetIntValue(sectionName, "NameID", 0);
                if (IsRoad(info))
                {
                    if (info.PrimaryType == TerrainType.Clear)
                        info.PrimaryType = TerrainType.Road;
                    if (info.SecondaryType == TerrainType.Clear)
                        info.SecondaryType = TerrainType.Road;
                }
                tileInfo.Add(currentId, info);
            }
            return tileInfo;
        }
        
        protected static void WriteTileInfo()
        {
            //tilesets2.ini - original saving code. To convert frmo tileinfo to tileinfo2
            String saveFile = Path.Combine(GeneralUtils.GetApplicationPath(), "tilesets2.ini");
            IniFile tileSaveFile = new IniFile(saveFile, true, IniFile.ENCODING_DOS_US, true);
            //Unused = 0
            //Clear = 1
            //Water = 2
            //Rock = 3 ([I]mpassable)
            //Beach = 4
            //Road = 5
            String terrainTypes = "_CWIBR";
            for (Int32 currentId = 0; currentId < 0xFF; currentId++)
            {
                TileInfo info;
                if (!TILEINFO.TryGetValue(currentId, out info))
                    continue;
                tileSaveFile.SetStringValue("TileSets", currentId.ToString(), info.TileName);
                tileSaveFile.SetIntValue(info.TileName, "X", info.Width);
                tileSaveFile.SetIntValue(info.TileName, "Y", info.Height);

                if (info.TileName == "S13")
                {
                    info.TileName += String.Empty;
                }

                String[] tileTerrainTypes = new String[info.Height];
                for (Int32 y = 0; y < info.Height; y++)
                {
                    Char[] thisRow = new Char[info.Width];
                    for (Int32 x = 0; x < info.Width; x++)
                    {
                        Int32 cell = y * info.Width + x;
                        if (info.SecondaryTypeCells.Contains(cell))
                            thisRow[x] = terrainTypes[(Int32)info.SecondaryType];
                        else
                            thisRow[x] = terrainTypes[(Int32)info.PrimaryType];
                    }
                    tileSaveFile.SetStringValue(info.TileName, "Terrain" + y, new String(thisRow));
                }
            }
            tileSaveFile.WriteIni();
        }

        private static Boolean IsRoad(TileInfo info)
        {
            if (ROADREGEX1.IsMatch(info.TileName))
                return true;
            if (ROADREGEX2.IsMatch(info.TileName))
                return true;
            if (ROADREGEX3.IsMatch(info.TileName))
                return true;
            return false;
        }
        //*/
    }

    public enum Theater
    {
        Desert,
        Jungle,
        Temperate,
        Winter,
        Snow
    }

}
