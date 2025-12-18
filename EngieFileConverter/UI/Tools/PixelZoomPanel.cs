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
            EventHandler handler = BackgroundFillColorChanged;
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
                RefreshImage(false);
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
            InitializeComponent();
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

        private void NumZoomValueChanged(Object sender, EventArgs e)
        {
            this.RefreshImage(true);
        }

        public void RefreshImage()
        {
            RefreshImage(false);
        }

        private void RefreshImage(Boolean adaptZoom)
        {
            Image bm = picImage.Image;
            Boolean loadOk = bm != null;
            picImage.Visible = loadOk;
            // Centering zoom code: save all information before image resize
            Double currentZoom = this.ZoomFactor;
            if (this.ZoomFactor == 0 || this.ZoomFactor == -1)
                currentZoom = 1;
            else if (this.ZoomFactor < -1)
                currentZoom = -1 / (Double)this.ZoomFactor;

            if (currentZoom < -1)
                picImage.InterpolationMode = InterpolationMode.Default;
            else
                picImage.InterpolationMode = InterpolationMode.NearestNeighbor;

            Int32 oldWidth = picImage.Width;
            Int32 oldHeight = picImage.Height;
            Int32 newWidth = loadOk ? (Int32)(bm.Width * currentZoom) : 100;
            Int32 newHeight = loadOk ? (Int32)(bm.Height * currentZoom) : 100;
            Int32 frameLeftVal = pnlImageScroll.DisplayRectangle.X;
            Int32 frameUpVal = pnlImageScroll.DisplayRectangle.Y;
            // Get previous zoom factor from current image size on the control.
            Double prevZoom = oldWidth * currentZoom / newWidth;
            Int32 visibleCenterXOld = Math.Min(oldWidth, pnlImageScroll.ClientRectangle.Width) / 2;
            Int32 visibleCenterYOld = Math.Min(oldHeight, pnlImageScroll.ClientRectangle.Height) / 2;

            picImage.Width = newWidth;
            picImage.Height = newHeight;
            picImage.PerformLayout();

            if (!adaptZoom || !loadOk || prevZoom <= 0 || ((Int32)prevZoom == (Int32)currentZoom && (Int32)(1 / prevZoom) == (Int32)(1 / currentZoom)))
                return;
            // Centering zoom code: Image resized. Apply zoom centering.
            // ClientRectangle data is fetched again since it changes when scrollbars appear and disappear.
            Int32 visibleCenterXNew = Math.Min(newWidth, pnlImageScroll.ClientRectangle.Width) / 2;
            Int32 visibleCenterYNew = Math.Min(newHeight, pnlImageScroll.ClientRectangle.Height) / 2;
            Int32 viewCenterActualX = (Int32)((-frameLeftVal + visibleCenterXOld) / prevZoom);
            Int32 viewCenterActualY = (Int32)((-frameUpVal + visibleCenterYOld) / prevZoom);
            Int32 frameLeftValNew = (Int32)(visibleCenterXNew - (viewCenterActualX * currentZoom));
            Int32 frameUpValNew = (Int32)(visibleCenterYNew - (viewCenterActualY * currentZoom));
            pnlImageScroll.SetDisplayRectLocation(frameLeftValNew, frameUpValNew);
            pnlImageScroll.PerformLayout();
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
            BackgroundFillColor = col;
            this.RefreshImage(false);
        }

        private void PnlImageScrollMouseScroll(Object sender, MouseEventArgs e)
        {
            Keys k = ModifierKeys;
            if ((k & Keys.Control) != 0)
            {
                Int32 diff = (e.Delta / 120);
                Decimal value = this.numZoom.Constrain(this.numZoom.Value + diff);
                if (diff > 0)
                {
                    if (this.numZoom.ZoomMode && value <= 1 && value > -2)
                        value = 1;
                }
                else if (diff < 0)
                {
                    if (this.numZoom.ZoomMode && value < 1 && value >= -2)
                        value = -2;
                }
                this.numZoom.EnteredValue = this.numZoom.Constrain(value);
                HandledMouseEventArgs args = e as HandledMouseEventArgs;
                if (args != null)
                    args.Handled = true;
            }
        }
    }
}
