namespace Nyerguds.Util.UI
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
            this.btnOk = new System.Windows.Forms.Button();
            this.palettePanel = new Nyerguds.Util.UI.PalettePanel();
            this.SuspendLayout();
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(216, 349);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(120, 23);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Cancel";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // btnOk
            //
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(20, 349);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(120, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            //
            // palettePanel
            //
            this.palettePanel.AutoSize = true;
            this.palettePanel.ColorSelectMode = Nyerguds.Util.UI.ColorSelMode.None;
            this.palettePanel.Location = new System.Drawing.Point(18, 12);
            this.palettePanel.Name = "palettePanel";
            this.palettePanel.Padding = new System.Windows.Forms.Padding(3);
            this.palettePanel.Palette = null;
            this.palettePanel.Remap = null;
            this.palettePanel.SelectedIndices = new int[0];
            this.palettePanel.Size = new System.Drawing.Size(322, 322);
            this.palettePanel.TabIndex = 0;
            this.palettePanel.TransItemBackColor = System.Drawing.Color.Empty;
            this.palettePanel.ColorLabelMouseDoubleClick += new Nyerguds.Util.UI.PaletteClickEventHandler(this.PalettePanel_LabelMouseDoubleClick);
            //
            // FrmPalette
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(356, 387);
            this.Controls.Add(this.palettePanel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::EngieFileConverter.Properties.Resources.EngieIcon;
            this.Name = "FrmPalette";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Color Palette";
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Button btnClose;
        protected System.Windows.Forms.Button btnOk;
        protected Nyerguds.Util.UI.PalettePanel palettePanel;

    }
}