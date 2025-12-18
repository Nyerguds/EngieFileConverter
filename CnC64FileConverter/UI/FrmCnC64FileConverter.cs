using CnC64FileConverter.Domain;
using CnC64FileConverter.Domain.HeightMap;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private Color backgroundFillColor = Color.Fuchsia;
        private Int32[] customcolors;


        private SupportedFileType GetLoadedFile()
        {
            if (this.m_LoadedFile == null)
                return null;
            Boolean hasFrames = m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            return hasFrames && numFrame.Value != -1 ? m_LoadedFile.Frames[(Int32)numFrame.Value] : m_LoadedFile;
        }

        public FrmCnC64FileConverter()
        {
            InitializeComponent();
            this.Text = GetTitle(true);
            picImage.BackColor = backgroundFillColor;
            lblTransparentColorVal.BackColor = backgroundFillColor;
            PalettePanel.InitPaletteControl(8, palColorViewer, new Color[0], PALETTE_DIM);
            this.palColorViewer.MaxColors = 0;
            m_DefaultPalettes = LoadDefaultPalettes();
            m_ReadPalettes = LoadExtraPalettes();
            RefreshPalettes(false, false);
            ContextMenu cmCopyPreview = new ContextMenu();
            MenuItem mniCopy = new MenuItem("Copy");
            mniCopy.Click += new EventHandler(this.PicImage_CopyPreview);
            cmCopyPreview.MenuItems.Add(mniCopy);
            picImage.ContextMenu = cmCopyPreview;
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
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 4, PaletteUtils.GenerateGrayPalette(4, false, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 4, PaletteUtils.GenerateRainbowPalette(4, false, false, true, 0, 160.0 / 240.0), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 4, PaletteUtils.GenerateGrayPalette(4, false, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 4, PaletteUtils.GenerateRainbowPalette(4, false, false, false), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Windows palette", 4, PaletteUtils.GenerateDefWindowsPalette(4, false, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Grayscale B->W", 8, PaletteUtils.GenerateGrayPalette(8, false, false), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Heights Blue->Red", 8, PaletteUtils.GenerateRainbowPalette(8, false, false, true, 0, 160, true), null, -1, false, false));
            //palettes.Add(new PaletteDropDownInfo("Grayscale W->B", 8, PaletteUtils.GenerateGrayPalette(8, false, true), null, -1, false, false));
            palettes.Add(new PaletteDropDownInfo("Rainbow", 8, PaletteUtils.GenerateRainbowPalette(8, false, false, false), null, -1, false, false));
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
            Bitmap bm = ImageUtils.PaintOn32bpp(image, this.backgroundFillColor);
            Clipboard.Clear();
            DataObject data = new DataObject();
            data.SetData(DataFormats.Bitmap, bm);
            Clipboard.SetDataObject(data);
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
                Exception error = null;
                try
                {
                    this.m_LoadedFile = null;
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
                            error = new FileTypeLoadException("Could not load file as " + selectedType.ShortTypeDescription + ":\n\n" + e.Message, e);
                        }
                    }
                    else
                    {
                        List<FileTypeLoadException> loadErrors;
                        this.m_LoadedFile = SupportedFileType.LoadImageAutodetect(path, possibleTypes, out loadErrors);
                        if (this.m_LoadedFile == null)
                        {
                            String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                            MessageBox.Show(this, "File type could not be identified. Errors returned by all attempts:\n\n" + errors, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    this.m_LoadedFile = null;
                }
                ReloadUi(true);
                if (error != null)
                    MessageBox.Show(this, "Image loading failed" + ": " + error.Message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.m_Loading = false;
            }
        }
                
        private void ReloadUi(Boolean fromNewFile)
        {
            Boolean hasFrames = m_LoadedFile != null && m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            lblFrame.Enabled = hasFrames;
            numFrame.Enabled = hasFrames;
            if (!hasFrames)
            {
                numFrame.Value = -1;
                lblNrOfFrames.Visible = false;
            }
            else
            {
                Int32 last = m_LoadedFile.Frames.Length - 1;
                numFrame.Maximum = last;
                lblNrOfFrames.Visible = true;
                lblNrOfFrames.Text = "/ " + last;
            }
            SupportedFileType loadedFile = GetLoadedFile();
            tsmiSave.Enabled = loadedFile != null;
            tsmiExport.Enabled = loadedFile != null;
            this.tsmiCopy.Enabled = loadedFile != null;
            tsmiToHeightMap.Enabled = loadedFile is FileMapPc;
            tsmiToPlateaus.Enabled = loadedFile is FileMapPc;
            tsmiToHeightMapAdv.Enabled = loadedFile is FileMapPc;
            tsmiTo65x65HeightMap.Enabled = loadedFile is FileImage;
            // 4 is the supertype; 8 the derivative
            this.tsmiTilesetsToFrames.Enabled = loadedFile is FileTilesN64Bpp4;
            this.tsmiTilesetsToTilesetFiles.Enabled = loadedFile is FileTilesN64Bpp4;
            if (loadedFile == null)
            {
                String emptystr = "---";
                lblValFilename.Text = emptystr;
                lblValType.Text = emptystr;
                lblValWidth.Text = emptystr;
                lblValHeight.Text = emptystr;
                lblValColorFormat.Text = emptystr;
                lblValColorsInPal.Text = emptystr;
                this.cmbPalettes.Enabled = false;
                this.cmbPalettes.SelectedIndex = 0;
                this.btnResetPalette.Enabled = false;
                this.btnSavePalette.Enabled = false;
                picImage.Image = null;
                RefreshImage();
                PalettePanel.InitPaletteControl(8, palColorViewer, new Color[0], PALETTE_DIM);
                this.palColorViewer.MaxColors = 0;
                return;
            }
            Int32 bpc = loadedFile.BitsPerColor;
            lblValFilename.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.LoadedFileName);
            lblValType.Text = GeneralUtils.DoubleFirstAmpersand(loadedFile.ShortTypeDescription);
            lblValWidth.Text = loadedFile.Width.ToString();
            lblValHeight.Text = loadedFile.Height.ToString();
            lblValColorFormat.Text = bpc == 0 ? "N/A" : (bpc + " BPP" + (bpc == 4 || bpc == 8 ? " (paletted)" : String.Empty));
            Color[] palette = loadedFile.GetColors();
            Int32 exposedColours = loadedFile.ColorsInPalette;
            Int32 actualColors = palette == null? 0 : palette.Length;
            Boolean needsPalette = exposedColours != actualColors;
            lblValColorsInPal.Text = actualColors + (needsPalette ? " (" + exposedColours + " in file)" : String.Empty);
            this.cmbPalettes.Enabled = needsPalette;
            picImage.Image = loadedFile.GetBitmap();
            this.RefreshPalettes(false, false);
            if (needsPalette && fromNewFile)
            {
                CmbPalettes_SelectedIndexChanged(null, null);
            }
            else
            {
                this.RefreshImage();
                this.RefreshColorControls();
            }
        }

        private ColorStatus GetColorStatus()
        {
            SupportedFileType loadedFile = GetLoadedFile();
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
            List<PaletteDropDownInfo> allPalettes = m_DefaultPalettes.Where(p => p.BitsPerPixel == bpp).ToList();
            if (reloadFiles)
                m_ReadPalettes = LoadExtraPalettes();
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
                ReloadUi(true);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            this.Save(false);
        }

        private void BtnSaveExport_Click(object sender, EventArgs e)
        {
            this.Save(true);
        }
        
        private void Save(Boolean export)
        {
            if (m_LoadedFile == null)
                return;
            SupportedFileType selectedItem;
            Boolean hasFrames = m_LoadedFile.Frames != null && m_LoadedFile.Frames.Length > 0;
            SupportedFileType loadedFile = hasFrames && numFrame.Value != -1 ? m_LoadedFile.Frames[(Int32)numFrame.Value] : m_LoadedFile;
            Type selectType;
            if (export || !SupportedFileType.SupportedSaveTypes.Contains(loadedFile.GetType()))
                selectType = loadedFile.PreferredExportType.GetType();
            else
                selectType = loadedFile.GetType();

            String filename = FileDialogGenerator.ShowSaveFileFialog(this, selectType, SupportedFileType.SupportedSaveTypes, true, loadedFile.LoadedFile, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            try
            {
                selectedItem.SaveAsThis(loadedFile, filename);
            }
            catch (NotSupportedException ex)
            {
                String message = ex.Message;
                if (String.IsNullOrEmpty(message))
                    message = "Cannot save type " + loadedFile.ShortTypeName + " as type " + selectedItem.ShortTypeName + ".";
                MessageBox.Show(this, message, GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
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
            this.LoadFile(filename, selectedItem, possibleTypes);
        }

        private void NumZoom_ValueChanged(object sender, EventArgs e)
        {
            RefreshImage();
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
            SupportedFileType loadedFile = GetLoadedFile();
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
            pnlImageScroll.Focus();
        }

        private void LblTransparentColorVal_Click(object sender, EventArgs e)
        {
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.backgroundFillColor;
            cdl.FullOpen = true;
            cdl.CustomColors = this.customcolors;
            DialogResult res = cdl.ShowDialog(this);
            customcolors = cdl.CustomColors;
            if (res == DialogResult.OK || res == DialogResult.Yes)
            {
                this.backgroundFillColor = cdl.Color;
                lblTransparentColorVal.BackColor = backgroundFillColor;
                picImage.BackColor = backgroundFillColor;
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
            if (!(this.m_LoadedFile is FileMapPc))
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
            FileMapPc map = (FileMapPc)this.m_LoadedFile;
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
            FileImgN64Gray file = new FileImgN64Gray();
            file.LoadImage(bm, imgFileName);
            this.m_LoadedFile = file;
            this.ReloadUi(false);
        }

        private void TsmiToPlateaus_Click(object sender, EventArgs e)
        {
            if (!(this.m_LoadedFile is FileMapPc))
                return;
            this.m_LoadedFile = HeightMapGenerator.GeneratePlateauImage64x64((FileMapPc)this.m_LoadedFile, "_lvl");
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
                targetPal = PaletteUtils.GenerateGrayPalette(8, false, false);
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
            this.m_LoadedFile.SetColors(targetPal);
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
                    this.m_LoadedFile.ResetColors();
                    colors = this.m_LoadedFile.GetColors();
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
                    this.m_LoadedFile.SetColors(colors);
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
            SupportedFileType loadedFile = GetLoadedFile();
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
            Int32 bpp = m_LoadedFile == null? 8 : m_LoadedFile.BitsPerColor;
            // Don't reload if it was the same :)
            if (oldBpp != -1 && oldBpp == bpp && !forced)
                return;
            Int32 index = -1;
            List<PaletteDropDownInfo> bppPalettes = this.GetPalettes(bpp, reloadFiles);
            if (forced && oldBpp != -1 && oldBpp == bpp && currentPal != null)
                index = bppPalettes.FindIndex(x => x.Name == currentPal.Name);
            if (bppPalettes.Count == 0)
                bppPalettes.Add(new PaletteDropDownInfo("None", -1, PaletteUtils.GenerateGrayPalette(8, false, false), null, -1, false, false));
            this.cmbPalettes.DataSource = bppPalettes;
            if (index >= 0)
                this.cmbPalettes.SelectedIndex = index;
        }

        private void PalColorViewer_ColorLabelMouseDoubleClick(object sender, PaletteClickEventArgs e)
        {
            SupportedFileType loadedFile = GetLoadedFile();
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
