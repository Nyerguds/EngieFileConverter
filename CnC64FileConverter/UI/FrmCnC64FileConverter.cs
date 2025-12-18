using CnC64FileConverter.Domain.HeightMap;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CnC64FileConverter.UI
{
    public partial class FrmCnC64FileConverter : Form
    {
        private const String PROG_NAME = "C&C64 File Converter";
        private const String PROG_AUTHOR = "Created by Nyerguds";
        private const Int32 PALETTE_DIM = 226;

        private String m_StartupParamPath;
        private Boolean m_Loading;
        private List<PaletteDropDownInfo> m_DefaultPalettes;
        private List<PaletteDropDownInfo> m_ReadPalettes;
        private Int32[] m_CustomColors;
        private SupportedFileType m_LoadedFile;
        private Color m_BackgroundFillColor = Color.Fuchsia;


        private SupportedFileType GetShownFile()
        {
            if (this.m_LoadedFile == null)
                return null;
            Boolean hasFrames = m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            return hasFrames && numFrame.Value != -1 ? (m_LoadedFile.Frames.Length > numFrame.Value? m_LoadedFile.Frames[(Int32)numFrame.Value] : null) : m_LoadedFile;
        }

        public FrmCnC64FileConverter()
        {
            this.InitializeComponent();
            this.Text = GetTitle(true);
            this.picImage.BackColor = m_BackgroundFillColor;
            this.lblTransparentColorVal.BackColor = m_BackgroundFillColor;
            PalettePanel.InitPaletteControl(8, palColorViewer, new Color[0], PALETTE_DIM);
            this.palColorViewer.MaxColors = 0;
            this.m_DefaultPalettes = LoadDefaultPalettes();
            this.m_ReadPalettes = LoadExtraPalettes();
            this.RefreshPalettes(false, false);
            ContextMenu cmCopyPreview = new ContextMenu();
            MenuItem mniCopy = new MenuItem("Copy");
            mniCopy.Click += new EventHandler(this.PicImage_CopyPreview);
            cmCopyPreview.MenuItems.Add(mniCopy);
            this.picImage.ContextMenu = cmCopyPreview;
        }

        public static String GetTitle(Boolean withAuthor)
        {
            String title = PROG_NAME + " " + GeneralUtils.ProgramVersion();
            if (withAuthor)
                title += " - " + PROG_AUTHOR;
            return title;
        }


        public FrmCnC64FileConverter(String[] args) : this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                this.m_StartupParamPath = args[0];
        }

        public List<PaletteDropDownInfo> LoadDefaultPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            palettes.Add(new PaletteDropDownInfo("Black/White", 1, new Color[]{Color.Black, Color.White}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("White/Black", 1, new Color[] { Color.White, Color.Black }, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Black/Red", 1, new Color[] { Color.Black, Color.Red}, null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 4, PaletteUtils.GenerateGrayPalette(4, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 4, PaletteUtils.GenerateRainbowPalette(4, false, false, true, 0, 160.0 / 240.0), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 4, PaletteUtils.GenerateGrayPalette(4, null, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 4, PaletteUtils.GenerateRainbowPalette(4, -1, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 4, PaletteUtils.GenerateDefWindowsPalette(4, false, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 8, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 8, PaletteUtils.GenerateRainbowPalette(8, -1, null, true, 0, 160, true), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 8, PaletteUtils.GenerateGrayPalette(8, false, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 8, PaletteUtils.GenerateRainbowPalette(8, -1, null, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 8, PaletteUtils.GenerateDefWindowsPalette(8, false, false), null, -1, false, false));
            return palettes;
        }

        private void PicImage_CopyPreview(object sender, EventArgs e)
        {
            this.CopyToClipboard();
        }

        private void tsmiCopy_Click(object sender, EventArgs e)
        {
            this.CopyToClipboard();
        }

        private void CopyToClipboard()
        {
            Image image = this.picImage.Image;
            if (image == null)
                return;
            Bitmap bm = ImageUtils.PaintOn32bpp(image, this.m_BackgroundFillColor);
            ClipboardImage.SetClipboardImage(image, bm, null);
        }

        private void Frm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Frm_DragDrop(object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            String path = files[0];
            SupportedFileType[] possibleTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, path);
            this.LoadFile(path, null, possibleTypes);
        }

        private void LoadFile(String path, SupportedFileType selectedType, SupportedFileType[] possibleTypes)
        {
            this.m_Loading = true;
            try
            {
                FileTypeLoadException error = null;
                Boolean isEmpty = false;
                try
                {
                    try
                    {
                        isEmpty = new FileInfo(path).Length == 0;
                    }
                    catch(Exception e)
                    {
                        error = new FileTypeLoadException("Could not access file!\n\n" + e.Message, e);
                        this.m_LoadedFile = null;
                    }
                    if (!isEmpty && error == null)
                    {
                        if (selectedType != null)
                        {
                            try
                            {
                                selectedType.LoadFile(path);
                                this.m_LoadedFile = selectedType;
                            }
                            catch (FileTypeLoadException e)
                            {
                                m_LoadedFile = null;
                                e.AttemptedLoadedType = selectedType.ShortTypeName;
                                error = e;
                                if (possibleTypes != null && possibleTypes.Length > 1)
                                    selectedType = null;
                            }
                        }
                        if (selectedType == null)
                        {
                            List<FileTypeLoadException> loadErrors;
                            this.m_LoadedFile = SupportedFileType.LoadFileAutodetect(path, possibleTypes, out loadErrors, error != null);
                            if (this.m_LoadedFile != null)
                                error = null;
                            else
                            {
                                if (error != null)
                                    loadErrors.Insert(0, error);
                                String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                                String filename = path == null ? String.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                                MessageBox.Show(this, "File type" + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = new FileTypeLoadException(ex.Message, ex);
                    this.m_LoadedFile = null;
                }
                if (isEmpty || error == null)
                {
                    SupportedFileType detectSource = m_LoadedFile;
                    if (isEmpty && possibleTypes.Length == 1)
                        detectSource = possibleTypes[0];
                    SupportedFileType frames = this.CheckForFrames(path, detectSource);
                    if (Object.ReferenceEquals(frames, detectSource) && isEmpty)
                        m_LoadedFile = null;
                    else
                        m_LoadedFile = frames;
                }
                ReloadUi(true);
                if (error != null)
                    MessageBox.Show(this, "File loading failed: " + error.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (this.m_LoadedFile == null && isEmpty)
                    MessageBox.Show(this, "File loading failed: The file is empty!", GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.m_Loading = false;
            }
        }

        /// <summary>
        /// Checks if the current type is frameless, and if so, checks if it is part of a numerical range of frames.
        /// If so, and all found frames can be loaded as the identified type, the program asks to load all the files
        /// as frames, instead of loading one file as image.
        /// </summary>
        /// <param name="path">path that was opened.</param>
        /// <param name="currentType">Currently loaded file from the given path.</param>
        /// <returns>A generic SupportedType object filled with the frames, or the original 'currentType' object if the detect failed or was aborted.</returns>
        private SupportedFileType CheckForFrames(String path, SupportedFileType currentType)
        {
            String minName;
            String maxName;
            Boolean hasEmptyFrames;
            FileFrames fr = FileFrames.CheckForFrames(path, currentType, out minName, out maxName, out hasEmptyFrames);
            if (fr == null)
                return currentType;
            String emptywarning = hasEmptyFrames ? "\nSome of these frames are empty files. Not every save format supports empty frames." : String.Empty;
            String message = "The file appears to be part of a range (" + minName + " - " + maxName + ")." + emptywarning + "\n\nDo you wish to load it as frames?";
            if (MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                return fr;
            return currentType;
        }

        private void ReloadUi(Boolean fromNewFile)
        {
            Boolean hasFrames = m_LoadedFile != null && m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            this.lblFrame.Enabled = hasFrames;
            this.numFrame.Enabled = hasFrames;
            this.numFrame.Minimum = -1;
            if (!hasFrames)
            {
                this.numFrame.Value = -1;
                this.numFrame.Maximum = -1;
                this.lblNrOfFrames.Visible = false;
            }
            else
            {
                if (fromNewFile)
                    this.numFrame.Value = -1;
                Int32 last = m_LoadedFile.Frames.Length - 1;
                this.numFrame.Maximum = last;
                this.lblNrOfFrames.Visible = true;
                this.lblNrOfFrames.Text = "/ " + last;
                if (last >= 0 && !this.m_LoadedFile.IsFramesContainer)
                    this.numFrame.Minimum = 0;
            }
            SupportedFileType loadedFile = this.GetShownFile();
            Boolean hasFile = loadedFile != null;
            this.tsmiSave.Enabled = hasFile;
            this.tsmiSaveFrames.Enabled = hasFile && (m_LoadedFile.FileClass & FileClass.FrameSet) != 0;
            this.tsmiCopy.Enabled = hasFile;
            // C&C64 toolsets
            this.tsmiToHeightMap.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiToPlateaus.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiToHeightMapAdv.Enabled = loadedFile is FileMapWwCc1Pc;
            this.tsmiTo65x65HeightMap.Enabled = hasFile && loadedFile.GetBitmap() != null && loadedFile.Width == 64 && loadedFile.Height == 64;
            // 4 is the supertype; 8 the derivative
            this.tsmiTilesetsToFrames.Enabled = loadedFile is FileTilesWwCc1N64Bpp4;
            this.tsmiTilesetsToTilesetFiles.Enabled = loadedFile is FileTilesWwCc1N64Bpp4;

            if (!hasFile)
            {
                String emptystr = "---";
                this.lblValFilename.Text = emptystr;
                this.lblValType.Text = emptystr;
                this.toolTip1.SetToolTip(lblValType, null);
                this.lblValWidth.Text = emptystr;
                this.lblValHeight.Text = emptystr;
                this.lblValColorFormat.Text = emptystr;
                this.lblValColorsInPal.Text = emptystr;
                this.lblValInfo.Text = String.Empty;
                this.cmbPalettes.Enabled = false;
                this.cmbPalettes.SelectedIndex = 0;
                this.btnResetPalette.Enabled = false;
                this.btnSavePalette.Enabled = false;
                this.picImage.Image = null;
                this.RefreshImage();
                PalettePanel.InitPaletteControl(8, palColorViewer, new Color[0], PALETTE_DIM);
                this.palColorViewer.MaxColors = 0;
                return;
            }
            Int32 bpc = loadedFile.BitsPerColor;
            this.lblValFilename.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.LoadedFileName);
            this.lblValType.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ShortTypeDescription);
            this.toolTip1.SetToolTip(lblValType, lblValType.Text);
            this.lblValWidth.Text = loadedFile.Width.ToString();
            this.lblValHeight.Text = loadedFile.Height.ToString();
            this.lblValColorFormat.Text = bpc == 0 ? "N/A" : (bpc + " BPP" + (bpc == 4 || bpc == 8 ? " (paletted)" : String.Empty));
            Color[] palette = loadedFile.GetColors();
            Int32 exposedColours = loadedFile.ColorsInPalette;
            Int32 actualColors = palette == null? 0 : palette.Length;
            Boolean needsPalette = exposedColours != actualColors;
            this.lblValColorsInPal.Text = actualColors + (needsPalette ? " (" + exposedColours + " in file)" : String.Empty);
            this.lblValInfo.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ExtraInfo);
            this.cmbPalettes.Enabled = needsPalette;
            this.picImage.Image = loadedFile.GetBitmap();
            this.RefreshPalettes(false, false);
            if (needsPalette && fromNewFile)
            {
                this.CmbPalettes_SelectedIndexChanged(null, null);
            }
            else
            {
                this.RefreshImage();
                this.RefreshColorControls();
            }
        }

        private ColorStatus GetColorStatus()
        {
            SupportedFileType loadedFile = this.GetShownFile();
            if (loadedFile == null)
                return ColorStatus.None;
            Color[] cols = loadedFile.GetColors();
            if (cols == null || cols.Length == 0)
                return ColorStatus.None;
            if (cols.Length != loadedFile.ColorsInPalette)
                return ColorStatus.External;
            if (loadedFile.ColorsInPalette == 0)
                return ColorStatus.None;
            return ColorStatus.Internal;
        }

        public List<PaletteDropDownInfo> GetPalettes(Int32 bpp, Boolean reloadFiles)
        {
            List<PaletteDropDownInfo> allPalettes = this.m_DefaultPalettes.Where(p => p.BitsPerPixel == bpp).ToList();
            if (reloadFiles)
                this.m_ReadPalettes = LoadExtraPalettes();
            allPalettes.AddRange(this.m_ReadPalettes.Where(p => p.BitsPerPixel == bpp));
            return allPalettes;
        }

        public List<PaletteDropDownInfo> LoadExtraPalettes()
        {
            List<PaletteDropDownInfo> palettes = new List<PaletteDropDownInfo>();
            String appFolder = Path.GetDirectoryName(Application.ExecutablePath);
            FileInfo[] files = new DirectoryInfo(appFolder).GetFiles("*.pal").OrderBy(x => x.Name).ToArray();
            foreach (FileInfo file in files)
                palettes.AddRange(PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(file, false, false, true));
            return palettes;
        }

        private void FrmCnC64FileConverter_Shown(object sender, EventArgs e)
        {
            if (this.m_StartupParamPath != null)
                this.LoadFile(this.m_StartupParamPath, null, null);
            else
                this.ReloadUi(true);
        }

        private void TsmiSave_Click(object sender, EventArgs e)
        {
            this.Save(false);
        }

        private void TsmiSaveFrames_Click(object sender, EventArgs e)
        {
            this.Save(true);
        }

        private void Save(Boolean frames)
        {
            if (this.m_LoadedFile == null)
                return;
            SupportedFileType selectedItem;
            Boolean hasFrames = m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            Boolean isFrame = !frames && hasFrames && numFrame.Value != -1;
            SupportedFileType loadedFile = isFrame ? m_LoadedFile.Frames[(Int32)numFrame.Value] : m_LoadedFile;
            Type selectType = frames ? typeof(FileImagePng) : loadedFile.GetType();
            Type[] saveTypes = SupportedFileType.SupportedSaveTypes;
            FileClass loadedFileType = loadedFile.FileClass;
            FileClass frameFileType = FileClass.None;
            if (hasFrames && !isFrame)
            {
                SupportedFileType first = m_LoadedFile.Frames.FirstOrDefault(x => x != null && x.GetBitmap() != null);
                if (first != null)
                    frameFileType = first.FileClass;
            }

            List<Type> filteredTypes = new List<Type>();
            foreach (Type saveType in saveTypes)
            {
                SupportedFileType tmpsft = (SupportedFileType)Activator.CreateInstance(saveType);
                FileClass diff = tmpsft.InputFileClass & (frames? frameFileType : loadedFileType);
                if ((diff & ~FileClass.FrameSet) != 0 || (!frames && (tmpsft.FrameInputFileClass & frameFileType) != 0))
                    filteredTypes.Add(saveType);
            }
            if (filteredTypes.Count == 0)
            {
                String message = "No types found for saving this data.";
                if (hasFrames && !isFrame)
                    message += "\nTry exporting as frames instead.";
                MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            saveTypes = filteredTypes.ToArray();
            if (!saveTypes.Contains(selectType))
            {
                Type newSelectType = saveTypes.FirstOrDefault(x => selectType.IsSubclassOf(x));
                if (newSelectType == null)
                    newSelectType = saveTypes.FirstOrDefault(x => x.IsSubclassOf(selectType));
                if (newSelectType == null)
                    newSelectType = typeof(FileImagePng);
                selectType = newSelectType;
            }
            String filename = FileDialogGenerator.ShowSaveFileFialog(this, selectType, saveTypes, true, loadedFile.LoadedFile, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            try
            {
                SaveOption[] saveOptions = selectedItem.GetSaveOptions(m_LoadedFile, filename);
                if (saveOptions != null && saveOptions.Length > 0)
                {
                    SaveOptionInfo soi = new SaveOptionInfo();
                    soi.Name = "Extra save options for " + selectedItem.ShortTypeDescription;
                    soi.Properties = saveOptions;
                    FrmExtraOptions extraopts = new FrmExtraOptions();
                    extraopts.Init(soi);
                    if (extraopts.ShowDialog(this) != DialogResult.OK)
                        return;
                    saveOptions = extraopts.GetSaveOptions();
                }
                if (!frames)
                    selectedItem.SaveAsThis(loadedFile, filename, saveOptions);
                else
                {
                    if (loadedFile.Frames == null)
                        return;
                    String framename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
                    String extension = Path.GetExtension(filename);
                    for (Int32 i = 0; i < loadedFile.Frames.Length; i++)
                    {
                        SupportedFileType frame = loadedFile.Frames[i];
                        String framePath = framename + "-" + i.ToString("D5") + extension;
                        if (frame.GetBitmap() == null) // Allow empty frames as empty files.
                            File.WriteAllBytes(framePath, new Byte[0]);
                        else
                            selectedItem.SaveAsThis(frame, framePath, saveOptions);
                    }
                }
            }
            catch (NotSupportedException ex)
            {
                String message = "Cannot save " + (frames ? "frame of " : String.Empty) + "type " + loadedFile.ShortTypeName
                    + " as type " + selectedItem.ShortTypeName + (String.IsNullOrEmpty(ex.Message) ? "." : ":\n" + ex.Message);
                MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TsmiExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TsmiOpen_Click(object sender, EventArgs e)
        {
            SupportedFileType selectedItem;
            String openPath = null;
            if (this.m_LoadedFile != null)
                openPath = this.m_LoadedFile.LoadedFile;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, null, SupportedFileType.SupportedOpenTypes, openPath, "images", null, out selectedItem);
            if (filename == null)
                return;
            SupportedFileType[] possibleTypes = null;
            if (selectedItem == null)
                possibleTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(SupportedFileType.AutoDetectTypes, filename);
            else
            {
                SupportedFileType curr = selectedItem;
                Type currType = curr.GetType();
                Type[] subTypes = SupportedFileType.AutoDetectTypes.Where(x => currType.IsAssignableFrom(x)).ToArray();
                if (subTypes.Length > 0 && subTypes[0] != currType)
                {
                    List<SupportedFileType> subTypeObjs = new List<SupportedFileType>(FileDialogGenerator.GetItemsList<SupportedFileType>(subTypes));
                    SupportedFileType[] extNarrowedTypes = FileDialogGenerator.IdentifyByExtension<SupportedFileType>(subTypes, filename);
                    if (extNarrowedTypes.Length == 1)
                        selectedItem = extNarrowedTypes[0];
                    subTypeObjs.RemoveAll(x => x.GetType() == selectedItem.GetType());
                    if (selectedItem.GetType() != currType && subTypeObjs.All(x => x.GetType() != currType))
                        subTypeObjs.Add(curr);
                    possibleTypes = subTypeObjs.ToArray();
                }
            }
            this.LoadFile(filename, selectedItem, possibleTypes);
        }

        private void NumZoom_ValueChanged(object sender, EventArgs e)
        {
            this.RefreshImage();
        }

        private void RefreshImage()
        {
            Bitmap bm = (Bitmap)picImage.Image;
            Boolean loadOk = bm != null;
            picImage.Visible = loadOk;
            picImage.Width = loadOk ? bm.Width * (Int32)numZoom.Value : 100;
            picImage.Height = loadOk ? bm.Height * (Int32)numZoom.Value : 100;
            picImage.Invalidate();
        }

        private void RefreshColorControls()
        {
            SupportedFileType loadedFile = this.GetShownFile();
            Boolean fileLoaded = loadedFile != null;
            ColorStatus cs = GetColorStatus();
            this.btnSavePalette.Enabled = fileLoaded && cs != ColorStatus.None;
            this.cmbPalettes.Enabled = cs == ColorStatus.External;
            // Ignore this if the palette is handled by the dropdown
            Boolean resetEnabled;
            Color[] pal;
            switch (cs)
            {
                case ColorStatus.Internal:
                    resetEnabled = loadedFile.ColorsChanged();
                    pal = loadedFile.GetColors();
                    break;
                case ColorStatus.External:
                    PaletteDropDownInfo currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    resetEnabled = currentPal != null && currentPal.IsChanged();
                    if (fileLoaded)// && currentPal.Colors.Length != loadedFile.GetColors().Length)
                        pal = loadedFile.GetColors();
                    else
                        pal = currentPal.Colors;
                    break;
                default:
                    resetEnabled = false;
                    pal = new Color[0];
                    break;
            }
            this.btnResetPalette.Enabled = resetEnabled;
            PalettePanel.InitPaletteControl(cs == ColorStatus.None ? 8 : loadedFile.BitsPerColor, this.palColorViewer, pal, PALETTE_DIM);
            this.palColorViewer.MaxColors = pal.Length;
        }

        private void PicImage_Click(object sender, EventArgs e)
        {
            this.pnlImageScroll.Focus();
        }

        private void LblTransparentColorVal_Click(object sender, EventArgs e)
        {
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.m_BackgroundFillColor;
            cdl.FullOpen = true;
            cdl.CustomColors = this.m_CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.m_CustomColors = cdl.CustomColors;
            if (res == DialogResult.OK || res == DialogResult.Yes)
            {
                this.m_BackgroundFillColor = cdl.Color;
                this.lblTransparentColorVal.BackColor = m_BackgroundFillColor;
                this.picImage.BackColor = m_BackgroundFillColor;
                this.NumZoom_ValueChanged(null, null);
            }
        }

        private void numFrame_ValueChanged(object sender, EventArgs e)
        {
            if (m_LoadedFile != null && m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0)
                ReloadUi(false);
        }

        private void TsmiToHeightMapAdv_Click(object sender, EventArgs e)
        {
            GenerateHeightMap(true);
        }
        private void TsmiToHeightMap_Click(object sender, EventArgs e)
        {
            GenerateHeightMap(false);
        }

        private void GenerateHeightMap(Boolean selectHeightMap)
        {
            if (!(this.m_LoadedFile is FileMapWwCc1Pc))
                return;
            String loadedPath = this.m_LoadedFile.LoadedFile;
            String baseFileName = Path.Combine(Path.GetDirectoryName(loadedPath), Path.GetFileNameWithoutExtension(loadedPath));
            String pngFileName = baseFileName + ".png";
            Bitmap plateauImage = null;
            if (selectHeightMap)
            {
                SupportedFileType selectedType;
                //String plateauFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename)) + "_lvl.png";
                String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select height levels image", new Type[] { typeof(FileImage) }, pngFileName, "images", null, out selectedType);
                if (filename == null)
                    return;
                try
                {
                    if (selectedType == null)
                        selectedType = new FileImage();
                    selectedType.LoadFile(filename);
                    plateauImage = selectedType.GetBitmap();
                }
                catch(Exception e)
                {
                    MessageBox.Show(this, "Could not load file as " + selectedType.ShortTypeDescription + ":\n\n" + e.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (plateauImage.Width != 64 || plateauImage.Height != 64)
                {
                    MessageBox.Show(this, "Height levels image needs to be 64x64!", GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            FileMapWwCc1Pc map = (FileMapWwCc1Pc)this.m_LoadedFile;
            this.m_LoadedFile = HeightMapGenerator.GenerateHeightMapImage64x64(map, plateauImage, null);
            this.ReloadUi(false);
        }

        private void TsmiTo65x65HeightMap_Click(object sender, EventArgs e)
        {
            if (this.m_LoadedFile == null || this.m_LoadedFile.Width != 64 || this.m_LoadedFile.Height != 64 || !(this.m_LoadedFile is FileImage))
                return;
            String baseFileName = Path.Combine(Path.GetDirectoryName(this.m_LoadedFile.LoadedFile), Path.GetFileNameWithoutExtension(this.m_LoadedFile.LoadedFile));
            String imgFileName = baseFileName + ".img";
            Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(this.m_LoadedFile.GetBitmap());
            //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
            FileImgWwN64 file = new FileImgWwN64();
            file.LoadGrayImage(bm, Path.GetFileName(imgFileName), imgFileName);
            this.m_LoadedFile = file;
            this.ReloadUi(false);
        }

        private void TsmiToPlateaus_Click(object sender, EventArgs e)
        {
            if (!(this.m_LoadedFile is FileMapWwCc1Pc))
                return;
            this.m_LoadedFile = HeightMapGenerator.GeneratePlateauImage64x64((FileMapWwCc1Pc)this.m_LoadedFile, "_lvl");
            this.ReloadUi(false);
        }

        private void CmbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(this.GetColorStatus() != ColorStatus.External)
                return;

            PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
            Color[] targetPal;
            if (currentPal == null)
            {
                if (!btnSavePalette.Enabled)
                    btnSavePalette.Enabled = true;
                targetPal = PaletteUtils.GenerateGrayPalette(8, null, false);
            }
            else
            {
                targetPal = currentPal.Colors;
                Int32 bpp = currentPal.BitsPerPixel;
                if (btnSavePalette.Enabled && bpp == 1)
                    btnSavePalette.Enabled = false;
                else if (!btnSavePalette.Enabled && bpp != 1)
                    btnSavePalette.Enabled = true;
                this.btnResetPalette.Enabled = currentPal.IsChanged();
            }
            this.GetShownFile().SetColors(targetPal);
            RefreshImage();
            this.RefreshColorControls();
        }

        private void BtnResetPalette_Click(object sender, EventArgs e)
        {
            ColorStatus cs = this.GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            Color[] colors;
            switch (cs)
            {
                case ColorStatus.Internal:
                    this.GetShownFile().ResetColors();
                    colors = this.GetShownFile().GetColors();
                    break;
                case ColorStatus.External:
                    PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    if (currentPal == null)
                        return;
                    if (currentPal.SourceFile != null && currentPal.Entry >= 0)
                    {
                        DialogResult dr = MessageBox.Show("This will remove all changes you have made to the palette since it was loaded!\n\nAre you sure you want to continue?", GetTitle(false), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                            return;
                    }
                    currentPal.Revert();
                    colors = currentPal.Colors;
                    this.GetShownFile().SetColors(colors);
                    break;
                default:
                    return;
            }
            this.RefreshImage();
            this.RefreshColorControls();
        }

        private void BtnSavePalette_Click(object sender, EventArgs e)
        {
            ColorStatus cs = this.GetColorStatus();
            if (cs == ColorStatus.None)
                return;
            SupportedFileType loadedFile = this.GetShownFile();
            if (loadedFile.BitsPerColor < 4)
                return;
            PaletteDropDownInfo currentPal;
            switch (cs) { case ColorStatus.External:
                    currentPal = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                    if (currentPal == null)
                        return;
                    break;
                case ColorStatus.Internal:
                    currentPal = new PaletteDropDownInfo(null, loadedFile.BitsPerColor, loadedFile.GetColors(), null, -1, false, false);
                    break;
                default:
                    return;
            }
            FrmManagePalettes palSave = new FrmManagePalettes(currentPal.BitsPerPixel);
            palSave.Icon = this.Icon;
            palSave.Title = GetTitle(false);
            palSave.PaletteToSave = currentPal;
            palSave.StartPosition = FormStartPosition.CenterParent;
            DialogResult dr = palSave.ShowDialog(this);
            if (dr != DialogResult.OK)
                return;
            if (cs == ColorStatus.Internal)
            {
                this.RefreshPalettes(true, true);
                this.RefreshColorControls();
                return;
            }
            // If null, it was a simple immediate overwrite, without the management box ever popping up, so
            // just consider the current entry "saved".
            if (palSave.PaletteToSave == null)
                currentPal.ClearRevert();
            else
            {
                // Get source position, reload all, then loop through to check which one to reselect.
                this.RefreshPalettes(true, true);
                String source = palSave.PaletteToSave.SourceFile;
                Int32 index = palSave.PaletteToSave.Entry;
                foreach (PaletteDropDownInfo pdd in this.cmbPalettes.Items)
                {
                    if (pdd.SourceFile != source || pdd.Entry != index)
                        continue;
                    this.cmbPalettes.SelectedItem = pdd;
                    break;
                }
            }
        }

        private void RefreshPalettes(Boolean forced, Boolean reloadFiles)
        {
            Int32 oldBpp = -1;
            PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal != null)
                oldBpp = currentPal.BitsPerPixel;
            SupportedFileType shown = GetShownFile();
            Int32 bpp = shown == null ? 8 : shown.BitsPerColor;
            // Don't reload if it was the same :)
            if (oldBpp != -1 && oldBpp == bpp && !forced)
                return;
            Int32 index = -1;
            List<PaletteDropDownInfo> bppPalettes = this.GetPalettes(bpp, reloadFiles);
            if (forced && oldBpp != -1 && oldBpp == bpp && currentPal != null)
                index = bppPalettes.FindIndex(x => x.Name == currentPal.Name);
            if (bppPalettes.Count == 0)
                bppPalettes.Add(new PaletteDropDownInfo("None", -1, PaletteUtils.GenerateGrayPalette(8, null, false), null, -1, false, false));
            this.cmbPalettes.DataSource = bppPalettes;
            if (index >= 0)
                this.cmbPalettes.SelectedIndex = index;
        }

        private void PnlImageScroll_MouseScroll(Object sender, MouseEventArgs e)
        {
            Keys k = Control.ModifierKeys;
            if ((k & Keys.Control) != 0)
            {
                this.numZoom.EnteredValue = this.numZoom.Constrain(this.numZoom.EnteredValue + (e.Delta / 120));
                HandledMouseEventArgs args = e as HandledMouseEventArgs;
                if (args != null)
                    args.Handled = true;
            }
        }

        private void PalColorViewer_ColorLabelMouseDoubleClick(object sender, PaletteClickEventArgs e)
        {
            SupportedFileType loadedFile = this.GetShownFile();
            if (e.Button != System.Windows.Forms.MouseButtons.Left)
                return;
            PalettePanel palpanel = sender as PalettePanel;
            if (palpanel == null)
                return;
            Int32 colindex = e.Index;
            ColorDialog cdl = new ColorDialog();
            cdl.Color = e.Color;
            cdl.FullOpen = true;
            cdl.CustomColors = this.m_CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.m_CustomColors = cdl.CustomColors;
            if (res == DialogResult.OK)
            {
                ColorStatus cs = this.GetColorStatus();
                palpanel.Palette[colindex] = cdl.Color;
                if (cs != ColorStatus.None)
                {
                    Color[] pal = loadedFile.GetColors();
                    if (pal.Length > colindex)
                        pal[colindex] = cdl.Color;
                    loadedFile.SetColors(pal);
                    if (cs == ColorStatus.External)
                    {
                        PaletteDropDownInfo currentPal = cmbPalettes.SelectedItem as PaletteDropDownInfo;
                        if (currentPal != null && currentPal.Colors.Length > colindex)
                            currentPal.Colors[colindex] = cdl.Color;
                    }
                }
                this.RefreshImage();
                this.RefreshColorControls();
            }
        }

        private enum ColorStatus
        {
            None,
            Internal,
            External
        }

    }
}
