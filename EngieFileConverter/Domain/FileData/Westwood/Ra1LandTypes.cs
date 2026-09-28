using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngieFileConverter.Domain.FileData.Westwood
{
    public class Ra1LandTypes
    {
        public static readonly Dictionary<char, byte> LandTypesMapping = new Dictionary<char, byte>
        {
            { 'X', 00 }, // Filler tile, or [Clear] terrain on 1x1 sets with multiple tiles.
            { 'C', 03 }, // [Clear] Normal clear terrain.
            { 'B', 06 }, // [Beach] Sandy beach. Can''t be built on.
            { 'I', 08 }, // [Rock]  Impassable terrain.
            { 'R', 09 }, // [Road]  Units move faster on this terrain.
            { 'W', 10 }, // [Water] Ships can travel over this.
            { 'V', 11 }, // [River] Ships normally can''t travel over this.
            { 'H', 14 }, // [Rough] Rough terrain. Can''t be built on
        };
        public static readonly Dictionary<byte, char> LandTypesMappingRev = LandTypesMapping.ToDictionary(x => x.Value, x => x.Key);

        public static byte[] LandTypesFromString(string types, int arrLen)
        {
            types = types.Replace(" ", String.Empty);
            arrLen = Math.Min(arrLen, types.Length);
            byte[] arr = new byte[arrLen];
            char[] input = types.ToUpperInvariant().ToCharArray();
            int inputLen = input.Length;
            for (int i = 0; i < input.Length; ++i)
            {
                arr[i] = (byte)(i >= inputLen ? 0 : LandTypesMapping.TryGetValue(input[i], out byte t) ? t : 0);
            }
            return arr;
        }
        public static string LandTypesToString(byte[] types)
        {
            char[] output = new char[types.Length];
            for (int i = 0; i < types.Length; ++i)
            {
                output[i] = LandTypesMappingRev.TryGetValue(types[i], out char t) ? t : 'X';
            }
            return new string(output);
        }
    }
}
