/*
 * This file is FREE. This file can freely be copied, edited, mutilated, compiled,
 * printed out and burned in bizarre rituals, or used in supervillain(*) activities.
 * I don't care, as long as you leave this notice(**) when distributing it.
 * 
 * Originally created by Nyerguds.
 * 
 * (*)  Supervillain activities are the ONLY criminal activities for which use of
 *      this code is endorsed by the original author
 * (**) If less than 20% of my original code remains, don't bother.
 */

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Nyerguds.Ini
{
    /// <summary>
    /// This class represents a single section in an ini-file, containing
    /// a set of keys and their values. The class contains methods to convert the
    /// values to and from their intended type when fetching and storing them.
    /// </summary>
    public class IniSection
    {
        /// <summary>The keys read from the ini file</summary>
        private List<string> m_iniKeys;
        /// <summary>Upper case versions of the keys read from the ini file, for quick case-insensitive comparison</summary>
        private List<string> m_iniKeysUpper;
        /// <summary>The values associated to the keys</summary>
        private List<string> m_iniValues;
        /// <summary>A status of which keys' values are read or changed by the program</summary>
        private List<bool> m_iniKeysAccessed;
        /// <summary>A status of which keys' values are changed by the program</summary>
        private List<bool> m_iniKeysChanged;
        ///<summary>The keys removed from the ini file by the program</summary>
        private List<string> m_iniKeysRemoved;
        /// <summary>The name of the ini section</summary>
        private string m_name;
        /// <summary>True to trim all values before retrieving them</summary>
        private bool m_trimValues;

        public bool TrimValues
        {
            get { return m_trimValues; }
            set { m_trimValues = value; }
        }

        /// <summary>Returns the name of this ini section</summary>
        /// <returns>The name of this ini section.</returns>
        public string GetName()
        {
            return m_name;
        }

        /// <summary>Creates a new Ini section object with the specified name</summary>
        /// <param name="name">The name for this ini section.</param>
        public IniSection(string name)
        {
            this.m_name = name;
            this.m_trimValues = false;
            this.m_iniKeys = new List<string>();
            this.m_iniKeysUpper = new List<string>();
            this.m_iniValues = new List<string>();
            this.m_iniKeysChanged = new List<bool>();
            this.m_iniKeysAccessed = new List<bool>();
            this.m_iniKeysRemoved = new List<string>();
        }

        /// <summary>
        /// Internal method to tell a section its reading from the ini file is complete, after which the Accessed and Changed status of each key is reset to False.
        /// </summary>
        internal void ResetStatuses()
        {
            int iniKeyCount = this.m_iniKeys.Count;
            for (int i = 0; i < iniKeyCount; ++i)
            {
                m_iniKeysChanged[i] = false;
                m_iniKeysAccessed[i] = false;
            }
        }

        /// <summary>Gets a String from the ini section</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned, and to 'true' if the value was successfully fetched.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public string GetStringValue(string key, string defaultValue, out bool success)
        {
            return GetStringValue(key, defaultValue, m_trimValues, out success);
        }

        /// <summary>Gets a String from the ini section</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="trimValue">True to trim the retrieved value.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned, and to 'true' if the value was successfully fetched.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public string GetStringValue(string key, string defaultValue, bool trimValue, out bool success)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentException("Key can not be empty");
            int index = m_iniKeysUpper.IndexOf(key.ToUpperInvariant());
            success = index > -1;
            string returnValue;
            if (success)
            {
                m_iniKeysAccessed[index] = true;
                returnValue = m_iniValues[index];
            }
            else
                returnValue = defaultValue;
            if (trimValue && returnValue != null)
                returnValue = returnValue.Trim(' ', '\t');
            return returnValue;
        }

        /// <summary>Sets a String value in the ini section.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        public void SetStringValue(string key, string value)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentException("Key can not be empty");
            string keyUpper = key.ToUpperInvariant();
            int index = m_iniKeysUpper.IndexOf(keyUpper);
            if (index > -1)
            {
                m_iniValues[index] = value;
                m_iniKeysAccessed[index] = true;
                m_iniKeysChanged[index] = true;
            }
            else
            {
                m_iniKeys.Add(key);
                m_iniKeysUpper.Add(keyUpper);
                m_iniValues.Add(value);
                m_iniKeysAccessed.Add(true);
                m_iniKeysChanged.Add(true);
                m_iniKeysRemoved.Remove(keyUpper);
            }
        }

        /// <summary>Removes a key from the ini section.</summary>
        /// <param name="key">The key to remove.</param>
        public void RemoveKey(string key)
        {
            key = key.ToUpperInvariant();
            int index = m_iniKeysUpper.IndexOf(key);
            if (index > -1)
            {
                m_iniKeys.RemoveAt(index);
                m_iniKeysUpper.RemoveAt(index);
                m_iniValues.RemoveAt(index);
                m_iniKeysAccessed.RemoveAt(index);
                m_iniKeysChanged.RemoveAt(index);
            }
            // Mark for deletion even if not found in current ini, for the off chance it's added during the object's lifetime.
            if (!m_iniKeysRemoved.Contains(key))
                m_iniKeysRemoved.Add(key);
        }

        /// <summary>Gets an Integer from the ini section.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public int GetIntValue(string key, int defaultValue, out bool success)
        {
            string value = GetStringValue(key, defaultValue.ToString(), out success);
            if (success)
            {
                try
                {
                    value = SplitOffComment(value)[0];
                    int intvalue = Int32.Parse(value);
                    return intvalue;
                }
                catch (Exception)
                {
                    success = false;
                }
            }
            return defaultValue;
        }

        /// <summary>Sets an Integer value in the ini section.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetIntValue(string key, int value, bool removeComments)
        {
            bool exists;
            string strValue = GetStringValue(key, null, out exists);
            string comment = String.Empty;
            if (exists && !removeComments)
                comment = SplitOffComment(strValue)[1];
            strValue = value.ToString() + comment;
            SetStringValue(key, strValue);
        }

        /// <summary>Gets a Character from the ini section.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public char GetCharValue(string key, char defaultValue, out bool success)
        {
            string value = GetStringValue(key, null, out success);
            if (success && value.Length > 0)
                return value[0];
            else
                success = false;
            return defaultValue;
        }

        /// <summary>Sets a Character value in the ini section.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetCharValue(string key, char value, bool removeComments)
        {
            bool exists;
            string strValue = GetStringValue(key, null, false, out exists);
            string comment = String.Empty;
            if (exists && !removeComments)
            {
                if (strValue.Length > 0)
                    comment = SplitOffComment(strValue.Substring(1))[1];
            }
            strValue = value.ToString() + comment;
            SetStringValue(key, strValue);
        }

        /// <summary>Gets a Boolean from the ini section. Note that the string-to-boolean conversion method actually only checks the first character.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public bool GetBoolValue(string key, bool defaultValue, out bool success)
        {
            string value = GetStringValue(key, defaultValue.ToString(), out success);
            bool returnvalue = defaultValue;
            if (success && value.Length > 0)
            {
                value = SplitOffComment(value)[0];
                value = value.Trim(' ', '\t');
                if (value.Length < 1)
                {
                    success = false;
                    return defaultValue;
                }
                switch (char.ToUpper(value[0]))
                {
                    case 'Y': // yes
                    case 'T': // true
                    case 'A': // aye / active / activated
                    case 'E': // enabled
                        returnvalue = true;
                        break;
                    case 'N': // no / nay
                    case 'F': // false
                    case 'D': // disabled / deactivated
                    case 'I': // inactive
                        returnvalue = false;
                        break;
                    default:
                        try
                        {
                            int intvalue;
                            if (!Int32.TryParse(value, out intvalue))
                            {
                                success = false;
                            }
                            else
                            {
                                if (intvalue == 0)
                                    returnvalue = false;
                                else if (intvalue == 1)
                                    returnvalue = true;
                                else success = false;
                            }
                        }
                        catch
                        {
                            success = false;
                        }
                        break;
                }
            }
            else
            {
                success = false;
            }
            return returnvalue;
        }

        /// <summary>Sets a Boolean value in the ini section, in the chosen boolean save mode.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="booleanmode">The BooleanMode (True/False, Yes/No, 1/0, etc) to use for saving booleans as string.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetBoolValue(string key, bool value, BooleanMode booleanmode, bool removeComments)
        {
            bool exists;
            string strValue = GetStringValue(key, String.Empty, out exists);
            string comment;
            if (exists && !removeComments)
                comment = SplitOffComment(strValue)[1];
            else
                comment = String.Empty;

            switch (booleanmode)
            {
                case BooleanMode.ONE_ZERO:
                    strValue = (value ? "1" : "0"); break;
                case BooleanMode.YES_NO:
                    strValue = (value ? "Yes" : "No"); break;
                case BooleanMode.ENABLED_DISABLED:
                    strValue = (value ? "Enabled" : "Disabled"); break;
                case BooleanMode.ACTIVE_INACTIVE:
                    strValue = (value ? "Active" : "Inactive"); break;
                case BooleanMode.AYE_NAY:
                    strValue = (value ? "Aye" : "Nay"); break;
                default: // includes True/False
                    strValue = value.ToString(); break;
            }
            strValue += comment;
            SetStringValue(key, strValue);
        }

        /// <summary>Gets a Float value from the ini section.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public double GetFloatValue(string key, double defaultValue, out bool success)
        {
            string value = GetStringValue(key, defaultValue.ToString(CultureInfo.InvariantCulture), out success);
            if (!success)
                return defaultValue;
            try
            {
                value = SplitOffComment(value)[0];
                double floatvalue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return floatvalue;
            }
            catch
            {
                return defaultValue;
            }
            
        }

        /// <summary>Sets a Float value in the ini section, with the chosen precision.</summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="precision">Precision for float.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetFloatValue(string key, double value, int precision, bool removeComments)
        {
            bool exists;
            string strValue = GetStringValue(key, String.Empty, out exists);
            precision = Math.Max(0, precision);
            // Don't allow ridiculously long precision
            precision = Math.Min(50, precision);
            string comment = String.Empty;
            double precisionfactor = Math.Pow(10, precision);
            value = Math.Truncate(value * precisionfactor) / precisionfactor;
            if (exists && !removeComments)
                comment = SplitOffComment(strValue)[1];
            strValue = String.Format(CultureInfo.InvariantCulture, "{0:F" + precision + "}", value);
            strValue += comment;
            SetStringValue(key, strValue);
        }

        /// <summary>Splits the comment off the given string value, and returns the two parts in a String array.</summary>
        /// <param name="value">The string to split.</param>
        /// <returns>A 2-element string array with the value as first element and the split off comment as second value.</returns>
        private string[] SplitOffComment(string value)
        {
            int semicolonOffset = value.IndexOf(";", StringComparison.Ordinal);
            string[] returnval = new string[2];
            if (semicolonOffset >= 0)
            {
                int commentOffset = semicolonOffset;
                // add all whitespace to the comment part.
                while (commentOffset > 0 && ((value[commentOffset - 1] == ' ') || (value[commentOffset - 1] == '\t')))
                    commentOffset--;

                returnval[0] = value.Substring(0, commentOffset);
                returnval[1] = value.Substring(commentOffset);
            }
            else
            {
                returnval[0] = value;
                returnval[1] = String.Empty;
            }
            return returnval;
        }

        /// <summary>Removes all keys in the ini section.</summary>
        public void Clear()
        {
            int nrOfKeys = this.m_iniKeysUpper.Count;
            for (int i = 0; i < nrOfKeys; ++i)
            {
                string key = this.m_iniKeysUpper[i];
                if (!this.m_iniKeysRemoved.Contains(key))
                    this.m_iniKeysRemoved.Add(key);
            }
            m_iniKeys.Clear();
            m_iniKeysUpper.Clear();
            m_iniValues.Clear();
            m_iniKeysChanged.Clear();
            m_iniKeysAccessed.Clear();
        }

        /// <summary>Gets all keys from the ini section.</summary>
        /// <returns>A copy of the list of all key names in the ini section.</returns>
        public List<string> GetKeys()
        {
            return new List<string>(m_iniKeys);
        }

        /// <summary>Gets all upper case keys from the ini section.</summary>
        /// <returns>A copy of the list of all upper case key names in the ini section.</returns>
        public List<string> GetUpperCaseKeys()
        {
            return new List<string>(m_iniKeysUpper);
        }

        /// <summary>Returns a copy of the ini section's key-value pairs map.</summary>
        /// <returns>A Dictionary with the key-value pairs.</returns>
        public Dictionary<string, string> GetKeyValuePairs()
        {
            return GetKeyValuePairs(false);
        }

        /// <summary>Returns a copy of the ini section's key-value pairs map.</summary>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A Dictionary with the key-value pairs.</returns>
        public Dictionary<string, string> GetKeyValuePairs(bool upperCaseKeys)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            int iniKeyCount = this.m_iniKeys.Count;
            for (int i = 0; i < iniKeyCount; ++i)
            {
                string key = upperCaseKeys ? m_iniKeysUpper[i] : m_iniKeys[i];
                string value = m_iniValues[i];
                if (value != null && m_trimValues)
                    value = m_iniValues[i].Trim(' ', '\t');
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        /// <summary>Returns a copy of the ini section's Accessed statuses for all keys.</summary>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A Dictionary with the key-value pairs.</returns>
        public Dictionary<string, bool> GetKeyValuePairsAccessed(bool upperCaseKeys)
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            int iniKeyCount = this.m_iniKeys.Count;
            for (int i = 0; i < iniKeyCount; ++i)
                dictionary.Add((upperCaseKeys ? m_iniKeysUpper[i] : m_iniKeys[i]), m_iniKeysAccessed[i]);
            return dictionary;
        }

        /// <summary>Returns a copy of the ini section's Changed statuses for all keys.</summary>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A Dictionary with the key-value pairs.</returns>
        public Dictionary<string, bool> GetKeyValuePairsChanged(bool upperCaseKeys)
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            int iniKeyCount = this.m_iniKeys.Count;
            for (int i = 0; i < iniKeyCount; ++i)
                dictionary.Add((upperCaseKeys ? m_iniKeysUpper[i] : m_iniKeys[i]), m_iniKeysChanged[i]);
            return dictionary;
        }

        /// <summary>Returns a list of upper case versions of the removed keys.</summary>
        /// <returns>A List of Strings.</returns>
        public List<string> GetRemovedKeys()
        {
            return m_iniKeysRemoved;
        }

        /// <summary>Returns the name of the section</summary>
        /// <returns>the name of the section.</returns>
        public override string ToString()
        {
            return this.m_name;
        }
    }
}