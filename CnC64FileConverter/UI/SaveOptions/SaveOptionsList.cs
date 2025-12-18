using CnC64FileConverter.Domain.FileTypes;
using Nyerguds.Util.Ui;

namespace CnC64FileConverter.UI.SaveOptions
{
    public class SaveOptionsList : ControlsList<SaveOptionControl, SaveOption>
    {
        protected override void FocusItem(SaveOptionControl control)
        {
            control.FocusValue();
        }
    }
}