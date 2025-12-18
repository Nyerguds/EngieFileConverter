using System;

namespace EngieFileConverter.Domain.FileTypes
{
    public class SaveOption
    {
        public SaveOption(String code, SaveOptionType type, String UiString, String saveData)
            : this(code, type, UiString, null, saveData) { }

        public SaveOption(String code, SaveOptionType type, String UiString, String initValue, String saveData)
            : this(code, type, UiString, initValue, saveData, null, null, false) { }
        public SaveOption(String code, SaveOptionType type, String UiString, String initValue, String saveData, String parentOption, String parentCheckValue, Boolean parentCheckInverted)
        {
            this.Code = code;
            this.Type = type;
            this.UiString = UiString;
            this.InitValue = initValue;
            this.SaveData = saveData;
            if (!String.Equals(parentOption, code))
                this.ParentOption = parentOption;
            this.ParentCheckValue = parentCheckValue;
            this.ParentCheckInverted = parentCheckInverted;
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
        /// <summary>Parent option. If filled in, this option will only be enabled if the ParentCheckValue matches the SaveData of the parent.</summary>
        public String ParentOption{ get; set; }
        /// <summary>If ParentOption is enabled, gives the value to match to the parent's SaveData.</summary>
        public String ParentCheckValue { get; set; }
        /// <summary>True if this option is enabled when the ParentCheckValue check fails.</summary>
        public Boolean ParentCheckInverted { get; set; }

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
        /// <summary>File selector. Use InitValue to specify a File Open mask.</summary>
        FileOpen,
        /// <summary>Additional file to be written. Use InitValue to specify a File Save mask.</summary>
        FileSave,
        /// <summary>Folder selector.</summary>
        Folder,
    }
}
