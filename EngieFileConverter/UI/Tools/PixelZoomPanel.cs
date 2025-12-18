using System;
using System.ComponentModel;
using System.Drawing;
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
                this.picImage.Image = value;
                RefreshImage(false);
            }
        }

        public Int32 ZoomFactor
        {
            get { return (Int32)this.numZoom.Value; }
            set { this.numZoom.EnteredValue = value; }
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
            Int32 currentZoom = this.ZoomFactor;
            Int32 oldWidth = picImage.Width;
            Int32 oldHeight = picImage.Height;
            Int32 newWidth = loadOk ? bm.Width * currentZoom : 100;
            Int32 newHeight = loadOk ? bm.Height * currentZoom : 100;
            Int32 frameLeftVal = pnlImageScroll.DisplayRectangle.X;
            Int32 frameUpVal = pnlImageScroll.DisplayRectangle.Y;
            // Get previous zoom factor from current image size on the control.
            Int32 prevZoom = oldWidth * currentZoom / newWidth;
            Int32 visibleCenterXOld = Math.Min(oldWidth, pnlImageScroll.ClientRectangle.Width) / 2;
            Int32 visibleCenterYOld = Math.Min(oldHeight, pnlImageScroll.ClientRectangle.Height) / 2;

            picImage.Width = newWidth;
            picImage.Height = newHeight;
            picImage.PerformLayout();

            if (!adaptZoom || !loadOk || prevZoom <= 0 || prevZoom == currentZoom)
                return;
            // Centering zoom code: Image resized. Apply zoom centering.
            // ClientRectangle data is fetched again since it changes when scrollbars appear and disappear.
            Int32 visibleCenterXNew = Math.Min(newWidth, pnlImageScroll.ClientRectangle.Width) / 2;
            Int32 visibleCenterYNew = Math.Min(newHeight, pnlImageScroll.ClientRectangle.Height) / 2;
            Int32 viewCenterActualX = (-frameLeftVal + visibleCenterXOld) / prevZoom;
            Int32 viewCenterActualY = (-frameUpVal + visibleCenterYOld) / prevZoom;
            Int32 frameLeftValNew = visibleCenterXNew - (viewCenterActualX * currentZoom);
            Int32 frameUpValNew = visibleCenterYNew - (viewCenterActualY * currentZoom);
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
                this.numZoom.EnteredValue = this.numZoom.Constrain(this.numZoom.EnteredValue + (e.Delta / 120));
                HandledMouseEventArgs args = e as HandledMouseEventArgs;
                if (args != null)
                    args.Handled = true;
            }
        }
    }
}
