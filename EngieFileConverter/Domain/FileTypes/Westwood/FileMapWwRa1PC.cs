using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileMapWwRa1Pc : SupportedFileType
    {
        // disabled for now.
        public override FileClass FileClass { get { return FileClass.None; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }

        public override String IdCode { get { return "WwRa1Map"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "RA Map"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Westwood RA1 PC map file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "mrp", "ini" }; } }
        public override Int32 Width { get { return this.width; } }
        public override Int32 Height { get { return this.height; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerPixel { get { return 0; } }

        protected Int32 width;
        protected Int32 height;


        public override void LoadFile(Byte[] fileData)
        {
            String fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(fileDataText, IniFile.ENCODING_DOS_US);
            ReadRAMap(mapini, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            String fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(filename, fileDataText, IniFile.ENCODING_DOS_US);
            ReadRAMap(mapini, filename);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotImplementedException();
        }

        private void ReadRAMap(IniFile mapini, String path)
        {
            List<String> sectionNames = mapini.GetSectionNames();
            if (!sectionNames.Contains("MapPack"))
                throw new FileTypeLoadException("No [MapPack] section found in file!");
            if (!sectionNames.Contains("Map"))
                throw new FileTypeLoadException("No [Map] section found in file!");

            Byte[] mapTerrain = ExpandRAMap(mapini, "MapPack");
            Byte[] mapOverlay = ExpandRAMap(mapini, "OverlayPack");
            SetFileNames(path);
        }


        private Byte[] ExpandRAMap(IniFile mapIniFile, String section)
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
            Byte[] mapFile = new Byte[128 * 128 * 3];

            while (readPtr + 4 <= compressedMap.Length)
            {
                UInt32 uLength = (UInt32)ArrayUtils.ReadIntFromByteArray(compressedMap, readPtr, 4, true);
                Int32 length = (Int32)(uLength & 0xDFFFFFFF);
                readPtr += 4;
                Byte[] dest = new Byte[8192];
                Int32 readPtr2 = readPtr;
                Int32 decompressed = Nyerguds.FileData.Westwood.WWCompression.LcwDecompress(compressedMap, ref readPtr2, dest, 0);
                Array.Copy(dest, 0, mapFile, writePtr, decompressed);
                readPtr += length;
                writePtr += decompressed;
            }
            return mapFile;
            /*/
            // Align from 24 to 32 bit
            Byte[] mapFile2 = new Byte[128 * 128 * 16];
            writePtr = 0;
            for (Int32 i = 0; i < mapFile.Length; i += 3)
            {
                writePtr += 8;
                mapFile2[writePtr++] = mapFile[i];
                mapFile2[writePtr++] = mapFile[i + 1];
                mapFile2[writePtr++] = mapFile[i + 2];
                writePtr += 5;
            }
            File.WriteAllBytes("SCA01EA_expanded16.BIN", mapFile2);
            //*/
        }

    }

}
