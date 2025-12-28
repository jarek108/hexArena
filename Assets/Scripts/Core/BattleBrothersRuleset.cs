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

        [Header("Combat Modifiers")]
        public float elevationBonus = 10f;
        public float elevationPenalty = 10f;
        public float surroundBonus = 5f;
        public float longWeaponProximityPenalty = 15f;
        public float rangedHighGroundBonus = 10f;
        public float rangedDistancePenalty = 2f;

        public AttackType currentAttackType = AttackType.None;

        private HexGame.Units.UnitVisualization pathGhost;
        private Unit lastGhostSource;
        private List<HexData> currentAoAHexes = new List<HexData>();

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

        public override void OnUnitSelected(Unit unit)
        {
            OnClearPathfindingVisuals();
        }

        public override void OnUnitDeselected(Unit unit)
        {
            OnClearPathfindingVisuals();
        }

        public override float HitChance(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return 0f;

            HexData attackerHex = attacker.CurrentHex?.Data;
            HexData targetHex = target.CurrentHex?.Data;
            if (attackerHex == null || targetHex == null) return 0f;

            int dist = HexMath.Distance(attackerHex, targetHex);
            int mrng = attacker.GetStat("MRNG", 1);
            int rrng = attacker.GetStat("RRNG", 0);

            // Determine which formula to use
            if (dist <= mrng)
            {
                return MeleeHitChance(attacker, target, attackerHex, targetHex, dist);
            }
            else if (dist <= rrng)
            {
                return RangedHitChance(attacker, target, attackerHex, targetHex, dist);
            }

            return 0f;
        }

        private float MeleeHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            // 1. Base Stats
            int mskl = attacker.GetStat("MSKL", 50);
            int mdef = target.GetStat("MDEF", 0);
            float score = mskl - mdef;

            // 2. Elevation Modifier
            if (attackerHex.Elevation > targetHex.Elevation) score += elevationBonus;
            else if (attackerHex.Elevation < targetHex.Elevation) score -= elevationPenalty;

            // 3. Distance-based modifiers
            int mrng = attacker.GetStat("MRNG", 1);

            // Long weapon proximity penalty
            if (mrng == 2 && dist == 1)
            {
                score -= longWeaponProximityPenalty;
            }

            // 4. Surround Bonus (Based on ally ZoC on target hex)
            int allyZoCCount = 0;
            string allyZoCPrefix = $"ZoC{attacker.teamId}_";
            foreach (var state in targetHex.States)
            {
                if (state.StartsWith(allyZoCPrefix))
                {
                    allyZoCCount++;
                }
            }
            
            // Surround bonus is (ZoC from team - 1) * bonus
            score += Mathf.Max(0, allyZoCCount - 1) * surroundBonus;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        private float RangedHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            // 1. Base Stats
            int rskl = attacker.GetStat("RSKL", 30);
            int rdef = target.GetStat("RDEF", 0);
            float score = rskl - rdef;

            // 2. Elevation Bonus (Ranged only gets bonus, no penalty for being low?)
            // BB usually gives +10% for high ground.
            if (attackerHex.Elevation > targetHex.Elevation)
            {
                score += rangedHighGroundBonus;
            }

            // 3. Distance Penalty
            // BB has a penalty per tile of distance. 
            // Often -2% or -3% per tile.
            score -= dist * rangedDistancePenalty;

            return Mathf.Clamp(score / 100f, 0f, 1f);
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

                        // Calculate and show AoA
                        ShowAoA(unit, ghostHex);
                    }
                }
                else
                {
                    pathGhost.gameObject.SetActive(false);
                }
            }
        }

        private void ShowAoA(Unit unit, HexData stopHex)
        {
            ClearAoA();

            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid == null) return;

            int mrng = unit.GetStat("MRNG", 1);
            int rrng = unit.GetStat("RRNG", 0);
            int range = Mathf.Max(mrng, rrng);
            bool isMelee = mrng >= rrng;

            string aoaState = $"AoA{unit.teamId}_{unit.Id}";
            var inRange = grid.GetHexesInRange(stopHex, range);

            foreach (var h in inRange)
            {
                if (h == stopHex) continue;

                // Melee AoA respects elevation
                if (isMelee)
                {
                    if (Mathf.Abs(h.Elevation - stopHex.Elevation) > maxElevationDelta) continue;
                }

                h.AddState(aoaState);
                currentAoAHexes.Add(h);
            }
        }

        private void ClearAoA()
        {
            if (currentAoAHexes.Count == 0) return;

            // We need to know the unit ID to clear the specific state
            // But since this is transient during pathfinding, we can just clear all AoA states from tracked hexes
            foreach (var h in currentAoAHexes)
            {
                // Remove any state starting with AoA
                var toRemove = new List<string>();
                foreach (var s in h.States)
                {
                    if (s.StartsWith("AoA")) toRemove.Add(s);
                }
                foreach (var s in toRemove) h.RemoveState(s);
            }
            currentAoAHexes.Clear();
        }

        public override void OnClearPathfindingVisuals()
        {
            ClearAoA();
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
            var sourceViz = sourceUnit.GetComponentInChildren<HexGame.Units.UnitVisualization>();
            if (sourceViz == null) return;

            var unitManager = UnitManager.Instance;
            if (unitManager == null) return;

            pathGhost = Instantiate(sourceViz, unitManager.transform);
            pathGhost.gameObject.name = "Pathfinding_PreviewGhost_BB";
            
            // Ensure preview identity is synced (in case simple viz logic changes)
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

            // 2. Logic: Roll for Success
            float chance = HitChance(attacker, target);
            float roll = Random.value;
            bool isHit = roll <= chance;

            string typeStr = currentAttackType == AttackType.Ranged ? "Ranged" : "Melee";
            string outcomeStr = isHit ? "<color=green>HIT</color>" : "<color=red>MISS</color>";
            Debug.Log($"[Ruleset] {typeStr} Attack: Chance {chance:P1}, Roll {roll:P1} -> {outcomeStr}");

            if (isHit)
            {
                // 3. Logic: Hooks
                OnAttack(attacker, target);
                OnBeingAttacked(attacker, target);

                // 4. View: Triggers
                var attackerViz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
                if (attackerViz != null) attackerViz.OnAttack(target);

                var targetViz = target.GetComponent<HexGame.Units.UnitVisualization>();
                if (targetViz != null) targetViz.OnTakeDamage(10); // Dummy
            }
            else
            {
                // Visual feedback for a miss? Maybe a different anim later.
                // For now, still trigger attacker's attack anim so they don't just stand there.
                var attackerViz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
                if (attackerViz != null) attackerViz.OnAttack(target);
            }
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
                    if (state.StartsWith("Occupied"))
                    {
                        // Parse "OccupiedT_U"
                        int underscoreIndex = state.IndexOf('_');
                        if (underscoreIndex > 8)
                        {
                            string teamPart = state.Substring(8, underscoreIndex - 8);
                            if (int.TryParse(teamPart, out int occupiedTeamId))
                            {
                                // Enemy occupation is always impassable
                                if (occupiedTeamId != unit.teamId)
                                {
                                    return float.PositiveInfinity;
                                }
                                else
                                {
                                    // Friendly occupation: Allowed pass through, forbidden ending there
                                    if (toHex == currentSearchTarget) return float.PositiveInfinity;

                                    if (currentSearchTarget != null && currentSearchTarget.Unit != null)
                                    {
                                        int range = Mathf.Max(unit.GetStat("MRNG", 1), unit.GetStat("RRNG", 0));
                                        if (HexMath.Distance(toHex, currentSearchTarget) <= range)
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
            if (unit != null)
            {
                foreach (var state in toHex.States)
                {
                    if (state.StartsWith("ZoC"))
                    {
                        // Parse "ZoCT_U"
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

        public override bool OnEntry(Unit unit, HexData hex)
        {
            if (unit == null || hex == null) return true;
            
            hex.AddState($"Occupied{unit.teamId}_{unit.Id}");

            // Add Zone of Control to neighbors
            // 1. Must have Melee Range
            if (unit.GetStat("MRNG") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string unitZocState = $"ZoC{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(hex))
                    {
                        // 2. Must be reachable (Elevation)
                        float delta = Mathf.Abs(neighbor.Elevation - hex.Elevation);
                        if (delta <= maxElevationDelta)
                        {
                            neighbor.AddState(unitZocState);
                        }
                    }
                }
            }
            return true;
        }

        public override bool OnDeparture(Unit unit, HexData hex)
        {
            if (unit == null || hex == null) return true;

            hex.RemoveState($"Occupied{unit.teamId}_{unit.Id}");

            // Remove Zone of Control from neighbors
            if (unit.GetStat("MRNG") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string unitZocState = $"ZoC{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(hex))
                    {
                        neighbor.RemoveState(unitZocState);
                    }
                }
            }
            return true;
        }
    }
}
