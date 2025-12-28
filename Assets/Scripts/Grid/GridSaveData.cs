
using System.Collections.Generic;

namespace HexGame
{
    [System.Serializable]
    public class HexSaveData
    {
        public int q;
        public int r;
        public float elevation;
        public TerrainType terrainType;
    }

    [System.Serializable]
    public class GridSaveData
    {
        public int width;
        public int height;
        public List<HexSaveData> hexes = new List<HexSaveData>();
    }
}
