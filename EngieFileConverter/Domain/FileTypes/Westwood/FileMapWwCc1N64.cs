using System;
using System.Drawing;
using System.IO;
using Nyerguds.FileData.Westwood;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
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
        public override String[] DescriptionsForExtensions { get { return new String[] {this.ShortTypeDescription }; } }

        public FileMapWwCc1N64() { }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            FileMapWwCc1Pc mapPc = fileToSave as FileMapWwCc1Pc;
            if (mapPc == null)
                throw new NotSupportedException(String.Empty);
            return mapPc.N64MapData;
        }

        protected override Bitmap ReadMapAsImage(Byte[] fileData, Theater theater, String sourceFile)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            Int32 len = fileData.Length / 2;
            for (Int32 i = 0; i < len; ++i)
            {
                Byte hiByte = fileData[i * 2];
                Byte loByte = fileData[i * 2 + 1];
                if (hiByte == 0xFF && loByte == 0x00)
                        throw new FileTypeLoadException("Bad format for clear N64 terrain!");
            }
            this.N64MapData = fileData;
            this.PCMapData = this.IdentifyTheaterAndConvert(fileData, ref theater, true, sourceFile);
            return this.ReadMapAsImage(this.PCMapData, theater);
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
            IniInfo iniInfo = this.GetIniInfo(filename, (Theater)0xFF, fileData);
            if (iniInfo == null || !String.Equals(Path.GetFileName(iniInfo.File), Path.GetFileName(filename), StringComparison.InvariantCultureIgnoreCase))
                throw new FileTypeLoadException("Not an ini file.");
            String mapFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".map";
            if (!File.Exists(mapFilename))
                throw new FileTypeLoadException("No .map file found for this ini file.");
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(filename));
            FileInfo[] fi2 = di.GetFiles((Path.GetFileNameWithoutExtension(filename)) + ".map");
            if (fi2.Length == 1)
                mapFilename = fi2[0].FullName;
            Byte[] mapFileData = File.ReadAllBytes(mapFilename);
            base.LoadFile(mapFileData, mapFilename, fileData, filename);
        }
    }
}
