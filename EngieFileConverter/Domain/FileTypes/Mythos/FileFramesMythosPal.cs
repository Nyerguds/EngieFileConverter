using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileFramesMythosPal : FileFramesMythosVgs
    {

        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }

        public override String IdCode { get { return "MythPal"; } }
        public override String ShortTypeName { get { return "Mythos Visage palette"; } }
        public override String LongTypeName { get { return "Mythos Visage Palette file"; } }
        public override String[] FileExtensions { get { return new String[] { "pal" }; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return null; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override Boolean IsFramesContainer { get { return false; } }
        public override Boolean[] TransparencyMask { get { return null; } }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.SetFileNames(filename);
            List<Point> framesXY;
            this.LoadFromFileData(fileData, filename, true, true, false, out framesXY, false);
            Byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (Byte)x).ToArray();
            PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, null);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName) { return new Option[0]; }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            saveOptions = new Option[] { new Option("PALONLY", OptionInputType.Boolean, String.Empty, "1") };
            return base.SaveToBytesAsThis(fileToSave, saveOptions);
        }

    }
}