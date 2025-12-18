using System;
using System.Collections.Generic;
using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public class SaveOptionInfo : CustomControlInfo<SaveOptionControl, Option>
    {
        public override SaveOptionControl MakeControl(Option property, ListedControlController<Option> controller)
        {
            switch (property.InputType)
            {
                case OptionInputType.Number:
                    return new SaveOptionNumber(property, controller);
                case OptionInputType.Boolean:
                    return new SaveOptionBoolean(property, controller);
                case OptionInputType.String:
                    return new SaveOptionString(property, controller);
                case OptionInputType.ChoicesList:
                    return new SaveOptionChoices(property, controller);
                case OptionInputType.Color:
                    return new SaveOptionColor(property, controller);
                case OptionInputType.Palette:
                    throw new NotImplementedException("Not yet implemented.");
                //    return new SaveOptionPalette(property, controller);
                case OptionInputType.FileOpen:
                    throw new NotImplementedException("Not yet implemented.");
                case OptionInputType.FileSave:
                    throw new NotImplementedException("Not yet implemented.");
                case OptionInputType.Folder:
                    throw new NotImplementedException("Not yet implemented.");
                default:
                    return null;
            }
        }

        public override SaveOptionControl GetControlByProperty(Option property, IEnumerable<SaveOptionControl> controls)
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