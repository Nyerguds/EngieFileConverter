using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Nyerguds.ImageManipulation;

namespace Nyerguds.Util.UI
{
    public partial class FrmPalette : Form
    {
        public Int32[] CustomColors { get; set; }
        public Boolean ColorsEditable { get; set; }
        public Int32[] SelectedIndices
        {
            get { return this.palettePanel.SelectedIndices; }
            set { this.palettePanel.SelectedIndices = value; }
        }

        public ColorSelMode SelectMode
        {
            get { return this.palettePanel.ColorSelectMode; }
            set { this.palettePanel.ColorSelectMode = value; }
        }

        public Color[] Palette
        {
            get { return this.palettePanel.Palette; }
            set { this.palettePanel.Palette = value; }
        }

        private FrmPalette()
            : this(null, false, ColorSelMode.None)
        { }

        public FrmPalette(Color[] palette, Boolean editable, ColorSelMode selectMode)
        {
            this.InitializeComponent();
            this.ColorsEditable = editable;
            this.palettePanel.Palette = palette;
            this.palettePanel.ColorSelectMode = selectMode;
        }

        public Int32[] GetSelectedIndices()
        {
            return this.palettePanel.SelectedIndices;
        }

        private void PalettePanel_LabelMouseDoubleClick(Object sender, MouseEventArgs e)
        {
            if (!this.ColorsEditable || e.Button != MouseButtons.Left)
                return;
            Int32 colindex = (Int32)sender;
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.palettePanel.Palette[colindex];
            cdl.FullOpen = true;
            cdl.CustomColors = this.CustomColors;
            DialogResult res = cdl.ShowDialog();
            this.CustomColors = cdl.CustomColors;
            if (res == DialogResult.OK || res == DialogResult.Yes)
                this.palettePanel.Palette[colindex] = cdl.Color;
        }
    }
}
