using System;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class SaveOptionBoolean : SaveOptionControl
    {
        private Boolean m_Loading;

        public SaveOptionBoolean() : this(null, null) { }

        public SaveOptionBoolean(Option info, ListedControlController<Option> controller)
        {
            this.InitializeComponent();
            this.Init(info, controller);
        }

        public override void UpdateInfo(Option info)
        {
            this.Info = info;
            this.chkOption.Text = GeneralUtils.DoubleAmpersands(this.Info.UiString);
            this.chkOption.Checked = GeneralUtils.IsTrueValue(this.Info.Data);
        }

        public override void FocusValue()
        {
            this.chkOption.Select();
        }

        public override void SetEnabled(Boolean enabled)
        {
            try
            {
                m_Loading = true;
                this.Enabled = enabled;
                this.chkOption.Checked = enabled && GeneralUtils.IsTrueValue(this.Info.Data);
            }
            finally
            {
                m_Loading = false;
            }
        }

        private void chkOption_CheckedChanged(Object sender, EventArgs e)
        {
            if (m_Loading || this.Info == null)
                return;
            this.Info.Data = this.chkOption.Checked ? "1" : "0";
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }
    }
}
