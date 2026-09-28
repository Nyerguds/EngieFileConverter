using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util.UI.SaveOptions;

namespace Nyerguds.Util.UI
{
    public partial class FrmManagePalettes : Form
    {
        private const string CREATENEW = "Create new...";

        private int bpp;
        private int nrOfColorsPerPal;
        private int nrOfSubPalettes;
        private PaletteDropDownInfo paletteToSave;
        private string paletteSavePath;
        private bool immediateSave = false;
        private Dictionary<string, List<PaletteDropDownInfo>> subPalettes;
        private List<string> removedPalettes;
        private int addedIndex = 0;

        public string Title { get; set; }

        public string SuggestedSaveName { get; set; }

        public PaletteDropDownInfo PaletteToSave
        {
            get { return this.paletteToSave; }
            set
            {
                this.paletteToSave = value;
                this.palReplaceBy.Palette = value == null ? null : value.Colors;
            }
        }

        public FrmManagePalettes(int bpp, string paletteSavePath)
        {
            this.InitializeComponent();
            this.paletteSavePath = paletteSavePath;
            this.bpp = bpp;
            this.nrOfColorsPerPal = 1 << bpp;
            this.nrOfSubPalettes = 256 / this.nrOfColorsPerPal;
            this.lbSubPalettes.Items.Clear();
            this.removedPalettes = new List<string>();
            this.Opacity = 0;
        }

        private void Init()
        {
            this.cmbPalettes.Items.Clear();
            this.subPalettes = new Dictionary<string, List<PaletteDropDownInfo>>();
            FileInfo[] files = new DirectoryInfo(this.paletteSavePath).GetFiles("*.pal").OrderBy(x => x.Name).ToArray();
            int filesLength = files.Length;
            for (int i = 0; i < filesLength; ++i)
            {
                FileInfo file = files[i];
                if (file.Length != 0x300)
                    continue;
                string name = file.Name;
                List<PaletteDropDownInfo> currentSubPals = PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(new FileInfo(file.FullName), true, true, false);
                if (currentSubPals.Count == 0)
                    continue;
                int curBpp = currentSubPals[0].BitsPerPixel;
                if (this.bpp == curBpp)
                {
                    this.subPalettes.Add(name, currentSubPals);
                    if (this.bpp == 4)
                        this.TrimSubPalettes(currentSubPals);
                }
            }
            foreach (string key in this.subPalettes.Keys)
                this.cmbPalettes.Items.Add(key);
            if (this.paletteToSave != null)
            {
                this.cmbPalettes.Items.Add(CREATENEW);
                this.addedIndex = 1;
            }
        }

        private void TrimSubPalettes(List<PaletteDropDownInfo> subPalettes)
        {
            for (int i = subPalettes.Count - 1; i >= 0; i--)
            {
                PaletteDropDownInfo curr = subPalettes[i];
                if (!String.IsNullOrEmpty(curr.Name))
                    break;
                subPalettes.RemoveAt(i);
            }
        }

        private void CmbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cmbPalettes.SelectedIndex < this.subPalettes.Count)
            {
                this.lbSubPalettes.SelectedItem = null;
                int items = this.ListSubPalettes((this.cmbPalettes.SelectedItem ?? String.Empty).ToString());
                //this.lbSubPalettes.Focus();
                this.lbSubPalettes.SelectedIndex = items > 0 ? 0 : -1;
                return;
            }
            // "Create new" item was selected.
            // Give "save as" dialog? Maybe not, it only works from the program's folder. Just name + existence check, then?
            this.lbSubPalettes.Items.Clear();
            string saveName = "newpal.pal";
            if (this.SuggestedSaveName != null)
                saveName = Path.GetFileNameWithoutExtension(this.SuggestedSaveName) + ".pal";
            string newPaletteName = this.GetTextInput("Palette name:", this.Title, saveName, FormStartPosition.CenterParent);
            char[] invalid = Path.GetInvalidFileNameChars();
            bool isNull = newPaletteName == null;
            bool isEmpty = !isNull && newPaletteName.Length == 0;
            bool illegalChars = !isNull && newPaletteName.Any(c => invalid.Contains(c));
            while (isEmpty || illegalChars)
            {
                string message;
                if (isEmpty)
                    message = "Palette needs a name.";
                else
                    message = "Invalid characters in given file name.";
                MessageBox.Show(this, message , this.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (isEmpty)
                    newPaletteName = saveName;
                newPaletteName = this.GetTextInput("Palette name:", this.Title, newPaletteName, FormStartPosition.CenterParent);
                isNull = newPaletteName == null;
                isEmpty = !isNull && newPaletteName.Length == 0;
                illegalChars = !isNull && newPaletteName.Any(c => invalid.Contains(c));
            }
            if (isNull)
            {
                this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                return;
            }
            if (!newPaletteName.EndsWith(".pal"))
                newPaletteName += ".pal";
            string newPath = Path.Combine(this.paletteSavePath, newPaletteName);
            List<PaletteDropDownInfo> newPalInfo;
            int existingpos = this.subPalettes.Keys.ToList().IndexOf(newPaletteName);
            if (existingpos != -1)
            {
                MessageBox.Show(this, "Palette already exists.", this.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.cmbPalettes.SelectedIndex = existingpos;
                return;
            }
            string barePalName = newPaletteName.Substring(0, newPaletteName.Length - 4);
            string iniPath = Path.Combine(this.paletteSavePath, barePalName + ".ini");
            bool iniExists = File.Exists(iniPath);
            if (File.Exists(newPath))
            {
                if (this.bpp == 4)
                {
                    DialogResult dr = DialogResult.Yes;
                    if (!iniExists)
                        dr = MessageBox.Show(this, "Palette already exists as 8-bit palette!\nAre you sure you want to use this for 4-bit palettes?", this.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        if (iniExists)
                        {
                            newPalInfo = PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(new FileInfo(newPath), true, true, false);
                            this.TrimSubPalettes(newPalInfo);
                        }
                        else
                        {
                            newPalInfo = new List<PaletteDropDownInfo>();
                            newPalInfo.Add(new PaletteDropDownInfo(barePalName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), newPaletteName, 0, true, false));
                        }
                        this.subPalettes.Add(newPaletteName, newPalInfo);
                        this.removedPalettes.Remove(newPaletteName);
                        int newIndex = this.subPalettes.Count - 1;
                        this.cmbPalettes.Items[this.subPalettes.Count - 1] = newPaletteName;
                        this.cmbPalettes.Items.Add(CREATENEW);
                        this.cmbPalettes.SelectedIndex = newIndex;
                    }
                    else
                    {
                        this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                    }
                }
                else if (this.bpp == 8)
                {
                    DialogResult dr = DialogResult.Yes;
                    if (iniExists)
                        dr = MessageBox.Show(this, "Palette already exists as 4-bit palette!\nAre you sure you want to use this as 8-bit palette?", this.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        newPalInfo = new List<PaletteDropDownInfo>();
                        newPalInfo.Add(new PaletteDropDownInfo(newPaletteName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), newPaletteName, 0, true, false));
                        this.subPalettes.Add(newPaletteName, newPalInfo);
                        this.removedPalettes.Remove(newPaletteName);
                        int newIndex = this.subPalettes.Count - 1;
                        this.cmbPalettes.Items[newIndex] = newPaletteName;
                        this.cmbPalettes.Items.Add(CREATENEW);
                        this.cmbPalettes.SelectedIndex = newIndex;
                    }
                    else
                    {
                        this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                    }
                }
                else
                {
                    // Can normally never happen; dialog is always for 4 or 8 bit.
                    this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                }
            }
            else
            {
                // Finally, the normal cases.
                newPalInfo = new List<PaletteDropDownInfo>();
                newPalInfo.Add(new PaletteDropDownInfo(this.bpp == 8 ? newPaletteName : barePalName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), newPaletteName, 0, true, false));
                this.subPalettes.Add(newPaletteName, newPalInfo);

                // Rename current "add new" case to the new palette
                this.removedPalettes.Remove(newPaletteName);
                this.cmbPalettes.Items[this.subPalettes.Count - 1] = newPaletteName;
                this.cmbPalettes.Items.Add(CREATENEW);
            }
        }

        private int ListSubPalettes(string filename)
        {
            this.lbSubPalettes.Items.Clear();
            List<PaletteDropDownInfo> subPals;
            if (!this.subPalettes.TryGetValue(filename, out subPals))
                return 0;
            // Trim all unused entries off the end
            for (int i = subPals.Count - 1; i >= 0; i--)
            {
                PaletteDropDownInfo curr = subPals[i];
                if (!String.IsNullOrEmpty(curr.Name))
                    break;
                subPals.RemoveAt(i);
            }
            this.lbSubPalettes.Items.AddRange(subPals.Select(x => (object)x).ToArray());
            return subPals.Count;
        }

        private void BtnRename_Click(object sender, EventArgs e)
        {
            string selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
            if (!this.subPalettes.ContainsKey(selectedPal))
                return;
            PaletteDropDownInfo currentPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal == null || !currentPal.SourceFile.Equals(selectedPal))
                return;
            DialogResult dr = DialogResult.No;
            string newPaletteName;
            do
            {
                newPaletteName = this.GetTextInput("Sub-palette name:", this.Title, currentPal.Name, FormStartPosition.CenterParent);
                if (String.Empty.Equals(newPaletteName))
                    dr = MessageBox.Show(this, "Giving the sub-palette an empty name will remove it\nfrom the FontEditor's palette listing!\n\nAre you sure you want to do that?", this.Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                else
                    dr = DialogResult.Yes;
            }
            while (dr == DialogResult.No && String.Empty.Equals(newPaletteName));
            if (newPaletteName == null)
                return;
            currentPal.Name = newPaletteName;
            this.lbSubPalettes.Items[this.lbSubPalettes.SelectedIndex] = currentPal;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (this.lbSubPalettes.Items.Count >= this.nrOfSubPalettes)
            {
                string message = "Can't add more than " + this.nrOfSubPalettes + " sub-palette" + (this.nrOfSubPalettes == 1 ? String.Empty : "s")
                                 + " in a" + (this.bpp == 8 ? "n" : String.Empty) + " " + this.bpp + " BPP palette.\n\nTo create a new palette file, select \""
                                 + CREATENEW + "\" from the end of the palettes dropdown list.";
                MessageBox.Show(this, message, this.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
            if (!this.subPalettes.ContainsKey(selectedPal))
                return;
            List<PaletteDropDownInfo> subList;
            if (!this.subPalettes.TryGetValue(selectedPal, out subList))
                return;

            DialogResult dr = DialogResult.No;
            string newPaletteName;
            do
            {
                newPaletteName = this.GetTextInput("New sub-palette name:", this.Title, String.Empty, FormStartPosition.CenterParent);
                if (String.Empty.Equals(newPaletteName))
                    dr = MessageBox.Show(this, "Giving the sub-palette an empty name will remove it\nfrom the FontEditor's palette listing!\n\nAre you sure you want to do that?", this.Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                else
                    dr = DialogResult.Yes;
            }
            while (dr == DialogResult.No && String.Empty.Equals(newPaletteName));
            if (newPaletteName == null)
                return;

            int pos = this.lbSubPalettes.SelectedIndex + 1;
            subList.Insert(pos, new PaletteDropDownInfo(newPaletteName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), selectedPal, pos, true, false));
            int subListCount = subList.Count;
            for (int i = 0; i < subListCount; ++i)
            {
                PaletteDropDownInfo info = subList[i];
                info.Entry = i;
                if (this.lbSubPalettes.Items.Count > i)
                    this.lbSubPalettes.Items[i] = info;
                else
                    this.lbSubPalettes.Items.Add(info);
            }
            this.lbSubPalettes.SelectedIndex = pos;
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            string selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
            if (!this.subPalettes.ContainsKey(selectedPal))
                return;
            List<PaletteDropDownInfo> subList;
            if (!this.subPalettes.TryGetValue(selectedPal, out subList))
                return;
            PaletteDropDownInfo currentPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal == null || !currentPal.SourceFile.Equals(selectedPal))
                return;

            if (this.lbSubPalettes.Items.Count == 1)
            {
                string message;
                if (this.nrOfSubPalettes == 1)
                    message = "This will remove this palette.";
                else
                    message= "Removing a palette's last entry will remove the palette.";
                DialogResult dr = MessageBox.Show(this, message + " Are you sure?", this.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.No)
                    return;
                this.subPalettes.Remove(selectedPal);
                this.removedPalettes.Add(selectedPal);
                int index = this.cmbPalettes.SelectedIndex;
                this.cmbPalettes.SelectedIndex = -1;
                this.cmbPalettes.Items.RemoveAt(index);
                this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                return;
            }
            DialogResult dr2 = MessageBox.Show(this, "Are you sure you want to remove this entry?", this.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr2 == DialogResult.No)
                return;
            int entry = currentPal.Entry;
            subList.RemoveAt(this.lbSubPalettes.SelectedIndex);
            this.lbSubPalettes.Items.RemoveAt(this.lbSubPalettes.SelectedIndex);
            int subListCount = subList.Count;
            for (int i = 0; i < subListCount; ++i)
            {
                PaletteDropDownInfo info = subList[i];
                info.Entry = i;
                this.lbSubPalettes.Items[i] = info;
            }
            int indexToSelect = entry;
            if (this.lbSubPalettes.Items.Count <= indexToSelect)
                indexToSelect--;
            this.lbSubPalettes.SelectedIndex = indexToSelect;
        }

        private void LbSubPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            PaletteDropDownInfo selectedSubPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
            Color[] colors;
            if (selectedSubPal == null)
                colors = Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray();
            else
                colors = selectedSubPal.Colors;
            this.palSelectedSubPal.Palette = colors;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (this.paletteToSave != null)
            {
                string selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
                PaletteDropDownInfo currentPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
                bool hasValidSelection = this.cmbPalettes.SelectedIndex > -1 && currentPal != null && selectedPal.Length > 0;
                if (!hasValidSelection || !currentPal.SourceFile.Equals(selectedPal))
                {
                    if (this.immediateSave)
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    MessageBox.Show("Please use the dropdown menu at the top to select a palette to overwrite, or select \"" + CREATENEW + "\" at the bottom to add a new palette.", this.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                string palette = currentPal.SourceFile;
                int entry = currentPal.Entry;
                if (palette == null || entry < 0 || entry >= this.nrOfSubPalettes)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
                FileInfo palfile = new FileInfo(Path.Combine(this.paletteSavePath, palette));
                if (palfile.Exists && palfile.Length == 0x300 && !currentPal.Colors.All(c => c.IsEmpty))
                {
                    MessageBoxButtons mbb = this.immediateSave ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo;
                    string message = "Overwrite this palette entry?";
                    if (this.immediateSave)
                        message += "\n\nPress \"No\" to pick another entry to save to.";

                    DialogResult dr = MessageBox.Show(message, this.Title, mbb, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Cancel)
                    {
                        // Can only happen if an "overwrite?" prompt was cancelled. Abort completely.
                        this.DialogResult = DialogResult.No;
                        this.Close();
                        return;
                    }
                    if (dr == DialogResult.No)
                    {
                        // Only case in which "immediateSave" gets disabled.
                        if (this.immediateSave)
                            this.immediateSave = false;
                        this.DialogResult = DialogResult.None;
                        return;
                    }
                }
                currentPal.Colors = this.paletteToSave.Colors;
                currentPal.ClearRevert();
                // so underlying layer can know which entry to select after reload.
                // Null + OK means "just clear revert on current".
                if (this.immediateSave)
                    this.paletteToSave = null;
                else
                    this.paletteToSave = currentPal;
            }
            // save all changes
            foreach (string palName in this.subPalettes.Keys)
            {
                Color[] thisFullPal;
                FileInfo pfile = new FileInfo(Path.Combine(this.paletteSavePath, palName));
                bool is8bpp = this.bpp == 8;

                string bareName = pfile.Name;
                string inipath = Path.Combine(pfile.DirectoryName, Path.GetFileNameWithoutExtension(bareName)) + ".ini";
                bool iniExists = File.Exists(inipath);
                IniFile paletteConfig = new IniFile(inipath);
                if (pfile.Exists && pfile.Length == 0x300)
                {
                    // Eight bit: if ini exists, and data is specifically identified as 8-bit
                    bool origIsEightBit = iniExists && paletteConfig.GetBoolValue(PaletteDropDownInfo.PALINISECTION, PaletteDropDownInfo.PALINIKEY8BIT, false);
                    byte[] palBytes = File.ReadAllBytes(pfile.FullName);
                    // ...or if no ini exists but the data contains values higher than 6-bit allows.
                    if (!iniExists && palBytes.Any(b => b > 0x3F))
                        origIsEightBit = true;
                    // Read the palette as 8-bit or as 6-bit, as determined above.
                    thisFullPal = origIsEightBit ? ColorUtils.ReadEightBitPalette(palBytes) : ColorUtils.ReadSixBitPalette(palBytes);
                }
                else
                    thisFullPal = Enumerable.Repeat(Color.Black, 256).ToArray();
                List<PaletteDropDownInfo> subPals = this.subPalettes[palName];
                int subPalsCount = subPals.Count;
                bool skip = false;
                for (int i = 0; i < subPalsCount; ++i)
                {
                    PaletteDropDownInfo subpal = subPals[i];
                    if (subpal.BitsPerPixel != this.bpp)
                    {
                        skip = true;
                        break;
                    }
                    Array.Copy(subpal.Colors, 0, thisFullPal, subpal.Entry * this.nrOfColorsPerPal, this.nrOfColorsPerPal);
                }
                if (skip)
                    continue;
                ColorUtils.WriteEightBitPaletteFile(thisFullPal, pfile.FullName, true);
                paletteConfig.WriteBackMode = WriteMode.WRITE_ALL;
                paletteConfig.ClearSectionKeys(PaletteDropDownInfo.PALINISECTION);
                paletteConfig.SetBoolValue(PaletteDropDownInfo.PALINISECTION, PaletteDropDownInfo.PALINIKEYSINGLE, is8bpp);
                paletteConfig.SetBoolValue(PaletteDropDownInfo.PALINISECTION, PaletteDropDownInfo.PALINIKEY8BIT, true);
                if (!is8bpp)
                {
                    for (int i = 0; i < this.nrOfSubPalettes; ++i)
                    {
                        PaletteDropDownInfo subPal = subPals.Find(x => x.Entry == i);
                        if (subPal != null)
                            paletteConfig.SetStringValue(PaletteDropDownInfo.PALINISECTION, i.ToString(), subPal.Name);
                    }
                }
                paletteConfig.WriteIni();
            }
            foreach (string pal in this.removedPalettes)
            {
                string palFile = Path.Combine(this.paletteSavePath, pal);
                string iniFile = Path.Combine(this.paletteSavePath, pal.Substring(0, pal.Length - 4)) + ".ini";
                File.Delete(palFile);
                File.Delete(iniFile);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private string GetTextInput(string prompt, string title, string defaultText, FormStartPosition startPosition)
        {
            SaveOptionInfo soi = new SaveOptionInfo();
            soi.Name = null;
            Option[] saveOption = { new Option("QUE", OptionInputType.String, prompt, defaultText ?? String.Empty) };
            soi.Properties = saveOption;
            using (FrmOptions opts = new FrmOptions(title, soi))
            {
                opts.StartPosition = startPosition;
                opts.Width = 500;
                opts.Height = opts.OptimalHeight;
                if (opts.ShowDialog(this) != DialogResult.OK)
                    return null;
                return Option.GetSaveOptionValue(opts.GetSaveOptions(), "QUE");
            }

        }

        private void FrmManagePalettes_Load(object sender, EventArgs e)
        {
            this.Init();
            if (this.paletteToSave != null)
            {
                this.palReplaceBy.Palette = this.paletteToSave.Colors;
                string source = this.paletteToSave.SourceFile;
                int entry = this.paletteToSave.Entry;
                if (!String.IsNullOrEmpty(source) && entry != -1 && this.cmbPalettes.Items.Contains(source))
                {
                    // Save an existing palette
                    int index = this.cmbPalettes.Items.IndexOf(source);
                    if (index != -1)
                    {
                        this.cmbPalettes.SelectedIndex = index;
                        this.lbSubPalettes.SelectedIndex = entry;
                        this.immediateSave = true;
                        this.BtnOk_Click(sender, e);
                        // If immediateSave is still enabled, window should close right away.
                        // Abort function so it won't go on to the palette inits, sicne they'll fail.
                        if (this.immediateSave)
                            return;
                    }
                    // Continue after "cancel" was pressed.
                    this.Opacity = 1;
                }
                else
                {
                    // Save a generated palette or a palette from a file
                    this.Opacity = 1;
                    this.cmbPalettes.SelectedIndex = -1; //this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                }
                PalettePanel.InitPaletteControl(this.bpp, this.palReplaceBy, this.paletteToSave.Colors, 74);
            }
            else
            {
                // No save functions. Management only.
                this.lblReplaceBy.Visible = false;
                this.palReplaceBy.Visible = false;
                this.btnAdd.Visible = false;
                this.Text = "Manage palettes";
                this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count - this.addedIndex > 0 ? 0 : -1;
                this.Opacity = 1;
            }
            if (this.bpp == 8)
            {
                this.btnRename.Enabled = false;
                this.btnAdd.Enabled = false;
            }
            PalettePanel.InitPaletteControl(this.bpp, this.palSelectedSubPal, this.palSelectedSubPal.Palette, 74);
        }

        private void FrmManagePalettes_Shown(object sender, EventArgs e)
        {
            if (this.cmbPalettes.SelectedIndex != -1)
                this.lbSubPalettes.Focus();
        }
    }
}