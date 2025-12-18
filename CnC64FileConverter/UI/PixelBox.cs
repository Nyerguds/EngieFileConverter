using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RedCell.UI.Controls
{
    /// <summary>
    /// A PictureBox with configurable interpolation mode.
    /// </summary>
    public class PixelBox : PictureBox
    {
        #region Initialization
        /// <summary>
        /// Initializes a new instance of the <see cref="PixelBox"/> class.
        /// </summary>
        public PixelBox ()
        {
            // Set default.
            InterpolationMode = InterpolationMode.Default;
        }

        void PixelBox_Paint(object sender, PaintEventArgs e)
        {

            /*/
            using (Brush brush = new SolidBrush(this.BackColor))
            {
                switch 
                Double scaleFactor = Math.Min(this.Width / this.Image.Width, this.Height / this.Image.Height);

                e.Graphics.DrawRectangle(
            }
            //e.Graphics.DrawImage(BMP, New Rectangle(0, 0, BMP.Width * PScale, BMP.Height * PScale), New Rectangle(0, 0, BMP.Width, BMP.Height), GraphicsUnit.Pixel)
            //*/
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the interpolation mode.
        /// </summary>
        /// <value>The interpolation mode.</value>
        [Category("Behavior")]
        [DefaultValue(InterpolationMode.Default)]
        public InterpolationMode InterpolationMode { get; set; }
        #endregion

        #region Overrides of PictureBox
        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
        /// </summary>
        /// <param name="pe">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data. </param>
        protected override void OnPaint (PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode;
            // docs on this are wrong; putting it to Half makes it not shift the whole thing up and to the left by half a (zoomed) pixel.
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            base.OnPaint(pe);
        }
        #endregion
    }
}
