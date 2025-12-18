using System;
using System.Linq;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util.Ui;

namespace EngieFileConverter.UI
{
    public partial class FrmExtraOptions : Form, ListedControlController<SaveOption>
    {
        private SaveOptionInfo m_soi;
   
        public FrmExtraOptions()
        {
            this.InitializeComponent();
            this.Text = FrmFileConverter.GetTitle(false);
            this.m_soi = new SaveOptionInfo();
        }

        public void Init(SaveOptionInfo soi)
        {
            this.m_soi = soi;
            this.lstOptions.Populate(this.m_soi, this);
            foreach (SaveOption option in this.m_soi.Properties)
                this.UpdateControlInfo(option);
        }

        public SaveOption[] GetSaveOptions()
        {
            return this.m_soi.Properties;
        }

        public void UpdateControlInfo(SaveOption updateInfo)
        {
            SaveOption current = this.m_soi.Properties.First(x => String.Equals(x.Code, updateInfo.Code));
            if (current == null)
                return;
            current.SaveData = updateInfo.SaveData;

            this.UpdateControlChildren(current);
        }

        public void UpdateControlChildren(SaveOption updateInfo)
        {
            SaveOption[] children = this.m_soi.Properties.Where(x => String.Equals(x.ParentOption, updateInfo.Code)).ToArray();
            if (children.Length == 0)
                return;
            SaveOptionControl parent = this.lstOptions.GetListedControlByInfoObject(updateInfo);
            foreach (SaveOption child in children)
            {
                SaveOptionControl soc = this.lstOptions.GetListedControlByInfoObject(child);
                if (soc == null)
                    continue;
                Boolean matches = String.Equals(child.ParentCheckValue, updateInfo.SaveData);
                Boolean enabled = (!child.ParentCheckInverted && matches) || (child.ParentCheckInverted && !matches);
                soc.DisableValue(enabled && parent.Enabled);
                this.UpdateControlChildren(child);
            }
        }

        private void FrmExtraOptions_Load(Object sender, EventArgs e)
        {
            this.lstOptions.FocusFirst();
        }
    }
}
