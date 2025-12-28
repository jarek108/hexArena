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

        public override List<PotentialHit> GetPotentialHits(Unit attacker, Unit target)
        {
            var results = new List<PotentialHit>();
            if (attacker == null || target == null) return results;

            HexData attackerHex = attacker.CurrentHex?.Data;
            HexData targetHex = target.CurrentHex?.Data;
            if (attackerHex == null || targetHex == null) return results;

            int dist = HexMath.Distance(attackerHex, targetHex);
            int mrng = attacker.GetStat("MRNG", 1);
            int rrng = attacker.GetStat("RRNG", 0);

            if (dist <= mrng)
            {
                float chance = MeleeHitChance(attacker, target, attackerHex, targetHex, dist);
                results.Add(new PotentialHit(target, 0, chance, 0, 1f, "Melee"));
            }
            else if (dist <= rrng)
            {
                float baseChance = GetBaseRangedHitChance(attacker, target, attackerHex, targetHex, dist);
                float scatterChance = Mathf.Clamp(baseChance - (scatterHitPenalty / 100f), 0f, 1f);

                // 1. Cover Logic
                HexData coverHex = (dist >= 3) ? GetCoverHex(attackerHex, targetHex) : null;
                bool hasCover = IsOccluding(coverHex);

                if (hasCover)
                {
                    float bypassFactor = (1.0f - coverMissChance);
                    results.Add(new PotentialHit(target, 0, baseChance * bypassFactor, 0, 1f, "Ranged (Target in Cover)"));
                    
                    if (coverHex.Unit != null)
                    {
                        results.Add(new PotentialHit(coverHex.Unit, bypassFactor, bypassFactor + (scatterChance * coverMissChance), 0, 1f - scatterDamagePenalty, "Scatter (Cover Interception)"));
                    }
                }
                else
                {
                    results.Add(new PotentialHit(target, 0, baseChance, 0, 1f, "Ranged"));
                }

                // 2. Miss Scatter (Stray Shot)
                if (dist >= 3)
                {
                    Unit strayTarget = GetStrayTarget(attacker, target, attackerHex, targetHex, dist);
                    if (strayTarget != null)
                    {
                        results.Add(new PotentialHit(strayTarget, 0, scatterChance, 1, 1f - scatterDamagePenalty, "Scatter (Stray Shot)"));
                    }
                }
            }

            return results;
        }

        private Unit GetStrayTarget(Unit attacker, Unit target, HexData start, HexData end, int dist)
        {
            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid == null) return null;

            HexData scatterHex = null;
            if (dist == 3)
            {
                var line = HexMath.GetLine(new Vector3Int(start.Q, start.R, start.S), new Vector3Int(end.Q, end.R, end.S));
                Vector3Int last = line[line.Count - 1];
                Vector3Int prev = line[line.Count - 2];
                Vector3Int dir = last - prev;
                Vector3Int behindPos = last + dir;
                scatterHex = grid.GetHexAt(behindPos.x, behindPos.y);
            }
            else
            {
                var neighbors = grid.GetNeighbors(end);
                if (neighbors.Count > 0)
                {
                    foreach (var n in neighbors)
                    {
                        if (n.Unit != null && Mathf.Abs(n.Elevation - end.Elevation) <= 1.0f) return n.Unit;
                    }
                }
            }

            if (scatterHex != null && scatterHex.Unit != null)
            {
                if (Mathf.Abs(scatterHex.Elevation - end.Elevation) <= 1.0f) return scatterHex.Unit;
            }
            return null;
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

        public override void ExecutePath(Unit unit, List<HexData> path, Hex targetHex)
        {
            if (unit == null || path == null) return;

            unit.MoveAlongPath(path, transitionSpeed, transitionPause, () => {
                if (targetHex != null && targetHex.Unit != null && targetHex.Unit.teamId != unit.teamId)
                {
                    PerformAttack(unit, targetHex.Unit);
                }
            });
        }

        private void PerformAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            attacker.FacePosition(target.transform.position);

            var hits = GetPotentialHits(attacker, target);
            if (hits.Count == 0) return;

            var drawIndices = new HashSet<int>();
            foreach (var h in hits) drawIndices.Add(h.drawIndex);

            var rolls = new Dictionary<int, float>();
            foreach (int idx in drawIndices) rolls[idx] = Random.value;

            bool primaryHitResolved = false;
            foreach (var hit in hits)
            {
                float roll = rolls[hit.drawIndex];
                if (roll >= hit.min && roll < hit.max)
                {
                    if (hit.drawIndex == 1 && primaryHitResolved) continue;

                    string outcome = "<color=green>HIT</color>";
                    Debug.Log($"[Ruleset] {hit.logInfo}: Chance [{hit.min:P0}-{hit.max:P0}], Roll {roll:P1} -> {outcome}");
                    
                    ApplyAttackSuccess(attacker, hit.target, 10f * hit.damageMultiplier);
                    
                    if (hit.drawIndex == 0) primaryHitResolved = true;
                }
            }

            var attackerViz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
            if (attackerViz != null) attackerViz.OnAttack(target);
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
            int mskl = attacker.GetStat("MSKL", 50);
            int mdef = target.GetStat("MDEF", 0);
            float score = mskl - mdef;

            if (attackerHex.Elevation > targetHex.Elevation) score += elevationBonus;
            else if (attackerHex.Elevation < targetHex.Elevation) score -= elevationPenalty;

            int mrng = attacker.GetStat("MRNG", 1);
            if (mrng == 2 && dist == 1) score -= longWeaponProximityPenalty;

            int allyZoCCount = 0;
            string allyZoCPrefix = $"ZoC{attacker.teamId}_";
            foreach (var state in targetHex.States)
            {
                if (state.StartsWith(allyZoCPrefix)) allyZoCCount++;
            }
            score += Mathf.Max(0, allyZoCCount - 1) * surroundBonus;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        public override float GetMoveCost(Unit unit, HexData fromHex, HexData toHex)
        {
            if (toHex == currentSearchTarget)
            {
                bool isEnemy = toHex.Unit != null && unit != null && toHex.Unit.teamId != unit.teamId;
                if (isEnemy)
                {
                    if (currentAttackType == AttackType.Melee)
                    {
                        float attackDelta = Mathf.Abs(toHex.Elevation - fromHex.Elevation);
                        if (attackDelta > maxElevationDelta) return float.PositiveInfinity;
                    }
                    return 0f;
                }
            }

            float delta = Mathf.Abs(toHex.Elevation - fromHex.Elevation);
            if (delta > maxElevationDelta) return float.PositiveInfinity;

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

            float cost = GetTerrainCost(toHex.TerrainType);
            if (toHex.Elevation > fromHex.Elevation) cost += uphillPenalty;

            if (unit != null)
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

        public override void OnAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            Debug.Log($"[Ruleset] {attacker.UnitName} attacks {target.UnitName}");
        }

        public override void OnBeingAttacked(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
        }

        public override int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            if (path == null || path.Count == 0 || unit == null) return 0;
            HexData lastHex = path[path.Count - 1];
            if (lastHex.Unit != null && lastHex.Unit.teamId != unit.teamId)
            {
                int mrng = unit.GetStat("MRNG", 1);
                int rrng = unit.GetStat("RRNG", 0);
                int range = Mathf.Max(mrng, rrng);
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
            if (unit.GetStat("MRNG") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string unitZocState = $"ZoC{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(hex))
                    {
                        float delta = Mathf.Abs(neighbor.Elevation - hex.Elevation);
                        if (delta <= maxElevationDelta) neighbor.AddState(unitZocState);
                    }
                }
            }
            return true;
        }

        public override bool OnDeparture(Unit unit, HexData hex)
        {
            if (unit == null || hex == null) return true;
            hex.RemoveState($"Occupied{unit.teamId}_{unit.Id}");
            if (unit.GetStat("MRNG") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string unitZocState = $"ZoC{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(hex)) neighbor.RemoveState(unitZocState);
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
                        ShowAoA(unit, ghostHex);
                    }
                }
                else pathGhost.gameObject.SetActive(false);
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
                if (isMelee && Mathf.Abs(h.Elevation - stopHex.Elevation) > maxElevationDelta) continue;
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
                foreach (var s in h.States) if (s.StartsWith("AoA")) toRemove.Add(s);
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
                if (r.sharedMaterial.HasProperty("_BaseColor")) color = r.sharedMaterial.GetColor("_BaseColor");
                color.a = 0.5f; 
                mpb.SetColor("_BaseColor", color);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}