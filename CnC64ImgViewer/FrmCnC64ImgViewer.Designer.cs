namespace CnC64ImgViewer
{
    partial class FrmCnC64ImgViewer
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblFilename = new System.Windows.Forms.Label();
            this.lblWidth = new System.Windows.Forms.Label();
            this.lblHeight = new System.Windows.Forms.Label();
            this.lblBytesPerCol = new System.Windows.Forms.Label();
            this.lblColorformat = new System.Windows.Forms.Label();
            this.lblImageData = new System.Windows.Forms.Label();
            this.lblPaletteData = new System.Windows.Forms.Label();
            this.lblValFilename = new System.Windows.Forms.Label();
            this.lblColorsInPal = new System.Windows.Forms.Label();
            this.lblValWidth = new System.Windows.Forms.Label();
            this.lblValHeight = new System.Windows.Forms.Label();
            this.lblValBytesPerCol = new System.Windows.Forms.Label();
            this.lblValColorFormat = new System.Windows.Forms.Label();
            this.lblValImageData = new System.Windows.Forms.Label();
            this.lblValColorsInPal = new System.Windows.Forms.Label();
            this.lblValPaletteData = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.lblZoom = new System.Windows.Forms.Label();
            this.lblTransparentColorVal = new System.Windows.Forms.Label();
            this.lblTransparentColor = new System.Windows.Forms.Label();
            this.numZoom = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.pnlImageScroll = new Nyerguds.Util.UI.SelectablePanel();
            this.picImage = new RedCell.UI.Controls.PixelBox();
            this.btnViewPalette = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numZoom)).BeginInit();
            this.pnlImageScroll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // lblFilename
            // 
            this.lblFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilename.Location = new System.Drawing.Point(378, 15);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(119, 23);
            this.lblFilename.TabIndex = 100;
            this.lblFilename.Text = "Filename:";
            this.lblFilename.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWidth
            // 
            this.lblWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWidth.Location = new System.Drawing.Point(378, 38);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(119, 23);
            this.lblWidth.TabIndex = 102;
            this.lblWidth.Text = "Image width:";
            this.lblWidth.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHeight
            // 
            this.lblHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeight.Location = new System.Drawing.Point(378, 61);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(119, 23);
            this.lblHeight.TabIndex = 104;
            this.lblHeight.Text = "Image height:";
            this.lblHeight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBytesPerCol
            // 
            this.lblBytesPerCol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBytesPerCol.Location = new System.Drawing.Point(378, 84);
            this.lblBytesPerCol.Name = "lblBytesPerCol";
            this.lblBytesPerCol.Size = new System.Drawing.Size(119, 23);
            this.lblBytesPerCol.TabIndex = 106;
            this.lblBytesPerCol.Text = "Bytes per palette color:";
            this.lblBytesPerCol.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblColorformat
            // 
            this.lblColorformat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColorformat.Location = new System.Drawing.Point(378, 107);
            this.lblColorformat.Name = "lblColorformat";
            this.lblColorformat.Size = new System.Drawing.Size(119, 23);
            this.lblColorformat.TabIndex = 108;
            this.lblColorformat.Text = "Color format:";
            this.lblColorformat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblImageData
            // 
            this.lblImageData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblImageData.Location = new System.Drawing.Point(378, 153);
            this.lblImageData.Name = "lblImageData";
            this.lblImageData.Size = new System.Drawing.Size(119, 23);
            this.lblImageData.TabIndex = 114;
            this.lblImageData.Text = "Image data:";
            this.lblImageData.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPaletteData
            // 
            this.lblPaletteData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPaletteData.Location = new System.Drawing.Point(378, 176);
            this.lblPaletteData.Name = "lblPaletteData";
            this.lblPaletteData.Size = new System.Drawing.Size(119, 23);
            this.lblPaletteData.TabIndex = 116;
            this.lblPaletteData.Text = "Palette data:";
            this.lblPaletteData.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblValFilename
            // 
            this.lblValFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValFilename.Location = new System.Drawing.Point(518, 15);
            this.lblValFilename.Name = "lblValFilename";
            this.lblValFilename.Size = new System.Drawing.Size(154, 23);
            this.lblValFilename.TabIndex = 101;
            this.lblValFilename.Text = "---";
            this.lblValFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColorsInPal
            // 
            this.lblColorsInPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColorsInPal.Location = new System.Drawing.Point(378, 130);
            this.lblColorsInPal.Name = "lblColorsInPal";
            this.lblColorsInPal.Size = new System.Drawing.Size(119, 23);
            this.lblColorsInPal.TabIndex = 112;
            this.lblColorsInPal.Text = "Colors in palette:";
            this.lblColorsInPal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblValWidth
            // 
            this.lblValWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValWidth.Location = new System.Drawing.Point(518, 38);
            this.lblValWidth.Name = "lblValWidth";
            this.lblValWidth.Size = new System.Drawing.Size(154, 23);
            this.lblValWidth.TabIndex = 103;
            this.lblValWidth.Text = "---";
            this.lblValWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValHeight
            // 
            this.lblValHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValHeight.Location = new System.Drawing.Point(518, 61);
            this.lblValHeight.Name = "lblValHeight";
            this.lblValHeight.Size = new System.Drawing.Size(154, 23);
            this.lblValHeight.TabIndex = 105;
            this.lblValHeight.Text = "---";
            this.lblValHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValBytesPerCol
            // 
            this.lblValBytesPerCol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValBytesPerCol.Location = new System.Drawing.Point(518, 84);
            this.lblValBytesPerCol.Name = "lblValBytesPerCol";
            this.lblValBytesPerCol.Size = new System.Drawing.Size(154, 23);
            this.lblValBytesPerCol.TabIndex = 107;
            this.lblValBytesPerCol.Text = "---";
            this.lblValBytesPerCol.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValColorFormat
            // 
            this.lblValColorFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValColorFormat.Location = new System.Drawing.Point(518, 107);
            this.lblValColorFormat.Name = "lblValColorFormat";
            this.lblValColorFormat.Size = new System.Drawing.Size(154, 23);
            this.lblValColorFormat.TabIndex = 109;
            this.lblValColorFormat.Text = "---";
            this.lblValColorFormat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValImageData
            // 
            this.lblValImageData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValImageData.Location = new System.Drawing.Point(518, 153);
            this.lblValImageData.Name = "lblValImageData";
            this.lblValImageData.Size = new System.Drawing.Size(154, 23);
            this.lblValImageData.TabIndex = 115;
            this.lblValImageData.Text = "---";
            this.lblValImageData.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValColorsInPal
            // 
            this.lblValColorsInPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValColorsInPal.Location = new System.Drawing.Point(518, 130);
            this.lblValColorsInPal.Name = "lblValColorsInPal";
            this.lblValColorsInPal.Size = new System.Drawing.Size(154, 23);
            this.lblValColorsInPal.TabIndex = 113;
            this.lblValColorsInPal.Text = "---";
            this.lblValColorsInPal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValPaletteData
            // 
            this.lblValPaletteData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValPaletteData.Location = new System.Drawing.Point(518, 176);
            this.lblValPaletteData.Name = "lblValPaletteData";
            this.lblValPaletteData.Size = new System.Drawing.Size(154, 23);
            this.lblValPaletteData.TabIndex = 117;
            this.lblValPaletteData.Text = "---";
            this.lblValPaletteData.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Enabled = false;
            this.btnSave.Location = new System.Drawing.Point(591, 327);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(81, 23);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Save image";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpen.Location = new System.Drawing.Point(510, 327);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 1;
            this.btnOpen.Text = "Open image";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // lblZoom
            // 
            this.lblZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblZoom.Location = new System.Drawing.Point(171, 330);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(72, 20);
            this.lblZoom.TabIndex = 23;
            this.lblZoom.Text = "Zoom factor:";
            this.lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTransparentColorVal
            // 
            this.lblTransparentColorVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransparentColorVal.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.lblTransparentColorVal.Location = new System.Drawing.Point(133, 330);
            this.lblTransparentColorVal.Name = "lblTransparentColorVal";
            this.lblTransparentColorVal.Size = new System.Drawing.Size(20, 20);
            this.lblTransparentColorVal.TabIndex = 118;
            this.lblTransparentColorVal.Click += new System.EventHandler(this.lblTransparentColorVal_Click);
            // 
            // lblTransparentColor
            // 
            this.lblTransparentColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransparentColor.Location = new System.Drawing.Point(12, 330);
            this.lblTransparentColor.Name = "lblTransparentColor";
            this.lblTransparentColor.Size = new System.Drawing.Size(115, 20);
            this.lblTransparentColor.TabIndex = 23;
            this.lblTransparentColor.Text = "Transparency color:";
            this.lblTransparentColor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numZoom
            // 
            this.numZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numZoom.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.Location = new System.Drawing.Point(252, 330);
            this.numZoom.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numZoom.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.Name = "numZoom";
            this.numZoom.Size = new System.Drawing.Size(120, 20);
            this.numZoom.TabIndex = 4;
            this.numZoom.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.ValueChanged += new System.EventHandler(this.numZoom_ValueChanged);
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
            this.pnlImageScroll.Location = new System.Drawing.Point(12, 12);
            this.pnlImageScroll.Margin = new System.Windows.Forms.Padding(0);
            this.pnlImageScroll.Name = "pnlImageScroll";
            this.pnlImageScroll.Size = new System.Drawing.Size(360, 307);
            this.pnlImageScroll.TabIndex = 3;
            this.pnlImageScroll.TabStop = true;
            // 
            // picImage
            // 
            this.picImage.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.picImage.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            this.picImage.Location = new System.Drawing.Point(0, 0);
            this.picImage.Margin = new System.Windows.Forms.Padding(0);
            this.picImage.Name = "picImage";
            this.picImage.Size = new System.Drawing.Size(100, 100);
            this.picImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picImage.TabIndex = 0;
            this.picImage.TabStop = false;
            this.picImage.Visible = false;
            this.picImage.Click += new System.EventHandler(this.picImage_Click);
            // 
            // btnViewPalette
            // 
            this.btnViewPalette.Enabled = false;
            this.btnViewPalette.Location = new System.Drawing.Point(521, 203);
            this.btnViewPalette.Name = "btnViewPalette";
            this.btnViewPalette.Size = new System.Drawing.Size(75, 23);
            this.btnViewPalette.TabIndex = 119;
            this.btnViewPalette.Text = "View palette";
            this.btnViewPalette.UseVisualStyleBackColor = true;
            this.btnViewPalette.Click += new System.EventHandler(this.btnViewPalette_Click);
            // 
            // FrmCnC64ImgViewer
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 362);
            this.Controls.Add(this.btnViewPalette);
            this.Controls.Add(this.lblTransparentColorVal);
            this.Controls.Add(this.lblTransparentColor);
            this.Controls.Add(this.lblZoom);
            this.Controls.Add(this.numZoom);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblValPaletteData);
            this.Controls.Add(this.lblValColorsInPal);
            this.Controls.Add(this.lblValImageData);
            this.Controls.Add(this.lblValColorFormat);
            this.Controls.Add(this.lblValBytesPerCol);
            this.Controls.Add(this.lblValHeight);
            this.Controls.Add(this.lblValWidth);
            this.Controls.Add(this.lblColorsInPal);
            this.Controls.Add(this.lblValFilename);
            this.Controls.Add(this.lblPaletteData);
            this.Controls.Add(this.lblImageData);
            this.Controls.Add(this.lblColorformat);
            this.Controls.Add(this.lblBytesPerCol);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.pnlImageScroll);
            this.Icon = global::CnC64ImgViewer.Properties.Resources.cnc64logo;
            this.MinimumSize = new System.Drawing.Size(700, 300);
            this.Name = "FrmCnC64ImgViewer";
            this.Text = "N64 IMG format viewer - Created by Nyerguds";
            this.Shown += new System.EventHandler(this.FrmCnC64ImgViewer_Shown);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Frm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Frm_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.numZoom)).EndInit();
            this.pnlImageScroll.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private RedCell.UI.Controls.PixelBox picImage;
        private Nyerguds.Util.UI.SelectablePanel pnlImageScroll;
        private System.Windows.Forms.Label lblFilename;
        private System.Windows.Forms.Label lblWidth;
        private System.Windows.Forms.Label lblHeight;
        private System.Windows.Forms.Label lblBytesPerCol;
        private System.Windows.Forms.Label lblColorformat;
        private System.Windows.Forms.Label lblImageData;
        private System.Windows.Forms.Label lblPaletteData;
        private System.Windows.Forms.Label lblValFilename;
        private System.Windows.Forms.Label lblColorsInPal;
        private System.Windows.Forms.Label lblValWidth;
        private System.Windows.Forms.Label lblValHeight;
        private System.Windows.Forms.Label lblValBytesPerCol;
        private System.Windows.Forms.Label lblValColorFormat;
        private System.Windows.Forms.Label lblValImageData;
        private System.Windows.Forms.Label lblValColorsInPal;
        private System.Windows.Forms.Label lblValPaletteData;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnOpen;
        private Nyerguds.Util.UI.EnhNumericUpDown numZoom;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.Label lblTransparentColorVal;
        private System.Windows.Forms.Label lblTransparentColor;
        private System.Windows.Forms.Button btnViewPalette;
    }
}

