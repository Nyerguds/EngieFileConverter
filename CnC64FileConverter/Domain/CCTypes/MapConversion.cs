using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nyerguds.CCTypes
{
    public static class MapConversion
    {
        public static Dictionary<Int32, CnCMapCell> LoadMapping(Byte[] fileData, out String[] errors)
        {
            List<String> errorMessages = new List<String>();
            Dictionary<Int32, CnCMapCell> n64MapValues = new Dictionary<Int32, CnCMapCell>();
            Dictionary<Int32, CnCMapCell> reverseValues = new Dictionary<Int32, CnCMapCell>();
            using (MemoryStream ms = new MemoryStream(fileData))
            {
                Int32 amount = (Int32)ms.Length / 4;
                if (ms.Length != amount * 4)
                    throw new ArgumentException("file size must be divisible by 4!", "fileData");
                Byte[] buffer = new Byte[4];
                for (Int32 i = 0; i < amount; i++)
                {
                    if (ms.Read(buffer, 0, 4) == 4)
                    {
                        CnCMapCell N64cell = new CnCMapCell(buffer[0], buffer[1]);
                        CnCMapCell PCcell = new CnCMapCell(buffer[2], buffer[3]);
                        if (n64MapValues.ContainsKey(N64cell.Value))
                        {
                            n64MapValues.Clear();
                            throw new ApplicationException("File contains duplicate entries!");
                        }
                        else
                        {
                            if (reverseValues.ContainsKey(PCcell.Value))
                                errorMessages.Add(String.Format("Value {0} - {1} - PC value {1} already mapped on N64 value {2}", N64cell.ToString(), PCcell.ToString(), reverseValues[PCcell.Value].ToString()));
                            else
                                reverseValues.Add(PCcell.Value, N64cell);
                            n64MapValues.Add(N64cell.Value, PCcell);
                        }
                    }
                }
            }
            errors = errorMessages.ToArray();
            return n64MapValues;
        }

        public static Byte[] SaveMapping(Dictionary<Int32, CnCMapCell> mapping)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                List<Int32> keys = new List<Int32>(mapping.Keys);
                keys.Sort();
                foreach (Int32 key in keys)
                {
                    CnCMapCell N64cell = new CnCMapCell(key);
                    CnCMapCell PCcell = mapping[key];
                    ms.WriteByte(N64cell.HighByte);
                    ms.WriteByte(N64cell.LowByte);
                    ms.WriteByte(PCcell.HighByte);
                    ms.WriteByte(PCcell.LowByte);
                }
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static CnCMap ConvertMap(CnCMap map, Dictionary<Int32, CnCMapCell> mapping, Byte? defaultHigh, Byte? defaultLow, Boolean toN64, out List<CnCMapCell> errorcells)
        {
            Byte highByte = defaultHigh.GetValueOrDefault((Byte)0xFF);
            Byte lowByte = defaultLow.GetValueOrDefault((Byte)(toN64 ? 0xFF : 0x00));
            CnCMap newmap = new CnCMap(map.GetAsBytes());
            if (toN64)
                CleanupXCCMess(newmap);
            errorcells = new List<CnCMapCell>();
            for (Int32 i = 0; i < newmap.Cells.Length; i++)
            {
                Int32 cellvalue = newmap[i].Value;
                if ((!toN64 && cellvalue == 0xFFFF) || (toN64 && cellvalue == 0xFF00))
                {
                    newmap[i] = new CnCMapCell(toN64 ? 0xFFFF : 0xFF00);
                }
                else if (mapping.ContainsKey(cellvalue))
                {
                    newmap[i] = mapping[cellvalue];
                }
                else
                {
                    errorcells.Add(new CnCMapCell(cellvalue));
                    newmap[i] = new CnCMapCell(highByte, lowByte);
                }
            }
            return newmap;
        }

        public static Dictionary<Int32, CnCMapCell[]> GetReverseMapping(Dictionary<Int32, CnCMapCell> mapping, out List<CnCMapCell> errorcells)
        {
            Dictionary<Int32, CnCMapCell[]> newmapping = new Dictionary<Int32, CnCMapCell[]>();
            errorcells = new List<CnCMapCell>();
            foreach (Int32 mapval in mapping.Keys)
            {
                CnCMapCell cell = mapping[mapval];
                if (!newmapping.ContainsKey(cell.Value))
                    newmapping.Add(cell.Value, new CnCMapCell[] { new CnCMapCell(mapval) });
                else
                {
                    CnCMapCell[] orig = newmapping[cell.Value];
                    CnCMapCell[] arr = new CnCMapCell[orig.Length + 1];
                    Array.Copy(orig, arr, orig.Length);
                    arr[orig.Length] = new CnCMapCell(mapval);
                    newmapping[cell.Value] = arr;
                    if (!errorcells.Contains(cell))
                        errorcells.Add(cell);
                }
            }
            return newmapping;
        }

        /// <summary>
        /// Cleans up the literally-saved 'blank terrain' cells by replacing them by the default FF00 terrain.
        /// </summary>
        /// <param name="map">The map to fix</param>
        public static void CleanupXCCMess(CnCMap map)
        {
            foreach (CnCMapCell cell in map.Cells)
            {
                if (cell.HighByte == 0)
                {
                    cell.HighByte = 0xFF;
                    cell.LowByte = 0x00;
                }
                else if (cell.HighByte == 0xFF && cell.LowByte == 0xFF)
                    cell.LowByte = 0x00;
            }
        }
    }
}
