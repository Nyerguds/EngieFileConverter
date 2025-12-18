using CnC64ImgViewer.Domain;
using ColorManipulation;
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

        public FrmCnC64ImgViewer()
        {
            InitializeComponent();
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
            ImgFile img = ImgFile.LoadFromFileData(data);
            if (img == null)
            {
                pictureBox1.Image = null;
                return;
            }
            Bitmap bm = img.GetBitmap();
            pictureBox1.Image = bm;
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null && filename != null)
            {
                String saveName = filename;
                if (filename.EndsWith(".img", StringComparison.InvariantCultureIgnoreCase))
                    saveName = saveName.Substring(0, saveName.Length - 4);
                saveName += ".png";
                ImageUtils.SaveImage((Bitmap)pictureBox1.Image, saveName);
            }
        }

        private void FrmCnC64ImgViewer_Shown(object sender, EventArgs e)
        {
            if (filename != null)
                LoadImage(filename);
        }

    }
}
