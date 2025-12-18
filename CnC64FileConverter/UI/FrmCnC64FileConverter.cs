using CnC64FileConverter.Domain;
using CnC64FileConverter.Domain.HeightMap;
using CnC64FileConverter.Domain.ImageFile;
using CnC64FileConverter.Domain.Utils;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CnC64FileConverter.UI
{
    public partial class FrmCnC64FileConverter : Form
    {
        private String m_Filename;
        private Boolean m_Loading;
        private N64FileType m_LoadedFile;
        private Color backgroundFillColor = Color.Fuchsia;
        private Int32[] customcolors;
        
        public FrmCnC64FileConverter()
        {
            InitializeComponent();
            this.Text = "C&C64 File Converter " + GeneralUtils.ProgramVersion() + " - Created by Nyerguds";
            picImage.BackColor = backgroundFillColor;
            lblTransparentColorVal.BackColor = backgroundFillColor;
            PalettePanel.InitPaletteControl(8, palettePanel1, new Color[0], 226);
        }

        public FrmCnC64FileConverter(String[] args) : this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                m_Filename = args[0];
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
            N64FileType[] possibleTypes = FileDialogGenerator.IdentifyByExtension<N64FileType>(N64FileType.AutoDetectTypes, path);
            this.LoadImage(path, null, possibleTypes);
        }
        
        private void LoadImage(String path, N64FileType selectedType, N64FileType[] possibleTypes)
        {
            this.m_Loading = true;
            try
            {
                this.m_Filename = path;
                String error = null;
                try
                {
                    this.m_LoadedFile = null;
                    if (selectedType != null)
                    {
                        try
                        {
                            selectedType.LoadImage(path);
                            this.m_LoadedFile = selectedType;
                            this.m_Filename = selectedType.LoadedFileName;
                        }
                        catch (FileTypeLoadException e)
                        {
                            m_LoadedFile = null;
                            error = "Could not load file as " + selectedType.ShortTypeDescription + ":\n\n" + e.Message;
                        }
                    }
                    else
                    {
                        List<FileTypeLoadException> loadErrors;
                        this.m_LoadedFile = N64FileType.LoadImageAutodetect(path, possibleTypes, out loadErrors);
                        if (this.m_LoadedFile != null)
                        {
                            this.m_Filename = this.m_LoadedFile.LoadedFileName;
                        }
                        else
                        {
                            String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                            MessageBox.Show(this, "File type could not be identified. Errors returned by all attempts:\n\n" + errors, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    this.m_LoadedFile = null;
                }
                ReloadUi();
                if (error != null)
                    MessageBox.Show(this, "Image loading failed" + (error == null ? "." : ": " + error), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.m_Loading = false;
            }
        }
                
        private void ReloadUi()
        {
            tsmiSave.Enabled = m_LoadedFile != null;
            tsmiExport.Enabled = m_LoadedFile != null;
            tsmiToHeightMap.Enabled = m_LoadedFile is FileMapPc;
            tsmiToPlateaus.Enabled = m_LoadedFile is FileMapPc;
            tsmiToHeightMapAdv.Enabled = m_LoadedFile is FileMapPc;
            tsmiTo65x65HeightMap.Enabled = m_LoadedFile is FileImage;
            if (m_LoadedFile == null)
            {
                String emptystr = "---";
                lblValFilename.Text = emptystr;
                lblValType.Text = emptystr;
                lblValWidth.Text = emptystr;
                lblValHeight.Text = emptystr;
                lblValColorFormat.Text = emptystr;
                lblValColorsInPal.Text = emptystr;
                picImage.Image = null;
                RefreshImage();
                PalettePanel.InitPaletteControl(8, palettePanel1, new Color[0], 226);
                return;
            }
            Int32 bpc = m_LoadedFile.GetBitsPerColor();
            lblValFilename.Text = GeneralUtils.DoubleFirstAmpersand(Path.GetFileName(m_Filename));
            lblValType.Text = GeneralUtils.DoubleFirstAmpersand(m_LoadedFile.ShortTypeDescription);
            lblValWidth.Text = m_LoadedFile.Width.ToString();
            lblValHeight.Text = m_LoadedFile.Height.ToString();
            lblValColorFormat.Text = bpc + " BPP" + (bpc == 4 || bpc == 8 ? " (paletted)" : String.Empty);
            lblValColorsInPal.Text = m_LoadedFile.ColorsInPalette + (!m_LoadedFile.FileHasPalette && m_LoadedFile.ColorsInPalette != 0 ? " (0 in header)" : String.Empty);
            picImage.Image = m_LoadedFile.GetBitmap();
            RefreshImage();
            if (m_LoadedFile.FileHasPalette)
                PalettePanel.InitPaletteControl(m_LoadedFile.GetBitsPerColor(), palettePanel1, m_LoadedFile.GetColors(), 226);
            else
                PalettePanel.InitPaletteControl(8, palettePanel1, new Color[0], 226);
        }

        private void FrmCnC64FileConverter_Shown(object sender, EventArgs e)
        {
            if (m_Filename != null)
                LoadImage(m_Filename, null, null);
            else
                ReloadUi();
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
            N64FileType selectedItem;

            Type selectType;
            if (export)
                selectType = m_LoadedFile.PreferredExportType.GetType();
            else
                selectType = m_LoadedFile.GetType();

            String filename = FileDialogGenerator.ShowSaveFileFialog(this, selectType, N64FileType.SupportedSaveTypes, true, m_Filename, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            try
            {
                selectedItem.SaveAsThis(m_LoadedFile, filename);
            }
            catch (NotSupportedException ex)
            {
                String message = ex.Message;
                if (String.IsNullOrEmpty(message))
                    message = "Cannot save type " + m_LoadedFile.ShortTypeName + " as type " + selectedItem.ShortTypeName + ".";
                MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            N64FileType selectedItem;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, null, N64FileType.SupportedOpenTypes, N64FileType.SupportedSaveTypes, this.m_Filename, "images", null, out selectedItem);
            if (filename == null)
                return;
            LoadImage(filename, selectedItem, null);
        }

        private void numZoom_ValueChanged(object sender, EventArgs e)
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
        }

        private void picImage_Click(object sender, EventArgs e)
        {
            pnlImageScroll.Focus();
        }

        private void lblTransparentColorVal_Click(object sender, EventArgs e)
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
                numZoom_ValueChanged(null, null);
            }
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
            if (this.m_LoadedFile is FileMapPc)
            {
                String baseFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename));
                String iniFileName = baseFileName + ".ini";
                String pngFileName = baseFileName + ".png";
                Bitmap plateauImage = null;
                if (selectHeightMap)
                {
                    N64FileType selectedType;
                    //String plateauFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename)) + "_lvl.png";
                    String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select height levels image", new Type[] { typeof(FileImage) }, null, pngFileName, "images", null, out selectedType);
                    if (filename == null)
                        return;
                    try
                    {
                        if (selectedType == null)
                            selectedType = new FileImage();
                        selectedType.LoadImage(filename);
                        plateauImage = selectedType.GetBitmap();
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(this, "Could not load file as " + selectedType.ShortTypeDescription + ":\n\n" + e.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (plateauImage.Width != 64 || plateauImage.Height != 64)
                    {
                        MessageBox.Show(this, "Height levels image needs to be 64x64!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                FileMapPc map = (FileMapPc)m_LoadedFile;
                IniFile mapInfo = !File.Exists(iniFileName) ? null : new IniFile(iniFileName, IniFile.ENCODING_DOS_US);
                this.m_LoadedFile = HeightMapGenerator.GenerateHeightMapImage64x64(map.Map, mapInfo, plateauImage);
                this.m_Filename = pngFileName;
                ReloadUi();
            }
        }

        private void TsmiTo65x65HeightMap_Click(object sender, EventArgs e)
        {
            if (this.m_LoadedFile != null && m_LoadedFile.Width == 64 && m_LoadedFile.Height == 64 && m_LoadedFile is FileImage)
            {
                String baseFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename));
                String imgFileName = baseFileName + ".img";
                Bitmap bm = HeightMapGenerator.GenerateHeightMapImage65x65(m_LoadedFile.GetBitmap());
                //Byte[] imageData = ImageUtils.GetSavedImageData(bm, ref imgFileName);
                FileImgN64Gray file = new FileImgN64Gray();
                file.LoadImage(bm);
                this.m_LoadedFile = file;
                this.m_Filename = imgFileName;
                ReloadUi();
            }
        }

        private void tsmiToPlateaus_Click(object sender, EventArgs e)
        {
            if (this.m_LoadedFile is FileMapPc)
            {
                String baseFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename));
                String iniFileName = baseFileName + ".ini";
                String plateauFileName = Path.Combine(Path.GetDirectoryName(m_Filename), Path.GetFileNameWithoutExtension(m_Filename)) + "_lvl.png";
                FileMapPc map = (FileMapPc)m_LoadedFile;
                IniFile mapInfo = !File.Exists(iniFileName) ? null : new IniFile(iniFileName, IniFile.ENCODING_DOS_US);
                this.m_LoadedFile = HeightMapGenerator.GeneratePlateauImage64x64(map.Map, mapInfo);
                this.m_Filename = plateauFileName;
                ReloadUi();
            }
        }
    }
}
