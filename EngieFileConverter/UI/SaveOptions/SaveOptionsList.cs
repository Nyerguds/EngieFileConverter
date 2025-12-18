using EngieFileConverter.Domain.FileTypes;
using Nyerguds.Util.Ui;

namespace EngieFileConverter.UI.SaveOptions
{
    public class SaveOptionsList : ControlsList<SaveOptionControl, SaveOption>
    {
        protected override void FocusItem(SaveOptionControl control)
        {
            control.FocusValue();
        }
    }
}