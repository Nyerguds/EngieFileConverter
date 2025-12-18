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
        private Int32 initialWidthLbl;
        private Int32 initialWidthCmb;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;
        private Boolean m_Loading;


        private volatile Boolean m_editingText;
        private Int32? m_minimum;
        private Int32? m_maximum;

        public SaveOptionNumber() : this(null, null) { }

        public SaveOptionNumber(Option info, ListedControlController<Option> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = this.numValue.Location.X;
            this.initialWidthLbl = this.lblName.Width;
            this.initialWidthCmb = this.numValue.Width;
            Int32 initialWidthFrm = this.DisplayRectangle.Width;
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
            String init = this.Info.InitValue;
            this.m_minimum = null;
            this.m_maximum = null;
            if (String.IsNullOrEmpty(init))
                return;
            Int32 cpos = init.IndexOf(",", StringComparison.Ordinal);
            if (cpos < 0)
                return;
            String min = init.Substring(0, cpos);
            Decimal minVal;
            if (String.IsNullOrEmpty(min) || !Decimal.TryParse(min, out minVal))
                minVal = Decimal.MinValue;
            String max = init.Substring(cpos + 1);
            Decimal maxVal;
            if (String.IsNullOrEmpty(max) || !Decimal.TryParse(max, out maxVal))
                maxVal = Decimal.MaxValue;
            if (minVal > maxVal)
                throw new ArgumentException("Initialization error: Given maximum is smaller than given minimum!", "info");
            this.numValue.Minimum = minVal;
            this.numValue.Maximum = maxVal;
        }

        public override void FocusValue()
        {
            this.numValue.Select();
        }

        private void numValue_ValueChanged(Object sender, EventArgs e)
        {
            // Update controller
            if (this.Info == null)
                return;
            this.Info.Data = this.numValue.Value.ToString(CultureInfo.InvariantCulture);
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }

        private void lblName_Resize(Object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            Double scaleFactor = (Double)this.DisplayRectangle.Width / (Double)this.initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblName.Width = newWidthLbl;
            this.numValue.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.numValue.Location.Y);
            this.numValue.Width = newWidthTxt;
        }
    }
}
