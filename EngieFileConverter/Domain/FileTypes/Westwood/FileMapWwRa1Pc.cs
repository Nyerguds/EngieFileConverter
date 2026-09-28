//#define EditsResearchMode

using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;
using System.Drawing;
using Nyerguds.ImageManipulation;
using System.Drawing.Imaging;
using System.IO;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileMapWwRa1Pc : SupportedFileType
    {
        public override string IdCode { get { return "WwRa1Map"; } }
        public override FileClass FileClass { get { return FileClass.RaMap | FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.RaMap; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "RA1 Map"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override string LongTypeName { get { return "Red Alert map file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override string[] FileExtensions { get { return new string[] { "mpr", "ini" }; } }
        public override int Width { get { return 128; } }
        public override int Height { get { return 128; } }
        public override int BitsPerPixel { get { return 8; } }

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public override bool CanSave { get { return false; } }

        public override void LoadFile(byte[] fileData)
        {
            string fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(fileDataText, IniFile.ENCODING_DOS_US);
            this.ReadRAMap(mapini, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            string fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(filename, fileDataText, IniFile.ENCODING_DOS_US);
            this.ReadRAMap(mapini, filename);
        }

        private void ReadRAMap(IniFile mapini, string path)
        {
            List<string> sectionNames = mapini.GetSectionNames();
            if (mapini.GetStringValue("Basic", "NewINIFormat", null) != "3")
                throw new FileTypeLoadException("Not a Red Alert Map file.");
            if (!sectionNames.Contains("MapPack"))
                throw new FileTypeLoadException("No [MapPack] section found in file.");
            if (!sectionNames.Contains("Map"))
                throw new FileTypeLoadException("No [Map] section found in file.");
            IniInfo iniInfo = GetIniInfo(mapini, Theater.Temperate);
            Rectangle usableArea = iniInfo == null ? Rectangle.Empty : new Rectangle(iniInfo.X, iniInfo.Y, iniInfo.Width, iniInfo.Height);
            List<string> errors = new List<string>();
            byte[] mapTerrain = this.DecompressLCWSection(mapini, "MapPack", 3, errors);
            if (errors.Count > 0)
                throw new FileTypeLoadException(String.Join("\n", errors.ToArray()));
            //Byte[] mapOverlay = this.DecompressLCWSection(mapini, "OverlayPack", 1, errors);
            if (errors.Count > 0)
                throw new FileTypeLoadException(String.Join("\n", errors.ToArray()));
            this.m_LoadedImage = this.ReadMapAsImage(mapTerrain, iniInfo.Theater, usableArea, null, out bool containsOldClear);
            if (containsOldClear)
            {
                this.ExtraInfo = "Contains old clear terrain.";
            }
            this.SetFileNames(path);
        }

        private byte[] DecompressLCWSection(IniFile mapIniFile, string section, int bytesPerCell, List<string> errors)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string key in mapIniFile.GetSectionKeys(section))
            {
                sb.Append(mapIniFile.GetStringValue(section, key, String.Empty));
            }
            byte[] compressedBytes;
            try
            {
                compressedBytes = Convert.FromBase64String(sb.ToString());
            }
            catch (FormatException)
            {
                errors.Add("Failed to unpack [" + section + "] from Base64.");
                return null;
            }
            int readPtr = 0;
            int writePtr = 0;
            byte[] decompressedBytes = new byte[CnCMap.LENGTH_RA * bytesPerCell];
            while ((readPtr + 4) <= compressedBytes.Length)
            {
                uint uLength;
                using (BinaryReader reader = new BinaryReader(new MemoryStream(compressedBytes, readPtr, 4)))
                {
                    uLength = reader.ReadUInt32();
                }
                int length = (int)(uLength & 0x0000FFFF);
                readPtr += 4;
                byte[] dest = new byte[8192];
                int readPtr2 = readPtr;
                int decompressed;
                try
                {
                    decompressed = WWCompression.LcwDecompress(compressedBytes, ref readPtr2, dest, 0);
                }
                catch
                {
                    errors.Add("Error decompressing [" + section + "].");
                    return decompressedBytes;
                }
                if (writePtr + decompressed > decompressedBytes.Length)
                {
                    errors.Add("Failed to decompress [" + section + "]: data exceeds map size.");
                    return decompressedBytes;
                }
                Array.Copy(dest, 0, decompressedBytes, writePtr, decompressed);
                readPtr += length;
                writePtr += decompressed;
            }
            return decompressedBytes;
        }

        protected IniInfo GetIniInfo(IniFile inifile, Theater defaultTheater)
        {
            IniInfo info = new IniInfo();
            info.Theater = defaultTheater;
            if (inifile == null || (!inifile.ContainsSection("Basic") && !inifile.ContainsSection("Map")))
                return null;
            string th = inifile.GetStringValue("Map", "Theater", null);
            info.Theater = GeneralUtils.TryParseEnum(th, defaultTheater, true);
            info.Name = inifile.GetStringValue("Basic", "Name", null);
            info.Width = inifile.GetIntValue("Map", "Width", 64);
            info.Height = inifile.GetIntValue("Map", "Height", 64);
            info.X = inifile.GetIntValue("Map", "X", 0);
            info.Y = inifile.GetIntValue("Map", "Y", 0);
            info.Player = inifile.GetStringValue("Basic", "Player", null);
            //info.Population = this.GetPopulation(inifile);
            //info.File = inipath;
            return info;
        }

        /// <summary>
        /// Converts a map to image.
        /// </summary>
        /// <param name="fileData">File data for a PC C&amp;C type map.</param>
        /// <param name="theater">Theater of the map.</param>
        /// <param name="usableArea">USable area of the map.</param>
        /// <param name="addedPixels">Added data to populate the map.</param>
        /// <returns></returns>
        protected Bitmap ReadMapAsImage(byte[] fileData, Theater theater, Rectangle usableArea, Dictionary<int, int> addedPixels, out bool containsOldClear)
        {
            if (fileData.Length != CnCMap.FILELENGTH_RA)
                throw new FileTypeLoadException("Incorrect file size.");
            containsOldClear = false;
            int oldClearOutsideBounds = 0;
            TerrainTypeEnh[] simplifiedMap;
            try
            {
                CnCMap ramap = new CnCMap(fileData, true);
#if DEBUG && EditsResearchMode
                if (addedPixels == null)
                    addedPixels = new Dictionary<Int32, Int32>();
                // Indicate new clear terrain
                for (int i = 0; i < ramap.Cells.Length; ++i)
                {
                    CnCMapCell cell = ramap.Cells[i];
                    if (cell.TemplateType == 0xFFFF) // For detecting edits on old clear terrain
                    //if (cell.TemplateType == 0x2) // For scmk4ea
                    {
                        byte val = 0x1F;
                        if (!usableArea.IsEmpty && !usableArea.Contains(new Point(i & 0x7F, i >> 7)))
                        {
                            val += 0x20;
                        }
                        addedPixels[i] = val;
                    }
                    if (cell.TemplateType == 0xFF) // For detecting edits on old clear terrain
                    {
                        byte val = 0x1E;
                        if (!usableArea.IsEmpty && !usableArea.Contains(new Point(i & 0x7F, i >> 7)))
                        {
                            val += 0x20;
                        }
                        addedPixels[i] = val;
                    }
                }
#endif
                simplifiedMap = MapConversion.SimplifyMap(ramap, MapConversion.TILEINFO_RA);
                // Wipe old clear terrain
                for (int i = 0; i < ramap.Cells.Length; ++i)
                {
                    CnCMapCell cell = ramap.Cells[i];
                    if (cell.TemplateType == 0xFF)
                    {
                        simplifiedMap[i] = TerrainTypeEnh.Clear;
                        containsOldClear = true;
                        int x = i % 128;
                        int y = i / 128;
                        if (x < usableArea.Left || x >= usableArea.Right || y < usableArea.Top || y >= usableArea.Bottom)
                        {
                            oldClearOutsideBounds++;
                        }
                    }
                }
            }
            catch (ArgumentException ex)
            {
                throw new FileTypeLoadException(GeneralUtils.RecoverArgExceptionMessage(ex, true), ex);
            }
            if (containsOldClear && ((double)(simplifiedMap.Length - (usableArea.Width * usableArea.Height)) / oldClearOutsideBounds) < 0.8)
            {
                containsOldClear = false;
            }
            Dictionary<byte, Color> extracol = null;
#if DEBUG && EditsResearchMode
            Color clearOld = Color.White;
            switch (theater)
    {
                case Theater.Desert:    clearOld = Color.FromArgb(0xFF, 0xFF, 0x00); break;
                case Theater.Jungle:    clearOld = Color.FromArgb(0x30, 0x70, 0x30); break;
                case Theater.Temperate: clearOld = Color.FromArgb(0x30, 0x70, 0x30); break;
                case Theater.Winter:    clearOld = Color.FromArgb(0x30, 0x70, 0x30); break;
                case Theater.Snow:      clearOld = Color.FromArgb(0xE0, 0xE0, 0xE0); break;
                case Theater.Interior:  clearOld = Color.FromArgb(0x30, 0x30, 0x30); break;

            }
        extracol = new Dictionary<byte, Color>() { { 0x1E, clearOld }, { 0x1F, Color.FromArgb(0xFF, 0x00, 0x80) } };
#endif
            Color[] palette = FileMapWwCc1Pc.GetTheaterPalette(theater, extracol);
            byte[] imageData = new byte[CnCMap.LENGTH_RA];
            if (usableArea == Rectangle.Empty)
            {
                for (int i = 0; i < simplifiedMap.Length; ++i)
                    imageData[i] = (byte)simplifiedMap[i];
            }
            else
            {
                // paint blue-tinted outside border
                if (usableArea != Rectangle.Empty)
                {
                    for (int i = 0; i < simplifiedMap.Length; ++i)
                        imageData[i] = (byte)(simplifiedMap[i] + 0x20);
                }
                // paint normal-colored area
                int minY = usableArea != Rectangle.Empty ? usableArea.Y : 0;
                int maxY = usableArea != Rectangle.Empty ? usableArea.Y + usableArea.Height : 128;
                int minX = usableArea != Rectangle.Empty ? usableArea.X : 0;
                int maxX = usableArea != Rectangle.Empty ? usableArea.X + usableArea.Width : 128;
                for (int y = minY; y < maxY; ++y)
                {
                    for (int x = minX; x < maxX; ++x)
                    {
                        int cell = (y << 7) | x;
                        imageData[cell] = (byte)simplifiedMap[cell];
                    }
                }
            }
            if (addedPixels != null)
            {
                int[] cells = addedPixels.Keys.ToArray();
                for (int i = 0; i < cells.Length; ++i)
                {
                    int cell = cells[i];
                    imageData[cell] = (byte)addedPixels[cell];
                }
            }
            return ImageUtils.BuildImage(imageData, 128, 128, 128, PixelFormat.Format8bppIndexed, palette, Color.Black);
        }


        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotImplementedException();
        }

    }

}
