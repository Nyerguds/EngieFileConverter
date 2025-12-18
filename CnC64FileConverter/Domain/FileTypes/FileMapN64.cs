using Nyerguds.CCTypes;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.ImageFile
{
    public class FileMapN64 : FileMapPc
    {        
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "N64Map"; } }
        public override String ShortTypeDescription { get { return "N64 C&C map file"; } }
        public override String[] FileExtensions { get { return new String[] { "map" }; } }
        /// <summary>Brief name and description of the specific types for all extensions, for the types dropdown in the save file dialog.</summary>
        public override String[] DescriptionsForExtensions { get { return new String[] { ShortTypeDescription }; } }
        public override N64FileType PreferredExportType { get { return new FileMapPc(); } }

        public FileMapN64() { }

        public override void LoadImage(Byte[] fileData)
        {
            if (fileData.Length != 8192)
                throw new FileTypeLoadException("Incorrect file size.");
            m_LoadedImage = ReadN64MapAsImage(fileData, (Theater)0xFF, null);
        }

        public override void LoadImage(String filename)
        {
            m_LoadedImage = ReadN64MapAsImage(filename);
        }

        public override void SaveAsThis(N64FileType fileToSave, String savePath)
        {
            if (fileToSave is FileMapPc)
                File.WriteAllBytes(savePath, ((FileMapPc)fileToSave).N64MapData);
            else
                throw new NotSupportedException();
        }
        
        private Bitmap ReadN64MapAsImage(String filename)
        {
            Theater theater = GetTheaterFromIni(filename, (Theater)0xFF);
            Byte[] fileData = File.ReadAllBytes(filename);
            return ReadN64MapAsImage(fileData, theater, filename);
        }

        private Bitmap ReadN64MapAsImage(Byte[] fileData, Theater theater, String sourceFile)
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
}
