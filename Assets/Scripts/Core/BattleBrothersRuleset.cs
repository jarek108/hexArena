using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    public enum AttackType { None, Melee, Ranged }

    [CreateAssetMenu(fileName = "BattleBrothersRuleset", menuName = "HexGame/Ruleset/BattleBrothers")]
    public class BattleBrothersRuleset : Ruleset
    {
        [Header("Debug / Control")]
        public bool ignoreAPs = false;
        public bool ignoreFatigue = false;
        public bool ignoreMoveOrder = false;

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
        public float meleeHighGroundBonus = 10f;
        public float meleeLowGroundPenalty = 10f;
        public float rangedHighGroundBonus = 10f;
        public float rangedLowGroundPenalty = 10f;
        public float surroundBonus = 5f;
        public float longWeaponProximityPenalty = 15f;
        public float rangedDistancePenalty = 2f;
        public float coverMissChance = 0.75f;
        public float scatterHitPenalty = 15f;
        public float scatterDamagePenalty = 0.25f;

        public AttackType currentAttackType = AttackType.None;

        private Unit lastPathfindingUnit;
        private List<HexData> currentAoAHexes = new List<HexData>();

        public override void OnStartPathfinding(HexData target, Unit unit)
        {
            base.OnStartPathfinding(target, unit);
            lastPathfindingUnit = unit;
            
            currentAttackType = AttackType.None;
            if (target.Unit != null && unit != null)
            {
                int mat = unit.GetStat("MAT", 0);
                int rat = unit.GetStat("RAT", 0);
                currentAttackType = (rat > mat) ? AttackType.Ranged : AttackType.Melee;
            }
        }

        public override void OnStartPathfinding(IEnumerable<HexData> targets, Unit unit)
        {
            base.OnStartPathfinding(targets, unit);
            lastPathfindingUnit = unit;
        }

        public override void OnUnitSelected(Unit unit)
        {
            ClearAoA(unit);
            EnsureResources(unit);
        }

        private void EnsureResources(Unit unit)
        {
            if (unit == null) return;
            // Initialize runtime resources if missing. 
            // Default to 9 AP if stat is missing (standard BB).
            if (!unit.Stats.ContainsKey("CAP")) unit.Stats["CAP"] = unit.GetStat("AP", 9);
            if (!unit.Stats.ContainsKey("CFAT")) unit.Stats["CFAT"] = 0;
        }

        public override void OnUnitDeselected(Unit unit)
        {
            ClearAoA(unit);
        }

        public override List<PotentialHit> GetPotentialHits(Unit attacker, Unit target, HexData fromHex = null)
        {
            var results = new List<PotentialHit>();
            if (attacker == null || target == null) return results;

            HexData attackerHex = fromHex ?? attacker.CurrentHex?.Data;
            HexData targetHex = target.CurrentHex?.Data;
            if (attackerHex == null || targetHex == null) return results;

            int dist = HexMath.Distance(attackerHex, targetHex);
            int mat = attacker.GetStat("MAT", 0);
            int rat = attacker.GetStat("RAT", 0);
            int rng = attacker.GetStat("RNG", 1);

            float currentMax = 0f;

            if (mat > 0 && dist <= rng)
            {
                float chance = CalculateMeleeHitChance(attacker, target, attackerHex, targetHex, dist);
                results.Add(new PotentialHit(target, 0, chance, 0, 1f, "Melee"));
            }
            else if (rat > 0 && dist <= rng)
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

                // Bucket B: Cover Interception (Individual RDF check)
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

                // Bucket C: Miss Scatter (Stray shots with individual RDF check)
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
            if (dist >= 3)
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

        public override void ExecutePath(Unit unit, List<HexData> path, Hex targetHex, System.Action onComplete = null)
        {
            if (unit == null || path == null) return;

            unit.MoveAlongPath(path, transitionSpeed, transitionPause, () => {
                if (targetHex != null && targetHex.Data.Unit != null && targetHex.Data.Unit.teamId != unit.teamId)
                {
                    // Basic Check: Do we have resources to attack?
                    int cap = unit.GetStat("CAP");
                    int cfat = unit.GetStat("CFAT");
                    int mfat = unit.GetStat("FAT");
                    int attackCost = 4; // Placeholder constant for AP attack cost

                    bool canAffordAP = ignoreAPs || cap >= attackCost;
                    bool canAffordFatigue = ignoreFatigue || cfat < mfat;

                    if (canAffordAP && canAffordFatigue)
                    {
                        PerformAttack(unit, targetHex.Data.Unit);
                        if (!ignoreAPs) unit.Stats["CAP"] -= attackCost;
                        if (!ignoreFatigue) unit.Stats["CFAT"] += unit.GetStat("AFAT", 10);
                    }
                    else
                    {
                        string reason = !canAffordAP ? "AP" : "Fatigue";
                        Debug.Log($"[Ruleset] {unit.UnitName} too exhausted ({reason}) to attack!");
                    }
                }
                onComplete?.Invoke();
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
                    
                    float rawDmg = Random.Range(attacker.GetStat("DMIN", 30), attacker.GetStat("DMAX", 40) + 1);
                    OnHit(attacker, hit.target, rawDmg * hit.damageMultiplier);
                    
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
            int currentARM = target.GetStat("ARM", 0);
            
            float aby = attacker.GetStat("ABY", 20) / 100f;
            float adm = attacker.GetStat("ADM", 100) / 100f;

            int armDmg = Mathf.RoundToInt(damage * adm);
            int directHPDmg = Mathf.RoundToInt(damage * aby);

            if (currentARM > 0)
            {
                int actualArmLoss = Mathf.Min(currentARM, armDmg);
                currentARM -= actualArmLoss;
                
                // Armour reduction: direct damage is reduced by 10% of remaining armour
                float reduction = currentARM * 0.1f;
                int finalHPDmg = Mathf.Max(0, Mathf.RoundToInt(directHPDmg - reduction));
                
                // If armour was destroyed, any leftover armour damage goes to HP at 100%
                if (armDmg > actualArmLoss)
                {
                    finalHPDmg += (armDmg - actualArmLoss);
                }
                
                currentHP -= finalHPDmg;
                Debug.Log($"[Ruleset] {target.UnitName} hit! ARM: {currentARM + actualArmLoss}->{currentARM}, HP: {currentHP} (Direct: {finalHPDmg})");
            }
            else
            {
                // No armour: full damage to HP
                currentHP -= Mathf.RoundToInt(damage);
                Debug.Log($"[Ruleset] {target.UnitName} hit! No ARM, HP: {currentHP} (Full: {Mathf.RoundToInt(damage)})");
            }
            
            target.Stats["HP"] = currentHP;
            target.Stats["ARM"] = currentARM;

            var targetViz = target.GetComponent<HexGame.Units.UnitVisualization>();
            if (targetViz != null) targetViz.OnTakeDamage(Mathf.RoundToInt(damage));

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
                unit.CurrentHex.Data.RemoveUnit(unit);
            }

            if (Application.isPlaying) Destroy(unit.gameObject);
            else DestroyImmediate(unit.gameObject);
        }

        public override float GetPathfindingMoveCost(Unit unit, HexData fromHex, HexData toHex)
        {
            if (toHex == currentSearchTarget)
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
                                    // It's a friendly unit. 
                                    // We can path THROUGH them, but we cannot STOP on them.
                                    // 'toHex == currentSearchTarget' handles the final destination.
                                    if (toHex == currentSearchTarget) return float.PositiveInfinity;
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

        public override MoveVerification VerifyMove(Unit unit, HexData fromHex, HexData toHex)
        {
            if (unit == null || fromHex == null || toHex == null) return MoveVerification.Failure("Invalid unit or hex.");

            // 0. Move Order check
            if (!ignoreMoveOrder)
            {
                // Placeholder for future turn management logic
                // Currently, we don't have a turn manager, so we allow all moves
            }

            // 1. Elevation check
            float delta = Mathf.Abs(toHex.Elevation - fromHex.Elevation);
            if (delta > maxElevationDelta) return MoveVerification.Failure("Elevation delta too high.");

            // 2. Occupation check
            if (toHex.Units.Count > 0)
            {
                foreach (var occupant in toHex.Units)
                {
                    if (occupant == unit) continue;
                    if (occupant.teamId != unit.teamId) return MoveVerification.Failure("Hex occupied by enemy.");
                    // Allies: Allowed to pass through (VerifyMove is step-logic, not stop-logic)
                }
            }

            // 3. Resource check
            EnsureResources(unit);
            float cost = GetPathfindingMoveCost(unit, fromHex, toHex);

            if (!ignoreAPs)
            {
                int cap = unit.GetStat("CAP");
                if (cap < cost) return MoveVerification.Failure($"Not enough AP. Required: {cost}, Have: {cap}");
            }

            if (!ignoreFatigue)
            {
                int cfat = unit.GetStat("CFAT");
                int mfat = unit.GetStat("FAT", 100);
                if (cfat + cost > mfat) return MoveVerification.Failure("Too much fatigue.");
            }

            return MoveVerification.Success();
        }

        public override void PerformMove(Unit unit, HexData fromHex, HexData toHex)
        {
            if (unit == null || toHex == null) return;
            EnsureResources(unit);

            // Cleanup tactical states from the PREVIOUS position
            if (fromHex != null)
            {
                fromHex.RemoveUnit(unit);
                unit.ClearOwnedHexStates();
            }

            // Deduct Resources
            float cost = fromHex != null ? GetPathfindingMoveCost(unit, fromHex, toHex) : 0f;
            if (!float.IsInfinity(cost))
            {
                if (!ignoreAPs) unit.Stats["CAP"] -= Mathf.RoundToInt(cost);
                if (!ignoreFatigue) unit.Stats["CFAT"] += Mathf.RoundToInt(cost);
            }

            // Apply new footprint on current hex
            toHex.AddUnit(unit);
            unit.AddOwnedHexState(toHex, $"Occupied{unit.teamId}_{unit.Id}");

            // Project ZoC if unit has melee range
            if (unit.GetStat("MAT") > 0)
            {
                var grid = GridVisualizationManager.Instance?.Grid;
                if (grid != null)
                {
                    string unitZocState = $"ZoC{unit.teamId}_{unit.Id}";
                    foreach (var neighbor in grid.GetNeighbors(toHex))
                    {
                        float delta = Mathf.Abs(neighbor.Elevation - toHex.Elevation);
                        if (delta <= maxElevationDelta)
                        {
                            unit.AddOwnedHexState(neighbor, unitZocState);
                        }
                    }
                }
            }
        }

        public override List<HexData> GetValidAttackPositions(Unit attacker, Unit target)
        {
            var results = new List<HexData>();
            if (attacker == null || target == null || target.CurrentHex == null) return results;

            var grid = GridVisualizationManager.Instance?.Grid;
            if (grid == null) return results;

            int rng = attacker.GetStat("RNG", 1);
            HexData targetHex = target.CurrentHex.Data;

            var inRange = grid.GetHexesInRange(targetHex, rng);
            foreach (var h in inRange)
            {
                // Must be empty (or the attacker themselves)
                if (h.Unit != null && h.Unit != attacker) continue;

                // Respect elevation delta
                float delta = Mathf.Abs(h.Elevation - targetHex.Elevation);
                if (delta > maxElevationDelta) continue;

                results.Add(h);
            }

            return results;
        }

        public override int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            if (path == null || path.Count == 0 || unit == null) return 0;
            EnsureResources(unit);

            // Determine if we are moving to attack an enemy
            int targetAttackIndex = path.Count - 1;
            bool isAttackMove = false;
            
            // If we have multiple targets (from GetValidAttackPositions), stop at the first one we touch
            if (currentSearchTargets != null && currentSearchTargets.Count > 1)
            {
                var targetSet = new HashSet<HexData>(currentSearchTargets);
                for (int i = 0; i < path.Count; i++)
                {
                    if (targetSet.Contains(path[i]))
                    {
                        targetAttackIndex = i;
                        isAttackMove = true;
                        break;
                    }
                }
            }
            else
            {
                // Single target fallback (direct move or single attack pos)
                Unit targetEnemy = currentSearchTarget?.Unit;
                if (targetEnemy != null && targetEnemy.teamId != unit.teamId)
                {
                    isAttackMove = true;
                    int rng = unit.GetStat("RNG", 1);
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (HexMath.Distance(path[i], currentSearchTarget) <= rng)
                        {
                            targetAttackIndex = i;
                            break;
                        }
                    }
                }
            }

            // Find furthest affordable index
            int maxReachableIndex = 0;
            if (ignoreAPs)
            {
                maxReachableIndex = isAttackMove ? targetAttackIndex : path.Count - 1;
            }
            else
            {
                float runningCost = 0;
                int limit = isAttackMove ? targetAttackIndex : path.Count - 1;
                
                for (int i = 1; i <= limit; i++)
                {
                    float stepCost = GetPathfindingMoveCost(unit, path[i - 1], path[i]);
                    if (runningCost + stepCost > unit.GetStat("CAP")) break;
                    runningCost += stepCost;
                    maxReachableIndex = i;
                }
            }

            // Backtrack if the reachable hex is occupied by ANY other unit
            for (int i = maxReachableIndex; i > 0; i--)
            {
                HexData hex = path[i];
                bool occupiedByOther = false;
                foreach (var u in hex.Units)
                {
                    if (u != unit)
                    {
                        occupiedByOther = true;
                        break;
                    }
                }

                if (!occupiedByOther) return i + 1;
            }

            return 1; // Fallback: Stay at start
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

        public override void OnFinishPathfinding(Unit unit, List<HexData> path, bool success)
        {
            lastPathfindingUnit = unit;
            if (!success || unit == null || path == null)
            {
                ClearAoA(unit);
                return;
            }

            int stopIndex = GetMoveStopIndex(unit, path);
            if (stopIndex > 0)
            {
                ShowAoA(unit, path[stopIndex - 1]);
            }
        }

        private void ShowAoA(Unit unit, HexData stopHex)
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
                currentAoAHexes.Add(h);
            }
        }

        private void ClearAoA(Unit unit)
        {
            if (unit != null)
            {
                unit.RemoveOwnedHexStatesByPrefix("AoA");
            }
            currentAoAHexes.Clear();
        }

        public override void OnClearPathfindingVisuals()
        {
            if (lastPathfindingUnit != null)
            {
                ClearAoA(lastPathfindingUnit);
            }
        }

        private float GetBaseRangedHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            int rat = attacker.GetStat("RAT", 30);
            int rdf = target.GetStat("RDF", 0);
            float score = rat - rdf;

            if (attackerHex.Elevation > targetHex.Elevation) score += rangedHighGroundBonus;
            else if (attackerHex.Elevation < targetHex.Elevation) score -= rangedLowGroundPenalty;
            
            score -= dist * rangedDistancePenalty;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        private float CalculateMeleeHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            int mat = attacker.GetStat("MAT", 50);
            int mdf = target.GetStat("MDF", 0);
            float score = mat - mdf;

            if (attackerHex.Elevation > targetHex.Elevation) score += meleeHighGroundBonus;
            else if (attackerHex.Elevation < targetHex.Elevation) score -= meleeLowGroundPenalty;

            int rng = attacker.GetStat("RNG", 1);
            if (rng == 2 && dist == 1)
            {
                score -= longWeaponProximityPenalty;
            }

            int engagedAlliesCount = 0;
            string allyZoCPrefix = $"ZoC{attacker.teamId}_";
            bool attackerIsEngaged = false;
            string attackerZoCState = $"ZoC{attacker.teamId}_{attacker.Id}";

            foreach (var state in targetHex.States)
            {
                if (state.StartsWith(allyZoCPrefix))
                {
                    engagedAlliesCount++;
                    if (state == attackerZoCState)
                    {
                        attackerIsEngaged = true;
                    }
                }
            }
            
            int effectiveSurroundCount = attackerIsEngaged ? engagedAlliesCount - 1 : engagedAlliesCount;
            score += effectiveSurroundCount * surroundBonus;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        private bool IsOccluding(HexData hex)
        {
            if (hex == null) return false;
            return hex.Unit != null || hex.TerrainType == TerrainType.Mountains;
        }
    }
}