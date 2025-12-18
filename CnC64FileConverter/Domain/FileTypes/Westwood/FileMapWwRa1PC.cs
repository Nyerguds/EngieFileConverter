using Nyerguds.Ini;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CnC64FileConverter.Domain.CCTypes;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileMapWwRa1PC : SupportedFileType
    {
        // disabled for now.
        public override FileClass FileClass { get { return FileClass.None; } }
        public override FileClass InputFileClass { get { return FileClass.None; } }

        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "RA Map"; } }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        public override String ShortTypeDescription { get { return "Westwood RA1 PC map file"; } }
        /// <summary>Possible file extensions for this file type.</summary>
        public override String[] FileExtensions { get { return new String[] { "mrp", "ini" }; } }
        public override Int32 Width { get { return this.width; } }
        public override Int32 Height { get { return this.height; } }
        public override Int32 ColorsInPalette { get { return 0; } }
        public override Int32 BitsPerColor { get { return 0; } }

        protected Int32 width;
        protected Int32 height;


        public override void LoadFile(Byte[] fileData)
        {
            String fileDataText = IniFile.ENCODING_DOS_US.GetString(fileData);
            IniFile mapini = new IniFile(fileDataText, IniFile.ENCODING_DOS_US);
            ReadRAMap(mapini);
        }

        public override void LoadFile(String filename)
        {
            IniFile mapini = new IniFile(filename, IniFile.ENCODING_DOS_US);
            ReadRAMap(mapini);
        }

        private void ReadRAMap(IniFile mapini)
        {
            List<String> sectionNames = mapini.GetSectionNames();
            if (!sectionNames.Contains("MapPack"))
                throw new FileTypeLoadException("No [MapPack] section found in file!");
            if (!sectionNames.Contains("Map"))
                throw new FileTypeLoadException("No [Map] section found in file!");

        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            throw new NotImplementedException();
        }
    }

}
