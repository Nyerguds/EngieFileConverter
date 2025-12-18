using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI
{
    public partial class FrmExtraOptions : Form, ListedControlController<SaveOption>
    {
        private SaveOptionInfo m_soi;
        
        public FrmExtraOptions()
        {
            InitializeComponent();
            this.Text = FrmCnC64FileConverter.GetTitle(false);
            this.m_soi = new SaveOptionInfo();
        }
        internal void Init(SaveOptionInfo soi)
        {
            this.m_soi = soi;
            this.lstOptions.Populate(this.m_soi, this);
        }

        internal SaveOption[] GetSaveOptions()
        {
            return this.m_soi.Properties;
        }

        public void UpdateControlInfo(SaveOption updateInfo)
        {
            SaveOption current = this.m_soi.Properties.First(x => String.Equals(x.Code, updateInfo.Code));
            if (current != null)
                current.SaveData = updateInfo.SaveData;
        }
    }
}
