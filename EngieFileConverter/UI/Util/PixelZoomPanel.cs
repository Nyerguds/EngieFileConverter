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
        private Boolean m_updating;

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

        public Int32[] CustomColors { get; set; }

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

        public Int32 ZoomFactor
        {
            get { return (Int32)this.numZoom.Value; }
            set { this.numZoom.EnteredValue = value; }
        }

        [DefaultValue(typeof(Int32), "1")]
        public Int32 ZoomFactorMinimum
        {
            get { return (Int32)this.numZoom.Minimum; }
            set { this.numZoom.Minimum = value; }
        }
        
        public Boolean ImageVisible
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
                Double allocatedMem = Math.Sqrt(image.Width * image.Height);
                this.numZoom.Maximum = Math.Max(1, (Int32)(10000 / allocatedMem));
            }
        }

        private void PicImage_CopyPreview(Object sender, EventArgs e)
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
            Int32 maxWidth = maxSize.Width;
            Int32 maxHeight = maxSize.Height;
            Int32 minZoomFactor = Int32.MaxValue;
            // Build list of images to check
            Int32 nrToCheck = frames.Length;
            for (Int32 i = 0; i < nrToCheck; ++i)
            {
                Bitmap image = frames[i];
                if (image == null)
                    continue;
                Int32 zoomFactor = Math.Max(1, Math.Min(maxWidth / image.Width, maxHeight / image.Height));
                minZoomFactor = Math.Min(zoomFactor, minZoomFactor);
            }
            if (minZoomFactor == Int32.MaxValue)
                minZoomFactor = 1;
            this.ZoomFactor = minZoomFactor;
        }


        private void NumZoomValueEntered(Object sender, ValueEnteredEventArgs e)
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

        private void RefreshImage(Boolean adaptZoom)
        {
            try
            {
                this.SuspendLayout();
                Image bm = this.picImage.Image;
                Boolean loadOk = bm != null;
                this.picImage.Visible = loadOk;
                // Centering zoom code: save all information before image resize
                Double currentZoom = this.ZoomFactor;
                if (this.ZoomFactor == 0 || this.ZoomFactor == -1)
                    currentZoom = 1;
                else if (this.ZoomFactor < -1)
                    currentZoom = -1 / (Double) this.ZoomFactor;

                if (currentZoom < -1)
                    this.picImage.InterpolationMode = InterpolationMode.Default;
                else
                    this.picImage.InterpolationMode = InterpolationMode.NearestNeighbor;

                Int32 oldWidth = this.picImage.Width;
                Int32 oldHeight = this.picImage.Height;
                Int32 newWidth = loadOk ? (Int32) (bm.Width * currentZoom) : 100;
                Int32 newHeight = loadOk ? (Int32) (bm.Height * currentZoom) : 100;
                Int32 frameLeftVal = this.pnlImageScroll.DisplayRectangle.X;
                Int32 frameUpVal = this.pnlImageScroll.DisplayRectangle.Y;
                // Get previous zoom factor from current image size on the control.
                Double prevZoom = oldWidth * currentZoom / newWidth;
                Int32 visibleCenterXOld = Math.Min(oldWidth, this.pnlImageScroll.ClientRectangle.Width) / 2;
                Int32 visibleCenterYOld = Math.Min(oldHeight, this.pnlImageScroll.ClientRectangle.Height) / 2;

                this.picImage.Width = newWidth;
                this.picImage.Height = newHeight;
                this.picImage.PerformLayout();

                if (!adaptZoom || !loadOk || prevZoom <= 0 || ((Int32) prevZoom == (Int32) currentZoom && (Int32) (1 / prevZoom) == (Int32) (1 / currentZoom)))
                    return;
                // Centering zoom code: Image resized. Apply zoom centering.
                // ClientRectangle data is fetched again since it changes when scrollbars appear and disappear.
                Int32 visibleCenterXNew = Math.Min(newWidth, this.pnlImageScroll.ClientRectangle.Width) / 2;
                Int32 visibleCenterYNew = Math.Min(newHeight, this.pnlImageScroll.ClientRectangle.Height) / 2;
                Int32 viewCenterActualX = (Int32) ((-frameLeftVal + visibleCenterXOld) / prevZoom);
                Int32 viewCenterActualY = (Int32) ((-frameUpVal + visibleCenterYOld) / prevZoom);
                Int32 frameLeftValNew = (Int32) (visibleCenterXNew - (viewCenterActualX * currentZoom));
                Int32 frameUpValNew = (Int32) (visibleCenterYNew - (viewCenterActualY * currentZoom));
                this.pnlImageScroll.SetDisplayRectLocation(frameLeftValNew, frameUpValNew);
                //this.pnlImageScroll.PerformLayout();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void PicImageClick(Object sender, EventArgs e)
        {
            this.pnlImageScroll.Focus();
        }
        

        private void LblTransparentColorValKeyPress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.AdjustColor();
        }

        private void lblTransparentColorVal_MouseClick(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.AdjustColor();
        }

        private void AdjustColor()
        {
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.BackgroundFillColor;
            cdl.FullOpen = true;
            cdl.CustomColors = this.CustomColors;
            DialogResult res = cdl.ShowDialog(this);
            this.CustomColors = cdl.CustomColors;
            if (res != DialogResult.OK && res != DialogResult.Yes)
                return;
            Color col = cdl.Color;
            this.BackgroundFillColor = col;
            this.RefreshImage(false);
        }

        private void PnlImageScrollMouseScroll(Object sender, MouseEventArgs e)
        {
            Keys k = ModifierKeys;
            if ((k & Keys.Control) != 0)
            {
                Int32 diff = (e.Delta / 120);
                if (diff == 0 && e.Delta != 0)
                    diff = e.Delta > 0 ? 1 : -1;
                Decimal value = this.numZoom.Constrain(this.numZoom.Value + diff);
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
        private void numZoom_ValueUpDown(Object sender, UpDownEventArgs e)
        {
            EnhNumericUpDown zoom = sender as EnhNumericUpDown;
            if (zoom == null)
                return;
            Decimal val = zoom.EnteredValue;
            if (e.Direction == UpDownAction.Down && val < 1 && val > -2)
                zoom.EnteredValue = -2;
            else if (e.Direction == UpDownAction.Up && val <= 1 && val > -2)
                zoom.EnteredValue = val <= -1 ? 1 : 2;
        }

    }
}
