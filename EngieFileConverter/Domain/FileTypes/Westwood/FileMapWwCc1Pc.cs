using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileMapWwCc1Pc : SupportedFileType
    {

        //protected static readonly Regex HEXREGEX = new Regex("^[0-9A-F]+h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //protected static readonly Regex ROADREGEX1 = new Regex("^D\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //protected static readonly Regex ROADREGEX2 = new Regex("^FORD[1-2]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //protected static readonly Regex ROADREGEX3 = new Regex("^BRIDGE[1-4]D?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex MAPREGEX = new Regex("^SC[GB]\\d{2}\\d*[EWX][A-EL]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex CIVREGEX = new Regex("^V\\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        protected static readonly Regex STRREGEX = new Regex("^([A-Za-z0-9]+),([A-Za-z0-9]+),(\\d+),(\\d+),(\\d+),([^\\s]+)$");
        protected static readonly Regex TERREGEX = new Regex("^([A-Za-z0-9]+),([^\\s]+)$");
        protected static readonly Regex INFREGEX = new Regex("^([A-Za-z0-9]+),([A-Za-z0-9]+),(\\d+),(\\d+),(\\d+),([a-zA-Z0-9]+),(\\d+),([^\\s]+)$");
        protected static readonly Regex VEHREGEX = new Regex("^([A-Za-z0-9]+),([A-Za-z0-9]+),(\\d+),(\\d+),(\\d+),([a-zA-Z0-9]+),([^\\s]+)$");

        public static Color[] PaletteTemperate = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0x35, 0x44, 0x35), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), // Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), // Beach = 4
            Color.FromArgb(0x5E, 0x55, 0x44), // Road = 5
            Color.FromArgb(0x6D, 0xA4, 0xDB), // River = 6
            Color.FromArgb(0x50, 0x50, 0x50), // CliffFace = 7
            Color.FromArgb(0x40, 0x40, 0x40), // CliffPlateau = 8
            Color.FromArgb(0x4D, 0x57, 0x4D), // Smudge = 9
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Snow = A
            Color.FromArgb(0x65, 0x59, 0x55), // Bibs = B
            Color.FromArgb(0x48, 0x79, 0x44), // Trees = C
            Color.FromArgb(0x70, 0x70, 0x70), // Rocks = D
            Color.FromArgb(0xE6, 0x95, 0x30), // Blossom Trees = E
            Color.FromArgb(0x58, 0x58, 0x58), // CliffPlateauWater= F
        };

        public static Color[] PaletteDesert = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0x88, 0x5E, 0x46), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), // Rock = 3
            Color.FromArgb(0xE3, 0xB5, 0x49), // Beach = 4
            Color.FromArgb(0xAB, 0x81, 0x55), // Road = 5
            Color.FromArgb(0x6D, 0xA4, 0xDB), // River = 6
            Color.FromArgb(0x68, 0x68, 0x68), // CliffFace = 7
            Color.FromArgb(0x50, 0x50, 0x50), // CliffPlateau = 8
            Color.FromArgb(0x5E, 0x48, 0x3E), // Smudge = 9
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Snow = A
            Color.FromArgb(0xA3, 0x78, 0x53), // Bibs = B
            Color.FromArgb(0x71, 0x69, 0x48), // Trees = C
            Color.FromArgb(0xAE, 0x79, 0x69), // Rocks = D
            Color.FromArgb(0xE6, 0x95, 0x30), // Blossom Trees = E
            Color.FromArgb(0x5C, 0x5C, 0x5C), // CliffPlateauWater= F
        };

        public static Color[] PaletteSnow = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x50, 0x50, 0x50), // Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), // Beach = 4
            Color.FromArgb(0x92, 0x8A, 0x80), // Road = 5
            Color.FromArgb(0x6D, 0xA4, 0xDB), // River = 6
            Color.FromArgb(0x68, 0x68, 0x68), // CliffFace = 7
            Color.FromArgb(0x50, 0x50, 0x50), // CliffPlateau = 8
            Color.FromArgb(0x9E, 0x9E, 0x9E), // Smudge = 9
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Snow = A
            Color.FromArgb(0x8C, 0x84, 0x7D), // Bibs = B
            Color.FromArgb(0x48, 0x79, 0x44), // Trees = C
            Color.FromArgb(0x70, 0x70, 0x70), // Rocks = D
            Color.FromArgb(0xE6, 0x95, 0x30), // Blossom Trees = E
            Color.FromArgb(0x5C, 0x5C, 0x5C), // CliffPlateauWater= F
        };

        public static Color[] PaletteInterior = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0x35, 0x35, 0x35), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), // Rock = 3
            Color.FromArgb(0x45, 0x39, 0x39), // Beach = 4
            Color.FromArgb(0xAE, 0xAE, 0xAE), // Road = 5
            Color.FromArgb(0x6D, 0xA4, 0xDB), // River = 6
            Color.FromArgb(0x68, 0x68, 0x68), // CliffFace = 7
            Color.FromArgb(0x50, 0x50, 0x50), // CliffPlateau = 8
            Color.FromArgb(0x4D, 0x00, 0x00), // Smudge = 9
            Color.FromArgb(0xE7, 0x92, 0x28), // Snow = A
            Color.FromArgb(0x65, 0x59, 0x55), // Bibs = B
            Color.FromArgb(0x48, 0x79, 0x44), // Trees = C
            Color.FromArgb(0x70, 0x70, 0x70), // Rocks = D
            Color.FromArgb(0xE6, 0x95, 0x30), // Blossom Trees = E
            Color.FromArgb(0x5C, 0x5C, 0x5C), // CliffPlateauWater= F
        };

        protected static Color[] PaletteGoodGuy = new Color[] { Color.FromArgb(0xf7, 0xd7, 0x79), Color.FromArgb(0x8a, 0x71, 0x38) };
        protected static Color[] PaletteBadGuy = new Color[] { Color.FromArgb(0xf7, 0x00, 0x00), Color.FromArgb(0x9a, 0x30, 0x24) };
        protected static Color[] PaletteNeutral = new Color[] { Color.FromArgb(0xa6, 0xa6, 0xbe), Color.FromArgb(0x49, 0x49, 0x5d) };
        protected static Color[] PaletteSpecial = new Color[] { Color.FromArgb(0xf7, 0x00, 0x00), Color.FromArgb(0x8a, 0x71, 0x38) };
        protected static Color[] PaletteMulti1 = new Color[] { Color.FromArgb(0x00, 0xaa, 0xaa), Color.FromArgb(0x00, 0x71, 0x71) };
        protected static Color[] PaletteMulti2 = new Color[] { Color.FromArgb(0xef, 0xae, 0x49), Color.FromArgb(0xd7, 0x7d, 0x10) };
        protected static Color[] PaletteMulti3 = new Color[] { Color.FromArgb(0x8e, 0xcb, 0x08), Color.FromArgb(0x3c, 0x9a, 0x38) };
        protected static Color[] PaletteMulti4 = new Color[] { Color.FromArgb(0xc3, 0xc3, 0xd3), Color.FromArgb(0x86, 0x86, 0x9e) };
        protected static Color[] PaletteMulti5 = new Color[] { Color.FromArgb(0xff, 0xff, 0x55), Color.FromArgb(0xae, 0xb2, 0x20) };
        protected static Color[] PaletteMulti6 = new Color[] { Color.FromArgb(0xf7, 0x00, 0x00), Color.FromArgb(0x9a, 0x30, 0x24) };

        protected static Color[] PaletteOverlay = new Color[]
        {
            Color.FromArgb(0xAA, 0xAA, 0xAA), // 0 = CONC
            Color.FromArgb(0xA1, 0xA1, 0x5D), // 1 = SBAG
            Color.FromArgb(0x85, 0x85, 0x9d), // 2 = CYCL
            Color.FromArgb(0xBE, 0xBE, 0xBE), // 3 = BRIK
            Color.FromArgb(0xA5, 0xA5, 0xBE), // 4 = BARB
            Color.FromArgb(0x65, 0x50, 0x38), // 5 = WOOD
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 6 = TI1
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 7 = TI2
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 8 = TI3
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 9 = TI4
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 10 = TI5
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 11 = TI6
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 12 = TI7
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 13 = TI8
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 14 = TI9
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 15 = TI10
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 16 = TI11
            Color.FromArgb(0xA6, 0xE3, 0x1C), // 17 = TI12
            Color.FromArgb(0x55, 0x55, 0x55), // 18 = ROAD
            Color.Black,                      // 19 = SQUISH
            Color.Black, /* PLACEHOLDER */    // 20 = V12
            Color.Black, /* PLACEHOLDER */    // 21 = V13
            Color.Black, /* PLACEHOLDER */    // 22 = V14
            Color.Black, /* PLACEHOLDER */    // 23 = V15
            Color.Black, /* PLACEHOLDER */    // 24 = V16
            Color.Black, /* PLACEHOLDER */    // 25 = V17
            Color.Black, /* PLACEHOLDER */    // 26 = V18
            Color.FromArgb(0xB2, 0x95, 0x50), // 27 = FPLS
            Color.FromArgb(0x6d, 0x65, 0x38), // 28 = WCRATE
            Color.FromArgb(0x65, 0x65, 0x7D), // 29 = SCRATE
        };

        public override String IdCode { get { return "WwCc1MapPC"; } }
        public override FileClass FileClass { get { return FileClass.CcMap | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.CcMap; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C Map"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String LongTypeName { get { return "C&C map file - PC"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "bin" }; } }
        public override Int32 Width { get { return 64; } }
        public override Int32 Height { get { return 64; } }
        public override Int32 BitsPerPixel { get { return 8; } }

        public Byte[] PCMapData { get; protected set; }
        public Byte[] N64MapData { get; protected set; }

        public CnCMap Map { get { return new CnCMap(this.PCMapData, false); } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, true);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFile(fileData, filename, null, null, true);
        }

        public void LoadFile(Byte[] fileData, Boolean isPc)
        {
            this.PCMapData = fileData;
            Theater theater = (Theater)0xFF;
            Int32 errToN64;
            Byte[] mapDataToN64 = this.IdentifyTheaterAndConvert(fileData, ref theater, false, null, out errToN64);
            Int32 errToPc;
            Byte[] mapDataToPC = this.IdentifyTheaterAndConvert(fileData, ref theater, false, null, out errToPc);
            if ((isPc && errToN64 > errToPc) || (!isPc && errToPc > errToN64))
                throw new FileTypeLoadException("Not a " + (isPc ? "PC" : "N64") + " C&C Map file!");
            this.PCMapData = isPc ? fileData : mapDataToPC;
            this.N64MapData = isPc ? mapDataToN64 : fileData;
            this.m_LoadedImage = this.ReadMapAsImage(fileData, theater, Rectangle.Empty, null);
        }

        public void LoadFile(Byte[] fileData, String filename, Byte[] iniContents, String iniFile, Boolean isPc)
        {
            IniInfo iniInfo = this.GetIniInfo(iniFile ?? filename, (Theater)0xFF, iniContents);
            Theater theater = iniInfo == null ? (Theater)0xFF : iniInfo.Theater;
            Rectangle usableArea = iniInfo == null ? Rectangle.Empty : new Rectangle(iniInfo.X, iniInfo.Y, iniInfo.Width, iniInfo.Height);
            this.DetectDataTypeAndConvert(fileData, theater, filename, usableArea, isPc);
            this.m_LoadedImage = this.ReadMapAsImage(this.PCMapData, theater, usableArea, iniInfo == null ? null : iniInfo.Population);
            if (iniInfo != null)
            {
                this.ExtraInfo = "Ini info loaded from \"" + Path.GetFileName(iniInfo.File) + "\""
                    + Environment.NewLine + "Map width: " + iniInfo.Width
                    + Environment.NewLine + "Map height: " + iniInfo.Height
                    + Environment.NewLine + "Map X: " + iniInfo.X
                    + Environment.NewLine + "Map Y: " + iniInfo.Y
                    + (iniInfo.Player == null ? String.Empty : (Environment.NewLine + "Player: " + iniInfo.Player));

                if (!String.IsNullOrEmpty(iniInfo.Name))
                    this.ExtraInfo += Environment.NewLine + "Mission name: " + iniInfo.Name;
            }
            this.SetFileNames(filename);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            FileMapWwCc1Pc cc1PcMap = fileToSave as FileMapWwCc1Pc;
            if (cc1PcMap == null)
                throw new ArgumentException("Cannot save the given file as C&C map.", "fileToSave");
            return ArrayUtils.CloneArray(cc1PcMap.PCMapData);
        }

        protected void DetectDataTypeAndConvert(Byte[] fileData, Theater theater, String sourceFile, Rectangle usableArea, Boolean isPc)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            Int32 errCells;
            Byte[] convertedData = this.IdentifyTheaterAndConvert(fileData, ref theater, !isPc, sourceFile, out errCells);
            if (errCells > 0)
            {
                Int32 errCells2;
                this.IdentifyTheaterAndConvert(fileData, ref theater, isPc, sourceFile, out errCells2);
                if (errCells > errCells2)
                    throw new FileTypeLoadException("Not a " + (isPc ? "PC" : "N64") + " C&C Map file!");
            }
            this.PCMapData = isPc ? fileData : convertedData;
            this.N64MapData = isPc ? convertedData : fileData;
        }

        protected IniInfo GetIniInfo(String filename, Theater defaultTheater, Byte[] iniData)
        {
            IniInfo info = new IniInfo();
            info.Theater = defaultTheater;
            String inipath = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".ini";
            IniFile inifile = null;
            if (iniData != null)
            {
                String iniStr = IniFile.ENCODING_DOS_US.GetString(iniData);
                inifile = new IniFile(inipath, iniStr, IniFile.ENCODING_DOS_US);
                inipath = filename;
            }
            else if (File.Exists(inipath))
            {
                inifile = new IniFile(inipath, IniFile.ENCODING_DOS_US);
            }
            if (inifile == null || inifile.ContainsSection("MapPack") || (!inifile.ContainsSection("Basic") && !inifile.ContainsSection("Map")))
                return null;
            String th = inifile.GetStringValue("Map", "Theater", null);
            info.Theater = GeneralUtils.TryParseEnum(th, defaultTheater, true);
            info.Name = inifile.GetStringValue("Basic", "Name", null);
            info.Width = inifile.GetIntValue("Map", "Width", 64);
            info.Height = inifile.GetIntValue("Map", "Height", 64);
            info.X = inifile.GetIntValue("Map", "X", 0);
            info.Y = inifile.GetIntValue("Map", "Y", 0);
            info.Player = inifile.GetStringValue("Basic", "Player", null);
            info.Population = this.GetPopulation(inifile);
            info.File = inipath;
            return info;
        }

        private Dictionary<Int32, Int32> GetPopulation(IniFile inifile)
        {
            Dictionary<Int32, Int32> cells = new Dictionary<Int32, Int32>();
            // Fetch structures
            Dictionary<String, String> str = inifile.GetSectionContent("Structures");
            List<MapStructure> structures = new List<MapStructure>();
            foreach (KeyValuePair<String, String> strPair in str)
            {
                Match strMatch = STRREGEX.Match(strPair.Value);
                if (!strMatch.Success)
                    continue;
                House strOwner = GeneralUtils.TryParseEnum(strMatch.Groups[1].Value, House.GoodGuy, true);
                String strName = strMatch.Groups[2].Value;
                Int32 strCell = Int32.Parse(strMatch.Groups[4].Value);
                StructInfo strInfo;
                if (!MapConversion.STRUCTUREINFO.TryGetValue(strName, out strInfo))
                    continue;
                structures.Add(new MapStructure(strInfo, strOwner, strCell));
            }
            // Paint bibs
            foreach (MapStructure mapStruct in structures)
            {
                StructInfo strInfo = mapStruct.StructType;
                if (!strInfo.HasBib || strInfo.Width < 2 || strInfo.Width > 4)
                    continue;
                Int32 strCell = mapStruct.Cell + (strInfo.Height - 1) * 64;
                Int32 bibsize = 2 * strInfo.Width;
                Int32 strWidth = strInfo.Width;
                for (Int32 i = 0; i < bibsize; ++i)
                {
                    cells[strCell] = (int)TerrainTypeEnh.Bibs;
                    strCell++;
                    if ((i + 1) % strWidth == 0)
                        strCell += 64 - strWidth;
                }
            }
            // Paint terrain
            Dictionary<String, String> ter = inifile.GetSectionContent("Terrain");
            foreach (KeyValuePair<String, String> terPair in ter)
            {
                Int32 terCell;
                if (!Int32.TryParse(terPair.Key, out terCell))
                    continue;
                Match terMatch = TERREGEX.Match(terPair.Value);
                if (!terMatch.Success)
                    continue;
                String terType = terMatch.Groups[1].Value;

                StructInfo strInfo;
                if (!MapConversion.TERRAININFO.TryGetValue(terType, out strInfo))
                    continue;
                TerrainTypeEnh terColor;
                if (terType.StartsWith("SPLIT", StringComparison.InvariantCultureIgnoreCase))
                    terColor = TerrainTypeEnh.BlossomTrees;
                else if (terType.StartsWith("ROCK", StringComparison.InvariantCultureIgnoreCase))
                    terColor = TerrainTypeEnh.Rocks;
                else
                    terColor = TerrainTypeEnh.Trees;
                Int32 strWidth = strInfo.Width;
                for (Int32 i = 0; i < strInfo.OccupyList.Length; ++i)
                {
                    if (strInfo.OccupyList[i])
                        cells[terCell] = (int)terColor;
                    terCell++;
                    if ((i + 1) % strWidth == 0)
                        terCell += 64 - strWidth;
                }
            }
            // Paint overlay
            Dictionary<String, String> ovl = inifile.GetSectionContent("Overlay");
            foreach (KeyValuePair<String, String> ovlPair in ovl)
            {
                Int32 ovlCell;
                if (!Int32.TryParse(ovlPair.Key, out ovlCell))
                    continue;
                String ovlType = ovlPair.Value;
                if (CIVREGEX.IsMatch(ovlType))
                    cells[ovlCell] = this.GetHouseColorIndex(House.Neutral, 1);
                else
                {
                    // using v12 as default because it is an already-handled case.
                    OverlayTd overlay = GeneralUtils.TryParseEnum(ovlType, OverlayTd.V12, true);
                    if (overlay != OverlayTd.V12)
                        cells[ovlCell] = this.GetOverlayColorIndex(overlay);
                }
            }
            // Paint structures
            foreach (MapStructure mapStruct in structures)
            {
                StructInfo strInfo = mapStruct.StructType;
                Int32 strColor = this.GetHouseColorIndex(mapStruct.Owner, 1);
                Int32 strCell = mapStruct.Cell;
                Int32 strWidth = strInfo.Width;
                for (Int32 i = 0; i < strInfo.OccupyList.Length; ++i)
                {
                    if (strInfo.OccupyList[i])
                        cells[strCell] = strColor;
                    strCell++;
                    if ((i + 1) % strWidth == 0)
                        strCell += 64 - strWidth;
                }
            }
            // Paint infantry
            Dictionary<String, String> inf = inifile.GetSectionContent("Infantry");
            foreach (KeyValuePair<String, String> infPair in inf)
            {
                Match infMatch = INFREGEX.Match(infPair.Value);
                if (infMatch.Success)
                {
                    House infOwner = GeneralUtils.TryParseEnum(infMatch.Groups[1].Value, House.GoodGuy, true);
                    Int32 infColor = this.GetHouseColorIndex(infOwner, 0);
                    Int32 infCell = Int32.Parse(infMatch.Groups[4].Value);
                    cells[infCell] = infColor;
                }
            }
            // Paint units
            Dictionary<String, String> veh = inifile.GetSectionContent("Units");
            foreach (KeyValuePair<String, String> vehPair in veh)
            {
                Match vehMatch = VEHREGEX.Match(vehPair.Value);
                if (vehMatch.Success)
                {
                    House vehOwner = GeneralUtils.TryParseEnum(vehMatch.Groups[1].Value, House.GoodGuy, true);
                    Int32 vehColor = this.GetHouseColorIndex(vehOwner, 0);
                    Int32 vehCell = Int32.Parse(vehMatch.Groups[4].Value);
                    cells[vehCell] = vehColor;
                }
            }
            return cells;
        }

        protected static Color[] GetHousePalette(House owner)
        {
            switch (owner)
            {
                case House.GoodGuy:
                default:
                    return PaletteGoodGuy;
                case House.BadGuy:
                    return PaletteBadGuy;
                case House.Neutral:
                    return PaletteNeutral;
                case House.Special:
                    return PaletteSpecial;
                case House.Multi1:
                    return PaletteMulti1;
                case House.Multi2:
                    return PaletteMulti2;
                case House.Multi3:
                    return PaletteMulti3;
                case House.Multi4:
                    return PaletteMulti4;
                case House.Multi5:
                    return PaletteMulti5;
                case House.Multi6:
                    return PaletteMulti6;
            }
        }

        protected Color GetHouseColor(House owner, Int32 index)
        {
            return GetHousePalette(owner)[index];
        }

        protected Int32 GetHouseColorIndex(House owner, Int32 index)
        {
            return 0x40 + ((Int32)owner * 2) + index;
        }

        protected Int32 GetOverlayColorIndex(OverlayTd type)
        {
            Int32 lastHouse = (Int32)Enum.GetValues(typeof(House)).Cast<House>().Max();
            return 0x40 + (lastHouse + 1) * 2 + (Int32)type;
        }

        /// <summary>
        /// Converts a map to image.
        /// </summary>
        /// <param name="fileData">File data for a PC C&amp;C type map.</param>
        /// <param name="theater">Theater of the map.</param>
        /// <param name="usableArea">USable area of the map.</param>
        /// <param name="addedPixels">Added data to populate the map.</param>
        /// <returns></returns>
        protected Bitmap ReadMapAsImage(Byte[] fileData, Theater theater, Rectangle usableArea, Dictionary<Int32, Int32> addedPixels)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            TerrainTypeEnh[] simplifiedMap;
            try
            {
                simplifiedMap = MapConversion.SimplifyMap(new CnCMap(fileData, false), MapConversion.TILEINFO_TD);
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, false), ex);
            }
            Color[] palette = GetTheaterPaletteFull(theater);
            Byte[] imageData = new Byte[CnCMap.LENGTH_TD];
            if (usableArea == Rectangle.Empty)
            {
                for (Int32 i = 0; i < simplifiedMap.Length; ++i)
                    imageData[i] = (Byte)simplifiedMap[i];
            }
            else
            {
                // paint blue-tinted outside border
                if (usableArea != Rectangle.Empty)
                {
                    for (Int32 i = 0; i < simplifiedMap.Length; ++i)
                        imageData[i] = (Byte)(simplifiedMap[i] + 0x20);
                }
                // paint normal-colored area
                Int32 minY = usableArea != Rectangle.Empty ? usableArea.Y : 0;
                Int32 maxY = usableArea != Rectangle.Empty ? usableArea.Y + usableArea.Height : 64;
                Int32 minX = usableArea != Rectangle.Empty ? usableArea.X : 0;
                Int32 maxX = usableArea != Rectangle.Empty ? usableArea.X + usableArea.Width : 64;
                for (Int32 y = minY; y < maxY; ++y)
                {
                    for (Int32 x = minX; x < maxX; ++x)
                    {
                        Int32 cell = (y << 6) | x;
                        imageData[cell] = (Byte)simplifiedMap[cell];
                    }
                }
            }
            if (addedPixels != null)
            {
                Int32[] cells = addedPixels.Keys.ToArray();
                for (Int32 i = 0; i < cells.Length; ++i)
                {
                    Int32 cell = cells[i];
                    imageData[cell] = (Byte)addedPixels[cell];
                }
            }
            return ImageUtils.BuildImage(imageData, 64, 64, 64, PixelFormat.Format8bppIndexed, palette, Color.Black);
        }
        public static Color[] GetTheaterPalette(Theater theater)
        {
            return GetTheaterPalette(theater, null);
        }

        public static Color[] GetTheaterPalette(Theater theater, Dictionary<byte, Color> overrides)
        {
            Color[] colBasic;
            switch (theater)
            {
                case Theater.Desert:
                    colBasic = PaletteDesert;
                    break;
                //case Theater.Jungle:
                //    colBasic= paletteTemperate;
                //    break;
                case Theater.Temperate:
                default:
                    colBasic = PaletteTemperate;
                    break;
                case Theater.Winter:
                    colBasic = PaletteTemperate;
                    break;
                case Theater.Snow:
                    colBasic = PaletteSnow;
                    break;
                case Theater.Interior:
                    colBasic = PaletteInterior;
                    break;
            }
            Color[] colFull = new Color[0x100];
            Array.Copy(colBasic, 0, colFull, 0, colBasic.Length);
            for (int i = colBasic.Length; i < 0x100; ++i)
            {
                colFull[i] = Color.Black;
            }
            // Apply overrides
            if (overrides != null)
            {
                foreach (KeyValuePair<byte, Color> kvp in overrides)
                {
                    colFull[kvp.Key] = kvp.Value;
                }
            }
            // Generate blue-tinted ones.
            using (Bitmap bm = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            using (Bitmap bm2 = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
            {
                Rectangle fullRect = new Rectangle(0, 0, 16, 16);
                using (Graphics g = Graphics.FromImage(bm2))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(0x80, 0x80, 0xFF)))
                {
                    g.FillRectangle(sb, fullRect);
                }
                for (int i = 0; i < 0x20; i++)
                {
                    bm.SetPixel(i % 0x10, i / 0x10, colFull[i]);
                }
                using (Graphics g = Graphics.FromImage(bm))
                using (ImageAttributes imageAttributes = new ImageAttributes())
                {
                    // 30% alpha.
                    ColorMatrix cm = new ColorMatrix(new float[][]
                    {
                        new float[] {1, 0, 0, 0, 0},
                        new float[] {0, 1, 0, 0, 0},
                        new float[] {0, 0, 1, 0, 0},
                        new float[] {0, 0, 0, 0.3f, 0},
                        new float[] {0, 0, 0, 0, 1},
                    });
                    imageAttributes.SetColorMatrix(cm);
                    g.DrawImage(bm2, fullRect, 0, 0, 16, 16, GraphicsUnit.Pixel, imageAttributes);
                }
                int i0 = 0;
                for (int i = 0x20; i < 0x40; i++)
                {
                    colFull[i] = bm.GetPixel(i0 % 0x10, i0 / 0x10);
                    i0++;
                }
            }
            return colFull;
        }

        public static Color[] GetTheaterPaletteFull(Theater theater)
        {
            Color[] colFull = GetTheaterPalette(theater);
            House[] houses = Enum.GetValues(typeof (House)).Cast<House>().ToArray();
            Array.Sort(houses);
            Int32 curIndex = 0x40;
            foreach (House house in houses)
            {
                Color[] housePal = GetHousePalette(house);
                Array.Copy(housePal, 0, colFull, curIndex, 2);
                curIndex += 2;
            }
            Array.Copy(PaletteOverlay, 0, colFull, curIndex, PaletteOverlay.Length);
            //curIndex += PaletteOverlay.Length;
            return colFull;
        }

        protected Byte[] IdentifyTheaterAndConvert(Byte[] fileData, ref Theater theater, Boolean toPC, String sourceFile, out Int32 errorCells)
        {
            Dictionary<Int32, CnCMapCell> mappingDes = toPC ? MapConversion.DESERT_MAPPING : MapConversion.DESERT_MAPPING_REVERSED;
            Dictionary<Int32, CnCMapCell> mappingTem = toPC ? MapConversion.TEMPERATE_MAPPING : MapConversion.TEMPERATE_MAPPING_REVERSED;
            errorCells = 0;
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
                    List<CnCMapCell> errCells;
                    fileData = this.ConvertMap(fileData, mapping, toPC, out errCells);
                    errorCells = errCells.Count;
                }
                else
                    theater = (Theater)0xFF;
            }
            if (theater == (Theater)0xFF)
            {
                List<CnCMapCell> errorcellsTemperate;
                Byte[] dataTemperate = this.ConvertMap(fileData, mappingTem, toPC, out errorcellsTemperate);
                List<CnCMapCell> errorcellsDesert;
                Byte[] dataDesert = this.ConvertMap(fileData, mappingDes, toPC, out errorcellsDesert);
                // Technically maps have more chance of being desert when they're Nod maps, so if the number of errors
                // is the same on each, and a filename is given, check the SC[G/B]##EA format of the filename.
                if (errorcellsTemperate.Count > errorcellsDesert.Count
                    || (errorcellsTemperate.Count == errorcellsDesert.Count && sourceFile != null && MAPREGEX.IsMatch(sourceFile = Path.GetFileNameWithoutExtension(sourceFile)) && sourceFile[2] == 'B'))
                {
                    fileData = dataDesert;
                    theater = Theater.Desert;
                    errorCells = errorcellsDesert.Count;
                }
                else
                {
                    // if sourceFile == null, use temperate as fallback if the amount of errors is equal
                    fileData = dataTemperate;
                    theater = Theater.Temperate;
                    errorCells = errorcellsTemperate.Count;
                }
            }
            return fileData;
        }

        protected Byte[] ConvertMap(Byte[] mapData, Dictionary<Int32, CnCMapCell> mapping, Boolean toPC, out List<CnCMapCell> errorcells)
        {
            CnCMap map = new CnCMap(mapData, false);
            map = MapConversion.ConvertMap(map, mapping, null, null, !toPC, out errorcells);
            return map.GetAsBytes();
        }

        private class MapStructure
        {
            public House Owner { get; set; }
            public StructInfo StructType { get; set; }
            public Int32 Cell { get; set; }

            public MapStructure(StructInfo structType, House owner, Int32 cell)
            {
                this.StructType = structType;
                this.Owner = owner;
                this.Cell = cell;
            }
        }
    }

    public class FileMapWwCc1PcFromIni : FileMapWwCc1Pc
    {
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "ini" }; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C Map ini"; } }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            IniInfo iniInfo;
            try
            {
                iniInfo = this.GetIniInfo(filename, (Theater)0xFF, fileData);
            }
            catch (Exception ex)
            {
                throw new FileTypeLoadException("Error loading ini file: "+ ex.Message);
            }
            if (iniInfo == null || !String.Equals(Path.GetFileName(iniInfo.File), Path.GetFileName(filename), StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("Could not load ini file.");
            String mapFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".bin";
            if (!File.Exists(mapFilename))
                throw new FileTypeLoadException("No .bin file found for this ini file.");
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(filename));
            FileInfo[] fi2 = di.GetFiles((Path.GetFileNameWithoutExtension(filename)) + ".bin");
            if (fi2.Length == 1)
                mapFilename = fi2[0].FullName;
            Byte[] mapFileData = File.ReadAllBytes(mapFilename);
            base.LoadFile(mapFileData, mapFilename, fileData, filename, true);
        }

    }

    public class IniInfo
    {
        public Theater Theater { get; set; }
        public String Name { get; set; }
        public String File { get; set; }
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public String Player { get; set; }
        public Dictionary<Int32, Int32> Population { get; set; }
    }

}
