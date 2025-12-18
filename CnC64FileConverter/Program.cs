using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CnC64FileConverter
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static Int32 Main(String[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            return CnC64ImgConverter.Run(args);
        }

        /// <summary>
        ///  Code to load embedded dll file. This is called when loading of an assembly fails. If the dll is embedded, the problem is resolved and the dll is loaded this way.
        ///  Based on http://stackoverflow.com/a/6362414/395685
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">A System.ResolveEventArgs that contains the event data.</param>
        /// <returns>The System.Reflection.Assembly that resolves the type, assembly, or resource; or null if the assembly cannot be resolved.</returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            String dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources"))
                return null;
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(typeof(Program).Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
            Byte[] dllBytes = rm.GetObject(dllName) as Byte[];
            return dllBytes == null ? null : System.Reflection.Assembly.Load(dllBytes);
        }
    }
}
