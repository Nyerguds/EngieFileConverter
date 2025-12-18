using System;
using System.Linq;
using System.Windows.Forms;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;
using System.Drawing;

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
            InitializeComponent();
            InitResize();
            Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = this.cmbChoices.Location.X;
            initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCmb = this.cmbChoices.Width;
            Int32 initialWidthFrm = this.DisplayRectangle.Width;
            m_PadLeft = this.lblDescription.Location.X;
            m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthCmb;
            m_PadMiddle = initialPosTxt - this.initialWidthLbl - m_PadLeft;
            initialWidthToScale = initialWidthFrm - m_PadLeft - m_PadRight - m_PadMiddle;
        }

        public override void UpdateInfo(SaveOption info)
        {
            this.m_Info = info;
            this.lblDescription.Text = this.m_Info.UiString;
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
            this.cmbChoices.Focus();
        }

        private void cmbChoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update controller
            if (this.m_Info == null)
                return;
            this.m_Info.SaveData = this.cmbChoices.SelectedIndex.ToString();
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(m_Info);
        }
        
        private void SaveOptionChoices_Resize(object sender, EventArgs e)
        {
            // What a mess just to make the center size...

            Double scaleFactor = (Double)this.DisplayRectangle.Width / (Double)initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);

            this.lblDescription.Width = newWidthLbl;
            this.cmbChoices.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.cmbChoices.Location.Y);
            this.cmbChoices.Width = newWidthTxt;
        }
    }
}
