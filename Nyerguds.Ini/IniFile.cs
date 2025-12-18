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
        public static Boolean DEFAULT_REMOVECOMMENTS = false;
        public static Int32 DEFAULT_DOUBLEPRECISION = 6;
        public static WriteMode DEFAULT_WRITEMODE = WriteMode.WRITE_ALL_ACCESSED;
        public static Boolean DEFAULT_INITIALCAPS = true;
        public static Encoding DEFAULT_ENCODING = new UTF8Encoding(false);
        public static Boolean DEFAULT_TRIMVALUES = false;

        /// <summary>DOS U.S. ASCII-437 encoding; the standard encoding for the ini files of most DOS games.</summary>
        public static Encoding ENCODING_DOS_US = Encoding.GetEncoding(437);

        #endregion

        #region global variables
        protected List<IniSection> m_IniSections;
        protected List<String> m_RemovedSections;

        protected BooleanMode m_BooleanMode = DEFAULT_BOOLEANMODE;
        protected Boolean m_RemoveComments = DEFAULT_REMOVECOMMENTS;
        protected Int32 m_DoublePrecision = DEFAULT_DOUBLEPRECISION;
        protected WriteMode m_WriteMode = DEFAULT_WRITEMODE;
        protected Boolean m_InitialCaps = DEFAULT_INITIALCAPS;
        protected Encoding m_Encoding = DEFAULT_ENCODING;
        protected Boolean m_TrimValues = DEFAULT_TRIMVALUES;

        protected String m_FilePath;
        protected String m_FileContents = null;

        #endregion

        /// <summary>Determines how booleans are saved back into the ini file.</summary>
        public BooleanMode BooleanWriteMode
        {
            get { return this.m_BooleanMode; }
            set { this.m_BooleanMode = value; }
        }

        /// <summary>Remove comments when writing non-string values to ini. If False, comments behind the values are preserved.</summary>
        public Boolean RemoveComments
        {
            get { return this.m_RemoveComments; }
            set { this.m_RemoveComments = value; }
        }

        /// <summary>The number of digits behind the decimal point to write when saving floating point values.</summary>
        public Int32 DoublePrecision
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
        public Boolean InitialCaps
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
        public Boolean TrimValues
        {
            get { return this.m_TrimValues; }
            set { this.m_TrimValues = value; }
        }

        /// <summary>The path to use as input and output file.</summary>
        public String FilePath
        {
            get { return this.m_FilePath; }
            set { this.m_FilePath = value; }
        }

        /// <summary>Retrieves the virtual file contents of the ini object. This can be used to retrieve ini text data after saving when the ini was originally given as content string without save path. Will be null if the file was read from disk.</summary>
        public String FileContents
        {
            get { return this.m_FileContents; }
        }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file.
        /// </summary>
        /// <param name="filePath">Path of the file to read</param>
        public IniFile(String filePath)
            : this(filePath, DEFAULT_INITIALCAPS, DEFAULT_ENCODING, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file.
        /// </summary>
        /// <param name="filePath">Path of the file to read</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file</param>
        public IniFile(String filePath, Encoding textEncoding)
            : this(filePath, DEFAULT_INITIALCAPS, textEncoding, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file.
        /// </summary>
        /// <param name="filePath">Path of the file to read</param>
        /// <param name="initialCaps">Write back all ini keys with initial capital letter</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file</param>
        /// <param name="trimValues">True to trim any retrieved values.</param>
        public IniFile(String filePath, Boolean initialCaps, Encoding textEncoding, Boolean trimValues)
        {
            if (textEncoding == null) throw new ArgumentNullException("textEncoding");
            if (filePath == null) throw new ArgumentNullException("filePath");
            this.m_RemovedSections = new List<String>();
            this.m_InitialCaps = initialCaps;
            this.m_TrimValues = trimValues;
            ReadIniFile(filePath, textEncoding);
        }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file
        ///     that doesn't necessarily exist yet.
        /// </summary>
        /// <param name="filePath">Path to write the file to when saving</param>
        /// <param name="filecontents">String with the file contents in it</param>
        public IniFile(String filePath, String filecontents)
            : this(filePath, filecontents, DEFAULT_INITIALCAPS, DEFAULT_ENCODING, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file
        ///     that doesn't necessarily exist yet.
        /// </summary>
        /// <param name="filePath">Path to write the file to when saving</param>
        /// <param name="filecontents">String with the file contents in it</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file</param>
        public IniFile(String filePath, String filecontents, Encoding textEncoding)
            : this(filePath, filecontents, DEFAULT_INITIALCAPS, textEncoding, DEFAULT_TRIMVALUES)
        { }

        /// <summary>
        ///     Creates an object for reading, editing and writing an ini file
        ///     that doesn't necessarily exist yet.
        /// </summary>
        /// <param name="filePath">Path to write the file to when saving</param>
        /// <param name="filecontents">String with the file contents in it</param>
        /// <param name="initialCaps">Write back all ini keys with initial capital letter</param>
        /// <param name="textEncoding">Text encoding to use for reading (and writing) the file</param>
        /// <param name="trimValues">Trim all values on read / write</param>
        public IniFile(String filePath, String filecontents, Boolean initialCaps, Encoding textEncoding, Boolean trimValues)
        {
            this.m_FilePath = filePath;
            this.m_Encoding = textEncoding;
            this.m_RemovedSections = new List<String>();
            this.m_InitialCaps = initialCaps;
            this.m_TrimValues = trimValues;
            this.m_FileContents = filecontents.Replace("\r\n", "\n").Replace('\r', '\n');
            ReadOnlyCollection<String> initext = new List<String>(filecontents.Split('\n')).AsReadOnly();
            this.m_IniSections = ReadIniContents(initext);
        }

        /// <summary>
        /// Sets the path for the ini file to a new string; and reads that file.
        /// This function does a complete reset of the object's data.
        /// </summary>
        /// <param name="iniFilePath">Path of the file to read</param>
        /// <param name="charEncoding">Character encoding to use</param>
        protected void ReadIniFile(String iniFilePath, Encoding charEncoding)
        {
            this.m_FilePath = iniFilePath;
            this.m_Encoding = charEncoding;
            this.m_RemovedSections.Clear();
            this.m_IniSections = null;
            if (File.Exists(this.m_FilePath))
            {
                StreamReader stream = null;
                try { stream = new StreamReader(this.m_FilePath, this.m_Encoding, false); }
                catch { /* ignore */ }
                if (stream != null)
                {
                    try
                    {
                        ReadOnlyCollection<String> initext = ReadLinesFromTextStream(stream, charEncoding).AsReadOnly();
                        this.m_IniSections = ReadIniContents(initext);
                    }
                    catch { /* ignore */ }
                }
            }
            if (this.m_IniSections == null)
                this.m_IniSections = new List<IniSection>();
        }

        /// <summary>Reads the ini contents of a stream, and returns it as a list of ini sections.</summary>
        /// <param name="initext">Read-only collection of strings to read the ini data from.</param>
        /// <returns>A List of IniSection objects with the read data.</returns>
        protected List<IniSection> ReadIniContents(ReadOnlyCollection<String> initext)
        {
            List<IniSection> readIniSections = new List<IniSection>();
            if (initext == null)
                return readIniSections;
            try
            {
                IniSection iniSection = null;
                foreach (String input in initext)
                {
                    if (input.StartsWith("[") && input.Contains("]"))
                    {
                        String sectionName = input.Substring(1, input.IndexOf("]", StringComparison.Ordinal) - 1);
                        if (!sectionName.Contains("[")) // valid ini section
                        {
                            iniSection = null;
                            Int32 sectionIndex = -1;
                            for (Int32 j = 0; j < readIniSections.Count; j++)
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
                        String[] keyValue = this.GetKeyAndValue(input);
                        if (keyValue != null && keyValue.Length == 2)
                            iniSection.SetStringValue(keyValue[0], keyValue[1]);
                    }
                }
            }
            catch (Exception) { /* ignore */ }
            // clear all Accessed and Modified statuses, since this is the initial read.
            foreach (IniSection section in readIniSections)
            {
                section.ResetStatuses();
                section.TrimValues = m_TrimValues;
            }
            return readIniSections;
        }

        /// <summary>
        /// Writes the modified ini object to the set FilePath, using the set encoding.
        /// Note that the writing system does not technically overwrite the target file; it just fills in the data, adds new sections, and adds
        /// and removes keys to adjust the file to the edits made in the stored data. Unknown sections and comments in the file are left untouched.
        /// Note that unknown keys in known sections are only removed if WriteBackMode is WRITE_ALL. The other modes only remove explicitly removed keys.
        /// </summary>
        /// <returns>True if the save operation succeeded</returns>
        public Boolean WriteIni()
        {
            return WriteIni(this.m_FilePath, this.m_Encoding);
        }

        /// <summary>
        /// Writes the modified ini object to a file.
        /// </summary>
        /// <param name="iniFilePath">Filename to write to</param>
        /// <param name="charEncoding">Character encoding to use</param>
        /// <returns>True if the save operation succeeded</returns>
        public Boolean WriteIni(String iniFilePath, Encoding charEncoding)
        {
            List<String> initext;
            if (iniFilePath == null && !String.IsNullOrEmpty(this.m_FileContents))
            {
                initext = new List<String>(this.m_FileContents.Split('\n'));
            }
            else
            {
                try
                {
                    //initext = new List<String>(File.ReadAllLines(iniFilePath, encoding));
                    StreamReader stream = null;
                    try
                    {
                        stream = new StreamReader(iniFilePath, charEncoding, false);
                    }
                    catch { /* ignore */ }
                    initext = ReadLinesFromTextStream(stream, charEncoding);
                }
                catch (Exception)
                {
                    initext = new List<String>();
                }
            }
            foreach (IniSection section in this.m_IniSections)
            {
                // writes keys in original case
                Dictionary<String, String> keypairs = section.GetKeyValuePairs();
                Dictionary<String, Boolean> keypairsAccessed = section.GetKeyValuePairsAccessed(false);
                Dictionary<String, Boolean> keypairsChanged = section.GetKeyValuePairsChanged(false);
                String sectionName = section.GetName();
                foreach (KeyValuePair<String, String> iniPair in keypairs)
                {
                    String newline = iniPair.Key;
                    if (m_WriteMode == WriteMode.WRITE_ALL
                        || ((m_WriteMode == WriteMode.WRITE_ALL_ACCESSED) && keypairsAccessed[newline])
                        || ((m_WriteMode == WriteMode.WRITE_MODIFIED_ONLY) && keypairsChanged[newline]))
                    {
                        if (this.m_InitialCaps)
                            newline = Char.ToUpper(newline[0]) + newline.Substring(1, newline.Length - 1);
                        newline += "=" + iniPair.Value;
                        Int32 linenumber = FindLine(initext, sectionName, iniPair.Key);
                        if (linenumber >= 0)
                            initext[linenumber] = newline;
                        else
                        {
                            linenumber = FindLastSectionLine(initext, sectionName, false) + 1;
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
                Dictionary<String, String> keypairsUpper = section.GetKeyValuePairs(true);
                List<String> removedKeys = section.GetRemovedKeys();
                Int32 firstLine = FindLine(initext, sectionName, null);
                Int32 lastLine = FindLastSectionLine(initext, sectionName, false);
                if (firstLine >= 0 && firstLine + keypairsUpper.Count < lastLine)
                {
                    for (Int32 line = lastLine; line > firstLine; line--)
                    {
                        String[] keyVal = GetKeyAndValue(initext[line]);
                        if (keyVal != null && keyVal.Length == 2)
                        {
                            String delkey = keyVal[0].ToUpperInvariant();
                            Boolean notpresent = !keypairsUpper.ContainsKey(delkey);
                            // only remove if either set to remove all changes, or if it's explicitly deleted
                            if (notpresent && (m_WriteMode == WriteMode.WRITE_ALL || removedKeys.Contains(delkey)))
                            {
                                initext.RemoveAt(line);
                            }
                        }
                    }
                }
            }
            // Remove explicitly removed sections
            foreach (String section in this.m_RemovedSections)
            {
                Int32 firstLine = FindLine(initext, section, null);
                if (firstLine > -1)
                {
                    Int32 lastLine = FindLastSectionLine(initext, section, true);
                    initext.RemoveRange(firstLine, lastLine - firstLine + 1);
                }
            }

            // trim all empty lines off the end of the file
            while (initext.Count > 0 && initext[initext.Count - 1].Trim().Length == 0)
            {
                initext.RemoveAt(initext.Count - 1);
            }
            Boolean returnvalue = true;

            if (iniFilePath == null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (String line in initext)
                    sb.AppendLine(line);
                this.m_FileContents = sb.ToString();
                this.m_IniSections = ReadIniContents(initext.AsReadOnly());
            }
            else
            {
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(iniFilePath, false, charEncoding);
                    foreach (String line in initext)
                        sw.WriteLine(line);
                }
                catch (IOException)
                {
                    returnvalue = false;
                }
                finally
                {
                    try
                    {
                        if (sw != null)
                        {
                            sw.Close();
                            sw.Dispose();
                        }
                    }
                    catch { /* ignore */ }
                }
                if (returnvalue)
                    ReadIniFile(iniFilePath, charEncoding);
            }
            return returnvalue;
        }

        /// <summary>Finds the line of a specific section's key.</summary>
        /// <param name="inifile">The ini file, as List of Strings</param>
        /// <param name="inisection">The name of the section the key has to in</param>
        /// <param name="inikey">The name of the key. If null, the index of the section will be returned.</param>
        /// <returns>The index in the inifile List which holds the key</returns>
        protected Int32 FindLine(List<String> inifile, String inisection, String inikey)
        {
            if (inifile == null)
                throw new ArgumentNullException("inifile");
            if (inisection == null)
                throw new ArgumentNullException("inisection");
            Boolean sectionfound = false;
            for (Int32 linenumber = 0; linenumber < inifile.Count; linenumber++)
            {
                String s = inifile[linenumber];
                if (s.StartsWith("[") && s.Contains("]"))
                {
                    String sectionName = s.Substring(1, s.IndexOf("]", StringComparison.Ordinal) - 1);
                    sectionfound = sectionName.Equals(inisection, StringComparison.InvariantCultureIgnoreCase);
                    if (inikey == null && sectionfound)
                        return linenumber;
                }
                else if (sectionfound) // correct ini section was found
                {
                    String[] keyVal = GetKeyAndValue(s);
                    if (keyVal != null && keyVal[0].Equals(inikey, StringComparison.InvariantCultureIgnoreCase))
                        return linenumber;
                }
            }
            return -1;
        }

        /// <summary>This function finds the last key line of an ini section, allowing new keys to be added behind it.</summary>
        /// <param name="inifile">The ini file, as List of Strings</param>
        /// <param name="inisection">The name of the section</param>
        /// <param name="includeBlanks">True if all blank lines after the section should be counted too</param>
        /// <returns>The index of the last key in this section before a new section or the end of the file.</returns>
        protected Int32 FindLastSectionLine(List<String> inifile, String inisection, Boolean includeBlanks)
        {
            Int32 lastLine = inifile.Count - 1;
            Boolean sectionfound = false;
            Boolean sectionwasfound = false;
            Int32 sectionLine = -1;
            for (Int32 linenumber = 0; linenumber < inifile.Count; linenumber++)
            {
                String s = inifile[linenumber];
                if (s.StartsWith("[") && s.Contains("]"))
                {
                    String sectionName = s.Substring(1, s.IndexOf("]", StringComparison.Ordinal) - 1);
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
                Int32 origLastLine = lastLine;

                while (lastLine > sectionLine && !IsValidKeyLine(inifile[lastLine]))
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
        /// <param name="input">input line of text</param>
        /// <returns>A 2-element String array containing the key name and value, or null if the line was not valid.</returns>
        protected String[] GetKeyAndValue(String input)
        {
            if (!IsValidKeyLine(input))
                return null;
            Int32 separator = input.IndexOf('=');
            if (separator < 1)
                return null;
            String[] returnval = new String[2];
            returnval[0] = input.Substring(0, separator).Trim();
            returnval[1] = input.Substring(separator + 1).Trim();
            return returnval;
        }

        /// <summary>Gets a String from the ini file</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <returns>The found value, or the given default value.</returns>
        public String GetStringValue(String sectionName, String key, String defaultValue)
        {
            Boolean rb = true;
            return GetStringValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>Gets a String from the ini file</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false'if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed</returns>
        public String GetStringValue(String sectionName, String key, String defaultValue, out Boolean success)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetStringValue(key, defaultValue, m_TrimValues, out success);
        }

        /// <summary>Sets a String value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        public void SetStringValue(String sectionName, String key, String value)
        {
            IniSection iniSection = GetSection(sectionName, true);
            iniSection.TrimValues = m_TrimValues;
            iniSection.SetStringValue(key, value);
        }

        /// <summary>Gets an Integer from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <returns>The found value, or the given default value if the fetch failed</returns>
        public Int32 GetIntValue(String sectionName, String key, Int32 defaultValue)
        {
            Boolean rb = true;
            return GetIntValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>Gets an Integer from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public Int32 GetIntValue(String sectionName, String key, Int32 defaultValue, out Boolean success)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetIntValue(key, defaultValue, out success);
        }

        /// <summary>Sets an Integer value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        public void SetIntValue(String sectionName, String key, Int32 value)
        {
            this.SetIntValue(sectionName, key, value, this.m_RemoveComments);
        }

        /// <summary>Sets an Integer value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetIntValue(String sectionName, String key, Int32 value, Boolean removeComments)
        {
            IniSection iniSection = GetSection(sectionName, true);
            iniSection.TrimValues = m_TrimValues;
            iniSection.SetIntValue(key, value, removeComments);
        }

        /// <summary>Gets a Character from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public Char GetCharValue(String sectionName, String key, Char defaultValue)
        {
            Boolean rb = true;
            return GetCharValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>Gets a Character from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public Char GetCharValue(String sectionName, String key, Char defaultValue, out Boolean success)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetCharValue(key, defaultValue, out success);
        }

        /// <summary>Sets a Character value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        public void SetCharValue(String sectionName, String key, Char value)
        {
            this.SetCharValue(sectionName, key, value, this.m_RemoveComments);
        }

        /// <summary>Sets a Character value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetCharValue(String sectionName, String key, Char value, Boolean removeComments)
        {
            IniSection iniSection = GetSection(sectionName, true);
            iniSection.TrimValues = m_TrimValues;
            iniSection.SetCharValue(key, value, removeComments);
        }

        /// <summary>Gets a Boolean from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">he name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        public Boolean GetBoolValue(String sectionName, String key, Boolean defaultValue)
        {
            Boolean rb = true;
            return GetBoolValue(sectionName, key, defaultValue, out rb);
        }

        /// <summary>
        /// Gets a Boolean from the ini file. Note that the string-to-boolean
        /// conversion method actually only checks the first character.
        /// </summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public Boolean GetBoolValue(String sectionName, String key, Boolean defaultValue, out Boolean success)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetBoolValue(key, defaultValue, out success);
        }

        /// <summary>Sets a Boolean value in the ini file, in the configured BooleanWriteMode. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        public void SetBoolValue(String sectionName, String key, Boolean value)
        {
            this.SetBoolValue(sectionName, key, value, this.m_BooleanMode, this.m_RemoveComments);
        }

        /// <summary>Sets a Boolean value in the ini file, as Yes or No. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetBoolValue(String sectionName, String key, Boolean value, Boolean removeComments)
        {
            this.SetBoolValue(sectionName, key, value, this.m_BooleanMode, removeComments);
        }

        /// <summary>Sets a Boolean value in the ini file, in the chosen boolean save mode.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="booleanmode">The BooleanMode (True/False, Yes/No, 1/0, etc) to use for saving Booleans as String.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetBoolValue(String sectionName, String key, Boolean value, BooleanMode booleanmode, Boolean removeComments)
        {
            IniSection iniSection = GetSection(sectionName, true);
            iniSection.TrimValues = m_TrimValues;
            iniSection.SetBoolValue(key, value, booleanmode, removeComments);
        }

        /// <summary>Sets a Boolean value in the ini file, in the chosen boolean save mode.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="booleanmode">The BooleanMode (True/False, Yes/No, 1/0, etc) to use for saving Booleans as String.</param>
        public void SetBoolValue(String sectionName, String key, Boolean value, BooleanMode booleanmode)
        {
            IniSection iniSection = GetSection(sectionName, true);
            iniSection.TrimValues = m_TrimValues;
            iniSection.SetBoolValue(key, value, booleanmode, m_RemoveComments);
        }

        /// <summary>Gets a floating point value from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public Double GetFloatValue(String sectionName, String key, Double defaultValue)
        {
            Boolean success;
            return GetFloatValue(sectionName, key, defaultValue, out success);
        }

        /// <summary>Gets a floating point value from the ini file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="defaultValue">The default value to return in case the key was not found</param>
        /// <param name="success">An output parameter containing a boolean which is set to 'false' if the fetch failed and the default value was returned.</param>
        /// <returns>The found value, or the given default value if the fetch failed.</returns>
        public Double GetFloatValue(String sectionName, String key, Double defaultValue, out Boolean success)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
            {
                success = false;
                return defaultValue;
            }
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetFloatValue(key, defaultValue, out success);
        }

        /// <summary>Sets a floating point value in the ini file, with the configured default precision.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        public void SetFloatValue(String sectionName, String key, Double value)
        {
            this.SetFloatValue(sectionName, key, value, this.m_DoublePrecision, this.m_RemoveComments);
        }

        /// <summary>Sets a floating point value in the ini file. This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="precision">Precision, in number of digits after the decimal point.</param>
        public void SetFloatValue(String sectionName, String key, Double value, Int32 precision)
        {
            this.SetFloatValue(sectionName, key, value, precision, this.m_RemoveComments);
        }

        /// <summary>Sets a floating point value in the ini file, with the configured default precision.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetFloatValue(String sectionName, String key, Double value, Boolean removeComments)
        {
            this.SetFloatValue(sectionName, key, value, this.m_DoublePrecision, removeComments);
        }

        /// <summary>Sets a floating point value in the ini file.
        /// This action does not save the file.</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        /// <param name="value">Value to write</param>
        /// <param name="precision">Precision, in number of digits after the decimal point.</param>
        /// <param name="removeComments">True to remove any comments put behind the value. The default behaviour is to filter out the comment and paste it behind the new value.</param>
        public void SetFloatValue(String sectionName, String key, Double value, Int32 precision, Boolean removeComments)
        {
            IniSection iniSection = GetSection(sectionName, true);
            iniSection.TrimValues = m_TrimValues;
            iniSection.SetFloatValue(key, value, precision, removeComments);
        }

        /// <summary>Removes the specified key from the specified section</summary>
        /// <param name="sectionName">The name of the section the key should be in</param>
        /// <param name="key">The name of the key</param>
        public void RemoveKey(String sectionName, String key)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
                return;
            iniSection.RemoveKey(key);
        }

        /// <summary>Removes all keys in a section.</summary>
        /// <param name="sectionName">The name of the section</param>
        public void RemoveAllKeys(String sectionName)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null) return;
            iniSection.Clear();
        }

        /// <summary>
        /// Removes a section from the ini file, and marks it for deletion on the next rewrite.
        /// </summary>
        /// <param name="sectionName">The name of the section</param>
        public void RemoveSection(String sectionName)
        {
            for (Int32 i = 0; i < this.m_IniSections.Count; i++)
            {
                String secname = this.m_IniSections[i].GetName();
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
        /// <param name="sectionName">The name of the section</param>
        public void ClearSectionKeys(String sectionName)
        {
            IniSection section = null;
            foreach (IniSection sec in this.m_IniSections)
            {
                String secname = sec.GetName();
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
        /// <param name="sectionName">The name of the section</param>
        /// <returns>A list of all key names in the section</returns>
        public List<String> GetSectionKeys(String sectionName)
        {
            return this.GetSectionKeys(sectionName, false);
        }

        /// <summary>Gets all keys from a section.</summary>
        /// <param name="sectionName">The name of the section</param>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A list of all key names in the section.</returns>
        public List<String> GetSectionKeys(String sectionName, Boolean upperCaseKeys)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null) return new List<String>();
            if (upperCaseKeys)
                return iniSection.GetUpperCaseKeys();
            else
                return iniSection.GetKeys();
        }

        /// <summary>Returns a copy of a specified section's key-value pairs map.</summary>
        /// <param name="sectionName">The name of the section</param>
        /// <returns>A Map with the key-value pairs</returns>
        public Dictionary<String, String> GetSectionContent(String sectionName)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null) return new Dictionary<String, String>();
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetKeyValuePairs();
        }

        /// <summary>Returns a copy of a specified section's key-value pairs map.</summary>
        /// <param name="sectionName">The name of the section</param>
        /// <param name="upperCaseKeys">True to return the keys as upper case strings, for easier case-insensitive search.</param>
        /// <returns>A Map with the key-value pairs</returns>
        public Dictionary<String, String> GetSectionContent(String sectionName, Boolean upperCaseKeys)
        {
            IniSection iniSection = GetSection(sectionName);
            if (iniSection == null)
                return new Dictionary<String, String>();
            iniSection.TrimValues = m_TrimValues;
            return iniSection.GetKeyValuePairs(upperCaseKeys);
        }

        /// <summary>
        /// Returns a list of the names of all sections in the ini.
        /// </summary>
        /// <returns>a List of the names of all sections in the ini</returns>
        public List<String> GetSectionNames()
        {
            List<String> sectionNames = new List<String>();
            foreach (IniSection section in this.m_IniSections)
                sectionNames.Add(section.GetName());
            return sectionNames;
        }

        /// <summary>Gets a section by name. Returns null if the section was not found.</summary>
        /// <param name="sectionName">The name of the section</param>
        /// <returns>The IniSection object, or null if not found.</returns>
        protected IniSection GetSection(String sectionName)
        {
            return GetSection(sectionName, false);
        }

        /// <summary>Gets a section by name.</summary>
        /// <param name="sectionName">The name of the section</param>
        /// <param name="createWhenNotFound">If the section was not found, create a new section with that name and return that.</param>
        /// <returns>The retrieved or new IniSection object with that name.</returns>
        protected IniSection GetSection(String sectionName, Boolean createWhenNotFound)
        {
            IniSection iniSection = null;
            foreach (IniSection testsec in this.m_IniSections)
            {
                if (testsec.GetName().Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    iniSection = testsec;
                    break;
                }
            }

            if (iniSection == null && createWhenNotFound) // doesn't exist yet
            {
                iniSection = new IniSection(sectionName);
                iniSection.TrimValues = m_TrimValues;
                this.m_IniSections.Add(iniSection);
                m_RemovedSections.Remove(sectionName.ToUpperInvariant());
            }
            return iniSection;
        }

        /// <summary>Reads lines of text from a stream, and returns it as a List of strings.</summary>
        /// <param name="stream">The stream to read as file</param>
        /// <param name="charEncoding">The character encoding to use when reading the file</param>
        /// <returns>A List of Strings, each String representing one line from the original text.</returns>
        protected List<String> ReadLinesFromTextStream(StreamReader stream, Encoding charEncoding)
        {
            List<String> text = new List<String>();
            try
            {
                String input = null;
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
            }
            finally
            {
                stream.Close();
            }
            return text;
        }

        /// <summary>A quick test to see if a line contains a valid ini key.</summary>
        /// <param name="line">The input to test</param>
        /// <returns>True if the line is not a comment, has key with a length greater than zero, and contains the '=' separator.</returns>
        protected Boolean IsValidKeyLine(String line)
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