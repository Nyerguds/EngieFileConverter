using System.ComponentModel;
using System.Windows.Forms;

namespace EngieFileConverter.UI.SaveOptions
{
    partial class SaveOptionNumber
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.lblName = new System.Windows.Forms.Label();
            this.numValue = new Nyerguds.Util.UI.EnhNumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numValue)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblName.Location = new System.Drawing.Point(6, 3);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(259, 30);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "OPTION";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblName.Resize += new System.EventHandler(this.lblName_Resize);
            // 
            // numValue
            // 
            this.numValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.numValue.EnteredValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numValue.Location = new System.Drawing.Point(271, 10);
            this.numValue.Maximum = new decimal(new int[] {
            -1,
            -1,
            -1,
            0});
            this.numValue.Minimum = new decimal(new int[] {
            -1,
            -1,
            -1,
            -2147483648});
            this.numValue.Name = "numValue";
            this.numValue.SelectedText = "";
            this.numValue.SelectionLength = 0;
            this.numValue.SelectionStart = 0;
            this.numValue.Size = new System.Drawing.Size(96, 20);
            this.numValue.TabIndex = 2;
            this.numValue.ValueChanged += new System.EventHandler(this.numValue_ValueChanged);
            // 
            // SaveOptionNumber
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numValue);
            this.Controls.Add(this.lblName);
            this.Name = "SaveOptionNumber";
            this.Size = new System.Drawing.Size(370, 36);
            ((System.ComponentModel.ISupportInitialize)(this.numValue)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Label lblName;
        private Nyerguds.Util.UI.EnhNumericUpDown numValue;
    }
}
