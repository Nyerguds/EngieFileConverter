using System;
using System.Windows.Forms;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public class SaveOptionControl : UserControl
    {
        public SaveOption Info { get; set; }
        protected ListedControlController<SaveOption> m_Controller;

        protected void Init(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.UpdateInfo(info);
            this.m_Controller = controller;
        }

        public virtual void SetEnabled(Boolean enabled)
        {
            this.Enabled = enabled;
        }

        public virtual void FocusValue() { this.Select(); }
        public virtual void UpdateInfo(SaveOption info) { this.Info = info; }

    }
}
