using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ColorManipulation;

namespace CnC64ImgViewer.Ui
{
    public partial class PalettePanel : UserControl
    {
        protected Label[] m_ColorLabels;

        protected Padding m_Padding = new Padding(0, 0, 0, 0);
        protected Size m_LabelSize = new Size(16, 16);
        protected Point m_PadBetween = new Point(4, 4);

        protected Color[] m_Palette;
        protected Int32[] m_Remap;
        protected List<Int32> m_SelectedIndices = new List<Int32>();

        protected Color m_EmptyIndicatorBackColor = Color.Black;
        protected Char m_EmptyIndicatorChar = 'X';
        protected Color m_EmptyIndicatorCharColor = Color.Red;

        protected Boolean m_ShowColorToolTips = true;
        protected Boolean m_Selectable = true;
        protected Boolean m_Multiselect = false;
        protected Boolean m_ShowRemappedPalette = true;

        [Description("Frame size. This is completely determined by the padding, label size, and padding between the labels, and can't be modified."), Category("Palette panel")]
        public new Size Size
        {
            get { return base.Size; }
            set
            {
                Int32 sizeX = m_Padding.Left + LabelSize.Width * 16 + PadBetween.X * 15 + m_Padding.Right;
                Int32 sizeY = m_Padding.Top + LabelSize.Height * 16 + PadBetween.Y * 15 + m_Padding.Bottom;
                base.Size = new Size(sizeX, sizeY);
                Refresh();
            }
        }
        
        [Description("Border padding around the control"), Category("Palette panel")]
        public Padding Border
        {
            get { return m_Padding; }
            set { this.m_Padding = value; Refresh(); }
        }

        [Description("Determines the size of the color labels."), Category("Palette panel")]
        public Size LabelSize
        {
            get { return m_LabelSize; }
            set { this.m_LabelSize = value; Refresh(); }
        }
        
        [Description("Padding between the labels."), Category("Palette panel")]
        public Point PadBetween
        {
            get { return m_PadBetween; }
            set { this.m_PadBetween = value; Refresh(); }
        }

        [Description("Color palette. This is normally not set manually through the designer."), Category("Palette panel")]
        public Color[] Palette
        {
            get { return m_Palette; }
            set { this.m_Palette = value; Refresh(); }
        }

        [Description("Table used to remap the color palette. Set to null for no remapping."), Category("Palette panel")]
        public Int32[] Remap
        {
            get { return m_Remap; }
            set { this.m_Remap = value; Refresh(); }
        }

        [Description("Selected indices on the palette."), Category("Palette panel")]
        public Int32[] SelectedIndices
        {
            get
            {
                return m_SelectedIndices.ToArray();
            }
            set
            {
                m_SelectedIndices.Clear();
                if (value != null)
                {
                    foreach (Int32 i in value)
                        if (!m_SelectedIndices.Contains(i) && i >= 0 && i <= 255)
                            m_SelectedIndices.Add(i);
                    m_SelectedIndices.Sort();
                    if (!m_Multiselect && m_SelectedIndices.Count > 1)
                    {
                        Int32 selected = m_SelectedIndices[0];
                        m_SelectedIndices.Clear();
                        m_SelectedIndices.Add(selected);
                    }
                }
                if (ColorSelectionChanged != null)
                    ColorSelectionChanged(-1, new EventArgs());
                Refresh();
            }
        }

        [Description("Color used to indicate entries not filled in on the palette."), Category("Palette panel")]
        public Color EmptyIndicatorBackColor
        {
            get { return m_EmptyIndicatorBackColor; }
            set { m_EmptyIndicatorBackColor = value; Refresh(); }
        }

        [Description("Character put on labels to indicate entries not filled in on the palette."), Category("Palette panel")]
        public Char EmptyIndicatorChar
        {
            get { return m_EmptyIndicatorChar; }
            set { m_EmptyIndicatorChar = value; Refresh(); }
        }

        [Description("Color of the character put on labels to indicate entries not filled in on the palette."), Category("Palette panel")]
        public Color EmptyIndicatorCharColor
        {
            get { return m_EmptyIndicatorCharColor; }
            set { m_EmptyIndicatorCharColor = value; Refresh(); }
        }
        
        [Description("Show tooltips on the labels, giving the index and color values."), Category("Palette panel")]
        public Boolean ShowColorToolTips
        {
            get { return this.m_ShowColorToolTips; }
            set { this.m_ShowColorToolTips = value; Refresh(); }
        }

        [Description("Allow selecting of colors on the palette."), Category("Palette panel")]
        public Boolean Selectable
        {
            get { return this.m_Selectable; }
            set { this.m_Selectable = value; }
        }

        [Description("Show the remapped palette instead of the original palette. Note that this does not change the Palette property."), Category("Palette panel")]
        public Boolean ShowRemappedPalette
        {
            get { return this.m_ShowRemappedPalette; }
            set { this.m_ShowRemappedPalette = value; }
        }

        [Description("Allow selecting of multiple colors on the palette."), Category("Palette panel")]
        public Boolean Multiselect
        {
            get { return this.m_Multiselect; }
            set
            {
                this.m_Multiselect = value;
                if (!m_Multiselect)
                {
                    Int32[] indices = SelectedIndices;
                    if (indices.Length > 0)
                        SelectedIndices = new Int32[] { indices[0] };
                }
                Refresh();
            }
        }

        [Description("Occurs when one of the labels is double clicked by the mouse."), Category("Palette panel")]
        public event MouseEventHandler LabelMouseDoubleClick;

        [Description("Occurs when the selection of the color labels has changed. Sender contains the index of the clicked label, or -1 if set through setting SelectedIndices"), Category("Palette panel")]
        public event EventHandler ColorSelectionChanged;

        public void SetVisibility(Int32[] colorLabelIndices, Boolean visible)
        {
            if (m_ColorLabels == null)
                return;
            for (Int32 i = 0; i < m_ColorLabels.Length; i++)
                if (colorLabelIndices.Contains(i))
                    m_ColorLabels[i].Visible = visible;
                else
                    m_ColorLabels[i].Visible = !visible;
            Refresh();
        }

        public void SetVisibility(Int32 colorLabelIndex, Boolean visible)
        {
            if (m_ColorLabels == null || colorLabelIndex < 0 || colorLabelIndex >= m_ColorLabels.Length)
                return;
            m_ColorLabels[colorLabelIndex].Visible = visible;
            Refresh();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PalettePanel()
        {
            InitializeComponent();
            DrawPalette();
            this.Paint += PalettePanel_Paint;
        }

        protected void PalettePanel_Paint(object sender, PaintEventArgs e)
        {
            this.SuspendLayout();
            DrawPalette();
            ResetTooltips();
            this.ResumeLayout(false);
        }

        protected void ResetTooltips()
        {
            if (this.m_ShowColorToolTips)
            {
                for (int i = 0; i < m_ColorLabels.Length; i++)
                    this.SetColorToolTip(i);
            }
            else
                this.toolTipColor.RemoveAll();
        }

        protected void DrawPalette()
        {
            Boolean HasColor = m_Palette != null;
            Boolean HasRemap = m_Remap != null;
            Boolean newPalette = m_ColorLabels == null;
            if (newPalette)
                m_ColorLabels = new Label[256];
            for (Int32 y = 0; y < 16; y++)
            {
                for (Int32 x = 0; x < 16; x++)
                {
                    Int32 index = y * 16 + x;

                    Color col = m_EmptyIndicatorBackColor;
                    Boolean emptyCol = false;
                    String lbl = String.Empty;
                    if (HasColor)
                    {
                        if (HasRemap && m_ShowRemappedPalette)
                        {
                            Int32 filterIndex = m_Remap[index];
                            if (index < m_Remap.Length && filterIndex >= 0 && filterIndex < m_Palette.Length)
                                col = m_Palette[filterIndex];
                            else
                                emptyCol = true;
                        }
                        else if (index < m_Palette.Length)
                        {
                            col = m_Palette[index];
                        }
                        else
                            emptyCol = true;
                        if (col.IsEmpty)
                            emptyCol = true;
                    }
                    else
                        emptyCol = true;

                    if (emptyCol == true)
                        lbl = m_EmptyIndicatorChar.ToString();

                    Boolean selectThis = m_SelectedIndices.Contains(index);
                    if (newPalette)
                        this.m_ColorLabels[index] = this.GenerateLabel(x, y, col, lbl, selectThis);
                    else
                        this.SetLabelProperties(this.m_ColorLabels[index], x, y, col, lbl, selectThis);
                    if (m_ShowColorToolTips)
                        this.SetColorToolTip(index);
                    if (newPalette) 
                        this.Controls.Add(m_ColorLabels[index]);
                }
            }
            Int32 sizeX = m_Padding.Left + LabelSize.Width * 16 + PadBetween.X * 15 + m_Padding.Right;
            Int32 sizeY = m_Padding.Top + LabelSize.Height * 16 + PadBetween.Y * 15 + m_Padding.Bottom;
            base.Size = new Size(sizeX, sizeY);
        }

        protected virtual void SetColorToolTip(Int32 index)
        {
            Label lbl = m_ColorLabels[index];
            String tooltipString;
            if (m_Palette == null || index < 0 || index >= m_Palette.Length || String.Equals(lbl.Text, m_EmptyIndicatorChar.ToString()))
            {
                tooltipString = "No color set";
            }
            else
            {
                Color c = lbl.BackColor;
                tooltipString = "#" + index;
                if (m_Remap != null && m_ShowRemappedPalette && m_Remap[index] >= 0)
                    tooltipString += " -> #" + m_Remap[index];
                tooltipString += " (" + c.R + "," + c.G + "," + c.B + ")" + (c.A != 255 ? " (" + (c.A == 0 ? "Transparent" : "Alpha: " + c.A) + ")" : String.Empty);
            }
            this.toolTipColor.SetToolTip(lbl, tooltipString);
        }

        protected virtual Label GenerateLabel(Int32 x, Int32 y, Color color, String text, Boolean addBorder)
        {
            Label lbl = new System.Windows.Forms.Label();
            SetLabelProperties(lbl, x, y, color, text, addBorder);
            lbl.MouseClick += new System.Windows.Forms.MouseEventHandler(this.ColorMouseClick);
            lbl.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ColorMouseDoubleClick);
            lbl.Paint += new System.Windows.Forms.PaintEventHandler(this.lblColor_Paint);
            return lbl;
        }

        protected virtual void SetLabelProperties(Label lbl, Int32 x, Int32 y, Color color, String text, Boolean addBorder)
        {
            Int32 index = y * 16 + x;
            lbl.BackColor = color.IsEmpty ? m_EmptyIndicatorBackColor : color;
            lbl.BorderStyle = addBorder ? BorderStyle.FixedSingle : BorderStyle.None;
            lbl.Location = new Point(m_Padding.Left + (LabelSize.Width + PadBetween.X) * x,
                                        m_Padding.Top + (LabelSize.Height + PadBetween.Y) * y);
            lbl.Name = "color" + index;
            lbl.Size = LabelSize;
            lbl.TabIndex = index;
            lbl.Margin = new System.Windows.Forms.Padding(0);
            lbl.Tag = index;
            lbl.Text = text;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.ForeColor = m_EmptyIndicatorCharColor;
            
        }

        protected virtual void lblColor_Paint(Object sender, PaintEventArgs e)
        {
            Label lbl = (Label)sender;
            if (lbl.BorderStyle == BorderStyle.FixedSingle)
                ControlPaint.DrawBorder(e.Graphics, lbl.DisplayRectangle, ImageUtils.GetVisibleBorderColor(lbl.BackColor), ButtonBorderStyle.Solid);
        }

        protected virtual void ColorMouseClick(object sender, MouseEventArgs e)
        {
            if (!m_Selectable || e.Button != System.Windows.Forms.MouseButtons.Left)
                return;
            Label lbl = (Label)sender;
            Int32 index = (Int32)lbl.Tag;
            
            if (!m_Multiselect)
            {
                foreach (Int32 i in m_SelectedIndices)
                    m_ColorLabels[i].BorderStyle = BorderStyle.None;
                m_SelectedIndices.Clear();
                m_SelectedIndices.Add(index);
                lbl.BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                if (!m_SelectedIndices.Contains(index))
                {
                    m_SelectedIndices.Add(index);
                    m_SelectedIndices.Sort();
                    lbl.BorderStyle = BorderStyle.FixedSingle;
                }
                else
                {
                    m_SelectedIndices.RemoveAll(i => i == index);
                    lbl.BorderStyle = BorderStyle.None;
                }
            }
            if (this.ColorSelectionChanged != null)
                this.ColorSelectionChanged(index, e);
        }

        protected virtual void ColorMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (LabelMouseDoubleClick != null)
            {
                Label lbl = (Label)sender;
                Int32 colindex = (Int32)lbl.Tag;
                LabelMouseDoubleClick(colindex, e);
            }
        }

        protected virtual void BackgroundMouseDoubleClick(object sender, MouseEventArgs e)
        {
            // disabled for now. Could be annoying when selecting a lot of indices,
            // if an accidental doubleclick on the background clears them all.
            /*/
            foreach (Int32 index in m_SelectedIndices)
                m_ColorLabels[index].BorderStyle = BorderStyle.None;
            this.m_SelectedIndices.Clear();
            //*/
        }
    }
}
