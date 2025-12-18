namespace EngieFileConverter.UI
{
    partial class FrmFramesCutter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblWidth = new System.Windows.Forms.Label();
            this.lblHeight = new System.Windows.Forms.Label();
            this.lblFramesOnImage = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblFramesOnImageVal = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnConvert = new System.Windows.Forms.Button();
            this.lblImageSizeVal = new System.Windows.Forms.Label();
            this.lblImageSize = new System.Windows.Forms.Label();
            this.lblCurFrame = new System.Windows.Forms.Label();
            this.chkTrimColor = new System.Windows.Forms.CheckBox();
            this.lblTrimColorVal = new System.Windows.Forms.Label();
            this.chkMatchPalette = new System.Windows.Forms.CheckBox();
            this.lblFrameSize = new System.Windows.Forms.Label();
            this.lblFrameSizeVal = new System.Windows.Forms.Label();
            this.cmbPalType = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.cmbPalettes = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.lblTrimColor = new Nyerguds.Util.UI.ImageButtonCheckBox();
            this.numFrames = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.numCurFrame = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.numHeight = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.numWidth = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.pzpFramePreview = new Nyerguds.Util.UI.PixelZoomPanel();
            ((System.ComponentModel.ISupportInitialize)(this.numFrames)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCurFrame)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWidth)).BeginInit();
            this.SuspendLayout();
            //
            // lblWidth
            //
            this.lblWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(350, 64);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(38, 13);
            this.lblWidth.TabIndex = 10;
            this.lblWidth.Text = "Width:";
            //
            // lblHeight
            //
            this.lblHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(350, 90);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(41, 13);
            this.lblHeight.TabIndex = 15;
            this.lblHeight.Text = "Height:";
            //
            // lblFramesOnImage
            //
            this.lblFramesOnImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFramesOnImage.AutoSize = true;
            this.lblFramesOnImage.Location = new System.Drawing.Point(349, 116);
            this.lblFramesOnImage.Name = "lblFramesOnImage";
            this.lblFramesOnImage.Size = new System.Drawing.Size(90, 13);
            this.lblFramesOnImage.TabIndex = 20;
            this.lblFramesOnImage.Text = "Frames on image:";
            //
            // label5
            //
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(350, 142);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 25;
            this.label5.Text = "Limit frames:";
            //
            // label3
            //
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(350, 38);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(93, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Frame information:";
            //
            // lblFramesOnImageVal
            //
            this.lblFramesOnImageVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFramesOnImageVal.AutoSize = true;
            this.lblFramesOnImageVal.Location = new System.Drawing.Point(441, 116);
            this.lblFramesOnImageVal.Name = "lblFramesOnImageVal";
            this.lblFramesOnImageVal.Size = new System.Drawing.Size(40, 13);
            this.lblFramesOnImageVal.TabIndex = 21;
            this.lblFramesOnImageVal.Text = "1 (1×1)";
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(457, 337);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 201;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            //
            // btnConvert
            //
            this.btnConvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConvert.Location = new System.Drawing.Point(376, 337);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(75, 23);
            this.btnConvert.TabIndex = 200;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.BtnConvertClick);
            //
            // lblImageSizeVal
            //
            this.lblImageSizeVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblImageSizeVal.AutoSize = true;
            this.lblImageSizeVal.Location = new System.Drawing.Point(441, 12);
            this.lblImageSizeVal.Name = "lblImageSizeVal";
            this.lblImageSizeVal.Size = new System.Drawing.Size(25, 13);
            this.lblImageSizeVal.TabIndex = 2;
            this.lblImageSizeVal.Text = "1×1";
            //
            // lblImageSize
            //
            this.lblImageSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblImageSize.AutoSize = true;
            this.lblImageSize.Location = new System.Drawing.Point(350, 12);
            this.lblImageSize.Name = "lblImageSize";
            this.lblImageSize.Size = new System.Drawing.Size(60, 13);
            this.lblImageSize.TabIndex = 1;
            this.lblImageSize.Text = "Image size:";
            //
            // lblCurFrame
            //
            this.lblCurFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurFrame.AutoSize = true;
            this.lblCurFrame.Location = new System.Drawing.Point(349, 168);
            this.lblCurFrame.Name = "lblCurFrame";
            this.lblCurFrame.Size = new System.Drawing.Size(66, 13);
            this.lblCurFrame.TabIndex = 30;
            this.lblCurFrame.Text = "Show frame:";
            this.lblCurFrame.Click += new System.EventHandler(this.lblCurFrame_Click);
            //
            // chkTrimColor
            //
            this.chkTrimColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkTrimColor.AutoSize = true;
            this.chkTrimColor.Location = new System.Drawing.Point(359, 219);
            this.chkTrimColor.Name = "chkTrimColor";
            this.chkTrimColor.Size = new System.Drawing.Size(75, 17);
            this.chkTrimColor.TabIndex = 40;
            this.chkTrimColor.Text = "Trim color:";
            this.chkTrimColor.UseVisualStyleBackColor = true;
            this.chkTrimColor.CheckedChanged += new System.EventHandler(this.ChkTrimColor_CheckedChanged);
            //
            // lblTrimColorVal
            //
            this.lblTrimColorVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTrimColorVal.AutoSize = true;
            this.lblTrimColorVal.Enabled = false;
            this.lblTrimColorVal.Location = new System.Drawing.Point(465, 220);
            this.lblTrimColorVal.Name = "lblTrimColorVal";
            this.lblTrimColorVal.Size = new System.Drawing.Size(10, 13);
            this.lblTrimColorVal.TabIndex = 42;
            this.lblTrimColorVal.Text = "-";
            //
            // chkMatchPalette
            //
            this.chkMatchPalette.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkMatchPalette.AutoSize = true;
            this.chkMatchPalette.Location = new System.Drawing.Point(353, 245);
            this.chkMatchPalette.Name = "chkMatchPalette";
            this.chkMatchPalette.Size = new System.Drawing.Size(94, 17);
            this.chkMatchPalette.TabIndex = 45;
            this.chkMatchPalette.Text = "Match palette:";
            this.chkMatchPalette.UseVisualStyleBackColor = true;
            this.chkMatchPalette.CheckedChanged += new System.EventHandler(this.ChkMatchPaletteCheckedChanged);
            //
            // lblFrameSize
            //
            this.lblFrameSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFrameSize.AutoSize = true;
            this.lblFrameSize.Location = new System.Drawing.Point(349, 194);
            this.lblFrameSize.Name = "lblFrameSize";
            this.lblFrameSize.Size = new System.Drawing.Size(60, 13);
            this.lblFrameSize.TabIndex = 35;
            this.lblFrameSize.Text = "Frame size:";
            //
            // lblFrameSizeVal
            //
            this.lblFrameSizeVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFrameSizeVal.AutoSize = true;
            this.lblFrameSizeVal.Location = new System.Drawing.Point(441, 194);
            this.lblFrameSizeVal.Name = "lblFrameSizeVal";
            this.lblFrameSizeVal.Size = new System.Drawing.Size(25, 13);
            this.lblFrameSizeVal.TabIndex = 36;
            this.lblFrameSizeVal.Text = "1×1";
            //
            // cmbPalType
            //
            this.cmbPalType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalType.Enabled = false;
            this.cmbPalType.FormattingEnabled = true;
            this.cmbPalType.Location = new System.Drawing.Point(444, 243);
            this.cmbPalType.Name = "cmbPalType";
            this.cmbPalType.Size = new System.Drawing.Size(88, 21);
            this.cmbPalType.TabIndex = 46;
            this.cmbPalType.SelectedIndexChanged += new System.EventHandler(this.CmbPalTypeSelectedIndexChanged);
            //
            // cmbPalettes
            //
            this.cmbPalettes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalettes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalettes.Enabled = false;
            this.cmbPalettes.FormattingEnabled = true;
            this.cmbPalettes.Location = new System.Drawing.Point(353, 270);
            this.cmbPalettes.Name = "cmbPalettes";
            this.cmbPalettes.Size = new System.Drawing.Size(179, 21);
            this.cmbPalettes.TabIndex = 50;
            this.cmbPalettes.SelectedIndexChanged += new System.EventHandler(this.cmbPalettes_SelectedIndexChanged);
            //
            // lblTrimColor
            //
            this.lblTrimColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTrimColor.BackColor = System.Drawing.Color.Black;
            this.lblTrimColor.Checked = true;
            this.lblTrimColor.DisabledBackColor = System.Drawing.Color.DarkGray;
            this.lblTrimColor.Enabled = false;
            this.lblTrimColor.Location = new System.Drawing.Point(444, 218);
            this.lblTrimColor.Name = "lblTrimColor";
            this.lblTrimColor.Size = new System.Drawing.Size(20, 20);
            this.lblTrimColor.TabIndex = 41;
            this.lblTrimColor.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblTrimColor.Toggle = false;
            this.lblTrimColor.TrueBackColor = System.Drawing.Color.Black;
            this.lblTrimColor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.lblTrimColor_KeyPress);
            this.lblTrimColor.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lblTrimColor_MouseClick);
            //
            // numFrames
            //
            this.numFrames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numFrames.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFrames.Location = new System.Drawing.Point(444, 140);
            this.numFrames.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFrames.Name = "numFrames";
            this.numFrames.SelectedText = "";
            this.numFrames.SelectionLength = 0;
            this.numFrames.SelectionStart = 0;
            this.numFrames.Size = new System.Drawing.Size(88, 20);
            this.numFrames.TabIndex = 26;
            this.numFrames.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFrames.ValueChanged += new System.EventHandler(this.NumFramesValueChanged);
            //
            // numCurFrame
            //
            this.numCurFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numCurFrame.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCurFrame.Location = new System.Drawing.Point(444, 166);
            this.numCurFrame.Name = "numCurFrame";
            this.numCurFrame.SelectedText = "";
            this.numCurFrame.SelectionLength = 0;
            this.numCurFrame.SelectionStart = 0;
            this.numCurFrame.Size = new System.Drawing.Size(88, 20);
            this.numCurFrame.TabIndex = 31;
            this.numCurFrame.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCurFrame.ValueChanged += new System.EventHandler(this.FrameChanged);
            //
            // numHeight
            //
            this.numHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numHeight.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.Location = new System.Drawing.Point(444, 88);
            this.numHeight.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.Name = "numHeight";
            this.numHeight.SelectedText = "";
            this.numHeight.SelectionLength = 0;
            this.numHeight.SelectionStart = 0;
            this.numHeight.Size = new System.Drawing.Size(88, 20);
            this.numHeight.TabIndex = 16;
            this.numHeight.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.ValueChanged += new System.EventHandler(this.DimensionsChanged);
            //
            // numWidth
            //
            this.numWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numWidth.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWidth.Location = new System.Drawing.Point(444, 62);
            this.numWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWidth.Name = "numWidth";
            this.numWidth.SelectedText = "";
            this.numWidth.SelectionLength = 0;
            this.numWidth.SelectionStart = 0;
            this.numWidth.Size = new System.Drawing.Size(88, 20);
            this.numWidth.TabIndex = 11;
            this.numWidth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWidth.ValueChanged += new System.EventHandler(this.DimensionsChanged);
            //
            // pzpFramePreview
            //
            this.pzpFramePreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pzpFramePreview.CustomColors = null;
            this.pzpFramePreview.Image = null;
            this.pzpFramePreview.ImageVisible = false;
            this.pzpFramePreview.Location = new System.Drawing.Point(11, 12);
            this.pzpFramePreview.Name = "pzpFramePreview";
            this.pzpFramePreview.Size = new System.Drawing.Size(322, 350);
            this.pzpFramePreview.TabIndex = 100;
            this.pzpFramePreview.ZoomFactor = 1;
            this.pzpFramePreview.ZoomFactorMinimum = -10;
            //
            // FrmFramesCutter
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(544, 372);
            this.Controls.Add(this.cmbPalType);
            this.Controls.Add(this.cmbPalettes);
            this.Controls.Add(this.chkMatchPalette);
            this.Controls.Add(this.lblTrimColor);
            this.Controls.Add(this.lblImageSizeVal);
            this.Controls.Add(this.lblImageSize);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.numFrames);
            this.Controls.Add(this.numCurFrame);
            this.Controls.Add(this.lblFrameSizeVal);
            this.Controls.Add(this.lblFramesOnImageVal);
            this.Controls.Add(this.lblTrimColorVal);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblFrameSize);
            this.Controls.Add(this.lblFramesOnImage);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.numHeight);
            this.Controls.Add(this.numWidth);
            this.Controls.Add(this.pzpFramePreview);
            this.Controls.Add(this.lblCurFrame);
            this.Controls.Add(this.chkTrimColor);
            this.Icon = global::EngieFileConverter.Properties.Resources.EngieIcon;
            this.MinimumSize = new System.Drawing.Size(560, 410);
            this.Name = "FrmFramesCutter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Image to frames";
            ((System.ComponentModel.ISupportInitialize)(this.numFrames)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCurFrame)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWidth)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblWidth;
        private System.Windows.Forms.Label lblHeight;
        private Nyerguds.Util.UI.EnhNumericUpDown numHeight;
        private Nyerguds.Util.UI.EnhNumericUpDown numWidth;
        private Nyerguds.Util.UI.PixelZoomPanel pzpFramePreview;
        private System.Windows.Forms.Label lblFramesOnImage;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblFramesOnImageVal;
        private Nyerguds.Util.UI.EnhNumericUpDown numCurFrame;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Label lblImageSizeVal;
        private System.Windows.Forms.Label lblImageSize;
        private Nyerguds.Util.UI.EnhNumericUpDown numFrames;
        private System.Windows.Forms.Label lblCurFrame;
        private System.Windows.Forms.CheckBox chkTrimColor;
        private Nyerguds.Util.UI.ImageButtonCheckBox lblTrimColor;
        private System.Windows.Forms.Label lblTrimColorVal;
        private System.Windows.Forms.CheckBox chkMatchPalette;
        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbPalettes;
        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbPalType;
        private System.Windows.Forms.Label lblFrameSize;
        private System.Windows.Forms.Label lblFrameSizeVal;
    }
}