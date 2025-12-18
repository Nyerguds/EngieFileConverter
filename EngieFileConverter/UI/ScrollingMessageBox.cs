using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EngieFileConverter.UI
{
    public partial class ScrollingMessageBox : Form
    {
        public string Title
        {
            set { this.Text = value; }
            get { return this.Text; }
        }

        public string TitleMessage
        {
            set { lblMessage.Text = value; }
            get { return lblMessage.Text; }
        }

        public IEnumerable<string> MessageList
        {
            set
            {
                txtMessage.Text = value == null ? String.Empty : string.Join(Environment.NewLine, value.ToArray());
                txtMessage.SelectionLength = 0;
                txtMessage.SelectionStart = 0;
            }
            get { return (txtMessage.Text ?? String.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'); }
        }

        public string Message
        {
            set
            {
                txtMessage.Text = value;
                txtMessage.SelectionLength = 0;
                txtMessage.SelectionStart = 0;
            }
            get { return txtMessage.Text; }
        }

        public bool UseWordWrap
        {
            set { txtMessage.WordWrap = value; }
            get { return txtMessage.WordWrap; }
        }



        public ScrollingMessageBox()
            : this(false)
        {
        }

        public ScrollingMessageBox(bool showCancelButton)
        {
            InitializeComponent();
            if (!showCancelButton)
            {
                btnOk.Location = btnCancel.Location;
                btnCancel.Visible = false;
            }
            SetWrap(txtMessage.WordWrap);
            if (!showCancelButton)
            {
                // Enables "esc" for closing the form.
                this.CancelButton = btnOk;
            }
        }

        private void SetWrap(Boolean wrap)
        {
            txtMessage.WordWrap = wrap;
            lblWrap.Text = wrap ? "[ON]" : "[OFF]";
        }

        private void btnWrap_Click(Object sender, EventArgs e)
        {
            SetWrap(!txtMessage.WordWrap);
        }

        private void btnCopy_Click(Object sender, EventArgs e)
        {
            Clipboard.SetText(txtMessage.Text);
        }

        public static DialogResult ShowAsDialog(string title, string titleMessage, string message, bool showCancel)
        {
            return ShowAsDialog(null, title, titleMessage, message, showCancel);
        }

        public static DialogResult ShowAsDialog(IWin32Window parent, string title, string titleMessage, string message, bool showCancel)
        {
            using (ScrollingMessageBox smsgb = new ScrollingMessageBox(showCancel))
            {
                smsgb.Title = title;
                smsgb.TitleMessage = titleMessage;
                smsgb.Message = message;
                if (parent != null)
                {
                    smsgb.StartPosition = FormStartPosition.CenterParent;
                }
                return smsgb.ShowDialog(parent);
            }
        }
        public static DialogResult ShowAsDialog(string title, string titleMessage, string[] message, bool showCancel)
        {
            return ShowAsDialog(null, title, titleMessage, message, showCancel);
        }

        public static DialogResult ShowAsDialog(IWin32Window parent, string title, string titleMessage, string[] message, bool showCancel)
        {
            using (ScrollingMessageBox smsgb = new ScrollingMessageBox(showCancel))
            {
                smsgb.Title = title;
                smsgb.TitleMessage = titleMessage;
                smsgb.MessageList = message;
                if (parent != null)
                {
                    smsgb.StartPosition = FormStartPosition.CenterParent;
                }
                return smsgb.ShowDialog(parent);
            }
        }

        private void ScrollingMessageBox_Load(Object sender, EventArgs e)
        {
            txtMessage.SelectionLength = 0;
            txtMessage.SelectionStart = 0;
        }
    }
}
