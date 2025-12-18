using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Nyerguds.Util.Ui
{
    /// <summary>
    /// Offers the ability to list user controls, which can send updates of their child controls back to a controller.
    /// </summary>
    /// <typeparam name="T">Type of the user controls with which to populate the list.</typeparam>
    /// <typeparam name="U">Type of the information objects that contain all information to create/manage a listed control.</typeparam>
    public abstract partial class ControlsList<T,U> : UserControl where T : Control
    {
        protected List<T> m_Contents = new List<T>();
        protected CustomControlInfo<T, U> m_CustomControlInfo;

        protected ControlsList()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populate the list with controls.
        /// </summary>
        /// <param name="cci">Contains a list of information objects with which to create the custom controls.</param>
        /// <param name="ebc">The controller to assign to the created custom controls.</param>
        public void Populate(CustomControlInfo<T, U> cci, ListedControlController<U> ebc)
        {
            this.Reset();
            if (cci == null)
                return;
            this.m_CustomControlInfo = cci;
            this.SuspendLayout();
            this.lblTypeName.Text = cci.Name;
            foreach (U vsi in cci.Properties)
            {
                try
                {
                    T eb = cci.MakeControl(vsi, ebc);
                    this.AddControl(eb, false);
                }
                catch (NotImplementedException) { /* ignore */ }
            }
            this.PerformLayout();
        }

        public virtual T GetListedControlByInfoObject(U infoObject)
        {
            if (this.m_CustomControlInfo == null)
                return null;
            return this.m_CustomControlInfo.GetControlByProperty(infoObject, m_Contents);
        }

        /// <summary>
        /// Focus the first listed item.
        /// </summary>
        public void FocusFirst()
        {
            if (this.m_Contents.Count == 0)
                return;
            //this.Select();
            this.FocusItem(this.m_Contents[0]);
        }

        /// <summary>
        /// Focus the item. Can be overridden to focus a specific sub-control on the item.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        protected virtual void FocusItem(T control)
        {
            control.Select();
        }

        protected void AddControl(T control, Boolean refresh)
        {
            if (refresh)
                this.SuspendLayout();
            Int32 YPos;
            if (this.m_Contents.Count == 0)
                YPos = this.lblTypeName.Location.Y * 2 + this.lblTypeName.Size.Height;
            else
            {
                T lastControl = this.m_Contents[this.m_Contents.Count - 1];
                YPos = lastControl.Location.Y + lastControl.Size.Height;
            }
            control.Location = new Point(0, YPos);
            this.m_Contents.Add(control);
            this.Controls.Add(control);
            control.TabIndex = this.Controls.Count;
            control.Size = new Size(this.DisplayRectangle.Width, control.Size.Height);
            this.Size = new Size(this.Size.Width, YPos + control.Size.Height);
            if (refresh)
                this.PerformLayout();
        }

        public void Reset()
        {
            this.SuspendLayout();
            this.lblTypeName.Text = String.Empty;
            foreach (T c in this.m_Contents)
            {
                this.Controls.Remove(c);
                c.Dispose();
            }
            this.m_Contents.Clear();
            this.PerformLayout();
        }

        protected void EffectBarList_Resize(Object sender, EventArgs e)
        {
            this.SuspendLayout();
            foreach (T c in this.m_Contents)
                c.Size = new Size(this.DisplayRectangle.Width, c.Size.Height);
            this.PerformLayout();
        }
    }
}
