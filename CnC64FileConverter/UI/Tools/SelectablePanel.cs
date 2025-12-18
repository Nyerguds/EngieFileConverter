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
            Boolean handled = false;
            if (!e.Shift && !e.Control && !e.Alt)
            {
                handled = true;
                switch (e.KeyValue)
                {
                    case (Int32)Keys.Down:
                        this.ScrollVertical(50);
                        break;
                    case (Int32)Keys.PageDown:
                        this.ScrollVertical(this.ClientRectangle.Height);
                        break;
                    case (Int32)Keys.Up:
                        this.ScrollVertical(-50);
                        break;
                    case (Int32)Keys.PageUp:
                        this.ScrollVertical(-this.ClientRectangle.Height);
                        break;
                    case (Int32)Keys.Right:
                        this.ScrollHorizontal(50);
                        break;
                    case (Int32)Keys.Left:
                        this.ScrollHorizontal(-50);
                        break;
                    default:
                        handled = false;
                        break;
                }
            }
            else if (e.Shift)
            {
                handled = true;
                // Shift+pgup/pgdn to scroll vertically.
                switch (e.KeyValue)
                {
                    case (Int32)Keys.PageDown:
                        this.ScrollHorizontal(this.ClientRectangle.Height);
                        break;
                    case (Int32)Keys.PageUp:
                        this.ScrollHorizontal(-this.ClientRectangle.Height);
                        break;
                    default:
                        handled = false;
                        break;
                }
                this.PerformLayout();
                this.Invalidate();
            }
            if (handled)
            {
                this.PerformLayout();
                this.Invalidate();
            }
            else
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
            Keys k = Control.ModifierKeys;
            HandledMouseEventArgs args = e as HandledMouseEventArgs;
            if (args != null)
                args.Handled = true;
            HandledMouseEventArgs arg = new HandledMouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta);
            if (MouseScroll != null)
                MouseScroll(this, arg);
            if (arg.Handled)
                return;
            if ((k & Keys.Shift) != 0)
            {
                ScrollHorizontal(-1 * e.Delta);
                return;
            }
            base.OnMouseWheel(e);
            
        }

        protected void ScrollVertical(Int32 delta)
        {
            if (!this.VScroll)
                return;
            Rectangle clientRectangle = this.ClientRectangle;
            Int32 num = -this.DisplayRectangle.Y;
            Int32 val2 = -(clientRectangle.Height - this.DisplayRectangle.Height);
            this.SetDisplayRectLocation(this.DisplayRectangle.X, -Math.Min(Math.Max(num + delta, 0), val2));
        }

        protected void ScrollHorizontal(Int32 delta)
        {
            if (!this.HScroll)
                return;
            Rectangle clientRectangle = this.ClientRectangle;
            Int32 num = -this.DisplayRectangle.X;
            Int32 val2 = -(clientRectangle.Width - this.DisplayRectangle.Width);
            this.SetDisplayRectLocation(-Math.Min(Math.Max(num + delta, 0), val2), this.DisplayRectangle.Y);
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
