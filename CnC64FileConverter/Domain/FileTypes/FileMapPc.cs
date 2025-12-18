using Nyerguds.Ini;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using Nyerguds.Util;
using Nyerguds.CCTypes;
using Nyerguds.ImageManipulation;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileMapPc : SupportedFileType
    {
        protected static readonly Regex HEXREGEX = new Regex("^[0-9A-F]+h$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX1 = new Regex("^D\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX2 = new Regex("^FORD[1-2]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex ROADREGEX3 = new Regex("^BRIDGE[1-4]D?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected static readonly Regex MAPREGEX = new Regex("^SC[GB]\\d{2}\\d*[EWX][A-E]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected static Color[] paletteTemperate = new Color[]
        {
            Color.Black,                      //Unused = 0
            Color.FromArgb(0x35, 0x44, 0x35), //Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), //Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), //Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), //Beach = 4
            Color.FromArgb(0x5E, 0x55, 0x44), //Road = 5
            Color.FromArgb(0x50, 0x50, 0x50), //CliffFace = 6
            Color.FromArgb(0x40, 0x40, 0x40), //CliffPlateau = 7
        };
        protected static Color[] paletteDesert = new Color[]
        {
            Color.Black,                      //Unused = 0
            Color.FromArgb(0x88, 0x5E, 0x46), //Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), //Water = 2
            Color.FromArgb(0x70, 0x70, 0x70), //Rock = 3
            Color.FromArgb(0xE3, 0xB5, 0x49), //Beach = 4
            Color.FromArgb(0xAB, 0x81, 0x55), //Road = 5
            Color.FromArgb(0x50, 0x50, 0x50), //CliffFace = 6
            Color.FromArgb(0x40, 0x40, 0x40), //CliffPlateau = 7
        };
        protected static Color[] paletteSnow = new Color[]
        {
            Color.Black,                      //Unused = 0
            Color.FromArgb(0xC8, 0xC8, 0xC8), //Clear = 1
            Color.FromArgb(0x99, 0xBB, 0xDD), //Water = 2
            Color.FromArgb(0x50, 0x50, 0x50), //Rock = 3
            Color.FromArgb(0xe3, 0xb5, 0x49), //Beach = 4
            Color.FromArgb(0x92, 0x8A, 0x80), //Road = 5
            Color.FromArgb(0x60, 0x60, 0x60), //CliffFace = 6
            Color.FromArgb(0x60, 0x60, 0x60), //CliffPlateau = 7
        };
        
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "PCMap"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "PC C&C map file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "bin" }; } }
        public override Int32 Width { get { return 64; } }
        public override Int32 Height { get { return 64; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override SupportedFileType PreferredExportType { get { return new FileMapN64(); } }
        public override Int32 BitsPerColor { get { return 0; } }

        public Byte[] PCMapData { get; protected set; }
        public Byte[] N64MapData { get; protected set; }

        public CnCMap Map { get { return new CnCMap(PCMapData);} }


        public FileMapPc() { }
        
        public override void LoadFile(Byte[] fileData)
        {
            this.PCMapData = fileData;
            Theater theater = (Theater)0xFF;
            N64MapData = IdentifyTheaterAndConvert(fileData, ref theater, true, null);
            m_LoadedImage = ReadMapAsImage(fileData, theater);
        }

        public override void LoadFile(String filename)
        {
            m_LoadedImage = ReadMapAsImage(filename, (Theater)0xFF);
            SetFileNames(filename);
        }
        
        public override Color[] GetColors()
        {
            return null;
        }
        
        public override void SaveAsThis(SupportedFileType fileToSave, String savePath)
        {
            if (fileToSave is FileMapPc)
                File.WriteAllBytes(savePath, ((FileMapPc)fileToSave).PCMapData);
            else
                throw new NotSupportedException(String.Empty);
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
            HeightTerrainType[] simplifiedMap;
            try
            {
                simplifiedMap = MapConversion.SimplifyMap(new CnCMap(fileData));
            }
            catch(NotSupportedException ex)
            {
                throw new FileTypeLoadException(ex.Message, ex);
            }
            Color[] palette;
            switch (theater)
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
            Byte[] imageData = new Byte[64 * 64];
            for (Int32 i = 0; i < simplifiedMap.Length; i++)
            {
                imageData[i] = (Byte)simplifiedMap[i];
            }
            return ImageUtils.BuildImage(imageData, 64, 64, 64, PixelFormat.Format8bppIndexed, palette, Color.Black);
        }
        
        protected Byte[] IdentifyTheaterAndConvert(Byte[] fileData, ref Theater theater, Boolean toPC, String sourceFile)
        {
            Dictionary<Int32, CnCMapCell> mappingDes = toPC ? MapConversion.DESERT_MAPPING : MapConversion.DESERT_MAPPING_REVERSED;
            Dictionary<Int32, CnCMapCell> mappingTem = toPC ? MapConversion.TEMPERATE_MAPPING : MapConversion.TEMPERATE_MAPPING_REVERSED;

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
    }

    public class FileMapPcFromIni : FileMapPc
    {
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "ini" }; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64MapIni"; } }

        public override void LoadFile(String filename)
        {
            String mapFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".bin";
            if (!File.Exists(mapFilename))
                throw new FileTypeLoadException("No .bin file found for this ini file.");
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(filename));
            FileInfo[] fi2 = di.GetFiles((Path.GetFileNameWithoutExtension(filename)) + ".bin");
            if (fi2.Length == 1)
                mapFilename = fi2[0].FullName;
            base.LoadFile(mapFilename);
        }

    }

    
}
