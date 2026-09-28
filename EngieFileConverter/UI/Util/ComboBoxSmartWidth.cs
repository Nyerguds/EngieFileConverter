using System;
using System.Drawing;
using System.Reflection;
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
            int widestStringInPixels = this.Width;
            bool hasScrollBar = this.Items.Count * this.ItemHeight > this.DropDownHeight;
            if (hasScrollBar)
                widestStringInPixels -= SystemInformation.VerticalScrollBarWidth;
            bool noDisplayMember = String.IsNullOrEmpty(this.DisplayMember);
            foreach (object o in this.Items)
            {
                if (o == null)
                    continue;
                string toCheck;
                if (noDisplayMember)
                    toCheck = o.ToString();
                else
                {
                    PropertyInfo pi = o.GetType().GetProperty(this.DisplayMember);
                    object val = pi == null ? null : pi.GetValue(o, null);
                    toCheck = val == null ? String.Empty : val.ToString();
                }
                if (toCheck.Length <= 0)
                    continue;
                int newWidth = TextRenderer.MeasureText(toCheck, this.Font).Width;
                int newWidth2;
                using (Graphics g = this.CreateGraphics())
                    newWidth2 = g.MeasureString(toCheck, this.Font).ToSize().Width;
                newWidth = Math.Max(newWidth, newWidth2);
                if (this.DrawMode == DrawMode.OwnerDrawFixed)
                    newWidth += 4;
                if (newWidth > widestStringInPixels)
                    widestStringInPixels = newWidth;
            }
            if (hasScrollBar)
                widestStringInPixels += SystemInformation.VerticalScrollBarWidth;
            this.DropDownWidth = widestStringInPixels;
        }
    }
}
