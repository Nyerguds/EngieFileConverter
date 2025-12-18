using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.FileData.Compression;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    class FileImgWwCpsToon : FileImgWwCps
    {
        public override String ShortTypeName { get { return "Toonstruck CPS"; } }
        public override String LongTypeName { get { return "Toonstruck CPS File"; } }

        // TODO might need inbuilt palette here.

        public FileImgWwCpsToon()
        {
            this.m_Width = 640;
            this.m_Height = 400;
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            if (fileData.Length < 4)
                throw new FileTypeLoadException("File is not long enough to be a valid CPS file.");
            const Int32 cpsn = 0x4E435053; // "SPCN" string
            const Int32 lzss = 0x53535A4C; // "LZSS" string
            const Int32 rnc = 0x53535A4C; // Identical? Check this!
            if (fileData.Length < 4)
                throw new FileTypeLoadException("Not a Toonstruck CPS!");
            UInt32 idBytes = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 0);
            if (idBytes == cpsn)
                this.LoadFile(fileData, filename, true);
            else if (idBytes == lzss || (idBytes & 0xFFFFFF) == rnc)
                throw new FileTypeLoadException("ToonStruck CPS files of the LZSS and RNC types are currently not supported.");
            /*/
            else if (idBytes == lzss )
            {
                // todo. Still experimental for now.
                Int32 decompressedSize = ArrayUtils.ReadInt32FromByteArrayLe(fileData, 0);
                Byte[] rawData = LzssHuffDecoder.LzssDecode(fileData, 8, fileData.Length, decompressedSize);
                Int32 palSize = decompressedSize - 256000;
                Byte[] imageData = palSize == 0 ? rawData : new Byte[256000];

                if (palSize < 0)
                    throw new FileTypeLoadException("Bad length for ToonStruck CPS file!");
                if (palSize > 0)
                {
                    Byte[] palette = new Byte[palSize];
                    Array.Copy(rawData, 0, palette, 0, palSize);
                    if (palSize % 3 != 0)
                        throw new FileTypeLoadException("Bad length for 6-bit CPS palette!");
                    Int32 colors = palSize / 3;
                    this.m_Palette = ColorUtils.ReadSixBitPalette(fileData, 0, colors);
                    this.HasPalette = true;
                    Array.Copy(rawData, palSize, imageData, 0, 256000);
                }
                this.CpsVersion = CpsVersion.Toonstruck;
                SetExtraInfo();
            }
            else if ((idBytes & 0xFFFFFF) == rnc) { }// todo.
            //*/
            else
                throw new FileTypeLoadException("Not a Toonstruck CPS!");
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException("File to save is empty!", "fileToSave");
            Bitmap image = fileToSave.GetBitmap();
            if (fileToSave.IsFramesContainer || image.Width != 640 || image.Height != 400 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException("Only 8-bit 640×400 images can be saved as CPS!", "fileToSave");

            FileImgWwCps cps = fileToSave as FileImgWwCps;
            Int32 compression = cps != null ? cps.CompressionType : 4;
            return new Option[]
            {
                new Option("PAL", OptionInputType.Boolean, "Include palette", (fileToSave.NeedsPalette ? 0 : 1).ToString()),
                new Option("CMP", OptionInputType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            List<Option> svOpts = new List<Option>();
            svOpts.AddRange(saveOptions.Where(opt => !String.Equals(opt.Code, "VER")));
            svOpts.Add(new Option("VER", OptionInputType.Number, "Version", ((Int32)CpsVersion.Toonstruck).ToString()));
            return base.SaveToBytesAsThis(fileToSave, svOpts.ToArray());
        }
    }
}
