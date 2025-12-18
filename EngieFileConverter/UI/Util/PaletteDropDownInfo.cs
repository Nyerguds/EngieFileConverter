using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;

namespace Nyerguds.Util.UI
{
    public class PaletteDropDownInfo
    {
        public const String PALINISECTION = "Palette";
        public const String PALINIKEY8BIT = "IsEightBit";
        public const String PALINIKEYSINGLE = "IsSinglePalette";

        public String Name { get; set; }
        public Color[] Colors { get; set; }
        public Color[] ColorBackup { get; private set; }
        public Int32 BitsPerPixel { get; private set; }
        public String SourceFile { get; private set; }
        public Int32 Entry { get; set; }
        public Boolean PrefixIndex { get; set; }
        public Boolean SuffixSource { get; set; }

        public PaletteDropDownInfo(String name, Int32 bpp, Color[] colors, String sourceFile, Int32 entry, Boolean prefixIndex, Boolean suffixSource)
        {
            this.Name = name;
            this.BitsPerPixel = bpp;
            Int32 expectedcolors = bpp == -1? 0 : 1 << bpp;
            Color[] palette = new Color[expectedcolors];
            Int32 copiedColors = Math.Min(colors.Length, expectedcolors);
            Array.Copy(colors, palette, copiedColors);
            for (Int32 i = copiedColors; i < expectedcolors; ++i)
                palette[i] = Color.Black;
            this.Colors = palette;
            this.ColorBackup = ArrayUtils.CloneArray(palette);
            this.SourceFile = sourceFile;
            this.Entry = entry;
            this.PrefixIndex = prefixIndex;
            this.SuffixSource = suffixSource;
        }


        public Boolean IsChanged(Boolean[] currentTypeTransMask)
        {
            Color[] compareArr = ArrayUtils.CloneArray(this.ColorBackup);
            PaletteUtils.ApplyPalTransparencyMask(compareArr, currentTypeTransMask);
            return !compareArr.SequenceEqual(this.Colors);
        }

        public void Revert(Boolean[] currentTypeTransMask)
        {
            Array.Copy(this.ColorBackup, this.Colors, this.Colors.Length);
            PaletteUtils.ApplyPalTransparencyMask(this.Colors, currentTypeTransMask);
        }

        public void ClearRevert()
        {
            Array.Copy(this.Colors, this.ColorBackup, this.Colors.Length);
        }

        public override String ToString()
        {
            String name = String.Empty;
            if (this.PrefixIndex)
                name += this.Entry.ToString("D2") + " ";
            name += this.Name;
            if (this.SuffixSource)
                name += " (" + this.SourceFile + " #" + this.Entry + ")";
            return name;
        }

        public static List<PaletteDropDownInfo> LoadSubPalettesInfoFromPalette(String filename, Boolean listAll, Boolean prefixIndex, Boolean suffixSource)
        {
            FileInfo file = new FileInfo(filename);
            return LoadSubPalettesInfoFromPalette(file, listAll, prefixIndex, suffixSource);
        }

        public static List<PaletteDropDownInfo> LoadSubPalettesInfoFromPalette(FileInfo file, Boolean listAll, Boolean prefixIndex, Boolean suffixSource)
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            try
            {
                if (!file.Exists || file.Length != 0x300)
                    return palettes;
                String bareName = file.Name;
                String inipath = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(bareName)) + ".ini";
                Boolean iniExists = File.Exists(inipath);
                IniFile paletteConfig = new IniFile(inipath);
                // Eight bit: if ini exists, and data is specifically identified as 8-bit
                Boolean ini8BitKeyExists = false;
                Boolean isEightBit = iniExists && paletteConfig.GetBoolValue(PALINISECTION, PALINIKEY8BIT, false, out ini8BitKeyExists);
                Byte[] palBytes = File.ReadAllBytes(file.FullName);
                // ...or if no ini exists but the data contains values higher than 6-bit allows.
                if ((!iniExists || !ini8BitKeyExists) && palBytes.Any(b => b > 0x3F))
                    isEightBit = true;
                // Single palette: if there is either no ini (old 6-bit palette) or the ini specifically says it's a single palette.
                Boolean isSinglePal = !iniExists || paletteConfig.GetBoolValue(PALINISECTION, PALINIKEYSINGLE, false);
                // Read the palette as 8-bit or as 6-bit, as determined above.
                Color[] fullPal = isEightBit ? ColorUtils.ReadEightBitPalette(palBytes) : ColorUtils.ReadSixBitPaletteAsEightBit(palBytes);
                if (!isSinglePal)
                {
                    // Read multiple 16-colour palettes
                    for (Int32 i = 0; i < 16; ++i)
                    {
                        String name = paletteConfig.GetStringValue(PALINISECTION, i.ToString(), null);
                        Boolean hasName = !String.IsNullOrEmpty(name);
                        if (!hasName)
                            name = null;
                        if (listAll && !hasName)
                            name = String.Empty;
                        if (name == null)
                            continue;
                        Color[] subPalette = new Color[16];
                        Array.Copy(fullPal, i * 16, subPalette, 0, 16);
                        palettes.Add(new PaletteDropDownInfo(name, 4, subPalette, bareName, i, prefixIndex, suffixSource));
                    }
                }
                else
                {
                    // Add as one single 256 colour palette
                    palettes.Add(new PaletteDropDownInfo(bareName, 8, fullPal, bareName, 0, false, false));
                }
            }
            catch { /* ignore and continue */ }
            return palettes;
        }
    }
}
