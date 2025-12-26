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
    }
}
