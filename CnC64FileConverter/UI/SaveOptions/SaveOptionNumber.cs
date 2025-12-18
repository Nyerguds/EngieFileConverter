using System;
using System.Linq;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI.SaveOptions
{
    public partial class SaveOptionNumber : SaveOptionControl
    {
        private Boolean m_editingText = false;
        private Int32? m_minimum;
        private Int32? m_maximum;

        public SaveOptionNumber() : this(null, null) { }

        public SaveOptionNumber(SaveOption info, ListedControlController<SaveOption> controller)
        {
            InitializeComponent();
            Init(info, controller);
        }

        public override void UpdateInfo(SaveOption info)
        {
            this.m_Info = info;
            this.lblName.Text = GeneralUtils.DoubleFirstAmpersand(this.m_Info.UiString);
            this.txtValue.Text = this.m_Info.SaveData;
            String init = this.m_Info.InitValue;
            this.m_minimum = null;
            this.m_maximum = null;
            if (String.IsNullOrEmpty(init))
                return;
            Int32 cpos = init.IndexOf(",", StringComparison.Ordinal);
            if (cpos < 0)
                return;
            String min = init.Substring(0, cpos);
            Int32 minVal;
            String max = init.Substring(cpos + 1);
            Int32 maxVal;
            if (!String.IsNullOrEmpty(min) && Int32.TryParse(min, out minVal))
                this.m_minimum = minVal;
            if (!String.IsNullOrEmpty(max) && Int32.TryParse(max, out maxVal))
                this.m_maximum = maxVal;
        }

        private void txtValue_TextChanged(object sender, EventArgs e)
        {
            if (m_editingText)
                return;
            try
            {
                m_editingText = true;
                // Filter to pure integers
                String input = txtValue.Text.ToUpperInvariant();
                Int32 selStart = this.txtValue.SelectionStart;
                String output = new String(input.Where(x => x == '-' || x >= '0' && x <= '9').ToArray());
                if (!String.Equals(txtValue.Text, output))
                {
                    if (Math.Min(selStart, output.Length) > 0 && selStart <= output.Length && output[selStart - 1] != input[selStart - 1])
                        selStart--;
                }
                if (output.Length > 0 && !"-".Equals(output) && (this.m_minimum.HasValue || this.m_maximum.HasValue))
                {
                    Int32 val = Int32.Parse(output);
                    if (this.m_minimum.HasValue)
                        val = Math.Max(this.m_minimum.Value, val);
                    if (this.m_maximum.HasValue)
                        val = Math.Min(this.m_maximum.Value, val);
                    output = val.ToString();
                }
                if (!String.Equals(txtValue.Text, output))
                {
                    // Fix selection
                    txtValue.Text = output;
                    this.txtValue.SelectionStart = Math.Min(selStart, txtValue.Text.Length);
                }
                // Update controller
                if (this.m_Info == null)
                    return;
                this.m_Info.SaveData = (output.Length == 0 || "-".Equals(output)) ? "0" : this.txtValue.Text;
                if (this.m_Controller != null)
                    this.m_Controller.UpdateControlInfo(m_Info);
            }
            finally
            {
                m_editingText = false;
            }
        }

        public override void FocusValue()
        {
            this.txtValue.Focus();
        }
    }
}
