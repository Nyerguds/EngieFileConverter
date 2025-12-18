using System;
using System.Windows.Forms;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public class SaveOptionControl : UserControl
    {
        public Option Info { get; set; }
        protected ListedControlController<Option> m_Controller;

        protected void Init(Option info, ListedControlController<Option> controller)
        {
            this.UpdateInfo(info);
            this.m_Controller = controller;
        }

        public virtual void SetEnabled(Boolean enabled)
        {
            this.Enabled = enabled;
        }

        public virtual void FocusValue() { this.Select(); }
        public virtual void UpdateInfo(Option info) { this.Info = info; }

    }
}
