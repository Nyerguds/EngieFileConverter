using System.ComponentModel;
using System.Windows.Forms;

namespace EngieFileConverter.UI.SaveOptions
{
    partial class SaveOptionBoolean
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
            this.chkOption = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // chkOption
            // 
            this.chkOption.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkOption.Location = new System.Drawing.Point(6, 3);
            this.chkOption.Name = "chkOption";
            this.chkOption.Size = new System.Drawing.Size(370, 30);
            this.chkOption.TabIndex = 2;
            this.chkOption.Text = "OPTION";
            this.chkOption.UseVisualStyleBackColor = true;
            this.chkOption.CheckedChanged += new System.EventHandler(this.chkOption_CheckedChanged);
            // 
            // SaveOptionBoolean
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkOption);
            this.Name = "SaveOptionBoolean";
            this.Size = new System.Drawing.Size(370, 36);
            this.ResumeLayout(false);

        }

        #endregion

        private CheckBox chkOption;
    }
}
