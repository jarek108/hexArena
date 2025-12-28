using UnityEngine;

namespace HexGame
{
    public static class HexMath
    {
        // Static directions for neighbors (pointy-top orientation)
        private static readonly Vector3Int[] directions =
        {
            new Vector3Int(1, 0, -1), new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 1),
            new Vector3Int(-1, 0, 1), new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, -1)
        };

        public static Vector3Int Add(Vector3Int a, Vector3Int b) => a + b;
        public static Vector3Int Subtract(Vector3Int a, Vector3Int b) => a - b;
        public static Vector3Int Scale(Vector3Int a, int k) => a * k;
        public static Vector3Int Direction(int direction) => directions[(direction % 6 + 6) % 6];
        public static Vector3Int Neighbor(Vector3Int hex, int direction) => Add(hex, Direction(direction));

        public static int Distance(Vector3Int a, Vector3Int b)
        {
            return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
        }

        public static int Distance(HexData a, HexData b)
        {
            return Distance(new Vector3Int(a.Q, a.R, a.S), new Vector3Int(b.Q, b.R, b.S));
        }

        public static System.Collections.Generic.List<Vector3Int> GetLine(Vector3Int start, Vector3Int end)
        {
            int dist = Distance(start, end);
            var results = new System.Collections.Generic.List<Vector3Int>();
            
            for (int i = 0; i <= dist; i++)
            {
                float t = dist == 0 ? 0 : (float)i / dist;
                Vector3 lerp = Vector3.Lerp(
                    new Vector3(start.x, start.y, start.z) + new Vector3(1e-6f, 1e-6f, -2e-6f), 
                    new Vector3(end.x, end.y, end.z), 
                    t);
                results.Add(RoundToHex(lerp.x, lerp.y));
            }
            return results;
        }

        private static Vector3Int RoundToHex(float q_float, float r_float)
        {
            float s_float = -q_float - r_float;
            int q = Mathf.RoundToInt(q_float);
            int r = Mathf.RoundToInt(r_float);
            int s = Mathf.RoundToInt(s_float);

            float q_diff = Mathf.Abs(q - q_float);
            float r_diff = Mathf.Abs(r - r_float);
            float s_diff = Mathf.Abs(s - s_float);

            if (q_diff > r_diff && q_diff > s_diff) q = -r - s;
            else if (r_diff > s_diff) r = -q - s;
            else s = -q - r;

            return new Vector3Int(q, r, s);
        }
    }
}
