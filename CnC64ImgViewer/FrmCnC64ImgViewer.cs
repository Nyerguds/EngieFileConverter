using CnC64ImgViewer.Domain;
using ColorManipulation;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CnC64ImgViewer
{
    public partial class FrmCnC64ImgViewer : Form
    {
        private String filename;
        private ImgFile rawImage;
        private Color backgroundFillColor = Color.Fuchsia;
        private Int32[] customcolors;


        public FrmCnC64ImgViewer()
        {
            InitializeComponent();
            picImage.BackColor = backgroundFillColor;
            lblTransparentColorVal.BackColor = backgroundFillColor;
        }

        public FrmCnC64ImgViewer(string[] args) : this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                filename = args[0];
        }



        private void Frm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Frm_DragDrop(object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                String path = files[0];
                String ext = Path.GetExtension(path);
                if (".img".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    LoadImage(path);
            }
        }

        private void LoadImage(String path)
        {
            filename = path;
            Byte[] data = File.ReadAllBytes(path);
            rawImage = ImgFile.LoadFromFileData(data);
            if (rawImage == null)
            {
                picImage.Image = null;
                return;
            }
            lblValFilename.Text = Path.GetFileName(filename);
            lblValWidth.Text = rawImage.Width.ToString();
            lblValHeight.Text = rawImage.Height.ToString();
            lblValBytesPerCol.Text = rawImage.BytesPerColor.ToString();
            lblValColorFormat.Text = rawImage.ColorFormat > 2 ? "Unknown" : ("[" + rawImage.ColorFormat + "] " + rawImage.GetBpp() + " BPP" + (rawImage.ColorFormat < 2? " (paletted)" : String.Empty));
            lblValUnknVal.Text = rawImage.Unknown3.ToString();
            lblValColorsInPal.Text = rawImage.ColorsInPalette.ToString();
            lblValImageData.Text = (rawImage.ImageData != null ? rawImage.ImageData.Length : 0).ToString() + " bytes";
            lblValPaletteData.Text = (rawImage.PaletteData != null && rawImage.PaletteOffset != 0 ? rawImage.PaletteData.Length : 0).ToString() + " bytes";
            Bitmap bm = rawImage.GetBitmap();
            picImage.Image = bm;
            RefreshImage();
            Boolean loadOk = bm != null;
            btnSave.Enabled = loadOk;
            if (loadOk)
                btnSave.Focus();

        }

        private String GetColorFormatInfo(ImgFile image)
        {
            return rawImage.ColorFormat > 2 ? "Unknown" : (rawImage.ColorFormat + ": " + rawImage.GetBpp() + "BPP" + (rawImage.ColorFormat < 2? " (paletted)" : String.Empty));
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            /*/
            if (picImage.Image == null || filename == null)
                return;
            String saveName = filename;
            if (filename.EndsWith(".img", StringComparison.InvariantCultureIgnoreCase))
                saveName = saveName.Substring(0, saveName.Length - 4);
            saveName += ".png";
            ImageUtils.SaveImage((Bitmap)picImage.Image, saveName);
            //*/
        }

        private void FrmCnC64ImgViewer_Shown(object sender, EventArgs e)
        {
            if (filename != null)
                LoadImage(filename);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (picImage.Image == null)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = String.IsNullOrEmpty(filename) ? Path.GetFullPath(".") : Path.GetDirectoryName(filename);
            sfd.FileName = Path.GetFileNameWithoutExtension(filename) + ".png";
            sfd.Filter = "PNG image (*.png)|*.png|Bitmap image|*.bmp|CompuServe GIF image|*.gif|JPEG. Gods, why would you do that?|*.jpg";
            sfd.Title = "Specify save path";
            DialogResult res = sfd.ShowDialog(this);
            if (res != System.Windows.Forms.DialogResult.OK)
                return;
            ImageUtils.SaveImage(rawImage.GetBitmap(), sfd.FileName);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "N64 Image Files (*.img)|*.img|All Files (*.*)|*.*";
            ofd.InitialDirectory = String.IsNullOrEmpty(filename) ? Path.GetFullPath(".") : Path.GetDirectoryName(filename);
            DialogResult res = ofd.ShowDialog(this);
            if (res != System.Windows.Forms.DialogResult.OK)
                return;
            LoadImage(ofd.FileName);
        }

        private void numZoom_ValueChanged(object sender, EventArgs e)
        {
            RefreshImage();
        }
        
        private void RefreshImage()
        {
            Bitmap bm = (Bitmap)picImage.Image;
            Boolean loadOk = bm != null;
            picImage.Visible = loadOk;
            picImage.Width = loadOk ? bm.Width * (Int32)numZoom.Value : 100;
            picImage.Height = loadOk ? bm.Height * (Int32)numZoom.Value : 100;
            picImage.BackColor = loadOk && ImageUtils.HasTransparency(bm) ? backgroundFillColor : SystemColors.Control;
        }

        private void picImage_Click(object sender, EventArgs e)
        {
            pnlImageScroll.Focus();
        }

        private void lblTransparentColorVal_Click(object sender, EventArgs e)
        {
            ColorDialog cdl = new ColorDialog();
            cdl.Color = this.backgroundFillColor;
            cdl.FullOpen = true;
            cdl.CustomColors = this.customcolors;
            DialogResult res = cdl.ShowDialog();
            customcolors = cdl.CustomColors;
            if (res == DialogResult.OK || res == DialogResult.Yes)
            {
                this.backgroundFillColor = cdl.Color;
                lblTransparentColorVal.BackColor = backgroundFillColor;
                numZoom_ValueChanged(null, null);
            }
        }

    }
}
