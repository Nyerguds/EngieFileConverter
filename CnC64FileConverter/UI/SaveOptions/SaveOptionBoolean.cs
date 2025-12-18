using System;
using System.Linq;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI.SaveOptions
{
    public partial class SaveOptionBoolean : SaveOptionControl
    {

        public SaveOptionBoolean() : this(null, null) { }

        public SaveOptionBoolean(SaveOption info, ListedControlController<SaveOption> controller)
        {
            InitializeComponent();
            Init(info, controller);
        }

        public override void UpdateInfo(SaveOption info)
        {
            this.m_Info = info;
            this.chkOption.Text = this.m_Info.UiString;
            this.chkOption.Checked = GeneralUtils.IsTrueValue(this.m_Info.SaveData);
        }
        
        public override void FocusValue()
        {
            this.chkOption.Focus();
        }

        private void chkOption_CheckedChanged(object sender, EventArgs e)
        {
            if (this.m_Info == null)
                return;
            this.m_Info.SaveData = this.chkOption.Checked ? "1" : "0";
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(m_Info);
        }
    }
}
