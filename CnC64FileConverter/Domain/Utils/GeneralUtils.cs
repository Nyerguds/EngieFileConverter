using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nyerguds.Util
{
    public class GeneralUtils
    {
        public static Boolean IsNumeric(String str)
        {
            foreach (Char c in str)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }


        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// If the value is null or the parse fails, the default is False.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T</returns>
        public static Boolean IsTrueValue(String value)
        {
            return IsTrueValue(value, false);
        }
        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="defaultVal">Default value to return in case parse fails</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T</returns>
        public static Boolean IsTrueValue(String value, Boolean defaultVal)
        {
            if (String.IsNullOrEmpty(value))
                return defaultVal;
            return Regex.IsMatch(value, "^(([TJYO].*)|(0*1))$", RegexOptions.IgnoreCase);
        }

        public static Boolean IsHexadecimal(String str)
        {
            return Regex.IsMatch(str, "^[0-9A-F]*$", RegexOptions.IgnoreCase);
        }

        public static String GetApplicationPath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        public static TEnum TryParseEnum<TEnum>(String value, TEnum defaultValue, Boolean ignoreCase) where TEnum : struct
        {
            if (String.IsNullOrEmpty(value))
                return defaultValue;
            try { return (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase); }
            catch (ArgumentException) { return defaultValue; }
        }

        public static String GetAbsolutePath(String relativePath, String basePath)
        {
            if (relativePath == null)
                return null;
            if (basePath == null)
                basePath = Path.GetFullPath("."); // quick way of getting current working directory
            else
                basePath = GetAbsolutePath(basePath, null); // to be REALLY sure ;)
            String path;
            // specific for windows paths starting on \ - they need the drive added to them.
            // I constructed this piece like this for possible Mono support.
            if (!Path.IsPathRooted(relativePath) || "\\".Equals(Path.GetPathRoot(relativePath)))
            {
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    path = Path.Combine(Path.GetPathRoot(basePath), relativePath.TrimStart(Path.DirectorySeparatorChar));
                else
                    path = Path.Combine(basePath, relativePath);
            }
            else
                path = relativePath;
            // resolves any internal "..\" to get the true full path.

            Int32 filenameStart = path.LastIndexOf(Path.DirectorySeparatorChar);
            String dirPart = path.Substring(0, filenameStart+1);
            String filePart = path.Substring(filenameStart+1);
            if (filePart.Contains("*") || filePart.Contains("?"))
            {
                dirPart = Path.GetFullPath(dirPart);
                return Path.Combine(dirPart, filePart);
            }
            return Path.GetFullPath(path);
        }

        public static String ProgramVersion()
        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            //Version v = AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version;
            String version = String.Format("v{0}.{1}", ver.FileMajorPart, ver.FileMinorPart);
            if (ver.FileBuildPart > 0)
                version += "." + ver.FileBuildPart;
            if (ver.FilePrivatePart > 0)
                version += "." + ver.FilePrivatePart;
            return version;
        }

        public static String DoubleFirstAmpersand(String input)
        {
            if (input == null)
                return null;
            Int32 index = input.IndexOf('&');
            if (index == -1)
                return input;
            return input.Substring(0, index) + '&' + input.Substring(index);
        }
    }
}
