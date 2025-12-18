using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nyerguds.CCTypes
{
    public class TileInfo
    {
        public String TileName;
        public Int32 Width;
        public Int32 Height;
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType PrimaryType = TerrainType.Clear;
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType SecondaryType;
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public List<Int32> SecondaryTypeCells;
        public Int32 NameID;
        public HeightTerrainType PrimaryHeightType = HeightTerrainType.Clear;
        public HeightTerrainType[] TypedCells = new HeightTerrainType[0];
    }
}
