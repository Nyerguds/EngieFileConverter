using System;
using System.Windows.Forms;
using EngieFileConverter.UI;

namespace EngieFileConverter
{
    public static class FileConverter
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static Int32 Run(String[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmFileConverter(args));
            return 0;
        }

    }
}
