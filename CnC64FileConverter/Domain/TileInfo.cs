using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CnC64FileConverter.Domain
{
    public class TileInfo
    {
        public String TileName;
        public Int32 Width;
        public Int32 Height;
        public TerrainType PrimaryType = TerrainType.Clear;
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public TerrainType SecondaryType;
        /// <summary>Obsolete. No longer used since the switch to tilesets2. TypedCells is used instead.</summary>
        public List<Int32> SecondaryTypeCells;
        public Int32 NameID;
        public TerrainType[] TypedCells = new TerrainType[0];
    }

    public enum TerrainType
    {
        Unused = 0,
        Clear = 1,
        Water = 2,
        Rock = 3,
        Beach = 4,
        Road = 5
    }
}
