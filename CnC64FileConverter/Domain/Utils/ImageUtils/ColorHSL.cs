using System;
using System.Drawing;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// From http://richnewman.wordpress.com/about/code-listings-and-diagrams/hslcolor-class/
    /// </summary>
    public class ColorHSL
    {
        // Private data members below are on scale 0-1
        // They are scaled for use externally based on scale
        private Double hue = 1.0;
        private Double saturation = 1.0;
        private Double luminosity = 1.0;

        public static Double SCALE = 240.0;

        public Double Hue
        {
            get { return this.hue * SCALE; }
            set { this.hue = this.CheckRange(value / SCALE); }
        }
        public Double Saturation
        {
            get { return this.saturation * SCALE; }
            set { this.saturation = this.CheckRange(value / SCALE); }
        }
        public Double Luminosity
        {
            get { return this.luminosity * SCALE; }
            set { this.luminosity = this.CheckRange(value / SCALE); }
        }

        private Double CheckRange(Double value)
        {
            if (value < 0.0)
                value = 0.0;
            else if (value > 1.0)
                value = 1.0;
            return value;
        }

        public override String ToString()
        {
            return String.Format("H: {0:#0.##} S: {1:#0.##} L: {2:#0.##}", this.Hue, this.Saturation, this.Luminosity);
        }

        public String ToRGBString()
        {
            Color color = (Color)this;
            return String.Format("R: {0:#0.##} G: {1:#0.##} B: {2:#0.##}", color.R, color.G, color.B);
        }

        #region Casts to/from System.Drawing.Color
        public static implicit operator Color(ColorHSL hslColor)
        {
            Double r = 0, g = 0, b = 0;
            if ((Int32)(hslColor.luminosity * 1000) != 0)
            {
                if ((Int32)(hslColor.saturation * 1000) == 0)
                    r = g = b = hslColor.luminosity;
                else
                {
                    Double temp2 = GetTemp2(hslColor);
                    Double temp1 = 2.0 * hslColor.luminosity - temp2;

                    r = GetColorComponent(temp1, temp2, hslColor.hue + 1.0 / 3.0);
                    g = GetColorComponent(temp1, temp2, hslColor.hue);
                    b = GetColorComponent(temp1, temp2, hslColor.hue - 1.0 / 3.0);
                }
            }
            return Color.FromArgb((Int32)(255 * r), (Int32)(255 * g), (Int32)(255 * b));
        }

        private static Double GetColorComponent(Double temp1, Double temp2, Double temp3)
        {
            temp3 = MoveIntoRange(temp3);
            if (temp3 < 1.0 / 6.0)
                return temp1 + (temp2 - temp1) * 6.0 * temp3;
            if (temp3 < 0.5)
                return temp2;
            if (temp3 < 2.0 / 3.0)
                return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
            return temp1;
        }
        private static Double MoveIntoRange(Double temp3)
        {
            if (temp3 < 0.0)
                temp3 += 1.0;
            else if (temp3 > 1.0)
                temp3 -= 1.0;
            return temp3;
        }
        private static Double GetTemp2(ColorHSL hslColor)
        {
            Double temp2;
            if (hslColor.luminosity < 0.5)  //<=??
                temp2 = hslColor.luminosity * (1.0 + hslColor.saturation);
            else
                temp2 = hslColor.luminosity + hslColor.saturation - (hslColor.luminosity * hslColor.saturation);
            return temp2;
        }

        public static implicit operator ColorHSL(Color color)
        {
            ColorHSL hslColor = new ColorHSL();
            hslColor.hue = color.GetHue() / 360.0; // we store hue as 0-1 as opposed to 0-360
            hslColor.luminosity = color.GetBrightness();
            hslColor.saturation = color.GetSaturation();
            return hslColor;
        }
        #endregion

        public void SetRGB(Int32 red, Int32 green, Int32 blue)
        {
            ColorHSL hslColor = (ColorHSL)Color.FromArgb(red, green, blue);
            this.hue = hslColor.hue;
            this.saturation = hslColor.saturation;
            this.luminosity = hslColor.luminosity;
        }

        public ColorHSL() { }
        public ColorHSL(Color color)
        {
            this.SetRGB(color.R, color.G, color.B);
        }
        public ColorHSL(Int32 red, Int32 green, Int32 blue)
        {
            this.SetRGB(red, green, blue);
        }
        public ColorHSL(Double hue, Double saturation, Double luminosity)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Luminosity = luminosity;
        }

    }
}