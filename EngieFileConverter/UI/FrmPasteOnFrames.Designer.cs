namespace EngieFileConverter.UI
{
    partial class FrmPasteOnFrames
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
            this.lblFramesRange = new System.Windows.Forms.Label();
            this.lblCoordinatesY = new System.Windows.Forms.Label();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.lblCoordinatesX = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblImage = new System.Windows.Forms.Label();
            this.txtImage = new System.Windows.Forms.TextBox();
            this.btnSelectImage = new System.Windows.Forms.Button();
            this.lblPaletteHandling = new System.Windows.Forms.Label();
            this.rbtMatchPalette = new System.Windows.Forms.RadioButton();
            this.rbtKeepIndices = new System.Windows.Forms.RadioButton();
            this.txtFrames = new System.Windows.Forms.TextBox();
            this.btnClipboard = new System.Windows.Forms.Button();
            this.numCoordsY = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.numCoordsX = new Nyerguds.Util.UI.EnhNumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numCoordsY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCoordsX)).BeginInit();
            this.SuspendLayout();
            // 
            // lblFramesRange
            // 
            this.lblFramesRange.AutoSize = true;
            this.lblFramesRange.Location = new System.Drawing.Point(13, 86);
            this.lblFramesRange.Name = "lblFramesRange";
            this.lblFramesRange.Size = new System.Drawing.Size(261, 13);
            this.lblFramesRange.TabIndex = 20;
            this.lblFramesRange.Text = "Frames: (comma-separated. Allows ranges like \"5-10\")";
            // 
            // lblCoordinatesY
            // 
            this.lblCoordinatesY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCoordinatesY.AutoSize = true;
            this.lblCoordinatesY.Location = new System.Drawing.Point(209, 60);
            this.lblCoordinatesY.Name = "lblCoordinatesY";
            this.lblCoordinatesY.Size = new System.Drawing.Size(17, 13);
            this.lblCoordinatesY.TabIndex = 13;
            this.lblCoordinatesY.Text = "Y:";
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.AutoSize = true;
            this.lblCoordinates.Location = new System.Drawing.Point(13, 60);
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(95, 13);
            this.lblCoordinates.TabIndex = 10;
            this.lblCoordinates.Text = "Paste coordinates:";
            // 
            // lblCoordinatesX
            // 
            this.lblCoordinatesX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCoordinatesX.AutoSize = true;
            this.lblCoordinatesX.Location = new System.Drawing.Point(114, 60);
            this.lblCoordinatesX.Name = "lblCoordinatesX";
            this.lblCoordinatesX.Size = new System.Drawing.Size(17, 13);
            this.lblCoordinatesX.TabIndex = 11;
            this.lblCoordinatesX.Text = "X:";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(136, 226);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 50;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOkClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(217, 226);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 51;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblImage
            // 
            this.lblImage.AutoSize = true;
            this.lblImage.Location = new System.Drawing.Point(13, 9);
            this.lblImage.Name = "lblImage";
            this.lblImage.Size = new System.Drawing.Size(39, 13);
            this.lblImage.TabIndex = 10;
            this.lblImage.Text = "Image:";
            // 
            // txtImage
            // 
            this.txtImage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtImage.BackColor = System.Drawing.SystemColors.Control;
            this.txtImage.Location = new System.Drawing.Point(12, 28);
            this.txtImage.Name = "txtImage";
            this.txtImage.ReadOnly = true;
            this.txtImage.Size = new System.Drawing.Size(213, 20);
            this.txtImage.TabIndex = 100;
            // 
            // btnSelectImage
            // 
            this.btnSelectImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectImage.Location = new System.Drawing.Point(231, 25);
            this.btnSelectImage.Name = "btnSelectImage";
            this.btnSelectImage.Size = new System.Drawing.Size(31, 23);
            this.btnSelectImage.TabIndex = 1;
            this.btnSelectImage.Text = "...";
            this.btnSelectImage.UseVisualStyleBackColor = true;
            this.btnSelectImage.Click += new System.EventHandler(this.BtnSelectImageClick);
            // 
            // lblPaletteHandling
            // 
            this.lblPaletteHandling.AutoSize = true;
            this.lblPaletteHandling.Location = new System.Drawing.Point(13, 166);
            this.lblPaletteHandling.Name = "lblPaletteHandling";
            this.lblPaletteHandling.Size = new System.Drawing.Size(86, 13);
            this.lblPaletteHandling.TabIndex = 40;
            this.lblPaletteHandling.Text = "Palette handling:";
            // 
            // rbtMatchPalette
            // 
            this.rbtMatchPalette.AutoSize = true;
            this.rbtMatchPalette.Enabled = false;
            this.rbtMatchPalette.Location = new System.Drawing.Point(121, 164);
            this.rbtMatchPalette.Name = "rbtMatchPalette";
            this.rbtMatchPalette.Size = new System.Drawing.Size(90, 17);
            this.rbtMatchPalette.TabIndex = 41;
            this.rbtMatchPalette.TabStop = true;
            this.rbtMatchPalette.Text = "Match palette";
            this.rbtMatchPalette.UseVisualStyleBackColor = true;
            // 
            // rbtKeepIndices
            // 
            this.rbtKeepIndices.AutoSize = true;
            this.rbtKeepIndices.Enabled = false;
            this.rbtKeepIndices.Location = new System.Drawing.Point(121, 187);
            this.rbtKeepIndices.Name = "rbtKeepIndices";
            this.rbtKeepIndices.Size = new System.Drawing.Size(86, 17);
            this.rbtKeepIndices.TabIndex = 42;
            this.rbtKeepIndices.TabStop = true;
            this.rbtKeepIndices.Text = "Keep indices";
            this.rbtKeepIndices.UseVisualStyleBackColor = true;
            // 
            // txtFrames
            // 
            this.txtFrames.Location = new System.Drawing.Point(12, 103);
            this.txtFrames.Multiline = true;
            this.txtFrames.Name = "txtFrames";
            this.txtFrames.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFrames.Size = new System.Drawing.Size(280, 55);
            this.txtFrames.TabIndex = 30;
            this.txtFrames.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxShortcuts);
            // 
            // btnClipboard
            // 
            this.btnClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClipboard.Image = global::EngieFileConverter.Properties.Resources.clipboard_image;
            this.btnClipboard.Location = new System.Drawing.Point(268, 25);
            this.btnClipboard.Name = "btnClipboard";
            this.btnClipboard.Size = new System.Drawing.Size(24, 23);
            this.btnClipboard.TabIndex = 2;
            this.btnClipboard.Text = "...";
            this.btnClipboard.UseVisualStyleBackColor = true;
            this.btnClipboard.Click += new System.EventHandler(this.btnClipboard_Click);
            // 
            // numCoordsY
            // 
            this.numCoordsY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numCoordsY.EnteredValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numCoordsY.Location = new System.Drawing.Point(232, 58);
            this.numCoordsY.Name = "numCoordsY";
            this.numCoordsY.SelectedText = "";
            this.numCoordsY.SelectionLength = 0;
            this.numCoordsY.SelectionStart = 0;
            this.numCoordsY.Size = new System.Drawing.Size(60, 20);
            this.numCoordsY.TabIndex = 14;
            // 
            // numCoordsX
            // 
            this.numCoordsX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numCoordsX.EnteredValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numCoordsX.Location = new System.Drawing.Point(137, 58);
            this.numCoordsX.Name = "numCoordsX";
            this.numCoordsX.SelectedText = "";
            this.numCoordsX.SelectionLength = 0;
            this.numCoordsX.SelectionStart = 0;
            this.numCoordsX.Size = new System.Drawing.Size(60, 20);
            this.numCoordsX.TabIndex = 12;
            // 
            // FrmPasteOnFrames
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(304, 261);
            this.Controls.Add(this.btnClipboard);
            this.Controls.Add(this.txtFrames);
            this.Controls.Add(this.rbtKeepIndices);
            this.Controls.Add(this.rbtMatchPalette);
            this.Controls.Add(this.lblPaletteHandling);
            this.Controls.Add(this.btnSelectImage);
            this.Controls.Add(this.txtImage);
            this.Controls.Add(this.lblImage);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblCoordinatesX);
            this.Controls.Add(this.lblCoordinatesY);
            this.Controls.Add(this.numCoordsY);
            this.Controls.Add(this.numCoordsX);
            this.Controls.Add(this.lblCoordinates);
            this.Controls.Add(this.lblFramesRange);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::EngieFileConverter.Properties.Resources.EngieIcon;
            this.Name = "FrmPasteOnFrames";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Paste on frames";
            ((System.ComponentModel.ISupportInitialize)(this.numCoordsY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCoordsX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblFramesRange;
        private System.Windows.Forms.Label lblCoordinatesY;
        private Nyerguds.Util.UI.EnhNumericUpDown numCoordsY;
        private Nyerguds.Util.UI.EnhNumericUpDown numCoordsX;
        private System.Windows.Forms.Label lblCoordinates;
        private System.Windows.Forms.Label lblCoordinatesX;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblImage;
        private System.Windows.Forms.TextBox txtImage;
        private System.Windows.Forms.Button btnSelectImage;
        private System.Windows.Forms.Label lblPaletteHandling;
        private System.Windows.Forms.RadioButton rbtMatchPalette;
        private System.Windows.Forms.RadioButton rbtKeepIndices;
        private System.Windows.Forms.TextBox txtFrames;
        private System.Windows.Forms.Button btnClipboard;
    }
}