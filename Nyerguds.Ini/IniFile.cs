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
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Nyerguds.Ini
{
    /// <summary>
    /// <p>This class represents a standard type ini file, which uses sections specified
    /// by a string between square brackets, and key-value pairs separated by the
    /// "equal" sign without any spaces padding the separator.</p>
    /// <p>Example:</p>
    /// <p>[Section]
    /// <br />Key1=value1
    /// <br />Key2=value2</p>
    /// <p>Keys and section names are treated as case insensitive, but the case used
    /// when first reading or creating a key or section will be preserved. For
    /// compatibility reasons, it is possible to force the ini system to write
    /// all of its keys with an upper case starting letter.</p>
    /// <p>Lines starting with a semicolon are seen as comments, and
    /// ignored by the reading system. For non-string data, there is a function to
    /// preserve comments put behind a value. There is no functionality for editing
    /// comments though.</p>
    /// </summary>
    public class IniFile
    {

        #region defaults
        public static BooleanMode DEFAULT_BOOLEANMODE = BooleanMode.YES_NO;
        public static bool DEFAULT_REMOVECOMMENTS = false;
        public static int DEFAULT_DOUBLEPRECISION = 6;
        public static WriteMode DEFAULT_WRITEMODE = WriteMode.WRITE_ALL_ACCESSED;
        public static bool DEFAULT_INITIALCAPS = true;
        public static Encoding DEFAULT_ENCODING = new UTF8Encoding(false);
        public static bool DEFAULT_TRIMVALUES = false;

        /// <summary>DOS U.S. ASCII-437 encoding; the standard encoding for the ini files of most DOS games.</summary>
        public static Encoding ENCODING_DOS_US = Encoding.GetEncoding(437);

        #endregion

        #region global variables
        protected List<IniSection> m_IniSections;
        protected List<string> m_RemovedSections;

        protected BooleanMode m_BooleanMode = DEFAULT_BOOLEANMODE;
        protected bool m_RemoveComments = DEFAULT_REMOVECOMMENTS;
        protected int m_DoublePrecision = DEFAULT_DOUBLEPRECISION;
        protected WriteMode m_WriteMode = DEFAULT_WRITEMODE;
        protected bool m_InitialCaps = DEFAULT_INITIALCAPS;
        protected Encoding m_Encoding = DEFAULT_ENCODING;
        protected bool m_TrimValues = DEFAULT_TRIMVALUES;

        protected string m_FilePath;
        protected string m_FileContents;

        #endregion

        /// <summary>Determines how booleans are saved back into the ini file.</summary>
        public BooleanMode BooleanWriteMode
        {
            get { return this.m_BooleanMode; }
            set { this.m_BooleanMode = value; }
        }

        /// <summary>Remove comments when writing non-string values to ini. If False, comments behind the values are preserved.</summary>
        public bool RemoveComments
        {
            get { return this.m_RemoveComments; }
            set { this.m_RemoveComments = value; }
        }

        /// <summary>The number of digits behind the decimal point to write when saving floating point values.</summary>
        public int DoublePrecision
        {
            get { return this.m_DoublePrecision; }
            set { this.m_DoublePrecision = value; }
        }

        /// <summary>Determines which keys are written and removed in the target file when saving.</summary>
        public WriteMode WriteBackMode
        {
            get { return this.m_WriteMode; }
            set { this.m_WriteMode = value; }
        }

        /// <summary>When enabled, this makes sure all ini keys that get saved start with a capital letter.</summary>
        public bool InitialCaps
        {
            get { return this.m_InitialCaps; }
            set { this.m_InitialCaps = value; }
        }

        /// <summary>The character encoding to be used by the IniFile object for I/O operations.</summary>
        public Encoding CharacterEncoding
        {
            get { return this.m_Encoding; }
            set { this.m_Encoding = value; }
        }

        /// <summary>When enabled, this makes sure all retrieved values are trimmed.</summary>
        public bool TrimValues
        {
            get { return this.m_TrimValues; }
            set { this.m_TrimValues = value; }
        }

        /// <summary>The path to use as input and output file.</summary>
        public string FilePath
        {
            get { return this.m_FilePath; }
            set { this.m_FilePath = value; }
        }

        /// <summary>Retrieves the virtual file contents of the ini object. This can be used to retrieve ini text data after saving when the ini was originally given as content string without save path. Will be null if the file was read from disk.</summary>
        public string FileContents
        {
            get { return this.m_FileContents; }
        }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file.
        /// </summary>
        /// <param name="filePath">Path of the file to read.</param>
        public IniFile(string filePath)
            : this(filePath, DEFAULT_INITIALCAPS, DEFAULT_ENCODING, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file.
        /// </summary>
        /// <param name="filePath">Path of the file to read.</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file.</param>
        public IniFile(string filePath, Encoding textEncoding)
            : this(filePath, DEFAULT_INITIALCAPS, textEncoding, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file.
        /// </summary>
        /// <param name="filePath">Path of the file to read.</param>
        /// <param name="initialCaps">Write back all ini keys with initial capital letter.</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file.</param>
        /// <param name="trimValues">True to trim any retrieved values.</param>
        public IniFile(string filePath, bool initialCaps, Encoding textEncoding, bool trimValues)
        {
            if (textEncoding == null) throw new ArgumentNullException("textEncoding");
            if (filePath == null) throw new ArgumentNullException("filePath");
            this.m_RemovedSections = new List<string>();
            this.m_InitialCaps = initialCaps;
            this.m_TrimValues = trimValues;
            this.ReadIniFile(filePath, textEncoding);
        }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file
        ///     that doesn't necessarily exist yet.
        /// </summary>
        /// <param name="filePath">Path to write the file to when saving.</param>
        /// <param name="filecontents">String with the file contents in it.</param>
        public IniFile(string filePath, string filecontents)
            : this(filePath, filecontents, DEFAULT_INITIALCAPS, DEFAULT_ENCODING, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file
        ///     that doesn't necessarily exist yet.
        /// </summary>
        /// <param name="filePath">Path to write the file to when saving.</param>
        /// <param name="filecontents">String with the file contents in it.</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file.</param>
        public IniFile(string filePath, string filecontents, Encoding textEncoding)
            : this(filePath, filecontents, DEFAULT_INITIALCAPS, textEncoding, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file
        ///     that doesn't necessarily exist yet.
        /// </summary>
        /// <param name="filePath">Path to write the file to when saving.</param>
        /// <param name="filecontents">String with the file contents in it.</param>
        /// <param name="initialCaps">Write back all ini keys with initial capital letter.</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file.</param>
        /// <param name="trimValues">Trim all values on read / write.</param>
        public IniFile(string filePath, string filecontents, bool initialCaps, Encoding textEncoding, bool trimValues)
        {
            this.m_FilePath = filePath;
            this.m_Encoding = textEncoding;
            this.m_RemovedSections = new List<string>();
            this.m_InitialCaps = initialCaps;
            this.m_TrimValues = trimValues;
            this.m_FileContents = filecontents.Replace("\r\n", "\n").Replace('\r', '\n');
            ReadOnlyCollection<string> initext = new List<string>(filecontents.Split('\n')).AsReadOnly();
            this.m_IniSections = this.ReadIniContents(initext);
        }

        /// <summary>
        /// Sets the path for the ini file to a new string; and reads that file.
        /// This function does a complete reset of the object's data.
        /// </summary>
        /// <param name="iniFilePath">Path of the file to read.</param>
        /// <param name="charEncoding">Character encoding to use.</param>
        protected void ReadIniFile(string iniFilePath, Encoding charEncoding)
        {
            this.m_FilePath = iniFilePath;
            this.m_Encoding = charEncoding;
            this.m_RemovedSections.Clear();
            this.m_IniSections = null;
            if (File.Exists(this.m_FilePath))
            {
                using (StreamReader stream = new StreamReader(this.m_FilePath, this.m_Encoding, false))
                {
                    ReadOnlyCollection<string> initext = this.ReadLinesFromTextStream(stream, charEncoding).AsReadOnly();
                    this.m_IniSections = this.ReadIniContents(initext);
                }
            }
            if (this.m_IniSections == null)
                this.m_IniSections = new List<IniSection>();
        }

        /// <summary>Reads the ini contents of a stream, and returns it as a list of ini sections.</summary>
        /// <param name="initext">Read-only collection of strings to read the ini data from.</param>
        /// <returns>A List of IniSection objects with the read data.</returns>
        protected List<IniSection> ReadIniContents(ReadOnlyCollection<string> initext)
        {
            List<IniSection> readIniSections = new List<IniSection>();
            if (initext == null)
                return readIniSections;
            try
            {
                IniSection iniSection = null;
                int initextLen = initext.Count;
                for (int i = 0; i < initextLen; ++i)
                {
                    string input = initext[i];
                    if (input.StartsWith("[") && input.Contains("]"))
                    {
                        string sectionName = input.Substring(1, input.IndexOf("]", StringComparison.Ordinal) - 1);
                        if (!sectionName.Contains("[")) // valid ini section
                        {
                            iniSection = null;
                            int sectionIndex = -1;
                            int sectionCount = readIniSections.Count;
                            for (int j = 0; j < sectionCount; ++j)
                            {
                                IniSection testsec = readIniSections[j];
                                if (testsec.GetName().Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                                    sectionIndex = j;
                            }
                            if (sectionIndex > -1)
                            {
                                iniSection = readIniSections[sectionIndex];
                            }
                            if (iniSection == null) // doesn't exist yet
                            {
                                iniSection = new IniSection(sectionName);
                                readIniSections.Add(iniSection);
                            }
                            else // section already exists; don't allow merging of different same-name sections. (needed for correct deleting of extra ini entries)
                                iniSection = null;
                        }
                    }
                    else if (iniSection != null) // ini section was found (everything before first ini section is ignored)
                    {
                        string[] keyValue = this.GetKeyAndValue(input);
                        if (keyValue != null && keyValue.Length == 2)
                            iniSection.SetStringValue(keyValue[0], keyValue[1]);
                    }
                }
            }
            catch (Exception) { /* ignore */ }
            // clear all Accessed and Modified statuses, since this is the initial read.
            int nrOfSections = readIniSections.Count;
            for (int i = 0; i < nrOfSections; ++i)
            {
                IniSection section = readIniSections[i];
                section.ResetStatuses();
                section.TrimValues = this.m_TrimValues;
            }
            return readIniSections;
        }

        /// <summary>
        /// Writes the modified ini object to the set FilePath, using the set encoding.
        /// Note that the writing system does not technically overwrite the target file; it just fills in the data, adds new sections, and adds
        /// and removes keys to adjust the file to the edits made in the stored data. Unknown sections and comments in the file are left untouched.
        /// Note that unknown keys in known sections are only removed if WriteBackMode is WRITE_ALL. The other modes only remove explicitly removed keys.
        /// </summary>
        /// <returns>True if the save operation succeeded.</returns>
        public bool WriteIni()
        {
            return this.WriteIni(this.m_FilePath, this.m_Encoding);
        }

        /// <summary>
        /// Writes the modified ini object to a file.
        /// </summary>
        /// <param name="iniFilePath">Filename to write to.</param>
        /// <param name="charEncoding">Character encoding to use.</param>
        /// <returns>True if the save operation succeeded.</returns>
        public bool WriteIni(string iniFilePath, Encoding charEncoding)
        {
            List<string> initext;
            if (iniFilePath == null && !String.IsNullOrEmpty(this.m_FileContents))
            {
                initext = new List<string>(this.m_FileContents.Split('\n'));
            }
            else if (File.Exists(iniFilePath))
            {
                using (StreamReader stream = new StreamReader(iniFilePath, charEncoding, false))
                {
                    initext = this.ReadLinesFromTextStream(stream, charEncoding);
                }
            }
            else
            {
                initext = new List<string>();
            }
            int nrOfSections = this.m_IniSections.Count;
            for (int i = 0; i < nrOfSections; ++i)
            {
                IniSection section = this.m_IniSections[i];
                // writes keys in original case
                Dictionary<string, string> keypairs = section.GetKeyValuePairs();
                Dictionary<string, bool> keypairsAccessed = section.GetKeyValuePairsAccessed(false);
                Dictionary<string, bool> keypairsChanged = section.GetKeyValuePairsChanged(false);
                string sectionName = section.GetName();
                foreach (KeyValuePair<string, string> iniPair in keypairs)
                {
                    string newline = iniPair.Key;
                    if (this.m_WriteMode == WriteMode.WRITE_ALL
                        || ((this.m_WriteMode == WriteMode.WRITE_ALL_ACCESSED) && keypairsAccessed[newline])
                        || ((this.m_WriteMode == WriteMode.WRITE_MODIFIED_ONLY) && keypairsChanged[newline]))
                    {
                        if (this.m_InitialCaps)
                            newline = char.ToUpper(newline[0]) + newline.Substring(1, newline.Length - 1);
                        newline += "=" + iniPair.Value;
                        int linenumber = this.FindLine(initext, sectionName, iniPair.Key);
                        if (linenumber >= 0)
                            initext[linenumber] = newline;
                        else
                        {
                            linenumber = this.FindLastSectionLine(initext, sectionName, false) + 1;
                            if (linenumber > 0)
                                initext.Insert(linenumber, newline);
                            else
                            {
                                if (initext.Count > 0)
                                    initext.Add(String.Empty);
                                initext.Add("[" + sectionName + "]");
                                initext.Add(newline);
                            }
                        }
                    }
                }

                // Removes all keys that are not in the section object. Does not remove empty sections.
                // Looks up keys as case insensitive.
                Dictionary<string, string> keypairsUpper = section.GetKeyValuePairs(true);
                List<string> removedKeys = section.GetRemovedKeys();
                int firstLine = this.FindLine(initext, sectionName, null);
                int lastLine = this.FindLastSectionLine(initext, sectionName, false);
                if (firstLine >= 0 && firstLine + keypairsUpper.Count < lastLine)
                {
                    for (int line = lastLine; line > firstLine; line--)
                    {
                        string[] keyVal = this.GetKeyAndValue(initext[line]);
                        if (keyVal != null && keyVal.Length == 2)
                        {
                            string delkey = keyVal[0].ToUpperInvariant();
                            bool notpresent = !keypairsUpper.ContainsKey(delkey);
                            // only remove if either set to remove all changes, or if it's explicitly deleted
                            if (notpresent && (this.m_WriteMode == WriteMode.WRITE_ALL || removedKeys.Contains(delkey)))
                            {
                                initext.RemoveAt(line);
                            }
                        }
                    }
                }
            }
            // Remove explicitly removed sections
            int nrOfRemSections = this.m_RemovedSections.Count;
            for (int i = 0; i < nrOfRemSections; ++i)
            {
                string section = this.m_RemovedSections[i];
                int firstLine = this.FindLine(initext, section, null);
                if (firstLine > -1)
                {
                    int lastLine = this.FindLastSectionLine(initext, section, true);
                    initext.RemoveRange(firstLine, lastLine - firstLine + 1);
                }
            }
            // trim all empty lines off the end of the file
            while (initext.Count > 0 && initext[initext.Count - 1].Trim().Length == 0)
            {
                initext.RemoveAt(initext.Count - 1);
            }
            bool returnvalue = true;

            if (iniFilePath == null)
            {
                StringBuilder sb = new StringBuilder();
                int nrOfLines = this.m_IniSections.Count;
                for (int i = 0; i < nrOfLines; ++i)
                    sb.AppendLine(initext[i]);
                this.m_FileContents = sb.ToString();
                this.m_IniSections = this.ReadIniContents(initext.AsReadOnly());
            }
            else
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(iniFilePath, false, charEncoding))
                    {
                        int nrOfLines = initext.Count;
                        for (int i = 0; i < nrOfLines; ++i)
                            sw.WriteLine(initext[i]);
                    }
                }
                catch (IOException)
                {
                    returnvalue = false;
                }
                if (returnvalue)
                    this.ReadIniFile(iniFilePath, charEncoding);
            }
            return returnvalue;
        }

        /// <summary>Finds the line of a specific section's key.</summary>
        /// <param name="inifile">The ini file, as List of Strings.</param>
        /// <param name="inisection">The name of the section the key has to in.</param>
        /// <param name="inikey">The name of the key. If null, the index of the section will be returned.</param>
        /// <returns>The index in the inifile List which holds the key.</returns>
        protected int FindLine(List<string> inifile, string inisection, string inikey)
        {
            if (inifile == null)
                throw new ArgumentNullException("inifile");
            if (inisection == null)
                throw new ArgumentNullException("inisection");
            bool sectionfound = false;
            int iniLines = inifile.Count;
            for (int linenumber = 0; linenumber < iniLines; ++linenumber)
            {
                string s = inifile[linenumber];
                if (s.StartsWith("[") && s.Contains("]"))
                {
                    string sectionName = s.Substring(1, s.IndexOf("]", StringComparison.Ordinal) - 1);
                    sectionfound = sectionName.Equals(inisection, StringComparison.InvariantCultureIgnoreCase);
                    if (inikey == null && sectionfound)
                        return linenumber;
                }
                else if (sectionfound) // correct ini section was found
                {
                    string[] keyVal = this.GetKeyAndValue(s);
                    if (keyVal != null && keyVal[0].Equals(inikey, StringComparison.InvariantCultureIgnoreCase))
                        return linenumber;
                }
            }
            return -1;
        }

        /// <summary>This function finds the last key line of an ini section, allowing new keys to be added behind it.</summary>
        /// <param name="inifile">The ini file, as List of Strings.</param>
        /// <param name="inisection">The name of the section.</param>
        /// <param name="includeBlanks">True if all blank lines after the section should be counted too.</param>
        /// <returns>The index of the last key in this section before a new section or the end of the file.</returns>
        protected int FindLastSectionLine(List<string> inifile, string inisection, bool includeBlanks)
        {
            int lastLine = inifile.Count - 1;
            bool sectionfound = false;
            bool sectionwasfound = false;
            int sectionLine = -1;
            int iniLines = inifile.Count;
            for (int linenumber = 0; linenumber < iniLines; ++linenumber)
            {
                string s = inifile[linenumber];
                if (s.StartsWith("[") && s.Contains("]"))
                {
                    string sectionName = s.Substring(1, s.IndexOf("]", StringComparison.Ordinal) - 1);
                    sectionwasfound = sectionfound;
                    sectionfound = sectionName.Equals(inisection, StringComparison.InvariantCultureIgnoreCase);
                    if (sectionfound)
                        sectionLine = linenumber;
                    // requested section was encountered last time, and the start of the new one was found now.
                    if (sectionwasfound && !sectionfound)
                    {
                        lastLine = linenumber - 1;
                        break;
                    }
                }
            }
            // trim off commented and non-key lines
            if (sectionwasfound || sectionfound)
            {
                int origLastLine = lastLine;

                while (lastLine > sectionLine && !this.IsValidKeyLine(inifile[lastLine]))
                    lastLine--;

                if (!includeBlanks)
                    return lastLine;
                else
                {
                    while (lastLine < origLastLine && inifile.Count > lastLine + 1 && String.Empty.Equals(inifile[lastLine + 1]))
                        lastLine++;
                    return lastLine;
                }

            }
            return -1;
        }

        /// <summary>Returns the key and value as 2-element String array</summary>
        /// <param name="input">input line of text.</param>
        /// <returns>A 2-element String array containing the key name and value, or null if the line was not valid.</returns>
        protected string[] GetKeyAndValue(string input)
        {
            if (!this.IsValidKeyLine(input))
                return null;
            int separator = input.IndexOf('=');
            if (separator < 1)
                return null;
            string[] returnval = new string[2];
            returnval[0] = input.Substring(0, separator).Trim();
            returnval[1] = input.Substring(separator + 1).Trim();
            return returnval;
        }

        /// <summary>Gets a String from the ini file</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <returns>The found value, or the given default value.</returns>
        public string GetStringValue(string sectionName, string key, string defaultValue)
        {
            bool rb;
            return this.GetStringValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>Gets a String from the ini file</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false'if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public string GetStringValue(string sectionName, string key, string defaultValue, out bool success)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetStringValue(key, defaultValue, this.m_TrimValues, out success);
        }

        /// <summary>Sets a String value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        public void SetStringValue(string sectionName, string key, string value)
        {
            IniSection iniSection = this.GetSection(sectionName, true);
            iniSection.TrimValues = this.m_TrimValues;
            iniSection.SetStringValue(key, value);
        }

        /// <summary>Gets an Integer from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public int GetIntValue(string sectionName, string key, int defaultValue)
        {
            bool rb;
            return this.GetIntValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>Gets an Integer from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public int GetIntValue(string sectionName, string key, int defaultValue, out bool success)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetIntValue(key, defaultValue, out success);
        }

        /// <summary>Sets an Integer value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        public void SetIntValue(string sectionName, string key, int value)
        {
            this.SetIntValue(sectionName, key, value, this.m_RemoveComments);
        }

        /// <summary>Sets an Integer value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetIntValue(string sectionName, string key, int value, bool removeComments)
        {
            IniSection iniSection = this.GetSection(sectionName, true);
            iniSection.TrimValues = this.m_TrimValues;
            iniSection.SetIntValue(key, value, removeComments);
        }

        /// <summary>Gets a Character from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public char GetCharValue(string sectionName, string key, char defaultValue)
        {
            bool rb;
            return this.GetCharValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>Gets a Character from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public char GetCharValue(string sectionName, string key, char defaultValue, out bool success)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetCharValue(key, defaultValue, out success);
        }

        /// <summary>Sets a Character value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        public void SetCharValue(string sectionName, string key, char value)
        {
            this.SetCharValue(sectionName, key, value, this.m_RemoveComments);
        }

        /// <summary>Sets a Character value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetCharValue(string sectionName, string key, char value, bool removeComments)
        {
            IniSection iniSection = this.GetSection(sectionName, true);
            iniSection.TrimValues = this.m_TrimValues;
            iniSection.SetCharValue(key, value, removeComments);
        }

        /// <summary>Gets a Boolean from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">he name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        public bool GetBoolValue(string sectionName, string key, bool defaultValue)
        {
            bool rb;
            return this.GetBoolValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>
        /// Gets a Boolean from the ini file. Note that the string-to-boolean
        /// conversion method actually only checks the first character.
        /// </summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public bool GetBoolValue(string sectionName, string key, bool defaultValue, out bool success)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetBoolValue(key, defaultValue, out success);
        }

        /// <summary>Sets a Boolean value in the ini file, in the configured BooleanWriteMode. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        public void SetBoolValue(string sectionName, string key, bool value)
        {
            this.SetBoolValue(sectionName, key, value, this.m_BooleanMode, this.m_RemoveComments);
        }

        /// <summary>Sets a Boolean value in the ini file, as Yes or No. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetBoolValue(string sectionName, string key, bool value, bool removeComments)
        {
            this.SetBoolValue(sectionName, key, value, this.m_BooleanMode, removeComments);
        }

        /// <summary>Sets a Boolean value in the ini file, in the chosen boolean save mode.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="booleanmode">The BooleanMode (True/False, Yes/No, 1/0, etc) to use for saving booleans as string.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetBoolValue(string sectionName, string key, bool value, BooleanMode booleanmode, bool removeComments)
        {
            IniSection iniSection = this.GetSection(sectionName, true);
            iniSection.TrimValues = this.m_TrimValues;
            iniSection.SetBoolValue(key, value, booleanmode, removeComments);
        }

        /// <summary>Sets a Boolean value in the ini file, in the chosen boolean save mode.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="booleanmode">The BooleanMode (True/False, Yes/No, 1/0, etc) to use for saving booleans as string.</param>
        public void SetBoolValue(string sectionName, string key, bool value, BooleanMode booleanmode)
        {
            IniSection iniSection = this.GetSection(sectionName, true);
            iniSection.TrimValues = this.m_TrimValues;
            iniSection.SetBoolValue(key, value, booleanmode, this.m_RemoveComments);
        }

        /// <summary>Gets a floating point value from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public double GetFloatValue(string sectionName, string key, double defaultValue)
        {
            bool success;
            return this.GetFloatValue(sectionName, key, defaultValue, out success);
        }

        /// <summary>Gets a floating point value from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="defaultValue">The default value to return in case the key was not found.</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public double GetFloatValue(string sectionName, string key, double defaultValue, out bool success)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetFloatValue(key, defaultValue, out success);
        }

        /// <summary>Sets a floating point value in the ini file, with the configured default precision.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        public void SetFloatValue(string sectionName, string key, double value)
        {
            this.SetFloatValue(sectionName, key, value, this.m_DoublePrecision, this.m_RemoveComments);
        }

        /// <summary>Sets a floating point value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="precision">Precision, in number of digits after the decimal point.</param>
        public void SetFloatValue(string sectionName, string key, double value, int precision)
        {
            this.SetFloatValue(sectionName, key, value, precision, this.m_RemoveComments);
        }

        /// <summary>Sets a floating point value in the ini file, with the configured default precision.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetFloatValue(string sectionName, string key, double value, bool removeComments)
        {
            this.SetFloatValue(sectionName, key, value, this.m_DoublePrecision, removeComments);
        }

        /// <summary>Sets a floating point value in the ini file.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="precision">Precision, in number of digits after the decimal point.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetFloatValue(string sectionName, string key, double value, int precision, bool removeComments)
        {
            IniSection iniSection = this.GetSection(sectionName, true);
            iniSection.TrimValues = this.m_TrimValues;
            iniSection.SetFloatValue(key, value, precision, removeComments);
        }

        /// <summary>Removes the specified key from the specified section</summary>
        /// <param name="sectionName">The name of the section the key should be in.</param>
        /// <param name="key">The name of the key.</param>
        public void RemoveKey(string sectionName, string key)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
                return;
            iniSection.RemoveKey(key);
        }

        /// <summary>Removes all keys in a section.</summary>
        /// <param name="sectionName">The name of the section.</param>
        public void RemoveAllKeys(string sectionName)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null) return;
            iniSection.Clear();
        }

        /// <summary>
        /// Removes a section from the ini file, and marks it for deletion on the next rewrite.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        public void RemoveSection(string sectionName)
        {
            int iniSecs = this.m_IniSections.Count;
            for (int i = 0; i < iniSecs; ++i)
            {
                string secname = this.m_IniSections[i].GetName();
                if (secname.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.m_IniSections.RemoveAt(i);
                    break;
                }
            }
            // Mark for deletion even if not found in current ini, for the off chance it's added during the object's lifetime.
            this.m_RemovedSections.Add(sectionName.ToUpperInvariant());
        }

        /// <summary>
        /// Removes a section from the ini file, and marks it for deletion on the next rewrite.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        public bool ContainsSection(string sectionName)
        {
            int iniSecs = this.m_IniSections.Count;
            for (int i = 0; i < iniSecs; ++i)
            {
                string secname = this.m_IniSections[i].GetName();
                if (secname.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Clears a section's keys.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        public void ClearSectionKeys(string sectionName)
        {
            IniSection section = null;
            int nrOfSections = this.m_IniSections.Count;
            for (int i = 0; i < nrOfSections; ++i)
            {
                IniSection sec = this.m_IniSections[i];
                string secname = sec.GetName();
                if (secname.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    section = sec;
                    break;
                }
            }
            if (section != null)
                section.Clear();
        }

        /// <summary>Gets all keys from a section.</summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>A list of all key names in the section.</returns>
        public List<string> GetSectionKeys(string sectionName)
        {
            return this.GetSectionKeys(sectionName, false);
        }

        /// <summary>Gets all keys from a section.</summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A list of all key names in the section.</returns>
        public List<string> GetSectionKeys(string sectionName, bool upperCaseKeys)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null) return new List<string>();
            if (upperCaseKeys)
                return iniSection.GetUpperCaseKeys();
            else
                return iniSection.GetKeys();
        }

        /// <summary>Returns a copy of a specified section's key-value pairs map.</summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>A Map with the key-value pairs.</returns>
        public Dictionary<string, string> GetSectionContent(string sectionName)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null) return new Dictionary<string, string>();
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetKeyValuePairs();
        }

        /// <summary>Returns a copy of a specified section's key-value pairs map.</summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A Map with the key-value pairs.</returns>
        public Dictionary<string, string> GetSectionContent(string sectionName, bool upperCaseKeys)
        {
            IniSection iniSection = this.GetSection(sectionName);
            if (iniSection == null)
                return new Dictionary<string, string>();
            iniSection.TrimValues = this.m_TrimValues;
            return iniSection.GetKeyValuePairs(upperCaseKeys);
        }

        /// <summary>
        /// Returns a list of the names of all sections in the ini.
        /// </summary>
        /// <returns>a List of the names of all sections in the ini.</returns>
        public List<string> GetSectionNames()
        {
            int nrOfSections = this.m_IniSections.Count;
            List<string> sectionNames = new List<string>(nrOfSections);
            for (int i = 0; i < nrOfSections; ++i)
                sectionNames.Add(this.m_IniSections[i].GetName());
            return sectionNames;
        }

        /// <summary>Gets a section by name. Returns null if the section was not found.</summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>The IniSection object, or null if not found.</returns>
        protected IniSection GetSection(string sectionName)
        {
            return this.GetSection(sectionName, false);
        }

        /// <summary>Gets a section by name.</summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="createWhenNotFound">If the section was not found, create a new section with that name and return that.</param>
        /// <returns>The retrieved or new IniSection object with that name.</returns>
        protected IniSection GetSection(string sectionName, bool createWhenNotFound)
        {
            IniSection iniSection = null;
            int nrOfSections = this.m_IniSections.Count;
            for (int i = 0; i < nrOfSections; ++i)
            {
                IniSection testsec = this.m_IniSections[i];
                if (!testsec.GetName().Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                iniSection = testsec;
                break;
            }

            if (iniSection == null && createWhenNotFound) // doesn't exist yet
            {
                iniSection = new IniSection(sectionName);
                iniSection.TrimValues = this.m_TrimValues;
                this.m_IniSections.Add(iniSection);
                this.m_RemovedSections.Remove(sectionName.ToUpperInvariant());
            }
            return iniSection;
        }

        /// <summary>Reads lines of text from a stream, and returns it as a List of strings.</summary>
        /// <param name="stream">The stream to read as file.</param>
        /// <param name="charEncoding">The character encoding to use when reading the file.</param>
        /// <returns>A List of Strings, each String representing one line from the original text.</returns>
        protected List<string> ReadLinesFromTextStream(StreamReader stream, Encoding charEncoding)
        {
            List<string> text = new List<string>();
            string input;
            while ((input = stream.ReadLine()) != null)
            {
                // fix for UTF8 with BOM read on UTF8 without BOM.
                if (text.Count == 0
                    && charEncoding.CodePage == 65001
                    && charEncoding.GetPreamble().Length == 0
                    && input.Length > 0 && input[0] == 0xFEFF)
                {
                    input = input.Substring(1);
                }
                text.Add(input);
            }
            return text;
        }

        /// <summary>A quick test to see if a line contains a valid ini key.</summary>
        /// <param name="line">The input to test.</param>
        /// <returns>True if the line is not a comment, has key with a length greater than zero, and contains the '=' separator.</returns>
        protected bool IsValidKeyLine(string line)
        {
            line = line.Trim();
            return line.Length > 0 // contains data
                    && line[0] != ';' // is not a comment
                    && line.IndexOf('=') > 0; // Line contains separator, and key is not empty
        }
    }

    /// <summary>This enum determines which keys are overwritten in the file when saving.</summary>
    public enum WriteMode
    {
        /// <summary>
        /// Write back all keys in the section objects, and remove any unknown keys found in the sections in the target file.
        /// Any values in the target file that were modified or added after the read will be lost on the next save.
        /// Unknown sections in the target file will be left as they are.
        /// </summary>
        WRITE_ALL,
        /// <summary>
        /// Write back all keys that were actually used by the program. Any values in the target file that were modified,
        /// added or deleted after the read will be kept as they were, except for those that were read, modified or deleted
        /// by the program. Re-saving all values that were read ensures consistency of all data the program used.
        /// Unknown sections in the target file will be left as they are.
        /// </summary>
        WRITE_ALL_ACCESSED,
        /// <summary>
        /// Write back all keys that were actually modified by the program. Any values in the target file that were modified,
        /// added or deleted after the read will be kept as they were, except for those that were modified or deleted
        /// by the program. Unknown sections in the target file will be left as they are.
        /// </summary>
        WRITE_MODIFIED_ONLY,
    }

    /// <summary>This enum is used to determine how an ini file saves booleans as string.</summary>
    public enum BooleanMode
    {
        /// <summary>True="1", False="0"</summary>
        ONE_ZERO,
        /// <summary>True="Yes", False="No"</summary>
        YES_NO,
        /// <summary>True="True", False="False"</summary>
        TRUE_FALSE,
        /// <summary>True="Enabled", False="Disabled"</summary>
        ENABLED_DISABLED,
        /// <summary>True="Active", False="Inactive"</summary>
        ACTIVE_INACTIVE,
        /// <summary>True="Aye", False="Nay"</summary>
        AYE_NAY
    }

}