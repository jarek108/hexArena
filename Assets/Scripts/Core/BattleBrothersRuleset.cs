using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    public enum AttackType { None, Melee, Ranged }

    [CreateAssetMenu(fileName = "BattleBrothersRuleset", menuName = "HexGame/Ruleset/BattleBrothers")]
    public class BattleBrothersRuleset : Ruleset
    {
        [Header("Movement Constraints")]
        public float maxElevationDelta = 1.0f;
        public float uphillPenalty = 1.0f;
        public float zocPenalty = 50.0f;
        public float transitionSpeed = 5.0f;
        public float transitionPause = 0.1f;

        [Header("Terrain Costs")]
        public float plainsCost = 2.0f;
        public float waterCost = 100000.0f;
        public float mountainCost = 5.0f;
        public float forestCost = 3.0f;
        public float desertCost = 4.0f;

        public AttackType currentAttackType = AttackType.None;

        private HexGame.Units.UnitVisualization pathGhost;
        private Unit lastGhostSource;

        public override void OnStartPathfinding(HexData target, Unit unit)
        {
            base.OnStartPathfinding(target, unit);
            
            currentAttackType = AttackType.None;
            if (target.Unit != null && unit != null)
            {
                int mrng = unit.GetStat("MRNG", 1);
                int rrng = unit.GetStat("RRNG", 0);
                currentAttackType = (rrng > mrng) ? AttackType.Ranged : AttackType.Melee;
            }
        }

        public override void OnFinishPathfinding(Unit unit, List<HexData> path, bool success)
        {
            if (!success || unit == null || path == null)
            {
                OnClearPathfindingVisuals();
                return;
            }

            // Sync ghost instance
            if (pathGhost == null || lastGhostSource != unit)
            {
                OnClearPathfindingVisuals();
                SpawnGhost(unit);
            }

            if (pathGhost != null)
            {
                int stopIndex = GetMoveStopIndex(unit, path);
                if (stopIndex > 0)
                {
                    HexData ghostHex = path[stopIndex - 1];
                    var manager = GridVisualizationManager.Instance;
                    Hex hexView = manager.GetHex(ghostHex.Q, ghostHex.R);
                    if (hexView != null)
                    {
                        Vector3 pos = hexView.transform.position;
                        pos.y += pathGhost.yOffset;
                        pathGhost.transform.position = pos;
                        pathGhost.gameObject.SetActive(true);
                    }
                }
                else
                {
                    pathGhost.gameObject.SetActive(false);
                }
            }
        }

        public override void OnClearPathfindingVisuals()
        {
            if (pathGhost != null)
            {
                if (Application.isPlaying) Destroy(pathGhost.gameObject);
                else DestroyImmediate(pathGhost.gameObject);
                pathGhost = null;
            }
            lastGhostSource = null;
        }

        private void SpawnGhost(Unit sourceUnit)
        {
            var unitManager = UnitManager.Instance;
            if (unitManager == null || unitManager.unitVisualizationPrefab == null) return;

            pathGhost = Instantiate(unitManager.unitVisualizationPrefab, unitManager.transform);
            pathGhost.gameObject.name = "Pathfinding_PreviewGhost_BB";
            pathGhost.SetPreviewIdentity(sourceUnit.UnitName);
            lastGhostSource = sourceUnit;

            ApplyGhostVisuals(pathGhost.gameObject);
        }

        private void ApplyGhostVisuals(GameObject ghostObj)
        {
            Renderer[] renderers = ghostObj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                
                Color color = Color.white;
                if (r.sharedMaterial.HasProperty("_BaseColor"))
                {
                    color = r.sharedMaterial.GetColor("_BaseColor");
                }

                color.a = 0.5f; 
                mpb.SetColor("_BaseColor", color);
                r.SetPropertyBlock(mpb);
            }
        }

        public override void ExecutePath(Unit unit, List<HexData> path, Hex targetHex)
        {
            if (unit == null || path == null) return;

            unit.MoveAlongPath(path, transitionSpeed, transitionPause, () => {
                // When path finishes, check if we need to attack
                if (targetHex != null && targetHex.Unit != null && targetHex.Unit.teamId != unit.teamId)
                {
                    PerformAttack(unit, targetHex.Unit);
                }
            });
        }

        private void PerformAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;

            // 1. Logic: Facing
            attacker.FacePosition(target.transform.position);

            // 2. Logic: Hooks
            OnAttack(attacker, target);
            OnBeingAttacked(attacker, target);

            // 3. View: Triggers
            var attackerViz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
            if (attackerViz != null) attackerViz.OnAttack(target);

            var targetViz = target.GetComponent<HexGame.Units.UnitVisualization>();
            if (targetViz != null) targetViz.OnTakeDamage(10); // Dummy
        }

        public override float GetMoveCost(Unit unit, HexData fromHex, HexData toHex)
        {
            // 0. Interaction Target override
            if (toHex == currentSearchTarget)
            {
                bool isEnemy = toHex.Unit != null && unit != null && toHex.Unit.teamId != unit.teamId;
                
                if (isEnemy)
                {
                    // Melee attacks are subject to elevation checks even for the reach step.
                    if (currentAttackType == AttackType.Melee)
                    {
                        float attackDelta = Mathf.Abs(toHex.Elevation - fromHex.Elevation);
                        if (attackDelta > maxElevationDelta) return float.PositiveInfinity;
                    }
                    
                    // For Ranged attacks, we allow the pathfinder to "reach" the target regardless of elevation
                    // so that the path correctly ends on the target and can be truncated.
                    return 0f;
                }
            }

            // 1. Elevation Check
            float delta = Mathf.Abs(toHex.Elevation - fromHex.Elevation);
            if (delta > maxElevationDelta) return float.PositiveInfinity;

            // 2. Occupation Check
            if (unit != null)
            {
                foreach (var state in toHex.States)
                {
                    if (state.StartsWith("Occupied_"))
                    {
                        var parts = state.Split('_');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int occupiedTeamId))
                        {
                            // Enemy occupation is always impassable
                            if (occupiedTeamId != unit.teamId)
                            {
                                return float.PositiveInfinity;
                            }
                            else
                            {
                                // Friendly occupation: 
                                // Allowed to pass through, but FORBIDDEN to end move there.
                                
                                // Case A: Direct move to this hex
                                if (toHex == currentSearchTarget)
                                    return float.PositiveInfinity;

                                // Case B: Stopping here to attack an enemy
                                if (currentSearchTarget != null && currentSearchTarget.Unit != null)
                                {
                                    int mrng = unit.GetStat("MRNG", 1);
                                    int rrng = unit.GetStat("RRNG", 0);
                                    int range = Mathf.Max(mrng, rrng);

                                    int dist = HexMath.Distance(toHex, currentSearchTarget);
                                    if (dist <= range)
                                    {
                                        // This hex is within attack range.
                                        // Since A* evaluates steps, if we reach a hex within range,
                                        // the pathfinder will stop the search there (via GetMoveStopIndex logic later).
                                        // Therefore, if it's occupied, we can't 'use' it as a strike position.
                                        return float.PositiveInfinity;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 3. Base Cost from Terrain
            float cost = GetTerrainCost(toHex.TerrainType);

            // 4. Uphill Penalty
            if (toHex.Elevation > fromHex.Elevation)
            {
                cost += uphillPenalty;
            }

            // 5. Zone of Control Penalty
            // Only applied if a unit is moving (unit != null)
            // and enters a hex with an enemy ZoC state.
            if (unit != null)
            {
                foreach (var state in toHex.States)
                {
                    // Check if state is a ZoC state (starts with "ZoC_")
                    if (state.StartsWith("ZoC_"))
                    {
                        var parts = state.Split('_');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int zocTeamId))
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

            return cost;
        }

        public override void OnAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            Debug.Log($"[Ruleset] {attacker.UnitName} attacks {target.UnitName}");
            // BB Specific: Attacking increases fatigue
            // attacker.Stats["FAT"] += 10;
        }

        public override void OnBeingAttacked(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            // BB Specific: Gain defensive bonus or morale check
        }

        public override int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            if (path == null || path.Count == 0 || unit == null) return 0;

            // Check if last hex is occupied by enemy
            HexData lastHex = path[path.Count - 1];
            if (lastHex.Unit != null && lastHex.Unit.teamId != unit.teamId)
            {
                int mrng = unit.GetStat("MRNG", 1);
                int rrng = unit.GetStat("RRNG", 0);
                int range = Mathf.Max(mrng, rrng);

                // Stop 'range' hexes before the end
                return Mathf.Max(1, path.Count - range);
            }

            return path.Count;
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
