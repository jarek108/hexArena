using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;

namespace HexGame.Tests
{
    public class SurroundBonusTests
    {
        private GameObject mockRoot;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        private Unit attacker;
        private Unit ally;
        private Unit target;

        [SetUp]
        public void Setup()
        {
            mockRoot = new GameObject("MockRoot");

            // Setup GameMaster
            var gmGo = new GameObject("MockGameMaster");
            gmGo.transform.SetParent(mockRoot.transform);
            var gm = gmGo.AddComponent<GameMaster>();
            
            // Setup Ruleset
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            ruleset.combat.surroundBonus = 5f;
            ruleset.combat.meleeHighGroundBonus = 0f;
            ruleset.combat.meleeLowGroundPenalty = 0f;
            ruleset.combat.longWeaponProximityPenalty = 0f;
            gm.ruleset = ruleset;

            // Setup GridVisualizationManager
            var gvmGo = new GameObject("MockGridVisManager");
            gvmGo.transform.SetParent(mockRoot.transform);
            var manager = gvmGo.AddComponent<GridVisualizationManager>();
            
            // Setup UnitManager
            var umGo = new GameObject("MockUnitManager");
            umGo.transform.SetParent(mockRoot.transform);
            umGo.AddComponent<UnitManager>();

            // Setup Grid
            grid = new Grid(10, 10);
            manager.VisualizeGrid(grid);

            // Create Units
            attacker = CreateUnit(1, "Attacker");
            ally = CreateUnit(1, "Ally");
            target = CreateUnit(2, "Target");
        }

        [TearDown]
        public void Teardown()
        {
            if (mockRoot != null) Object.DestroyImmediate(mockRoot);
            Object.DestroyImmediate(ruleset);
            if (attacker != null) Object.DestroyImmediate(attacker.gameObject);
            if (ally != null) Object.DestroyImmediate(ally.gameObject);
            if (target != null) Object.DestroyImmediate(target.gameObject);
        }

        private Unit CreateUnit(int teamId, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(mockRoot.transform);
            var unit = go.AddComponent<Unit>();
            // unit.Id = name; // REMOVED: Id is read-only GetInstanceID()
            unit.teamId = teamId;
            unit.SetStat("MAT", 50);
            unit.SetStat("MDF", 0);
            unit.SetStat("RAT", 0);
            unit.SetStat("RDF", 0);
            unit.SetStat("RNG", 1);
            unit.SetStat("HP", 100);
            return unit;
        }

        private void PlaceUnit(Unit unit, int q, int r)
        {
            var manager = GridVisualizationManager.Instance;
            Hex hexView = manager.GetHex(q, r);
            
            if (hexView == null)
            {
                // Create a Hex GO if it doesn't exist
                GameObject hexGO = new GameObject($"Hex_{q}_{r}");
                hexGO.transform.SetParent(manager.transform);
                hexView = hexGO.AddComponent<Hex>();
                
                HexData data = grid.GetHexAt(q, r);
                if (data == null)
                {
                    data = new HexData(q, r);
                    grid.AddHex(data);
                }
                hexView.AssignData(data);
            }

            unit.SetHex(hexView);
        }

        private void SetUnitStats(Unit unit, int mat, int rng)
        {
            unit.SetStat("MAT", mat);
            unit.SetStat("RNG", rng);
            unit.SetStat("MDF", 0);
            unit.SetStat("RAT", 0);
            unit.SetStat("RDF", 0);
            unit.SetStat("HP", 100);
        }

        [Test]
        public void SurroundBonus_Applies_When_Attacker_Is_Not_Adjacent()
        {
            // Scenario:
            // Target at (0, 2)
            // Ally at (0, 1) -> Adjacent to Target
            // Attacker at (0, 0) -> Range 2 from Target
            
            // 1. Place Units
            PlaceUnit(target, 0, 2);
            PlaceUnit(ally, 0, 1);
            PlaceUnit(attacker, 0, 0);

            // 2. Set Stats AFTER placement (important!)
            SetUnitStats(attacker, 50, 2); // Reach weapon
            SetUnitStats(ally, 50, 1);
            SetUnitStats(target, 50, 1);
            
            // Verify Ally ZoC is on Target
            HexData targetHex = grid.GetHexAt(0, 2);
            bool hasAllyZoC = false;
            foreach(var state in targetHex.States) if(state.Contains($"ZoC{ally.teamId}_{ally.Id}")) hasAllyZoC = true;
            Assert.IsTrue(hasAllyZoC, $"Ally (ID: {ally.Id}) should project ZoC on target. States: {string.Join(", ", targetHex.States)}");

            // Verify Attacker does NOT project ZoC on Target (too far)
            bool hasAttackerZoC = false;
            foreach(var state in targetHex.States) if(state.Contains($"ZoC{attacker.teamId}_{attacker.Id}")) hasAttackerZoC = true;
            Assert.IsFalse(hasAttackerZoC, "Attacker at range 2 should NOT project ZoC on target.");

            // 4. Calculate Hit Chance
            // Base = 50 (MAT) - 0 (MDF) = 0.50
            // Surround = 1 Ally * 5% = +0.05
            // Expected = 0.55
            
            var hits = ruleset.GetPotentialHits(attacker, target);
            Assert.IsNotEmpty(hits, "GetPotentialHits should not be empty for a valid melee attack.");
            
            var hit = hits[0];
            Debug.Log($"[Test] Hit Result: Target={hit.target.UnitName}, Min={hit.min}, Max={hit.max}, Draw={hit.drawIndex}, Info={hit.logInfo}");

            // Check .max (hit chance)
            float expected = 0.55f;
            Assert.AreEqual(expected, hit.max, 0.001f, $"Expected hit chance {expected} but got {hit.max}. Info: {hit.logInfo}");
        }

        [Test]
        public void SurroundBonus_Applies_Standard_2v1()
        {
            // Scenario: Standard 2v1 flank
            // Target (0, 1)
            // Attacker (0, 0)
            // Ally (0, 2)
            
            PlaceUnit(target, 0, 1);
            PlaceUnit(attacker, 0, 0);
            PlaceUnit(ally, 0, 2);

            SetUnitStats(attacker, 50, 1);
            SetUnitStats(ally, 50, 1);
            SetUnitStats(target, 50, 1);

            // Hit Chance:
            // Base 0.50
            // Allies on target: Attacker + Ally = 2 ZoCs.
            // Bonus: (2 - 1 self) = 1 * 5% = +0.05
            // Total: 0.55

            var hits = ruleset.GetPotentialHits(attacker, target);
            Assert.IsNotEmpty(hits, "GetPotentialHits should not be empty for a valid melee attack.");
            
            var hit = hits[0];
            Debug.Log($"[Test] 2v1 Hit Result: Target={hit.target.UnitName}, Min={hit.min}, Max={hit.max}, Draw={hit.drawIndex}, Info={hit.logInfo}");

            Assert.AreEqual(0.55f, hit.max, 0.001f);
        }
    }
}