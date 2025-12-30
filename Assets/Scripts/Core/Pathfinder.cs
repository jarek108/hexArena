using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexGame
{
    public struct PathResult
    {
        public List<HexData> Path;
        public float TotalCost;
        public bool Success;
    }

    public static class Pathfinder
    {
        private class Node
        {
            public HexData Data;
            public Node Parent;
            public float G; // Cost from start
            public float H; // Heuristic to target
            public float F => G + H;

            public Node(HexData data, Node parent, float g, float h)
            {
                Data = data;
                Parent = parent;
                G = g;
                H = h;
            }
        }

        public static PathResult FindPath(Grid grid, Unit unit, HexData start, HexData target)
        {
            if (grid == null || start == null || target == null)
                return new PathResult { Success = false };

            if (start == target)
                return new PathResult { Path = new List<HexData> { start }, TotalCost = 0, Success = true };

            Ruleset ruleset = GameMaster.Instance != null ? GameMaster.Instance.ruleset : 
                Object.FindFirstObjectByType<GameMaster>()?.ruleset;

            var openSet = new List<Node>();
            var closedSet = new HashSet<HexData>();

            openSet.Add(new Node(start, null, 0, HexMath.Distance(start, target)));

            while (openSet.Count > 0)
            {
                // Get node with lowest F score
                Node current = openSet.OrderBy(n => n.F).ThenBy(n => n.H).First();

                if (current.Data == target)
                {
                    return RetracePath(current);
                }

                openSet.Remove(current);
                closedSet.Add(current.Data);

                foreach (HexData neighbor in grid.GetNeighbors(current.Data))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    // --- RULESET LOGIC ---
                    float moveCost = 1.0f; // Default fallback
                    if (ruleset != null)
                    {
                        moveCost = ruleset.GetPathfindingMoveCost(unit, current.Data, neighbor);
                    }
                    else
                    {
                        // Fallback logic if no ruleset exists
                        if (Mathf.Abs(neighbor.Elevation - current.Data.Elevation) > 1.0f) continue;
                        moveCost = neighbor.GetMovementCost();
                    }

                    if (float.IsInfinity(moveCost) || moveCost > 1000000) continue;

                    float newG = current.G + moveCost;
                    Node neighborNode = openSet.FirstOrDefault(n => n.Data == neighbor);

                    if (neighborNode == null)
                    {
                        openSet.Add(new Node(neighbor, current, newG, HexMath.Distance(neighbor, target)));
                    }
                    else if (newG < neighborNode.G)
                    {
                        neighborNode.G = newG;
                        neighborNode.Parent = current;
                    }
                }
            }

            return new PathResult { Success = false };
        }

        private static PathResult RetracePath(Node targetNode)
        {
            List<HexData> path = new List<HexData>();
            float totalCost = targetNode.G;
            Node current = targetNode;

            while (current != null)
            {
                path.Add(current.Data);
                current = current.Parent;
            }

            path.Reverse();
            return new PathResult { Path = path, TotalCost = totalCost, Success = true };
        }
    }
}