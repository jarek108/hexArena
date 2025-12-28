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
        public float coverMissChance = 0.75f;
        public float scatterHitPenalty = 15f;
        public float scatterDamagePenalty = 0.25f;

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

            if (dist <= mrng)
            {
                return MeleeHitChance(attacker, target, attackerHex, targetHex, dist);
            }
            else if (dist <= rrng)
            {
                float baseChance = GetBaseRangedHitChance(attacker, target, attackerHex, targetHex, dist);
                
                // Effective chance for tooltip
                if (dist >= 3)
                {
                    HexData coverHex = GetCoverHex(attackerHex, targetHex);
                    if (IsOccluding(coverHex))
                    {
                        baseChance *= (1.0f - coverMissChance);
                    }
                }
                return baseChance;
            }

            return 0f;
        }

        private HexData GetCoverHex(HexData start, HexData end)
        {
            var line = HexMath.GetLine(new Vector3Int(start.Q, start.R, start.S), new Vector3Int(end.Q, end.R, end.S));
            if (line.Count < 2) return null;
            return GridVisualizationManager.Instance?.Grid?.GetHexAt(line[line.Count - 2].x, line[line.Count - 2].y);
        }

        private bool IsOccluding(HexData hex)
        {
            if (hex == null) return false;
            return hex.Unit != null || hex.TerrainType == TerrainType.Mountains;
        }

        private void PerformAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            attacker.FacePosition(target.transform.position);

            HexData attackerHex = attacker.CurrentHex?.Data;
            HexData targetHex = target.CurrentHex?.Data;
            if (attackerHex == null || targetHex == null) return;

            int dist = HexMath.Distance(attackerHex, targetHex);

            // Handle Ranged specifically for Cover/Scatter
            if (currentAttackType == AttackType.Ranged && dist >= 3)
            {
                // Stage 1: Cover Check
                HexData coverHex = GetCoverHex(attackerHex, targetHex);
                if (IsOccluding(coverHex))
                {
                    if (Random.value < coverMissChance)
                    {
                        Debug.Log($"[Ruleset] Ranged Attack: <color=orange>BLOCKED BY COVER</color>");
                        if (coverHex.Unit != null)
                        {
                            PerformScatterShot(attacker, coverHex.Unit, "Cover");
                        }
                        else
                        {
                            // Blocked by terrain, still trigger animation
                            var viz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
                            if (viz != null) viz.OnAttack(target);
                        }
                        return; 
                    }
                }
            }

            // Stage 2: Main Roll
            float chance = currentAttackType == AttackType.Ranged ? 
                GetBaseRangedHitChance(attacker, target, attackerHex, targetHex, dist) : 
                MeleeHitChance(attacker, target, attackerHex, targetHex, dist);
            
            float roll = Random.value;
            bool isHit = roll <= chance;

            string typeStr = currentAttackType == AttackType.Ranged ? "Ranged" : "Melee";
            string outcomeStr = isHit ? "<color=green>HIT</color>" : "<color=red>MISS</color>";
            Debug.Log($"[Ruleset] {typeStr} Attack: Chance {chance:P1}, Roll {roll:P1} -> {outcomeStr}");

            var attackerViz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
            if (attackerViz != null) attackerViz.OnAttack(target);

            if (isHit)
            {
                ApplyAttackSuccess(attacker, target, 10f);
            }
            else if (currentAttackType == AttackType.Ranged && dist >= 3)
            {
                // Stage 3: Miss Scatter
                ResolveMissScatter(attacker, target, attackerHex, targetHex, dist);
            }
        }

        private void ResolveMissScatter(Unit attacker, Unit target, HexData start, HexData end, int dist)
        {
            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid == null) return;

            HexData scatterHex = null;
            if (dist == 3)
            {
                // Tile behind target
                var line = HexMath.GetLine(new Vector3Int(start.Q, start.R, start.S), new Vector3Int(end.Q, end.R, end.S));
                Vector3Int last = line[line.Count - 1];
                Vector3Int prev = line[line.Count - 2];
                Vector3Int dir = last - prev;
                Vector3Int behindPos = last + dir;
                scatterHex = grid.GetHexAt(behindPos.x, behindPos.y);
            }
            else
            {
                // Random adjacent
                var neighbors = grid.GetNeighbors(end);
                if (neighbors.Count > 0) scatterHex = neighbors[Random.Range(0, neighbors.Count)];
            }

            if (scatterHex != null && scatterHex.Unit != null)
            {
                // Elevation check: within 1 level
                if (Mathf.Abs(scatterHex.Elevation - end.Elevation) <= 1.0f)
                {
                    PerformScatterShot(attacker, scatterHex.Unit, "Miss");
                }
            }
        }

        private void PerformScatterShot(Unit attacker, Unit target, string reason)
        {
            HexData attackerHex = attacker.CurrentHex?.Data;
            HexData targetHex = target.CurrentHex?.Data;
            if (attackerHex == null || targetHex == null) return;

            int dist = HexMath.Distance(attackerHex, targetHex);
            float baseChance = GetBaseRangedHitChance(attacker, target, attackerHex, targetHex, dist);
            
            // Scatter Penalties
            float chance = Mathf.Clamp(baseChance - (scatterHitPenalty / 100f), 0f, 1f);
            float roll = Random.value;
            bool isHit = roll <= chance;

            string outcomeStr = isHit ? "<color=green>HIT</color>" : "<color=red>MISS</color>";
            Debug.Log($"[Ruleset] SCATTER ({reason}): Chance {chance:P1}, Roll {roll:P1} -> {outcomeStr}");

            if (isHit)
            {
                ApplyAttackSuccess(attacker, target, 10f * (1.0f - scatterDamagePenalty));
            }
        }

        private void ApplyAttackSuccess(Unit attacker, Unit target, float damage)
        {
            OnAttack(attacker, target);
            OnBeingAttacked(attacker, target);
            var targetViz = target.GetComponent<HexGame.Units.UnitVisualization>();
            if (targetViz != null) targetViz.OnTakeDamage((int)damage);
        }

        private float GetBaseRangedHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            int rskl = attacker.GetStat("RSKL", 30);
            int rdef = target.GetStat("RDEF", 0);
            float score = rskl - rdef;

            if (attackerHex.Elevation > targetHex.Elevation) score += rangedHighGroundBonus;
            score -= dist * rangedDistancePenalty;

            return Mathf.Clamp(score / 100f, 0f, 1f);
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

            foreach (var h in currentAoAHexes)
            {
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
    }
}