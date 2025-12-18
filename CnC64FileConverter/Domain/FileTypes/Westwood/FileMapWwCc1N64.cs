using Nyerguds.GameData.Westwood;
using Nyerguds.Util;
using System;
using System.Drawing;
using System.IO;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileMapWwCc1N64 : FileMapWwCc1Pc
    {
        public override FileClass FileClass { get { return FileClass.CcMap; } }
        public override FileClass InputFileClass { get { return FileClass.CcMap; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 Map"; } }
        public override String ShortTypeDescription { get { return "Westwood C&C N64 map file"; } }
        public override String[] FileExtensions { get { return new String[] { "map" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] { ShortTypeDescription }; } }

        public FileMapWwCc1N64() { }

        public override void LoadFile(Byte[] fileData)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            m_LoadedImage = ReadN64MapAsImage(fileData, (Theater)0xFF, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            m_LoadedImage = ReadN64MapAsImage(fileData, filename, null);
            SetFileNames(filename);
        }

        public void LoadFile(Byte[] fileData, String filename, Byte[] iniData)
        {
            m_LoadedImage = ReadN64MapAsImage(fileData, filename, iniData);
            SetFileNames(filename);
        }
        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            FileMapWwCc1Pc mapPc = fileToSave as FileMapWwCc1Pc;
            if (mapPc == null)
                throw new NotSupportedException(String.Empty);
            return mapPc.N64MapData;
        }

        protected Bitmap ReadN64MapAsImage(Byte[] fileData, String filename, Byte[] iniData)
        {
            Theater theater = GetTheaterFromIni(filename, (Theater)0xFF, iniData);
            return ReadN64MapAsImage(fileData, theater, filename);
        }

        protected Bitmap ReadN64MapAsImage(Byte[] fileData, Theater theater, String sourceFile)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            Int32 len = fileData.Length / 2;
            for (Int32 i = 0; i < len; i++)
            {
                Byte hiByte = fileData[i * 2];
                Byte loByte = fileData[i * 2 + 1];
                if (hiByte == 0xFF && loByte == 0x00)
                        throw new FileTypeLoadException("Bad format for clear N64 terrain!");
            }
            N64MapData = fileData;
            PCMapData = IdentifyTheaterAndConvert(fileData, ref theater, true, sourceFile);
            return ReadMapAsImage(PCMapData, theater);
        }
    }

    public class FileMapWwCc1N64FromIni : FileMapWwCc1N64
    {
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "ini" }; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "C&C64 Map ini"; } }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            String mapFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".map";
            if (!File.Exists(mapFilename))
                throw new FileTypeLoadException("No .map file found for this ini file.");
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(filename));
            FileInfo[] fi2 = di.GetFiles((Path.GetFileNameWithoutExtension(filename)) + ".map");
            if (fi2.Length == 1)
                mapFilename = fi2[0].FullName;
            Byte[] mapData = File.ReadAllBytes(mapFilename);
            base.LoadFile(mapData, mapFilename, fileData);
        }
    }
}
