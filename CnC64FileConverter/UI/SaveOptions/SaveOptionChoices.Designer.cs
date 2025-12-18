namespace CnC64FileConverter.UI.SaveOptions
{
    partial class SaveOptionChoices
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblDescription = new System.Windows.Forms.Label();
            this.cmbChoices = new Nyerguds.Util.UI.ComboBoxSmartWidth();
            this.SuspendLayout();
            // 
            // lblDescription
            // 
            this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lblDescription.Location = new System.Drawing.Point(6, 3);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(179, 30);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "DESCRIPTION";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbChoices
            // 
            this.cmbChoices.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbChoices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbChoices.Location = new System.Drawing.Point(188, 8);
            this.cmbChoices.Name = "cmbChoices";
            this.cmbChoices.Size = new System.Drawing.Size(179, 21);
            this.cmbChoices.TabIndex = 3;
            this.cmbChoices.SelectedIndexChanged += new System.EventHandler(this.cmbChoices_SelectedIndexChanged);
            // 
            // SaveOptionChoices
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbChoices);
            this.Controls.Add(this.lblDescription);
            this.Name = "SaveOptionChoices";
            this.Size = new System.Drawing.Size(370, 36);
            this.Resize += new System.EventHandler(this.SaveOptionChoices_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblDescription;
        private Nyerguds.Util.UI.ComboBoxSmartWidth cmbChoices;
    }
}
