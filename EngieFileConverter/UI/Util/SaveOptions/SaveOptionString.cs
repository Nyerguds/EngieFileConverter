using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.UI;
using Nyerguds.Util;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class SaveOptionString : SaveOptionControl
    {
        private Int32 initialWidthLbl;
        private Int32 initialWidthTxt;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;
        private Boolean m_Loading;
        private Char[] m_AllowedMask;

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
            this.Info = info;
            this.lblDescription.Text = GeneralUtils.DoubleAmpersands(this.Info.UiString);
            this.m_AllowedMask = String.IsNullOrEmpty(info.InitValue) ? null : info.InitValue.ToCharArray();
            this.txtValue.Text = this.Info.SaveData;
        }

        public override void FocusValue()
        {
            this.txtValue.Select();
        }

        public override void DisableValue(Boolean enabled)
        {
            try
            {
                this.m_Loading = true;
                this.Enabled = enabled;
                if (enabled)
                    this.txtValue.Text = this.Info.SaveData;
                else
                    this.txtValue.Text = String.Empty;
            }
            finally
            {
                this.m_Loading = false;
            }
        }

        private void TextBoxCheckLines(Object sender, EventArgs e)
        {
            if (this.m_Loading)
                return;
            const String editing = "editing";
            TextBox textbox = sender as TextBox;
            if (textbox == null)
                return;
            if (editing.Equals(textbox.Tag))
                return;
            try
            {
                // Remove any line breaks.
                textbox.Tag = editing;
                Int32 caret = textbox.SelectionStart;
                Int32 len1 = textbox.Text.Length;
                Char[] text = textbox.Text.Replace("\n", String.Empty).Replace("\r", String.Empty).ToCharArray();
                Int32 txtLen = text.Length;
                if (m_AllowedMask != null)
                    for (Int32 i = 0; i < txtLen; ++i)
                        if (!this.m_AllowedMask.Contains(text[i]))
                            text[i] = '\0';
                textbox.Text = new String(text).Replace("\0", String.Empty);
                Int32 len2 = textbox.Text.Length;
                textbox.SelectionStart = Math.Min(caret - (len1 - len2), textbox.Text.Length);

                // Update controller
                if (this.Info == null)
                    return;
                this.Info.SaveData = textbox.Text;
                if (this.m_Controller != null)
                    this.m_Controller.UpdateControlInfo(this.Info);
            }
            finally
            {
                textbox.Tag = null;
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
