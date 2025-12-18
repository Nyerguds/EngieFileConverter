using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Compression;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;

namespace EngieFileConverter.Domain.FileTypes
{
    class FileImgWwCpsToon : FileImgWwCps
    {
        public override String ShortTypeName { get { return "Toonstruck CPS"; } }
        public override String ShortTypeDescription { get { return "Toonstruck CPS File"; } }

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
            const Int32 cpsn = 0x4E435053;
            const Int32 lzss = 0x53535A4C;
            const Int32 rnc = 0x53535A4C;
            if (fileData.Length < 4)
                throw new FileTypeLoadException("Not a Toonstruck CPS!");
            UInt32 idBytes = (UInt32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 4, true);
            if (idBytes == cpsn)
                this.LoadFile(fileData, filename, true);
            else if (idBytes == lzss || (idBytes & 0xFFFFFF) == rnc)
                throw new FileTypeLoadException("ToonStruck CPS files of the LZSS and RNC types are currently not supported.");
            /*/
            else if (idBytes == lzss )
            {
                // todo. Still experimental for now.
                Int32 decompressedSize = (Int32)ArrayUtils.ReadIntFromByteArray(fileData, 0, 4, true);
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
                    ColorSixBit[] sbPalette = ColorUtils.ReadSixBitPalette(fileData, 0, colors);
                    this.m_Palette = ColorUtils.GetEightBitColorPalette(sbPalette);
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

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new NotSupportedException("File to save is empty!");
            Bitmap image = fileToSave.GetBitmap();
            if (fileToSave.IsFramesContainer || image.Width != 640 || image.Height != 400 || image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new NotSupportedException("Only 8-bit 640×400 images can be saved as CPS!");

            // If it is a non-image format which does contain colours, offer to save with palette
            Boolean hasColors = fileToSave.ColorsInPalette != 0;
            FileImgWwCps cps = fileToSave as FileImgWwCps;
            Int32 compression = cps != null ? cps.CompressionType : 4;
            return new SaveOption[]
            {
                new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", (hasColors ? 1 : 0).ToString()),
                new SaveOption("CMP", SaveOptionType.ChoicesList, "Compression type:", String.Join(",", this.compressionTypes), compression.ToString())
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
        {
            List<SaveOption> svOpts = new List<SaveOption>();
            svOpts.AddRange(saveOptions.Where(opt => !String.Equals(opt.Code, "VER")));
            svOpts.Add(new SaveOption("VER", SaveOptionType.Number, "Version", ((Int32)CpsVersion.Toonstruck).ToString()));
            return base.SaveToBytesAsThis(fileToSave, svOpts.ToArray());
        }
    }
}
