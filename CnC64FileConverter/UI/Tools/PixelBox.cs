using System;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
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
            this.InterpolationMode = InterpolationMode.NearestNeighbor;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the interpolation mode.
        /// </summary>
        /// <value>The interpolation mode.</value>
        [Category("Behavior")]
        [DefaultValue(InterpolationMode.NearestNeighbor)]
        public InterpolationMode InterpolationMode { get; set; }
        #endregion

        #region Overrides of PictureBox
        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
        /// </summary>
        /// <param name="pe">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data. </param>
        protected override void OnPaint (PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = this.InterpolationMode;
            // docs on this are wrong; if the interpolation mode is NearestNeighbor, putting it on
            // Half makes it NOT shift the whole thing up and to the left by half a (zoomed) pixel.
            // I'm frankly baffled they didn't just make this an automatic part of the interpolation modes.
            if (this.InterpolationMode == InterpolationMode.NearestNeighbor)
                pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            base.OnPaint(pe);
        }
        #endregion
    }
}
