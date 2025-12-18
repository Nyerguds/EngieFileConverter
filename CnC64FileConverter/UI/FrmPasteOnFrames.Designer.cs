namespace CnC64FileConverter.UI
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
            this.numCoordsY = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.numCoordsX = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.lblCoordinatesX = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtImage = new System.Windows.Forms.TextBox();
            this.btnSelectImage = new System.Windows.Forms.Button();
            this.lblPaletteHandling = new System.Windows.Forms.Label();
            this.rbtMatchPalette = new System.Windows.Forms.RadioButton();
            this.rbtKeepIndices = new System.Windows.Forms.RadioButton();
            this.txtFrames = new System.Windows.Forms.TextBox();
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
            this.lblFramesRange.TabIndex = 1;
            this.lblFramesRange.Text = "Frames: (comma-separated. Allows ranges like \"5-10\")";
            // 
            // lblCoordinatesY
            // 
            this.lblCoordinatesY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCoordinatesY.AutoSize = true;
            this.lblCoordinatesY.Location = new System.Drawing.Point(209, 60);
            this.lblCoordinatesY.Name = "lblCoordinatesY";
            this.lblCoordinatesY.Size = new System.Drawing.Size(17, 13);
            this.lblCoordinatesY.TabIndex = 8;
            this.lblCoordinatesY.Text = "Y:";
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
            this.numCoordsY.Size = new System.Drawing.Size(60, 20);
            this.numCoordsY.TabIndex = 9;
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
            this.numCoordsX.Size = new System.Drawing.Size(60, 20);
            this.numCoordsX.TabIndex = 7;
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.AutoSize = true;
            this.lblCoordinates.Location = new System.Drawing.Point(13, 60);
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(95, 13);
            this.lblCoordinates.TabIndex = 5;
            this.lblCoordinates.Text = "Paste coordinates:";
            // 
            // lblCoordinatesX
            // 
            this.lblCoordinatesX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCoordinatesX.AutoSize = true;
            this.lblCoordinatesX.Location = new System.Drawing.Point(114, 60);
            this.lblCoordinatesX.Name = "lblCoordinatesX";
            this.lblCoordinatesX.Size = new System.Drawing.Size(17, 13);
            this.lblCoordinatesX.TabIndex = 6;
            this.lblCoordinatesX.Text = "X:";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(136, 226);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 20;
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
            this.btnCancel.TabIndex = 21;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Image:";
            // 
            // txtImage
            // 
            this.txtImage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtImage.BackColor = System.Drawing.SystemColors.Control;
            this.txtImage.Location = new System.Drawing.Point(12, 28);
            this.txtImage.Name = "txtImage";
            this.txtImage.ReadOnly = true;
            this.txtImage.Size = new System.Drawing.Size(243, 20);
            this.txtImage.TabIndex = 11;
            // 
            // btnSelectImage
            // 
            this.btnSelectImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectImage.Location = new System.Drawing.Point(261, 26);
            this.btnSelectImage.Name = "btnSelectImage";
            this.btnSelectImage.Size = new System.Drawing.Size(31, 23);
            this.btnSelectImage.TabIndex = 12;
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
            this.lblPaletteHandling.TabIndex = 13;
            this.lblPaletteHandling.Text = "Palette handling:";
            // 
            // rbtMatchPalette
            // 
            this.rbtMatchPalette.AutoSize = true;
            this.rbtMatchPalette.Enabled = false;
            this.rbtMatchPalette.Location = new System.Drawing.Point(121, 164);
            this.rbtMatchPalette.Name = "rbtMatchPalette";
            this.rbtMatchPalette.Size = new System.Drawing.Size(90, 17);
            this.rbtMatchPalette.TabIndex = 14;
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
            this.rbtKeepIndices.TabIndex = 15;
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
            this.txtFrames.TabIndex = 22;
            // 
            // FrmPasteOnFrames
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(304, 261);
            this.Controls.Add(this.txtFrames);
            this.Controls.Add(this.rbtKeepIndices);
            this.Controls.Add(this.rbtMatchPalette);
            this.Controls.Add(this.lblPaletteHandling);
            this.Controls.Add(this.btnSelectImage);
            this.Controls.Add(this.txtImage);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblCoordinatesX);
            this.Controls.Add(this.lblCoordinatesY);
            this.Controls.Add(this.numCoordsY);
            this.Controls.Add(this.numCoordsX);
            this.Controls.Add(this.lblCoordinates);
            this.Controls.Add(this.lblFramesRange);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::CnC64FileConverter.Properties.Resources.cnc64logo;
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtImage;
        private System.Windows.Forms.Button btnSelectImage;
        private System.Windows.Forms.Label lblPaletteHandling;
        private System.Windows.Forms.RadioButton rbtMatchPalette;
        private System.Windows.Forms.RadioButton rbtKeepIndices;
        private System.Windows.Forms.TextBox txtFrames;
    }
}