using System.ComponentModel;
using System.Windows.Forms;
using Nyerguds.Util.UI;

namespace EngieFileConverter.UI.SaveOptions
{
    partial class SaveOptionColor
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
            this.pnlColorControls = new System.Windows.Forms.Panel();
            this.lblAlpha = new System.Windows.Forms.Label();
            this.numAlpha = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.chkTransparent = new System.Windows.Forms.CheckBox();
            this.lblColor = new Nyerguds.Util.UI.ImageButtonCheckBox();
            this.pnlColorControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAlpha)).BeginInit();
            this.SuspendLayout();
            // 
            // lblDescription
            // 
            this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lblDescription.Location = new System.Drawing.Point(6, 3);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(179, 50);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "DESCRIPTION";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlColorControls
            // 
            this.pnlColorControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlColorControls.Controls.Add(this.lblAlpha);
            this.pnlColorControls.Controls.Add(this.numAlpha);
            this.pnlColorControls.Controls.Add(this.chkTransparent);
            this.pnlColorControls.Controls.Add(this.lblColor);
            this.pnlColorControls.Location = new System.Drawing.Point(188, 0);
            this.pnlColorControls.Margin = new System.Windows.Forms.Padding(0);
            this.pnlColorControls.Name = "pnlColorControls";
            this.pnlColorControls.Size = new System.Drawing.Size(182, 53);
            this.pnlColorControls.TabIndex = 4;
            // 
            // lblAlpha
            // 
            this.lblAlpha.AutoSize = true;
            this.lblAlpha.Location = new System.Drawing.Point(43, 30);
            this.lblAlpha.Name = "lblAlpha";
            this.lblAlpha.Size = new System.Drawing.Size(37, 13);
            this.lblAlpha.TabIndex = 128;
            this.lblAlpha.Text = "Alpha:";
            // 
            // numAlpha
            // 
            this.numAlpha.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numAlpha.EnteredValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numAlpha.Location = new System.Drawing.Point(90, 28);
            this.numAlpha.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numAlpha.Name = "numAlpha";
            this.numAlpha.SelectedText = "";
            this.numAlpha.SelectionLength = 0;
            this.numAlpha.SelectionStart = 0;
            this.numAlpha.Size = new System.Drawing.Size(89, 20);
            this.numAlpha.TabIndex = 127;
            this.numAlpha.ValueChanged += new System.EventHandler(this.numAlpha_ValueChanged);
            // 
            // chkTransparent
            // 
            this.chkTransparent.AutoSize = true;
            this.chkTransparent.Location = new System.Drawing.Point(46, 5);
            this.chkTransparent.Name = "chkTransparent";
            this.chkTransparent.Size = new System.Drawing.Size(83, 17);
            this.chkTransparent.TabIndex = 125;
            this.chkTransparent.Text = "Transparent";
            this.chkTransparent.UseVisualStyleBackColor = true;
            this.chkTransparent.CheckedChanged += new System.EventHandler(this.chkTransparent_CheckedChanged);
            // 
            // lblColor
            // 
            this.lblColor.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblColor.BackColor = System.Drawing.Color.Fuchsia;
            this.lblColor.Checked = true;
            this.lblColor.DisabledBackColor = System.Drawing.Color.DarkGray;
            this.lblColor.Location = new System.Drawing.Point(3, 16);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new System.Drawing.Size(20, 20);
            this.lblColor.TabIndex = 124;
            this.lblColor.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblColor.Toggle = false;
            this.lblColor.TrueBackColor = System.Drawing.Color.Fuchsia;
            this.lblColor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LblColorKeyPress);
            this.lblColor.Click += new System.EventHandler(this.LblColorClick);
            // 
            // SaveOptionColor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlColorControls);
            this.Controls.Add(this.lblDescription);
            this.Name = "SaveOptionColor";
            this.Size = new System.Drawing.Size(370, 53);
            this.Resize += new System.EventHandler(this.SaveOptionChoices_Resize);
            this.pnlColorControls.ResumeLayout(false);
            this.pnlColorControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAlpha)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Label lblDescription;
        private Panel pnlColorControls;
        private CheckBox chkTransparent;
        private Label lblAlpha;
        private EnhNumericUpDown numAlpha;
        private Nyerguds.Util.UI.ImageButtonCheckBox lblColor;
    }
}
