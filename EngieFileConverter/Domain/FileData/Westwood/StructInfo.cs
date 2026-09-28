using System;

namespace Nyerguds.FileData.Westwood
{
    public class StructInfo
    {
        public string StructName { get; set; }
        public bool HasBib { get; set; }
        public bool[] OccupyList { get; set; }
        public int Width { get; set; }
        public int Height { get; set; } 
    }
}