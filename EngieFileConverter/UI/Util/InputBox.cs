using System;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    /// <summary>
    /// Shows a prompt in a dialog box using the static method Show().
    /// </summary>
    public partial class InputBox: Form
    {
        public delegate String TextValidator(String text);

        public TextValidator Validator { get; set; }

        private Boolean textChanging;

        private InputBox()
        {
            this.InitializeComponent();
        }

        private void buttonCancel_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonOK_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response</param>
        /// <param name="xpos">Numeric expression that specifies the distance of the left edge of the dialog box from the left edge of the screen.</param>
        /// <param name="ypos">Numeric expression that specifies the distance of the upper edge of the dialog box from the top of the screen</param>
        /// <param name="validator">Text validator function. Leave null for no restriction on text.</param>
        /// <param name="startPosition">Form start position</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        private static String Show(String prompt, String title, String defaultText, TextValidator validator, Int32 xpos, Int32 ypos, FormStartPosition startPosition)
        {
            using (InputBox form = new InputBox())
            {
                form.labelPrompt.Text = prompt;
                form.Text = title;
                form.Validator = validator;
                form.textBoxText.Text = defaultText;
                if (startPosition == FormStartPosition.Manual && xpos >= 0 && ypos >= 0)
                {
                    form.Left = xpos;
                    form.Top = ypos;
                }
                else
                    form.StartPosition = startPosition;
                DialogResult result = form.ShowDialog();
                if (result != DialogResult.OK)
                    return null;
                return form.textBoxText.Text;
            }
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response</param>
        /// <param name="validator">Text validator function. Leave null for no restriction on text.</param>
        /// <param name="xpos">Numeric expression that specifies the distance of the left edge of the dialog box from the left edge of the screen.</param>
        /// <param name="ypos">Numeric expression that specifies the distance of the upper edge of the dialog box from the top of the screen</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        public static String Show(String prompt, String title, String defaultText, TextValidator validator, Int32 xpos, Int32 ypos)
        {
            return Show(prompt, title, defaultText, validator, xpos, ypos, FormStartPosition.Manual);
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response</param>
        /// <param name="validator">Text validator function. Leave null for no restriction on text.</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        public static String Show(String prompt, String title, String defaultText, TextValidator validator)
        {
            return Show(prompt, title, defaultText, validator, -1, -1, FormStartPosition.CenterScreen);
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response</param>
        /// <param name="validator">Text validator function. Leave null for no restriction on text.</param>
        /// <param name="startPosition">Form start position</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        public static String Show(String prompt, String title, String defaultText, TextValidator validator, FormStartPosition startPosition)
        {
            return Show(prompt, title, defaultText, validator, -1, -1, startPosition);
        }

        private void textBoxText_TextChanged(Object sender, EventArgs e)
        {
            if (this.Validator == null || this.textChanging)
                return;
            try
            {
                this.textChanging = true;
                String input = this.textBoxText.Text;
                Int32 selStart = this.textBoxText.SelectionStart;
                String output = this.Validator(input);
                Int32 diff = input.Length - output.Length;
                Boolean wasFiltered = !String.Equals(input, output);
                Boolean wasAccepted = diff == 0;
                if (!wasFiltered)
                    return;
                // Update text field
                this.textBoxText.Text = output;
                this.textBoxText.SelectionStart = Math.Max(0, Math.Min(selStart - diff, this.textBoxText.Text.Length));
                this.textBoxText.SelectionLength = 0;
                if (!wasAccepted)
                    System.Media.SystemSounds.Asterisk.Play();
            }
            finally
            {
                this.textChanging = false;
            }
        }
    }
}
