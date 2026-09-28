using System;
using System.Linq;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;
using System.Drawing;
using System.Globalization;
using EngieFileConverter.UI;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class SaveOptionNumber : SaveOptionControl
    {
        private int initialWidthLbl;
        private int initialWidthCmb;
        private int initialWidthToScale;
        private int m_PadLeft;
        private int m_PadMiddle;
        private int m_PadRight;
        private bool m_Loading;


        private volatile bool m_editingText;
        private int? m_minimum;
        private int? m_maximum;

        public SaveOptionNumber() : this(null, null) { }

        public SaveOptionNumber(Option info, ListedControlController<Option> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            int initialPosTxt = this.numValue.Location.X;
            this.initialWidthLbl = this.lblName.Width;
            this.initialWidthCmb = this.numValue.Width;
            int initialWidthFrm = this.DisplayRectangle.Width;
            this.m_PadLeft = this.lblName.Location.X;
            this.m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthCmb;
            this.m_PadMiddle = initialPosTxt - this.initialWidthLbl - this.m_PadLeft;
            this.initialWidthToScale = initialWidthFrm - this.m_PadLeft - this.m_PadRight - this.m_PadMiddle;
        }

        public override void UpdateInfo(Option info)
        {
            this.Info = info;
            this.lblName.Text = GeneralUtils.DoubleAmpersands(this.Info.UiString);
            this.numValue.Text = this.Info.Data;
            string init = this.Info.InitValue;
            this.m_minimum = null;
            this.m_maximum = null;
            if (String.IsNullOrEmpty(init))
                return;
            int cpos = init.IndexOf(",", StringComparison.Ordinal);
            if (cpos < 0)
                return;
            string min = init.Substring(0, cpos);
            decimal minVal;
            if (String.IsNullOrEmpty(min) || !decimal.TryParse(min, out minVal))
                minVal = decimal.MinValue;
            string max = init.Substring(cpos + 1);
            decimal maxVal;
            if (String.IsNullOrEmpty(max) || !decimal.TryParse(max, out maxVal))
                maxVal = decimal.MaxValue;
            if (minVal > maxVal)
                throw new ArgumentException("Initialization error: Given maximum is smaller than given minimum.", "info");
            this.numValue.Minimum = minVal;
            this.numValue.Maximum = maxVal;
        }

        public override void FocusValue()
        {
            this.numValue.Select();
        }

        private void numValue_ValueChanged(object sender, EventArgs e)
        {
            // Update controller
            if (this.Info == null)
                return;
            this.Info.Data = this.numValue.Value.ToString(CultureInfo.InvariantCulture);
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }

        private void lblName_Resize(object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            double scaleFactor = (double)this.DisplayRectangle.Width / (double)this.initialWidthToScale;
            int newWidthLbl = (int)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            int newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblName.Width = newWidthLbl;
            this.numValue.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.numValue.Location.Y);
            this.numValue.Width = newWidthTxt;
        }
    }
}
