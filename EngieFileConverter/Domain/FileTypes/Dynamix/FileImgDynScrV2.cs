using System;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileImgDynScrV2 : FileImgDynScr
    {
        public override string[] FileExtensions { get { return new string[] { "scr" }; } }
        public override string ShortTypeName { get { return "Dynamix SCR v2"; } }
        public override string LongTypeName { get { return "Dynamix Screen file v2"; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null, true);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.SetFileNames(filename);
            this.LoadFromFileData(fileData, filename, true);
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            return this.SaveToBytesAsThis(fileToSave, saveOptions, true);
        }

    }

}