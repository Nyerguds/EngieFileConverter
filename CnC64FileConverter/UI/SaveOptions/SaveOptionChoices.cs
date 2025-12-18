using System;
using System.Drawing;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI.SaveOptions
{
    public partial class SaveOptionChoices : SaveOptionControl
    {
        private Int32 initialWidthLbl;
        private Int32 initialWidthCmb;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;

        public SaveOptionChoices() : this(null, null) { }

        public SaveOptionChoices(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = this.cmbChoices.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCmb = this.cmbChoices.Width;
            Int32 initialWidthFrm = this.DisplayRectangle.Width;
            this.m_PadLeft = this.lblDescription.Location.X;
            this.m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthCmb;
            this.m_PadMiddle = initialPosTxt - this.initialWidthLbl - this.m_PadLeft;
            this.initialWidthToScale = initialWidthFrm - this.m_PadLeft - this.m_PadRight - this.m_PadMiddle;
        }

        public override void UpdateInfo(SaveOption info)
        {
            this.m_Info = info;
            this.lblDescription.Text = GeneralUtils.DoubleFirstAmpersand(this.m_Info.UiString);
            String[] options = this.m_Info.InitValue.Split(',');
            Int32 select;
            Int32.TryParse(this.m_Info.SaveData, out select);
            for (Int32 i = 0; i < options.Length; i++)
                options[i] = options[i].Trim(" \t\r\n".ToCharArray());
            this.cmbChoices.DataSource = options;
            if (options.Length > select)
                this.cmbChoices.SelectedIndex = select;
        }
        
        public override void FocusValue()
        {
            this.cmbChoices.Select();
        }

        private void cmbChoices_SelectedIndexChanged(Object sender, EventArgs e)
        {
            // Update controller
            if (this.m_Info == null)
                return;
            this.m_Info.SaveData = this.cmbChoices.SelectedIndex.ToString();
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.m_Info);
        }
        
        private void SaveOptionChoices_Resize(Object sender, EventArgs e)
        {
            // What a mess just to make the center size...

            Double scaleFactor = (Double)this.DisplayRectangle.Width / (Double) this.initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);

            this.lblDescription.Width = newWidthLbl;
            this.cmbChoices.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.cmbChoices.Location.Y);
            this.cmbChoices.Width = newWidthTxt;
        }
    }
}
