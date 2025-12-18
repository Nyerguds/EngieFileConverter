namespace Nyerguds.Util.UI
{
    partial class FrmManagePalettes
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
            this.cmbPalettes = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.lbSubPalettes = new System.Windows.Forms.ListBox();
            this.btnRename = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.palSelectedSubPal = new Nyerguds.Util.UI.PalettePanel();
            this.palReplaceBy = new Nyerguds.Util.UI.PalettePanel();
            this.lblReplaceBy = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblSelectedSubPal = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmbPalettes
            // 
            this.cmbPalettes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalettes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalettes.FormattingEnabled = true;
            this.cmbPalettes.Location = new System.Drawing.Point(74, 6);
            this.cmbPalettes.Name = "cmbPalettes";
            this.cmbPalettes.Size = new System.Drawing.Size(175, 21);
            this.cmbPalettes.TabIndex = 0;
            this.cmbPalettes.SelectedIndexChanged += new System.EventHandler(this.CmbPalettes_SelectedIndexChanged);
            // 
            // lbSubPalettes
            // 
            this.lbSubPalettes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbSubPalettes.FormattingEnabled = true;
            this.lbSubPalettes.Items.AddRange(new object[] {
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15"});
            this.lbSubPalettes.Location = new System.Drawing.Point(12, 55);
            this.lbSubPalettes.Name = "lbSubPalettes";
            this.lbSubPalettes.Size = new System.Drawing.Size(237, 212);
            this.lbSubPalettes.TabIndex = 1;
            this.lbSubPalettes.SelectedIndexChanged += new System.EventHandler(this.LbSubPalettes_SelectedIndexChanged);
            // 
            // btnRename
            // 
            this.btnRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRename.Location = new System.Drawing.Point(12, 277);
            this.btnRename.Name = "btnRename";
            this.btnRename.Size = new System.Drawing.Size(75, 23);
            this.btnRename.TabIndex = 2;
            this.btnRename.Text = "Rename";
            this.btnRename.UseVisualStyleBackColor = true;
            this.btnRename.Click += new System.EventHandler(this.BtnRename_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(174, 277);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 23);
            this.btnRemove.TabIndex = 4;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.BtnRemove_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnAdd.Location = new System.Drawing.Point(93, 277);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(174, 348);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(174, 377);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // palSelectedSubPal
            // 
            this.palSelectedSubPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.palSelectedSubPal.AutoSize = true;
            this.palSelectedSubPal.ColorSelectMode = Nyerguds.Util.UI.ColorSelMode.None;
            this.palSelectedSubPal.ColorTableWidth = 4;
            this.palSelectedSubPal.Location = new System.Drawing.Point(12, 326);
            this.palSelectedSubPal.MaxColors = 16;
            this.palSelectedSubPal.Name = "palSelectedSubPal";
            this.palSelectedSubPal.PadBetween = new System.Drawing.Point(2, 2);
            this.palSelectedSubPal.Palette = null;
            this.palSelectedSubPal.Remap = null;
            this.palSelectedSubPal.SelectedIndices = new int[0];
            this.palSelectedSubPal.Size = new System.Drawing.Size(74, 74);
            this.palSelectedSubPal.TabIndex = 5;
            this.palSelectedSubPal.TransItemBackColor = System.Drawing.Color.Empty;
            // 
            // palReplaceBy
            // 
            this.palReplaceBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.palReplaceBy.AutoSize = true;
            this.palReplaceBy.ColorSelectMode = Nyerguds.Util.UI.ColorSelMode.None;
            this.palReplaceBy.ColorTableWidth = 4;
            this.palReplaceBy.Location = new System.Drawing.Point(93, 326);
            this.palReplaceBy.MaxColors = 16;
            this.palReplaceBy.Name = "palReplaceBy";
            this.palReplaceBy.PadBetween = new System.Drawing.Point(2, 2);
            this.palReplaceBy.Palette = null;
            this.palReplaceBy.Remap = null;
            this.palReplaceBy.SelectedIndices = new int[0];
            this.palReplaceBy.Size = new System.Drawing.Size(74, 74);
            this.palReplaceBy.TabIndex = 6;
            this.palReplaceBy.TransItemBackColor = System.Drawing.Color.Empty;
            // 
            // lblReplaceBy
            // 
            this.lblReplaceBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblReplaceBy.AutoSize = true;
            this.lblReplaceBy.Location = new System.Drawing.Point(95, 310);
            this.lblReplaceBy.Name = "lblReplaceBy";
            this.lblReplaceBy.Size = new System.Drawing.Size(64, 13);
            this.lblReplaceBy.TabIndex = 9;
            this.lblReplaceBy.Text = "Replace by:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Palette file:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 36);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "Palette entry:";
            // 
            // lblSelectedSubPal
            // 
            this.lblSelectedSubPal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSelectedSubPal.AutoSize = true;
            this.lblSelectedSubPal.Location = new System.Drawing.Point(13, 310);
            this.lblSelectedSubPal.Name = "lblSelectedSubPal";
            this.lblSelectedSubPal.Size = new System.Drawing.Size(78, 13);
            this.lblSelectedSubPal.TabIndex = 12;
            this.lblSelectedSubPal.Text = "Selected entry:";
            // 
            // FrmManagePalettes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(259, 414);
            this.Controls.Add(this.lblSelectedSubPal);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblReplaceBy);
            this.Controls.Add(this.palSelectedSubPal);
            this.Controls.Add(this.palReplaceBy);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnRename);
            this.Controls.Add(this.lbSubPalettes);
            this.Controls.Add(this.cmbPalettes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FrmManagePalettes";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Save palette";
            this.Load += new System.EventHandler(this.FrmManagePalettes_Load);
            this.Shown += new System.EventHandler(this.FrmManagePalettes_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbPalettes;
        private System.Windows.Forms.ListBox lbSubPalettes;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private Nyerguds.Util.UI.PalettePanel palSelectedSubPal;
        private Nyerguds.Util.UI.PalettePanel palReplaceBy;
        private System.Windows.Forms.Label lblReplaceBy;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblSelectedSubPal;
    }
}