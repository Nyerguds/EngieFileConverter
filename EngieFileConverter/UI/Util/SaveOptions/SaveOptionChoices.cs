using System;
using System.Drawing;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class SaveOptionChoices : SaveOptionControl
    {
        private int initialWidthLbl;
        private int initialWidthCmb;
        private int initialWidthToScale;
        private int m_PadLeft;
        private int m_PadMiddle;
        private int m_PadRight;
        private bool m_Loading;

        public SaveOptionChoices() : this(null, null) { }

        public SaveOptionChoices(Option info, ListedControlController<Option> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            int initialPosTxt = this.cmbChoices.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCmb = this.cmbChoices.Width;
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
                string[] options = this.Info.InitValue.Split(',');
                char[] trim = " \t\r\n".ToCharArray();
                int nrOfOpts = options.Length;
                for (int i = 0; i < nrOfOpts; ++i)
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
            int select;
            Int32.TryParse(this.Info.Data, out select);
            if (this.cmbChoices.Items.Count > select)
                this.cmbChoices.SelectedIndex = select;
        }

        public override void FocusValue()
        {
            this.cmbChoices.Select();
        }


        public override void SetEnabled(bool enabled)
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

        private void cmbChoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update controller
            if (this.m_Loading || this.Info == null)
                return;
            this.Info.Data = this.cmbChoices.SelectedIndex.ToString();
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }

        private void SaveOptionChoices_Resize(object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            double scaleFactor = (double)this.DisplayRectangle.Width / (double) this.initialWidthToScale;
            int newWidthLbl = (int)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            int newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblDescription.Width = newWidthLbl;
            this.cmbChoices.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.cmbChoices.Location.Y);
            this.cmbChoices.Width = newWidthTxt;
        }
    }
}
