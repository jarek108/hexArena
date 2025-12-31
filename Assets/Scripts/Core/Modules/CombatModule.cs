using UnityEngine;
using System.Collections.Generic;

namespace HexGame
{
    [CreateAssetMenu(fileName = "CombatModule", menuName = "HexGame/Ruleset/Modules/Combat")]
    public class CombatModule : ScriptableObject
    {
        [Header("Melee Modifiers")]
        public float meleeHighGroundBonus = 10f;
        public float meleeLowGroundPenalty = 10f;
        public float surroundBonus = 5f;
        public float longWeaponProximityPenalty = 15f;

        [Header("Ranged Modifiers")]
        public float rangedHighGroundBonus = 10f;
        public float rangedLowGroundPenalty = 10f;
        public float rangedDistancePenalty = 2f;
        public float coverMissChance = 0.75f;
        public float scatterHitPenalty = 15f;
        public float scatterDamagePenalty = 0.25f;

        public float GetHitChance(Unit attacker, Unit target, BattleBrothersRuleset ruleset)
        {
            if (attacker == null || target == null || attacker.CurrentHex == null || target.CurrentHex == null) return 0f;
            int dist = HexMath.Distance(attacker.CurrentHex.Data, target.CurrentHex.Data);
            
            int mat = attacker.GetStat("MAT", 0);
            int rat = attacker.GetStat("RAT", 0);
            
            if (rat > mat) 
                return GetBaseRangedHitChance(attacker, target, attacker.CurrentHex.Data, target.CurrentHex.Data, dist);
            else 
                return CalculateMeleeHitChance(attacker, target, attacker.CurrentHex.Data, target.CurrentHex.Data, dist);
        }

        public List<PotentialHit> GetPotentialHits(Unit attacker, Unit target, BattleBrothersRuleset ruleset, HexData fromHex = null)
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

                // Bucket B: Cover Interception
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

                // Bucket C: Miss Scatter
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

        public float CalculateMeleeHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
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

        public float GetBaseRangedHitChance(Unit attacker, Unit target, HexData attackerHex, HexData targetHex, int dist)
        {
            int rat = attacker.GetStat("RAT", 30);
            int rdf = target.GetStat("RDF", 0);
            float score = rat - rdf;

            if (attackerHex.Elevation > targetHex.Elevation) score += rangedHighGroundBonus;
            else if (attackerHex.Elevation < targetHex.Elevation) score -= rangedLowGroundPenalty;
            
            score -= dist * rangedDistancePenalty;

            return Mathf.Clamp(score / 100f, 0f, 1f);
        }

        public float GetDamage(Unit attacker, Unit target, bool isHeadshot)
        {
            int dmin = attacker.GetStat("DMIN", 30);
            int dmax = attacker.GetStat("DMAX", 40);
            return Random.Range(dmin, dmax + 1);
        }
    }
}