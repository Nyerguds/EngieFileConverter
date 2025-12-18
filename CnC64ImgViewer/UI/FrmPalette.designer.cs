namespace CnC64ImgViewer.Ui
{
    partial class FrmPalette
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
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSavePalette = new System.Windows.Forms.Button();
            this.chkColorOption = new System.Windows.Forms.CheckBox();
            this.palettePanel = new CnC64ImgViewer.Ui.PalettePanel();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(216, 398);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(120, 23);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close window";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSavePalette
            // 
            this.btnSavePalette.Location = new System.Drawing.Point(20, 398);
            this.btnSavePalette.Margin = new System.Windows.Forms.Padding(0);
            this.btnSavePalette.Name = "btnSavePalette";
            this.btnSavePalette.Size = new System.Drawing.Size(120, 23);
            this.btnSavePalette.TabIndex = 2;
            this.btnSavePalette.Text = "Save Palette";
            this.btnSavePalette.UseVisualStyleBackColor = true;
            this.btnSavePalette.Click += new System.EventHandler(this.btnSavePalette_Click);
            // 
            // chkColorOption
            // 
            this.chkColorOption.AutoSize = true;
            this.chkColorOption.Location = new System.Drawing.Point(20, 362);
            this.chkColorOption.Name = "chkColorOption";
            this.chkColorOption.Size = new System.Drawing.Size(230, 17);
            this.chkColorOption.TabIndex = 1;
            this.chkColorOption.Text = "Show only colors used in the filtered palette";
            this.chkColorOption.UseVisualStyleBackColor = true;
            this.chkColorOption.CheckedChanged += new System.EventHandler(this.chkColorOption_CheckedChanged);
            // 
            // palettePanel
            // 
            this.palettePanel.Border = new System.Windows.Forms.Padding(20);
            this.palettePanel.EmptyIndicatorBackColor = System.Drawing.Color.Black;
            this.palettePanel.EmptyIndicatorChar = 'X';
            this.palettePanel.EmptyIndicatorCharColor = System.Drawing.Color.Red;
            this.palettePanel.LabelSize = new System.Drawing.Size(16, 16);
            this.palettePanel.Location = new System.Drawing.Point(0, 0);
            this.palettePanel.Multiselect = false;
            this.palettePanel.Name = "palettePanel";
            this.palettePanel.PadBetween = new System.Drawing.Point(4, 4);
            this.palettePanel.Palette = null;
            this.palettePanel.Remap = null;
            this.palettePanel.Selectable = false;
            this.palettePanel.SelectedIndices = new int[0];
            this.palettePanel.ShowColorToolTips = true;
            this.palettePanel.ShowRemappedPalette = true;
            this.palettePanel.Size = new System.Drawing.Size(356, 356);
            this.palettePanel.TabIndex = 0;
            this.palettePanel.LabelMouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.palettePanel_LabelMouseDoubleClick);
            // 
            // FrmPalette
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(356, 441);
            this.Controls.Add(this.palettePanel);
            this.Controls.Add(this.chkColorOption);
            this.Controls.Add(this.btnSavePalette);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::CnC64ImgViewer.Properties.Resources.cnc64logo;
            this.Name = "FrmPalette";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Color Palette";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        protected System.Windows.Forms.Button btnClose;
        protected System.Windows.Forms.Button btnSavePalette;
        protected System.Windows.Forms.CheckBox chkColorOption;
        protected Ui.PalettePanel palettePanel;

    }
}