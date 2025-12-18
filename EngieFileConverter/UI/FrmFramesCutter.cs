using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EngieFileConverter.UI
{
    public partial class FrmFramesCutter : Form
    {
        private const Char TimesSymbol = '×';
        public Int32 FrameWidth { get; private set; }
        public Int32 FrameHeight { get; private set; }
        public Int32 Frames { get; private set; }

        public Int32[] CustomColors
        {
            get { return this.pzpFramePreview.CustomColors; }
            set { this.pzpFramePreview.CustomColors = value; }
        }

        private Bitmap m_Image;
        private Bitmap m_ImageHi;
        private Boolean m_Loading;

        public FrmFramesCutter(Bitmap image, Int32[] customColors)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            m_Image = image;
            m_ImageHi = new Bitmap(image);
            this.InitializeComponent();
            this.CustomColors = customColors;
            this.lblImageSizeVal.Text = String.Concat(this.m_Image.Width, TimesSymbol, this.m_Image.Height);
            this.numWidth.Maximum = this.m_Image.Width;
            this.numWidth.Value = this.m_Image.Width;
            this.numHeight.Maximum = this.m_Image.Height;
            this.numHeight.Value = this.m_Image.Height;
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

        private void UpdateUiInfo(Boolean updateAmount)
        {
            Int32 width = (Int32) this.numWidth.Value;
            Int32 height = (Int32) this.numHeight.Value;
            Int32 fullWidth = this.m_Image.Width;
            Int32 fullHeight = this.m_Image.Height;
            Int32 framesX = fullWidth / width;
            Int32 framesY = fullHeight / height;
            Int32 frames = framesX * framesY;
            if (updateAmount)
            {
                this.numFrames.Maximum = frames;
                this.numFrames.Value = frames;
                this.numCurFrame.Minimum = 0;
                this.numCurFrame.Maximum = numFrames.Value - 1;
            }
            Int32 frameNr = (Int32) this.numCurFrame.Value;
            Int32 rectY = frameNr / framesX;
            Int32 rectX = frameNr % framesX;
            Rectangle section = new Rectangle(rectX * width, rectY * height, width, height);
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(this.m_ImageHi, 0, 0, section, GraphicsUnit.Pixel);
            }
            Image oldImage = this.pzpFramePreview.Image;
            this.pzpFramePreview.Image = bmp;
            this.lblFramesOnImageVal.Text = String.Concat(framesX ,TimesSymbol, framesY);
            if (oldImage != null && !ReferenceEquals(oldImage, this.m_Image))
            {
                try { oldImage.Dispose(); }
                catch { /* ignore */ }
            }
        }

        private void numFrames_ValueChanged(object sender, EventArgs e)
        {
            this.numCurFrame.Maximum = numFrames.Value - 1;
        }
        
        private void btnCancel_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnImport_Click(Object sender, EventArgs e)
        {
            if (this.m_ImageHi != null)
                this.m_ImageHi.Dispose();
            this.FrameWidth = (Int32) this.numWidth.Value;
            this.FrameHeight = (Int32) this.numHeight.Value;
            this.Frames = (Int32)this.numFrames.Value;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
