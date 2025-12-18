using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    // Should actually change depending on the color select mode...
    //[DefaultEvent("ColorSelectionChanged")]
    [DefaultEvent("ColorLabelMouseClick")]
    public partial class PalettePanel : UserControl
    {
        protected static Padding DefaultLabelPadding = new Padding(2);

        protected Label[] m_ColorLabels;

        //protected Padding Padding = new Padding(0, 0, 0, 0);
        protected Size m_LabelSize = new Size(16, 16);
        protected Point m_PadBetween = new Point(4, 4);

        protected Color[] m_Palette;
        protected Int32[] m_Remap;
        protected ColorSelMode m_ColorSelectMode = ColorSelMode.Single;
        protected Int32[] m_SelectedIndicesArr = new Int32[1];
        protected List<Int32> m_SelectedIndicesList = null;

        protected Color m_EmptyItemBackColor = Color.Black;
        protected Char m_EmptyItemChar = 'X';
        protected Color m_EmptyItemCharColor = Color.Red;
        protected String m_EmptyItemToolTip = "No color set";

        protected Color m_TransItemBackColor = Color.Empty;
        protected Char m_TransItemChar = 'T';
        protected Color m_TransItemCharColor = Color.Blue;

        protected Int32 m_ColorTableWidth = 16;
        protected Int32 m_MaxColors = 256;
        protected Boolean m_ShowColorToolTips = true;
        protected Boolean m_ShowRemappedPalette = false;

        public static void InitPaletteControl(Int32 bitsPerPixel, PalettePanel palPanel, Color[] palette, Int32 maxDimension)
        {
            Int32 colors = 1 << bitsPerPixel;
            palPanel.MaxColors = colors;
            Int32 squaresPerRow = (Int32)Math.Sqrt(colors);
            Int32 squaresPerCol = colors / squaresPerRow + ((colors % squaresPerRow) > 0 ? 1 : 0);
            squaresPerRow = Math.Max(squaresPerRow, squaresPerCol);
            Int32 sqrWidth = (Int32)Math.Ceiling(maxDimension * 7.5 / 8.5 / squaresPerRow);
            Int32 padding = (Int32)Math.Max(1, Math.Round(sqrWidth / 8.5));
            while (maxDimension < squaresPerRow * sqrWidth + (squaresPerRow - 1) * padding)
            {
                sqrWidth--;
                padding = (Int32)Math.Max(1, Math.Ceiling(sqrWidth / 8.5));
            }
            palPanel.ColorTableWidth = squaresPerRow;
            palPanel.LabelSize = new Size(sqrWidth, sqrWidth);
            palPanel.PadBetween = new Point(padding, padding);
            palPanel.Palette = palette;
        }


        [Description("Frame size. This is completely determined by the padding, label size, and padding between the labels, and can't be modified."), Category("Palette panel")]
        [DefaultValue(typeof(Size), "320, 320")]
        public new Size Size
        {
            get { return base.Size; }
            set { ResetSize(); }
        }

        public new Int32 Width
        {
            get { return Size.Width; }
            set { ResetSize(); }
        }
        public new Int32 Height
        {
            get { return Size.Height; }
            set { ResetSize(); }
        }

        [Description("Autosize"), Category("Layout")]
        public new Boolean AutoSize
        {
            get { return true; }
            set {  }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Padding), "2, 2, 2, 2")]
        public new Padding Padding
        {
            get { return base.Padding; }
            set { base.Padding = value; ResetSize(); }
        }

        [Description("Determines the size of the color labels."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Size), "16, 16")]
        public Size LabelSize
        {
            get { return m_LabelSize; }
            set { this.m_LabelSize = value; ResetSize(); }
        }

        [Description("Padding between the labels."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Point), "4, 4")]
        public Point PadBetween
        {
            get { return m_PadBetween; }
            set { this.m_PadBetween = value; ResetSize(); }
        }

        [Description("Color palette. This is normally not set manually through the designer."), Category("Palette panel")]
        public Color[] Palette
        {
            get { return m_Palette; }
            set { this.m_Palette = value; this.Invalidate(); }
        }

        [Description("Maximum amount of colours that can be shown on the palette."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(256)]
        public Int32 MaxColors
        {
            get { return m_MaxColors; }
            set { this.m_MaxColors = value; ResetSize(); }
        }

        [Description("Amount of colors shown on each rows."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(16)]
        public Int32 ColorTableWidth
        {
            get { return this.m_ColorTableWidth; }
            set { this.m_ColorTableWidth = value; ResetSize(); }
        }

        [Description("Table used to remap the color palette. Set to null for no remapping."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        public Int32[] Remap
        {
            get { return m_Remap; }
            set { this.m_Remap = value; this.Invalidate(); }
        }

        [Description("Selected indices on the palette. This has/expects a different array size depending on the ColorSelectMode:"
            + " None gives a 0-size array, Single gives a 1-item array, TwoMousebuttons has a 2-element array; one per mouse button, and Multi has a dynamic length depending on selected items."),
            Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        public Int32[] SelectedIndices
        {
            get
            {
                if (m_ColorSelectMode == ColorSelMode.Multi)
                    return m_SelectedIndicesList.ToArray();
                else
                {
                    return m_SelectedIndicesArr.ToArray();
                }
            }
            set
            {
                switch (m_ColorSelectMode)
                {
                    case ColorSelMode.None:
                        break;
                    case ColorSelMode.Single:
                        if (value.Length > 0)
                            m_SelectedIndicesArr[0] = value[0];
                        else
                            m_SelectedIndicesArr[0] = 0;
                        break;
                    case ColorSelMode.TwoMouseButtons:
                        if (value.Length == 0)
                        {
                            m_SelectedIndicesArr[0] = 0;
                            m_SelectedIndicesArr[1] = 1;
                        }
                        else if (value.Length == 1)
                        {
                            m_SelectedIndicesArr[0] = value[0];
                            m_SelectedIndicesArr[1] = value[0] == 0 ? 1 : 0;
                        }
                        else
                        {
                            m_SelectedIndicesArr[0] = value[0];
                            m_SelectedIndicesArr[1] = value[1];
                        }
                        break;
                    case ColorSelMode.Multi:
                        foreach (Int32 i in value)
                            if (!m_SelectedIndicesList.Contains(i) && i >= 0 && i < MaxColors)
                                m_SelectedIndicesList.Add(i);
                        break;
                }
                if (ColorSelectionChanged != null)
                    ColorSelectionChanged(this, new EventArgs());
                Refresh();
            }
        }

        [Description("Color used to indicate entries not filled in on the palette."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "0x000000")]
        public Color EmptyItemBackColor
        {
            get { return this.m_EmptyItemBackColor; }
            set { this.m_EmptyItemBackColor = value; Invalidate(); }
        }

        [Description("Character put on entries not filled in on the palette. Not drawn if EmptyItemCharColor is set to Color.Empty."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue('X')]
        public Char EmptyItemChar
        {
            get { return this.m_EmptyItemChar; }
            set { this.m_EmptyItemChar = value; Invalidate(); }
        }

        [Description("Color of the character put on entries not filled in on the palette. Setting this to Color.Empty causes the character not to be drawn."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "0xFF0000")]
        public Color EmptyItemCharColor
        {
            get { return this.m_EmptyItemCharColor; }
            set { this.m_EmptyItemCharColor = value; Invalidate(); }
        }
        
        [Description("Tooltip shown on an empty image entry. Leave empty to disable tooltips on empty entries."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue("No color set")]
        public String EmptyItemToolTip
        {
            get { return this.m_EmptyItemToolTip; }
            set { this.m_EmptyItemToolTip = value; Invalidate(); }
        }
        [Description("Color used to indicate entries that are transparent on the palette. Setting this to Color.Empty will use the value of the actual color itself, and will automatically generate a visible color for the indicator character instead of using TransItemCharColor."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        public Color TransItemBackColor
        {
            get { return this.m_TransItemBackColor; }
            set { this.m_TransItemBackColor = value; Invalidate(); }
        }

        [Description("Character put on labels to indicate entries that are transparent on the palette. Not drawn if TransItemCharColor is set to Color.Empty."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue('T')]
        public Char TransItemChar
        {
            get { return this.m_TransItemChar; }
            set { this.m_TransItemChar = value; Invalidate(); }
        }

        [Description("Color of the character put on labels to indicate entries that are transparent on the palette. Not used if TransItemBackColor is set to Color.Empty. Setting this to Color.Empty causes the character not to be drawn, regardless of the TransItemBackColor overriding its value."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "0x0000FF")]
        public Color TransItemCharColor
        {
            get { return this.m_TransItemCharColor; }
            set { this.m_TransItemCharColor = value; Invalidate(); }
        }

        [Description("Show tooltips on the labels, giving the index and color values."), Category("Palette panel")]
        [DefaultValue(true)]
        public Boolean ShowColorToolTips
        {
            get { return this.m_ShowColorToolTips; }
            set { this.m_ShowColorToolTips = value; ResetTooltips(); }
        }

        [Description("Change the way colors can be selected on the palette."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(ColorSelMode), "Single")]
        public ColorSelMode ColorSelectMode
        {
            get { return this.m_ColorSelectMode; }
            set
            {
                Int32[] selInd = this.SelectedIndices;
                this.m_ColorSelectMode = value;
                switch (ColorSelectMode)
                {
                    case ColorSelMode.None:
                        m_SelectedIndicesArr = new Int32[0];
                        m_SelectedIndicesList = null;
                        break;
                    case ColorSelMode.Single:
                    default:
                        m_SelectedIndicesArr = new Int32[1];
                        m_SelectedIndicesList = null;
                        break;
                    case ColorSelMode.TwoMouseButtons:
                        m_SelectedIndicesArr = new Int32[2];
                        m_SelectedIndicesList = null;
                        break;
                    case ColorSelMode.Multi:
                        m_SelectedIndicesArr = null;
                        m_SelectedIndicesList = new List<Int32>();
                        break;
                }
                // reset this
                this.SelectedIndices = selInd;
            }
        }

        [Description("Show the remapped palette instead of the original palette. Note that this does not change the Palette property."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(false)]
        public Boolean ShowRemappedPalette
        {
            get { return this.m_ShowRemappedPalette; }
            set { this.m_ShowRemappedPalette = value; }
        }

        [Description("Occurs when one of the labels is double clicked by the mouse."), Category("Palette panel")]
        public event PaletteClickEventHandler ColorLabelMouseDoubleClick;

        [Description("Occurs when one of the labels is clicked by the mouse."), Category("Palette panel")]
        public event PaletteClickEventHandler ColorLabelMouseClick;

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

        private void ResetSize()
        {
            Int32 rows = m_MaxColors / this.m_ColorTableWidth + (m_MaxColors % this.m_ColorTableWidth > 0 ? 1 : 0);
            Int32 sizeX = this.Padding.Left + LabelSize.Width * this.m_ColorTableWidth + PadBetween.X * (this.m_ColorTableWidth - 1) + this.Padding.Right;
            Int32 sizeY = this.Padding.Top + LabelSize.Height * rows + PadBetween.Y * (rows - 1) + this.Padding.Bottom;
            base.Size = new Size(sizeX, sizeY);
            this.Invalidate();            
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

        /// <summary>
        /// Constructor
        /// </summary>
        public PalettePanel(Int32 width, Int32 maxColors)
        {
            this.m_ColorTableWidth = width;
            m_MaxColors = maxColors;
            InitializeComponent();
            DrawPalette();
            this.Paint += PalettePanel_Paint;
        }


        protected void PalettePanel_Paint(object sender, PaintEventArgs e)
        {
            this.SuspendLayout();
            DrawPalette();
            this.ResumeLayout(false);
        }

        protected void ResetTooltips()
        {
            if (this.m_ShowColorToolTips)
            {
                for (Int32 i = 0; i < m_ColorLabels.Length; i++)
                {
                    Color col = Color.Empty;
                    Boolean emptyCol = false;
                    if (m_Palette != null)
                    {
                        col = GetColor(i);
                        if (col.IsEmpty)
                            emptyCol = true;
                    }
                    else
                        emptyCol = true;
                    Boolean transparentCol = !emptyCol && col.A == 0;
                    this.SetColorToolTip(i, emptyCol, transparentCol);
                }
            }
            else
                this.toolTipColor.RemoveAll();
        }

        protected void DrawPalette()
        {
            Boolean HasColor = m_Palette != null;
            Boolean newPalette = m_ColorLabels == null;
            Int32 rows = m_MaxColors / this.m_ColorTableWidth + ((m_MaxColors % this.m_ColorTableWidth > 0) ? 1 : 0);
            if (!newPalette && m_ColorLabels.Length != m_MaxColors)
            {
                foreach (Label colorLabel in m_ColorLabels)
                {
                    this.Controls.Remove(colorLabel);
                    colorLabel.Dispose();
                }
                newPalette = true;
            }
            if (newPalette)
                m_ColorLabels = new Label[m_MaxColors];
            for (Int32 y = 0; y < rows; y++)
            {
                for (Int32 x = 0; x < this.m_ColorTableWidth; x++)
                {
                    Int32 index = y * this.m_ColorTableWidth + x;
                    if (index >= m_MaxColors)
                        break;
                    Color col = this.m_EmptyItemBackColor;
                    Boolean emptyCol = false;
                    Boolean transparentCol = false;
                    if (HasColor)
                    {
                        col = GetColor(index);
                        if (col.IsEmpty)
                            emptyCol = true;
                    }
                    else
                        emptyCol = true;
                    if (!emptyCol && col.A == 0)
                        transparentCol = true;
                    Boolean selectThis;
                    if (this.m_ColorSelectMode == ColorSelMode.Multi)
                        selectThis = m_SelectedIndicesList.Contains(index);
                    else
                        selectThis = m_SelectedIndicesArr.Contains(index);
                    if (newPalette)
                        this.m_ColorLabels[index] = this.GenerateLabel(x, y, col, emptyCol, transparentCol, selectThis);
                    else
                        this.SetLabelProperties(this.m_ColorLabels[index], x, y, col, emptyCol, transparentCol, selectThis);
                    if (m_ShowColorToolTips)
                        this.SetColorToolTip(index, emptyCol, transparentCol);
                    if (newPalette) 
                        this.Controls.Add(m_ColorLabels[index]);
                }
            }
            if (!m_ShowColorToolTips)
                this.toolTipColor.RemoveAll();
            Int32 sizeX = this.Padding.Left + LabelSize.Width * this.m_ColorTableWidth + PadBetween.X * (this.m_ColorTableWidth - 1) + this.Padding.Right;
            Int32 sizeY = this.Padding.Top + LabelSize.Height * rows + PadBetween.Y * (rows - 1) + this.Padding.Bottom;
            base.Size = new Size(sizeX, sizeY);
        }


        protected Color GetColor(Int32 index)
        {
            if (m_Remap != null && m_ShowRemappedPalette)
            {
                Int32 filterIndex;
                if (index < m_Remap.Length && (filterIndex = m_Remap[index]) >= 0 && filterIndex < m_Palette.Length)
                    return m_Palette[filterIndex];
                else
                    return Color.Empty;
            }
            else if (index < m_Palette.Length)
                return m_Palette[index];
            return Color.Empty;
        }

        protected virtual void SetColorToolTip(Int32 index, Boolean isEmpty, Boolean isTransparent)
        {
            Label lbl = m_ColorLabels[index];
            String tooltipString;
            if (isEmpty)
            {
                if (String.IsNullOrEmpty(EmptyItemToolTip))
                    tooltipString = null;
                else
                    tooltipString = EmptyItemToolTip;
            }
            else
            {
                Color c = lbl.BackColor;
                tooltipString = "#" + index;
                if (m_Remap != null && m_ShowRemappedPalette && m_Remap[index] >= 0)
                    tooltipString += " -> #" + m_Remap[index];
                tooltipString += String.Format(" ({0},{1},{2})", c.R, c.G, c.B);
                if (isTransparent)
                    tooltipString += " (Transparent)";
            }
            this.toolTipColor.SetToolTip(lbl, tooltipString);
        }

        protected virtual Label GenerateLabel(Int32 x, Int32 y, Color color, Boolean isEmpty, Boolean isTransparent, Boolean addBorder)
        {
            Label lbl = new LabelNoCopyOnDblClick();
            SetLabelProperties(lbl, x, y, color, isEmpty, isTransparent, addBorder);
            lbl.MouseClick += this.ColorLblMouseClick;
            lbl.MouseDoubleClick += this.ColorLblMouseDoubleClick;
            lbl.ImageAlign = ContentAlignment.MiddleCenter;
            lbl.Paint += this.lblColor_Paint;
            return lbl;
        }

        protected virtual void SetLabelProperties(Label lbl, Int32 x, Int32 y, Color color, Boolean isEmpty, Boolean isTransparent, Boolean addBorder)
        {
            Int32 index = y * this.m_ColorTableWidth + x;
            if (isEmpty)
            {
                lbl.BackColor = this.m_EmptyItemBackColor;
                Boolean fgisEmpty = this.m_EmptyItemCharColor == Color.Empty;
                lbl.Text = fgisEmpty ? String.Empty : this.m_EmptyItemChar.ToString();
                lbl.ForeColor = fgisEmpty ? Color.Transparent : this.m_EmptyItemCharColor;
            }
            else if (isTransparent)
            {
                Boolean bgisEmpty = this.m_TransItemBackColor == Color.Empty;
                lbl.BackColor = bgisEmpty ? Color.FromArgb(255, color.R, color.G, color.B) : this.m_TransItemBackColor;
                Boolean fgisEmpty = this.m_TransItemCharColor == Color.Empty;
                lbl.Text = fgisEmpty ? String.Empty : this.m_TransItemChar.ToString();
                lbl.ForeColor = fgisEmpty ? Color.Transparent : bgisEmpty ? GetVisibleBorderColor(lbl.BackColor) : this.m_TransItemCharColor;
            }
            else
            {
                lbl.BackColor = color;
                lbl.Text = String.Empty;
                lbl.ForeColor = Color.Black;
            }
            
            lbl.BorderStyle = addBorder ? BorderStyle.FixedSingle : BorderStyle.None;
            lbl.Location = new Point(this.Padding.Left + (LabelSize.Width + PadBetween.X) * x,
                                        this.Padding.Top + (LabelSize.Height + PadBetween.Y) * y);
            lbl.Name = "color" + index;
            lbl.Size = LabelSize;
            lbl.TabIndex = index;
            lbl.Margin = new System.Windows.Forms.Padding(0);
            lbl.Padding = new System.Windows.Forms.Padding(0);
            // Reduce font size to fit label size if needed. Don't bother if the text is empty anyway.
            if (!String.IsNullOrEmpty(lbl.Text))
            {
                Single maxHeight = (Single)(LabelSize.Height * 6.0 / 8.0);
                Single currentFontSize;
                using (Graphics g = this.CreateGraphics())
                {
                    Single points = lbl.Font.SizeInPoints;
                    currentFontSize = points * g.DpiX / 72;
                }
                if (currentFontSize > maxHeight)
                    lbl.Font = new Font(lbl.Font.FontFamily, maxHeight, lbl.Font.Style, GraphicsUnit.Pixel);
            }
            lbl.Tag = index;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
        }

        protected virtual void lblColor_Paint(Object sender, PaintEventArgs e)
        {
            Label lbl = (Label)sender;
            if (lbl.BorderStyle == BorderStyle.FixedSingle)
            {
                ButtonBorderStyle bs = ButtonBorderStyle.Solid;
                if (m_ColorSelectMode == ColorSelMode.TwoMouseButtons)
                {
                    Int32 index = (Int32)lbl.Tag;
                    if (m_SelectedIndicesArr[0] == index)
                        bs = ButtonBorderStyle.Outset;
                    else if (m_SelectedIndicesArr[1] == index)
                        bs = ButtonBorderStyle.Inset;
                }
                ControlPaint.DrawBorder(e.Graphics, lbl.DisplayRectangle, Parent.BackColor, bs);
            }
        }

        protected virtual void ColorLblMouseClick(object sender, MouseEventArgs e)
        {
            Label lbl = (Label)sender;
            Int32 index = lbl != null? (Int32)lbl.Tag : -1;
            Int32 mousebutton = -1;
            if ((e.Button & System.Windows.Forms.MouseButtons.Left) != 0)
                mousebutton = 0;
            if ((e.Button & System.Windows.Forms.MouseButtons.Right) != 0)
                mousebutton = 1;
            if (mousebutton != -1 && index != -1)
            {
                if ((m_ColorSelectMode == ColorSelMode.Single && mousebutton == 0) || m_ColorSelectMode == ColorSelMode.TwoMouseButtons)
                {
                    Int32 oldVal = m_SelectedIndicesArr[mousebutton];
                    if (m_ColorSelectMode == ColorSelMode.Single)
                    {
                        if (index != oldVal)
                        {
                            m_ColorLabels[oldVal].BorderStyle = BorderStyle.None;
                            m_SelectedIndicesArr[0] = index;
                            lbl.BorderStyle = BorderStyle.FixedSingle;
                        }
                    }
                    else if (m_ColorSelectMode == ColorSelMode.TwoMouseButtons)
                    {
                        Int32 mousebuttonOther = mousebutton == 0 ? 1 : 0;
                        Int32 oldValOther = m_SelectedIndicesArr[mousebuttonOther];
                        if (index != oldVal)
                        {
                            if (index == oldValOther)
                            {
                                m_SelectedIndicesArr[mousebutton] = index;
                                m_SelectedIndicesArr[mousebuttonOther] = oldVal;
                                m_ColorLabels[oldVal].Invalidate();
                            }
                            else
                            {
                                m_ColorLabels[oldVal].BorderStyle = BorderStyle.None;
                                m_SelectedIndicesArr[mousebutton] = index;
                                lbl.BorderStyle = BorderStyle.FixedSingle;
                            }
                        }
                    }
                }
                else if (m_ColorSelectMode == ColorSelMode.Multi && mousebutton == 0)
                {
                    if (!m_SelectedIndicesList.Contains(index))
                    {
                        m_SelectedIndicesList.Add(index);
                        m_SelectedIndicesList.Sort();
                        lbl.BorderStyle = BorderStyle.FixedSingle;
                    }
                    else
                    {
                        m_SelectedIndicesList.RemoveAll(i => i == index);
                        lbl.BorderStyle = BorderStyle.None;
                    }
                }
                // force refresh
                lbl.Invalidate();
                if (this.ColorSelectionChanged != null)
                    this.ColorSelectionChanged(this, new PaletteClickEventArgs(e, index, GetColor(index)));
            }
            if (this.ColorLabelMouseClick != null)
                this.ColorLabelMouseClick(this, new PaletteClickEventArgs(e, index, GetColor(index)));
            
        }

        protected virtual void ColorLblMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ColorLabelMouseDoubleClick != null)
            {
                Label lbl = (Label)sender;
                Int32 index = lbl != null ? (Int32)lbl.Tag : -1;
                ColorLabelMouseDoubleClick(this, new PaletteClickEventArgs(e, index, GetColor(index)));
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

        protected static Color GetVisibleBorderColor(Color color)
        {
            float bri = color.GetBrightness();
            if (color.GetSaturation() < .16)
            {
                // this color is grey
                return bri < .5 ? Color.White : Color.Black;
            }
            return Color.FromArgb((Int32)(0x00FFFFFFu ^ (UInt32)color.ToArgb()));
        }
    }

    public enum ColorSelMode
    {
        /// <summary>No selection box is drawn on click. Use the ColorLabelMouseClick event to catch clicks.</summary>
        None,
        /// <summary>Left clicking selects a single item, which can be retrieved from SelectedIndices. In this mode, something is always selected.</summary>
        Single,
        /// <summary>Left and right clicks select two distinct items, which can be retrieved from SelectedIndices as index 0 and 1 in the array. In this mode, something is always selected for both buttons.</summary>
        TwoMouseButtons,
        /// <summary>Multi-select. Left clicking an item will select or deselect it. The full list can be retrieved from SelectedIndices.</summary>
        Multi
    }

    public delegate void PaletteClickEventHandler(object sender, PaletteClickEventArgs e);

    public class PaletteClickEventArgs : MouseEventArgs
    {
        public Int32 Index { get; private set; }
        public Color Color { get; private set; }

        public PaletteClickEventArgs(MouseEventArgs e, Int32 index, Color color)
            : base(e.Button, e.Clicks, e.X, e.Y, e.Delta)
        {
            this.Index = index;
            this.Color = color;
        }

    }

    /// <summary>
    /// Disables the "feature" that double-clicking a label copies its text. Since said copy apparently happens
    /// on the internal text variable in the Label class, an override fixes this problem.
    /// </summary>
    public class LabelNoCopyOnDblClick: Label
    {
        private String text;

        public override String Text
        {
            get
            {
                return text;
            }
            set
            {
                if (value == null)
                {
                    value = "";
                }

                if (text != value)
                {
                    text = value;
                    Refresh();
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }
    }

}
