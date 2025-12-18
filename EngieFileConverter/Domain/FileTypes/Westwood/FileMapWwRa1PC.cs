using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Westwood;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileMapWwRa1Pc : SupportedFileType
    {
        // disabled for now.
        public override FileClass FileClass { get { return FileClass.RaMap; } }
        public override FileClass InputFileClass { get { return FileClass.RaMap; } }

        public override String IdCode { get { return "WwRa1Map"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "RA1 Map"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Red Alert map file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "mpr", "ini" }; } }
        public override Int32 Width { get { return this.width; } }
        public override Int32 Height { get { return this.height; } }
        public override Int32 BitsPerPixel { get { return 0; } }

        // TODO remove when implemented.
        /// <summary>True if this type can save.</summary>
        public virtual Boolean CanSave { get { return false; } }

        protected Int32 width;
        protected Int32 height;

        public override void LoadFile(Byte[] fileData)
        {
            String fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(fileDataText, IniFile.ENCODING_DOS_US);
            this.ReadRAMap(mapini, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            String fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(filename, fileDataText, IniFile.ENCODING_DOS_US);
            this.ReadRAMap(mapini, filename);
        }

        private void ReadRAMap(IniFile mapini, String path)
        {
            List<String> sectionNames = mapini.GetSectionNames();
            if (!sectionNames.Contains("MapPack"))
                throw new FileTypeLoadException("No [MapPack] section found in file!");
            if (!sectionNames.Contains("Map"))
                throw new FileTypeLoadException("No [Map] section found in file!");

            Byte[] mapTerrain = this.ExpandRAMap(mapini, "MapPack", 3);
            Byte[] mapOverlay = this.ExpandRAMap(mapini, "OverlayPack", 1);
            this.SetFileNames(path);
        }

        private Byte[] ExpandRAMap(IniFile mapIniFile, String section, Int32 cellSize)
        {
            Dictionary<String, String> sectionValues = mapIniFile.GetSectionContent(section);
            StringBuilder sb = new StringBuilder();
            Int32 lineNr = 1;
            while (sectionValues.ContainsKey(lineNr.ToString()))
            {
                sb.Append(sectionValues[lineNr.ToString()]);
                lineNr++;
            }
            Byte[] compressedMap = Convert.FromBase64String(sb.ToString());
            Int32 readPtr = 0;
            Int32 writePtr = 0;
            Byte[] mapFile = new Byte[128 * 128 * cellSize];

            while (readPtr + 4 <= compressedMap.Length)
            {
                UInt32 uLength = ArrayUtils.ReadUInt32FromByteArrayLe(compressedMap, readPtr);
                Int32 length = (Int32)(uLength & 0xDFFFFFFF);
                readPtr += 4;
                Byte[] dest = new Byte[8192];
                Int32 readPtr2 = readPtr;
                Int32 decompressed = WWCompression.LcwDecompress(compressedMap, ref readPtr2, dest, 0);
                Array.Copy(dest, 0, mapFile, writePtr, decompressed);
                readPtr += length;
                writePtr += decompressed;
            }
            return mapFile;
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotImplementedException();
        }

    }

}
