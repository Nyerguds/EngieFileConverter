using System.ComponentModel;
using System.Windows.Forms;

namespace EngieFileConverter.UI.SaveOptions
{
    partial class SaveOptionString
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
            this.txtValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(6, 3);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(179, 50);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "DESCRIPTION";
            // 
            // txtValue
            // 
            this.txtValue.Location = new System.Drawing.Point(188, 3);
            this.txtValue.Multiline = true;
            this.txtValue.Name = "txtValue";
            this.txtValue.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtValue.Size = new System.Drawing.Size(179, 50);
            this.txtValue.TabIndex = 3;
            this.txtValue.TextChanged += new System.EventHandler(this.TextBoxCheckLines);
            this.txtValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxSelectAll);
            this.txtValue.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxCheckKeyPress);
            // 
            // SaveOptionString
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblDescription);
            this.Name = "SaveOptionString";
            this.Size = new System.Drawing.Size(370, 56);
            this.Resize += new System.EventHandler(this.SaveOptionString_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblDescription;
        private TextBox txtValue;
    }
}
