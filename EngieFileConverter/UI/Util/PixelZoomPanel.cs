using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;

namespace Nyerguds.Util.UI
{
    public partial class PixelZoomPanel : UserControl
    {
        private bool m_updating;

        [RefreshProperties(RefreshProperties.Repaint)]
        [DefaultValue(typeof(Color), "Fuchsia")]
        public Color BackgroundFillColor
        {
            get { return this.lblTransparentColorVal.TrueBackColor; }
            set
            {
                this.lblTransparentColorVal.TrueBackColor = value;
                this.picImage.BackColor = value;
                this.OnBackgroundFillColorChanged(new EventArgs());
            }
        }

        [Description("Occurs when the user changes the background fill color"), Category("Action")]
        public event EventHandler BackgroundFillColorChanged;

        protected virtual void OnBackgroundFillColorChanged(EventArgs e)
        {
            EventHandler handler = this.BackgroundFillColorChanged;
            if (handler != null)
                handler(this, e);
        }

        public int[] CustomColors { get; set; }

        public Image Image
        {
            get { return this.picImage.Image; }
            set
            {
                this.SetMaxZoom(value);
                this.picImage.Image = value;
                this.RefreshImage(false);
            }
        }

        public int ZoomFactor
        {
            get { return (int)this.numZoom.Value; }
            set { this.numZoom.EnteredValue = value; }
        }

        [DefaultValue(typeof(int), "1")]
        public int ZoomFactorMinimum
        {
            get { return (int)this.numZoom.Minimum; }
            set { this.numZoom.Minimum = value; }
        }

        public bool ImageVisible
        {
            get { return this.picImage.Visible; }
            set { this.picImage.Visible = value; }
        }

        public Size MaxImageSize
        {
            get { return this.pnlImageScroll.ClientSize; }
        }

        public PixelZoomPanel()
        {
            this.InitializeComponent();
            ContextMenu cmCopyPreview = new ContextMenu();
            MenuItem mniCopy = new MenuItem("Copy");
            mniCopy.Click += this.PicImage_CopyPreview;
            cmCopyPreview.MenuItems.Add(mniCopy);
            this.picImage.ContextMenu = cmCopyPreview;
        }

        private void SetMaxZoom(Image image)
        {
            if (image == null)
            {
                this.numZoom.Maximum = 20;
            }
            else
            {
                // Get average "square side". This should give an approximation
                // of how many times we are allowed to zoom before we get problems.
                double allocatedMem = Math.Sqrt(image.Width * image.Height);
                this.numZoom.Maximum = Math.Max(1, (int)(10000 / allocatedMem));
            }
        }

        private void PicImage_CopyPreview(object sender, EventArgs e)
        {
            this.CopyToClipboard();
        }

        public void CopyToClipboard()
        {
            Image image = this.picImage.Image;
            if (image == null)
                return;
            using (Bitmap bm = new Bitmap(image))
            using (Bitmap bmnt = ImageUtils.PaintOn32bpp(image, this.BackgroundFillColor))
                ClipboardImage.SetClipboardImage(bm, bmnt, null);
        }

        public void AutoSetZoom(Bitmap[] frames)
        {
            if (frames == null)
                return;
            // Set image invisible to remove scrollbars.
            this.ImageVisible = false;
            Size maxSize = this.MaxImageSize;
            int maxWidth = maxSize.Width;
            int maxHeight = maxSize.Height;
            int minZoomFactor = Int32.MaxValue;
            // Build list of images to check
            int nrToCheck = frames.Length;
            for (int i = 0; i < nrToCheck; ++i)
            {
                Bitmap image = frames[i];
                if (image == null)
                    continue;
                int zoomFactor = Math.Max(1, Math.Min(maxWidth / image.Width, maxHeight / image.Height));
                minZoomFactor = Math.Min(zoomFactor, minZoomFactor);
            }
            if (minZoomFactor == Int32.MaxValue)
                minZoomFactor = 1;
            this.ZoomFactor = minZoomFactor;
        }


        private void NumZoomValueEntered(object sender, ValueEnteredEventArgs e)
        {
            if (this.m_updating)
                return;
            try
            {
                this.m_updating = true;
                this.RefreshImage(true);
            }
            finally
            {
                this.m_updating = false;
            }
        }

        public void RefreshImage()
        {
            if (this.m_updating)
                return;
            try
            {
                this.m_updating = true;
                this.RefreshImage(false);
            }
            finally
            {
                this.m_updating = false;
            }
        }

        private void RefreshImage(bool adaptZoom)
        {
            try
            {
                this.SuspendLayout();
                Image bm = this.picImage.Image;
                bool loadOk = bm != null;
                this.picImage.Visible = loadOk;
                // Centering zoom code: save all information before image resize
                double currentZoom = this.ZoomFactor;
                if (this.ZoomFactor == 0 || this.ZoomFactor == -1)
                    currentZoom = 1;
                else if (this.ZoomFactor < -1)
                    currentZoom = -1 / (double) this.ZoomFactor;

                if (currentZoom < -1)
                    this.picImage.InterpolationMode = InterpolationMode.Default;
                else
                    this.picImage.InterpolationMode = InterpolationMode.NearestNeighbor;

                int oldWidth = this.picImage.Width;
                int oldHeight = this.picImage.Height;
                int newWidth = loadOk ? (int) (bm.Width * currentZoom) : 100;
                int newHeight = loadOk ? (int) (bm.Height * currentZoom) : 100;
                int frameLeftVal = this.pnlImageScroll.DisplayRectangle.X;
                int frameUpVal = this.pnlImageScroll.DisplayRectangle.Y;
                // Get previous zoom factor from current image size on the control.
                double prevZoom = oldWidth * currentZoom / newWidth;
                int visibleCenterXOld = Math.Min(oldWidth, this.pnlImageScroll.ClientRectangle.Width) / 2;
                int visibleCenterYOld = Math.Min(oldHeight, this.pnlImageScroll.ClientRectangle.Height) / 2;

                this.picImage.Width = newWidth;
                this.picImage.Height = newHeight;
                this.picImage.PerformLayout();

                if (!adaptZoom || !loadOk || prevZoom <= 0 || ((int) prevZoom == (int) currentZoom && (int) (1 / prevZoom) == (int) (1 / currentZoom)))
                    return;
                // Centering zoom code: Image resized. Apply zoom centering.
                // ClientRectangle data is fetched again since it changes when scrollbars appear and disappear.
                int visibleCenterXNew = Math.Min(newWidth, this.pnlImageScroll.ClientRectangle.Width) / 2;
                int visibleCenterYNew = Math.Min(newHeight, this.pnlImageScroll.ClientRectangle.Height) / 2;
                int viewCenterActualX = (int) ((-frameLeftVal + visibleCenterXOld) / prevZoom);
                int viewCenterActualY = (int) ((-frameUpVal + visibleCenterYOld) / prevZoom);
                int frameLeftValNew = (int) (visibleCenterXNew - (viewCenterActualX * currentZoom));
                int frameUpValNew = (int) (visibleCenterYNew - (viewCenterActualY * currentZoom));
                this.pnlImageScroll.SetDisplayRectLocation(frameLeftValNew, frameUpValNew);
                //this.pnlImageScroll.PerformLayout();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void PicImageClick(object sender, EventArgs e)
        {
            this.pnlImageScroll.Focus();
        }


        private void LblTransparentColorValKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.AdjustColor();
        }

        private void lblTransparentColorVal_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.AdjustColor();
        }

        private void AdjustColor()
        {
            Color col;
            using (ColorDialog cdl = new ColorDialog())
            {
                cdl.Color = this.BackgroundFillColor;
                cdl.FullOpen = true;
                cdl.CustomColors = this.CustomColors;
                DialogResult res = cdl.ShowDialog(this);
                this.CustomColors = cdl.CustomColors;
                if (res != DialogResult.OK && res != DialogResult.Yes)
                    return;
                col = cdl.Color;
            }
            this.BackgroundFillColor = col;
            this.RefreshImage(false);
        }

        private void PnlImageScrollMouseScroll(object sender, MouseEventArgs e)
        {
            Keys k = ModifierKeys;
            if ((k & Keys.Control) != 0)
            {
                int diff = (e.Delta / 120);
                if (diff == 0 && e.Delta != 0)
                    diff = e.Delta > 0 ? 1 : -1;
                decimal value = this.numZoom.Constrain(this.numZoom.Value + diff);
                if (diff != 0)
                {
                    this.numZoom.EnteredValue = this.numZoom.Constrain(value);
                    numZoom_ValueUpDown(this.numZoom, new UpDownEventArgs(diff > 0 ? UpDownAction.Up : UpDownAction.Down, diff, true));
                }
                HandledMouseEventArgs args = e as HandledMouseEventArgs;
                if (args != null)
                    args.Handled = true;

            }
        }

        /// <summary>
        /// Ensures that zoom level 0 and -1 are skipped when using mouse scroll or arrows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numZoom_ValueUpDown(object sender, UpDownEventArgs e)
        {
            EnhNumericUpDown zoom = sender as EnhNumericUpDown;
            if (zoom == null)
                return;
            decimal val = zoom.EnteredValue;
            if (e.Direction == UpDownAction.Down && val < 1 && val > -2)
                zoom.EnteredValue = -2;
            else if (e.Direction == UpDownAction.Up && val <= 1 && val > -2)
                zoom.EnteredValue = val <= -1 ? 1 : 2;
        }
    }
}
