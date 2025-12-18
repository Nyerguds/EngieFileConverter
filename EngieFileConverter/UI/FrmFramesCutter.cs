using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;
using Nyerguds.Util.UI;
using Nyerguds.Util;

namespace EngieFileConverter.UI
{
    public partial class FrmFramesCutter : Form
    {
        
        public Int32 FrameWidth { get; private set; }
        public Int32 FrameHeight { get; private set; }
        public Int32 Frames { get; private set; }
        public Color? TrimColor { get; private set; }
        public Int32? TrimIndex  { get; private set; }
        public Int32 MatchBpp  { get; private set; }
        public Color[] MatchPalette { get; private set; }

        private PaletteDropDownInfo[] m_allPalettes;
        private const String AllowedCharsMask = "01243456789-, ";

        public Int32[] CustomColors
        {
            get { return this.pzpFramePreview.CustomColors; }
            set { this.pzpFramePreview.CustomColors = value; }
        }

        private Bitmap m_Image;
        private Int32  m_OriginalBpp;
        private Color[] m_OriginalPalette;
        private Boolean m_Loading;

        public FrmFramesCutter(Bitmap image, Int32[] customColors, PaletteDropDownInfo[] palettes)
        {
            m_Loading = true;
            if (image == null)
                throw new ArgumentNullException("image");
            this.m_OriginalBpp = Image.GetPixelFormatSize(image.PixelFormat);
            if (m_OriginalBpp > 8)
                this.m_Image = new Bitmap(image);
            else
            {
                Int32 stride;
                Int32 width = image.Width;
                Int32 height =image.Height;
                this.m_OriginalPalette = image.Palette.Entries;
                Boolean is8Bit = m_OriginalBpp == 8;
                Byte[] imageData = ImageUtils.GetImageData(image, out stride, is8Bit);
                if (!is8Bit)
                    imageData = ImageUtils.ConvertTo8Bit(imageData, width, height, 0, m_OriginalBpp, true, ref stride);
                this.m_Image = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, m_OriginalPalette, Color.Empty);
            }
            this.m_allPalettes = palettes ?? new PaletteDropDownInfo[0];

            this.InitializeComponent();

            if (m_OriginalBpp < 8)
            {
                lblTrimColor.TrueBackColor = m_OriginalPalette[0];
                lblTrimColor.Tag = 0;
            }
            this.cmbPalType.DataSource = new String[] {"1-bit", "4-bit", "8-bit"};
            this.cmbPalType.SelectedIndex = 2;

            this.CustomColors = customColors;
            this.lblImageSizeVal.Text = String.Concat(image.Width, '×', image.Height);
            this.numWidth.Maximum = image.Width;
            this.numWidth.Value = image.Width;
            this.numHeight.Maximum = image.Height;
            this.numHeight.Value = image.Height;
            m_Loading = false;
            this.UpdateUiInfo(true);
        }

        private void FrameChanged(Object sender, EventArgs e)
        {
            if (this.m_Loading)
                return;
            this.UpdateUiInfo(false);
        }

        private void DimensionsChanged(Object sender, EventArgs e)
        {
            if (this.m_Loading)
                return;
            this.UpdateUiInfo(true);
        }

        private void UpdateUiInfo(Boolean updateAmount)
        {
            try
            {
                this.m_Loading = true;
                Int32 width = (Int32) this.numWidth.Value;
                Int32 height = (Int32) this.numHeight.Value;
                Int32 fullWidth = this.m_Image.Width;
                Int32 fullHeight = this.m_Image.Height;
                Int32 framesX = fullWidth / width;
                Int32 framesY = fullHeight / height;
                Int32 frames = framesX * framesY;
                Image oldImage = this.pzpFramePreview.Image;
                if (updateAmount)
                {
                    this.pzpFramePreview.Image = null;
                    Size max = this.pzpFramePreview.MaxImageSize;
                    Int32 maxZoom = Math.Min(max.Width / width, max.Height / height);
                    this.numFrames.Minimum = 1;
                    this.numFrames.Maximum = frames;
                    this.numFrames.Value = frames;
                    this.numCurFrame.Minimum = 0;
                    this.numCurFrame.Maximum = this.numFrames.Value - 1;
                    this.lblFramesOnImageVal.Text = String.Concat(framesX * framesY," (", framesX, '×', framesY, ")");
                    this.pzpFramePreview.ZoomFactor = Math.Max(1, maxZoom);
                }
                Int32 frameNr = (Int32) this.numCurFrame.Value;
                Int32? trimIndex = null;
                Color? trimColor = null;
                Int32 matchBpp = 0;
                Color[] matchPalette = null;
                if (this.chkTrimColor.Checked)
                {
                    if (m_OriginalBpp > 8)
                        trimColor = lblTrimColor.TrueBackColor;
                    else
                        trimIndex = lblTrimColor.Tag as Int32?;
                }
                PaletteDropDownInfo pdd = cmbPalettes.SelectedItem as PaletteDropDownInfo;
                if (chkMatchPalette.Checked && pdd != null)
                {
                    matchBpp = pdd.BitsPerPixel;
                    matchPalette = pdd.Colors;
                }
                Bitmap[] result = ImageUtils.ImageToFrames(this.m_Image, width, height, trimColor, trimIndex, matchBpp, matchPalette, frameNr, frameNr);
                Bitmap bmp = result.Length > 0 ? result[0] : null;
                this.pzpFramePreview.Image = bmp;
                Int32 frWidth = bmp != null ? bmp.Width : 0;
                Int32 frHeight = bmp != null ? bmp.Height : 0;
                this.lblFrameSizeVal.Text = String.Concat(frWidth, '×', frHeight);
                if (oldImage != null)
                {
                    try { oldImage.Dispose(); }
                    catch { /* ignore */ }
                }
            }
            finally
            {
                this.m_Loading = false;
            }
        }

        private void NumFramesValueChanged(Object sender, EventArgs e)
        {
            this.numCurFrame.Maximum = this.numFrames.Value - 1;
        }

        private void ChkTrimColor_CheckedChanged(Object sender, EventArgs e)
        {
            Boolean trimCol = this.chkTrimColor.Checked;
            lblTrimColor.Enabled = trimCol;
            lblTrimColorVal.Enabled = trimCol;
            UpdateUiInfo(false);
        }

        private void lblTrimColor_KeyPress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.PickTrimColor();
        }

        private void lblTrimColor_MouseClick(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.PickTrimColor();
        }

        private void PickTrimColor()
        {
            if (m_OriginalBpp > 8)
            {
                ColorDialog cdl = new ColorDialog();
                cdl.Color = this.lblTrimColor.TrueBackColor;
                cdl.FullOpen = true;
                cdl.CustomColors = this.CustomColors;
                DialogResult res = cdl.ShowDialog(this);
                this.CustomColors = cdl.CustomColors;
                if (res != DialogResult.OK && res != DialogResult.Yes)
                    return;
                Color col = cdl.Color;
                this.lblTrimColor.TrueBackColor = cdl.Color;
                this.lblTrimColorVal.Text = ColorUtils.HexStringFromColor(col, false);
            }
            else
            {
                FrmPalette palSelect = new FrmPalette(m_OriginalPalette.ToArray(), false, ColorSelMode.Single);
                palSelect.SelectedIndices = new Int32[] { lblTrimColor.Tag as Int32? ?? 0 };
                if (palSelect.ShowDialog() != DialogResult.OK)
                    return;
                Int32 selectedColor = palSelect.SelectedIndices.Length == 0 ? 0 : palSelect.SelectedIndices[0];
                lblTrimColor.Tag = selectedColor;
                lblTrimColor.TrueBackColor = m_OriginalPalette[selectedColor];
                this.lblTrimColorVal.Text = "Index " + selectedColor;
            }
            this.UpdateUiInfo(false);
        }

        private void ChkMatchPaletteCheckedChanged(Object sender, EventArgs e)
        {
            Boolean matchPal = chkMatchPalette.Checked;
            cmbPalType.Enabled = matchPal;
            cmbPalettes.Enabled = matchPal;
            UpdateUiInfo(false);
        }

        private void CmbPalTypeSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 bpp = 0;
            String selText = this.cmbPalType.Text;
            if (!String.IsNullOrEmpty(selText))
                bpp = selText[0] - '0';
            PaletteDropDownInfo[] filteredPalettes = this.m_allPalettes.Where(p => p.BitsPerPixel == bpp).ToArray();
            this.cmbPalettes.DataSource = filteredPalettes;
            this.cmbPalettes.SelectedIndex = filteredPalettes.Length > 0 ? 0 : -1;
        }

        private void cmbPalettes_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (!chkMatchPalette.Checked || m_Loading)
                return;
            UpdateUiInfo(false);
        }
        
        private void BtnCancelClick(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnImportClick(Object sender, EventArgs e)
        {
            this.FrameWidth = (Int32)this.numWidth.Value;
            this.FrameHeight = (Int32)this.numHeight.Value;
            this.Frames = (Int32)this.numFrames.Value;
            this.TrimColor = this.chkTrimColor.Checked ? (Color?)lblTrimColor.TrueBackColor : null;
            this.TrimIndex = this.chkTrimColor.Checked && lblTrimColor.Tag is Int32 ? (Int32?)lblTrimColor.Tag : null;
            PaletteDropDownInfo pdd = cmbPalettes.SelectedItem as PaletteDropDownInfo;
            this.MatchBpp = chkMatchPalette.Checked && pdd != null ? pdd.BitsPerPixel : 0;
            this.MatchPalette = chkMatchPalette.Checked && pdd != null ? pdd.Colors : null;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                if (this.components != null)
                    this.components.Dispose();
                if (this.m_Image != null)
                    this.m_Image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
