using System;
using System.Drawing;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;

namespace EngieFileConverter.UI.SaveOptions
{
    public partial class SaveOptionChoices : SaveOptionControl
    {
        private Int32 initialWidthLbl;
        private Int32 initialWidthCmb;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;
        private Boolean m_Loading;

        public SaveOptionChoices() : this(null, null) { }

        public SaveOptionChoices(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = this.cmbChoices.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCmb = this.cmbChoices.Width;
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
                String[] options = this.Info.InitValue.Split(',');
                Char[] trim = " \t\r\n".ToCharArray();
                Int32 nrOfOpts = options.Length;
                for (Int32 i = 0; i < nrOfOpts; ++i)
                    options[i] = options[i].Trim(trim);
                this.cmbChoices.DataSource = options;
                this.SelectFromSaveData();
            }
            finally
            {
                m_Loading = false;
            }
        }

        private void SelectFromSaveData()
        {
            Int32 select;
            Int32.TryParse(this.Info.SaveData, out select);
            if (this.cmbChoices.Items.Count > select)
                this.cmbChoices.SelectedIndex = select;
        }

        public override void FocusValue()
        {
            this.cmbChoices.Select();
        }


        public override void DisableValue(Boolean enabled)
        {
            try
            {
                this.m_Loading = true;
                this.Enabled = enabled;
                if (enabled)
                    this.SelectFromSaveData();
                else
                    this.cmbChoices.SelectedItem = null;
            }
            finally
            {
                this.m_Loading = false;
            }
        }

        private void cmbChoices_SelectedIndexChanged(Object sender, EventArgs e)
        {
            // Update controller
            if (this.m_Loading || this.Info == null)
                return;
            this.Info.SaveData = this.cmbChoices.SelectedIndex.ToString();
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }
        
        private void SaveOptionChoices_Resize(Object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            Double scaleFactor = (Double)this.DisplayRectangle.Width / (Double) this.initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblDescription.Width = newWidthLbl;
            this.cmbChoices.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.cmbChoices.Location.Y);
            this.cmbChoices.Width = newWidthTxt;
        }
    }
}
