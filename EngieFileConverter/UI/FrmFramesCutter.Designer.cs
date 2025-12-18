namespace EngieFileConverter.UI
{
    partial class FrmFramesCutter
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
            this.lblWidth = new System.Windows.Forms.Label();
            this.lblHeight = new System.Windows.Forms.Label();
            this.numHeight = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.numWidth = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.pzpFramePreview = new Nyerguds.Util.UI.PixelZoomPanel();
            this.lblFramesOnImage = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblFramesOnImageVal = new System.Windows.Forms.Label();
            this.numCurFrame = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.lblCurFrame = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnConvert = new System.Windows.Forms.Button();
            this.lblImageSizeVal = new System.Windows.Forms.Label();
            this.lblImageSize = new System.Windows.Forms.Label();
            this.numFrames = new Nyerguds.Util.UI.EnhNumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCurFrame)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFrames)).BeginInit();
            this.SuspendLayout();
            // 
            // lblWidth
            // 
            this.lblWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(350, 69);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(38, 13);
            this.lblWidth.TabIndex = 2;
            this.lblWidth.Text = "Width:";
            // 
            // lblHeight
            // 
            this.lblHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(350, 95);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(41, 13);
            this.lblHeight.TabIndex = 4;
            this.lblHeight.Text = "Height:";
            // 
            // numHeight
            // 
            this.numHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numHeight.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.Location = new System.Drawing.Point(435, 93);
            this.numHeight.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.Name = "numHeight";
            this.numHeight.SelectedText = "";
            this.numHeight.SelectionLength = 0;
            this.numHeight.SelectionStart = 0;
            this.numHeight.Size = new System.Drawing.Size(87, 20);
            this.numHeight.TabIndex = 5;
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
            this.numWidth.Location = new System.Drawing.Point(435, 67);
            this.numWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWidth.Name = "numWidth";
            this.numWidth.SelectedText = "";
            this.numWidth.SelectionLength = 0;
            this.numWidth.SelectionStart = 0;
            this.numWidth.Size = new System.Drawing.Size(87, 20);
            this.numWidth.TabIndex = 3;
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
            this.pzpFramePreview.BackgroundFillColor = System.Drawing.Color.Fuchsia;
            this.pzpFramePreview.CustomColors = null;
            this.pzpFramePreview.Image = null;
            this.pzpFramePreview.ImageVisible = false;
            this.pzpFramePreview.Location = new System.Drawing.Point(11, 12);
            this.pzpFramePreview.Name = "pzpFramePreview";
            this.pzpFramePreview.Size = new System.Drawing.Size(322, 230);
            this.pzpFramePreview.TabIndex = 30;
            this.pzpFramePreview.ZoomFactor = 1;
            // 
            // lblFramesOnImage
            // 
            this.lblFramesOnImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFramesOnImage.AutoSize = true;
            this.lblFramesOnImage.Location = new System.Drawing.Point(350, 146);
            this.lblFramesOnImage.Name = "lblFramesOnImage";
            this.lblFramesOnImage.Size = new System.Drawing.Size(55, 13);
            this.lblFramesOnImage.TabIndex = 8;
            this.lblFramesOnImage.Text = "On image:";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(350, 121);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Frames:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(350, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(93, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Frame information:";
            // 
            // lblFramesOnImageVal
            // 
            this.lblFramesOnImageVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFramesOnImageVal.AutoSize = true;
            this.lblFramesOnImageVal.Location = new System.Drawing.Point(432, 147);
            this.lblFramesOnImageVal.Name = "lblFramesOnImageVal";
            this.lblFramesOnImageVal.Size = new System.Drawing.Size(25, 13);
            this.lblFramesOnImageVal.TabIndex = 9;
            this.lblFramesOnImageVal.Text = "1×1";
            // 
            // numCurFrame
            // 
            this.numCurFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numCurFrame.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCurFrame.Location = new System.Drawing.Point(435, 170);
            this.numCurFrame.Name = "numCurFrame";
            this.numCurFrame.SelectedText = "";
            this.numCurFrame.SelectionLength = 0;
            this.numCurFrame.SelectionStart = 0;
            this.numCurFrame.Size = new System.Drawing.Size(87, 20);
            this.numCurFrame.TabIndex = 21;
            this.numCurFrame.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numCurFrame.ValueChanged += new System.EventHandler(this.FrameChanged);
            // 
            // lblCurFrame
            // 
            this.lblCurFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurFrame.AutoSize = true;
            this.lblCurFrame.Location = new System.Drawing.Point(350, 172);
            this.lblCurFrame.Name = "lblCurFrame";
            this.lblCurFrame.Size = new System.Drawing.Size(66, 13);
            this.lblCurFrame.TabIndex = 20;
            this.lblCurFrame.Text = "Show frame:";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(447, 215);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 41;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnConvert
            // 
            this.btnConvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConvert.Location = new System.Drawing.Point(366, 215);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(75, 23);
            this.btnConvert.TabIndex = 40;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // lblImageSizeVal
            // 
            this.lblImageSizeVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblImageSizeVal.AutoSize = true;
            this.lblImageSizeVal.Location = new System.Drawing.Point(432, 18);
            this.lblImageSizeVal.Name = "lblImageSizeVal";
            this.lblImageSizeVal.Size = new System.Drawing.Size(25, 13);
            this.lblImageSizeVal.TabIndex = 104;
            this.lblImageSizeVal.Text = "1×1";
            // 
            // lblImageSize
            // 
            this.lblImageSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblImageSize.AutoSize = true;
            this.lblImageSize.Location = new System.Drawing.Point(350, 17);
            this.lblImageSize.Name = "lblImageSize";
            this.lblImageSize.Size = new System.Drawing.Size(60, 13);
            this.lblImageSize.TabIndex = 103;
            this.lblImageSize.Text = "Image size:";
            // 
            // numFrames
            // 
            this.numFrames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numFrames.EnteredValue = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFrames.Location = new System.Drawing.Point(435, 119);
            this.numFrames.Name = "numFrames";
            this.numFrames.SelectedText = "";
            this.numFrames.SelectionLength = 0;
            this.numFrames.SelectionStart = 0;
            this.numFrames.Size = new System.Drawing.Size(87, 20);
            this.numFrames.TabIndex = 21;
            this.numFrames.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFrames.ValueChanged += new System.EventHandler(this.numFrames_ValueChanged);
            // 
            // FrmFramesCutter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(534, 250);
            this.Controls.Add(this.lblImageSizeVal);
            this.Controls.Add(this.lblImageSize);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblCurFrame);
            this.Controls.Add(this.numFrames);
            this.Controls.Add(this.numCurFrame);
            this.Controls.Add(this.lblFramesOnImageVal);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblFramesOnImage);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.numHeight);
            this.Controls.Add(this.numWidth);
            this.Controls.Add(this.pzpFramePreview);
            this.Icon = global::EngieFileConverter.Properties.Resources.EngieIcon;
            this.MinimumSize = new System.Drawing.Size(550, 288);
            this.Name = "FrmFramesCutter";
            this.Text = "Image to frames";
            ((System.ComponentModel.ISupportInitialize)(this.numHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCurFrame)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFrames)).EndInit();
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
        private System.Windows.Forms.Label lblCurFrame;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Label lblImageSizeVal;
        private System.Windows.Forms.Label lblImageSize;
        private Nyerguds.Util.UI.EnhNumericUpDown numFrames;
    }
}