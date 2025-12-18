using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.UI;
using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using Nyerguds.Util.Ui;
using Nyerguds.Util.UI;

namespace Nyerguds.Util.UI.SaveOptions
{
    public partial class SaveOptionPalette : SaveOptionControl
    {
        private Int32 initialWidthLbl;
        private Int32 initialWidthCol;
        private Int32 initialWidthToScale;
        private Int32 m_PadLeft;
        private Int32 m_PadMiddle;
        private Int32 m_PadRight;
        private Boolean m_Loading;
        private Color[] m_Palette;
        private Int32 m_paletteBpp;

        public SaveOptionPalette() : this(null, null) { }

        public SaveOptionPalette(SaveOption info, ListedControlController<SaveOption> controller)
        {
            this.InitializeComponent();
            this.InitResize();
            this.Init(info, controller);
        }
        
        private void InitResize()
        {
            Int32 initialPosTxt = this.pnlColor.Location.X;
            this.initialWidthLbl = this.lblDescription.Width;
            this.initialWidthCol = this.pnlColor.Width;
            Int32 initialWidthFrm = this.DisplayRectangle.Width;
            this.m_PadLeft = this.lblDescription.Location.X;
            this.m_PadRight = initialWidthFrm - initialPosTxt - this.initialWidthCol;
            this.m_PadMiddle = initialPosTxt - this.initialWidthLbl - this.m_PadLeft;
            this.initialWidthToScale = initialWidthFrm - this.m_PadLeft - this.m_PadRight - this.m_PadMiddle;
        }

        public override void UpdateInfo(SaveOption info)
        {
            try
            {
                m_Loading = true;
                this.Info = info;
                this.lblDescription.Text = GeneralUtils.DoubleAmpersands(this.Info.UiString);
                Regex r = new Regex("^\\s*(\\d+)\\s*\\|(\\s*#?[0-9a-fA-F]{6}\\s*(,\\s*#?[0-9a-fA-F]{6}\\s*)*)$");
                String initInfo = this.Info.InitValue;
                Match m = r.Match(initInfo);
                if (m.Success)
                {
                    m_paletteBpp = Int32.Parse(m.Groups[1].Value);
                    String[] paletteStr = m.Groups[2].Value.Split(',');
                    Int32 palStrLen = paletteStr.Length;
                    this.m_Palette = new Color[palStrLen];
                    for (Int32 i = 0; i < palStrLen; ++i)
                        this.m_Palette[i] = ColorUtils.ColorFromHexString(paletteStr[i].Trim());
                }
                this.SelectFromSaveData();
            }
            finally
            {
                m_Loading = false;
            }
        }

        private void SelectFromSaveData()
        {
            Int32 select;
            Int32.TryParse(this.Info.SaveData, out select);
            if (this.m_Palette != null && select >= 0 && select < this.m_Palette.Length)
            {
                this.lblColor.TrueBackColor = this.m_Palette[select];
                this.lblColorVal.Text = "Index " + select;
            }
        }


        private void LblColor_KeyPress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '\r' || e.KeyChar == '\n')
                this.PickTrimColor();
        }

        private void LblColor_MouseClick(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.PickTrimColor();
        }

        private void PickTrimColor()
        {
            FrmPalette palSelect = new FrmPalette(this.m_Palette.ToArray(), false, ColorSelMode.Single);
            palSelect.SelectedIndices = new Int32[] { lblColor.Tag as Int32? ?? 0 };
            if (palSelect.ShowDialog() != DialogResult.OK)
                return;
            Int32 selectedColor = palSelect.SelectedIndices.Length == 0 ? 0 : palSelect.SelectedIndices[0];
            lblColor.Tag = selectedColor;
            lblColor.TrueBackColor = this.m_Palette[selectedColor];
            this.lblColorVal.Text = "Index " + selectedColor;
            this.Info.SaveData = selectedColor.ToString();
            if (this.m_Controller != null)
                this.m_Controller.UpdateControlInfo(this.Info);
        }

        public override void FocusValue()
        {
            this.lblColor.Select();
        }

        private void SaveOptionPalette_Resize(Object sender, EventArgs e)
        {
            // What a mess just to make the center size...
            Double scaleFactor = (Double)this.DisplayRectangle.Width / (Double)this.initialWidthToScale;
            Int32 newWidthLbl = (Int32)Math.Round(this.initialWidthLbl * scaleFactor, MidpointRounding.AwayFromZero);
            Int32 newWidthTxt = this.DisplayRectangle.Width - (this.m_PadLeft + newWidthLbl + this.m_PadMiddle + this.m_PadRight);
            this.lblDescription.Width = newWidthLbl;
            this.pnlColor.Location = new Point(this.m_PadLeft + newWidthLbl + this.m_PadMiddle, this.pnlColor.Location.Y);
            this.pnlColor.Width = newWidthTxt;
        }
    }
}
