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
            SaveOption[] children = this.m_soi.Properties.Where(x => x.Filters.Any(f => f.CheckOption == updateInfo.Code)).ToArray();
            if (children.Length == 0)
                return;
            foreach (SaveOption child in children)
            {
                SaveOptionControl soc = this.lstOptions.GetListedControlByInfoObject(child);
                if (soc == null)
                    continue;
                Boolean matches = true;
                foreach (SaveEnableFilter filter in child.Filters)
                {
                    if (!EvaluateFilter(filter))
                    {
                        matches = false;
                        break;
                    }
                }
                soc.DisableValue(matches);
                this.UpdateControlChildren(child);
                
            }
        }

        private Boolean EvaluateFilter(SaveEnableFilter filter)
        {
            SaveOption checkOpt = this.m_soi.Properties.FirstOrDefault(p => p.Code == filter.CheckOption);
            if (checkOpt == null)
                return false;
            SaveOptionControl checkSoc = this.lstOptions.GetListedControlByInfoObject(checkOpt);
            if (!checkSoc.Enabled)
                return false;
            Boolean curMatches = filter.CheckValues.Contains(checkOpt.SaveData);
            return (!filter.CheckInverted && curMatches) || (filter.CheckInverted && !curMatches);
        }

        private void FrmExtraOptions_Load(Object sender, EventArgs e)
        {
            this.lstOptions.FocusFirst();
        }
    }
}
