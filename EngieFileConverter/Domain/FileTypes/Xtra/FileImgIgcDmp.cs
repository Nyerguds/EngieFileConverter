using System;
using System.Drawing;
using System.Drawing.Imaging;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace EngieFileConverter.Domain.FileTypes
{
    /// <summary>
    /// Interactive Girls image files.
    /// Has a special format where files suffixed with -tl, -tr, -bl and -br
    /// (top left, top right, bottom left, bottom right) are combined to one larger image.
    /// </summary>
    public class FileImgIgcDmp : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }

        public override string IdCode { get { return "IgDmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "Interactive Girls DMP file"; } }
        public override string[] FileExtensions { get { return new string[] { "dmp" }; } }
        public override string LongTypeName { get { return "Interactive Girls DMP image file"; } }
        public override bool NeedsPalette { get { return !this.m_PaletteLoaded; } }
        public override int BitsPerPixel { get { return 8; } }
        protected bool m_PaletteLoaded;
        public bool Combined { get; private set; }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(byte[] fileData, string filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(byte[] fileData, string sourcePath)
        {
            string filename = sourcePath;
            string basePath = Path.GetDirectoryName(sourcePath);
            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string baseExt = Path.GetExtension(sourcePath);
            string curFile = Path.Combine(basePath, Path.GetFileName(filename));
            byte[] palette = null;
            int width = -1;
            int height = -1;
            byte[] imageData = null;
            Regex nameEnd = new Regex("^(.*?)-[tb][lr]$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Match m;
            bool combined = false;
            if (baseName != null && baseExt != null && (m = nameEnd.Match(baseName)).Success)
            {
                combined = true;
                string baseFileName = m.Groups[1].Value;
                string baseFilePath = Path.Combine(basePath, m.Groups[1].Value);
                string[] frameSuffixes = new string[] {"-tl", "-tr", "-bl", "-br"};
                byte[][] frames = new byte[4][];
                int[] widths = new int[4];
                int[] heights = new int[4];
                StringBuilder extraInfo = new StringBuilder();
                for (int i = 0; i < 4; ++i)
                {
                    string testPath = baseFilePath + frameSuffixes[i] + baseExt;
                    if (!File.Exists(testPath))
                    {
                        combined = false;
                        break;
                    }
                    byte[] pal2 = null;
                    try
                    {
                        int frHeight;
                        int frWidth;
                        byte[] frameData = testPath == curFile ? fileData : File.ReadAllBytes(testPath);
                        frames[i] = this.ReadSingleFrame(frameData, testPath, out frWidth, out frHeight, ref pal2);
                        if (palette == null && pal2 != null)
                        {
                            palette = pal2;
                            extraInfo.AppendLine(String.Format("Palette loaded from {0}{1}.pal", baseFileName, frameSuffixes[i]));
                        }
                        widths[i] = frWidth;
                        heights[i] = frHeight;
                    }
                    catch
                    {
                        combined = false;
                        break;
                    }
                    if (pal2 == null || ReferenceEquals(palette, pal2))
                        continue;
                    if (pal2.Length != palette.Length)
                    {
                        combined = false;
                        break;
                    }
                    for (int c = 0; c < 0x300; ++c)
                    {
                        if (palette[i] == pal2[i])
                            continue;
                        combined = false;
                        break;
                    }
                }
                if (combined)
                {
                    this.Combined = true;
                    int halfWidth1 = Math.Max(widths[0], widths[2]);
                    int halfWidth2 = Math.Max(widths[1], widths[3]);
                    int halfHeight1 = Math.Max(heights[0], heights[1]);
                    int halfHeight2 = Math.Max(heights[2], heights[3]);
                    width = halfWidth1 + halfWidth2;
                    height = halfHeight1 + halfHeight2;
                    imageData = new byte[width * height];
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[0], widths[0], heights[0], widths[0], new Rectangle(0, 0, widths[0], heights[0]), null, true);
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[1], widths[1], heights[1], widths[1], new Rectangle(halfWidth1, 0, widths[1], heights[1]), null, true);
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[2], widths[2], heights[2], widths[2], new Rectangle(0, halfHeight1, widths[2], heights[2]), null, true);
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[3], widths[3], heights[3], widths[3], new Rectangle(halfWidth1, halfHeight1, widths[3], heights[3]), null, true);
                    filename = baseFilePath + baseExt;
                    extraInfo.AppendLine("Composed from four files: ");
                    extraInfo.Append("  ");
                    for (int i = 0; i < 4; ++i)
                    {
                        extraInfo.Append(baseFileName).Append(frameSuffixes[i]).Append(baseExt);
                        if (i != 3)
                            extraInfo.Append(", ");
                    }

                    this.ExtraInfo = extraInfo.ToString();
                }
            }
            if (!combined)
            {
                imageData = this.ReadSingleFrame(fileData, sourcePath, out width, out height, ref palette);
            }
            this.m_PaletteLoaded = palette != null;
            if (this.m_PaletteLoaded)
                this.m_Palette = ColorUtils.ReadSixBitPalette(palette);
            else
            {
                string palFile = Path.Combine(basePath, baseName + ".pal");
                FileInfo palInfo;
                if (baseName != null && (palInfo = new FileInfo(palFile)).Exists && palInfo.Length == 0x300)
                {
                    palette = File.ReadAllBytes(palFile);
                    try
                    {
                        this.m_Palette = ColorUtils.ReadSixBitPalette(palette);
                        this.ExtraInfo = "Palette loaded from " + baseName + ".pal";
                        this.m_PaletteLoaded = this.m_Palette != null;
                    }
                    catch (ArgumentException)
                    {
                        this.m_Palette = ColorUtils.ReadEightBitPalette(palette);
                        this.ExtraInfo = "Palette loaded from " + baseName + ".pal";
                        this.m_PaletteLoaded = true;
                    }
                }
                if (this.m_Palette == null)
                    this.m_Palette = PaletteUtils.GenerateGrayPalette(8, null, false);
            }
            this.m_LoadedImage = ImageUtils.BuildImage(imageData, width, height, width, PixelFormat.Format8bppIndexed, this.m_Palette, null);
            this.SetFileNames(filename);
        }

        protected byte[] ReadSingleFrame(byte[] fileData, string sourcePath, out int width, out int height, ref byte[] palette)
        {
            // Specs:
            // 00 - Byte   - Magic marker '01'
            // 01 - Byte   - Palette indicator. 0 or 1
            // 02 - UInt16 - Width
            // 04 - UInt16 - Height
            // 06 - UInt32 - Padding (empty)
            // 0A - Byte[0x300] - Palette (if palette indicator is 1)
            // 30A - Byte[Width*Height] - Data
            int fileDataLength = fileData.Length;
            if (fileDataLength < 6)
                throw new FileTypeLoadException("Not an ICG DMP file.");
            int magic = fileData[0];
            if (magic != 1)
                throw new FileTypeLoadException("Not an ICG DMP file.");
            int hasPalette = fileData[1];
            if (hasPalette > 1)
                throw new FileTypeLoadException("Not an ICG DMP file.");
            bool paletteLoaded = hasPalette == 1;
            width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            uint padding = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 6);
            if (padding != 0)
                throw new FileTypeLoadException("Not an ICG DMP file.");
            int dataStart = 0x0A + hasPalette * 0x300;
            int dataSize = width * height;
            if (paletteLoaded)
            {
                palette = new byte[0x300];
                Array.Copy(fileData, 0x0A, palette, 0, 0x300);
            }
            if (fileDataLength != dataSize + dataStart)
                throw new FileTypeLoadException("Not an ICG DMP file.");
            byte[] imgBytes = new byte[dataSize];
            Array.Copy(fileData, dataStart, imgBytes, 0, dataSize);
            return imgBytes;
        }

        public override Option[] GetSaveOptions(SupportedFileType fileToSave, string targetFileName)
        {
            PerformPreliminaryChecks(fileToSave);
            return new Option[]
            {
                new Option("PAL", OptionInputType.Boolean, "Include palette", fileToSave.NeedsPalette ? "0" : "1"),
            };
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            // Specs:
            // 00 - Byte   - Magic marker '01'
            // 01 - Byte   - Palette indicator. 0 or 1
            // 02 - UInt16 - Width
            // 04 - UInt16 - Height
            // 06 - UInt32 - Padding (empty)
            // 0A - Byte[0x300] - 6-bit palette (if palette indicator is 1)
            // 30A - Byte[Width*Height] - Data

            PerformPreliminaryChecks(fileToSave);
            bool asPaletted = GeneralUtils.IsTrueValue(Option.GetSaveOptionValue(saveOptions, "PAL"));
            int stride;
            byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            int imageLength = imageBytes.Length;
            byte hasPalette = asPaletted ? (byte)1 : (byte)0;
            int dataStart = 0x0A + hasPalette * 0x300;
            byte[] dmpData = new byte[dataStart + imageLength];
            dmpData[0] = 0x01;
            dmpData[1] = hasPalette;
            ArrayUtils.WriteUInt16ToByteArrayLe(dmpData, 2, (ushort)fileToSave.Width);
            ArrayUtils.WriteUInt16ToByteArrayLe(dmpData, 4, (ushort)fileToSave.Height);
            if (asPaletted)
            {
                byte[] palette = ColorUtils.GetSixBitPaletteData(fileToSave.GetColors());
                Array.Copy(palette, 0, dmpData, 0xA, palette.Length);
            }
            Array.Copy(imageBytes, 0, dmpData, dataStart, imageLength);
            return dmpData;
        }

        public static void PerformPreliminaryChecks(SupportedFileType fileToSave)
        {
            // Preliminary checks
            if (fileToSave == null || fileToSave.GetBitmap() == null)
                throw new ArgumentException(ERR_EMPTY_FILE, "fileToSave");
            if (fileToSave.BitsPerPixel != 8)
                throw new ArgumentException(String.Format(ERR_BPP_INPUT_EXACT, 8), "fileToSave");
            FileImgIgcDmp dmp = fileToSave as FileImgIgcDmp;
            if (fileToSave.Width == 640 || fileToSave.Height == 400 && dmp != null && dmp.Combined)
                throw new ArgumentException("To re-save a combined image, split the image into four 320x200 frames and save the frames as separate .dmp files, with the palette saved into the first (the '-tl') file.", "fileToSave");
            if (fileToSave.Width > 320 || fileToSave.Height > 200)
                throw new ArgumentException(ERR_DIMENSIONS_TOO_LARGE, "fileToSave");
        }

    }

}