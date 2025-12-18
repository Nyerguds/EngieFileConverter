using System;

namespace Nyerguds.FileData.Westwood
{
    public class StructInfo
    {
        public String StructName { get; set; }
        public Boolean HasBib { get; set; }
        public Boolean[] OccupyList { get; set; }
        public Int32 Width { get; set; }
        public Int32 Height { get; set; } 
    }
}