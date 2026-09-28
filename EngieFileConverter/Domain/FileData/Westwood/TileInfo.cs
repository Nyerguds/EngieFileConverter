using System;
using System.Collections.Generic;

namespace Nyerguds.FileData.Westwood
{
    public class TileInfo
    {
        public string TileName { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType PrimaryType { get; set; }
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType SecondaryType { get; set; }
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public List<int> SecondaryTypeCells { get; set; }
        public int NameID { get; set; }
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
