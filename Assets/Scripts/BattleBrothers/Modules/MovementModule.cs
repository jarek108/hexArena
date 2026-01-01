using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    [CreateAssetMenu(fileName = "MovementModule", menuName = "HexGame/BattleBrothers/Modules/Movement")]
    public class MovementModule : ScriptableObject
    {
        [Header("Constraints")]
        public float maxElevationDelta = 1.0f;
        public float uphillPenalty = 1.0f;
        public float zocPenalty = 50.0f;

        [Header("Terrain Costs")]
        public float plainsCost = 2.0f;
        public float waterCost = 100000.0f;
        public float mountainCost = 5.0f;
        public float forestCost = 3.0f;
        public float desertCost = 4.0f;

        public float GetTerrainCost(TerrainType type)
        {
            switch (type)
            {
                case TerrainType.Plains: return plainsCost;
                case TerrainType.Water: return waterCost;
                case TerrainType.Mountains: return mountainCost;
                case TerrainType.Forest: return forestCost;
                case TerrainType.Desert: return desertCost;
                default: return 1.0f;
            }
        }

        public float GetMoveCost(Unit unit, HexData fromHex, HexData toHex, BattleBrothersRuleset ruleset)
        {
            // Implementation moved from BattleBrothersRuleset.GetPathfindingMoveCost
            if (toHex == ruleset.currentSearchTarget)
            {
                if (unit != null)
                {
                    foreach (var u in toHex.Units)
                    {
                        if (u == unit) continue;
                        if (u.teamId != unit.teamId)
                        {
                            // Enemy: Attack logic
                            int mat = unit.GetStat("MAT", 0);
                            if (mat > 0) // Melee check
                            {
                                float attackDelta = fromHex != null ? Mathf.Abs(toHex.Elevation - fromHex.Elevation) : 0f;
                                if (attackDelta > maxElevationDelta) return float.PositiveInfinity;
                            }
                            return 0f;
                        }
                        else
                        {
                            // Ally: Cannot stop on ally
                            return float.PositiveInfinity;
                        }
                    }
                }
            }

            float delta = fromHex != null ? Mathf.Abs(toHex.Elevation - fromHex.Elevation) : 0f;
            if (delta > maxElevationDelta) return float.PositiveInfinity;

            bool isOccupiedByTeammate = false;
            if (unit != null)
            {
                foreach (var state in toHex.States)
                {
                    if (state.StartsWith("Occupied"))
                    {
                        int underscoreIndex = state.IndexOf('_');
                        if (underscoreIndex > 8)
                        {
                            string teamPart = state.Substring(8, underscoreIndex - 8);
                            if (int.TryParse(teamPart, out int occupiedTeamId))
                            {
                                if (occupiedTeamId != unit.teamId) return float.PositiveInfinity;
                                else
                                {
                                    isOccupiedByTeammate = true;
                                    if (toHex == ruleset.currentSearchTarget) return float.PositiveInfinity;
                                }
                            }
                        }
                    }
                }
            }

            float cost = GetTerrainCost(toHex.TerrainType);
            if (fromHex != null && toHex.Elevation > fromHex.Elevation) cost += uphillPenalty;

            if (unit != null && !isOccupiedByTeammate)
            {
                foreach (var state in toHex.States)
                {
                    if (state.StartsWith("ZoC"))
                    {
                        int underscoreIndex = state.IndexOf('_');
                        if (underscoreIndex > 3)
                        {
                            string teamPart = state.Substring(3, underscoreIndex - 3);
                            if (int.TryParse(teamPart, out int zocTeamId))
                            {
                                if (zocTeamId != unit.teamId)
                                {
                                    cost += zocPenalty;
                                    break; 
                                }
                            }
                        }
                    }
                }
            }

            return cost;
        }
    }
}
