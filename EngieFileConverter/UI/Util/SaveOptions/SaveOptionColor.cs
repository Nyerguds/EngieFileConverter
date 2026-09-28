using System;
using System.Drawing;
using Nyerguds.Util.Ui;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class SaveOptionColor : SaveOptionControl
    {
        private int initialWidthLbl;
        private int initialWidthCmb;
        private int initialWidthToScale;
        private int m_PadLeft;
        private int m_PadMiddle;
        private int m_PadRight;
        private bool m_Loading;

        public SaveOptionColor() : this(null, null) { }

        public SaveOptionColor(Option info, ListedControlController<Option> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            int initialPosTxt = this.pnlColorControls.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCmb = this.pnlColorControls.Width;
            int initialWidthFrm = this.DisplayRectangle.Width;
            this.m_PadLeft = this.lblDescription.Location.X;
            this.m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthCmb;
            this.m_PadMiddle = initialPosTxt - this.initialWidthLbl - this.m_PadLeft;
            this.initialWidthToScale = initialWidthFrm - this.m_PadLeft - this.m_PadRight - this.m_PadMiddle;
        }

        public override void UpdateInfo(Option info)
        {
            try
            {
                m_Loading = true;
                this.Info = info;
                this.lblDescription.Text = GeneralUtils.DoubleAmpersands(this.Info.UiString);
                string initVal = String.IsNullOrEmpty(this.Info.InitValue) ? String.Empty : this.Info.InitValue.Trim();
                char transOptions = initVal.Length == 0 ? '\0' : this.Info.InitValue.Trim()[0];
                chkTransparent.Enabled = false;
                lblAlpha.Enabled = false;
                numAlpha.Enabled = false;
                switch (transOptions)
                {
                    case 'A':
                        lblAlpha.Enabled = true;
                        numAlpha.Enabled = true;
                        break;
                    case 'T':
                        chkTransparent.Enabled = true;
                        break;
                }
                this.SelectFromSaveData();
            }
            finally
            {
                m_Loading = false;
            }
        }

        private void SelectFromSaveData()
        {

            string saveData = this.Info.Data;
            Color col = ColorUtils.ColorFromHexString(saveData);
            lblColor.TrueBackColor = Color.FromArgb(0xFF, col);
            if (numAlpha.Enabled)
                numAlpha.Value = col.A;
            else if (chkTransparent.Enabled && col.A < 128)
                chkTransparent.Checked = true;
        }

        public override void FocusValue()
        {
            this.lblColor.Select();
        }

        private void SaveOptionChoices_Resize(object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            double scaleFactor = (double)this.DisplayRectangle.Width / this.initialWidthToScale;
            int newWidthLbl = (int)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            int newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblDescription.Width = newWidthLbl;
            this.pnlColorControls.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.pnlColorControls.Location.Y);
            this.pnlColorControls.Width = newWidthTxt;
        }

        private void LblColorKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.LblColorClick(sender, e);
        }

        private void LblColorClick(object sender, EventArgs e)
        {
            ImageButtonCheckBox lbl = sender as ImageButtonCheckBox;
            if (lbl == null) return;
            using (ColorDialog cdl = new ColorDialog())
            {
                cdl.Color = lbl.TrueBackColor;
                cdl.FullOpen = true;
                DialogResult res = cdl.ShowDialog(this);
                if (res != DialogResult.OK && res != DialogResult.Yes)
                    return;
                lbl.TrueBackColor = cdl.Color;
            }
            if (this.chkTransparent.Enabled)
                this.chkTransparent.Checked = false;
            else if (this.numAlpha.Enabled && this.numAlpha.Value == 0)
                this.numAlpha.Value = 0xFF;
            this.UpdateController();
        }

        private void chkTransparent_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_Loading)
                UpdateController();
        }

        private void numAlpha_ValueChanged(object sender, EventArgs e)
        {
            if (!m_Loading)
                UpdateController();
        }

        private void UpdateController()
        {
            // Update controller
            if (this.m_Loading || this.Info == null)
                return;
            Color col = this.lblColor.TrueBackColor;
            if (chkTransparent.Enabled)
                col = Color.FromArgb(chkTransparent.Checked ? 0x00 : 0xFF, col);
            else if (numAlpha.Enabled)
                col = Color.FromArgb((int)numAlpha.Value, col);
            this.Info.Data = ColorUtils.HexStringFromColor(col, true);
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }


    }
}
