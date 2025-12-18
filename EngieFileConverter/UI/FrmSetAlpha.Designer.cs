namespace CnC64FileConverter.UI
{
    partial class FrmSetAlpha
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
            this.trbAlpha = new System.Windows.Forms.TrackBar();
            this.numAlpha = new Nyerguds.Util.UI.EnhNumericUpDown();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trbAlpha)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAlpha)).BeginInit();
            this.SuspendLayout();
            // 
            // trbAlpha
            // 
            this.trbAlpha.LargeChange = 10;
            this.trbAlpha.Location = new System.Drawing.Point(12, 12);
            this.trbAlpha.Maximum = 255;
            this.trbAlpha.Name = "trbAlpha";
            this.trbAlpha.Size = new System.Drawing.Size(268, 45);
            this.trbAlpha.TabIndex = 0;
            this.trbAlpha.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trbAlpha.ValueChanged += new System.EventHandler(this.TrbAlpha_ValueChanged);
            // 
            // numAlpha
            // 
            this.numAlpha.EnteredValue = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numAlpha.Location = new System.Drawing.Point(286, 12);
            this.numAlpha.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numAlpha.Name = "numAlpha";
            this.numAlpha.Size = new System.Drawing.Size(75, 20);
            this.numAlpha.TabIndex = 1;
            this.numAlpha.ValueEntered += new System.EventHandler<Nyerguds.Util.UI.ValueEnteredEventArgs>(this.NumAlpha_ValueEntered);
            this.numAlpha.ValueChanged += new System.EventHandler(this.NumAlpha_ValueChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(280, 62);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(199, 62);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // FrmSetAlpha
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(367, 97);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.numAlpha);
            this.Controls.Add(this.trbAlpha);
            this.Icon = global::CnC64FileConverter.Properties.Resources.cnc64logo;
            this.Name = "FrmSetAlpha";
            this.Text = "Set Alpha";
            ((System.ComponentModel.ISupportInitialize)(this.trbAlpha)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAlpha)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trbAlpha;
        private Nyerguds.Util.UI.EnhNumericUpDown numAlpha;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}