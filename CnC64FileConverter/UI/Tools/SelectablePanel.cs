using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    public class SelectablePanel : Panel
    {
        /// <summary>
        /// When set, and the handling function sets its Handled property, this overrides the MouseWheel event.
        /// </summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public event MouseEventHandler MouseScroll;

        public SelectablePanel()
        {
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();
            base.OnMouseDown(e);
        }

        protected override Boolean IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (!e.Shift && !e.Control && !e.Alt)
            {
                switch (e.KeyValue)
                {
                    case (Int32)System.Windows.Forms.Keys.Down:
                        if (this.VerticalScroll.Visible)
                            this.VerticalScroll.Value = Math.Min(this.VerticalScroll.Maximum, this.VerticalScroll.Value + 50);
                        break;
                    case (Int32)System.Windows.Forms.Keys.PageDown:
                        if (this.VerticalScroll.Visible)
                            this.VerticalScroll.Value = Math.Min(this.VerticalScroll.Maximum, this.VerticalScroll.Value + this.ClientRectangle.Height);
                        break;
                    case (Int32)System.Windows.Forms.Keys.Up:
                        if (this.VerticalScroll.Visible)
                            this.VerticalScroll.Value = Math.Max(this.VerticalScroll.Minimum, this.VerticalScroll.Value - 50);
                        break;
                    case (Int32)System.Windows.Forms.Keys.PageUp:
                        if (this.VerticalScroll.Visible)
                            this.VerticalScroll.Value = Math.Max(this.VerticalScroll.Minimum, this.VerticalScroll.Value - this.ClientRectangle.Height);
                        break;
                    case (Int32)System.Windows.Forms.Keys.Right:
                        if (this.HorizontalScroll.Visible)
                            this.HorizontalScroll.Value = Math.Min(this.HorizontalScroll.Maximum, this.HorizontalScroll.Value + 50);
                        break;
                    case (Int32)System.Windows.Forms.Keys.Left:
                        if (this.HorizontalScroll.Visible)
                            this.HorizontalScroll.Value = Math.Max(this.HorizontalScroll.Minimum, this.HorizontalScroll.Value - 50);
                        break;
                }
                this.PerformLayout();
                this.Invalidate();
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            this.PerformLayout();
            this.Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandledMouseEventArgs args = e as HandledMouseEventArgs;
            if (args != null)
                args.Handled = true;
            HandledMouseEventArgs arg = new HandledMouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta);
            if (MouseScroll != null)
                MouseScroll(this, arg);
            if (!arg.Handled)
                base.OnMouseWheel(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            this.PerformLayout();
            this.Invalidate();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.Invalidate();
            this.PerformLayout();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            this.Invalidate();
            this.PerformLayout();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            if (this.Focused)
            {
                // disabled because it leaves refresh errors all over the place.
                Rectangle rc = this.ClientRectangle;
                rc.Inflate(-2, -2);
                ControlPaint.DrawFocusRectangle(pe.Graphics, rc);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // 0x115 and 0x20a both tell the control to scroll. If either one comes 
            // through, you can handle the scrolling before any repaints take place
            if (m.Msg == 0x115 || m.Msg == 0x20a)
            {
                this.Invalidate();
                this.PerformLayout();
            }
        }
    }
}
