using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class FileImgDynScrV2 : FileImgDynScr
    {
        public override String[] FileExtensions { get { return new String[] { "scr" }; } }
        public override String ShortTypeName { get { return "DynScrV2"; } }
        public override String ShortTypeDescription { get { return "Dynamix Screen file v2"; } }

        public override void SetColors(Color[] palette)
        {
            m_Palette = palette.ToArray();
            base.SetColors(palette);
        }

        public override void LoadFile(String filename)
        {
            Byte[] fileData = File.ReadAllBytes(filename);
            SetFileNames(filename);
            LoadFile(fileData, filename, true);
        }

        public override void LoadFile(Byte[] fileData)
        {
            LoadFile(fileData, null, true);
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Boolean dontCompress)
        {
            return SaveToBytesAsThis(fileToSave, dontCompress, true);
        }

    }

}