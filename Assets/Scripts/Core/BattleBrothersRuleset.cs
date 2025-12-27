using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    [CreateAssetMenu(fileName = "BattleBrothersRuleset", menuName = "HexGame/Ruleset/BattleBrothers")]
    public class BattleBrothersRuleset : Ruleset
    {
        [Header("Movement Constraints")]
        public float maxElevationDelta = 1.0f;
        public float uphillPenalty = 1.0f;
        public float zocPenalty = 50.0f;

        [Header("Terrain Costs")]
        public float plainsCost = 2.0f;
        public float waterCost = 10.0f;
        public float mountainCost = 5.0f;
        public float forestCost = 3.0f;
        public float desertCost = 4.0f;

        public override float GetMoveCost(Unit unit, HexData fromHex, HexData toHex)
        {
            // 1. Elevation Check
            float delta = Mathf.Abs(toHex.Elevation - fromHex.Elevation);
            if (delta > maxElevationDelta) return float.PositiveInfinity;

            // 2. Base Cost from Terrain
            float cost = GetTerrainCost(toHex.TerrainType);

            // 3. Uphill Penalty
            if (toHex.Elevation > fromHex.Elevation)
            {
                cost += uphillPenalty;
            }

            // 4. Zone of Control Penalty
            // Only applied if a unit is moving (unit != null)
            // and enters a hex with an enemy ZoC state.
            if (unit != null)
            {
                foreach (var state in toHex.States)
                {
                    // Check if state is a ZoC state (starts with "ZoC_")
                    if (state.StartsWith("ZoC_"))
                    {
                        // Extract team ID from "ZoC_X" or "ZoC_X_Y"
                        // The format is always ZoC_{teamId} or ZoC_{teamId}_{unitId}
                        // simpler check: "ZoC_{teamId}" string presence isn't enough because we need to know IF it is enemy.
                        
                        // Let's parse the team ID.
                        // Format: "ZoC_1", "ZoC_1_102"
                        var parts = state.Split('_');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int zocTeamId))
                        {
                            if (zocTeamId != unit.teamId)
                            {
                                cost += zocPenalty;
                                break; // Apply penalty once per hex max (or per enemy team? Usually just "in ZoC")
                            }
                        }
                    }
                }
            }

            return cost;
        }

        private float GetTerrainCost(TerrainType type)
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

        public override void OnEntry(Unit unit, HexData hex)
        {
            if (unit == null || hex == null) return;
            
            hex.AddState("Occupied_" + unit.teamId);

            // Add Zone of Control to neighbors
            // 1. Must have Melee Range
            if (unit.GetStat("MRNG") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string teamZocState = "ZoC_" + unit.teamId;
                    string unitZocState = $"ZoC_{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(hex))
                    {
                        // 2. Must be reachable (Elevation)
                        float delta = Mathf.Abs(neighbor.Elevation - hex.Elevation);
                        if (delta <= maxElevationDelta)
                        {
                            neighbor.AddState(teamZocState);
                            neighbor.AddState(unitZocState);
                        }
                    }
                }
            }
        }

        public override void OnDeparture(Unit unit, HexData hex)
        {
            if (unit == null || hex == null) return;

            hex.RemoveState("Occupied_" + unit.teamId);

            // Remove Zone of Control from neighbors
            // Only if unit actually exerts ZoC
            if (unit.GetStat("MRNG") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string teamZocState = "ZoC_" + unit.teamId;
                    string unitZocState = $"ZoC_{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(hex))
                    {
                        // We can blindly remove; if it wasn't added due to elevation, 
                        // removing it does nothing.
                        neighbor.RemoveState(teamZocState);
                        neighbor.RemoveState(unitZocState);
                    }
                }
            }
        }
    }
}
