using CnC64ImgViewer.Domain;
using ColorManipulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CnC64ImgViewer
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static Int32 Main(String[] args)
        {
            //if (AttachConsole(-1))
            //    Console.ReadKey();
            if (args.Length > 1)
                return ConvertImage(args);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmCnC64ImgViewer(args));
            return 0;
        }

        [DllImport("Kernel32.dll")]
        public static extern Boolean AttachConsole(int processId);


        private static Int32 ConvertImage(String[] args)
        {            
            Boolean hasconsole = AttachConsole(-1);
            Boolean showErrors = true;
            Boolean showFeedback = false;
            Int32 i;
            for(i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("/"))
                    break;
                if (String.Equals(args[i], "/Q", StringComparison.InvariantCultureIgnoreCase))
                    showErrors = false;
                if (showErrors && String.Equals(args[i], "/V", StringComparison.InvariantCultureIgnoreCase))
                    showFeedback = true;
            }
            if (args.Length - i < 2)
            {
                if (hasconsole && showErrors)
                    Console.WriteLine("Insufficient parameters!");
            }
            String input = args[i];
            String output = args[i+1];
            String inputFull = GeneralUtils.GetAbsolutePath(input, null);
            String outputFull = GeneralUtils.GetAbsolutePath(output, null);
            String feedback = String.Format("Converting file \"{0}\" to \"{1}\"... ", Path.GetFileName(inputFull), Path.GetFileName(outputFull));
            if (hasconsole && showFeedback)
                Console.Write(feedback);
            if (!File.Exists(inputFull))
            {
                if (hasconsole && showErrors)
                    Console.WriteLine(String.Format("Error: File not found \"{0}\"", inputFull));
                return 1;
            }
            try
            {
                Byte[] data = File.ReadAllBytes(inputFull);
                ImgFile img = new ImgFile(data);
                if (img == null)
                    return 0;
                Bitmap bm = img.GetBitmap();
                ImageUtils.SaveImage(bm, outputFull);
                if (hasconsole && showFeedback)
                    Console.WriteLine("Done.");
            }
            catch (Exception e)
            {
                if (hasconsole && showErrors)
                {
                    if (!showFeedback)
                        Console.Write(feedback);
                    Console.WriteLine("Error: " + e.Message);
                }
                
                return 1;
            }
            return 0;
        }
    }
}
