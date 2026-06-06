using Nyerguds.Util.Ui;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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
        private bool m_AllowLineBreak;

        public SaveOptionString() : this(null, null) { }

        public SaveOptionString(Option info, ListedControlController<Option> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }

        private void InitResize()
        {
            Int32 initialPosTxt = txtValue.Location.X;
            initialWidthLbl = lblDescription.Width;
            initialWidthTxt = txtValue.Width;
            Int32 initialWidthFrm = DisplayRectangle.Width;
            m_PadLeft = lblDescription.Location.X;
            m_PadRight = initialWidthFrm - initialPosTxt - initialWidthTxt;
            m_PadMiddle = initialPosTxt - initialWidthLbl - m_PadLeft;
            initialWidthToScale = initialWidthFrm - m_PadLeft - m_PadRight - m_PadMiddle;
        }

        public override void UpdateInfo(Option info)
        {
            Info = info;
            lblDescription.Text = GeneralUtils.DoubleAmpersands(Info.UiString);
            m_AllowedMask = String.IsNullOrEmpty(info.InitValue) ? null : info.InitValue.ToCharArray();
            // Only allow if explicitly in the InitValue.
            m_AllowLineBreak = info.InitValue != null && (info.InitValue.Contains('\r') || info.InitValue.Contains('\n'));
            string strData = Info.Data ?? String.Empty;
            strData = strData.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n").Trim('\r', '\n', '\t', ' ');
            txtValue.Text = strData;
        }

        public override void FocusValue()
        {
            txtValue.Select();
        }

        public override void SetEnabled(Boolean enabled)
        {
            try
            {
                m_Loading = true;
                Enabled = enabled;
                if (enabled)
                    txtValue.Text = Info.Data;
                else
                    txtValue.Text = String.Empty;
            }
            finally
            {
                m_Loading = false;
            }
        }

        private void TextBoxCheckLines(Object sender, EventArgs e)
        {
            if (m_Loading)
                return;
            const String editing = "editing";
            TextBox textbox = sender as TextBox;
            if (textbox == null)
                return;
            if (editing.Equals(textbox.Tag))
                return;
            try
            {
                if (m_AllowedMask != null)
                {
                    // Remove any line breaks.
                    textbox.Tag = editing;
                    Int32 caret = textbox.SelectionStart;
                    Char[] text = textbox.Text.ToCharArray();
                    Int32 txtLen = text.Length;
                    Int32 caretSubtract = 0;
                    for (Int32 i = 0; i < txtLen; ++i)
                    {
                        if (!m_AllowedMask.Contains(text[i]))
                        {
                            text[i] = '\0';
                            if (i < caret)
                                caretSubtract++;
                        }
                    }
                    textbox.Text = new String(text).Replace("\0", String.Empty);
                    textbox.SelectionStart = Math.Min(Math.Max(0, caret - caretSubtract), textbox.Text.Length);
                }
                // Update controller
                if (this.Info == null)
                    return;
                this.Info.Data = textbox.Text;
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
            if (!m_AllowLineBreak && (e.KeyChar == '\r' || e.KeyChar == '\n'))
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
