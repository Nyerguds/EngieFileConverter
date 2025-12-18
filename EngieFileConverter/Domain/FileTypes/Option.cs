using System;
using System.Linq;

namespace Nyerguds.Util
{
    public class Option
    {
        /// <summary>
        /// Creates a new option for saving or loading.
        /// </summary>
        /// <param name="code">Code to easily retrieve this option.</param>
        /// <param name="inputType">Input data type</param>
        /// <param name="UiString">String to show on the UI for this option</param>
        /// <param name="data">The value of this option. Fill this in in advance to give a default value.</param>
        public Option(String code, OptionInputType inputType, String UiString, String data)
            : this(code, inputType, UiString, null, data) { }

        /// <summary>
        /// Creates a new option for saving or loading.
        /// </summary>
        /// <param name="code">Code to easily retrieve this option.</param>
        /// <param name="inputType">Input data type</param>
        /// <param name="UiString">String to show on the UI for this option</param>
        /// <param name="initValue">Initialisation value. Used differently by all types.</param>
        /// <param name="data">The value of this option. Fill this in in advance to give a default value.</param>
        public Option(String code, OptionInputType inputType, String UiString, String initValue, String data)
            : this(code, inputType, UiString, initValue, data, false) { }

        /// <summary>
        /// Creates a new option for saving or loading.
        /// </summary>
        /// <param name="code">Code to easily retrieve this option.</param>
        /// <param name="inputType">Input data type</param>
        /// <param name="UiString">String to show on the UI for this option</param>
        /// <param name="initValue">Initialisation value. Used differently by all types.</param>
        /// <param name="data">The value of this option. Fill this in in advance to give a default value.</param>
        /// <param name="filters">Filters. At least one of these filters needs to match to enable an option.</param>
        public Option(String code, OptionInputType inputType, String UiString, String initValue, String data, params EnableFilter[] filters)
            : this(code, inputType, UiString, initValue, data, false, filters) { }

        /// <summary>
        /// Creates a new option for saving or loading.
        /// </summary>
        /// <param name="code">Code to easily retrieve this option.</param>
        /// <param name="inputType">Input data type</param>
        /// <param name="UiString">String to show on the UI for this option</param>
        /// <param name="initValue">Initialisation value. Used differently by all types.</param>
        /// <param name="data">The value of this option. Fill this in in advance to give a default value.</param>
        /// <param name="filterAnd">True if all filters need to apply to enable or disable a control.</param>
        /// <param name="filters">Filters. Unless filterAnd is enabled, at least one of these filters needs to match to enable an option.</param>
        public Option(String code, OptionInputType inputType, String UiString, String initValue, String data, Boolean filterAnd, params EnableFilter[] filters)
        {
            this.Code = code;
            this.InputType = inputType;
            this.UiString = UiString;
            this.InitValue = initValue;
            this.Data = data;
            this.FilterAnd = filterAnd;
            // Remove circular references.
            this.Filters = filters.Where(f => f.CheckOption != code).ToArray();
        }

        /// <summary>Code to easily retrieve this option.</summary>
        public String Code { get; private set; }
        /// <summary>Input data type</summary>
        public OptionInputType InputType { get; private set; }
        /// <summary>String to show on the UI for this option</summary>
        public String UiString { get; private set; }
        /// <summary>Initialisation value. Used differently by all types.</summary>
        public String InitValue { get; private set; }
        /// <summary>The value of this option. Fill this in in advance to give a default value.</summary>
        public String Data { get; set; }
        /// <summary>True if all filters need to apply to enable or disable a control.</summary>
        public Boolean FilterAnd { get; set; }
        /// <summary>Filters. Unless FilterAnd is enabled, at least one of these filters needs to match to enable an option.</summary>
        public EnableFilter[] Filters { get; set; }


        public static String GetSaveOptionValue(Option[] list, String code)
        {
            Option option = GetSaveOption(list, code);
            return option == null ? null : option.Data;
        }

        public static Option GetSaveOption(Option[] list, String code)
        {
            if (list == null)
                return null;
            Int32 listLength = list.Length;
            for (Int32 i = 0; i < listLength; ++i)
            {
                Option option = list[i];
                if (option == null)
                    continue;
                if (String.Equals(option.Code, code, StringComparison.InvariantCultureIgnoreCase))
                    return option;
            }
            return null;
        }

        public override String ToString()
        {
            return this.Code + "=" + this.Data;
        }
    }

    public enum OptionInputType
    {
        /// <summary>Simple numeric input. InitValue can be left empty, or give a comma-separated minimum and/or maximum in the format "min,max".</summary>
        Number,
        /// <summary>Checkbox. Data value should always be either "0" and "1".</summary>
        Boolean,
        /// <summary>Free text field. If InitValue is specified, it limits the possible input characters to the characters inside the given string.</summary>
        String,
        /// <summary>Dropdown. Use InitValue to set a comma-separated list of options. Returns the chosen index (0-based) as string. SaveData can be used to set a default index.</summary>
        ChoicesList,
        /// <summary>Color selector. Set InitValue to "A" to enable alpha selector. Set to "T" to enable only transparency on/off selector.</summary>
        Color,
        /// <summary>To do. InitValue should be the palette bpp, then a pipe, and then a palette of comma-separated hex value triplets, and the initial selection index as SaveData.</summary>
        Palette,
        /// <summary>File selector. Use InitValue to specify a File Open mask.</summary>
        FileOpen,
        /// <summary>Additional file to be written. Use InitValue to specify a File Save mask.</summary>
        FileSave,
        /// <summary>Folder selector.</summary>
        Folder,
    }

    public class EnableFilter
    {
        public String CheckOption { get; set; }
        public String[] CheckMatchValues { get; set; }
        public Boolean WhenCheckMatches { get; set; }

        /// <summary>
        /// Creates a new instance of EnableFilter. This class can help disable options that are not relevant depending on the values set in other options.
        /// </summary>
        /// <param name="checkOption">Option to check.</param>
        /// <param name="whenCheckMatches">True if the filter enables if the check matches. If false, the filter enables when it does not match.</param>
        /// <param name="checkMatchValues">All possible values for checkOption that count as valid match.</param>
        public EnableFilter(String checkOption, Boolean whenCheckMatches, params String[] checkMatchValues)
        {
            this.CheckOption = checkOption;
            this.WhenCheckMatches = whenCheckMatches;
            this.CheckMatchValues = checkMatchValues;
        }
    }
}
