using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using Nyerguds.FileData.Westwood;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileMapWwCc1Pc : SupportedFileType
    {

        protected static readonly Regex HEXREGEX = new Regex("^[0-9A-F]+h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX1 = new Regex("^D\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX2 = new Regex("^FORD[1-2]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX3 = new Regex("^BRIDGE[1-4]D?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex MAPREGEX = new Regex("^SC[GB]\\d{2}\\d*[EWX][A-EL]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected static Color[] PaletteTemperate = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0x35, 0x44, 0x35), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), // Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), // Beach = 4
            Color.FromArgb(0x5E, 0x55, 0x44), // Road = 5
            Color.FromArgb(0x50, 0x50, 0x50), // CliffFace = 6
            Color.FromArgb(0x40, 0x40, 0x40), // CliffPlateau = 7
            Color.FromArgb(0x4D, 0x57, 0x4D), // Smudge = 8
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Snow = 9
            Color.Black,                      // Unused = A
            Color.Black,                      // Unused = B
            Color.Black,                      // Unused = C
            Color.Black,                      // Unused = D
            Color.Black,                      // Unused = E
            Color.Black,                      // Unused = F
            Color.FromArgb(0x00, 0x00, 0x95), // Blue, Unused = 10
            Color.FromArgb(0x2C, 0x39, 0x9A), // Blue, Clear = 11
            Color.FromArgb(0x82, 0x9F, 0xE8), // Blue, Water = 12
            Color.FromArgb(0x5F, 0x5F, 0xAC), // Blue, Rock = 13
            Color.FromArgb(0xC2, 0x9A, 0x9F), // Blue, Beach = 14
            Color.FromArgb(0x4F, 0x47, 0x9E), // Blue, Road = 15
            Color.FromArgb(0x43, 0x43, 0xA1), // Blue, CliffFace = 16
            Color.FromArgb(0x35, 0x35, 0x9D), // Blue, CliffPlateau = 17
            Color.FromArgb(0x40, 0x49, 0xA0), // Blue, Smudge = 18
            Color.FromArgb(0xAA, 0xAA, 0xDA), // Blue, Snow = 19
        };

        protected static Color[] PaletteDesert = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0x88, 0x5E, 0x46), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), // Rock = 3
            Color.FromArgb(0xE3, 0xB5, 0x49), // Beach = 4
            Color.FromArgb(0xAB, 0x81, 0x55), // Road = 5
            Color.FromArgb(0x50, 0x50, 0x50), // CliffFace = 6
            Color.FromArgb(0x40, 0x40, 0x40), // CliffPlateau = 7
            Color.FromArgb(0x5E, 0x48, 0x3E), // Smudge = 8
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Snow = 9
            Color.Black,                      // Unused = A
            Color.Black,                      // Unused = B
            Color.Black,                      // Unused = C
            Color.Black,                      // Unused = D
            Color.Black,                      // Unused = E
            Color.Black,                      // Unused = F
            Color.FromArgb(0x00, 0x00, 0x95), // Blue, Unused = 10
            Color.FromArgb(0x73, 0x4F, 0x9E), // Blue, Clear = 11
            Color.FromArgb(0x82, 0x9F, 0xE8), // Blue, Water = 12
            Color.FromArgb(0x5F, 0x5F, 0xAC), // Blue, Rock = 13
            Color.FromArgb(0xC2, 0x9A, 0x9F), // Blue, Beach = 14
            Color.FromArgb(0x91, 0x6D, 0xA2), // Blue, Road = 15
            Color.FromArgb(0x43, 0x43, 0xA1), // Blue, CliffFace = 16
            Color.FromArgb(0x35, 0x35, 0x9D), // Blue, CliffPlateau = 17
            Color.FromArgb(0x4F, 0x3C, 0x9C), // Blue, Smudge = 18
            Color.FromArgb(0xAA, 0xAA, 0xDA), // Blue, Snow = 19
        };

        protected static Color[] PaletteSnow = new Color[]
        {
            Color.Black,                      // Unused = 0
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), // Water = 2
            Color.FromArgb(0x50, 0x50, 0x50), // Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), // Beach = 4
            Color.FromArgb(0x92, 0x8A, 0x80), // Road = 5
            Color.FromArgb(0x60, 0x60, 0x60), // CliffFace = 6
            Color.FromArgb(0x60, 0x60, 0x60), // CliffPlateau = 7
            Color.FromArgb(0x9E, 0x9E, 0x9E), // Smudge = 8
            Color.FromArgb(0xC8, 0xC8, 0xC8), // Snow = 9
            Color.Black,                      // Unused = A
            Color.Black,                      // Unused = B
            Color.Black,                      // Unused = C
            Color.Black,                      // Unused = D
            Color.Black,                      // Unused = E
            Color.Black,                      // Unused = F
            Color.FromArgb(0x00, 0x00, 0x95), // Blue, Unused = 10
            Color.FromArgb(0xAA, 0xAA, 0xDA), // Blue, Clear = 11
            Color.FromArgb(0x82, 0x9F, 0xE8), // Blue, Water = 12
            Color.FromArgb(0x43, 0x43, 0xA1), // Blue, Rock = 13
            Color.FromArgb(0xC2, 0x9A, 0x9F), // Blue, Beach = 14
            Color.FromArgb(0x7C, 0x75, 0xB3), // Blue, Road = 15
            Color.FromArgb(0x51, 0x51, 0xA6), // Blue, CliffFace = 16
            Color.FromArgb(0x51, 0x51, 0xA6), // Blue, CliffPlateau = 17
            Color.FromArgb(0x86, 0x86, 0xC2), // Blue, Smudge = 18
            Color.FromArgb(0xAA, 0xAA, 0xDA), // Blue, Snow = 19
        };

        public override String IdCode { get { return "WwCc1MapPC"; } }
        public override FileClass FileClass { get { return FileClass.CcMap; } }
        public override FileClass InputFileClass { get { return FileClass.CcMap; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C Map"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "C&C map file - PC"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "bin" }; } }
        public override Int32 Width { get { return 64; } }
        public override Int32 Height { get { return 64; } }
        public override Int32 BitsPerPixel { get { return 0; } }

        public Byte[] PCMapData { get; protected set; }
        public Byte[] N64MapData { get; protected set; }

        public CnCMap Map { get { return new CnCMap(this.PCMapData);} }


        public FileMapWwCc1Pc() { }

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
            this.m_LoadedImage = this.ReadMapAsImage(fileData, theater, Rectangle.Empty);
        }

        public void LoadFile(Byte[] fileData, String filename, Byte[] iniContents, String iniFile, Boolean isPc)
        {
            IniInfo iniInfo = this.GetIniInfo(iniFile ?? filename, (Theater)0xFF, iniContents);
            Theater theater = iniInfo == null ? (Theater)0xFF : iniInfo.Theater;
            Rectangle usableArea = iniInfo == null ? Rectangle.Empty : new Rectangle(iniInfo.X, iniInfo.Y, iniInfo.Width, iniInfo.Height);
            this.DetectDataTypeAndConvert(fileData, theater, filename, usableArea, isPc);
            this.m_LoadedImage = this.ReadMapAsImage(this.PCMapData, theater, usableArea);
            if (iniInfo != null)
            {
                this.ExtraInfo = "Ini info loaded from \"" + Path.GetFileName(iniInfo.File) + "\"" + Environment.NewLine
                    + "Map width: " + iniInfo.Width + Environment.NewLine
                    + "Map height: " + iniInfo.Height + Environment.NewLine
                    + "Map X: " + iniInfo.X + Environment.NewLine
                    + "Map Y: " + iniInfo.Y;
                if (!String.IsNullOrEmpty(iniInfo.Name))
                    this.ExtraInfo += Environment.NewLine + "Mission name: " + iniInfo.Name;
            }
            this.SetFileNames(filename);
        }

        public override Color[] GetColors()
        {
            // This type does not expost itself as an image, and will pretend not to have a colour palette.
            return null;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
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
            try
            {
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
                if (inifile == null || (!inifile.ContainsSection("Basic") && !inifile.ContainsSection("Map")))
                    return null;
                String th = inifile.GetStringValue("Map", "Theater", null);
                info.Theater = GeneralUtils.TryParseEnum(th, defaultTheater, true);
                info.Name = inifile.GetStringValue("Basic", "Name", null);
                info.Width = inifile.GetIntValue("Map", "Width", 64);
                info.Height = inifile.GetIntValue("Map", "Height", 64);
                info.X = inifile.GetIntValue("Map", "X", 0);
                info.Y = inifile.GetIntValue("Map", "Y", 0);
                info.File = inipath;
            }
            catch { return null; }
            return info;
        }

        /// <summary>
        /// Converts a map to image.
        /// </summary>
        /// <param name="fileData">File data for a PC C&amp;C type map.</param>
        /// <param name="theater">Theater of the map.</param>
        /// <param name="usableArea"></param>
        /// <returns></returns>
        protected Bitmap ReadMapAsImage(Byte[] fileData, Theater theater, Rectangle usableArea)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            TerrainTypeEnh[] simplifiedMap;
            try
            {
                simplifiedMap = MapConversion.SimplifyMap(new CnCMap(fileData));
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException(ex.Message, ex);
            }
            Color[] palette;
            switch (theater)
            {
                case Theater.Desert:
                    palette = PaletteDesert;
                    break;
                //case Theater.Jungle:
                //    palette = paletteTemperate;
                //    break;
                case Theater.Temperate:
                default:
                    palette = PaletteTemperate;
                    break;
                case Theater.Winter:
                    palette = PaletteTemperate;
                    break;
                case Theater.Snow:
                    palette = PaletteSnow;
                    break;
            }
            Byte[] imageData = new Byte[64 * 64];
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
                        imageData[i] = (Byte)(simplifiedMap[i] + 0x10);
                }
                // paint normal-coloured area
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
            return ImageUtils.BuildImage(imageData, 64, 64, 64, PixelFormat.Format8bppIndexed, palette, Color.Black);
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
            CnCMap map = new CnCMap(mapData);
            map = MapConversion.ConvertMap(map, mapping, null, null, !toPC, out errorcells);
            return map.GetAsBytes();
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
            IniInfo iniInfo = this.GetIniInfo(filename, (Theater)0xFF, fileData);
            if (iniInfo == null || !String.Equals(Path.GetFileName(iniInfo.File), Path.GetFileName(filename), StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("Not an ini file.");
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
        public Int32 Width;
        public Int32 Height;
        public Int32 X;
        public Int32 Y;
    }
}
