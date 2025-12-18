using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CnC64FileConverter.UI
{
    public partial class FrmSetAlpha : Form
    {
        private Boolean _changing;

        public Int32 Alpha { get; set; }

        public FrmSetAlpha() : this(0)
        {
        }

        public FrmSetAlpha(Int32 alpha)
        {
            this.InitializeComponent();
            this.Alpha = alpha;
            try
            {
                this._changing = true;
                this.trbAlpha.Value = alpha;
                this.numAlpha.Value = alpha;
            }
            finally
            {
                this._changing = false;
            }
        }

        private void NumAlpha_ValueChanged(Object sender, EventArgs e)
        {
            if (this._changing)
                return;
            try
            {
                this._changing = true;
                this.trbAlpha.Value = (Int32) this.numAlpha.Value;
            }
            finally
            {
                this._changing = false;
            }
        }

        private void NumAlpha_ValueEntered(Object sender, Nyerguds.Util.UI.ValueEnteredEventArgs e)
        {
            if (this._changing)
                return;
            try
            {
                this._changing = true;
                this.trbAlpha.Value = (Int32) this.numAlpha.Value;
            }
            finally
            {
                this._changing = false;
            }
        }

        private void TrbAlpha_ValueChanged(Object sender, EventArgs e)
        {
            if (this._changing)
                return;
            try
            {
                this._changing = true;
                this.numAlpha.Value = this.trbAlpha.Value;
            }
            finally
            {
                this._changing = false;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Alpha = (Int32)numAlpha.Value;
        }
    }
}
