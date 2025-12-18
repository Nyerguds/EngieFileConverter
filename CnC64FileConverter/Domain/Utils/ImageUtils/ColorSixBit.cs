using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Nyerguds.ImageManipulation
{
    public class ColorSixBit
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
                this.m_Blue = value;
            }
        }

        public ColorSixBit(Byte red, Byte green, Byte blue)
        {
            this.R = red;
            this.G = green;
            this.B = blue;
        }

        public ColorSixBit(Color color)
        {
            this.m_Red = (Byte)(color.R / 4);
            this.m_Green = (Byte)(color.G / 4);
            this.m_Blue = (Byte)(color.B / 4);
        }

        public Color GetAsColor()
        {
            return Color.FromArgb(this.m_Red * 4, this.m_Green * 4, this.m_Blue * 4);
        }

        public Byte[] GetAsByteArray()
        {
            return new Byte[] { this.m_Red, this.m_Green, this.m_Blue };
        }

        public void WriteToByteArray(Byte[] array, Int32 offset)
        {
            array[offset + 0] = this.m_Red;
            array[offset + 1] = this.m_Green;
            array[offset + 2] = this.m_Blue;
        }

        public override String ToString()
        {
            return String.Format("Values=({0}, {1}, {2}), RGB=({3}, {4}, {5})", this.m_Red, this.m_Green, this.m_Blue, this.m_Red * 4, this.m_Green * 4, this.m_Blue * 4);
        }
    }
}
