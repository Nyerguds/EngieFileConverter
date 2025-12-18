using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nyerguds.Util.UI;
using System.IO;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.ImageManipulation;
using System.Drawing.Imaging;

namespace EngieFileConverter.UI
{
    public partial class FrmPasteOnFrames : Form
    {
        public Int32[] FrameRange { get; private set; }
        public Point Coords { get; private set; }
        public Bitmap Image { get; private set; }
        public String LastSelectedFolder { get; private set; }
        public Boolean KeepIndices { get; private set; }

        private Int32 m_Frames;
        private Int32 m_FramesBpp;
        private String labelText;
        private Int32 m_PasteAreaWidth;
        private Int32 m_PasteAreaHeight;

        public FrmPasteOnFrames(Int32 frames, Int32 width, Int32 height, Int32 framesBpp, String lastOpenedFolder)
        {
            this.m_Frames = frames;
            this.m_PasteAreaWidth = width;
            this.m_PasteAreaHeight = height;
            this.LastSelectedFolder = lastOpenedFolder;
            this.InitializeComponent();
            this.labelText = lblImage.Text;
            this.m_FramesBpp = framesBpp;
            this.numCoordsX.Maximum = width - 1;
            this.numCoordsY.Maximum = height- 1;
            if (m_FramesBpp > 0 && m_FramesBpp <= 8)
            {
                this.rbtMatchPalette.Enabled = true;
                this.rbtMatchPalette.Checked = true;
                this.rbtKeepIndices.Enabled = true;
            }
            if (m_Frames == 1)
            {
                this.txtFrames.Text = "0";
                this.txtFrames.ReadOnly = true;
                this.txtFrames.BackColor = SystemColors.Control;
            }
        }

        private void BtnSelectImageClick(Object sender, EventArgs e)
        {
            Type[] openTypes = FileTypesFactory.SupportedOpenTypes;
            SupportedFileType[] sft = openTypes.Select(ft => new FileDialogItem<SupportedFileType>(ft).ItemObject).Where(ft => (ft.InputFileClass & FileClass.Image) != 0).ToArray();
            List<Type> filteredTypes = sft.Select(ft => ft.GetType()).ToList();
            SupportedFileType selectedType;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select image", filteredTypes.ToArray(), openTypes, this.LastSelectedFolder, "images", null, true, out selectedType);
            if (filename == null)
                return;
            this.LastSelectedFolder = Path.GetDirectoryName(filename);
            Boolean loaded = false;
            try
            {
                Byte[] fileData = File.ReadAllBytes(filename);
                // "*.*" was selected.
                if (selectedType == null)
                {
                    List<FileTypeLoadException> loadErrors;
                    selectedType = FileTypesFactory.LoadFileAutodetect(fileData, filename, sft, true, out loadErrors);
                    if (selectedType == null)
                    {
                        String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                        MessageBox.Show(this, "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors, FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    loaded = true;
                }
                if (!loaded)
                    selectedType.LoadFile(fileData, filename);
                this.Image = ImageUtils.CloneImage(selectedType.GetBitmap());
                this.txtImage.Text = Path.GetFullPath(filename);
                this.lblImage.Text = this.labelText + " " + selectedType.Width + "×" + selectedType.Height + ", " + selectedType.BitsPerPixel + "bpp";
                loaded = true;
                this.btnOK.Enabled = true;
                this.btnCenterX.Enabled = true;
                this.btnCenterY.Enabled = true;
                Int32 selectedBpp = selectedType.BitsPerPixel;
                this.rbtKeepIndices.Enabled = m_FramesBpp <= 8 && selectedBpp > 0 && selectedBpp <= 8 && selectedBpp <= m_FramesBpp;
                if (!this.rbtKeepIndices.Enabled)
                {
                    if (this.rbtMatchPalette.Enabled)
                        this.rbtMatchPalette.Checked = true;
                    else
                        this.rbtKeepIndices.Checked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load file as " + selectedType.LongTypeName + ":\n\n" + ex.Message, FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (selectedType != null)
                    selectedType.Dispose();
                if (!loaded)
                {
                    if (this.Image != null)
                    {
                        this.Image.Dispose();
                        this.Image = null;
                    }
                    this.txtImage.Text = String.Empty;
                    this.lblImage.Text = this.labelText;
                    this.btnOK.Enabled = false;
                    this.btnCenterX.Enabled = false;
                    this.btnCenterY.Enabled = false;
                }
            }
        }

        private void btnClipboard_Click(Object sender, EventArgs e)
        {
            GetImageFromClipboard(false);
        }

        private Boolean GetImageFromClipboard(Boolean failSilently)
        {
            DataObject retrievedData = (DataObject)Clipboard.GetDataObject();
            if (retrievedData == null)
            {
                if (!failSilently)
                    MessageBox.Show(this, "No data on clipboard!", FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                FileImage clipImage = new FileImagePng();
                using (Bitmap clipboardimage = ClipboardImage.GetClipboardImage(retrievedData))
                {
                    if (clipboardimage == null)
                    {
                        if (!failSilently)
                            MessageBox.Show(this, "No image data on clipboard!", FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        clipboardimage.Save(ms, ImageFormat.Png);
                        clipImage.LoadFile(ms.ToArray(), ".\\image.png");
                    }
                }
                this.Image = clipImage.GetBitmap();
                Int32 selectedBpp = clipImage.BitsPerPixel;
                this.txtImage.Text = "[From clipboard]";
                this.lblImage.Text = this.labelText + " " + clipImage.Width + "×" + clipImage.Height + ", " + clipImage.BitsPerPixel + "bpp";
                this.btnOK.Enabled = true;
                this.btnCenterX.Enabled = true;
                this.btnCenterY.Enabled = true;
                this.rbtKeepIndices.Enabled = selectedBpp > 0 && selectedBpp <= 8 && selectedBpp <= m_FramesBpp;
                if (!this.rbtKeepIndices.Enabled)
                {
                    if (this.rbtMatchPalette.Enabled)
                        this.rbtMatchPalette.Checked = true;
                    else
                        this.rbtKeepIndices.Checked = false;
                }
            }
            catch (Exception ex)
            {
                if (!failSilently)
                    MessageBox.Show(this, "Could not load clipboard data:\n\n" + ex.Message, FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            finally
            {
                if (this.Image == null)
                {
                    this.txtImage.Text = String.Empty;
                    this.lblImage.Text = this.labelText;
                    this.btnOK.Enabled = false;
                    this.btnCenterX.Enabled = false;
                    this.btnCenterY.Enabled = false;
                }
            }
            return true;
        }

        protected override Boolean ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.V) && GetImageFromClipboard(true))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void btnCenterX_Click(object sender, EventArgs e)
        {
            if (this.Image == null)
                return;
            numCoordsX.Value = Math.Min(Math.Max(0, (this.m_PasteAreaWidth - this.Image.Width) / 2), numCoordsX.Maximum);
        }

        private void btnCenterY_Click(object sender, EventArgs e)
        {
            if (this.Image == null)
                return;
            numCoordsY.Value = Math.Min(Math.Max(0, (this.m_PasteAreaHeight - this.Image.Height) / 2), numCoordsY.Maximum);
        }

        private void TextBoxShortcuts(Object sender, KeyEventArgs e)
        {
            // Split off to override menu shortcuts when this control is selected.
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;
            if (e.Control)
            {
                Boolean handled = true;
                if (e.KeyCode == Keys.A)
                    textBox.SelectAll();
                else if (e.KeyCode == Keys.Z)
                    textBox.Undo();
                else if (e.KeyCode == Keys.V)
                    textBox.Paste();
                else if (e.KeyCode == Keys.X)
                    textBox.Cut();
                else if (e.KeyCode == Keys.C || e.KeyCode == Keys.Insert)
                    textBox.Copy();
                else
                    handled = false;
                if (handled)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            }
            else if (e.Shift && e.KeyCode == Keys.Insert)
            {
                textBox.Paste();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }
        
        private void BtnOkClick(Object sender, EventArgs e)
        {
            if (txtFrames.Text.Trim(",- \r\n\t".ToCharArray()).Length == 0)
            {
                MessageBox.Show(this, "No frame ranges specified.", FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Int32[] frameRange = GeneralUtils.GetRangedNumbers(txtFrames.Text).Where(i => i < m_Frames).ToArray();
            if (frameRange.Length == 0)
            {
                MessageBox.Show(this, "No valid frame ranges found in given text.", FrmFileConverter.GetTitle(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.FrameRange = frameRange;
            this.Coords = new Point((Int32) this.numCoordsX.Value, (Int32) this.numCoordsY.Value);
            this.KeepIndices = this.rbtKeepIndices.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
