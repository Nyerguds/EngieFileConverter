using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    class SelectablePanel : Panel
    {
        public SelectablePanel()
        {
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
            this.PreviewKeyDown += sc_PreviewKeyDown;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Focus();
            base.OnMouseDown(e);
        }
        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down) return true;
            if (keyData == Keys.Left || keyData == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }

        private void sc_PreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
        {
            SelectablePanel sc = sender as SelectablePanel;
            switch (e.KeyValue)
            {
                case (int)System.Windows.Forms.Keys.Down:
                    sc.VerticalScroll.Value += 50;
                    break;
                case (int)System.Windows.Forms.Keys.Up:
                    if (sc.VerticalScroll.Value - 50 < 0)
                        sc.VerticalScroll.Value = 0;
                    else sc.VerticalScroll.Value -= 50;
                    break;
                case (int)System.Windows.Forms.Keys.Right:
                    sc.HorizontalScroll.Value += 50;
                    break;
                case (int)System.Windows.Forms.Keys.Left:
                    if (sc.HorizontalScroll.Value - 50 < 0)
                        sc.HorizontalScroll.Value = 0;
                    else sc.HorizontalScroll.Value -= 50;
                    break;
            }
            sc.PerformLayout();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
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
                //Rectangle rc = this.ClientRectangle;
                //rc.Inflate(-2, -2);
                //ControlPaint.DrawFocusRectangle(pe.Graphics, rc);
            }
        }
    }
}
