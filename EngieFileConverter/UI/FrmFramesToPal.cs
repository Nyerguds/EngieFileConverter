using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using EngieFileConverter.Domain.FileTypes;
using Nyerguds.ImageManipulation;
using Nyerguds.Util.UI;

namespace EngieFileConverter.UI
{
    public partial class FrmFramesToPal : Form
    {
        
        public Int32 MatchBpp  { get; private set; }
        public Color[] MatchPalette { get; private set; }

        private PaletteDropDownInfo[] m_allPalettes;

        public Int32[] CustomColors
        {
            get { return this.pzpFramePreview.CustomColors; }
            set { this.pzpFramePreview.CustomColors = value; }
        }

        private SupportedFileType m_File;
        private Boolean m_Loading;

        public FrmFramesToPal(SupportedFileType origFile, PaletteDropDownInfo[] palettes)
        {
            this.m_Loading = true;
            this.m_File = origFile;
            if (origFile == null)
                throw new ArgumentNullException("origFile");
            this.m_allPalettes = palettes ?? new PaletteDropDownInfo[0];
            this.InitializeComponent();
            Boolean hasFrames = origFile.IsFramesContainer;
            this.numCurFrame.Maximum = hasFrames ? origFile.Frames.Length - 1 : 0;
            this.cmbPalType.DataSource = new String[] {"1-bit", "4-bit", "8-bit"};
            this.cmbPalType.SelectedIndex = 2;
            this.m_Loading = false;
            this.UpdateUiInfo();
            this.pzpFramePreview.AutoSetZoom(GetListToAutoSetZoom(m_File));
        }

        private void FrameChanged(Object sender, EventArgs e)
        {
            UpdateUiInfo();
        }

        private void UpdateUiInfo()
        {
            try
            {
                this.m_Loading = true;
                SupportedFileType curFrame = m_File.IsFramesContainer ? m_File.Frames[(Int32)numCurFrame.Value] : m_File;
                
                PaletteDropDownInfo pdd = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
                if(pdd == null)
                    return;
                Bitmap image = curFrame.GetBitmap();
                Image oldImage = this.pzpFramePreview.Image;
                if (image == null)
                {
                    this.pzpFramePreview.Image = null;
                    if (oldImage != null)
                    {
                        try { oldImage.Dispose(); }
                        catch { /* ignore */ }
                    }
                    return;
                }
                Int32 matchBpp = pdd.BitsPerPixel;
                Color[] matchPalette = pdd.Colors;
                Color[] loadedColors = this.palPreviewPal.Palette;
                Boolean match = matchPalette != null && loadedColors != null && matchPalette.Length == loadedColors.Length;
                if (match)
                {
                    Int32 amount = loadedColors.Length;
                    for (Int32 i = 0; i < amount; ++i)
                    {
                        match = loadedColors[i].ToArgb() == matchPalette[i].ToArgb();
                        if (match == false)
                            break;
                    }
                }
                if (!match)
                    PalettePanel.InitPaletteControl(pdd.BitsPerPixel, this.palPreviewPal, matchPalette, 200);
                Bitmap[] result = ImageUtils.ImageToFrames(image, image.Width, image.Height, null, null, matchBpp, matchPalette, 0, 0);
                Bitmap bmp = result.Length > 0 ? result[0] : null;
                this.pzpFramePreview.Image = bmp;
                if (oldImage != null)
                {
                    try { oldImage.Dispose(); }
                    catch { /* ignore */ }
                }
            }
            finally
            {
                this.m_Loading = false;
            }
        }

        private static Bitmap[] GetListToAutoSetZoom(SupportedFileType file)
        {
            if (file == null)
                return null;
            if (!file.IsFramesContainer)
                return new Bitmap[] {file.GetBitmap()};
            List<Bitmap> framesToCheck = new List<Bitmap>();
            SupportedFileType[] frames = file.Frames;
            Int32 nrOfFrames;
            if (frames != null && (nrOfFrames = frames.Length) > 0)
            {
                for (Int32 i = 0; i < nrOfFrames; ++i)
                {
                    Bitmap img;
                    if (frames[i] != null && (img = frames[i].GetBitmap()) != null)
                        framesToCheck.Add(img);
                }
            }
            return framesToCheck.ToArray();
        }


        private void CmbPalTypeSelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 bpp = 0;
            String selText = this.cmbPalType.Text;
            if (!String.IsNullOrEmpty(selText))
                bpp = selText[0] - '0';
            PaletteDropDownInfo[] filteredPalettes = this.m_allPalettes.Where(p => p.BitsPerPixel == bpp).ToArray();
            this.cmbPalettes.DataSource = filteredPalettes;
            this.cmbPalettes.SelectedIndex = filteredPalettes.Length > 0 ? 0 : -1;
        }

        private void cmbPalettes_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (this.m_Loading)
                return;
            this.UpdateUiInfo();
        }
        
        private void BtnCancelClick(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnConvertClick(Object sender, EventArgs e)
        {
            PaletteDropDownInfo pdd = this.cmbPalettes.SelectedItem as PaletteDropDownInfo;
            this.MatchBpp = pdd != null ? pdd.BitsPerPixel : 0;
            this.MatchPalette = pdd != null ? pdd.Colors : null;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                if (this.components != null)
                    this.components.Dispose();
                if (this.m_File != null)
                    this.m_File.Dispose();
            }
            base.Dispose(disposing);
        }

        private void FrmFramesToPal_Load(object sender, EventArgs e)
        {

        }

    }
}
