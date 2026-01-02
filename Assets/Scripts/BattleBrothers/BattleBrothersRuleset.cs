using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;

namespace HexGame
{
    [CreateAssetMenu(fileName = "BattleBrothersRuleset", menuName = "HexGame/BattleBrothers")]
    public class BattleBrothersRuleset : Ruleset
    {
        public MovementModule movement;
        public CombatModule combat;
        public TacticalModule tactical;
        public FlowModule flow;

        public float transitionSpeed = 5.0f;
        public float transitionPause = 0.1f;

        private Unit lastPathfindingUnit;

        public override int RoundNumber => flow != null ? flow.roundNumber : 0;
        public override Unit ActiveUnit => flow != null ? flow.activeUnit : null;
        public override List<Unit> TurnQueue => flow != null ? flow.TurnQueue : new List<Unit>();

        public override void StartCombat() => flow?.StartNewRound(this);
        public override void AdvanceTurn() => flow?.AdvanceTurn(this);
        public override void WaitTurn() => flow?.WaitCurrentTurn(this);
        public override void StopCombat() => flow?.EndCombat();

        public override void OnStartPathfinding(HexData target, Unit unit)
        {
            base.OnStartPathfinding(target, unit);
            lastPathfindingUnit = unit;
        }

        public override void OnStartPathfinding(IEnumerable<HexData> targets, Unit unit)
        {
            base.OnStartPathfinding(targets, unit);
            lastPathfindingUnit = unit;
        }

        public override void OnUnitSelected(Unit unit)
        {
            if (tactical != null) tactical.ClearAoA(unit);
            EnsureResources(unit);
        }

        public void EnsureResources(Unit unit)
        {
            if (unit == null) return;
            // No longer adding CAP/CFAT keys to dictionary.
            // Stats are initialized from UnitType.
        }

        public override void OnUnitDeselected(Unit unit)
        {
            if (tactical != null) tactical.ClearAoA(unit);
        }

        // --- Turn Flow Implementation ---

        public override int GetTurnPriority(Unit unit)
        {
            if (unit == null) return 0;
            // EffectiveINI = INI - CFAT
            return unit.GetBaseStat("INI", 100) - unit.GetStat("FAT");
        }

        public override void OnRoundStart(IEnumerable<Unit> allUnits)
        {
            foreach (var unit in allUnits)
            {
                if (unit == null) continue;
                // Standard +15 Fatigue recovery per round in Battle Brothers
                int currentFat = unit.GetStat("FAT");
                unit.SetStat("FAT", Mathf.Max(0, currentFat - 15));
            }
        }

        public override void OnTurnStart(Unit unit)
        {
            if (unit == null) return;
            // Restore AP to max at start of turn
            unit.SetStat("AP", unit.GetBaseStat("AP", 9));
        }

        // --------------------------------

        public override List<PotentialHit> GetPotentialHits(Unit attacker, Unit target, HexData fromHex = null)
        {
            if (combat == null) return new List<PotentialHit>();
            return combat.GetPotentialHits(attacker, target, this, fromHex);
        }

        public override void ExecutePath(Unit unit, List<HexData> path, Hex targetHex, System.Action onComplete = null)
        {
            if (unit == null || path == null) return;

            unit.MoveAlongPath(path, transitionSpeed, transitionPause, () => {
                if (targetHex != null && targetHex.Data.Unit != null && targetHex.Data.Unit.teamId != unit.teamId)
                {
                    int ap = unit.GetStat("AP");
                    int fat = unit.GetStat("FAT");
                    int mfat = unit.GetBaseStat("FAT", 100);
                    int attackCost = 4;

                    if ((ignoreAPs || ap >= attackCost) && (ignoreFatigue || fat < mfat))
                    {
                        PerformAttack(unit, targetHex.Data.Unit);
                        if (!ignoreAPs) unit.SetStat("AP", ap - attackCost);
                        if (!ignoreFatigue) unit.SetStat("FAT", fat + unit.GetStat("AFAT", 10));
                    }
                }
                onComplete?.Invoke();
            });
        }

        private void PerformAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null || combat == null) return;
            attacker.FacePosition(target.transform.position);
            OnAttacked(attacker, target);

            float chance = combat.GetHitChance(attacker, target, this);
            float roll = Random.value;

            if (roll <= chance)
            {
                float rawDmg = Random.Range(attacker.GetStat("DMIN", 30), attacker.GetStat("DMAX", 40) + 1);
                OnHit(attacker, target, rawDmg);
            }
            else
            {
                Debug.Log($"[Combat] {attacker.UnitName} MISSES {target.UnitName} (Roll: {roll:P1} > Chance: {chance:P1})");
            }
        }

        public override void OnAttacked(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return;
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
                float reduction = currentARM * 0.1f;
                int finalHPDmg = Mathf.Max(0, Mathf.RoundToInt(directHPDmg - reduction));
                if (armDmg > actualArmLoss) finalHPDmg += (armDmg - actualArmLoss);
                currentHP -= finalHPDmg;
            }
            else
            {
                currentHP -= Mathf.RoundToInt(damage);
            }
            
            target.SetStat("HP", currentHP);
            target.SetStat("ARM", currentARM);

            Debug.Log($"[Combat] {attacker.UnitName} HITS {target.UnitName} for {damage:F0} damage. Remaining HP: {currentHP}");

            var targetViz = target.GetComponent<HexGame.Units.UnitVisualization>();
            if (targetViz != null) targetViz.OnTakeDamage(Mathf.RoundToInt(damage));

            if (currentHP <= 0) OnDie(target);
        }

        public override void OnDie(Unit unit)
        {
            if (unit == null) return;
            if (unit.CurrentHex != null) unit.CurrentHex.Data.RemoveUnit(unit);
            if (Application.isPlaying) Destroy(unit.gameObject);
            else DestroyImmediate(unit.gameObject);
        }

        public override float GetPathfindingMoveCost(Unit unit, HexData fromHex, HexData toHex)
        {
            if (unit != null) EnsureResources(unit);
            if (movement == null) return 1.0f;
            return movement.GetMoveCost(unit, fromHex, toHex, this);
        }

        public override MoveVerification TryMoveStep(Unit unit, HexData fromHex, HexData toHex)
        {
            if (unit == null || fromHex == null || toHex == null || movement == null) 
                return MoveVerification.Failure("Invalid setup.");

            // 1. Module-based resource and constraint validation
            float cost = movement.GetMoveCost(unit, fromHex, toHex, this);
            if (float.IsInfinity(cost)) return MoveVerification.Failure("Unreachable.");

            if (!ignoreAPs)
            {
                int ap = unit.GetStat("AP");
                if (ap < cost) return MoveVerification.Failure("Not enough AP.");
            }

            if (!ignoreFatigue)
            {
                int fat = unit.GetStat("FAT");
                int mfat = unit.GetBaseStat("FAT", 100);
                if (fat + cost > mfat) return MoveVerification.Failure("Too much fatigue.");
            }

            // 2. Attack of Opportunity (AoO)
            if (combat != null)
            {
                // Simple ZoC check moved from old implementation
                bool inEnemyZoC = false;
                foreach (var state in fromHex.States)
                {
                    if (state.StartsWith("ZoC"))
                    {
                        if (int.TryParse(state.Substring(3, 1), out int teamId))
                        {
                            if (teamId != unit.teamId) { inEnemyZoC = true; break; }
                        }
                    }
                }

                if (inEnemyZoC)
                {
                    var grid = GridVisualizationManager.Instance?.Grid;
                    if (grid != null)
                    {
                        foreach (var neighbor in grid.GetNeighbors(fromHex))
                        {
                            var enemy = neighbor.Unit;
                            if (enemy != null && enemy.teamId != unit.teamId && enemy.GetStat("MAT") > 0)
                            {
                                float chance = combat.GetHitChance(enemy, unit, this);
                                if (Random.value <= chance)
                                {
                                    OnHit(enemy, unit, combat.GetDamage(enemy, unit, false));
                                    return MoveVerification.Failure("Stopped by Attack of Opportunity!");
                                }
                            }
                        }
                    }
                }
            }

            return MoveVerification.Success();
        }

        public override void PerformMove(Unit unit, HexData fromHex, HexData toHex)
        {
            if (unit == null || toHex == null || movement == null || tactical == null) return;
            EnsureResources(unit);

            if (fromHex != null)
            {
                fromHex.RemoveUnit(unit);
                unit.ClearOwnedHexStates();
            }

            float cost = fromHex != null ? movement.GetMoveCost(unit, fromHex, toHex, this) : 0f;
            if (!float.IsInfinity(cost))
            {
                if (!ignoreAPs) unit.SetStat("AP", unit.GetStat("AP") - Mathf.RoundToInt(cost));
                if (!ignoreFatigue) unit.SetStat("FAT", unit.GetStat("FAT") + Mathf.RoundToInt(cost));
            }

            toHex.AddUnit(unit);
            tactical.ProjectUnitInfluence(unit, toHex, movement.maxElevationDelta);
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
                if (h.Units.Count > 0 && !h.Units.Contains(attacker)) continue;

                // Respect elevation delta for melee attacks (RNG 1)
                if (movement != null && rng == 1)
                {
                    float delta = Mathf.Abs(h.Elevation - targetHex.Elevation);
                    if (delta > movement.maxElevationDelta) continue;
                }

                results.Add(h);
            }

            return results;
        }


        public override int GetMoveStopIndex(Unit unit, List<HexData> path)
        {
            if (path == null || path.Count == 0 || unit == null || movement == null) return 0;
            
            // Simplified Budgeting logic
            int maxReachableIndex = 0;
            if (ignoreAPs)
            {
                maxReachableIndex = path.Count - 1;
            }
            else
            {
                float runningCost = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    float stepCost = movement.GetMoveCost(unit, path[i - 1], path[i], this);
                    if (runningCost + stepCost > unit.GetStat("AP")) break;
                    runningCost += stepCost;
                    maxReachableIndex = i;
                }
            }

            // Backtrack if occupied
            for (int i = maxReachableIndex; i > 0; i--)
            {
                if (path[i].Units.Count == 0 || (path[i].Units.Count == 1 && path[i].Units[0] == unit)) 
                    return i + 1;
            }

            return 1;
        }

        public override void OnFinishPathfinding(Unit unit, List<HexData> path, bool success)
        {
            lastPathfindingUnit = unit;
            if (!success || unit == null || path == null || tactical == null)
            {
                if (tactical != null) tactical.ClearAoA(unit);
                return;
            }

            int stopIndex = GetMoveStopIndex(unit, path);
            if (stopIndex > 0) tactical.ShowAoA(unit, path[stopIndex - 1], movement.maxElevationDelta);
        }

        public override void OnClearPathfindingVisuals()
        {
            if (lastPathfindingUnit != null && tactical != null) tactical.ClearAoA(lastPathfindingUnit);
            lastPathfindingUnit = null;
        }
    }
}
