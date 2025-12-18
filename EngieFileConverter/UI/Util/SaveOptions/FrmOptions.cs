using System;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class FrmOptions : Form, ListedControlController<SaveOption>
    {
        private SaveOptionInfo m_soi;

        public Int32 OptimalHeight { get; private set; }

        public FrmOptions()
        {
            this.InitializeComponent();
            this.m_soi = new SaveOptionInfo();
        }

        public FrmOptions(String title, SaveOptionInfo soi)
        {
            this.InitializeComponent();
            this.Text = title;
            if (soi == null)
                this.m_soi = new SaveOptionInfo();
            else
                this.Init(soi);
        }

        public void Init(SaveOptionInfo soi)
        {
            this.m_soi = soi;
            this.lstOptions.Populate(this.m_soi, this);
            SaveOption[] props = this.m_soi.Properties;
            Int32 nrOfProps = props.Length;
            for (Int32 i = 0; i < nrOfProps; ++i)
                this.UpdateControlInfo(props[i]);
            this.OptimalHeight = this.Height - pnlOptions.Height + lstOptions.Height;
        }

        public SaveOption[] GetSaveOptions()
        {
            return this.m_soi.Properties;
        }

        public void UpdateControlInfo(SaveOption updateInfo)
        {
            SaveOption current = null;
            SaveOption[] props = this.m_soi.Properties;
            Int32 nrOfProps = props.Length;
            String updCode = updateInfo.Code;
            for (Int32 i = 0; i < nrOfProps; ++i)
            {
                SaveOption prop = props[i];
                if (String.Equals(prop.Code, updCode))
                {
                    current = prop;
                    break;
                }
            }
            if (current == null)
                return;
            current.SaveData = updateInfo.SaveData;
            this.UpdateControlChildren(current);
        }

        public void UpdateControlChildren(SaveOption dependingOn)
        {
            String checkCode = dependingOn.Code;
            SaveOption[] dependentControls = this.m_soi.Properties;
            Int32 nrOfDependentControls = dependentControls.Length;
            for (Int32 i = 0; i < nrOfDependentControls; ++i)
            {
                SaveOption dependentControl = dependentControls[i];
                SaveEnableFilter[] filters = dependentControl.Filters;
                Int32 nrOfFilters = filters.Length;
                Boolean hasFilter = false;
                for (Int32 f = 0; f < nrOfFilters; ++f)
                {
                    if (filters[f].CheckOption != checkCode)
                        continue;
                    hasFilter = true;
                    break;
                }
                if (!hasFilter)
                    continue;
                SaveOptionControl soc = this.lstOptions.GetListedControlByInfoObject(dependentControl);
                if (soc == null)
                    continue;
                Int32 matchAmount = 0;
                Int32 neededAmount = nrOfFilters;
                for (Int32 f = 0; f < nrOfFilters; ++f)
                {
                    Boolean controlFound;
                    if (this.EvaluateFilter(filters[f], out controlFound))
                        matchAmount++;
                    if (!controlFound)
                        neededAmount--;
                }
                Boolean passed = dependentControl.saveFilterAnd ? matchAmount == neededAmount : matchAmount > 0;
                soc.SetEnabled(passed);
                this.UpdateControlChildren(dependentControl);
            }
        }

        private Boolean EvaluateFilter(SaveEnableFilter filter, out Boolean controlFound)
        {
            String checkCode = filter.CheckOption;
            SaveOption[] saveOpts = this.m_soi.Properties;
            Int32 nrOfOpts = saveOpts.Length;
            controlFound = false;
            for (Int32 i = 0; i < nrOfOpts; ++i)
            {
                SaveOption opt = saveOpts[i];
                if (opt.Code != checkCode)
                    continue;
                SaveOptionControl checkSoc = this.lstOptions.GetListedControlByInfoObject(opt);
                // A control that can't be modified automatically fails the test.
                if (!checkSoc.Enabled)
                    return false;
                controlFound = true;
                Boolean curMatches = filter.CheckValues.Contains(opt.SaveData);
                return filter.CheckInverted ? !curMatches : curMatches;
            }
            return false;
        }

        private void FrmExtraOptions_Load(Object sender, EventArgs e)
        {
            this.lstOptions.FocusFirst();
        }
    }
}
