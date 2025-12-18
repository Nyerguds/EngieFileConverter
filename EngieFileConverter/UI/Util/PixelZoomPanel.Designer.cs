namespace Nyerguds.Util.UI
{
    partial class PixelZoomPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTransparentColor = new System.Windows.Forms.Label();
            this.lblZoom = new System.Windows.Forms.Label();
            this.lblTransparentColorVal = new Nyerguds.Util.UI.ImageButtonCheckBox();
            this.numZoom = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.pnlImageScroll = new Nyerguds.Util.UI.SelectablePanel();
            this.picImage = new Nyerguds.Util.UI.PixelBox();
            ((System.ComponentModel.ISupportInitialize)(this.numZoom)).BeginInit();
            this.pnlImageScroll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTransparentColor
            // 
            this.lblTransparentColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransparentColor.Location = new System.Drawing.Point(77, 207);
            this.lblTransparentColor.Name = "lblTransparentColor";
            this.lblTransparentColor.Size = new System.Drawing.Size(75, 20);
            this.lblTransparentColor.TabIndex = 1;
            this.lblTransparentColor.Text = "Background:";
            this.lblTransparentColor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblZoom
            // 
            this.lblZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblZoom.Location = new System.Drawing.Point(173, 207);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(72, 20);
            this.lblZoom.TabIndex = 3;
            this.lblZoom.Text = "Zoom factor:";
            this.lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTransparentColorVal
            // 
            this.lblTransparentColorVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransparentColorVal.BackColor = System.Drawing.Color.Fuchsia;
            this.lblTransparentColorVal.Checked = true;
            this.lblTransparentColorVal.Location = new System.Drawing.Point(158, 207);
            this.lblTransparentColorVal.Name = "lblTransparentColorVal";
            this.lblTransparentColorVal.Size = new System.Drawing.Size(20, 20);
            this.lblTransparentColorVal.TabIndex = 2;
            this.lblTransparentColorVal.Toggle = false;
            this.lblTransparentColorVal.TrueBackColor = System.Drawing.Color.Fuchsia;
            this.lblTransparentColorVal.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LblTransparentColorValKeyPress);
            this.lblTransparentColorVal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lblTransparentColorVal_MouseClick);
            // 
            // numZoom
            // 
            this.numZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numZoom.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.Location = new System.Drawing.Point(251, 207);
            this.numZoom.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.Name = "numZoom";
            this.numZoom.SelectedText = "";
            this.numZoom.SelectionLength = 0;
            this.numZoom.SelectionStart = 0;
            this.numZoom.Size = new System.Drawing.Size(68, 20);
            this.numZoom.TabIndex = 4;
            this.numZoom.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.ValueUpDown += new System.EventHandler<Nyerguds.Util.UI.UpDownEventArgs>(this.numZoom_ValueUpDown);
            this.numZoom.ValueEntered += new System.EventHandler<Nyerguds.Util.UI.ValueEnteredEventArgs>(this.NumZoomValueEntered);
            // 
            // pnlImageScroll
            // 
            this.pnlImageScroll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlImageScroll.AutoScroll = true;
            this.pnlImageScroll.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlImageScroll.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlImageScroll.Controls.Add(this.picImage);
            this.pnlImageScroll.Location = new System.Drawing.Point(0, 0);
            this.pnlImageScroll.Margin = new System.Windows.Forms.Padding(0);
            this.pnlImageScroll.Name = "pnlImageScroll";
            this.pnlImageScroll.Size = new System.Drawing.Size(322, 202);
            this.pnlImageScroll.TabIndex = 0;
            this.pnlImageScroll.TabStop = true;
            this.pnlImageScroll.MouseScroll += new System.Windows.Forms.MouseEventHandler(this.PnlImageScrollMouseScroll);
            // 
            // picImage
            // 
            this.picImage.BackColor = System.Drawing.Color.Fuchsia;
            this.picImage.Location = new System.Drawing.Point(0, 0);
            this.picImage.Margin = new System.Windows.Forms.Padding(0);
            this.picImage.Name = "picImage";
            this.picImage.Size = new System.Drawing.Size(100, 100);
            this.picImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picImage.TabIndex = 0;
            this.picImage.TabStop = false;
            this.picImage.Visible = false;
            this.picImage.Click += new System.EventHandler(this.PicImageClick);
            // 
            // PixelZoomPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblTransparentColorVal);
            this.Controls.Add(this.lblTransparentColor);
            this.Controls.Add(this.lblZoom);
            this.Controls.Add(this.numZoom);
            this.Controls.Add(this.pnlImageScroll);
            this.Name = "PixelZoomPanel";
            this.Size = new System.Drawing.Size(322, 230);
            ((System.ComponentModel.ISupportInitialize)(this.numZoom)).EndInit();
            this.pnlImageScroll.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Nyerguds.Util.UI.ImageButtonCheckBox lblTransparentColorVal;
        private System.Windows.Forms.Label lblTransparentColor;
        private System.Windows.Forms.Label lblZoom;
        private Nyerguds.Util.UI.EnhNumericUpDown numZoom;
        private Nyerguds.Util.UI.SelectablePanel pnlImageScroll;
        private Nyerguds.Util.UI.PixelBox picImage;
    }
}
