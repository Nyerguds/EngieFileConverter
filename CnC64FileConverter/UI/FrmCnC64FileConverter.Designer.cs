namespace CnC64FileConverter.UI
{
    partial class FrmCnC64FileConverter
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
            this.lblColorformat = new System.Windows.Forms.Label();
            this.lblValFilename = new System.Windows.Forms.Label();
            this.lblColorsInPal = new System.Windows.Forms.Label();
            this.lblValWidth = new System.Windows.Forms.Label();
            this.lblValHeight = new System.Windows.Forms.Label();
            this.lblValColorFormat = new System.Windows.Forms.Label();
            this.lblValColorsInPal = new System.Windows.Forms.Label();
            this.lblZoom = new System.Windows.Forms.Label();
            this.lblTransparentColorVal = new System.Windows.Forms.Label();
            this.lblTransparentColor = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.lblValType = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExport = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.convertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.heightMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiToHeightMap = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiToPlateaus = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiToHeightMapAdv = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiTo65x65HeightMap = new System.Windows.Forms.ToolStripMenuItem();
            this.palettePanel1 = new Nyerguds.Util.UI.PalettePanel();
            this.numZoom = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.pnlImageScroll = new Nyerguds.Util.UI.SelectablePanel();
            this.picImage = new RedCell.UI.Controls.PixelBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numZoom)).BeginInit();
            this.pnlImageScroll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // lblFilename
            // 
            this.lblFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilename.Location = new System.Drawing.Point(444, 24);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(94, 23);
            this.lblFilename.TabIndex = 100;
            this.lblFilename.Text = "Filename:";
            this.lblFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWidth
            // 
            this.lblWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWidth.Location = new System.Drawing.Point(444, 100);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(94, 23);
            this.lblWidth.TabIndex = 102;
            this.lblWidth.Text = "Image width:";
            this.lblWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHeight
            // 
            this.lblHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeight.Location = new System.Drawing.Point(444, 123);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(94, 23);
            this.lblHeight.TabIndex = 104;
            this.lblHeight.Text = "Image height:";
            this.lblHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColorformat
            // 
            this.lblColorformat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColorformat.Location = new System.Drawing.Point(444, 146);
            this.lblColorformat.Name = "lblColorformat";
            this.lblColorformat.Size = new System.Drawing.Size(94, 23);
            this.lblColorformat.TabIndex = 108;
            this.lblColorformat.Text = "Color format:";
            this.lblColorformat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValFilename
            // 
            this.lblValFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValFilename.Location = new System.Drawing.Point(444, 47);
            this.lblValFilename.Name = "lblValFilename";
            this.lblValFilename.Size = new System.Drawing.Size(294, 23);
            this.lblValFilename.TabIndex = 101;
            this.lblValFilename.Text = "---";
            this.lblValFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblColorsInPal
            // 
            this.lblColorsInPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColorsInPal.Location = new System.Drawing.Point(444, 169);
            this.lblColorsInPal.Name = "lblColorsInPal";
            this.lblColorsInPal.Size = new System.Drawing.Size(94, 23);
            this.lblColorsInPal.TabIndex = 112;
            this.lblColorsInPal.Text = "Colors in palette:";
            this.lblColorsInPal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValWidth
            // 
            this.lblValWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValWidth.Location = new System.Drawing.Point(544, 100);
            this.lblValWidth.Name = "lblValWidth";
            this.lblValWidth.Size = new System.Drawing.Size(194, 23);
            this.lblValWidth.TabIndex = 103;
            this.lblValWidth.Text = "---";
            this.lblValWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValHeight
            // 
            this.lblValHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValHeight.Location = new System.Drawing.Point(544, 123);
            this.lblValHeight.Name = "lblValHeight";
            this.lblValHeight.Size = new System.Drawing.Size(194, 23);
            this.lblValHeight.TabIndex = 105;
            this.lblValHeight.Text = "---";
            this.lblValHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValColorFormat
            // 
            this.lblValColorFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValColorFormat.Location = new System.Drawing.Point(544, 146);
            this.lblValColorFormat.Name = "lblValColorFormat";
            this.lblValColorFormat.Size = new System.Drawing.Size(194, 23);
            this.lblValColorFormat.TabIndex = 109;
            this.lblValColorFormat.Text = "---";
            this.lblValColorFormat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValColorsInPal
            // 
            this.lblValColorsInPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValColorsInPal.Location = new System.Drawing.Point(544, 169);
            this.lblValColorsInPal.Name = "lblValColorsInPal";
            this.lblValColorsInPal.Size = new System.Drawing.Size(194, 23);
            this.lblValColorsInPal.TabIndex = 113;
            this.lblValColorsInPal.Text = "---";
            this.lblValColorsInPal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZoom
            // 
            this.lblZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblZoom.Location = new System.Drawing.Point(237, 436);
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
            this.lblTransparentColorVal.Location = new System.Drawing.Point(199, 436);
            this.lblTransparentColorVal.Name = "lblTransparentColorVal";
            this.lblTransparentColorVal.Size = new System.Drawing.Size(20, 20);
            this.lblTransparentColorVal.TabIndex = 118;
            this.lblTransparentColorVal.Click += new System.EventHandler(this.lblTransparentColorVal_Click);
            // 
            // lblTransparentColor
            // 
            this.lblTransparentColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransparentColor.Location = new System.Drawing.Point(78, 436);
            this.lblTransparentColor.Name = "lblTransparentColor";
            this.lblTransparentColor.Size = new System.Drawing.Size(115, 20);
            this.lblTransparentColor.TabIndex = 23;
            this.lblTransparentColor.Text = "Transparency color:";
            this.lblTransparentColor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblType
            // 
            this.lblType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblType.Location = new System.Drawing.Point(444, 77);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(94, 23);
            this.lblType.TabIndex = 102;
            this.lblType.Text = "File type:";
            this.lblType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblValType
            // 
            this.lblValType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValType.Location = new System.Drawing.Point(544, 77);
            this.lblValType.Name = "lblValType";
            this.lblValType.Size = new System.Drawing.Size(194, 23);
            this.lblValType.TabIndex = 120;
            this.lblValType.Text = "---";
            this.lblValType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.convertToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(684, 24);
            this.menuStrip1.TabIndex = 121;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpen,
            this.tsmiSave,
            this.tsmiExport,
            this.tsmiExit});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // tsmiOpen
            // 
            this.tsmiOpen.Name = "tsmiOpen";
            this.tsmiOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.tsmiOpen.Size = new System.Drawing.Size(199, 22);
            this.tsmiOpen.Text = "&Open File";
            this.tsmiOpen.Click += new System.EventHandler(this.BtnOpen_Click);
            // 
            // tsmiSave
            // 
            this.tsmiSave.Name = "tsmiSave";
            this.tsmiSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.tsmiSave.Size = new System.Drawing.Size(199, 22);
            this.tsmiSave.Text = "&Save File...";
            this.tsmiSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // tsmiExport
            // 
            this.tsmiExport.Name = "tsmiExport";
            this.tsmiExport.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.tsmiExport.Size = new System.Drawing.Size(199, 22);
            this.tsmiExport.Text = "Quick Conv&ert...";
            this.tsmiExport.Click += new System.EventHandler(this.BtnSaveExport_Click);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.tsmiExit.Size = new System.Drawing.Size(199, 22);
            this.tsmiExit.Text = "Exit";
            this.tsmiExit.Click += new System.EventHandler(this.BtnExit_Click);
            // 
            // convertToolStripMenuItem
            // 
            this.convertToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.heightMapToolStripMenuItem});
            this.convertToolStripMenuItem.Name = "convertToolStripMenuItem";
            this.convertToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.convertToolStripMenuItem.Text = "Convert";
            // 
            // heightMapToolStripMenuItem
            // 
            this.heightMapToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiToHeightMap,
            this.tsmiToPlateaus,
            this.tsmiToHeightMapAdv,
            this.tsmiTo65x65HeightMap});
            this.heightMapToolStripMenuItem.Name = "heightMapToolStripMenuItem";
            this.heightMapToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.heightMapToolStripMenuItem.Text = "Height map generation";
            // 
            // tsmiToHeightMap
            // 
            this.tsmiToHeightMap.Name = "tsmiToHeightMap";
            this.tsmiToHeightMap.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
            this.tsmiToHeightMap.Size = new System.Drawing.Size(330, 22);
            this.tsmiToHeightMap.Text = "To &basic height map image (from map)";
            this.tsmiToHeightMap.Click += new System.EventHandler(this.TsmiToHeightMap_Click);
            // 
            // tsmiToPlateaus
            // 
            this.tsmiToPlateaus.Name = "tsmiToPlateaus";
            this.tsmiToPlateaus.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.tsmiToPlateaus.Size = new System.Drawing.Size(330, 22);
            this.tsmiToPlateaus.Text = "To basic &levels image (from map)";
            this.tsmiToPlateaus.Click += new System.EventHandler(this.tsmiToPlateaus_Click);
            // 
            // tsmiToHeightMapAdv
            // 
            this.tsmiToHeightMapAdv.Name = "tsmiToHeightMapAdv";
            this.tsmiToHeightMapAdv.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this.tsmiToHeightMapAdv.Size = new System.Drawing.Size(330, 22);
            this.tsmiToHeightMapAdv.Text = "To &height map using levels (from map)";
            this.tsmiToHeightMapAdv.Click += new System.EventHandler(this.TsmiToHeightMapAdv_Click);
            // 
            // tsmiTo65x65HeightMap
            // 
            this.tsmiTo65x65HeightMap.Name = "tsmiTo65x65HeightMap";
            this.tsmiTo65x65HeightMap.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.tsmiTo65x65HeightMap.Size = new System.Drawing.Size(330, 22);
            this.tsmiTo65x65HeightMap.Text = "To 65x65 height map &image (from image)";
            this.tsmiTo65x65HeightMap.Click += new System.EventHandler(this.TsmiTo65x65HeightMap_Click);
            // 
            // palettePanel1
            // 
            this.palettePanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.palettePanel1.AutoSize = true;
            this.palettePanel1.ColorSelectMode = Nyerguds.Util.UI.ColorSelMode.None;
            this.palettePanel1.EmptyItemBackColor = System.Drawing.Color.Empty;
            this.palettePanel1.EmptyItemChar = '\0';
            this.palettePanel1.EmptyItemCharColor = System.Drawing.Color.Empty;
            this.palettePanel1.EmptyItemToolTip = "";
            this.palettePanel1.LabelSize = new System.Drawing.Size(12, 12);
            this.palettePanel1.Location = new System.Drawing.Point(447, 195);
            this.palettePanel1.Name = "palettePanel1";
            this.palettePanel1.PadBetween = new System.Drawing.Point(2, 2);
            this.palettePanel1.Palette = null;
            this.palettePanel1.Remap = null;
            this.palettePanel1.SelectedIndices = new int[0];
            this.palettePanel1.Size = new System.Drawing.Size(226, 226);
            this.palettePanel1.TabIndex = 122;
            this.palettePanel1.TransItemBackColor = System.Drawing.Color.Empty;
            // 
            // numZoom
            // 
            this.numZoom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numZoom.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numZoom.Location = new System.Drawing.Point(318, 436);
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
            this.pnlImageScroll.Location = new System.Drawing.Point(12, 30);
            this.pnlImageScroll.Margin = new System.Windows.Forms.Padding(0);
            this.pnlImageScroll.Name = "pnlImageScroll";
            this.pnlImageScroll.Size = new System.Drawing.Size(426, 398);
            this.pnlImageScroll.TabIndex = 3;
            this.pnlImageScroll.TabStop = true;
            // 
            // picImage
            // 
            this.picImage.BackColor = System.Drawing.SystemColors.ActiveCaption;
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
            // FrmCnC64FileConverter
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 462);
            this.Controls.Add(this.palettePanel1);
            this.Controls.Add(this.lblValType);
            this.Controls.Add(this.lblTransparentColorVal);
            this.Controls.Add(this.lblTransparentColor);
            this.Controls.Add(this.lblZoom);
            this.Controls.Add(this.numZoom);
            this.Controls.Add(this.lblValColorsInPal);
            this.Controls.Add(this.lblValColorFormat);
            this.Controls.Add(this.lblValHeight);
            this.Controls.Add(this.lblValWidth);
            this.Controls.Add(this.lblColorsInPal);
            this.Controls.Add(this.lblValFilename);
            this.Controls.Add(this.lblColorformat);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.pnlImageScroll);
            this.Controls.Add(this.menuStrip1);
            this.Icon = global::CnC64FileConverter.Properties.Resources.cnc64logo;
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(700, 500);
            this.Name = "FrmCnC64FileConverter";
            this.Text = "C&C64 File Converter - Created by Nyerguds";
            this.Shown += new System.EventHandler(this.FrmCnC64FileConverter_Shown);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Frm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Frm_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numZoom)).EndInit();
            this.pnlImageScroll.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private RedCell.UI.Controls.PixelBox picImage;
        private Nyerguds.Util.UI.SelectablePanel pnlImageScroll;
        private System.Windows.Forms.Label lblFilename;
        private System.Windows.Forms.Label lblWidth;
        private System.Windows.Forms.Label lblHeight;
        private System.Windows.Forms.Label lblColorformat;
        private System.Windows.Forms.Label lblValFilename;
        private System.Windows.Forms.Label lblColorsInPal;
        private System.Windows.Forms.Label lblValWidth;
        private System.Windows.Forms.Label lblValHeight;
        private System.Windows.Forms.Label lblValColorFormat;
        private System.Windows.Forms.Label lblValColorsInPal;
        private Nyerguds.Util.UI.EnhNumericUpDown numZoom;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.Label lblTransparentColorVal;
        private System.Windows.Forms.Label lblTransparentColor;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Label lblValType;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpen;
        private System.Windows.Forms.ToolStripMenuItem tsmiSave;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiExport;
        private Nyerguds.Util.UI.PalettePanel palettePanel1;
        private System.Windows.Forms.ToolStripMenuItem convertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem heightMapToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiToHeightMap;
        private System.Windows.Forms.ToolStripMenuItem tsmiToPlateaus;
        private System.Windows.Forms.ToolStripMenuItem tsmiToHeightMapAdv;
        private System.Windows.Forms.ToolStripMenuItem tsmiTo65x65HeightMap;
    }
}

