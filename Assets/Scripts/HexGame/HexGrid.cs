using System.Collections.Generic;
using UnityEngine;

namespace HexGame
{
    public class HexGrid
    {
        private readonly Dictionary<Vector2Int, HexData> hexes = new Dictionary<Vector2Int, HexData>();

        public int Width { get; }
        public int Height { get; }
        
        public HexGrid(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void AddHex(HexData hex)
        {
            if (hex != null)
            {
                Vector2Int coords = new Vector2Int(hex.Q, hex.R);
                if (!hexes.ContainsKey(coords))
                {
                    hexes.Add(coords, hex);
                }
            }
        }

        public HexData GetHexAt(Vector2Int coordinates)
        {
            hexes.TryGetValue(coordinates, out HexData hex);
            return hex;
        }
        
        public HexData GetHexAt(int q, int r)
        {
            return GetHexAt(new Vector2Int(q, r));
        }

        public List<HexData> GetNeighbors(HexData hex)
        {
            List<HexData> neighbors = new List<HexData>();
            if (hex == null) return neighbors;
            
            // Hex neighbor directions (Cube coordinates logic adaptation)
            // Directions: (1, -1, 0), (1, 0, -1), (0, 1, -1), (-1, 1, 0), (-1, 0, 1), (0, -1, 1)
            // Q, R changes:
            // (+1, 0), (+1, -1), (0, -1), (-1, 0), (-1, +1), (0, +1)
            
            int[] dq = { 1, 1, 0, -1, -1, 0 };
            int[] dr = { 0, -1, -1, 0, 1, 1 };

            for (int i = 0; i < 6; i++)
            {
                int nq = hex.Q + dq[i];
                int nr = hex.R + dr[i];
                
                HexData neighbor = GetHexAt(nq, nr);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }
            return neighbors;
        }

        public IEnumerable<HexData> GetAllHexes()
        {
            return hexes.Values;
        }
        
        public void Clear()
        {
            hexes.Clear();
        }
    }
}