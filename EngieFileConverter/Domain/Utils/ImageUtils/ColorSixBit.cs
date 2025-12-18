using System;
using System.Drawing;

namespace Nyerguds.ImageManipulation
{
    public class ColorSixBit
    {
        //public const Double ConvertValTo8 = 255D / 63D; // 4,0476190
        //public const Double ConvertValTo6 = 63D / 255D; // 0,2470588
        public const Byte MaxValue = 63;
        private const String ArgError = "Color value can not be higher than {0}!";

        private static readonly Byte[] ConvertToEightBit = new Byte[64];
        private static readonly Byte[] ConvertToSixBit = new Byte[256];

        static ColorSixBit()
        {
            // Build easy lookup tables for this, so no calculations are ever needed for this later.
            for (Int32 i = 0; i < 64; ++i)
                ConvertToEightBit[i] = (Byte)(i * 255 / 63);
            for (Int32 i = 0; i < 256; ++i)
                ConvertToSixBit[i] = (Byte)(i * 63 / 255);
        }

        protected Byte m_Red;
        protected Byte m_Green;
        protected Byte m_Blue;

        public Byte R
        {
            get { return this.m_Red; }
            set
            {
                if (value > MaxValue)
                    throw new ArgumentException(String.Format(ArgError, MaxValue), "value");
                this.m_Red = value;
            }
        }
        public Byte G
        {
            get { return this.m_Green; }
            set
            {
                if (value > MaxValue)
                    throw new ArgumentException(String.Format(ArgError, MaxValue), "value");
                this.m_Green = value;
            }
        }
        public Byte B
        {
            get { return this.m_Blue; }
            set
            {
                if (value > MaxValue)
                    throw new ArgumentException(String.Format(ArgError, MaxValue), "value");
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
            this.m_Red = ConvertToSixBit[color.R];
            this.m_Green = ConvertToSixBit[color.G];
            this.m_Blue = ConvertToSixBit[color.B];
        }

        public Color GetAsColor()
        {
            return Color.FromArgb(ConvertToEightBit[this.m_Red], ConvertToEightBit[this.m_Green], ConvertToEightBit[this.m_Blue]);
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
        
        public static implicit operator Color(ColorSixBit color)
        {
            return color.GetAsColor();
        }

        public override String ToString()
        {
            return String.Format("Values=({0}, {1}, {2}), RGB=({3}, {4}, {5})", this.m_Red, this.m_Green, this.m_Blue, ConvertToEightBit[this.m_Red], ConvertToEightBit[this.m_Green], ConvertToEightBit[this.m_Blue]);
        }
    }
}
