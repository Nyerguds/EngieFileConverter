using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    public class SelectablePanel : Panel
    {
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

        protected override bool IsInputKey(Keys keyData)
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
                    case (int)System.Windows.Forms.Keys.Down:
                        if (this.VerticalScroll.Visible)
                            this.VerticalScroll.Value = Math.Min(this.VerticalScroll.Maximum, this.VerticalScroll.Value + 50);
                        break;
                    case (int)System.Windows.Forms.Keys.Up:
                        if (this.VerticalScroll.Visible)
                            this.VerticalScroll.Value = Math.Max(this.VerticalScroll.Minimum, this.VerticalScroll.Value - 50);
                        break;
                    case (int)System.Windows.Forms.Keys.Right:
                        if (this.HorizontalScroll.Visible)
                            this.HorizontalScroll.Value = Math.Min(this.HorizontalScroll.Maximum, this.HorizontalScroll.Value + 50);
                        break;
                    case (int)System.Windows.Forms.Keys.Left:
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
            this.Invalidate();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            this.Invalidate();
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
    }
}
