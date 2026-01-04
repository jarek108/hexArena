using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace HexGame
{
    public struct PathResult
    {
        public HexData Source;
        public HexData Target;
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

        public static PathResult FindPath(Grid grid, Unit unit, HexData start, params HexData[] targets)
        {

            //Debug.Log("start path profiling");
            Profiler.BeginSample("Pathfinder.FindPath");

            HexData primaryTarget = (targets != null && targets.Length > 0) ? targets[0] : null;

            if (grid == null || start == null || targets == null || targets.Length == 0)
            {
                Profiler.EndSample();
                //Debug.Log("end path profiling");

                return new PathResult { Source = start, Target = primaryTarget, Success = false };
            }

            HashSet<HexData> targetSet = new HashSet<HexData>(targets);

            if (targetSet.Contains(start))
            {
                Profiler.EndSample();
                //Debug.Log("end profiling");

                return new PathResult { Source = start, Target = start, Path = new List<HexData> { start }, TotalCost = 0, Success = true };
            }

            Ruleset ruleset = GameMaster.Instance != null ? GameMaster.Instance.ruleset : 
                Object.FindFirstObjectByType<GameMaster>()?.ruleset;

            var openSet = new List<Node>();
            var closedSet = new HashSet<HexData>();

            Node startNode = new Node(start, null, 0, GetHeuristic(start, targetSet));
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // Get node with lowest F score
                Node current = openSet.OrderBy(n => n.F).ThenBy(n => n.H).First();

                if (targetSet.Contains(current.Data))
                {
                    var path = RetracePath(startNode, current);

                    Profiler.EndSample();
                    //Debug.Log("end path profiling");

                    return path;
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
                        openSet.Add(new Node(neighbor, current, newG, GetHeuristic(neighbor, targetSet)));
                    }
                    else if (newG < neighborNode.G)
                    {
                        neighborNode.G = newG;
                        neighborNode.Parent = current;
                    }
                }
            }

            Profiler.EndSample();
            //Debug.Log("end path profiling");
            return new PathResult { Source = start, Target = primaryTarget, Success = false };
        }

        private static float GetHeuristic(HexData current, HashSet<HexData> targets)
        {
            float min = float.MaxValue;
            foreach (var target in targets)
            {
                int dist = HexMath.Distance(current, target);
                if (dist < min) min = dist;
            }
            return min;
        }

        private static PathResult RetracePath(Node startNode, Node targetNode)
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
            return new PathResult { 
                Source = startNode.Data, 
                Target = targetNode.Data, 
                Path = path, 
                TotalCost = totalCost, 
                Success = true 
            };
        }
    }
}