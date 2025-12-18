using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    /// <summary>
    /// Enhanced NumericUpDown that allows catching the specific "value up/down" and "value entered" events
    /// instead of "value changed", to allow unnecessary calls on boxes where values are often typed in.
    /// Also offers a property to change the amount of items scrolled by the mouse scroll wheel.
    /// </summary>
    public class EnhNumericUpDown : NumericUpDown
    {
        [DefaultValue(1)]
        [Category("Data")]
        [Description("Indicates the amount to increment or decrement on mouse wheel scroll.")]
        public Int32 MouseWheelIncrement { get; set; }
        [Category("Action")]
        [Description("Occurs when the value is changed a single tick through either the up-down arrow keys, the up-down buttons or the scrollwheel.")]
        public event EventHandler<UpDownEventArgs> ValueUpDown;
        [Category("Action")]
        [Description("Occurs when the user presses the Enter key after changing the value.")]
        public event EventHandler<ValueEnteredEventArgs> ValueEntered;
        [Category("Data")]
        [Description("True to make the scrollwheel action cause validation on EnteredValue.")]
        [DefaultValue(true)]
        public Boolean ScrollValidatesEnter { get { return _ScrollValidatesEnter; } set { _ScrollValidatesEnter = value; } }
        [Category("Data")]
        [DefaultValue(true)]
        [Description("True to make the up-down arrow keys or controls cause validation on EnteredValue.")]
        public Boolean UpDownValidatesEnter { get { return _UpDownValidatesEnter; } set { _UpDownValidatesEnter = value; } }
        
        /// <summary>
        /// Last validated entered value.
        /// </summary>
        [Category("Data")]
        [DefaultValue(0)]
        [Description("The last validated value of the EnhNumericUpDownControl.")]
        public Decimal EnteredValue
        {
            get { return LimitRange(_EnteredValue);  }
            set
            {
                this.Value = value;
                ValidateValue();
            }
        }

        private Decimal _EnteredValue = 0;
        private Boolean _ScrollValidatesEnter = true;
        private Boolean _UpDownValidatesEnter = true;


        public EnhNumericUpDown()
        {
            this.MouseWheelIncrement = 1;
            this.KeyDown += CheckKeyPress;
            this.TextChanged += EnhNumericUpDown_TextChanged;
        }

        private void EnhNumericUpDown_TextChanged(object sender, EventArgs e)
        {
            Boolean allowminus = this.Minimum < 0;
            if (Regex.IsMatch(this.Text, allowminus ? "^-?\\d*$" : "^\\d*$"))
                return;
            // something snuck in, probably with ctrl+v. Remove it.
            System.Media.SystemSounds.Beep.Play();
            StringBuilder text = new StringBuilder();
            String txt = this.Text;
            Boolean hasMinus = txt.StartsWith("-");
            Int32 firstIllegalChar = 0;
            for (Int32 i = 0; i < txt.Length; i++)
            {
                Char c = txt[i];
                if ((c < '0' || c > '9') && (!allowminus || i > 0 || c != '-'))
                {
                    if (firstIllegalChar == 0)
                        firstIllegalChar = i;
                    continue;
                }
                text.Append(c);
            }
            Int32 value;
            if (Int32.TryParse(text.ToString(), out value))
            {
                value = Math.Max((Int32)this.Minimum, Math.Min((Int32)this.Maximum, value));
                // will trigger this function again, but that's okay, it'll immediately fail the regex and abort.
                this.Text = value.ToString();
            }
            this.Select(firstIllegalChar,0);
            
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandledMouseEventArgs hme = e as HandledMouseEventArgs;
            if (hme != null)
                hme.Handled = true;
            // fix for negative value input.
            if (this.MouseWheelIncrement < 0)
                this.MouseWheelIncrement = 1;
            UpDownAction action;
            if (e.Delta > 0)
            {
                this.Value = Math.Min(this.Maximum, this.Value + this.MouseWheelIncrement);
                action = UpDownAction.Up;
            }
            else if (e.Delta < 0)
            {
                this.Value = Math.Max(this.Minimum, this.Value - this.MouseWheelIncrement);
                action = UpDownAction.Down;
            }
            else
                return;
            if (ScrollValidatesEnter)
                ValidateValue();
            if (ValueUpDown != null)
                ValueUpDown(this, new UpDownEventArgs(action, this.MouseWheelIncrement, true));
        }

        private void CheckKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = ValidateValue();
            }
        }

        private Boolean ValidateValue()
        {
            Decimal oldval = this._EnteredValue;
            this._EnteredValue = this.Value;
            if (ValueEntered != null)
                ValueEntered(this, new ValueEnteredEventArgs(oldval));
            return true;
        }

        private Decimal LimitRange(Decimal value)
        {
            if (value > this.Maximum)
                value = this.Maximum;
            else if (value < this.Minimum)
                value = this.Minimum;
            return value;
        }

        /// <summary>
        /// Decrements the value of the spin box (also known as an up-down control).
        /// </summary>
        public override void DownButton()
        {
            base.DownButton();
            if (UpDownValidatesEnter)
                ValidateValue();
            if (ValueUpDown != null)
                ValueUpDown(this, new UpDownEventArgs(UpDownAction.Up));
        }

        /// <summary>
        /// Increments the value of the spin box (also known as an up-down control).
        /// </summary>
        public override void UpButton()
        {
            base.UpButton();
            if (UpDownValidatesEnter)
                ValidateValue();
            if (ValueUpDown != null)
                ValueUpDown(this, new UpDownEventArgs(UpDownAction.Down));
        }
    }

    public class ValueEnteredEventArgs : EventArgs
    {
        public Decimal Oldvalue;

        public ValueEnteredEventArgs(Decimal oldvalue)
        {
            this.Oldvalue = oldvalue;
        }
    }

    public class UpDownEventArgs : EventArgs
    {
        public UpDownAction Direction;
        public Int32 Increment;
        public Boolean FromMouseWheel;

        public UpDownEventArgs(UpDownAction direction)
            : this(direction, 1, false)
        { }

        public UpDownEventArgs(UpDownAction direction, Int32 increment, Boolean fromMouseWheel)
        {
            this.Direction = direction;
            this.Increment = increment;
            this.FromMouseWheel = fromMouseWheel;
        }
    }

    public enum UpDownAction
    {
        Up,
        Down
    }
}
