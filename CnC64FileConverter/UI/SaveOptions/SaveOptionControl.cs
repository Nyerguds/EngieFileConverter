using System.Windows.Forms;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI
{
    public class SaveOptionControl : UserControl
    {
        protected SaveOption m_Info = null;
        protected ListedControlController<SaveOption> m_Controller = null;

        protected void Init(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.UpdateInfo(info);
            this.m_Controller = controller;
        }
        public virtual void FocusValue() { this.Focus(); }
        public virtual void UpdateInfo(SaveOption info) { this.m_Info = info; }
    }
}
