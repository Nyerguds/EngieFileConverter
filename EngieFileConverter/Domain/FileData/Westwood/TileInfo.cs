using System;
using System.Collections.Generic;

namespace Nyerguds.FileData.Westwood
{
    public class TileInfo
    {
        public String TileName { get; set; }

        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType PrimaryType { get; set; }
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType SecondaryType { get; set; }
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public List<Int32> SecondaryTypeCells { get; set; }
        public Int32 NameID { get; set; }
        public TerrainTypeEnh PrimaryHeightType { get; set; }
        public TerrainTypeEnh[] TypedCells { get; set; }

        public TileInfo()
        {
            PrimaryType = TerrainType.Clear;
            PrimaryHeightType = TerrainTypeEnh.Clear;
            TypedCells = new TerrainTypeEnh[0];
        }
    }
}
