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
            
            for (int i = 0; i < 6; i++)
            {
                Vector3Int neighborCoords = Hex.Neighbor(new Vector3Int(hex.Q, hex.R, hex.S), i);
                HexData neighbor = GetHexAt(neighborCoords.x, neighborCoords.y);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }
            return neighbors;
        }
        
        public List<HexData> GetHexesInRange(HexData center, int radius)
        {
            List<HexData> results = new List<HexData>();
            if (center == null) return results;

            Vector3Int centerCoords = new Vector3Int(center.Q, center.R, center.S);

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = Mathf.Max(-radius, -q - radius); r <= Mathf.Min(radius, -q + radius); r++)
                {
                    Vector3Int relativeCoords = new Vector3Int(q, r, -q-r);
                    HexData hex = GetHexAt(center.Q + q, center.R + r);
                    if (hex != null)
                    {
                        results.Add(hex);
                    }
                }
            }
            return results;
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