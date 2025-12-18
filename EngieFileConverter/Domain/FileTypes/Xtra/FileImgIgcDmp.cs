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

        public override String IdCode { get { return "IgDmp"; } }
        /// <summary>Very short code name for this type.</summary>
        public override String ShortTypeName { get { return "Interactive Girls DMP file"; } }
        public override String[] FileExtensions { get { return new String[] { "dmp" }; } }
        public override String ShortTypeDescription { get { return "Interactive Girls DMP image file"; } }
        public override Boolean NeedsPalette { get { return !this.m_PaletteLoaded; } }
        public override Int32 BitsPerPixel { get { return 8; } }
        protected Boolean m_PaletteLoaded;
        public Boolean Combined { get; private set; }

        public override void LoadFile(Byte[] fileData)
        {
            this.LoadFromFileData(fileData, null);
        }

        public override void LoadFile(Byte[] fileData, String filename)
        {
            this.LoadFromFileData(fileData, filename);
        }

        protected void LoadFromFileData(Byte[] fileData, String sourcePath)
        {
            String filename = sourcePath;
            String basePath = Path.GetDirectoryName(sourcePath);
            String baseName = Path.GetFileNameWithoutExtension(sourcePath);
            String baseExt = Path.GetExtension(sourcePath);
            String curFile = Path.Combine(basePath, Path.GetFileName(filename));
            Byte[] palette = null;
            Int32 width = -1;
            Int32 height = -1;
            Byte[] imageData = null;
            Regex nameEnd = new Regex("^(.*?)-[tb][lr]$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Match m;
            Boolean combined = false;
            if (baseName != null && baseExt != null && (m = nameEnd.Match(baseName)).Success)
            {
                combined = true;
                String baseFileName = m.Groups[1].Value;
                String baseFilePath = Path.Combine(basePath, m.Groups[1].Value);
                String[] frameSuffixes = new String[] {"-tl", "-tr", "-bl", "-br"};
                Byte[][] frames = new Byte[4][];
                Int32[] widths = new Int32[4];
                Int32[] heights = new Int32[4];
                StringBuilder extraInfo = new StringBuilder();
                for (Int32 i = 0; i < 4; ++i)
                {
                    String testPath = baseFilePath + frameSuffixes[i] + baseExt;
                    if (!File.Exists(testPath))
                    {
                        combined = false;
                        break;
                    }
                    Byte[] pal2 = null;
                    try
                    {
                        Int32 frHeight;
                        Int32 frWidth;
                        Byte[] frameData = testPath == curFile ? fileData : File.ReadAllBytes(testPath);
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
                    for (Int32 c = 0; c < 0x300; ++c)
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
                    Int32 halfWidth1 = Math.Max(widths[0], widths[2]);
                    Int32 halfWidth2 = Math.Max(widths[1], widths[3]);
                    Int32 halfHeight1 = Math.Max(heights[0], heights[1]);
                    Int32 halfHeight2 = Math.Max(heights[2], heights[3]);
                    width = halfWidth1 + halfWidth2;
                    height = halfHeight1 + halfHeight2;
                    imageData = new Byte[width * height];
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[0], widths[0], heights[0], widths[0], new Rectangle(0, 0, widths[0], heights[0]), null, true);
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[1], widths[1], heights[1], widths[1], new Rectangle(halfWidth1, 0, widths[1], heights[1]), null, true);
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[2], widths[2], heights[2], widths[2], new Rectangle(0, halfHeight1, widths[2], heights[2]), null, true);
                    ImageUtils.PasteOn8bpp(imageData, width, height, width, frames[3], widths[3], heights[3], widths[3], new Rectangle(halfWidth1, halfHeight1, widths[3], heights[3]), null, true);
                    filename = baseFilePath + baseExt;
                    extraInfo.AppendLine("Composed from four files: ");
                    extraInfo.Append("  ");
                    for (Int32 i = 0; i < 4; ++i)
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
                this.m_Palette = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPalette(palette, 0, 0x100));
            else
            {
                String palFile = Path.Combine(basePath, baseName + ".pal");
                FileInfo palInfo;
                if (baseName != null && (palInfo = new FileInfo(palFile)).Exists && palInfo.Length == 0x300)
                {
                    palette = File.ReadAllBytes(palFile);
                    try
                    {
                        this.m_Palette = ColorUtils.ReadSixBitPaletteAsEightBit(palette);
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

        protected Byte[] ReadSingleFrame(Byte[] fileData, String sourcePath, out Int32 width, out Int32 height, ref Byte[] palette)
        {
            // Specs:
            // 00 - Byte   - Magic marker '01'
            // 01 - Byte   - Palette indicator. 0 or 1
            // 02 - UInt16 - Width
            // 04 - UInt16 - Height
            // 06 - UInt32 - Padding (empty)
            // 0A - Byte[0x300] - Palette (if palette indicator is 1)
            // 30A - Byte[Width*Height] - Data
            Int32 fileDataLength = fileData.Length;
            if (fileDataLength < 6)
                throw new FileTypeLoadException("Not an ICG DMP file!");
            Int32 magic = fileData[0];
            if (magic != 1)
                throw new FileTypeLoadException("Not an ICG DMP file!");
            Int32 hasPalette = fileData[1];
            if (hasPalette > 1)
                throw new FileTypeLoadException("Not an ICG DMP file!");
            Boolean paletteLoaded = hasPalette == 1;
            width = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 2);
            height = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, 4);
            UInt32 padding = ArrayUtils.ReadUInt32FromByteArrayLe(fileData, 6);
            if (padding != 0)
                throw new FileTypeLoadException("Not an ICG DMP file!");
            Int32 dataStart = 0x0A + hasPalette * 0x300;
            Int32 dataSize = width * height;
            if (paletteLoaded)
            {
                palette = new Byte[0x300];
                Array.Copy(fileData, 0x0A, palette, 0, 0x300);
            }
            if (fileDataLength != dataSize + dataStart)
                throw new FileTypeLoadException("Not an ICG DMP file!");
            Byte[] imgBytes = new Byte[dataSize];
            Array.Copy(fileData, dataStart, imgBytes, 0, dataSize);
            return imgBytes;
        }

        public override SaveOption[] GetSaveOptions(SupportedFileType fileToSave, String targetFileName)
        {
            PerformPreliminaryChecks(fileToSave);
            return new SaveOption[]
            {
                new SaveOption("PAL", SaveOptionType.Boolean, "Include palette", fileToSave.NeedsPalette ? "0" : "1"),
            };
        }

        public override Byte[] SaveToBytesAsThis(SupportedFileType fileToSave, SaveOption[] saveOptions)
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
            Boolean asPaletted = GeneralUtils.IsTrueValue(SaveOption.GetSaveOptionValue(saveOptions, "PAL"));
            Int32 stride;
            Byte[] imageBytes = ImageUtils.GetImageData(fileToSave.GetBitmap(), out stride, true);
            Int32 imageLength = imageBytes.Length;
            Byte hasPalette = asPaletted ? (Byte)1 : (Byte)0;
            Int32 dataStart = 0x0A + hasPalette * 0x300;
            Byte[] dmpData = new Byte[dataStart + imageLength];
            dmpData[0] = 0x01;
            dmpData[1] = hasPalette;
            ArrayUtils.WriteInt16ToByteArrayLe(dmpData, 2, fileToSave.Width);
            ArrayUtils.WriteInt16ToByteArrayLe(dmpData, 4, fileToSave.Height);
            if (asPaletted)
            {
                Byte[] palette = ColorUtils.GetSixBitPaletteData(ColorUtils.GetSixBitColorPalette(fileToSave.GetColors()));
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
                throw new ArgumentException(ERR_8BPP_INPUT, "fileToSave");
            FileImgIgcDmp dmp = fileToSave as FileImgIgcDmp;
            if (fileToSave.Width == 640 || fileToSave.Height == 400 && dmp != null && dmp.Combined)
                throw new ArgumentException("To re-save a combined image, split the image into four 320x200 frames and save the frames as separate .dmp files, with the palette saved into the first (the '-tl') file.", "fileToSave");
            if (fileToSave.Width > 320 || fileToSave.Height > 200)
                throw new ArgumentException(ERR_IMAGE_TOO_LARGE, "fileToSave");
        }

    }

}