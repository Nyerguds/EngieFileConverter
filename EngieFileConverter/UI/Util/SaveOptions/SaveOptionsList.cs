using Nyerguds.Util.Ui;

namespace Nyerguds.Util.UI.SaveOptions
{
    public class SaveOptionsList : ControlsList<SaveOptionControl, Option>
    {
        protected override void FocusItem(SaveOptionControl control)
        {
            control.FocusValue();
        }
    }
}