using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{

    [DefaultEvent("CheckStateChanged")]
    public class ImageButtonCheckBox : Label
    {
        private Boolean m_Checked = false;
        private Boolean m_Toggle = true;
        private Boolean m_Clicking = false;
        public Boolean m_TabStop = true;
        public event EventHandler CheckStateChanged;
        Color m_DisabledBackColor = Color.Transparent;

        /// <summary>
        /// Color to use as background color when the control is disabled. Any fully transparent color will disable the logic. Color.Transparent is taken as default.
        /// </summary>
        [Description("Color to use as background color when the control is disabled. Any fully transparent color will disable the logic. Color.Transparent is taken as default."), Category("Appearance")]
        [DefaultValue(typeof(Color), "Transparent")]
        public Color DisabledBackColor
        {
            get { return m_DisabledBackColor; }
            set { m_DisabledBackColor = value; }
        }

        /// <summary>
        /// Use this to get and set the control's background color. BackColor itself is overridden for the 'greyed out' mechanic.
        /// </summary>
        [Description("Use this to get and set the control's background color. BackColor itself is overridden for the 'greyed out' mechanic."), Category("Appearance")]
        public Color TrueBackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
        }

        public override Color BackColor
        {
            get { return !this.Enabled && DisabledBackColor.A != 0 ? DisabledBackColor : base.BackColor; }
            set { base.BackColor = value; }
        }

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        public new event KeyPressEventHandler KeyPress
        {
            add
            {
                base.KeyPress += value;
            }
            remove
            {
                base.KeyPress -= value;
            }
        }

        [Bindable(true)]
        [DefaultValue(false)]
        [RefreshProperties(RefreshProperties.Repaint)]
        [SettingsBindable(true)]
        public Boolean Checked
        {
            get { return this.m_Checked; }
            set
            {
                this.m_Checked = value;
                this.Invalidate();
                if (CheckStateChanged != null)
                    CheckStateChanged(this, new EventArgs());
            }
        }

        [Bindable(true)]
        [DefaultValue(true)]
        [SettingsBindable(true)]
        public Boolean Toggle
        {
            get { return this.m_Toggle; }
            set { this.m_Toggle = value; }
        }

        [Browsable(true)]
        [DefaultValue(true)]
        public new Boolean TabStop
        {
            get { return m_TabStop; }
            set
            {
                // For the editor; it doesnt't execute the SetStyle code in the constructor so it always fetches "false" here.
                m_TabStop = value;
                base.TabStop = value;
            }
        }

        [Browsable(true)]
        [DefaultValue(true)]
        public new Boolean Enabled
        {
            get { return base.Enabled; }
            set
            {
                base.Enabled= value;
                this.BorderStyle = value ? BorderStyle.None : BorderStyle.FixedSingle;
            }
        }

        public ImageButtonCheckBox()
        {
            this.SetStyle(ControlStyles.Selectable, true);
            base.BorderStyle = BorderStyle.None;
            base.TabStop = m_TabStop;
            this.ImageAlign = ContentAlignment.MiddleCenter;
        }
        
        protected override void OnEnter(EventArgs e)
        {
            this.Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            this.Invalidate();
            base.OnLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();
            this.m_Clicking = true;
            this.Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (this.ClientRectangle.Contains(e.Location))
            {
                if (this.Toggle)
                    this.Checked = !this.Checked;
                else
                    this.Checked = true;
            }
            this.m_Clicking = false;
            this.Invalidate();
            base.OnMouseUp(e);
        }

        protected override Boolean IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Enter || keyData == Keys.Space) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (!e.Alt && !e.Control && (e.KeyValue == (Int32)System.Windows.Forms.Keys.Space || e.KeyValue == (Int32)System.Windows.Forms.Keys.Enter))
            {
                m_Clicking = true;
                this.Invalidate();
            }
            this.PerformLayout();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (!e.Alt && !e.Control && (e.KeyValue == (Int32)System.Windows.Forms.Keys.Space || e.KeyValue == (Int32)System.Windows.Forms.Keys.Enter))
            {
                if (this.Toggle)
                    this.Checked = !this.Checked;
                else
                    this.Checked = true;
                this.m_Clicking = false;
                this.Invalidate();
            }
            this.PerformLayout();
            base.OnKeyUp(e);
        }
                
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            ButtonBorderStyle bs;
            if (!this.Enabled)
                bs = ButtonBorderStyle.None;
            else if (this.m_Clicking)
                bs = ButtonBorderStyle.Inset;
            else if (this.m_Checked)
                bs = ButtonBorderStyle.Inset;
            else
                bs = ButtonBorderStyle.Outset;
            Boolean hasImage = this.Image != null;
            Int32 centerOffsetX = hasImage ? (this.ClientRectangle.Width - this.Image.Width) / 2 : 0;
            Int32 centerOffsetY = hasImage? (this.ClientRectangle.Height - this.Image.Height) / 2 : 0;
            if (this.m_Clicking)
            {
                bs = ButtonBorderStyle.Inset;
                if (hasImage && ImageAlign == ContentAlignment.MiddleCenter)
                    ControlPaint.DrawImageDisabled(pe.Graphics, Image, centerOffsetX, centerOffsetY, this.BackColor);
            }
            ControlPaint.DrawBorder(pe.Graphics, ClientRectangle, Parent.BackColor, bs);
            if (this.Focused)
            {
                Rectangle rc = this.ClientRectangle;
                rc.Inflate(-1, -1);
                ControlPaint.DrawFocusRectangle(pe.Graphics, rc);
            }
        }
    }
}
