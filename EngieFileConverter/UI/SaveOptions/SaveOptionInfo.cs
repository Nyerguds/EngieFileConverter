using System;
using System.Collections.Generic;
using EngieFileConverter.Domain.FileTypes;
using EngieFileConverter.UI.SaveOptions;
using Nyerguds.Util.Ui;

namespace EngieFileConverter.UI
{
    public class SaveOptionInfo : CustomControlInfo<SaveOptionControl, SaveOption>
    {
        public override SaveOptionControl MakeControl(SaveOption property, ListedControlController<SaveOption> controller)
        {
            switch (property.Type)
            {
                case SaveOptionType.Number:
                    return new SaveOptionNumber(property, controller);
                case SaveOptionType.Boolean:
                    return new SaveOptionBoolean(property, controller);
                case SaveOptionType.String:
                    return new SaveOptionString(property, controller);
                case SaveOptionType.ChoicesList:
                    return new SaveOptionChoices(property, controller);
                case SaveOptionType.Color:
                    return new SaveOptionColor(property, controller);
                case SaveOptionType.FileOpen:
                    throw new NotImplementedException("Not yet implemented.");
                case SaveOptionType.FileSave:
                    throw new NotImplementedException("Not yet implemented.");
                case SaveOptionType.Folder:
                    throw new NotImplementedException("Not yet implemented.");
                default:
                    return null;
            }
        }

        public override SaveOptionControl GetControlByProperty(SaveOption property, IEnumerable<SaveOptionControl> controls)
        {
            if (property == null || controls == null)
                return null;
            foreach (SaveOptionControl soc in controls)
            {
                if (soc.Info != null && soc.Info.Code == property.Code)
                    return soc;
            }
            return null;
        }

    }
}