using System.ComponentModel;
using System.Windows.Forms;
using Nyerguds.Util.UI;

namespace Nyerguds.Util.UI.SaveOptions
{
    partial class SaveOptionPalette
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
            this.lblDescription = new System.Windows.Forms.Label();
            this.pnlColor = new System.Windows.Forms.Panel();
            this.lblColorVal = new System.Windows.Forms.Label();
            this.lblColor = new Nyerguds.Util.UI.ImageButtonCheckBox();
            this.pnlColor.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(6, 3);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(179, 30);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "DESCRIPTION";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlColor
            // 
            this.pnlColor.Controls.Add(this.lblColorVal);
            this.pnlColor.Controls.Add(this.lblColor);
            this.pnlColor.Location = new System.Drawing.Point(191, 0);
            this.pnlColor.Name = "pnlColor";
            this.pnlColor.Size = new System.Drawing.Size(179, 36);
            this.pnlColor.TabIndex = 127;
            // 
            // lblColorVal
            // 
            this.lblColorVal.AutoSize = true;
            this.lblColorVal.Location = new System.Drawing.Point(31, 12);
            this.lblColorVal.Name = "lblColorVal";
            this.lblColorVal.Size = new System.Drawing.Size(35, 13);
            this.lblColorVal.TabIndex = 128;
            this.lblColorVal.Text = "label1";
            // 
            // lblColor
            // 
            this.lblColor.BackColor = System.Drawing.Color.Fuchsia;
            this.lblColor.Checked = true;
            this.lblColor.DisabledBackColor = System.Drawing.Color.DarkGray;
            this.lblColor.Location = new System.Drawing.Point(5, 8);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new System.Drawing.Size(20, 20);
            this.lblColor.TabIndex = 127;
            this.lblColor.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblColor.Toggle = false;
            this.lblColor.TrueBackColor = System.Drawing.Color.Fuchsia;
            this.lblColor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LblColor_KeyPress);
            this.lblColor.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LblColor_MouseClick);
            // 
            // SaveOptionPalette
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlColor);
            this.Controls.Add(this.lblDescription);
            this.Name = "SaveOptionPalette";
            this.Size = new System.Drawing.Size(370, 36);
            this.Resize += new System.EventHandler(this.SaveOptionPalette_Resize);
            this.pnlColor.ResumeLayout(false);
            this.pnlColor.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Label lblDescription;
        private Panel pnlColor;
        private Label lblColorVal;
        private ImageButtonCheckBox lblColor;
    }
}
