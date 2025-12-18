using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
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

        protected Size m_LabelSize = new Size(16, 16);
        protected Point m_PadBetween = new Point(4, 4);

        protected Color[] m_Palette;
        protected Int32[] m_Remap;
        protected ColorSelMode m_ColorSelectMode = ColorSelMode.Single;
        protected Int32[] m_SelectedIndicesArr = new Int32[1];
        protected List<Int32> m_SelectedIndicesList;

        protected Color m_EmptyItemBackColor = Color.Black;
        protected Char m_EmptyItemChar = 'X';
        protected Color m_EmptyItemCharColor = Color.Red;
        protected String m_EmptyItemToolTip = "No color set";

        protected Color m_TransItemBackColor = Color.Transparent;
        protected Char m_TransItemChar = 'T';
        protected Color m_TransItemCharColor = Color.Transparent;
        protected String m_TransItemToolTip = "Transparent";

        protected Char m_AlphaItemChar = 'A';
        protected Color m_AlphaItemCharColor = Color.Transparent;

        protected Int32 m_ColorTableWidth = 16;
        protected Int32 m_MaxColors = 256;
        protected Boolean m_ShowColorToolTips = true;
        protected Boolean m_ShowColorToolTipsAlpha;
        protected Boolean m_ShowRemappedPalette;
        protected Int32 m_LastAdjustedMaxDimension = -1;
        protected Int32 m_LastAdjustedBpp = -1;

        public static void InitPaletteControl(Int32 bitsPerPixel, PalettePanel palPanel, Color[] palette, Int32 maxDimension)
        {
            if (palPanel.m_LastAdjustedMaxDimension == maxDimension && palPanel.m_LastAdjustedBpp == bitsPerPixel)
            {
                palPanel.Palette = palette;
                return;
            }
            Boolean disable = bitsPerPixel <= 0 || bitsPerPixel > 8;
            Int32 colors = disable ? 1 : 1 << bitsPerPixel;
            palPanel.MaxColors = disable ? 0 : colors;
            Int32 squaresPerRow = (Int32) Math.Sqrt(colors);
            Int32 squaresPerCol = colors == 0 ? 0 : colors / squaresPerRow + ((colors % squaresPerRow) > 0 ? 1 : 0);
            squaresPerRow = Math.Max(squaresPerRow, squaresPerCol);
            Int32 sqrWidth = (Int32) Math.Ceiling(maxDimension * 7.5 / 8.5 / squaresPerRow);
            Int32 padding = (Int32) Math.Max(1, Math.Round(sqrWidth / 8.5));
            while (maxDimension < squaresPerRow * sqrWidth + (squaresPerRow - 1) * padding)
            {
                sqrWidth--;
                padding = (Int32) Math.Max(1, Math.Ceiling(sqrWidth / 8.5));
            }
            palPanel.ColorTableWidth = squaresPerRow;
            palPanel.LabelSize = new Size(sqrWidth, sqrWidth);
            palPanel.PadBetween = new Point(padding, padding);
            palPanel.m_LastAdjustedMaxDimension = maxDimension;
            palPanel.m_LastAdjustedBpp = bitsPerPixel;
            palPanel.Palette = palette;
        }

        [Description("Frame size. This is completely determined by the padding, label size, and padding between the labels, and can't be modified."), Category("Palette panel")]
        [DefaultValue(typeof(Size), "320, 320")]
        public new Size Size
        {
            get { return base.Size; }
            set { this.ResetSize(); }
        }

        public new Int32 Width
        {
            get { return this.Size.Width; }
            set { this.ResetSize(); }
        }

        public new Int32 Height
        {
            get { return this.Size.Height; }
            set { this.ResetSize(); }
        }

        [Description("Autosize"), Category("Layout")]
        public new Boolean AutoSize
        {
            get { return true; }
            set { }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Padding), "2, 2, 2, 2")]
        public new Padding Padding
        {
            get { return base.Padding; }
            set
            {
                base.Padding = value;
                this.ResetSize();
            }
        }

        [Description("Determines the size of the color labels."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Size), "16, 16")]
        public Size LabelSize
        {
            get { return this.m_LabelSize; }
            set
            {
                this.m_LabelSize = value;
                this.ResetSize();
            }
        }

        [Description("Padding between the labels."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Point), "4, 4")]
        public Point PadBetween
        {
            get { return this.m_PadBetween; }
            set
            {
                this.m_PadBetween = value;
                this.ResetSize();
            }
        }

        [Description("Color palette. This is normally not set manually through the designer."), Category("Palette panel")]
        public Color[] Palette
        {
            get { return this.m_Palette; }
            set
            {
                this.m_Palette = value;
                this.Invalidate();
            }
        }

        [Description("Maximum amount of colors that can be shown on the palette."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(256)]
        public Int32 MaxColors
        {
            get { return this.m_MaxColors; }
            set
            {
                this.m_MaxColors = value;
                this.ResetSize();
            }
        }

        [Description("Amount of colors shown on each rows."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(16)]
        public Int32 ColorTableWidth
        {
            get { return this.m_ColorTableWidth; }
            set
            {
                this.m_ColorTableWidth = value;
                this.ResetSize();
            }
        }

        [Description("Table used to remap the color palette. Set to null for no remapping."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        public Int32[] Remap
        {
            get { return this.m_Remap; }
            set
            {
                this.m_Remap = value;
                this.Invalidate();
            }
        }

        [Description("Selected indices on the palette. This has/expects a different array size depending on the ColorSelectMode:"
                     + " None gives a 0-size array, Single gives a 1-item array, TwoMousebuttons has a 2-element array; one per mouse button, and Multi has a dynamic length depending on selected items."),
         Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        public Int32[] SelectedIndices
        {
            get
            {
                return this.m_ColorSelectMode == ColorSelMode.Multi ? this.m_SelectedIndicesList.ToArray() : this.m_SelectedIndicesArr.ToArray();
            }
            set
            {
                if (value == null)
                    value = new Int32[0];
                switch (this.m_ColorSelectMode)
                {
                    case ColorSelMode.None:
                        break;
                    case ColorSelMode.Single:
                        this.m_SelectedIndicesArr[0] = value.Length > 0 ? value[0] : 0;
                        break;
                    case ColorSelMode.TwoMouseButtons:
                        this.m_SelectedIndicesArr[0] = value.Length > 0 ? value[0] : 0;
                        this.m_SelectedIndicesArr[1] = value.Length > 1 ? value[1] : (this.m_SelectedIndicesArr[0] == 0 ? 1 : 0);
                        break;
                    case ColorSelMode.Multi:
                        this.m_SelectedIndicesList.Clear();
                        this.m_SelectedIndicesList.AddRange(value.Where(ind => ind >= 0 && ind < this.m_MaxColors).Distinct());
                        break;
                }
                if (this.ColorSelectionChanged != null)
                    this.ColorSelectionChanged(this, new EventArgs());
                this.Refresh();
            }
        }

        [Description("Color used to indicate entries not filled in on the palette. Alpha on this entry is ignored."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "Black")]
        public Color EmptyItemBackColor
        {
            get { return this.m_EmptyItemBackColor; }
            set
            {
                this.m_EmptyItemBackColor = value;
                this.Invalidate();
            }
        }

        [Description("Character put on entries not filled in on the palette. Not drawn if set to U+0000 or space."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue('X')]
        public Char EmptyItemChar
        {
            get { return this.m_EmptyItemChar; }
            set
            {
                this.m_EmptyItemChar = value;
                this.Invalidate();
            }
        }

        [Description("Color of the character put on entries not filled in on the palette. Setting this to a transparent color will automatically generate a visible color for the indicator character."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "Red")]
        public Color EmptyItemCharColor
        {
            get { return this.m_EmptyItemCharColor; }
            set
            {
                this.m_EmptyItemCharColor = value;
                this.Invalidate();
            }
        }

        [Description("Tooltip shown on an empty color entry if ShowColorToolTips is enabled. Leave empty to disable tooltips on empty entries."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue("No color set")]
        public String EmptyItemToolTip
        {
            get { return this.m_EmptyItemToolTip; }
            set
            {
                this.m_EmptyItemToolTip = value;
                this.Invalidate();
            }
        }

        [Description("Color used to indicate entries that are transparent on the palette. Setting this to a transparent color will use the value of the actual color itself."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "Transparent")]
        public Color TransItemBackColor
        {
            get { return this.m_TransItemBackColor; }
            set
            {
                this.m_TransItemBackColor = value;
                this.Invalidate();
            }
        }

        [Description("Character put on labels to indicate entries that are transparent on the palette. Not drawn if set to U+0000 or space."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue('T')]
        public Char TransItemChar
        {
            get { return this.m_TransItemChar; }
            set
            {
                this.m_TransItemChar = value;
                this.Invalidate();
            }
        }

        [Description("Color of the character put on labels to indicate entries that are transparent on the palette. Setting this to a transparent color will automatically generate a visible color for the indicator character."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "Transparent")]
        public Color TransItemCharColor
        {
            get { return this.m_TransItemCharColor; }
            set
            {
                this.m_TransItemCharColor = value;
                this.Invalidate();
            }
        }
        
        [Description("Character put on labels to indicate entries that are translucent on the palette. Not drawn if set to \0 or space."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue('A')]
        public Char AlphaItemChar
        {
            get { return this.m_AlphaItemChar; }
            set
            {
                this.m_AlphaItemChar = value;
                this.Invalidate();
            }
        }

        [Description("Color of the character put on labels to indicate entries that are translucent on the palette. Setting this to a transparent color will automatically generate a visible color for the indicator character."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "Transparent")]
        public Color AlphaItemCharColor
        {
            get { return this.m_AlphaItemCharColor; }
            set
            {
                this.m_AlphaItemCharColor = value;
                this.Invalidate();
            }
        }

        [Description("Show tooltips on the labels, giving the index and color values."), Category("Palette panel")]
        [DefaultValue(true)]
        public Boolean ShowColorToolTips
        {
            get { return this.m_ShowColorToolTips; }
            set
            {
                this.m_ShowColorToolTips = value;
                this.ResetTooltips();
            }
        }

        [Description("If ShowColorToolTips is enabled, add alpha to the shown color values."), Category("Palette panel")]
        [DefaultValue(false)]
        public Boolean ShowColorToolTipsAlpha
        {
            get { return this.m_ShowColorToolTipsAlpha; }
            set
            {
                this.m_ShowColorToolTipsAlpha = value;
                this.ResetTooltips();
            }
        }

        [Description("String to show on the tooltip to indicate transparent colors if ShowColorToolTips is enabled. Leave empty to disable specific transparency indication."), Category("Palette panel")]
        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue("Transparent")]
        public String TransItemToolTip
        {
            get { return this.m_TransItemToolTip; }
            set
            {
                this.m_TransItemToolTip = value;
                this.Invalidate();
            }
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
                switch (this.ColorSelectMode)
                {
                    case ColorSelMode.None:
                        this.m_SelectedIndicesArr = new Int32[0];
                        this.m_SelectedIndicesList = null;
                        break;
                    case ColorSelMode.Single:
                    default:
                        this.m_SelectedIndicesArr = new Int32[1];
                        this.m_SelectedIndicesList = null;
                        break;
                    case ColorSelMode.TwoMouseButtons:
                        this.m_SelectedIndicesArr = new Int32[2];
                        this.m_SelectedIndicesList = null;
                        break;
                    case ColorSelMode.Multi:
                        this.m_SelectedIndicesArr = null;
                        this.m_SelectedIndicesList = new List<Int32>();
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
            if (this.m_ColorLabels == null)
                return;
            Int32 nrOfLabels = this.m_ColorLabels.Length;
            for (Int32 i = 0; i < nrOfLabels; ++i)
                if (colorLabelIndices.Contains(i))
                    this.m_ColorLabels[i].Visible = visible;
                else
                    this.m_ColorLabels[i].Visible = !visible;
            this.Refresh();
        }

        private void ResetSize()
        {
            this.m_LastAdjustedMaxDimension = -1;
            this.m_LastAdjustedBpp = -1;
            Int32 rows = this.m_MaxColors / this.m_ColorTableWidth + (this.m_MaxColors % this.m_ColorTableWidth > 0 ? 1 : 0);
            Int32 sizeX = this.Padding.Left + this.m_LabelSize.Width * this.m_ColorTableWidth + this.m_PadBetween.X * (this.m_ColorTableWidth - 1) + this.Padding.Right;
            Int32 sizeY = this.Padding.Top + this.m_LabelSize.Height * rows + this.m_PadBetween.Y * (rows - 1) + this.Padding.Bottom;
            base.Size = new Size(sizeX, sizeY);
            this.Invalidate();
        }

        public void SetVisibility(Int32 colorLabelIndex, Boolean visible)
        {
            if (this.m_ColorLabels == null || colorLabelIndex < 0 || colorLabelIndex >= this.m_ColorLabels.Length)
                return;
            this.m_ColorLabels[colorLabelIndex].Visible = visible;
            this.Refresh();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PalettePanel()
        {
            this.InitializeComponent();
            this.DrawPalette();
            this.Paint += this.PalettePanel_Paint;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PalettePanel(Int32 width, Int32 maxColors)
        {
            this.m_ColorTableWidth = width;
            this.m_MaxColors = maxColors;
            this.InitializeComponent();
            this.DrawPalette();
            this.Paint += this.PalettePanel_Paint;
        }
        
        protected void PalettePanel_Paint(Object sender, PaintEventArgs e)
        {
            this.SuspendLayout();
            this.DrawPalette();
            this.ResumeLayout(false);
        }

        protected void ResetTooltips()
        {
            if (this.m_ShowColorToolTips)
            {
                Int32 nrOfLabels = this.m_ColorLabels.Length;
                for (Int32 i = 0; i < nrOfLabels; ++i)
                {
                    Color col = Color.Empty;
                    Boolean emptyCol = false;
                    if (this.m_Palette != null)
                    {
                        col = this.GetColor(i);
                        if (col.IsEmpty)
                            emptyCol = true;
                    }
                    else
                        emptyCol = true;
                    this.SetColorToolTip(i, emptyCol, col.A);
                }
            }
            else
                this.toolTipColor.RemoveAll();
        }

        protected void DrawPalette()
        {
            Boolean hasColor = this.m_Palette != null;
            Boolean newPalette = this.m_ColorLabels == null;
            Int32 rows = this.m_MaxColors / this.m_ColorTableWidth + ((this.m_MaxColors % this.m_ColorTableWidth > 0) ? 1 : 0);
            if (newPalette)
                this.m_ColorLabels = new Label[this.m_MaxColors];
            else
            {
                Int32 nrOfLabels = this.m_ColorLabels.Length;
                for (Int32 i = this.m_MaxColors; i < nrOfLabels; ++i)
                {
                    Label colorLabel = this.m_ColorLabels[i];
                    this.Controls.Remove(colorLabel);
                    colorLabel.Dispose();
                    this.m_ColorLabels[i] = null;
                }
                Label[] newLabels = new Label[this.m_MaxColors];
                Array.Copy(this.m_ColorLabels, newLabels, Math.Min(this.m_ColorLabels.Length, this.m_MaxColors));
                this.m_ColorLabels = newLabels;
            }
            Color emptyCol = Color.FromArgb(this.m_EmptyItemBackColor.R, this.m_EmptyItemBackColor.G, this.m_EmptyItemBackColor.B);
            for (Int32 y = 0; y < rows; ++y)
            {
                for (Int32 x = 0; x < this.m_ColorTableWidth; ++x)
                {
                    Int32 index = y * this.m_ColorTableWidth + x;
                    if (index >= this.m_MaxColors)
                        break;
                    Color col;
                    Boolean isEmptyCol = false;
                    Int32 alpha;
                    if (hasColor)
                    {
                        col = this.GetColor(index);
                        alpha = col.A;
                        if (col.IsEmpty)
                        {
                            isEmptyCol = true;
                            col = emptyCol;
                        }
                    }
                    else
                    {
                        isEmptyCol = true;
                        col = emptyCol;
                        alpha = 0;
                    }
                    Boolean selectThis = this.m_ColorSelectMode == ColorSelMode.Multi ? this.m_SelectedIndicesList.Contains(index) : this.m_SelectedIndicesArr.Contains(index);
                    if (this.m_ColorLabels[index] == null)
                    {
                        this.m_ColorLabels[index] = this.GenerateLabel(x, y, col, isEmptyCol, selectThis);
                        this.Controls.Add(this.m_ColorLabels[index]);
                    }
                    else
                        this.SetLabelProperties(this.m_ColorLabels[index], x, y, col, isEmptyCol, selectThis);
                    if (this.m_ShowColorToolTips)
                        this.SetColorToolTip(index, isEmptyCol, alpha);
                }
            }
            if (!this.m_ShowColorToolTips)
                this.toolTipColor.RemoveAll();
            Int32 sizeX = this.Padding.Left + this.m_LabelSize.Width * this.m_ColorTableWidth + this.m_PadBetween.X * (this.m_ColorTableWidth - 1) + this.Padding.Right;
            Int32 sizeY = this.Padding.Top + this.m_LabelSize.Height * rows + this.m_PadBetween.Y * (rows - 1) + this.Padding.Bottom;
            base.Size = new Size(sizeX, sizeY);
        }

        protected Color GetColor(Int32 index)
        {
            if (this.m_Remap != null && this.m_ShowRemappedPalette)
            {
                Int32 filterIndex;
                if (index < this.m_Remap.Length && (filterIndex = this.m_Remap[index]) >= 0 && filterIndex < this.m_Palette.Length)
                    return this.m_Palette[filterIndex];
                return Color.Empty;
            }
            if (index < this.m_Palette.Length)
                return this.m_Palette[index];
            return Color.Empty;
        }

        protected virtual void SetColorToolTip(Int32 index, Boolean isEmpty, Int32 alpha)
        {
            Label lbl = this.m_ColorLabels[index];
            String tooltipString;
            if (isEmpty)
            {
                tooltipString = String.IsNullOrEmpty(this.EmptyItemToolTip) ? null : this.EmptyItemToolTip;
            }
            else
            {
                Color c = lbl.BackColor;
                StringBuilder tssb = new StringBuilder();
                tssb.Append("#").Append(index);
                if (this.m_Remap != null && this.m_ShowRemappedPalette && this.m_Remap[index] >= 0)
                    tssb.Append(" -> #").Append(this.m_Remap[index]);
                tssb.Append(" (");
                if (this.m_ShowColorToolTipsAlpha)
                    tssb.Append(alpha).Append(",");
                tssb.Append(c.R).Append(",").Append(c.G).Append(",").Append(c.B).Append(")");
                if (alpha == 0 && !String.IsNullOrEmpty(this.m_TransItemToolTip))
                    tssb.Append(" (").Append(this.m_TransItemToolTip).Append(")");
                tooltipString = tssb.ToString();
            }
            this.toolTipColor.SetToolTip(lbl, tooltipString);
        }

        protected virtual Label GenerateLabel(Int32 x, Int32 y, Color color, Boolean isEmpty, Boolean addBorder)
        {
            Label lbl = new LabelNoCopyOnDblClick();
            this.SetLabelProperties(lbl, x, y, color, isEmpty, addBorder);
            lbl.MouseClick += this.ColorLblMouseClick;
            lbl.MouseDoubleClick += this.ColorLblMouseDoubleClick;
            lbl.ImageAlign = ContentAlignment.MiddleCenter;
            lbl.Paint += this.lblColor_Paint;
            return lbl;
        }

        protected virtual void SetLabelProperties(Label lbl, Int32 x, Int32 y, Color color, Boolean isEmpty, Boolean addBorder)
        {
            Int32 index = y * this.m_ColorTableWidth + x;
            Int32 alpha = color.A;
            if (isEmpty)
            {
                lbl.BackColor = color;
                Boolean charIsEmpty = this.m_EmptyItemChar == '\0' || this.m_EmptyItemChar == ' ';
                Boolean fgisEmpty = charIsEmpty || this.m_EmptyItemCharColor.A == 0;
                lbl.Text = charIsEmpty ? String.Empty : this.m_EmptyItemChar.ToString();
                lbl.ForeColor = fgisEmpty ? GetVisibleColorOn(color) : Color.FromArgb(255, this.m_EmptyItemCharColor.R, this.m_EmptyItemCharColor.G, this.m_EmptyItemCharColor.B);
            }
            else if (alpha != 255)
            {
                Color indicCharColor = alpha != 0 ? this.m_AlphaItemCharColor : this.m_TransItemCharColor;
                Char indicChar = alpha != 0 ? this.m_AlphaItemChar : this.m_TransItemChar;
                lbl.BackColor = alpha != 0 || this.m_TransItemBackColor.A == 0 ?
                    Color.FromArgb(255, color.R, color.G, color.B) : Color.FromArgb(255, this.m_TransItemBackColor.R, this.m_TransItemBackColor.G, this.m_TransItemBackColor.B);
                Boolean charIsEmpty = indicChar == '\0' || indicChar == ' ';
                Boolean fgIsEmpty = indicCharColor.A == 0;
                lbl.Text = charIsEmpty ? String.Empty : indicChar.ToString();
                lbl.ForeColor = charIsEmpty ? Color.Transparent : fgIsEmpty ? GetVisibleColorOn(lbl.BackColor) : indicCharColor;
            }
            else
            {
                lbl.BackColor = color;
                lbl.Text = String.Empty;
                lbl.ForeColor = Color.Black;
            }

            lbl.BorderStyle = addBorder ? BorderStyle.FixedSingle : BorderStyle.None;
            lbl.Location = new Point(this.Padding.Left + (this.m_LabelSize.Width + this.PadBetween.X) * x,
                this.Padding.Top + (this.m_LabelSize.Height + this.PadBetween.Y) * y);
            lbl.Name = "col" + index.ToString("D3");
            lbl.Size = this.m_LabelSize;
            lbl.TabIndex = index;
            lbl.Margin = new Padding(0);
            lbl.Padding = new Padding(0);
            // Reduce font size to fit label size if needed. Don't bother if the text is empty anyway.
            if (!String.IsNullOrEmpty(lbl.Text))
            {
                Single maxHeight = (Single)(this.m_LabelSize.Height * 6.0 / 8.0);
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
            Label lbl = sender as Label;
            if (lbl == null || !(lbl.Tag is Int32) || lbl.BorderStyle != BorderStyle.FixedSingle)
                return;
            ButtonBorderStyle bs = ButtonBorderStyle.Solid;
            if (this.m_ColorSelectMode == ColorSelMode.TwoMouseButtons)
            {
                Int32 index = (Int32) lbl.Tag;
                if (this.m_SelectedIndicesArr[0] == index)
                    bs = ButtonBorderStyle.Outset;
                else if (this.m_SelectedIndicesArr[1] == index)
                    bs = ButtonBorderStyle.Inset;
            }
            ControlPaint.DrawBorder(e.Graphics, lbl.DisplayRectangle, this.Parent.BackColor, bs);
        }

        protected virtual void ColorLblMouseClick(Object sender, MouseEventArgs e)
        {
            Label lbl = (Label) sender;
            if (lbl == null || !(lbl.Tag is Int32))
                return;
            Int32 index = (Int32)lbl.Tag;
            Int32 mousebutton = -1;
            if ((e.Button & MouseButtons.Left) != 0)
                mousebutton = 0;
            if ((e.Button & MouseButtons.Right) != 0)
                mousebutton = 1;
            if (mousebutton != -1)
            {
                if ((this.m_ColorSelectMode == ColorSelMode.Single && mousebutton == 0) || this.m_ColorSelectMode == ColorSelMode.TwoMouseButtons)
                {
                    Int32 oldVal = this.m_SelectedIndicesArr[mousebutton];
                    if (this.m_ColorSelectMode == ColorSelMode.Single)
                    {
                        if (index != oldVal)
                        {
                            this.m_ColorLabels[oldVal].BorderStyle = BorderStyle.None;
                            this.m_SelectedIndicesArr[0] = index;
                            lbl.BorderStyle = BorderStyle.FixedSingle;
                        }
                    }
                    else if (this.m_ColorSelectMode == ColorSelMode.TwoMouseButtons)
                    {
                        Int32 mousebuttonOther = mousebutton == 0 ? 1 : 0;
                        Int32 oldValOther = this.m_SelectedIndicesArr[mousebuttonOther];
                        if (index != oldVal)
                        {
                            if (index == oldValOther)
                            {
                                this.m_SelectedIndicesArr[mousebutton] = index;
                                this.m_SelectedIndicesArr[mousebuttonOther] = oldVal;
                                this.m_ColorLabels[oldVal].Invalidate();
                            }
                            else
                            {
                                this.m_ColorLabels[oldVal].BorderStyle = BorderStyle.None;
                                this.m_SelectedIndicesArr[mousebutton] = index;
                                lbl.BorderStyle = BorderStyle.FixedSingle;
                            }
                        }
                    }
                }
                else if (this.m_ColorSelectMode == ColorSelMode.Multi && mousebutton == 0)
                {
                    if (!this.m_SelectedIndicesList.Contains(index))
                    {
                        this.m_SelectedIndicesList.Add(index);
                        this.m_SelectedIndicesList.Sort();
                        lbl.BorderStyle = BorderStyle.FixedSingle;
                    }
                    else
                    {
                        this.m_SelectedIndicesList.RemoveAll(i => i == index);
                        lbl.BorderStyle = BorderStyle.None;
                    }
                }
                // force refresh
                lbl.Invalidate();
                if (this.ColorSelectionChanged != null)
                    this.ColorSelectionChanged(this, new PaletteClickEventArgs(e, lbl.Location, index, this.GetColor(index)));
            }
            if (this.ColorLabelMouseClick != null)
                this.ColorLabelMouseClick(this, new PaletteClickEventArgs(e, lbl.Location, index, this.GetColor(index)));
        }

        protected virtual void ColorLblMouseDoubleClick(Object sender, MouseEventArgs e)
        {
            Label lbl = sender as Label;
            if (this.ColorLabelMouseDoubleClick == null || lbl == null || !(lbl.Tag is Int32))
                return;
            Int32 index = (Int32)lbl.Tag;
            this.ColorLabelMouseDoubleClick(this, new PaletteClickEventArgs(e, lbl.Location, index, this.GetColor(index)));
        }

        protected virtual void BackgroundMouseDoubleClick(Object sender, MouseEventArgs e)
        {
            // disabled for now. Could be annoying when selecting a lot of indices,
            // if an accidental doubleclick on the background clears them all.
            /*/
            foreach (Int32 index in m_SelectedIndices)
                m_ColorLabels[index].BorderStyle = BorderStyle.None;
            this.m_SelectedIndices.Clear();
            //*/
        }

        /// <summary>
        /// Generate a color that should always be visible on the given background.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        protected static Color GetVisibleColorOn(Color color)
        {
            Single bri = color.GetBrightness();
            // See if color is close to grey
            if (color.GetSaturation() < .16)
                return bri < .5 ? Color.White : Color.Black;
            // Take inverted color.
            return Color.FromArgb((Int32) (0x00FFFFFFu ^ (UInt32) color.ToArgb()));
        }

        /// <summary>
        /// Disables the "feature" that double-clicking a label copies its text. Since said copy apparently happens
        /// on the internal text variable in the Label class, an override fixes this problem.
        /// </summary>
        protected class LabelNoCopyOnDblClick : Label
        {
            private String text;

            public override String Text
            {
                get { return this.text; }
                set
                {
                    if (value == null)
                        value = String.Empty;
                    if (this.text == value)
                        return;
                    this.text = value;
                    this.Refresh();
                    this.OnTextChanged(EventArgs.Empty);
                }
            }
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

    public delegate void PaletteClickEventHandler(Object sender, PaletteClickEventArgs e);

    public class PaletteClickEventArgs : MouseEventArgs
    {
        public Int32 Index { get; private set; }
        public Color Color { get; private set; }

        public PaletteClickEventArgs(MouseEventArgs e, Point sourceLocation, Int32 index, Color color)
            : base(e.Button, e.Clicks, sourceLocation.X + e.X, sourceLocation.Y + e.Y, e.Delta)
        {
            this.Index = index;
            this.Color = color;
        }
    }
}
