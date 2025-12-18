using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nyerguds.Util.UI;
using System.IO;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;

namespace EngieFileConverter.UI
{
    public partial class FrmPasteOnFrames : Form
    {
        public Int32[] FrameRange { get; private set; }
        public Point Coords { get; private set; }
        public Bitmap Image { get; private set; }
        public String LastSelectedFolder { get; private set; }
        public Boolean KeepIndices { get; private set; }

        private Bitmap m_Image;
        private Int32 m_Frames; 
        private Int32 m_FramesBpp;

        public FrmPasteOnFrames(Int32 frames, Int32 width, Int32 height, Int32 framesBpp, String lastOpenedFolder)
        {
            this.m_Frames = frames;
            this.LastSelectedFolder = lastOpenedFolder;
            this.InitializeComponent();
            this.m_FramesBpp = framesBpp;
            this.numCoordsX.Maximum = width - 1;
            this.numCoordsY.Maximum = height- 1;
            if (m_FramesBpp > 0 && m_FramesBpp <= 8)
            {
                this.rbtMatchPalette.Enabled = true;
                this.rbtMatchPalette.Checked = true;
                this.rbtKeepIndices.Enabled = true;
            }
        }

        private void BtnSelectImageClick(Object sender, EventArgs e)
        {
            Type[] saveTypes = SupportedFileType.SupportedSaveTypes;
            List<Type> filteredTypes = new List<Type>();
            foreach (Type saveType in saveTypes)
            {
                SupportedFileType tmpsft = (SupportedFileType)Activator.CreateInstance(saveType);
                if ((tmpsft.InputFileClass & FileClass.Image) != 0)
                    filteredTypes.Add(saveType);
            }
            SupportedFileType selectedType;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, "Select image", filteredTypes.ToArray(), this.LastSelectedFolder, "images", null, out selectedType);
            if (filename == null)
                return;
            this.LastSelectedFolder = Path.GetDirectoryName(filename);
            Boolean loaded = false;
            try
            {
                Byte[] fileData = File.ReadAllBytes(filename);
                if (selectedType == null)
                {
                    SupportedFileType[] sft = filteredTypes.Select(ft => new FileDialogItem<SupportedFileType>(ft).ItemObject).ToArray();
                    List<FileTypeLoadException> loadErrors;
                    selectedType = SupportedFileType.LoadFileAutodetect(fileData, filename, sft, out loadErrors, true);
                    if (selectedType == null)
                    {
                        String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                        MessageBox.Show(this, "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors, FrmFileConverter.GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                selectedType.LoadFile(fileData, filename);
                this.m_Image = selectedType.GetBitmap();
                this.txtImage.Text = Path.GetFullPath(filename);
                loaded = true;
                this.btnOK.Enabled = true;
                Int32 selectedBpp = selectedType.BitsPerPixel;
                this.rbtKeepIndices.Enabled =  selectedBpp > 0 && selectedBpp <= 8 && selectedBpp <= m_FramesBpp;
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
                MessageBox.Show(this, "Could not load file as " + selectedType.ShortTypeDescription + ":\n\n" + ex.Message, FrmFileConverter.GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (!loaded)
                {
                    this.m_Image = null;
                    this.txtImage.Text = String.Empty;
                    this.btnOK.Enabled = false;
                }
            }
        }

        private void BtnOkClick(Object sender, EventArgs e)
        {
            if (txtFrames.Text.Trim(",- \r\n\t".ToCharArray()).Length == 0)
            {
                MessageBox.Show(this, "No frame ranges specified.", FrmFileConverter.GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Int32[] frameRange = GeneralUtils.GetRangedNumbers(txtFrames.Text).Where(i => i < m_Frames).ToArray();
            if (frameRange.Length == 0)
            {
                MessageBox.Show(this, "No valid frame ranges found in given text.", FrmFileConverter.GetTitle(false), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.FrameRange = frameRange;
            this.Coords = new Point((Int32) this.numCoordsX.Value, (Int32) this.numCoordsY.Value);
            this.Image = this.m_Image;
            this.KeepIndices = this.rbtKeepIndices.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
