using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ColorManipulation;

namespace CnC64ImgViewer.Ui
{
    public partial class FrmPalette : Form
    {

        protected Int32[] m_Customcolors;
        protected Boolean m_ApplyRemap;
        protected Boolean m_ShowFilterToggle;
        protected String m_Filename;
        protected Boolean m_Editable;

        private FrmPalette()
            : this(null, null, false, true, false, null, false, false, false, false, null)
        { }
        
        public FrmPalette(Color[] palette, Int32[] filter, Boolean showRemappedPalette, Boolean showFilterToggle, Boolean activateFilterToggle, String filename, Boolean editable, Boolean allowSave, Boolean selectable, Boolean multiselect, Int32[] selectedIndices)
        {
            InitializeComponent();

            palettePanel.Palette = palette;
            palettePanel.Remap = filter;
            palettePanel.Selectable = selectable;
            palettePanel.Multiselect = multiselect;
            palettePanel.ShowRemappedPalette = showRemappedPalette;
            palettePanel.SelectedIndices = selectedIndices;

            this.m_ShowFilterToggle = showFilterToggle;
            this.chkColorOption.Visible = m_ShowFilterToggle;
            this.chkColorOption.Checked = m_ShowFilterToggle && activateFilterToggle;
            this.m_Filename = filename;
            btnSavePalette.Visible = allowSave;
            this.m_Editable = editable;

            if (!m_ShowFilterToggle)
            {
                Int32 diff = btnSavePalette.Location.Y - chkColorOption.Location.Y;
                btnSavePalette.Location = new Point(btnSavePalette.Location.X, btnSavePalette.Location.Y - diff);
                btnClose.Location = new Point(btnClose.Location.X, btnClose.Location.Y - diff);
                this.Size = new Size(this.Size.Width, this.Size.Height - diff);
            }
        }

        public Int32[] GetSelectedIndices()
        {
            return palettePanel.SelectedIndices;
        }

        protected virtual void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected void btnSavePalette_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = Path.GetDirectoryName(m_Filename);
            sfd.FileName = Path.GetDirectoryName(m_Filename) + "\\" + Path.GetFileNameWithoutExtension(m_Filename) + ".pal";
            sfd.Title = "Save palette";
            sfd.Filter = "C&C color palette file (*.pal)|*.pal|All Files|*.*";
            sfd.DefaultExt = "pal";
            sfd.AddExtension = true;
            DialogResult ofres = sfd.ShowDialog();
            if (ofres == System.Windows.Forms.DialogResult.Cancel)
                return;

            String palfilename = sfd.FileName;

            //ImageUtils.WritePaletteFile(palettePanel.Palette, palfilename);

            sfd.Dispose();
            sfd = null;
        }

        protected virtual void chkColorOption_CheckedChanged(object sender, EventArgs e)
        {
            if (palettePanel.Remap == null)
                return;
            if (chkColorOption.Checked)
                palettePanel.SetVisibility(palettePanel.Remap, true);
            else
                palettePanel.SetVisibility(new Int32[0], false);
        }

        private void palettePanel_LabelMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!m_Editable || e.Button != System.Windows.Forms.MouseButtons.Left)
                return;
            Int32 colindex = (Int32)sender;
            ColorDialog cdl = new ColorDialog();
            cdl.Color = palettePanel.Palette[colindex];
            cdl.FullOpen = true;
            cdl.CustomColors = m_Customcolors;
            DialogResult res = cdl.ShowDialog();
            m_Customcolors = cdl.CustomColors;
            if (res == DialogResult.OK || res == DialogResult.Yes)
            {
                palettePanel.Palette[colindex] = cdl.Color;
            }
        }
    }
}
