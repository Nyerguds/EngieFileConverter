using System;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class FrmOptions : Form, ListedControlController<Option>
    {
        private SaveOptionInfo m_soi;

        public int OptimalHeight { get; private set; }

        public FrmOptions()
        {
            this.InitializeComponent();
            this.m_soi = new SaveOptionInfo();
        }

        public FrmOptions(string title, SaveOptionInfo soi)
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
            Option[] props = this.m_soi.Properties;
            int nrOfProps = props.Length;
            for (int i = 0; i < nrOfProps; ++i)
                this.UpdateControlInfo(props[i]);
            this.OptimalHeight = this.Height - pnlOptions.Height + lstOptions.Height;
        }

        public Option[] GetSaveOptions()
        {
            return this.m_soi.Properties;
        }

        public void UpdateControlInfo(Option updateInfo)
        {
            Option current = null;
            Option[] props = this.m_soi.Properties;
            int nrOfProps = props.Length;
            string updCode = updateInfo.Code;
            for (int i = 0; i < nrOfProps; ++i)
            {
                Option prop = props[i];
                if (String.Equals(prop.Code, updCode))
                {
                    current = prop;
                    break;
                }
            }
            if (current == null)
                return;
            current.Data = updateInfo.Data;
            this.UpdateControlChildren(current);
        }

        public void UpdateControlChildren(Option dependingOn)
        {
            string checkCode = dependingOn.Code;
            Option[] dependentControls = this.m_soi.Properties;
            int nrOfDependentControls = dependentControls.Length;
            for (int i = 0; i < nrOfDependentControls; ++i)
            {
                Option dependentControl = dependentControls[i];
                EnableFilter[] filters = dependentControl.Filters;
                int nrOfFilters = filters.Length;
                bool hasFilter = false;
                for (int f = 0; f < nrOfFilters; ++f)
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
                int matchAmount = 0;
                int neededAmount = nrOfFilters;
                for (int f = 0; f < nrOfFilters; ++f)
                {
                    bool controlFound;
                    if (this.EvaluateFilter(filters[f], out controlFound))
                        matchAmount++;
                    if (!controlFound)
                        neededAmount--;
                }
                bool passed = dependentControl.FilterAnd ? matchAmount == neededAmount : matchAmount > 0;
                soc.SetEnabled(passed);
                this.UpdateControlChildren(dependentControl);
            }
        }

        private bool EvaluateFilter(EnableFilter filter, out bool controlFound)
        {
            string checkCode = filter.CheckOption;
            Option[] saveOpts = this.m_soi.Properties;
            int nrOfOpts = saveOpts.Length;
            controlFound = false;
            for (int i = 0; i < nrOfOpts; ++i)
            {
                Option opt = saveOpts[i];
                if (opt.Code != checkCode)
                    continue;
                SaveOptionControl checkSoc = this.lstOptions.GetListedControlByInfoObject(opt);
                // A control that can't be modified automatically fails the test.
                if (!checkSoc.Enabled)
                    return false;
                controlFound = true;
                bool curMatches = filter.CheckMatchValues.Contains(opt.Data);
                return filter.WhenCheckMatches ? curMatches : !curMatches;
            }
            return false;
        }

        private void FrmExtraOptions_Load(object sender, EventArgs e)
        {
            this.lstOptions.FocusFirst();
        }
    }
}
