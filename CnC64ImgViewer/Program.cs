using CnC64ImgViewer.Domain;
using ColorManipulation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CnC64ImgViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(String[] args)
        {
            if (args.Length == 2)
            {
                ConvertImage(args[0], args[1]);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmCnC64ImgViewer(args));
        }

        private static void ConvertImage(String input, String output)
        {
            String inputFull = GeneralUtils.GetAbsolutePath(input, null);
            String outputFull = GeneralUtils.GetAbsolutePath(output, null);

            if (File.Exists(inputFull))
            {
                try
                {
                    Byte[] data = File.ReadAllBytes(inputFull);
                    ImgFile img = ImgFile.LoadFromFileData(data);
                    if (img == null)
                        return;
                    Bitmap bm = img.GetBitmap();
                    ImageUtils.SaveImage(bm, outputFull);
                }
                catch (Exception e) { Console.WriteLine(e.Message); /* failed? Whatevs.*/ }
            }
        }
    }
}
