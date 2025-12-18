using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain.FileTypes
{
    public class SaveOption
    {
        public SaveOption(SaveOptionType type, String name, String initValue)
        {
            this.Type = type;
            this.OptionName = name;
            this.InitValue = initValue;
        }
        
        public SaveOptionType Type { get; private set; }
        public String OptionName { get; private set; }
        public String InitValue { get; private set; }
        public String SaveData { get; set; }
    }

    public enum SaveOptionType
    {
        // Simple numeric input. Avoid setting non-numeric input in InitValue.
        Integer,
        // Checkbox. Set InitValue to "0" or "1".
        Boolean,
        // Free text field.
        String,
        // Dropdown. Use InitValue to set a semicolon-separated list of options. The first one will be the default value. Returns the chosen index as string.
        ChoicesList,
        // File selector. Any InitValue will be evaluated as relative to the save path. Use "*.EXT" for same filename as save file but with a different extension. USe NAME.*
        FileOpen,
        // File save dialog. Same InitValue format as FileOpen.
        FileSave,
        // Folder selector.
        Folder,
    }
}
