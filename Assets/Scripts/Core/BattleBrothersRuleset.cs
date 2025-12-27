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
            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid != null)
            {
                string teamZocState = "ZoC_" + unit.teamId;
                string unitZocState = $"ZoC_{unit.teamId}_{unit.Id}";
                foreach (var neighbor in grid.GetNeighbors(hex))
                {
                    neighbor.AddState(teamZocState);
                    neighbor.AddState(unitZocState);
                }
            }
        }

        public override void OnDeparture(Unit unit, HexData hex)
        {
            if (unit == null || hex == null) return;

            hex.RemoveState("Occupied_" + unit.teamId);

            // Remove Zone of Control from neighbors
            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid != null)
            {
                string teamZocState = "ZoC_" + unit.teamId;
                string unitZocState = $"ZoC_{unit.teamId}_{unit.Id}";
                foreach (var neighbor in grid.GetNeighbors(hex))
                {
                    neighbor.RemoveState(teamZocState);
                    neighbor.RemoveState(unitZocState);
                }
            }
        }
    }
}
