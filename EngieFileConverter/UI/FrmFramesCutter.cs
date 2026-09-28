using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;
using Nyerguds.Util.UI;

namespace EngieFileConverter.UI
{
    public partial class FrmFramesCutter : Form
    {

        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }
        public int Frames { get; private set; }
        public Color? TrimColor { get; private set; }
        public int? TrimIndex  { get; private set; }
        public int MatchBpp  { get; private set; }
        public Color[] MatchPalette { get; private set; }

        private PaletteDropDownInfo[] m_allPalettes;

        public int[] CustomColors
        {
            get { return this.pzpFramePreview.CustomColors; }
            set { this.pzpFramePreview.CustomColors = value; }
        }

        private Bitmap m_Image;
        private int m_OriginalBpp;
        private Color[] m_OriginalPalette;
        private bool m_Loading;
        private Bitmap m_previewImage;

        public FrmFramesCutter(Bitmap image, int[] customColors, PaletteDropDownInfo[] palettes)
        {
            this.m_Loading = true;
            if (image == null)
                throw new ArgumentNullException("image");
            this.m_OriginalBpp = Image.GetPixelFormatSize(image.PixelFormat);
            if (this.m_OriginalBpp > 8)
                this.m_Image = new Bitmap(image);
            else
            {
                int stride;
                int width = image.Width;
                int height =image.Height;
                this.m_OriginalPalette = image.Palette.Entries;
                bool is8Bit = this.m_OriginalBpp == 8;
                byte[] imageData = ImageUtils.GetImageData(image, out stride, is8Bit);
                if (!is8Bit)
                    imageData = ImageUtils.ConvertTo8Bit(imageData, width, height, 0, this.m_OriginalBpp, true, ref stride);
                this.m_Image = ImageUtils.BuildImage(imageData, width, height, stride, PixelFormat.Format8bppIndexed, this.m_OriginalPalette, Color.Empty);
            }
            this.m_allPalettes = palettes ?? new PaletteDropDownInfo[0];

            this.InitializeComponent();

            if (this.m_OriginalBpp < 8)
            {
                this.lblTrimColor.TrueBackColor = this.m_OriginalPalette[0];
                this.lblTrimColor.Tag = 0;
            }
            this.cmbPalType.DataSource = new string[] {"1-bit", "4-bit", "8-bit"};
            this.cmbPalType.SelectedIndex = 2;

            this.CustomColors = customColors;
            this.lblImageSizeVal.Text = String.Concat(image.Width, '×', image.Height);
            this.numWidth.Maximum = image.Width;
            this.numWidth.Value = image.Width;
            this.numHeight.Maximum = image.Height;
            this.numHeight.Value = image.Height;
            this.m_Loading = false;
            this.UpdateUiInfo(true);
        }

        private void FrameChanged(object sender, EventArgs e)
        {
            if (this.m_Loading)
                return;
            this.UpdateUiInfo(false);
        }

        private void DimensionsChanged(object sender, EventArgs e)
        {
            if (this.m_Loading)
                return;
            this.UpdateUiInfo(true);
        }

        private void UpdateUiInfo(bool updateAmount)
        {
            try
            {
                this.m_Loading = true;
                int width = (int) this.numWidth.Value;
                int height = (int) this.numHeight.Value;
                int fullWidth = this.m_Image.Width;
                int fullHeight = this.m_Image.Height;
                int framesX = fullWidth / width;
                int framesY = fullHeight / height;
                int frames = framesX * framesY;
                Image oldImage = this.m_previewImage;
                if (updateAmount)
                {
                    this.pzpFramePreview.Image = null;
                    Size max = this.pzpFramePreview.MaxImageSize;
                    int maxZoom = Math.Min(max.Width / width, max.Height / height);
                    this.numFrames.Minimum = 1;
                    this.numFrames.Maximum = frames;
                    this.numFrames.Value = frames;
                    this.numCurFrame.Minimum = 0;
                    this.numCurFrame.Maximum = this.numFrames.Value - 1;
                    this.lblFramesOnImageVal.Text = String.Concat(framesX * framesY," (", framesX, '×', framesY, ")");
                    this.pzpFramePreview.ZoomFactor = Math.Max(1, maxZoom);
                }
                int frameNr = (int) this.numCurFrame.Value;
                int? trimIndex = null;
                Color? trimColor = null;
                int matchBpp = 0;
                Color[] matchPalette = null;
                if (this.chkTrimColor.Checked)
                {
                    if (this.m_OriginalBpp > 8)
                        trimColor = this.lblTrimColor.TrueBackColor;
                    else
                        trimIndex = this.lblTrimColor.Tag as int?;
                }
                PaletteDropDownInfo pdd = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                if (this.chkMatchPalette.Checked && pdd != null)
                {
                    matchBpp = pdd.BitsPerPixel;
                    matchPalette = pdd.Colors;
                }
                // Call this specifically with a 1-frame range so it generates only one image.
                Bitmap[] result = ImageUtils.ImageToFrames(this.m_Image, width, height, trimColor, trimIndex, matchBpp, matchPalette, frameNr, frameNr);
                Bitmap bmp = result.Length > 0 ? result[0] : null;
                this.m_previewImage = bmp;
                this.pzpFramePreview.Image = bmp;
                int frWidth = bmp != null ? bmp.Width : 0;
                int frHeight = bmp != null ? bmp.Height : 0;
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

        private void NumFramesValueChanged(object sender, EventArgs e)
        {
            this.numCurFrame.Maximum = this.numFrames.Value - 1;
        }

        private void ChkTrimColor_CheckedChanged(object sender, EventArgs e)
        {
            bool trimCol = this.chkTrimColor.Checked;
            this.lblTrimColor.Enabled = trimCol;
            this.lblTrimColorVal.Enabled = trimCol;
            this.UpdateColorInfo();
            this.UpdateUiInfo(false);
        }

        private void lblTrimColor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.PickTrimColor();
        }

        private void lblTrimColor_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.PickTrimColor();
        }

        private void PickTrimColor()
        {
            if (this.m_OriginalBpp > 8)
            {
                ColorDialog cdl = new ColorDialog();
                cdl.Color = this.lblTrimColor.TrueBackColor;
                cdl.FullOpen = true;
                cdl.CustomColors = this.CustomColors;
                DialogResult res = cdl.ShowDialog(this);
                this.CustomColors = cdl.CustomColors;
                if (res != DialogResult.OK && res != DialogResult.Yes)
                    return;
                this.lblTrimColor.Tag = null;
                this.lblTrimColor.TrueBackColor = cdl.Color;
            }
            else
            {
                FrmPalette palSelect = new FrmPalette(-1, this.m_OriginalPalette.ToArray(), false, ColorSelMode.Single);
                palSelect.SelectedIndices = new int[] { this.lblTrimColor.Tag as int? ?? 0 };
                if (palSelect.ShowDialog() != DialogResult.OK)
                    return;
                int selectedColor = palSelect.SelectedIndices.Length == 0 ? 0 : palSelect.SelectedIndices[0];
                if (selectedColor >= this.m_OriginalPalette.Length)
                    return;
                this.lblTrimColor.Tag = selectedColor;
                this.lblTrimColor.TrueBackColor = this.m_OriginalPalette[selectedColor];
            }
            this.UpdateColorInfo();
            this.UpdateUiInfo(false);
        }

        private void UpdateColorInfo()
        {
            if (!this.lblTrimColorVal.Enabled)
            {
                this.lblTrimColorVal.Text = String.Empty;
                return;
            }
            if (this.m_OriginalBpp <= 8)
            {
                int index = 0;
                if (this.lblTrimColor.Tag == null)
                {
                    this.lblTrimColor.Tag = index;
                }
                else if (this.lblTrimColor.Tag is int)
                {
                    index = (int) this.lblTrimColor.Tag;
                }
                if (this.m_OriginalPalette == null)
                    return;
                index = Math.Min(index, this.m_OriginalPalette.Length);
                this.lblTrimColorVal.Text = "Index " + index;
            }
            else
            {
                Color col = this.lblTrimColor.TrueBackColor;
                this.lblTrimColorVal.Text = ColorUtils.HexStringFromColor(col, false);
            }
        }

        private void ChkMatchPaletteCheckedChanged(object sender, EventArgs e)
        {
            bool matchPal = this.chkMatchPalette.Checked;
            this.cmbPalType.Enabled = matchPal;
            this.cmbPalettes.Enabled = matchPal;
            this.UpdateUiInfo(false);
        }

        private void CmbPalTypeSelectedIndexChanged(object sender, EventArgs e)
        {
            int bpp = 0;
            string selText = this.cmbPalType.Text;
            if (!String.IsNullOrEmpty(selText))
                bpp = selText[0] - '0';
            PaletteDropDownInfo[] filteredPalettes = this.m_allPalettes.Where(p => p.BitsPerPixel == bpp).ToArray();
            this.cmbPalettes.DataSource = filteredPalettes;
            this.cmbPalettes.SelectedIndex = filteredPalettes.Length > 0 ? 0 : -1;
        }

        private void cmbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.chkMatchPalette.Checked || this.m_Loading)
                return;
            this.UpdateUiInfo(false);
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnConvertClick(object sender, EventArgs e)
        {
            this.FrameWidth = (int)this.numWidth.Value;
            this.FrameHeight = (int)this.numHeight.Value;
            this.Frames = (int)this.numFrames.Value;
            this.TrimColor = this.chkTrimColor.Checked ? (Color?) this.lblTrimColor.TrueBackColor : null;
            this.TrimIndex = this.chkTrimColor.Checked && this.lblTrimColor.Tag is int ? (int?) this.lblTrimColor.Tag : null;
            PaletteDropDownInfo pdd = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
            this.MatchBpp = this.chkMatchPalette.Checked && pdd != null ? pdd.BitsPerPixel : 0;
            this.MatchPalette = this.chkMatchPalette.Checked && pdd != null ? pdd.Colors : null;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.m_previewImage != null)
                {
                    try {
                        this.m_previewImage.Dispose(); }
                    catch { /* ignore */ }
                }
                if (this.components != null)
                    this.components.Dispose();
                if (this.m_Image != null)
                {
                    try {
                        this.m_Image.Dispose(); }
                    catch { /* ignore */ }
                }
            }
            base.Dispose(disposing);
        }

        private void lblCurFrame_Click(object sender, EventArgs e)
        {

        }

    }
}
