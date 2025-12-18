using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace Nyerguds.Util.UI
{
	/// <summary>
	/// The InputBox class is used to show a prompt in a dialog box using the static method Show().
	/// </summary>
	/// <remarks>
	/// Copyright © 2003 Reflection IT
	/// 
	/// This software is provided 'as-is', without any express or implied warranty.
	/// In no event will the authors be held liable for any damages arising from the
	/// use of this software.
	/// 
	/// Permission is granted to anyone to use this software for any purpose,
	/// including commercial applications, subject to the following restrictions:
	/// 
	/// 1. The origin of this software must not be misrepresented; you must not claim
	/// that you wrote the original software. 
	/// 
	/// 2. No substantial portion of the source code of this library may be redistributed
	/// without the express written permission of the copyright holders, where
	/// "substantial" is defined as enough code to be recognizably from this library. 
	/// 
	/// </remarks>
	partial class InputBox
    {

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.textBoxText = new System.Windows.Forms.TextBox();
            this.labelPrompt = new System.Windows.Forms.Label();
            this.errorProviderText = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderText)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(288, 72);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.CausesValidation = false;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(376, 72);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // textBoxText
            // 
            this.textBoxText.Location = new System.Drawing.Point(16, 32);
            this.textBoxText.Name = "textBoxText";
            this.textBoxText.Size = new System.Drawing.Size(416, 20);
            this.textBoxText.TabIndex = 1;
            this.textBoxText.TextChanged += new System.EventHandler(this.textBoxText_TextChanged);
            // 
            // labelPrompt
            // 
            this.labelPrompt.AutoSize = true;
            this.labelPrompt.Location = new System.Drawing.Point(15, 15);
            this.labelPrompt.Name = "labelPrompt";
            this.labelPrompt.Size = new System.Drawing.Size(39, 13);
            this.labelPrompt.TabIndex = 0;
            this.labelPrompt.Text = "prompt";
            // 
            // errorProviderText
            // 
            this.errorProviderText.ContainerControl = this;
            this.errorProviderText.DataMember = "";
            // 
            // InputBox
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(464, 104);
            this.Controls.Add(this.labelPrompt);
            this.Controls.Add(this.textBoxText);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputBox";
            this.Text = "Title";
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderText)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
		#endregion

		protected System.Windows.Forms.Button buttonOK;
		protected System.Windows.Forms.Button buttonCancel;
		protected System.Windows.Forms.Label labelPrompt;
		protected System.Windows.Forms.TextBox textBoxText;
		protected System.Windows.Forms.ErrorProvider errorProviderText;
        private IContainer components;
	}
}
