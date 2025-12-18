using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        /// <summary>
        /// Groups numbers into ranges and returns  the result as String. Example: [1,2,3,5,7,8,9] becomes "1-3, 5, 7-9".
        /// Duplicates in the given numbers range are ignored.
        /// </summary>
        /// <param name="numbers">The array of numbers.</param>
        /// <returns>The resulting String.</returns>
        /// <remarks>Designed to work for all integer types, though it will overflow on UInt64 values larger than Int64.MaxValue.</remarks>
        public static String GroupNumbers<T>(IEnumerable<T> numbers) where T : IComparable, IConvertible
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendNumbersGrouped(numbers);
            return sb.ToString();
        }

        /// <summary>
        /// Groups numbers into ranges and writes the result to a given StringBuilder. Example: [1,2,3,5,7,8,9] becomes "1-3, 5, 7-9".
        /// Duplicates in the given numbers range are ignored.
        /// </summary>
        /// <param name="sb">String builder to write the result to.</param>
        /// <param name="numbers">The array of numbers.</param>
        /// <returns>The given string builder arg, for convenience for further appending.</returns>
        /// <remarks>Designed to work for all integer types, though it will overflow on UInt64 values larger than Int64.MaxValue.</remarks>
        public static StringBuilder AppendNumbersGrouped<T>(this StringBuilder sb, IEnumerable<T> numbers) where T : IComparable, IConvertible
        {
            T[] numbersArr = numbers.Distinct().OrderBy(x => x).ToArray();
            Int64 len = numbersArr.LongLength;
            Int64 index = 0;
            while (index < len)
            {
                if (index > 0)
                    sb.Append(", ");
                T cur = numbersArr[index];
                Int64 curIndex = index;
                sb.Append(cur);
                while (index + 1 < len && numbersArr[index].ToInt64(CultureInfo.InvariantCulture) + 1 == numbersArr[index + 1].ToInt64(CultureInfo.InvariantCulture))
                    index++;
                if (index > curIndex)
                    sb.Append("-").Append(numbersArr[index]);
                index++;
            }
            return sb;
        }

        public static Int32[] GetRangedNumbers(String input)
        {
            if (String.IsNullOrEmpty(input))
                return new Int32[0];
            Char[] trimVals = ",- \t".ToCharArray();
            input = input.Trim(trimVals);
            if (input.Length == 0)
                return new Int32[0];
            String[] parts = input.Split(new Char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            List<Int32> numbers = new List<Int32>();
            foreach (String part in parts)
            {
                String edPart = part.Trim(trimVals);
                if (edPart.Length == 0)
                    continue;
                String[] range = edPart.Split(new Char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                List<Int32> rangeNumbers = new List<Int32>();
                foreach (String rangePart in range)
                {
                    String edRangePart = rangePart.Trim(trimVals);
                    Int32 num;
                    if (Int32.TryParse(edRangePart, out num))
                        rangeNumbers.Add(num);
                }
                Int32 lowest = rangeNumbers.Min();
                Int32 highest = rangeNumbers.Max();
                numbers.AddRange(Enumerable.Range(lowest, highest - lowest + 1));
            }
            return numbers.Distinct().ToArray();
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