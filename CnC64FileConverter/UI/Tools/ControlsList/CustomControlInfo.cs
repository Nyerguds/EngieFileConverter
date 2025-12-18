using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Nyerguds.Util.Ui
{
    public abstract class CustomControlInfo<T, U> where T : Control
    {
        public String Name { get; set; }
        public String ClassName { get; set; }
        public U[] Properties { get; set; }

        public abstract T MakeControl(U property, ListedControlController<U> controller);
        public abstract T GetControlByProperty(U property, IEnumerable<T> controls);

        public override String ToString()
        {
            return this.Name;
        }
    }
}
