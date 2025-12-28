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

        public override List<PotentialHit> GetPotentialHits(Unit attacker, Unit target, HexData fromHex = null)
        {
            var results = new List<PotentialHit>();
            if (attacker == null || target == null) return results;

            HexData attackerHex = fromHex ?? attacker.CurrentHex?.Data;
            HexData targetHex = target.CurrentHex?.Data;
            if (attackerHex == null || targetHex == null) return results;

            int dist = HexMath.Distance(attackerHex, targetHex);
            int mrng = attacker.GetStat("MRNG", 1);
            int rrng = attacker.GetStat("RRNG", 0);

            float currentMax = 0f;

            if (dist <= mrng)
            {
                float chance = CalculateMeleeHitChance(attacker, target, attackerHex, targetHex, dist);
                results.Add(new PotentialHit(target, 0, chance, 0, 1f, "Melee"));
            }
            else if (dist <= rrng)
            {
                // 1. Analyze Surroundings
                List<Unit> covers, strays;
                AnalyzeRangedEnvironment(attackerHex, targetHex, dist, out covers, out strays);
                bool hasCover = covers.Count > 0 && dist >= 3;

                // 2. Calculate Individual Hit Chances
                float primaryBase = GetBaseRangedHitChance(attacker, target, attackerHex, targetHex, dist);
                float scatterPenaltyVal = scatterHitPenalty / 100f;

                // Bucket A: Primary Target (Reduced by cover interception chance)
                float primaryWidth = primaryBase * (hasCover ? (1.0f - coverMissChance) : 1.0f);
                results.Add(new PotentialHit(target, 0, primaryWidth, 0, 1f, "Target"));
                currentMax = primaryWidth;

                // Bucket B: Cover Interception (Individual RDEF check)
                if (hasCover)
                {
                    foreach (var c in covers)
                    {
                        float coverBase = GetBaseRangedHitChance(attacker, c, attackerHex, c.CurrentHex.Data, dist);
                        float coverHitChance = Mathf.Clamp(coverBase - scatterPenaltyVal, 0f, 1f);
                        float coverWidth = (coverHitChance * coverMissChance) / covers.Count;
                        
                        results.Add(new PotentialHit(c, currentMax, currentMax + coverWidth, 0, 1f - scatterDamagePenalty, "Cover"));
                        currentMax += coverWidth;
                    }
                }

                // Bucket C: Miss Scatter (Stray shots with individual RDEF check)
                if (dist >= 3 && strays.Count > 0)
                {
                    float missChance = 1.0f - primaryWidth - (hasCover ? (1.0f * coverMissChance) : 0f);
                    missChance = Mathf.Max(0, missChance);

                    foreach (var s in strays)
                    {
                        float strayBase = GetBaseRangedHitChance(attacker, s, attackerHex, s.CurrentHex.Data, dist);
                        float strayHitChance = Mathf.Clamp(strayBase - scatterPenaltyVal, 0f, 1f);
                        float strayWidth = (strayHitChance * missChance) / strays.Count;

                        results.Add(new PotentialHit(s, currentMax, currentMax + strayWidth, 0, 1f - scatterDamagePenalty, "Stray"));
                        currentMax += strayWidth;
                    }
                }
            }

            return results;
        }

        private void AnalyzeRangedEnvironment(HexData attackerHex, HexData targetHex, int dist, out List<Unit> covers, out List<Unit> strays)
        {
            covers = new List<Unit>();
            strays = new List<Unit>();
            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid == null) return;

            var neighbors = grid.GetNeighbors(targetHex);
            HexData hexBehind = null;
            if (dist == 3)
            {
                var line = HexMath.GetLine(new Vector3Int(attackerHex.Q, attackerHex.R, attackerHex.S), new Vector3Int(targetHex.Q, targetHex.R, targetHex.S));
                if (line.Count >= 2)
                {
                    Vector3Int dir = line[line.Count - 1] - line[line.Count - 2];
                    Vector3Int behindPos = line[line.Count - 1] + dir;
                    hexBehind = grid.GetHexAt(behindPos.x, behindPos.y);
                }
            }

            foreach (var n in neighbors)
            {
                if (n.Unit == null || Mathf.Abs(n.Elevation - targetHex.Elevation) > 1.0f) continue;
                int d = HexMath.Distance(attackerHex, n);
                if (d < dist) covers.Add(n.Unit);
                else if (dist == 3 ? n == hexBehind : dist >= 4) strays.Add(n.Unit);
            }
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

            OnAttacked(attacker, target); // Trigger attack event

            var hits = GetPotentialHits(attacker, target);
            if (hits.Count == 0) return;

            float roll = Random.value;
            bool hitResolved = false;

            foreach (var hit in hits)
            {
                if (roll >= hit.min && roll < hit.max)
                {
                    Debug.Log($"[Ruleset] {hit.logInfo} HIT: Chance {hit.min:P0}-{hit.max:P0}, Roll {roll:P1}");
                    OnHit(attacker, hit.target, 10f * hit.damageMultiplier);
                    hitResolved = true;
                    break;
                }
            }

            if (!hitResolved)
            {
                Debug.Log($"[Ruleset] MISS: Roll {roll:P1} fell outside all potential hit ranges.");
            }
        }

        public override void OnAttacked(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
            Debug.Log($"[Ruleset] {attacker.UnitName} attacks {target.UnitName}");
            
            var attackerViz = attacker.GetComponent<HexGame.Units.UnitVisualization>();
            if (attackerViz != null) attackerViz.OnAttack(target);
        }

        public override void OnHit(Unit attacker, Unit target, float damage)
        {
            if (target == null) return;
            
            int currentHP = target.GetStat("HP");
            int dmgInt = Mathf.RoundToInt(damage);
            currentHP -= dmgInt;
            target.Stats["HP"] = currentHP;
            
            Debug.Log($"[Ruleset] {target.UnitName} took {dmgInt} damage. HP: {currentHP}");

            var targetViz = target.GetComponent<HexGame.Units.UnitVisualization>();
            if (targetViz != null) targetViz.OnTakeDamage(dmgInt);

            if (currentHP <= 0)
            {
                OnDie(target);
            }
        }

        public override void OnDie(Unit unit)
        {
            if (unit == null) return;
            Debug.Log($"[Ruleset] {unit.UnitName} HAS DIED!");
            
            if (unit.CurrentHex != null)
            {
                // Ensure logic cleanup, though simple destruction might suffice for now
                unit.CurrentHex.Data.Unit = null;
                OnDeparture(unit, unit.CurrentHex.Data);
            }

            if (Application.isPlaying) Destroy(unit.gameObject);
            else DestroyImmediate(unit.gameObject);
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
                                if (occupiedTeamId != unit.teamId)
                                {
                                    return float.PositiveInfinity;
                                }
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

        public override int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            if (path == null || path.Count == 0 || unit == null) return 0;
            HexData lastHex = path[path.Count - 1];
            
            if (lastHex.Unit != null && lastHex.Unit.teamId != unit.teamId)
            {
                int mrng = unit.GetStat("MRNG", 1);
                int rrng = unit.GetStat("RRNG", 0);
                int range = Mathf.Max(mrng, rrng);

                for (int i = 0; i < path.Count; i++)
                {
                    if (HexMath.Distance(path[i], lastHex) <= range)
                    {
                        return i + 1;
                    }
                }
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

        private float GetBaseRangedHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            int rskl = attacker.GetStat("RSKL", 30);
            int rdef = target.GetStat("RDEF", 0);
            float score = rskl - rdef;

            if (attackerHex.Elevation > targetHex.Elevation) score += rangedHighGroundBonus;
            score -= dist * rangedDistancePenalty;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        private float CalculateMeleeHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            int mskl = attacker.GetStat("MSKL", 50);
            int mdef = target.GetStat("MDEF", 0);
            float score = mskl - mdef;

            if (attackerHex.Elevation > targetHex.Elevation) score += elevationBonus;
            else if (attackerHex.Elevation < targetHex.Elevation) score -= elevationPenalty;

            int mrng = attacker.GetStat("MRNG", 1);
            if (mrng == 2 && dist == 1)
            {
                score -= longWeaponProximityPenalty;
            }

            int allyZoCCount = 0;
            string allyZoCPrefix = $"ZoC{attacker.teamId}_";
            foreach (var state in targetHex.States)
            {
                if (state.StartsWith(allyZoCPrefix))
                {
                    allyZoCCount++;
                }
            }
            
            score += Mathf.Max(0, allyZoCCount - 1) * surroundBonus;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        private bool IsOccluding(HexData hex)
        {
            if (hex == null) return false;
            return hex.Unit != null || hex.TerrainType == TerrainType.Mountains;
        }
    }
}
