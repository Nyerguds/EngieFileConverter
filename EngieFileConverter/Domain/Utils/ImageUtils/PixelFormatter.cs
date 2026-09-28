using System;
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
    /// <remarks>Written by Nyerguds.</remarks>
    public class PixelFormatter
    {
        #region Predefined pixelformatters for common use
        /// <summary>Standard PixelFormatter for .Net's little-endian 32-bit RGBA format.</summary>
        public static PixelFormatter Format32BitArgbLe = new PixelFormatter(4, 0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF, true);
        /// <summary>Standard PixelFormatter for .Net's little-endian 24-bit RGB format.</summary>
        public static PixelFormatter Format24BitRgbLe = new PixelFormatter(3, 0x00000000, 0x00FF0000, 0x0000FF00, 0x000000FF, true);
        /// <summary>Standard PixelFormatter for .Net's little-endian 16-bit RGBA format with 1-bit transparency.</summary>
        public static PixelFormatter Format16BitArgb1555Le = new PixelFormatter(2, 0x8000, 0x7C00, 0x03E0, 0x001F, true);
        /// <summary>Standard PixelFormatter for .Net's little-endian 16-bit RGB format with 5-bit components.</summary>
        public static PixelFormatter Format16BitRgb555Le = new PixelFormatter(2, 0x0000, 0x7C00, 0x03E0, 0x001F, true);
        /// <summary>Standard PixelFormatter for .Net's little-endian 16-bit RGB format with 6-bit green.</summary>
        public static PixelFormatter Format16BitRgb565Le = new PixelFormatter(2, 0x0000, 0xF800, 0x07E0, 0x001F, true);
        /// <summary>PixelFormatter for classic 6-bit VGA color palette entries.</summary>
        public static PixelFormatter Format6BitVgaPal = new PixelFormatter(3, 0x00000000, 0x003F0000, 0x00003F00, 0x0000003F, false);
        /// <summary>PixelFormatter for classic 8-bit VGA color palette entries.</summary>
        public static PixelFormatter Format8BitVgaPal = new PixelFormatter(3, 0x00000000, 0x00FF0000, 0x0000FF00, 0x000000FF, false);
        #endregion

        #region public properties. All read-only; the only data input is in the constructor.
        public int BytesPerPixel
        {
            get { return this.bytesPerPixel; }
        }

        public bool LittleEndian
        {
            get { return this.littleEndian; }
        }

        /// <summary>Bit masks to get the bits for each color component (A,R,G,B).</summary>
        public ReadOnlyCollection<uint> BitMasks
        {
            get { return Array.AsReadOnly(this.bitMasks); }
        }

        /// <summary>Amount of bits for each component (A,R,G,B).</summary>
        public ReadOnlyCollection<byte> BitsAmounts
        {
            get { return Array.AsReadOnly(this.bitsAmounts); }
        }

        /// <summary>Multiplier for each component (A,R,G,B).</summary>
        public ReadOnlyCollection<double> Multipliers
        {
            get { return Array.AsReadOnly(this.multipliers); }
        }

        /// <summary>Maximum value for each component (A,R,G,B)</summary>
        public ReadOnlyCollection<uint> Maximums
        {
            get { return Array.AsReadOnly(this.maxChan); }
        }
        #endregion

        #region private variables
        /// <summary>Internal maximum bits per color component. Currently set to support 8-bit color components. 16-bit could work, but seems problematic to combine with masks.</summary>
        private const int InternalMaxBits = 8;
        /// <summary>Internal maximum value per color component.</summary>
        private const uint InternalMax = (1 << InternalMaxBits) - 1;
        /// <summary>Internal maximum bits for the full processed color value.</summary>
        private const int InternalMaxSize = InternalMaxBits * 4;
        private const double MultiplierFor8BitCol = 255.0 / InternalMax;
        /// <summary>Number of bytes to read per pixel. Since this only handles ARGB, less than 1 is unsupported.</summary>
        private readonly byte bytesPerPixel;
        /// <summary>Bit masks get the bits for each color component (A,R,G,B). If not explicitly given this can be derived from the number of bits.</summary>
        private readonly uint[] bitMasks = new uint[4];
        /// <summary>Amount of bits for each component (A,R,G,B).</summary>
        private readonly byte[] bitsAmounts = new byte[4];
        /// <summary>Multiplier for each component (A,R,G,B). If not explicitly given this can be derived from the number of bits.</summary>
        private readonly double[] multipliers = new double[4];
        /// <summary>Maximum value for each component (A,R,G,B).</summary>
        private readonly uint[] maxChan = new uint[4];
        /// <summary>Defaults for each component (A,R,G,B). This is always the maximum value for Alpha, and 0 for the rest.</summary>
        private readonly uint[] defaultsChan = new uint[4];
        /// <summary>True to read the input bytes as little-endian.</summary>
        private readonly bool littleEndian;
        #endregion

        #region Indices. These are saved as bare ints rather than an enum to avoid unnecessary casts.
        /// <summary>The index used for the Alpha color components in all arrays.</summary>
        public const int ColA = 0;
        /// <summary>The index used for the Red color components in all arrays.</summary>
        public const int ColR = 1;
        /// <summary>The index used for the Green color components in all arrays.</summary>
        public const int ColG = 2;
        /// <summary>The index used for the Blue color components in all arrays.</summary>
        public const int ColB = 3;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new PixelFormatter based on bit masks.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel.</param>
        /// <param name="maskAlpha">Bit mask for alpha component.</param>
        /// <param name="maskRed">Bit mask for red component.</param>
        /// <param name="maskGreen">Bit mask for green component.</param>
        /// <param name="maskBlue">Bit mask for blue component.</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(byte bytesPerPixel, uint maskAlpha, uint maskRed, uint maskGreen, uint maskBlue, bool littleEndian)
            : this(bytesPerPixel, maskAlpha, -1, maskRed, -1, maskGreen, -1, maskBlue, -1, littleEndian)
        {
        }

        /// <summary>
        /// Creates a new PixelFormatter based on bit masks.
        /// </summary>
        /// <param name="bytesPerPixel">Amount of bytes to read per pixel.</param>
        /// <param name="maskAlpha">Bit mask for alpha component.</param>
        /// <param name="alphaMultiplier">Multiplier for alpha component's value to adjust it to the normal 0-255 range. Use -1 to generate it from the mask.</param>
        /// <param name="maskRed">Bit mask for red component.</param>
        /// <param name="redMultiplier">Multiplier for red component's value to adjust it to the normal 0-255 range. Use -1 to generate it from the mask.</param>
        /// <param name="maskGreen">Bit mask for green component.</param>
        /// <param name="greenMultiplier">Multiplier for green component's value to adjust it to the normal 0-255 range. Use -1 to generate it from the mask.</param>
        /// <param name="maskBlue">Bit mask for blue component.</param>
        /// <param name="blueMultiplier">Multiplier for blue component's value to adjust it to the normal 0-255 range. Use -1 to generate it from the mask.</param>
        /// <param name="littleEndian">True if the read bytes are interpreted as little-endian.</param>
        public PixelFormatter(byte bytesPerPixel,
            uint maskAlpha, double alphaMultiplier,
            uint maskRed, double redMultiplier,
            uint maskGreen, double greenMultiplier,
            uint maskBlue, double blueMultiplier,
            bool littleEndian)
        {
            this.bytesPerPixel = bytesPerPixel;
            this.littleEndian = littleEndian;

            byte alphaBits = BitsFromMask(maskAlpha);
            this.bitsAmounts[ColA] = alphaBits;
            this.multipliers[ColA] = alphaMultiplier >= 0 ? alphaMultiplier : MakeMultiplier(alphaBits);
            this.bitMasks[ColA] = maskAlpha;
            uint maxValAlpha = MakeMaxVal(alphaBits);
            this.maxChan[ColA] = maxValAlpha;
            this.defaultsChan[ColA] = InternalMax;

            byte redBits = BitsFromMask(maskRed);
            this.bitsAmounts[ColR] = redBits;
            this.multipliers[ColR] = redMultiplier >= 0 ? redMultiplier : MakeMultiplier(redBits);
            this.bitMasks[ColR] = maskRed;
            this.maxChan[ColR] = MakeMaxVal(redBits);
            this.defaultsChan[ColR] = 0;

            byte greenBits = BitsFromMask(maskGreen);
            this.bitsAmounts[ColG] = greenBits;
            this.multipliers[ColG] = greenMultiplier >= 0 ? greenMultiplier : MakeMultiplier(greenBits);
            this.bitMasks[ColG] = maskGreen;
            this.maxChan[ColG] = MakeMaxVal(greenBits);
            this.defaultsChan[ColG] = 0;

            byte blueBits = BitsFromMask(maskBlue);
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
        public PixelFormatter(byte bytesPerPixel,
            byte alphaBits, byte alphaShift,
            byte redBits, byte redShift,
            byte greenBits, byte greenShift,
            byte blueBits, byte blueShift,
            bool littleEndian)
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
        public PixelFormatter(byte bytesPerPixel,
            byte alphaBits, byte alphaShift, double alphaMultiplier,
            byte redBits, byte redShift, double redMultiplier,
            byte greenBits, byte greenShift, double greenMultiplier,
            byte blueBits, byte blueShift, double blueMultiplier,
            bool littleEndian)
        {
            this.bytesPerPixel = bytesPerPixel;
            this.littleEndian = littleEndian;
            this.bitsAmounts[ColA] = alphaBits;
            this.multipliers[ColA] = alphaMultiplier >= 0 ? alphaMultiplier : MakeMultiplier(alphaBits);
            this.bitMasks[ColA] = MakeMask(alphaBits, alphaShift);
            uint maxValAlpha = MakeMaxVal(alphaBits);
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
        #endregion

        #region Private functions
        /// <summary>
        /// Counts the amount of bits in a mask.
        /// </summary>
        /// <param name="mask">The bit mask.</param>
        /// <returns>Amount of enabled bits in the mask.</returns>
        private static byte BitsFromMask(uint mask)
        {
            uint bits = 0;
            for (int bitloc = 0; bitloc < InternalMaxSize; ++bitloc)
                bits += ((mask >> bitloc) & 1);
            return (byte) bits;
        }

        /// <summary>
        /// Gets the data from a value according to a bit mask. Collates all bits as they are in the mask, effectively giving a value where all non-masked bits are "removed".
        /// </summary>
        /// <param name="mask">The bit mask.</param>
        /// <param name="inputVal">Input value.</param>
        /// <returns>The value from the mask.</returns>
        private static uint GetValueFromMask(uint mask, uint inputVal)
        {
            uint curVal = 0;
            int outIndex = 0;
            for (int bitloc = 0; bitloc < InternalMaxSize; ++bitloc)
            {
                if (((mask >> bitloc) & 1) != 1)
                    continue;
                uint bit = (inputVal >> bitloc) & 1;
                curVal = curVal | (bit << outIndex);
                outIndex++;
            }
            return curVal;
        }

        /// <summary>
        /// Gets the raw value of a specific component from the given integer value, without adjustment to 0-255 range.
        /// </summary>
        /// <param name="readValue">The read integer value.</param>
        /// <param name="component">The color component to get.</param>
        /// <returns>The read color component.</returns>
        private uint GetRawChannelFromValue(uint readValue, int component)
        {
            return GetValueFromMask(this.bitMasks[component], readValue);
        }

        /// <summary>
        /// Gets a specific color component from a read integer value. The returned value is adjusted to 0-255 range.
        /// </summary>
        /// <param name="readValue">The read integer value.</param>
        /// <param name="component">The color component to get.</param>
        /// <returns>The read color component, adjust to /256 fraction.</returns>
        private uint GetChannelFromValue(uint readValue, int component)
        {
            if (this.bitsAmounts[component] == 0)
                return this.defaultsChan[component];
            uint val = this.GetRawChannelFromValue(readValue, component);
            double valD = (val * this.multipliers[component]);
            return Math.Min(InternalMax, (uint)Math.Round(valD, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Adds the bits of a value to a destination value according to a mask.
        /// </summary>
        /// <param name="destValue">Value to add the current input to.</param>
        /// <param name="mask">The bit mask.</param>
        /// <param name="value">Input value.</param>
        /// <returns>The destValue with the value repalced on it according to the mask.</returns>
        private static uint AddValueWithMask(uint destValue, uint mask, uint value)
        {
            int inIndex = 0;
            // Clear affected bits, so 1-bits already on destvalue that fall inside the mask don't change the added value.
            destValue = (destValue & (~mask));
            for (int bitloc = 0; bitloc < InternalMaxSize; ++bitloc)
            {
                if (((mask >> bitloc) & 1) != 1)
                    continue;
                uint bit = (value >> inIndex) & 1;
                destValue |= (bit << bitloc);
                inIndex++;
            }
            return destValue;
        }

        private static uint MakeMask(byte colorComponentBitLength, byte shift)
        {
            return (uint) (((1 << colorComponentBitLength) - 1) << shift);
        }

        private static uint MakeMaxVal(byte colorComponentBitLength)
        {
            return (uint) ((1 << colorComponentBitLength) - 1);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Using this multiplier instead of a basic int ensures a true uniform distribution of values of this bits length over the 0-255 range.
        /// </summary>
        /// <param name="colorComponentBitLength">Bits length of the color component.</param>
        /// <returns>The most correct multiplier to convert color components of the given bits length to a 0-255 range.</returns>
        public static double MakeMultiplier(byte colorComponentBitLength)
        {
            if (colorComponentBitLength == 0)
                return 0;
            return ((double)InternalMax) / ((1 << colorComponentBitLength) - 1);
        }

        /// <summary>
        /// Gets a set of color components from the data, based on an offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The color at that position.</returns>
        public byte[] GetColorComponents(byte[] data, int offset)
        {
            uint value = (uint)ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
            return this.GetColorComponentsFromValue(value);
        }

        /// <summary>
        /// Gets a color pixel from the data, based on an offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The color at that position.</returns>
        public Color GetColor(byte[] data, int offset)
        {
            uint value = (uint) ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
            return this.GetColorFromValue(value);
        }

        /// <summary>
        /// Reads an array of colors from the data, starting at the given offset and increasing by the set color byte length.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset from which to start reading in the data.</param>
        /// <param name="amount">Amount of colors to read.</param>
        /// <returns>The colors at that position.</returns>
        public Color[] GetColorRange(byte[] data, int offset, int amount)
        {
            Color[] range = new Color[amount];
            int step = this.bytesPerPixel;
            int end = offset + step * amount;
            if (data.Length < end)
                throw new IndexOutOfRangeException("Requested range is too long to be read from the given array.");
            int palIndex = 0;
            for (int offs = offset; offs < end; offs += step)
                range[palIndex++] = this.GetColor(data, offs);
            return range;
        }

        /// <summary>
        /// Reads the raw data of a pixel as ARGB array in the original internal format from the data from the given offset.
        /// Each component is the actual color value, stored in the amount of bits specified in the read mask.
        /// The ColorComponent enum can be used to get the correct values out.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset to read in the data.</param>
        /// <returns>The raw bit data of the color at that position.</returns>
        public uint[] GetRawComponents(byte[] data, int offset)
        {
            uint value = (uint) ReadIntFromByteArray(data, offset, this.bytesPerPixel, this.littleEndian);
            return this.GetRawComponentsFromValue(value);
        }

        /// <summary>
        /// Writes a color pixel in the data at the given offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset at which to write in the data.</param>
        /// <param name="components">Array of the color values to set at that position, as [A, R, G, B].</param>
        public void WriteColorComponents(byte[] data, int offset, byte[] components)
        {
            uint value = this.GetValueFromColorComponents(components);
            WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
        }

        /// <summary>
        /// Writes a color pixel in the data at the given offset.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset at which to write in the data.</param>
        /// <param name="color">The color to set at that position.</param>
        public void WriteColor(byte[] data, int offset, Color color)
        {
            uint value = this.GetValueFromColor(color);
            WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
        }

        /// <summary>
        /// Writes raw data of a pixel as ARGB array in the original internal format from the data to the given offset.
        /// The data must match the indices given in the ColorComponent enum.
        /// </summary>
        /// <param name="data">Image data as byte array.</param>
        /// <param name="offset">Offset at which to write in the data.</param>
        /// <param name="rawComponents">The raw color components to set at that position.</param>
        public void WriteRawComponents(byte[] data, int offset, uint[] rawComponents)
        {
            uint value = this.GetValueFromRawComponents(rawComponents);
            WriteIntToByteArray(data, offset, this.bytesPerPixel, this.littleEndian, value);
        }

        /// <summary>
        /// Gets a color from a read UInt32 value.
        /// </summary>
        /// <param name="readValue">The read 4-byte value.</param>
        /// <returns>The color.</returns>
        public Color GetColorFromValue(uint readValue)
        {
            byte[] components = GetColorComponentsFromValue(readValue);
            return Color.FromArgb(components[ColA], components[ColR], components[ColG], components[ColB]);
        }

        /// <summary>
        /// Gets a color from a read UInt32 value.
        /// </summary>
        /// <param name="readValue">The read 4-byte value.</param>
        /// <returns>The color.</returns>
        public byte[] GetColorComponentsFromValue(uint readValue)
        {
            byte[] components = new byte[4];
            for (int i = 0; i < 4; ++i)
                components[i] = (byte)Math.Min(255, (int)Math.Round(this.GetChannelFromValue(readValue, i) * MultiplierFor8BitCol, MidpointRounding.AwayFromZero));
            return components;
        }

        /// <summary>
        /// Gets the raw data of a pixel as ARGB array in the original internal format from the given value.
        /// The ColorComponent enum can be used to get the correct values out.
        /// </summary>
        /// <param name="readValue">The read 4-byte value.</param>
        /// <returns>The color.</returns>
        public uint[] GetRawComponentsFromValue(uint readValue)
        {
            uint[] components = new uint[4];
            for (int i = 0; i < 4; ++i)
                components[i] = this.GetRawChannelFromValue(readValue, i);
            return components;
        }

        /// <summary>
        /// Gets the bare integer value of a color.
        /// </summary>
        /// <param name="components">Array of the color values to convert, as [A, R, G, B].</param>
        /// <returns>The integer value to write.</returns>
        public uint GetValueFromColorComponents(byte[] components)
        {
            uint val = 0;
            int len = Math.Min(components.Length, 4);
            for (int i = 0; i < len; ++i)
            {
                double tempValD = components[i] / this.multipliers[i];
                uint tempVal = Math.Min(this.maxChan[i], (uint)Math.Round(tempValD, MidpointRounding.AwayFromZero));
                val = AddValueWithMask(val, this.bitMasks[i], tempVal);
            }
            return val;
        }

        /// <summary>
        /// Gets the bare integer value of a color.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The integer value to write.</returns>
        public uint GetValueFromColor(Color color)
        {
            byte[] components = new byte[] {color.A, color.R, color.G, color.B};
            return GetValueFromColorComponents(components);
        }

        /// <summary>
        /// Allows converting one raw format to a value of another raw format.
        /// </summary>
        /// <param name="components">The color components to convert. These need to already be in the correct format for this function to work.</param>
        /// <returns>The integer value to write.</returns>
        public uint GetValueFromRawComponents(uint[] components)
        {
            uint[] componentsChecked = new uint[4];
            for (int i = 0; i < 4; ++i)
                componentsChecked[i] = (i < components.Length) ? components[i] : this.defaultsChan[i];
            uint val = 0;
            for (int i = 0; i < 4; ++i)
                val = AddValueWithMask(val, this.bitMasks[i], componentsChecked[i]);
            return val;
        }
        #endregion

        #region Array utils. Copied from Nyerguds.Util.ArrayUtils class to avoid unnecessary dependencies.
        private static uint ReadIntFromByteArray(byte[] data, int startIndex, int bytes, bool littleEndian)
        {
            int lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            uint value = 0;
            for (int index = 0; index < bytes; ++index)
            {
                int offs = startIndex + (littleEndian ? index : lastByte - index);
                value += (uint)(data[offs] << (8 * index));
            }
            return value;
        }

        private static void WriteIntToByteArray(byte[] data, int startIndex, int bytes, bool littleEndian, uint value)
        {
            int lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (int index = 0; index < bytes; ++index)
            {
                int offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (byte)(value >> (8 * index) & 0xFF);
            }
        }
        #endregion

        #region Static tools
        /// <summary>
        /// Reorders the bits inside a byte array to a new pixel format of equal length. Both formats are specified by a PixelFormatter object.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="stride">Image data stride.</param>
        /// <param name="inputFormat">Input pixel formatter.</param>
        /// <param name="outputFormat">Output pixel formatter.</param>
        public static void ReorderBits(byte[] imageData, int width, int height, int stride, PixelFormatter inputFormat, PixelFormatter outputFormat)
        {
            if (inputFormat.BytesPerPixel != outputFormat.BytesPerPixel)
                throw new ArgumentException("Output format's bytes per pixel do not match input format.", "outputFormat");
            if (inputFormat.BitMasks.SequenceEqual(outputFormat.BitMasks))
                return; // Nothing to change; they're the same already.
            int step = outputFormat.BytesPerPixel;
            int lineOffset = 0;
            if (inputFormat.BitsAmounts.SequenceEqual(outputFormat.BitsAmounts))
            {
                // Actually has same bit amounts : simply reorder the raw data.
                for (int y = 0; y < height; ++y)
                {
                    int offset = lineOffset;
                    for (int x = 0; x < width; ++x)
                    {
                        uint[] argbValues = inputFormat.GetRawComponents(imageData, offset);
                        outputFormat.WriteRawComponents(imageData, offset, argbValues);
                        offset += step;
                    }
                    lineOffset += stride;
                }
                return;
            }
            ReadOnlyCollection<double> mulIn = inputFormat.Multipliers;
            ReadOnlyCollection<double> mulOut = outputFormat.Multipliers;
            ReadOnlyCollection<byte> bitsOut = outputFormat.BitsAmounts;
            uint[] maxOut = outputFormat.Maximums.ToArray();
            // Get converter multiplier.
            bool[] isZeroOut = new bool[4];
            double[] multiplier = new double[4];
            for (int i = 0; i < 4; ++i)
            {
                bool outChanIsZero = bitsOut[i] == 0;
                isZeroOut[i] = outChanIsZero;
                multiplier[i] = outChanIsZero ? 0 : mulIn[i] / mulOut[i];
            }
            for (int y = 0; y < height; ++y)
            {
                int offset = lineOffset;
                for (int x = 0; x < width; ++x)
                {
                    uint[] argbValues = inputFormat.GetRawComponents(imageData, offset);
                    for (int i = 0; i < 4; ++i)
                        argbValues[i] = isZeroOut[i] ? 0 : Math.Min((uint)Math.Round(argbValues[i] * multiplier[i], MidpointRounding.AwayFromZero), maxOut[i]);
                    outputFormat.WriteRawComponents(imageData, offset, argbValues);
                    offset += step;
                }
                lineOffset += stride;
            }
        }

        /// <summary>
        /// Converts the bits inside a byte array to a new pixel format. Both formats are specified by a PixelFormatter object.
        /// If both formats have the same pixel length, use ReorderBits.
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="stride">Image data stride. Is adjusted to the output's stride.</param>
        /// <param name="inputFormat">Input pixel formatter.</param>
        /// <param name="outputFormat">Output pixel formatter.</param>
        public static byte[] ConvertBits(byte[] imageData, int width, int height, ref int stride, PixelFormatter inputFormat, PixelFormatter outputFormat)
        {
            int stepIn = inputFormat.BytesPerPixel;
            int stepOut = outputFormat.BytesPerPixel;
            int newStride = stepOut * width;
            int newSize = newStride * height;
            byte[] newData = new byte[newSize];

            // Converter multiplier. Example:
            // in:  3 bits => 111    => max  7 => multfactor = 255 /  7 = 36.428571
            // out: 6 bits => 111111 => max 63 => multfactor = 255 / 63 =  4.047619
            // Conversion multiplication factor: (36.428571/4.047619) = 9
            // 7 * 9 = 63 => successful conversion from 'in' to 'out' format.

            // Caching these in advance, because every call to the getter repeats the readonly-wrapping.
            ReadOnlyCollection<double> mulIn = inputFormat.Multipliers;
            ReadOnlyCollection<byte> bitsOut = outputFormat.BitsAmounts;
            ReadOnlyCollection<double> mulOut = outputFormat.Multipliers;
            ReadOnlyCollection<uint> maxOut = outputFormat.Maximums;
            // Get converter multiplier.
            bool[] isZeroOut = new bool[4];
            double[] multiplier = new double[4];
            for (int i = 0; i < 4; ++i)
            {
                bool outChanIsZero = bitsOut[i] == 0;
                isZeroOut[i] = outChanIsZero;
                multiplier[i] = outChanIsZero ? 0 : mulIn[i] / mulOut[i];
            }
            int lineOffsetIn = 0;
            int lineOffsetOut = 0;
            for (int y = 0; y < height; ++y)
            {
                int offsetIn = lineOffsetIn;
                int offsetOut = lineOffsetOut;
                for (int x = 0; x < width; ++x)
                {
                    uint[] argbValues = inputFormat.GetRawComponents(imageData, offsetIn);
                    for (int i = 0; i < 4; ++i)
                        argbValues[i] = isZeroOut[i] ? 0 : Math.Min((uint)Math.Round(argbValues[i] * multiplier[i], MidpointRounding.AwayFromZero), maxOut[i]);
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

        /// <summary>
        /// Enum version of the color component indices.
        /// </summary>
        public enum ColorComponent
        {
            /// <summary>The index used for the Alpha color components in all arrays.</summary>
            ColA = PixelFormatter.ColA,
            /// <summary>The index used for the Red color components in all arrays.</summary>
            ColR = PixelFormatter.ColR,
            /// <summary>The index used for the Green color components in all arrays.</summary>
            ColG = PixelFormatter.ColG,
            /// <summary>The index used for the Blue color components in all arrays.</summary>
            ColB = PixelFormatter.ColB,
        }
    }
}
