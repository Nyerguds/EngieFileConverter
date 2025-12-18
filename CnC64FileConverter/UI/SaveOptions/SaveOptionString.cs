using System;
using System.Drawing;
using System.Windows.Forms;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI.SaveOptions
{
    public partial class SaveOptionString : SaveOptionControl
    {
        private Int32 initialWidthLbl;
        private Int32 initialWidthTxt;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;

        public SaveOptionString() : this(null, null) { }

        public SaveOptionString(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = this.txtValue.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthTxt = this.txtValue.Width;
            Int32 initialWidthFrm = this.DisplayRectangle.Width;
            this.m_PadLeft = this.lblDescription.Location.X;
            this.m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthTxt;
            this.m_PadMiddle = initialPosTxt - this.initialWidthLbl - this.m_PadLeft;
            this.initialWidthToScale = initialWidthFrm - this.m_PadLeft - this.m_PadRight - this.m_PadMiddle;
        }

        public override void UpdateInfo(SaveOption info)
        {
            this.m_Info = info;
            this.lblDescription.Text = GeneralUtils.DoubleFirstAmpersand(this.m_Info.UiString);
            this.txtValue.Text = this.m_Info.SaveData;
        }
        
        public override void FocusValue()
        {
            this.txtValue.Select();
        }        

        private void TextBoxCheckLines(Object sender, EventArgs e)
        {
            const String editing = "editing";
            if (sender is TextBox)
            {
                TextBox textbox = (TextBox)sender;
                if (editing.Equals(textbox.Tag))
                    return;
                try
                {
                    // Remove any line breaks.
                    textbox.Tag = editing;
                    Int32 caret = textbox.SelectionStart;
                    Int32 len1 = textbox.Text.Length;
                    textbox.Text = textbox.Text.Replace("\n", String.Empty).Replace("\r", String.Empty);
                    Int32 len2 = textbox.Text.Length;
                    textbox.SelectionStart = Math.Min(caret - (len1 - len2), textbox.Text.Length);
                    
                    // Update controller
                    if (this.m_Info == null)
                        return;
                    this.m_Info.SaveData = textbox.Text;
                    if (this.m_Controller != null)
                        this.m_Controller.UpdateControlInfo(this.m_Info);
                }
                finally
                {
                    textbox.Tag = null;
                }
            }
        }

        private void TextBoxCheckKeyPress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' || e.KeyChar == '\n')
                e.Handled = true;
        }

        private void TextBoxSelectAll(Object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.A))
            {
                if (sender != null && sender is TextBox)
                {
                    ((TextBox)sender).SelectAll();
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            }
        }

        private void SaveOptionString_Resize(Object sender, EventArgs e)
        {
            // What a mess just to make the center size...

            Double scaleFactor = (Double)this.DisplayRectangle.Width / this.initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);

            this.lblDescription.Width = newWidthLbl;
            this.txtValue.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.txtValue.Location.Y);
            this.txtValue.Width = newWidthTxt;
        }

    }
}
