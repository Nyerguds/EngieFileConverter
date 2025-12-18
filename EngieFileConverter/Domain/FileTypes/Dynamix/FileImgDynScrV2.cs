using System;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImgDynScrV2 : FileImgDynScr
    {
        public override String[] FileExtensions { get { return new String[] { "scr" }; } }
        public override String ShortTypeName { get { return "Dynamix SCR v2"; } }
        public override String ShortTypeDescription { get { return "Dynamix Screen file v2"; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null, true, false);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.SetFileNames(filename);
            this.LoadFile(fileData, filename, true, false);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            return this.SaveToBytesAsThis(fileToSave, saveOptions, true);
        }

    }

}