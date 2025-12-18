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
        /// <param name="value">String to parse.</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T.</returns>
        public static Boolean IsTrueValue(String value)
        {
            return IsTrueValue(value, false);
        }

        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// </summary>
        /// <param name="value">String to parse.</param>
        /// <param name="defaultVal">Default value to return in case parse fails.</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T.</returns>
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
            return new StringBuilder().AppendNumbersGrouped(numbers).ToString();
        }

        /// <summary>
        /// Groups numbers into ranges and writes the result to a given StringBuilder. Example: [1,2,3,5,7,8,9] becomes "1-3, 5, 7-9".
        /// Duplicates in the given numbers range are ignored.
        /// </summary>
        /// <param name="sb">String builder to write the result to.</param>
        /// <param name="numbers">The array of numbers.</param>
        /// <returns>The given string builder arg, for convenience for further appending.</returns>
        /// <remarks>
        /// Designed to work for all integer types, though it will overflow on UInt64 values larger than Int64.MaxValue.</remarks>
        public static StringBuilder AppendNumbersGrouped<T>(this StringBuilder sb, IEnumerable<T> numbers) where T : IComparable, IConvertible
        {
            return AppendNumbersGrouped(sb, numbers, "-", ", ");
        }

        /// <summary>
        /// Groups numbers into ranges and writes the result to a given StringBuilder. Example: [1,2,3,5,7,8,9] becomes "1-3, 5, 7-9".
        /// Duplicates in the given numbers range are ignored.
        /// </summary>
        /// <param name="sb">String builder to write the result to.</param>
        /// <param name="numbers">The array of numbers.</param>
        /// <param name="rangeSeparator">String to put between two numbers in a range, like the '-' in "1-3".</param>
        /// <param name="groupsSeparator">String to put between two groups, like the ', ' in "1-5, 9-10".</param>
        /// <returns>The given string builder arg, for convenience for further appending.</returns>
        /// <remarks>
        /// Designed to work for all integer types, though it will overflow on UInt64 values larger than Int64.MaxValue.</remarks>
        public static StringBuilder AppendNumbersGrouped<T>(this StringBuilder sb, IEnumerable<T> numbers, String rangeSeparator, String groupsSeparator) where T : IComparable, IConvertible
        {
            T[] numbersArr = numbers.Distinct().OrderBy(x => x).ToArray();
            Int64 len = numbersArr.LongLength;
            Int64 index = 0;
            while (index < len)
            {
                if (index > 0)
                    sb.Append(groupsSeparator);
                T cur = numbersArr[index];
                Int64 curIndex = index;
                sb.Append(cur);
                while (index + 1 < len && numbersArr[index].ToInt64(CultureInfo.InvariantCulture) + 1 == numbersArr[index + 1].ToInt64(CultureInfo.InvariantCulture))
                    index++;
                if (index > curIndex)
                    sb.Append(rangeSeparator).Append(numbersArr[index]);
                index++;
            }
            return sb;
        }

        /// <summary>
        /// Converts grouped positive numbers into an array of integers. Example: "1-3, 5, 7-9" becomes [1,2,3,5,7,8,9].
        /// Duplicates in the given numbers range are ignored.
        /// </summary>
        /// <param name="input">A comma-separated list of positive numbers and number ranges.</param>
        /// <returns>An array of distinct integers.</returns>
        public static Int32[] GetRangedNumbers(String input)
        {
            return GetRangedNumbers(input, "-", ",");
        }

        /// <summary>
        /// Converts grouped positive numbers into an array of integers. Example: "1-3, 5, 7-9" becomes [1,2,3,5,7,8,9].
        /// Duplicates in the given numbers range are ignored.
        /// </summary>
        /// <param name="input">A comma-separated list of positive numbers and number ranges.</param>
        /// <param name="rangeSeparator">String put between two numbers in a range, like the '-' in "1-3".</param>
        /// <param name="groupsSeparator">String put between two groups, like the ',' in "1-5, 9-10". Spaces are trimmed off both this string and the split results.</param>
        /// <returns>An array of integers.</returns>
        public static Int32[] GetRangedNumbers(String input, String rangeSeparator, String groupsSeparator)
        {
            if (String.IsNullOrEmpty(input))
                return new Int32[0];
            Char[] trimVals = " \t\r\n".ToCharArray();
            input = input.Trim(trimVals);
            if (input.Length == 0)
                return new Int32[0];
            groupsSeparator = groupsSeparator.Trim();
            String[] parts = input.Split(new String[] { groupsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            List<Int32> numbers = new List<Int32>();
            foreach (String part in parts)
            {
                String edPart = part.Trim(trimVals);
                if (edPart.Length == 0)
                    continue;
                // Unlike a simple Split, the use of regex allows the use of negative values if the range splitter is "-".
                Regex split = new Regex("^(-?\\d+)\\s*" + Regex.Escape(rangeSeparator) + "\\s*(-?\\d+)$");
                Match m = split.Match(edPart);
                if (m.Success)
                {
                    Int32 val1 = Int32.Parse(m.Groups[1].Value);
                    Int32 val2 = Int32.Parse(m.Groups[2].Value);
                    Int32 lowest = Math.Min(val1, val2);
                    Int32 highest = Math.Max(val1, val2);
                    numbers.AddRange(Enumerable.Range(lowest, highest - lowest + 1));
                }
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

    }

}