using System;
using UnityEngine;

namespace HexGame
{
    [Serializable]
    public class HexData
    {
        public int Q;
        public int R;
        public int S;
        public float Elevation;
        public TerrainType TerrainType;
        public Unit Unit;
        
        public HexData(int q, int r)
        {
            this.Q = q;
            this.R = r;
            this.S = -q - r;
        }
    }
}