using System;
using System.Drawing;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;
using Nyerguds.Util.UI;

namespace EngieFileConverter.UI.SaveOptions
{
    public partial class SaveOptionColor : SaveOptionControl
    {
        private Int32 initialWidthLbl;
        private Int32 initialWidthCmb;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;
        private Boolean m_Loading;

        public SaveOptionColor() : this(null, null) { }

        public SaveOptionColor(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = this.pnlColorControls.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCmb = this.pnlColorControls.Width;
            Int32 initialWidthFrm = this.DisplayRectangle.Width;
            this.m_PadLeft = this.lblDescription.Location.X;
            this.m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthCmb;
            this.m_PadMiddle = initialPosTxt - this.initialWidthLbl - this.m_PadLeft;
            this.initialWidthToScale = initialWidthFrm - this.m_PadLeft - this.m_PadRight - this.m_PadMiddle;
        }

        public override void UpdateInfo(SaveOption info)
        {
            try
            {
                m_Loading = true;
                this.Info = info;
                this.lblDescription.Text = GeneralUtils.DoubleFirstAmpersand(this.Info.UiString);
                String initVal = String.IsNullOrEmpty(this.Info.InitValue) ? String.Empty : this.Info.InitValue.Trim();
                Char transOptions = initVal.Length == 0 ? '\0' : this.Info.InitValue.Trim()[0];
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

            String saveData = this.Info.SaveData;
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
                
        private void SaveOptionChoices_Resize(Object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            Double scaleFactor = (Double)this.DisplayRectangle.Width / (Double) this.initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblDescription.Width = newWidthLbl;
            this.pnlColorControls.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.pnlColorControls.Location.Y);
            this.pnlColorControls.Width = newWidthTxt;
        }

        private void LblColorKeyPress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.LblColorClick(sender, e);
        }

        private void LblColorClick(Object sender, EventArgs e)
        {
            ImageButtonCheckBox lbl = sender as ImageButtonCheckBox;
            if (lbl == null) return;
            ColorDialog cdl = new ColorDialog();
            cdl.Color = lbl.TrueBackColor;
            cdl.FullOpen = true;
            DialogResult res = cdl.ShowDialog(this);
            if (res != DialogResult.OK && res != DialogResult.Yes)
                return;
            lbl.TrueBackColor = cdl.Color;
            if (this.chkTransparent.Enabled)
                this.chkTransparent.Checked = false;
            else if (this.numAlpha.Enabled && this.numAlpha.Value == 0)
                this.numAlpha.Value = 0xFF;
            this.UpdateController();
        }

        private void chkTransparent_CheckedChanged(Object sender, EventArgs e)
        {
            if (!m_Loading)
                UpdateController();
        }

        private void numAlpha_ValueChanged(Object sender, EventArgs e)
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
                col = Color.FromArgb((Int32)numAlpha.Value, col);
            this.Info.SaveData = ColorUtils.HexStringFromColor(col, true);
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }


    }
}
