using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    public class ComboBoxSmartWidth : ComboBox
    {
        protected override void OnDropDown(EventArgs e)
        {
            this.SetDropDownWidth(e);
            base.OnDropDown(e);
        }

        private void SetDropDownWidth(EventArgs e)
        {
            Int32 widestStringInPixels = this.Width;
            Boolean hasScrollBar = this.Items.Count * this.ItemHeight > this.DropDownHeight;
            if (hasScrollBar)
                widestStringInPixels -= SystemInformation.VerticalScrollBarWidth;
            Boolean noDisplayMember = String.IsNullOrEmpty(this.DisplayMember);
            foreach (Object o in this.Items)
            {
                String toCheck;
                if (noDisplayMember)
                    toCheck = o == null ? String.Empty : o.ToString();
                else
                {
                    Object val = null;
                    try { val = o.GetType().GetProperty(this.DisplayMember).GetValue(o, null); }
                    catch { /* ignore; if it fails, just consider it empty. */ }
                    toCheck = val == null ? String.Empty : val.ToString();
                }
                if (toCheck.Length > 0)
                {
                    Int32 newWidth = TextRenderer.MeasureText(toCheck, this.Font).Width;
                    Int32 newWidth2;
                    using (Graphics g = this.CreateGraphics())
                        newWidth2 = g.MeasureString(toCheck, this.Font).ToSize().Width;
                    newWidth = Math.Max(newWidth, newWidth2);
                    if (this.DrawMode == DrawMode.OwnerDrawFixed)
                        newWidth += 4;
                    if (newWidth > widestStringInPixels)
                        widestStringInPixels = newWidth;
                }
            }
            if (hasScrollBar)
                widestStringInPixels += SystemInformation.VerticalScrollBarWidth;
            this.DropDownWidth = widestStringInPixels;
        }
    }
}
