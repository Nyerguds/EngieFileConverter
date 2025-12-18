using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Class to automate the unpacking (and packing/writing) of RGB(A) colours in colour formats with packed bits.
    /// Inspired by https://github.com/scummvm/scummvm/blob/master/graphics/pixelformat.h
    /// This class works slightly differently than the ScummVM version, using 4-entry arrays for all data, with each entry
    /// representing one of the colour components, so the code can easily loop over them and perform the same action on each one.
    /// </summary>
    public class PixelFormatter
    {
        /// <summary>Standard PixelFormatter for .Net's RGBA format.</summary>
        public static PixelFormatter Format32BitArgb = new PixelFormatter(4, 0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF, true);
        public static PixelFormatter Format16BitArgb1555 = new PixelFormatter(2, 0x8000, 0x7C00, 0x3E0, 0x1F, true);

        public Int32 BytesPerPixel { get { return this.bytesPerPixel; } }
        public Boolean LittleEndian { get { return this.littleEndian; } }
        public ReadOnlyCollection<UInt32> LimitMasks { get { return new List<UInt32>(this.limitMasks).AsReadOnly(); } }
        public ReadOnlyCollection<Byte> BitsAmounts { get { return new List<Byte>(this.bytesPerPixel).AsReadOnly(); } }

        /// <summary>Number of bytes to read per pixel.</summary>
        private Byte bytesPerPixel;
        /// <summary>Masks to limit the amount of bits for each component, derived from the bitsAmounts.</summary>
        private UInt32[] limitMasks = new UInt32[4];
        /// <summary>Amount of bits for each component (A,R,G,B)</summary>
        private Byte[] bitsAmounts = new Byte[4];
        /// <summary>Multiplier for each component (A,R,G,B). If not explicitly given this can be derived from the number of bits.</summary>
        private Double[] multipliers = new Double[4];
        /// <summary>Maximum value for each component (A,R,G,B)</summary>
        private UInt32[] maxChan = new UInt32[4];
        /// <summary>Defaults for each component (A,R,G,B)</summary>
        private UInt32[] defaultsChan = new UInt32[4];
        /// <summary>True to read the input bytes as little-endian.</summary>
        private Boolean littleEndian;

        /// <summary>The colour components. Though most stuff will just loop an int from 0 to 4, this shows the order.</summary>
        private enum ColorComponent
        {
            Alpha = 0,
            Red = 1,
            Green = 2,
            Blue = 3,
        }

        /// <summary>
        /// Creates a new PixelFormatter based on bit masks.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel.</param>
        /// <param name="maskAlpha">Bit mask for alpha component.</param>
        /// <param name="maskRed">Bit mask for red component.</param>
        /// <param name="maskGreen">Bit mask for green component.</param>
        /// <param name="maskBlue">Bit mask for blue component.</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(Byte bytesPerPixel, UInt32 maskAlpha, UInt32 maskRed, UInt32 maskGreen, UInt32 maskBlue, Boolean littleEndian)
            : this(bytesPerPixel, maskAlpha, -1, maskRed, -1, maskGreen, -1, maskBlue, -1, littleEndian)
        { }

        /// <summary>
        /// Creates a new PixelFormatter based on bit masks.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel.</param>
        /// <param name="maskAlpha">Bit mask for alpha component.</param>
        /// <param name="alphaMultiplier">Multiplier for alpha component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="maskRed">Bit mask for red component.</param>
        /// <param name="redMultiplier">Multiplier for red component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="maskGreen">Bit mask for green component.</param>
        /// <param name="greenMultiplier">Multiplier for green component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="maskBlue">Bit mask for blue component.</param>
        /// <param name="blueMultiplier">Multiplier for blue component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(Byte bytesPerPixel,
            UInt32 maskAlpha, Double alphaMultiplier,
            UInt32 maskRed, Double redMultiplier,
            UInt32 maskGreen, Double greenMultiplier,
            UInt32 maskBlue, Double blueMultiplier,
            Boolean littleEndian)
        {
            this.bytesPerPixel = bytesPerPixel;
            this.littleEndian = littleEndian;

            Byte alphaBits = this.BitsFromMask(maskAlpha);
            Byte redBits = this.BitsFromMask(maskRed);
            Byte greenBits = this.BitsFromMask(maskGreen);
            Byte blueBits = this.BitsFromMask(maskBlue);

            this.bitsAmounts[(Int32) ColorComponent.Alpha] = alphaBits;
            this.multipliers[(Int32) ColorComponent.Alpha] = alphaMultiplier >= 0 ? alphaMultiplier : MakeMultiplier(alphaBits);
            this.limitMasks[(Int32) ColorComponent.Alpha] = maskAlpha;
            this.maxChan[(Int32) ColorComponent.Alpha] = MakeMaxVal(alphaBits);
            this.defaultsChan[(Int32) ColorComponent.Alpha] = this.maxChan[(Int32) ColorComponent.Alpha];

            this.bitsAmounts[(Int32) ColorComponent.Red] = redBits;
            this.multipliers[(Int32) ColorComponent.Red] = redMultiplier >= 0 ? redMultiplier : MakeMultiplier(redBits);
            this.limitMasks[(Int32) ColorComponent.Red] = maskRed;
            this.maxChan[(Int32) ColorComponent.Red] = MakeMaxVal(redBits);
            this.defaultsChan[(Int32) ColorComponent.Red] = 0;

            this.bitsAmounts[(Int32) ColorComponent.Green] = greenBits;
            this.multipliers[(Int32) ColorComponent.Green] = greenMultiplier >= 0 ? greenMultiplier : MakeMultiplier(greenBits);
            this.limitMasks[(Int32) ColorComponent.Green] = maskGreen;
            this.maxChan[(Int32) ColorComponent.Green] = MakeMaxVal(greenBits);
            this.defaultsChan[(Int32) ColorComponent.Green] = 0;

            this.bitsAmounts[(Int32) ColorComponent.Blue] = blueBits;
            this.multipliers[(Int32) ColorComponent.Blue] = blueMultiplier >= 0 ? blueMultiplier : MakeMultiplier(blueBits);
            this.limitMasks[(Int32) ColorComponent.Blue] = maskBlue;
            this.maxChan[(Int32) ColorComponent.Blue] = MakeMaxVal(blueBits);
            this.defaultsChan[(Int32) ColorComponent.Blue] = 0;
        }

        /// <summary>
        /// Creats a new PixelFormatter, with automatic calculation of colour multipliers using the CalculateMultiplier function.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel</param>
        /// <param name="alphaBits">Amount of bits to read for the alpha colour component</param>
        /// <param name="alphaShift">Amount of bits to shift the data to get to the alpha colour component</param>
        /// <param name="redBits">Amount of bits to read for the red colour component</param>
        /// <param name="redShift">Amount of bits to shift the data to get to the red colour component</param>
        /// <param name="greenBits">Amount of bits to read for the green colour component</param>
        /// <param name="greenShift">Amount of bits to shift the data to get to the green colour component</param>
        /// <param name="blueBits">Amount of bits to read for the blue colour component</param>
        /// <param name="blueShift">Amount of bits to shift the data to get to the blue colour component</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(Byte bytesPerPixel,
            Byte alphaBits, Byte alphaShift,
            Byte redBits, Byte redShift,
            Byte greenBits, Byte greenShift,
            Byte blueBits, Byte blueShift,
            Boolean littleEndian)
            : this(bytesPerPixel, alphaBits, alphaShift, -1, redBits, redShift, -1, greenBits, greenShift, -1,
                blueBits, blueShift, -1, littleEndian)
        { }

        /// <summary>
        /// Creates a new PixelFormatter.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel</param>
        /// <param name="alphaBits">Amount of bits to read for the alpha colour component</param>
        /// <param name="alphaShift">Amount of bits to shift the data to get to the alpha colour component</param>
        /// <param name="alphaMultiplier">Multiplier for the alpha component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="redBits">Amount of bits to read for the red colour component</param>
        /// <param name="redShift">Amount of bits to shift the data to get to the red colour component</param>
        /// <param name="redMultiplier">Multiplier for the red component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="greenBits">Amount of bits to read for the green colour component</param>
        /// <param name="greenShift">Amount of bits to shift the data to get to the green colour component</param>
        /// <param name="greenMultiplier">Multiplier for the green component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="blueBits">Amount of bits to read for the blue colour component</param>
        /// <param name="blueShift">Amount of bits to shift the data to get to the blue colour component</param>
        /// <param name="blueMultiplier">Multiplier for the blue component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(Byte bytesPerPixel,
            Byte alphaBits, Byte alphaShift, Double alphaMultiplier,
            Byte redBits, Byte redShift, Double redMultiplier,
            Byte greenBits, Byte greenShift, Double greenMultiplier,
            Byte blueBits, Byte blueShift, Double blueMultiplier,
            Boolean littleEndian)
        {
            this.bytesPerPixel = bytesPerPixel;
            this.littleEndian = littleEndian;
            UInt32 maxValAlpha = MakeMaxVal(alphaBits);

            this.bitsAmounts[(Int32) ColorComponent.Alpha] = alphaBits;
            this.multipliers[(Int32) ColorComponent.Alpha] = alphaMultiplier >= 0 ? alphaMultiplier : MakeMultiplier(alphaBits);
            this.limitMasks[(Int32) ColorComponent.Alpha] = MakeMask(alphaBits, alphaShift);
            this.maxChan[(Int32) ColorComponent.Alpha] = maxValAlpha;
            this.defaultsChan[(Int32) ColorComponent.Alpha] = maxValAlpha;

            this.bitsAmounts[(Int32) ColorComponent.Red] = redBits;
            this.multipliers[(Int32) ColorComponent.Red] = redMultiplier >= 0 ? redMultiplier : MakeMultiplier(redBits);
            this.limitMasks[(Int32) ColorComponent.Red] = MakeMask(redBits, redShift);
            this.maxChan[(Int32) ColorComponent.Red] = MakeMaxVal(redBits);
            this.defaultsChan[(Int32) ColorComponent.Red] = 0;

            this.bitsAmounts[(Int32) ColorComponent.Green] = greenBits;
            this.multipliers[(Int32) ColorComponent.Green] = greenMultiplier >= 0 ? greenMultiplier : MakeMultiplier(greenBits);
            this.limitMasks[(Int32) ColorComponent.Green] = MakeMask(greenBits, greenShift);
            this.maxChan[(Int32) ColorComponent.Green] = MakeMaxVal(greenBits);
            this.defaultsChan[(Int32) ColorComponent.Green] = 0;

            this.bitsAmounts[(Int32) ColorComponent.Blue] = blueBits;
            this.multipliers[(Int32) ColorComponent.Blue] = blueMultiplier >= 0 ? blueMultiplier : MakeMultiplier(blueBits);
            this.limitMasks[(Int32) ColorComponent.Blue] = MakeMask(blueBits, blueShift);
            this.maxChan[(Int32) ColorComponent.Blue] = MakeMaxVal(blueBits);
            this.defaultsChan[(Int32) ColorComponent.Blue] = 0;
        }

        /// <summary>
        /// Counts the amount of bits in a mask.
        /// </summary>
        /// <param name="mask">The bit mask.</param>
        /// <returns>Amount of enabled bits in the mask.</returns>
        private Byte BitsFromMask(UInt32 mask)
        {
            UInt32 bits = 0;
            for (Int32 bitloc = 0; bitloc < 32; bitloc++)
                bits += ((mask >> bitloc) & 1);
            return (Byte) bits;
        }

        /// <summary>
        /// Gets the data from a value according to a bit mask.
        /// </summary>
        /// <param name="mask">The bit mask.</param>
        /// <param name="inputVal">Input value</param>
        /// <returns></returns>
        private UInt32 GetValueFromMask(UInt32 mask, UInt32 inputVal)
        {
            UInt32 curVal = 0;
            Int32 outIndex = 0;
            for (Int32 bitloc = 0; bitloc < 32; bitloc++)
            {
                if (((mask >> bitloc) & 1) != 1)
                    continue;
                UInt32 bit = (inputVal >> bitloc) & 1;
                curVal = curVal | (bit << outIndex);
                outIndex++;
            }
            return curVal;
        }

        /// <summary>
        /// Adds the bits of a value to a destination value according to a mask.
        /// </summary>
        /// <param name="destValue">Value to add the current input to.</param>
        /// <param name="mask">The bit mask.</param>
        /// <param name="value">Input value</param>
        /// <returns>The destValue with the value repalced on it according to the mask.</returns>
        private UInt32 AddValueWithMask(UInt32 destValue, UInt32 mask, UInt32 value)
        {
            Int32 inIndex = 0;
            // Clear affected bits, so 1-bits already on destvalue that fall inside the mask don't change the added value.
            destValue = (destValue & (~mask));
            for (Int32 bitloc = 0; bitloc < 32; bitloc++)
            {
                if (((mask >> bitloc) & 1) != 1)
                    continue;
                UInt32 bit = (value >> inIndex) & 1;
                destValue |= (bit << bitloc);
                inIndex++;
            }
            return destValue;
        }

        private static UInt32 MakeMask(Byte bits, Byte shift)
        {
            return (UInt32) (((1 << bits) - 1) << shift);
        }

        private static UInt32 MakeMaxVal(Byte bits)
        {
            return (UInt32) ((1 << bits) - 1);
        }

        /// <summary>
        /// Using this multiplier instead of a basic int ensures a true uniform distribution of values of this bits length over the 0-255 range.
        /// </summary>
        /// <param name="colorComponentBitLength">Bits length of the color component</param>
        /// <returns>The most correct multiplier to convert colour components of the given bits length to a 0-255 range.</returns>
        public static Double MakeMultiplier(Byte colorComponentBitLength)
        {
            if (colorComponentBitLength == 0)
                return 0;
            return 255.0/((1 << colorComponentBitLength) - 1);
        }

        /// <summary>
        /// Gets a color pixel from the data, based on an offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The color at that position.</returns>
        public Color GetColor(Byte[] data, Int32 offset)
        {
            UInt32 value = (UInt32)ArrayUtils.ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
            return this.GetColorFromValue(value);
        }

        /// <summary>
        /// Reads the raw data of a pixel as ARGB array in the original internal format from the data from the given offset.
        /// The ColorComponent enum can be used to get the correct values out.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The raw bit data of the color at that position.</returns>
        public UInt32[] GetRawComponents(Byte[] data, Int32 offset)
        {
            UInt32 value = (UInt32)ArrayUtils.ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
            return this.GetRawComponentsFromValue(value);
        }

        /// <summary>
        /// Writes a color pixel in the data at the given offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset at which to write in the data.</param>
        /// <param name="color">The color to set at that position.</param>
        public void WriteColor(Byte[] data, Int32 offset, Color color)
        {
            UInt32 value = this.GetValueFromColor(color);
            ArrayUtils.WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
        }

        /// <summary>
        /// Writes raw data of a pixel as ARGB array in the original internal format from the data to the given offset.
        /// The data must match the indices given in the ColorComponent enum.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset at which to write in the data.</param>
        /// <param name="rawComponents">The raw color components to set at that position.</param>
        public void WriteRawComponents(Byte[] data, Int32 offset, UInt32[] rawComponents)
        {
            UInt32 value = this.GetValueFromRawComponents(rawComponents);
            ArrayUtils.WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
        }

        /// <summary>
        /// Gets a colour from a read UInt32 value.
        /// </summary>
        /// <param name="readValue">The read 4-byte value.</param>
        /// <returns>The color.</returns>
        public Color GetColorFromValue(UInt32 readValue)
        {
            Byte[] components = new Byte[4];
            for (Int32 i = 0; i < 4; i++)
                components[i] = this.GetChannelFromValue(readValue, (ColorComponent) i);
            return Color.FromArgb(components[(Int32) ColorComponent.Alpha],
                                  components[(Int32) ColorComponent.Red],
                                  components[(Int32) ColorComponent.Green],
                                  components[(Int32) ColorComponent.Blue]);
        }

        /// <summary>
        /// Gets the raw data of a pixel as ARGB array in the original internal format from the given value.
        /// The ColorComponent enum can be used to get the correct values out.
        /// </summary>
        /// <param name="readValue">The read 4-byte value.</param>
        /// <returns>The color.</returns>
        public UInt32[] GetRawComponentsFromValue(UInt32 readValue)
        {
            UInt32[] components = new UInt32[4];
            for (Int32 i = 0; i < 4; i++)
                components[i] = this.GetRawChannelFromValue(readValue, (ColorComponent) i);
            return components;
        }

        /// <summary>
        /// Gets a specific component from a read integer value.
        /// </summary>
        /// <param name="readValue">The read integer value.</param>
        /// <param name="type">The color component to get.</param>
        /// <returns>The read color component.</returns>
        private UInt32 GetRawChannelFromValue(UInt32 readValue, ColorComponent type)
        {
            return this.GetValueFromMask(this.limitMasks[(Int32) type], readValue);
        }

        /// <summary>
        /// Gets a specific color component from a read integer value.
        /// </summary>
        /// <param name="readValue">The read integer value.</param>
        /// <param name="type">The color component to get.</param>
        /// <returns>The read color component, adjust to /256 fraction.</returns>
        private Byte GetChannelFromValue(UInt32 readValue, ColorComponent type)
        {
            UInt32 val = this.GetRawChannelFromValue(readValue, type);
            Double valD = (val*this.multipliers[(Int32) type]);
            return (Byte) Math.Min(255, Math.Round(valD, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Gets the bare integer value of a color.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The integer value to write.</returns>
        public UInt32 GetValueFromColor(Color color)
        {
            Byte[] components = new Byte[] { color.A, color.R, color.G, color.B };
            UInt32 val = 0;
            for (Int32 i = 0; i < 4; i++)
            {
                Double tempValD = components[i]/this.multipliers[i];
                UInt32 tempVal = (UInt32) Math.Min(this.maxChan[i], Math.Round(tempValD, MidpointRounding.AwayFromZero));
                val = this.AddValueWithMask(val, this.limitMasks[i], tempVal);
            }
            return val;
        }

        /// <summary>
        /// Allows converting one raw format to a value of another raw format.
        /// </summary>
        /// <param name="components">The color components to convert. These need to already be in the correct format for this function to work.</param>
        /// <returns>The integer value to write.</returns>
        public UInt32 GetValueFromRawComponents(UInt32[] components)
        {
            UInt32[] componentsChecked = new UInt32[4];
            for (Int32 i = 0; i < 4; i++)
                componentsChecked[i] = (i < components.Length) ? components[i] : this.defaultsChan[i];
            UInt32 val = 0;
            for (Int32 i = 0; i < 4; i++)
                val = this.AddValueWithMask(val, this.limitMasks[i], componentsChecked[i]);
            return val;
        }

    }
}
