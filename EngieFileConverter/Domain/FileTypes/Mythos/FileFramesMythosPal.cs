using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Mythos Visage format. See https://www.shikadi.net/wiki/Visage_Format
    /// </summary>
    public class FileFramesMythosPal : FileFramesMythosVgs
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override FileClass FrameInputFileClass { get { return FileClass.None; } }

        public override string IdCode { get { return "MythPal"; } }
        public override string ShortTypeName { get { return "Mythos Visage palette"; } }
        public override string LongTypeName { get { return "Mythos Visage Palette file"; } }
        public override string[] FileExtensions { get { return new string[] { "pal" }; } }

        /// <summary>Retrieves the sub-frames inside this file. This works even if the type is not set as frames container.</summary>
        public override SupportedFileType[] Frames { get { return null; } }
        /// <summary>See this as nothing but a container for frames, as opposed to a file that just has the ability to visualize its data as frames. Types with frames where this is set to false wil not get an index -1 in the frames list.</summary>
        public override bool IsFramesContainer { get { return false; } }
        public override bool[] TransparencyMask { get { return null; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.SetFileNames(filename);
            List<Point> framesXY;
            this.LoadFromFileData(fileData, filename, true, true, false, out framesXY, false);
            byte[] imageData = Enumerable.Range(0, 0x100).Select(x => (byte)x).ToArray();
            PaletteUtils.ApplyPalTransparencyMask(this.m_Palette, null);
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, 16, 16, 16, PixelFormat.Format8bppIndexed, this.m_Palette, Color.Black);
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName) { return new Option[0]; }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            saveOptions = new Option[] { new Option("PALONLY", OptionInputType.Boolean, String.Empty, "1") };
            return base.SaveToBytesAsThis(fileToSave, saveOptions);
        }

    }
}