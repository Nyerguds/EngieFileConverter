namespace EngieFileConverter.UI
{
    partial class FrmFramesToPal
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnConvert = new System.Windows.Forms.Button();
            this.lblMatchPalette = new System.Windows.Forms.Label();
            this.cmbPalType = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.cmbPalettes = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.pzpFramePreview = new Nyerguds.Util.UI.PixelZoomPanel();
            this.numCurFrame = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.lblCurFrame = new System.Windows.Forms.Label();
            this.palPreviewPal = new Nyerguds.Util.UI.PalettePanel();
            ((System.ComponentModel.ISupportInitialize)(this.numCurFrame)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(467, 337);
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
            this.btnConvert.Location = new System.Drawing.Point(386, 337);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(75, 23);
            this.btnConvert.TabIndex = 200;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.BtnConvertClick);
            // 
            // lblMatchPalette
            // 
            this.lblMatchPalette.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMatchPalette.AutoSize = true;
            this.lblMatchPalette.Location = new System.Drawing.Point(339, 20);
            this.lblMatchPalette.Name = "lblMatchPalette";
            this.lblMatchPalette.Size = new System.Drawing.Size(75, 13);
            this.lblMatchPalette.TabIndex = 45;
            this.lblMatchPalette.Text = "Match palette:";
            // 
            // cmbPalType
            // 
            this.cmbPalType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalType.FormattingEnabled = true;
            this.cmbPalType.Location = new System.Drawing.Point(452, 17);
            this.cmbPalType.Name = "cmbPalType";
            this.cmbPalType.Size = new System.Drawing.Size(90, 21);
            this.cmbPalType.TabIndex = 46;
            this.cmbPalType.SelectedIndexChanged += new System.EventHandler(this.CmbPalTypeSelectedIndexChanged);
            // 
            // cmbPalettes
            // 
            this.cmbPalettes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalettes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalettes.FormattingEnabled = true;
            this.cmbPalettes.Location = new System.Drawing.Point(342, 45);
            this.cmbPalettes.Name = "cmbPalettes";
            this.cmbPalettes.Size = new System.Drawing.Size(200, 21);
            this.cmbPalettes.TabIndex = 50;
            this.cmbPalettes.SelectedIndexChanged += new System.EventHandler(this.cmbPalettes_SelectedIndexChanged);
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
            // numCurFrame
            // 
            this.numCurFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numCurFrame.EnteredValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numCurFrame.Location = new System.Drawing.Point(452, 272);
            this.numCurFrame.Name = "numCurFrame";
            this.numCurFrame.SelectedText = "";
            this.numCurFrame.SelectionLength = 0;
            this.numCurFrame.SelectionStart = 0;
            this.numCurFrame.Size = new System.Drawing.Size(90, 20);
            this.numCurFrame.TabIndex = 203;
            this.numCurFrame.ValueChanged += new System.EventHandler(this.FrameChanged);
            // 
            // lblCurFrame
            // 
            this.lblCurFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurFrame.AutoSize = true;
            this.lblCurFrame.Location = new System.Drawing.Point(358, 274);
            this.lblCurFrame.Name = "lblCurFrame";
            this.lblCurFrame.Size = new System.Drawing.Size(66, 13);
            this.lblCurFrame.TabIndex = 202;
            this.lblCurFrame.Text = "Show frame:";
            // 
            // palPreviewPal
            // 
            this.palPreviewPal.AutoSize = true;
            this.palPreviewPal.ColorSelectMode = Nyerguds.Util.UI.ColorSelMode.None;
            this.palPreviewPal.LabelSize = new System.Drawing.Size(10, 10);
            this.palPreviewPal.Location = new System.Drawing.Point(342, 72);
            this.palPreviewPal.Name = "palPreviewPal";
            this.palPreviewPal.PadBetween = new System.Drawing.Point(2, 2);
            this.palPreviewPal.Palette = null;
            this.palPreviewPal.Remap = null;
            this.palPreviewPal.SelectedIndices = new int[0];
            this.palPreviewPal.Size = new System.Drawing.Size(194, 194);
            this.palPreviewPal.TabIndex = 204;
            // 
            // FrmFramesToPal
            // 
            this.AcceptButton = this.btnConvert;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(554, 372);
            this.Controls.Add(this.palPreviewPal);
            this.Controls.Add(this.numCurFrame);
            this.Controls.Add(this.lblCurFrame);
            this.Controls.Add(this.cmbPalType);
            this.Controls.Add(this.cmbPalettes);
            this.Controls.Add(this.lblMatchPalette);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.pzpFramePreview);
            this.Icon = global::EngieFileConverter.Properties.Resources.EngieIcon;
            this.MinimumSize = new System.Drawing.Size(560, 410);
            this.Name = "FrmFramesToPal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Match to palette";
            this.Load += new System.EventHandler(this.FrmFramesToPal_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numCurFrame)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Nyerguds.Util.UI.PixelZoomPanel pzpFramePreview;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Label lblMatchPalette;
        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbPalettes;
        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbPalType;
        private Nyerguds.Util.UI.EnhNumericUpDown numCurFrame;
        private System.Windows.Forms.Label lblCurFrame;
        private Nyerguds.Util.UI.PalettePanel palPreviewPal;
    }
}