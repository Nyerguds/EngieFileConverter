using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    public partial class FrmPalette : Form
    {
        private readonly int paletteDim;

        public int[] CustomColors { get; set; }
        public bool ColorsEditable { get; set; }
        public int[] SelectedIndices
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
            : this(-1, null, false, ColorSelMode.None)
        { }

        public FrmPalette(Color[] palette, bool editable, ColorSelMode selectMode)
            : this (-1, palette, editable, selectMode)
        { }

        public FrmPalette(int bitsPerPixel, Color[] palette, bool editable, ColorSelMode selectMode)
        {
            this.InitializeComponent();
            paletteDim = Math.Max(palettePanel.Width, palettePanel.Height);
            this.ColorsEditable = editable;
            if (bitsPerPixel == -1)
            {
                if (palette == null)
                {
                    bitsPerPixel = 8;
                }
                else
                {
                    int palLen = palette.Length;
                    bitsPerPixel = 1;
                    while ((1 << bitsPerPixel) < palLen && bitsPerPixel < 8)
                        bitsPerPixel *= 2;
                }
            }

            PalettePanel.InitPaletteControl(bitsPerPixel, palettePanel, palette, paletteDim);
            this.palettePanel.Palette = palette;
            this.palettePanel.ColorSelectMode = selectMode;
        }

        public Color[] GetSelectedColors()
        {
            int[] selectedIndices = this.SelectedIndices;
            Color[] allColors = this.palettePanel.Palette;
            int selLen = selectedIndices.Length;
            Color[] selCol = new Color[selLen];
            for (int i = 0; i < selLen; ++i)
                selCol[i] = allColors[selectedIndices[i]];
            return selCol;
        }

        private void PalettePanel_LabelMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!this.ColorsEditable || e.Button != MouseButtons.Left)
                return;
            PalettePanel panel = sender as PalettePanel;
            PaletteClickEventArgs palEv = e as PaletteClickEventArgs;
            if (panel == null || palEv == null)
                return;
            int colindex = palEv.Index;
            using (ColorDialog cdl = new ColorDialog())
            {
                Color[] pal = panel.Palette;
                cdl.Color = pal[colindex];
                cdl.FullOpen = true;
                cdl.CustomColors = this.CustomColors;
                DialogResult res = cdl.ShowDialog();
                this.CustomColors = cdl.CustomColors;
                if (res == DialogResult.OK || res == DialogResult.Yes)
                {
                    pal[colindex] = cdl.Color;
                    this.palettePanel.Palette = pal;
                }
            }
        }
    }
}
