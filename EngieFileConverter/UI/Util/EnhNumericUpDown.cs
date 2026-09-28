using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    /// <summary>
    /// Enhanced NumericUpDown that allows catching the specific "value up/down" and "value entered" events
    /// instead of "value changed", to avoid unnecessary calls on boxes where values are often typed in.
    /// Also offers a property to change the amount of items scrolled by the mouse scroll wheel.
    /// </summary>
    public class EnhNumericUpDown : NumericUpDown
    {
        [DefaultValue(1)]
        [Category("Data")]
        [Description("Indicates the amount to increment or decrement on mouse wheel scroll.")]
        public int MouseWheelIncrement { get; set; }
        [Category("Action")]
        [Description("Occurs when the value is changed a single tick through either the up-down arrow keys, the up-down buttons or the scrollwheel.")]
        public event EventHandler<UpDownEventArgs> ValueUpDown;
        [Category("Action")]
        [Description("Occurs when the user presses the Enter key after changing the value.")]
        public event EventHandler<ValueEnteredEventArgs> ValueEntered;
        [Category("Data")]
        [Description("True to make the scrollwheel action cause validation on EnteredValue.")]
        [DefaultValue(true)]
        public bool ScrollValidatesEnter { get { return this._ScrollValidatesEnter; } set { this._ScrollValidatesEnter = value; } }
        [Category("Data")]
        [DefaultValue(true)]
        [Description("True to make the up-down arrow keys or controls cause validation on EnteredValue.")]
        public bool UpDownValidatesEnter { get { return this._UpDownValidatesEnter; } set { this._UpDownValidatesEnter = value; } }

        /// <summary>
        /// Last validated entered value.
        /// </summary>
        [Category("Data")]
        [DefaultValue(0)]
        [Description("The last validated value of the EnhNumericUpDownControl.")]
        public decimal EnteredValue
        {
            get { return this.Constrain(this._EnteredValue);  }
            set
            {
                this.Value = this.Constrain(value);
                this.ValidateValue();
            }
        }

        private decimal _EnteredValue;
        private bool _ScrollValidatesEnter = true;
        private bool _UpDownValidatesEnter = true;
        private TextBox _TextBox;

        public EnhNumericUpDown()
        {
            this.MouseWheelIncrement = 1;
            this.KeyDown += this.CheckKeyPress;
            foreach (Control control in this.Controls)
            {
                if (control is TextBox)
                {
                    this._TextBox = control as TextBox;
                    break;
                }
            }
        }

        public TextBox TextBox { get { return this._TextBox; } }

        protected override void OnTextChanged(EventArgs e)
        {
            bool allowminus = this.Minimum < 0;
            bool allowHex = this.Hexadecimal;
            string pattern = allowHex ? "(\\d|[A-F])*" : (allowminus ? "-?\\d*" : "\\d*");
            if (Regex.IsMatch(this.Text, "^" + pattern + "$", RegexOptions.IgnoreCase))
                return;
            // something snuck in, probably with ctrl+v. Remove it.
            System.Media.SystemSounds.Beep.Play();
            StringBuilder text = new StringBuilder();
            string txt = this.Text.ToUpperInvariant();
            int txtLen = txt.Length;
            int firstIllegalChar = -1;
            for (int i = 0; i < txtLen; ++i)
            {
                char c = txt[i];
                bool isNumRange = (c >= '0' && c <= '9');
                bool isAllowedHexRange = allowHex && (c >= 'A' && c <= 'F');
                bool isAllowedMinus = (i == 0 && c == '-' && !allowHex);
                if (!isNumRange && !isAllowedHexRange && !isAllowedMinus)
                {
                    if (firstIllegalChar == -1)
                        firstIllegalChar = i;
                    continue;
                }
                text.Append(c);
            }
            string filteredText = text.ToString();
            decimal value;
            NumberStyles ns = allowHex ? NumberStyles.HexNumber : NumberStyles.Number;
            if (decimal.TryParse(filteredText, ns, NumberFormatInfo.CurrentInfo, out value))
            {
                value = Math.Max((int)this.Minimum, Math.Min(this.Maximum, value));
                // will trigger this function again, but that's okay, it'll immediately fail the regex and abort.
                this.Text = value.ToString(CultureInfo.InvariantCulture);
            }
            else
                this.Text = filteredText;
            if (firstIllegalChar == -1)
                firstIllegalChar = 0;
            this.Select(firstIllegalChar, 0);
        }

        /// <summary>Gets or sets the starting point of text selected in the text box.</summary>
        public int SelectionStart
        {
            get { return this._TextBox.SelectionStart; }
            set { this._TextBox.SelectionStart = value; }
        }

        /// <summary>Gets or sets the number of characters selected in the text box.</summary>
        public int SelectionLength
        {
            get { return this._TextBox.SelectionLength; }
            set { this._TextBox.SelectionLength = value; }
        }

        /// <summary>Gets or sets a value indicating the currently selected text in the control.</summary>
        public string SelectedText
        {
            get { return this._TextBox.SelectedText; }
            set { this._TextBox.SelectedText = value; }
        }

        public void SelectAll()
        {
            this._TextBox.SelectionStart = 0;
            this._TextBox.SelectionLength = this.TextBox.TextLength;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandledMouseEventArgs hme = e as HandledMouseEventArgs;
            if (hme != null)
                hme.Handled = true;
            int delta = e.Delta;
            int scroll = this.MouseWheelIncrement;
            // Negative increment is perfectly allowed, but will simply be handled as opposite direction scrolling.
            if (scroll < 0)
            {
                delta = -delta;
                scroll = -scroll;
            }
            UpDownAction action;
            if (delta > 0)
            {
                decimal value = this.Value + scroll;
                this.Value = Math.Min(this.Maximum, value);
                action = UpDownAction.Up;
            }
            else if (delta < 0)
            {
                decimal value = this.Value - scroll;
                this.Value = Math.Max(this.Minimum, value);
                action = UpDownAction.Down;
            }
            else
                return;
            if (this.ScrollValidatesEnter)
                this.ValidateValue();
            if (this.ValueUpDown != null)
                this.ValueUpDown(this, new UpDownEventArgs(action, scroll, true));
        }

        private void CheckKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = this.ValidateValue();
            }
        }

        private bool ValidateValue()
        {
            decimal oldval = this._EnteredValue;
            this._EnteredValue = this.Value;
            if (this.ValueEntered != null)
                this.ValueEntered(this, new ValueEnteredEventArgs(oldval));
            return true;
        }

        public decimal Constrain(decimal value)
        {
            if (value < this.Minimum)
                value = this.Minimum;
            else if (value > this.Maximum)
                value = this.Maximum;
            return value;
        }

        /// <summary>
        /// Decrements the value of the spin box (also known as an up-down control).
        /// </summary>
        public override void DownButton()
        {
            base.DownButton();
            //Decimal value = this.Value;
            //this.Value = Math.Max(this.Minimum, value);
            if (this.UpDownValidatesEnter)
                this.ValidateValue();
            if (this.ValueUpDown != null)
                this.ValueUpDown(this, new UpDownEventArgs(UpDownAction.Down));
        }

        /// <summary>
        /// Increments the value of the spin box (also known as an up-down control).
        /// </summary>
        public override void UpButton()
        {
            base.UpButton();
            //Decimal value = this.Value;
            //this.Value = Math.Min(this.Maximum, value);
            if (this.UpDownValidatesEnter)
                this.ValidateValue();
            if (this.ValueUpDown != null)
                this.ValueUpDown(this, new UpDownEventArgs(UpDownAction.Up));
        }

        // Sets the value without triggering the "OnValueChanged" event.
        protected void SetInternalValue(int value)
        {
            Type numUpDownType = this.GetType();
            FieldInfo init = numUpDownType.GetField("initializing");
            bool initializing = (bool)init.GetValue(this);

            if (!initializing && ((value < Minimum) || (value > Maximum)))
            {
                // Let the system take care of the 'out of range' exception.
                this.Value = value;
            }
            else
            {
                FieldInfo val = numUpDownType.GetField("currentValue");
                val.SetValue(this, value);
                FieldInfo valChanged = numUpDownType.GetField("currentValueChanged");
                valChanged.SetValue(this, true);
                UpdateEditText();
            }
        }
    }

    public class ValueEnteredEventArgs : EventArgs
    {
        public decimal Oldvalue;

        public ValueEnteredEventArgs(decimal oldvalue)
        {
            this.Oldvalue = oldvalue;
        }
    }

    public class UpDownEventArgs : EventArgs
    {
        public UpDownAction Direction;
        public int Increment;
        public bool FromMouseWheel;

        public UpDownEventArgs(UpDownAction direction)
            : this(direction, 1, false)
        { }

        public UpDownEventArgs(UpDownAction direction, int increment, bool fromMouseWheel)
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
