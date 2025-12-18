using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Nyerguds.GameData.Westwood
{
    public class SixBitColor
    {
        protected const String argError = "Color value can not be higher than 63!";

        protected Byte m_Red;
        protected Byte m_Green;
        protected Byte m_Blue;

        public Byte R
        {
            get { return m_Red; }
            set
            {
                if (value > 63)
                    throw new ArgumentException(argError, "value");
                else
                    this.m_Red = value;
            }
        }
        public Byte G
        {
            get { return m_Green; }
            set
            {
                if (value > 63)
                    throw new ArgumentException(argError, "value");
                else
                    this.m_Green = value;
            }
        }
        public Byte B
        {
            get { return m_Blue; }
            set
            {
                if (value > 63)
                    throw new ArgumentException(argError, "value");
                else
                    this.m_Blue = value;
            }
        }

        public SixBitColor(Byte red, Byte green, Byte blue)
        {
            R = red;
            G = green;
            B = blue;
        }

        public SixBitColor(Color color)
        {
            R = (Byte)(color.R / 4);
            G = (Byte)(color.G / 4);
            B = (Byte)(color.B / 4);
        }

        public Color GetAsColor()
        {
            return Color.FromArgb(R * 4, G * 4, B * 4);
        }

        public Byte[] GetAsByteArray()
        {
            return new Byte[] { R, G, B };
        }

        public void WriteToByteArray(Byte[] array, Int32 offset)
        {
            array[offset + 0] = R;
            array[offset + 1] = G;
            array[offset + 2] = B;
        }


        public override String ToString()
        {
            return String.Format("Values=({0}, {1}, {2}), RGB=({3}, {4}, {5})", R, G, B, R*4, G*4, B*4);
        }
    }
}
