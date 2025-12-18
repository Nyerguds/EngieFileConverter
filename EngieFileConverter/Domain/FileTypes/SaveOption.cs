using System;
using System.Linq;
using System.Collections.Generic;

namespace EngieFileConverter.Domain.FileTypes
{
    public class SaveOption
    {
        public SaveOption(String code, SaveOptionType type, String UiString, String saveData)
            : this(code, type, UiString, null, saveData) { }

        public SaveOption(String code, SaveOptionType type, String UiString, String initValue, String saveData, params SaveEnableFilter[] filters)
        {
            this.Code = code;
            this.Type = type;
            this.UiString = UiString;
            this.InitValue = initValue;
            this.SaveData = saveData;
            this.Filters = filters.Where(f => f.CheckOption != code).ToArray();
        }

        /// <summary>Code to easily retrieve this option</summary>
        public String Code { get; private set; }
        /// <summary>Data type</summary>
        public SaveOptionType Type { get; private set; }
        /// <summary>String to show on the UI for this option</summary>
        public String UiString { get; private set; }
        /// <summary>Initialisation value. Used differently by all types.</summary>
        public String InitValue { get; private set; }
        /// <summary>The value of this option. Fill this in in advance to give a default value.</summary>
        public String SaveData { get; set; }
        /// <summary>Filters. If given, all filters need to match to enable an option.</summary>
        public SaveEnableFilter[] Filters { get; set; }


        public static String GetSaveOptionValue(SaveOption[] list, String code)
        {
            foreach (SaveOption option in list)
            {
                if (String.Equals(option.Code, code, StringComparison.InvariantCultureIgnoreCase))
                    return option.SaveData;
            }
            return null;
        }
    }

    public enum SaveOptionType
    {
        /// <summary>Simple numeric input.</summary>
        Number,
        /// <summary>Checkbox. Values should be "0" and "1".</summary>
        Boolean,
        /// <summary>Free text field. If InitValue is specified, it limits the input to the given characters.</summary>
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

    public class SaveEnableFilter
    {
        public String CheckOption { get; set; }
        public String[] CheckValues { get; set; }
        public Boolean CheckInverted { get; set; }

        public SaveEnableFilter(String checkOption, Boolean checkInverted, params String[] checkValues)
        {
            this.CheckOption = checkOption;
            this.CheckInverted = checkInverted;
            this.CheckValues = checkValues;
        }
    }
}
