using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace Nyerguds.ImageManipulation
{
    /// <summary>
    /// Class to automate the unpacking (and packing/writing) of RGB(A) colors in color formats with packed bits.
    /// Inspired by https://github.com/scummvm/scummvm/blob/master/graphics/pixelformat.h
    /// This class works slightly differently than the ScummVM version, using 4-entry arrays for all data, with each entry
    /// representing one of the color components, so the code can easily loop over them and perform the same action on each one.
    /// </summary>
    public class PixelFormatter
    {
        /// <summary>Standard PixelFormatter for .Net's 32-bit RGBA format.</summary>
        public static PixelFormatter Format32BitArgb = new PixelFormatter(4, 0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF, true);

        /// <summary>Standard PixelFormatter for .Net's 24-bit RGB format.</summary>
        public static PixelFormatter Format24BitRgb = new PixelFormatter(3, 0, 0x00FF0000, 0x0000FF00, 0x000000FF, true);

        /// <summary>Standard PixelFormatter for .Net's 16-bit RGBA format with 1-bit transparency.</summary>
        public static PixelFormatter Format16BitArgb1555 = new PixelFormatter(2, 0x8000, 0x7C00, 0x03E0, 0x001F, true);

        /// <summary>Standard PixelFormatter for .Net's 16-bit RGB format with 5-bit components.</summary>
        public static PixelFormatter Format16BitRgb555 = new PixelFormatter(2, 0x0000, 0x7C00, 0x03E0, 0x001F, true);

        /// <summary>Standard PixelFormatter for .Net's 16-bit RGB format with 6-bit green.</summary>
        public static PixelFormatter Format16BitRgb565 = new PixelFormatter(2, 0x0000, 0xF800, 0x07E0, 0x001F, true);

        public Int32 BytesPerPixel
        {
            get { return this.bytesPerPixel; }
        }

        public Boolean LittleEndian
        {
            get { return this.littleEndian; }
        }

        /// <summary>Bit masks get the bits for each color component (A,R,G,B).</summary>
        public ReadOnlyCollection<UInt32> BitMasks
        {
            get { return new List<UInt32>(this.bitMasks).AsReadOnly(); }
        }

        /// <summary>Amount of bits for each component (A,R,G,B).</summary>
        public ReadOnlyCollection<Byte> BitsAmounts
        {
            get { return new List<Byte>(this.bitsAmounts).AsReadOnly(); }
        }

        /// <summary>Multiplier for each component (A,R,G,B).</summary>
        public ReadOnlyCollection<Double> Multipliers
        {
            get { return new List<Double>(this.multipliers).AsReadOnly(); }
        }
        /// <summary>Maximum value for each component (A,R,G,B)</summary>
        public ReadOnlyCollection<UInt32> Maximums
        {
            get { return new List<UInt32>(this.maxChan).AsReadOnly(); }
        }

        /// <summary>Internal maximum. Could be adjusted to 0xFFFF to support 16-bit colour components</summary>
        private const UInt32 InternalMax = 0xFFFF;
        private const Double MultiplierFor8BitCol = 255.0 / InternalMax;

        /// <summary>Number of bytes to read per pixel. since this only handles ARGB, less than 1 is unsupported.</summary>
        private Byte bytesPerPixel;

        /// <summary>Bit masks get the bits for each color component (A,R,G,B). If not explicitly given this can be derived from the number of bits.</summary>
        private UInt32[] bitMasks = new UInt32[4];

        /// <summary>Amount of bits for each component (A,R,G,B).</summary>
        private Byte[] bitsAmounts = new Byte[4];

        /// <summary>Multiplier for each component (A,R,G,B). If not explicitly given this can be derived from the number of bits.</summary>
        private Double[] multipliers = new Double[4];

        /// <summary>Maximum value for each component (A,R,G,B)</summary>
        private UInt32[] maxChan = new UInt32[4];

        /// <summary>Defaults for each component (A,R,G,B). This is always the maximum value for Alpha, and 0 for the rest.</summary>
        private UInt32[] defaultsChan = new UInt32[4];

        /// <summary>True to read the input bytes as little-endian.</summary>
        private Boolean littleEndian;

        // The following values are saved as bare ints rather than an enum to avoid unnecessary casts.

        /// <summary>The index used for the Alpha color components in all arrays.</summary>
        public const Int32 ColA = 0;

        /// <summary>The index used for the Red color components in all arrays.</summary>
        public const Int32 ColR = 1;

        /// <summary>The index used for the Green color components in all arrays.</summary>
        public const Int32 ColG = 2;

        /// <summary>The index used for the Blue color components in all arrays.</summary>
        public const Int32 ColB = 3;

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
        {
        }

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

            Byte alphaBits = BitsFromMask(maskAlpha);
            this.bitsAmounts[ColA] = alphaBits;
            this.multipliers[ColA] = alphaMultiplier >= 0 ? alphaMultiplier : MakeMultiplier(alphaBits);
            this.bitMasks[ColA] = maskAlpha;
            UInt32 maxValAlpha = MakeMaxVal(alphaBits);
            this.maxChan[ColA] = maxValAlpha;
            this.defaultsChan[ColA] = InternalMax;

            Byte redBits = BitsFromMask(maskRed);
            this.bitsAmounts[ColR] = redBits;
            this.multipliers[ColR] = redMultiplier >= 0 ? redMultiplier : MakeMultiplier(redBits);
            this.bitMasks[ColR] = maskRed;
            this.maxChan[ColR] = MakeMaxVal(redBits);
            this.defaultsChan[ColR] = 0;

            Byte greenBits = BitsFromMask(maskGreen);
            this.bitsAmounts[ColG] = greenBits;
            this.multipliers[ColG] = greenMultiplier >= 0 ? greenMultiplier : MakeMultiplier(greenBits);
            this.bitMasks[ColG] = maskGreen;
            this.maxChan[ColG] = MakeMaxVal(greenBits);
            this.defaultsChan[ColG] = 0;

            Byte blueBits = BitsFromMask(maskBlue);
            this.bitsAmounts[ColB] = blueBits;
            this.multipliers[ColB] = blueMultiplier >= 0 ? blueMultiplier : MakeMultiplier(blueBits);
            this.bitMasks[ColB] = maskBlue;
            this.maxChan[ColB] = MakeMaxVal(blueBits);
            this.defaultsChan[ColB] = 0;
        }

        /// <summary>
        /// Creats a new PixelFormatter, with automatic calculation of color multipliers using the CalculateMultiplier function.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel.</param>
        /// <param name="alphaBits">Amount of bits to read for the alpha color component.</param>
        /// <param name="alphaShift">Amount of bits to shift the data to get to the alpha color component.</param>
        /// <param name="redBits">Amount of bits to read for the red color component.</param>
        /// <param name="redShift">Amount of bits to shift the data to get to the red color component.</param>
        /// <param name="greenBits">Amount of bits to read for the green color component.</param>
        /// <param name="greenShift">Amount of bits to shift the data to get to the green color component.</param>
        /// <param name="blueBits">Amount of bits to read for the blue color component.</param>
        /// <param name="blueShift">Amount of bits to shift the data to get to the blue color component.</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(Byte bytesPerPixel,
            Byte alphaBits, Byte alphaShift,
            Byte redBits, Byte redShift,
            Byte greenBits, Byte greenShift,
            Byte blueBits, Byte blueShift,
            Boolean littleEndian)
            : this(bytesPerPixel, alphaBits, alphaShift, -1, redBits, redShift, -1, greenBits, greenShift, -1,
                blueBits, blueShift, -1, littleEndian)
        {
        }

        /// <summary>
        /// Creates a new PixelFormatter.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel.</param>
        /// <param name="alphaBits">Amount of bits to read for the alpha color component.</param>
        /// <param name="alphaShift">Amount of bits to shift the data to get to the alpha color component.</param>
        /// <param name="alphaMultiplier">Multiplier for the alpha component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="redBits">Amount of bits to read for the red color component.</param>
        /// <param name="redShift">Amount of bits to shift the data to get to the red color component.</param>
        /// <param name="redMultiplier">Multiplier for the red component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="greenBits">Amount of bits to read for the green color component.</param>
        /// <param name="greenShift">Amount of bits to shift the data to get to the green color component.</param>
        /// <param name="greenMultiplier">Multiplier for the green component's value to adjust it to the normal 0-255 range.</param>
        /// <param name="blueBits">Amount of bits to read for the blue color component.</param>
        /// <param name="blueShift">Amount of bits to shift the data to get to the blue color component.</param>
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
            this.bitsAmounts[ColA] = alphaBits;
            this.multipliers[ColA] = alphaMultiplier >= 0 ? alphaMultiplier : MakeMultiplier(alphaBits);
            this.bitMasks[ColA] = MakeMask(alphaBits, alphaShift);
            UInt32 maxValAlpha = MakeMaxVal(alphaBits);
            this.maxChan[ColA] = maxValAlpha;
            this.defaultsChan[ColA] = InternalMax;

            this.bitsAmounts[ColR] = redBits;
            this.multipliers[ColR] = redMultiplier >= 0 ? redMultiplier : MakeMultiplier(redBits);
            this.bitMasks[ColR] = MakeMask(redBits, redShift);
            this.maxChan[ColR] = MakeMaxVal(redBits);
            this.defaultsChan[ColR] = 0;

            this.bitsAmounts[ColG] = greenBits;
            this.multipliers[ColG] = greenMultiplier >= 0 ? greenMultiplier : MakeMultiplier(greenBits);
            this.bitMasks[ColG] = MakeMask(greenBits, greenShift);
            this.maxChan[ColG] = MakeMaxVal(greenBits);
            this.defaultsChan[ColG] = 0;

            this.bitsAmounts[ColB] = blueBits;
            this.multipliers[ColB] = blueMultiplier >= 0 ? blueMultiplier : MakeMultiplier(blueBits);
            this.bitMasks[ColB] = MakeMask(blueBits, blueShift);
            this.maxChan[ColB] = MakeMaxVal(blueBits);
            this.defaultsChan[ColB] = 0;
        }

        /// <summary>
        /// Counts the amount of bits in a mask.
        /// </summary>
        /// <param name="mask">The bit mask.</param>
        /// <returns>Amount of enabled bits in the mask.</returns>
        private static Byte BitsFromMask(UInt32 mask)
        {
            UInt32 bits = 0;
            for (Int32 bitloc = 0; bitloc < 32; ++bitloc)
                bits += ((mask >> bitloc) & 1);
            return (Byte) bits;
        }

        /// <summary>
        /// Gets the data from a value according to a bit mask. Collates all bits as they are in the mask, effectively giving a value where all non-masked bits are "removed".
        /// </summary>
        /// <param name="mask">The bit mask.</param>
        /// <param name="inputVal">Input value.</param>
        /// <returns>The value from the mask.</returns>
        private static UInt32 GetValueFromMask(UInt32 mask, UInt32 inputVal)
        {
            UInt32 curVal = 0;
            Int32 outIndex = 0;
            for (Int32 bitloc = 0; bitloc < 32; ++bitloc)
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
        /// <param name="value">Input value.</param>
        /// <returns>The destValue with the value repalced on it according to the mask.</returns>
        private static UInt32 AddValueWithMask(UInt32 destValue, UInt32 mask, UInt32 value)
        {
            Int32 inIndex = 0;
            // Clear affected bits, so 1-bits already on destvalue that fall inside the mask don't change the added value.
            destValue = (destValue & (~mask));
            for (Int32 bitloc = 0; bitloc < 32; ++bitloc)
            {
                if (((mask >> bitloc) & 1) != 1)
                    continue;
                UInt32 bit = (value >> inIndex) & 1;
                destValue |= (bit << bitloc);
                inIndex++;
            }
            return destValue;
        }

        private static UInt32 MakeMask(Byte colorComponentBitLength, Byte shift)
        {
            return (UInt32) (((1 << colorComponentBitLength) - 1) << shift);
        }

        private static UInt32 MakeMaxVal(Byte colorComponentBitLength)
        {
            return (UInt32) ((1 << colorComponentBitLength) - 1);
        }

        /// <summary>
        /// Using this multiplier instead of a basic int ensures a true uniform distribution of values of this bits length over the 0-255 range.
        /// </summary>
        /// <param name="colorComponentBitLength">Bits length of the color component.</param>
        /// <returns>The most correct multiplier to convert color components of the given bits length to a 0-255 range.</returns>
        public static Double MakeMultiplier(Byte colorComponentBitLength)
        {
            if (colorComponentBitLength == 0)
                return 0;
            return ((Double)InternalMax) / ((1 << colorComponentBitLength) - 1);
        }

        /// <summary>
        /// Gets a color pixel from the data, based on an offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The color at that position.</returns>
        public Color GetColor(Byte[] data, Int32 offset)
        {
            UInt32 value = (UInt32) ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
            return this.GetColorFromValue(value);
        }

        /// <summary>
        /// Reads a color palette from the data, starting at the given offset and increasing by the set color byte length.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <param name="colors">Amount of colors in the palette.</param>
        /// <returns>The color at that position.</returns>
        public Color[] GetColorPalette(Byte[] data, Int32 offset, Int32 colors)
        {
            Color[] palette = new Color[colors];
            Int32 step = this.bytesPerPixel;
            Int32 end = offset + step * colors;
            if (data.Length < end)
                throw new IndexOutOfRangeException("Palette is too long to be read from the given array!");
            Int32 palIndex = 0;
            for (Int32 offs = offset; offs < end; offs += step)
                palette[palIndex++] = this.GetColor(data, offs);
            return palette;
        }

        /// <summary>
        /// Reads the raw data of a pixel as ARGB array in the original internal format from the data from the given offset.
        /// Each component is the actual colour value, stored in the amount of bits specified in the read mask.
        /// The ColorComponent enum can be used to get the correct values out.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The raw bit data of the color at that position.</returns>
        public UInt32[] GetRawComponents(Byte[] data, Int32 offset)
        {
            UInt32 value = (UInt32) ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
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
            WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
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
            WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
        }

        /// <summary>
        /// Gets a color from a read UInt32 value.
        /// </summary>
        /// <param name="readValue">The read 4-byte value.</param>
        /// <returns>The color.</returns>
        public Color GetColorFromValue(UInt32 readValue)
        {
            Byte[] components = new Byte[4];
            for (Int32 i = 0; i < 4; ++i)
                components[i] = (Byte)Math.Min(255, (Int32)Math.Round(this.GetChannelFromValue(readValue, i) * MultiplierFor8BitCol, MidpointRounding.AwayFromZero));
            return Color.FromArgb(components[ColA], components[ColR], components[ColG], components[ColB]);
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
            for (Int32 i = 0; i < 4; ++i)
                components[i] = this.GetRawChannelFromValue(readValue, i);
            return components;
        }

        /// <summary>
        /// Gets the raw value of a specific component from the given integer value, without adjustment to 0-255 range.
        /// </summary>
        /// <param name="readValue">The read integer value.</param>
        /// <param name="component">The color component to get.</param>
        /// <returns>The read color component.</returns>
        private UInt32 GetRawChannelFromValue(UInt32 readValue, Int32 component)
        {
            return GetValueFromMask(this.bitMasks[component], readValue);
        }

        /// <summary>
        /// Gets a specific color component from a read integer value. The returned value is adjusted to 0-255 range.
        /// </summary>
        /// <param name="readValue">The read integer value.</param>
        /// <param name="component">The color component to get.</param>
        /// <returns>The read color component, adjust to /256 fraction.</returns>
        private UInt32 GetChannelFromValue(UInt32 readValue, Int32 component)
        {
            if (this.bitsAmounts[component] == 0)
                return this.defaultsChan[component];
            UInt32 val = this.GetRawChannelFromValue(readValue, component);
            Double valD = (val * this.multipliers[component]);
            return Math.Min(InternalMax, (UInt32)Math.Round(valD, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Gets the bare integer value of a color.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The integer value to write.</returns>
        public UInt32 GetValueFromColor(Color color)
        {
            Byte[] components = new Byte[] {color.A, color.R, color.G, color.B};
            UInt32 val = 0;
            for (Int32 i = 0; i < 4; ++i)
            {
                Double tempValD = components[i] / this.multipliers[i];
                UInt32 tempVal = Math.Min(this.maxChan[i], (UInt32)Math.Round(tempValD, MidpointRounding.AwayFromZero));
                val = AddValueWithMask(val, this.bitMasks[i], tempVal);
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
            for (Int32 i = 0; i < 4; ++i)
                componentsChecked[i] = (i < components.Length) ? components[i] : this.defaultsChan[i];
            UInt32 val = 0;
            for (Int32 i = 0; i < 4; ++i)
                val = AddValueWithMask(val, this.bitMasks[i], componentsChecked[i]);
            return val;
        }

        #region ArrayUtils import

        private static UInt64 ReadIntFromByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            UInt64 value = 0;
            for (Int32 index = 0; index < bytes; ++index)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                value += (UInt64)(data[offs] << (8 * index));
            }
            return value;
        }

        private static void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt64 value)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; ++index)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte)(value >> (8 * index) & 0xFF);
            }
        }

        #endregion

        #region toolsets

        /// <summary>
        /// Reorders the bits inside a byte array to a new pixel format of equal length. Both formats are specified by a PixelFormatter object.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="stride">Image data stride.</param>
        /// <param name="inputFormat">Input pixel formatter.</param>
        /// <param name="outputFormat">Output pixel formatter.</param>
        public static void ReorderBits(Byte[] imageData, Int32 width, Int32 height, Int32 stride, PixelFormatter inputFormat, PixelFormatter outputFormat)
        {
            if (inputFormat.BytesPerPixel != outputFormat.BytesPerPixel)
                throw new ArgumentException("Output format's bytes per pixel do not match input format!", "outputFormat");
            if (inputFormat.BitMasks.SequenceEqual(outputFormat.BitMasks))
                return; // Nothing to fix; they're the same already.
            Int32 step = outputFormat.BytesPerPixel;
            Int32 lineOffset = 0;
            if (inputFormat.BitsAmounts.SequenceEqual(outputFormat.BitsAmounts))
            {
                // Actually has same bit amounts : simply reorder the raw data.
                for (Int32 y = 0; y < height; ++y)
                {
                    Int32 offset = lineOffset;
                    for (Int32 x = 0; x < width; ++x)
                    {
                        UInt32[] argbValues = inputFormat.GetRawComponents(imageData, offset);
                        outputFormat.WriteRawComponents(imageData, offset, argbValues);
                        offset += step;
                    }
                    lineOffset += stride;
                }
                return;
            }
            ReadOnlyCollection<Double> mulIn = inputFormat.Multipliers;
            ReadOnlyCollection<Double> mulOut = outputFormat.Multipliers;
            ReadOnlyCollection<Byte> bitsOut = outputFormat.BitsAmounts;
            UInt32[] maxOut = outputFormat.Maximums.ToArray();
            // Get converter multiplier.
            Boolean[] isZeroOut = new Boolean[4];
            Double[] multiplier = new Double[4];
            for (Int32 i = 0; i < 4; ++i)
            {
                Boolean outChanIsZero = bitsOut[i] == 0;
                isZeroOut[i] = outChanIsZero;
                multiplier[i] = outChanIsZero ? 0 : mulIn[i] / mulOut[i];
            }
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                for (Int32 x = 0; x < width; ++x)
                {
                    UInt32[] argbValues = inputFormat.GetRawComponents(imageData, offset);
                    for (Int32 i = 0; i < 4; ++i)
                        argbValues[i] = isZeroOut[i] ? 0 : Math.Min((UInt32)Math.Round(argbValues[i] * multiplier[i], MidpointRounding.AwayFromZero), maxOut[i]);
                    outputFormat.WriteRawComponents(imageData, offset, argbValues);
                    offset += step;
                }
                lineOffset += stride;
            }
        }

        /// <summary>
        /// Converts the bits inside a byte array to a new pixel format. Both formats are specified by a PixelFormatter object.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="stride">Image data stride. Is adjusted to the output's stride.</param>
        /// <param name="inputFormat">Input pixel formatter.</param>
        /// <param name="outputFormat">Output pixel formatter.</param>
        public static Byte[] ConvertBits(Byte[] imageData, Int32 width, Int32 height, ref Int32 stride, PixelFormatter inputFormat, PixelFormatter outputFormat)
        {
            Int32 stepIn = inputFormat.BytesPerPixel;
            Int32 stepOut = outputFormat.BytesPerPixel;
            Int32 newStride = stepOut * width;
            Int32 newSize = newStride * height;
            Byte[] newData = new Byte[newSize];

            // Converter multiplier. Example:
            // in:  3 bits => 111    => max  7 => multfactor = 255 /  7 = 36,428571
            // out: 6 bits => 111111 => max 63 => multfactor = 255 / 63 =  4,047619
            // Conversion multiplication factor: (36,428571/4,047619) = 9
            // 7 * 9 = 63 => successful conversion from 'in' to 'out' format.

            ReadOnlyCollection<Double> mulIn = inputFormat.Multipliers;
            ReadOnlyCollection<Byte> bitsOut = outputFormat.BitsAmounts;
            ReadOnlyCollection<Double> mulOut = outputFormat.Multipliers;
            ReadOnlyCollection<UInt32> maxOut = outputFormat.Maximums;
            // Get converter multiplier.
            Boolean[] isZeroOut = new Boolean[4];
            Double[] multiplier = new Double[4];
            for (Int32 i = 0; i < 4; ++i)
            {
                Boolean outChanIsZero = bitsOut[i] == 0;
                isZeroOut[i] = outChanIsZero;
                multiplier[i] = outChanIsZero ? 0 : mulIn[i] / mulOut[i];
            }
            Int32 lineOffsetIn = 0;
            Int32 lineOffsetOut = 0;
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offsetIn = lineOffsetIn;
                Int32 offsetOut = lineOffsetOut;
                for (Int32 x = 0; x < width; ++x)
                {
                    UInt32[] argbValues = inputFormat.GetRawComponents(imageData, offsetIn);
                    for (Int32 i = 0; i < 4; ++i)
                        argbValues[i] = isZeroOut[i] ? 0 : Math.Min((UInt32)Math.Round(argbValues[i] * multiplier[i], MidpointRounding.AwayFromZero), maxOut[i]);
                    outputFormat.WriteRawComponents(newData, offsetOut, argbValues);
                    offsetIn += stepIn;
                    offsetOut += stepOut;
                }
                lineOffsetIn += stride;
                lineOffsetOut += newStride;
            }
            stride = newStride;
            return newData;
        }

        #endregion

    }
}
