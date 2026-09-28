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
        public const string PALINISECTION = "Palette";
        public const string PALINIKEY8BIT = "IsEightBit";
        public const string PALINIKEYSINGLE = "IsSinglePalette";

        public string Name { get; set; }
        public Color[] Colors { get; set; }
        public Color[] ColorBackup { get; private set; }
        public int BitsPerPixel { get; private set; }
        public string SourceFile { get; private set; }
        public int Entry { get; set; }
        public bool PrefixIndex { get; set; }
        public bool SuffixSource { get; set; }

        public PaletteDropDownInfo(string name, int bpp, Color[] colors, string sourceFile, int entry, bool prefixIndex, bool suffixSource)
        {
            this.Name = name;
            this.BitsPerPixel = bpp;
            int expectedcolors = bpp == -1? 0 : 1 << bpp;
            Color[] palette = new Color[expectedcolors];
            int copiedColors = Math.Min(colors.Length, expectedcolors);
            Array.Copy(colors, palette, copiedColors);
            for (int i = copiedColors; i < expectedcolors; ++i)
                palette[i] = Color.Black;
            this.Colors = palette;
            this.ColorBackup = ArrayUtils.CloneArray(palette);
            this.SourceFile = sourceFile;
            this.Entry = entry;
            this.PrefixIndex = prefixIndex;
            this.SuffixSource = suffixSource;
        }


        public bool IsChanged(bool[] currentTypeTransMask)
        {
            Color[] compareArr = ArrayUtils.CloneArray(this.ColorBackup);
            PaletteUtils.ApplyPalTransparencyMask(compareArr, currentTypeTransMask);
            return !compareArr.SequenceEqual(this.Colors);
        }

        public void Revert(bool[] currentTypeTransMask)
        {
            Array.Copy(this.ColorBackup, this.Colors, this.Colors.Length);
            PaletteUtils.ApplyPalTransparencyMask(this.Colors, currentTypeTransMask);
        }

        public void ClearRevert()
        {
            Array.Copy(this.Colors, this.ColorBackup, this.Colors.Length);
        }

        public override string ToString()
        {
            string name = String.Empty;
            if (this.PrefixIndex)
                name += this.Entry.ToString("D2") + " ";
            name += this.Name;
            if (this.SuffixSource)
                name += " (" + this.SourceFile + " #" + this.Entry + ")";
            return name;
        }

        public static List<PaletteDropDownInfo> LoadSubPalettesInfoFromPalette(string filename, bool listAll, bool prefixIndex, bool suffixSource)
        {
            FileInfo file = new FileInfo(filename);
            return LoadSubPalettesInfoFromPalette(file, listAll, prefixIndex, suffixSource);
        }

        public static List<PaletteDropDownInfo> LoadSubPalettesInfoFromPalette(FileInfo file, bool listAll, bool prefixIndex, bool suffixSource)
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            try
            {
                if (!file.Exists || file.Length != 0x300)
                    return palettes;
                string bareName = file.Name;
                string inipath = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(bareName)) + ".ini";
                bool iniExists = File.Exists(inipath);
                IniFile paletteConfig = new IniFile(inipath);
                // Eight bit: if ini exists, and data is specifically identified as 8-bit
                bool ini8BitKeyExists = false;
                bool isEightBit = iniExists && paletteConfig.GetBoolValue(PALINISECTION, PALINIKEY8BIT, false, out ini8BitKeyExists);
                byte[] palBytes = File.ReadAllBytes(file.FullName);
                // ...or if no ini exists but the data contains values higher than 6-bit allows.
                if ((!iniExists || !ini8BitKeyExists) && palBytes.Any(b => b > 0x3F))
                    isEightBit = true;
                // Single palette: if there is either no ini (old 6-bit palette) or the ini specifically says it's a single palette.
                bool isSinglePal = !iniExists || paletteConfig.GetBoolValue(PALINISECTION, PALINIKEYSINGLE, false);
                // Read the palette as 8-bit or as 6-bit, as determined above.
                Color[] fullPal = isEightBit ? ColorUtils.ReadEightBitPalette(palBytes) : ColorUtils.ReadSixBitPalette(palBytes);
                if (!isSinglePal)
                {
                    // Read multiple 16-color palettes
                    for (int i = 0; i < 16; ++i)
                    {
                        string name = paletteConfig.GetStringValue(PALINISECTION, i.ToString(), null);
                        bool hasName = !String.IsNullOrEmpty(name);
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
                    // Add as one single 256 color palette
                    palettes.Add(new PaletteDropDownInfo(bareName, 8, fullPal, bareName, 0, false, false));
                }
            }
            catch { /* ignore and continue */ }
            return palettes;
        }
    }
}
