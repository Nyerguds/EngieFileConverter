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
            this.InitializeComponent();
            this.Init(info, controller);
        }

        public override void UpdateInfo(SaveOption info)
        {
            this.Info = info;
            this.lblName.Text = GeneralUtils.DoubleFirstAmpersand(this.Info.UiString);
            this.txtValue.Text = this.Info.SaveData;
            String init = this.Info.InitValue;
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

        private void txtValue_TextChanged(Object sender, EventArgs e)
        {
            if (this.m_editingText)
                return;
            try
            {
                this.m_editingText = true;
                // Filter to pure integers
                String input = this.txtValue.Text.ToUpperInvariant();
                Int32 selStart = this.txtValue.SelectionStart;
                String output = new String(input.Where(x => x == '-' || x >= '0' && x <= '9').ToArray());
                if (!String.Equals(this.txtValue.Text, output))
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
                if (!String.Equals(this.txtValue.Text, output))
                {
                    // Fix selection
                    this.txtValue.Text = output;
                    this.txtValue.SelectionStart = Math.Min(selStart, this.txtValue.Text.Length);
                }
                // Update controller
                if (this.Info == null)
                    return;
                this.Info.SaveData = (output.Length == 0 || "-".Equals(output)) ? "0" : this.txtValue.Text;
                if (this.m_Controller != null)
                    this.m_Controller.UpdateControlInfo(this.Info);
            }
            finally
            {
                this.m_editingText = false;
            }
        }

        public override void FocusValue()
        {
            this.txtValue.Select();
        }
    }
}
