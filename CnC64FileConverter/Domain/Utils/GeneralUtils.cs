using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Nyerguds.Util
{
    public static class GeneralUtils
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
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static TEnum TryParseEnum<TEnum>(String value, TEnum defaultValue, Boolean ignoreCase) where TEnum : struct
        {
            if (String.IsNullOrEmpty(value))
                return defaultValue;
            try
            {
                return (TEnum) Enum.Parse(typeof (TEnum), value, ignoreCase);
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        public static String GetAbsolutePath(String relativePath)
        {
            return GetAbsolutePath(null, relativePath);
        }

        public static String GetAbsolutePath(String basePath, String relativePath)
        {
            if (relativePath == null)
                return null;
            if (basePath == null)
                basePath = Path.GetFullPath("."); // quick way of getting current working directory
            else
                basePath = GetAbsolutePath(null, basePath); // to be REALLY sure ;)
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
            String dirPart = path.Substring(0, filenameStart + 1);
            String filePart = path.Substring(filenameStart + 1);
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

        public static T ToBounds<T>(this T value, T min, T max) where T : IComparable
        {
            Comparer<T> comparer = Comparer<T>.Default;
            if (comparer.Compare(value, max) > 0) return max;
            if (comparer.Compare(value, min) < 0) return min;
            return value;
        }

        /// <summary>
        /// For https://stackoverflow.com/q/50407661/395685
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static Int32 DegreesToInt24(Double degrees)
        {
            if(degrees > 180.0 || degrees < -180.0)
                throw new ArgumentOutOfRangeException("degrees");
            const Int32 bottom = 0x058730;
            const Int32 range = 0x4F1A0;
            return (Int32)((degrees + 180.0) / 360.0 * range) + bottom;
        }

    }

    /// <summary>
    ///  Helper class for fetching data from custom attributes on properties.
    ///  Inspired by https://stackoverflow.com/a/50247942/395685
    /// </summary>
    /// <typeparam name="T">Class in which the property is located</typeparam>
    /// <typeparam name="TAttr">Attribute type to fetch</typeparam>
    public static class PropertyCustomAttributeUtil<T, TAttr> where TAttr : Attribute
    {
        /// <summary>
        ///  Get an attribute value from a specific property of a class
        /// </summary>
        /// <typeparam name="TProp">Result type of the property.</typeparam>
        /// <typeparam name="TRes">Result type of the attrExpression.</typeparam>
        /// <param name="expression">Expression specifying the class property.</param>
        /// <param name="attrExpression">Expression specifying the property to return from the attribute.</param>
        /// <returns></returns>
        public static TRes GetValue<TProp, TRes>(Expression<Func<T, TProp>> expression, Func<TAttr, TRes> attrExpression)
        {
            // example: AttrPropertyType attrProp = PropertyCustomAttributeUtil<MyClass, MyAttribute>.GetValue(cl => cl.MyProperty, attr => attr.AttrProperty);
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentNullException();
            Object[] attrs = memberExpression.Member.GetCustomAttributes(typeof(TAttr), true);
            if (attrs.Length == 0)
                return default(TRes);
            return attrExpression((TAttr)attrs[0]);
        }
    }

}