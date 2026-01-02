using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    [CreateAssetMenu(fileName = "TacticalModule", menuName = "HexGame/BattleBrothers/Modules/Tactical")]
    public class TacticalModule : ScriptableObject
    {
        public void ProjectUnitInfluence(Unit unit, HexData targetHex, float maxElevationDelta)
        {
            if (unit == null || targetHex == null) return;

            // 1. Occupation
            unit.AddOwnedHexState(targetHex, $"Occupied{unit.teamId}_{unit.Id}");

            // 2. ZoC
            if (unit.GetStat("MAT") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string unitZocState = $"ZoC{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(targetHex))
                    {
                        float delta = Mathf.Abs(neighbor.Elevation - targetHex.Elevation);
                        if (delta <= maxElevationDelta)
                        {
                            unit.AddOwnedHexState(neighbor, unitZocState);
                        }
                    }
                }
            }

            // 3. Active Turn Highlight
            if (GameMaster.Instance != null && GameMaster.Instance.activeUnit == unit)
            {
                unit.AddOwnedHexState(targetHex, "Active");
            }
        }

        public void ShowAoA(Unit unit, HexData stopHex, float maxElevationDelta)
        {
            if (unit == null) return;
            ClearAoA(unit);

            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid == null) return;

            int rng = unit.GetStat("RNG", 1);
            int mat = unit.GetStat("MAT", 0);
            bool isMelee = mat > 0;

            string aoaState = $"AoA{unit.teamId}_{unit.Id}";
            var inRange = grid.GetHexesInRange(stopHex, rng);

            foreach (var h in inRange)
            {
                if (h == stopHex) continue;
                
                if (isMelee)
                {
                    float delta = Mathf.Abs(h.Elevation - stopHex.Elevation);
                    if (delta > maxElevationDelta) continue;
                }

                unit.AddOwnedHexState(h, aoaState);
            }
        }

        public void ClearAoA(Unit unit)
        {
            if (unit != null)
            {
                unit.RemoveOwnedHexStatesByPrefix("AoA");
            }
        }
    }
}
