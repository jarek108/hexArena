using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Units;

namespace HexGame.Tests
{
    public class BattleBrothersHitChanceTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        
        private Unit attacker;
        private Unit target;
        private Unit ally;
        
        private UnitSet unitSet;

        [SetUp]
        public void SetUp()
        {
            managerGO = new GameObject("Manager");
            manager = managerGO.AddComponent<GridVisualizationManager>();
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, manager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            // Manually set singleton for EditMode
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, gameMaster);

            grid = new Grid(10, 10);
            manager.Grid = grid;

            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            gameMaster.ruleset = ruleset;
            
            unitSet = ScriptableObject.CreateInstance<UnitSet>();
            var type = new UnitType { Name = "Unit" };
            unitSet.units = new List<UnitType> { type };

            attacker = CreateUnit("Attacker", 1, 60, 0); // Team 1, MSKL 60
            target = CreateUnit("Target", 2, 50, 10);   // Team 2, MDEF 10, RDEF 0
            ally = CreateUnit("Ally", 1, 50, 0);       // Team 1
        }

        [TearDown]
        public void TearDown()
        {
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, null);

            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(unitSet);
            if (attacker != null) Object.DestroyImmediate(attacker.gameObject);
            if (target != null) Object.DestroyImmediate(target.gameObject);
            if (ally != null) Object.DestroyImmediate(ally.gameObject);
        }

        private Unit CreateUnit(string name, int team, int mskl, int mdef)
        {
            GameObject go = new GameObject(name);
            Unit u = go.AddComponent<Unit>();
            
            var customType = new UnitType { Name = name };
            customType.Stats = new List<UnitStatValue>
            {
                new UnitStatValue { id = "MSKL", value = mskl },
                new UnitStatValue { id = "MDEF", value = mdef },
                new UnitStatValue { id = "RSKL", value = 30 },
                new UnitStatValue { id = "RDEF", value = 0 },
                new UnitStatValue { id = "MRNG", value = 1 },
                new UnitStatValue { id = "RRNG", value = 0 }
            };
            
            UnitSet singleSet = ScriptableObject.CreateInstance<UnitSet>();
            singleSet.units = new List<UnitType> { customType };
            u.Initialize(singleSet, 0, team);
            
            return u;
        }

        private Hex SetupHex(int q, int r, float elevation, Unit occupant)
        {
            GameObject go = new GameObject($"Hex_{q}_{r}");
            Hex h = go.AddComponent<Hex>();
            HexData data = new HexData(q, r);
            data.Elevation = elevation;
            h.AssignData(data);
            grid.AddHex(data);
            
            if (occupant != null)
            {
                occupant.SetHex(h);
            }
            
            return h;
        }

        [Test]
        public void HitChance_BaseCalculation_IsCorrect()
        {
            Hex hAttacker = SetupHex(0, 0, 0, attacker);
            Hex hTarget = SetupHex(1, 0, 0, target);

            // Attacker(60 MSKL) - Target(10 MDEF) = 50%
            float chance = ruleset.HitChance(attacker, target);
            Assert.AreEqual(0.5f, chance, "Base hit chance should be (60-10)/100 = 0.5");
        }

        [Test]
        public void HitChance_AttackerHighGround_AddsBonus()
        {
            ruleset.elevationBonus = 15f;
            Hex hAttacker = SetupHex(0, 0, 1.0f, null);
            Hex hTarget = SetupHex(1, 0, 0.0f, null);
            
            attacker.SetHex(hAttacker);
            target.SetHex(hTarget);

            // (60 - 10) + 15 = 65%
            float chance = ruleset.HitChance(attacker, target);
            Assert.AreEqual(0.65f, chance, "Attacker on high ground should get custom +15 bonus");
        }

        [Test]
        public void HitChance_AttackerLowGround_AppliesPenalty()
        {
            ruleset.elevationPenalty = 20f;
            Hex hAttacker = SetupHex(0, 0, 0.0f, null);
            Hex hTarget = SetupHex(1, 0, 1.0f, null);
            
            attacker.SetHex(hAttacker);
            target.SetHex(hTarget);

            // (60 - 10) - 20 = 30%
            float chance = ruleset.HitChance(attacker, target);
            Assert.AreEqual(0.3f, chance, "Attacker on low ground should get custom -20 penalty");
        }

        [Test]
        public void HitChance_SurroundBonus_AddsFivePerAlly()
        {
            ruleset.surroundBonus = 7f;
            
            // 1. Create all hexes first
            Hex hAttacker = SetupHex(0, 0, 0, null);
            Hex hTarget = SetupHex(1, 0, 0, null);
            Hex hAlly = SetupHex(2, -1, 0, null);

            // 2. Place units so OnEntry can find neighbors
            attacker.SetHex(hAttacker);
            target.SetHex(hTarget);
            ally.SetHex(hAlly);

            // Attacker and Ally both provide ZoC to Target hex.
            // (2 - 1) * 7 = 7% bonus.
            float chance = ruleset.HitChance(attacker, target);
            Assert.AreEqual(0.57f, chance, "Two allies (including attacker) providing ZoC should give 1x bonus");
        }

        [Test]
        public void HitChance_LongWeaponProximityPenalty_AppliesAtRangeOne()
        {
            // Setup Attacker with MRNG = 2
            attacker = CreateUnit("Polearm", 1, 60, 0);
            attacker.Stats["MRNG"] = 2; // Direct modify for test

            ruleset.longWeaponProximityPenalty = 15f;
            Hex hAttacker = SetupHex(0, 0, 0, attacker);
            Hex hTarget = SetupHex(1, 0, 0, target);

            // (60 - 10) - 15 = 35%
            float chance = ruleset.HitChance(attacker, target);
            Assert.AreEqual(0.35f, chance, "Long weapon at range 1 should have proximity penalty");
        }

        [Test]
        public void HitChance_RangedAlly_NoSurroundBonus()
        {
            attacker = CreateUnit("Melee", 1, 60, 0);
            Unit rangedAlly = CreateUnit("Archer", 1, 50, 0);
            rangedAlly.Stats["MRNG"] = 0; // Ensure pure ranged

            Hex hAttacker = SetupHex(0, 0, 0, attacker);
            Hex hTarget = SetupHex(1, 0, 0, target);
            Hex hAlly = SetupHex(2, -1, 0, rangedAlly);

            // Ranged ally provides no ZoC. Only attacker provides ZoC.
            // AllyZoCCount = 1. (1 - 1) * 5 = 0.
            float chance = ruleset.HitChance(attacker, target);
            Assert.AreEqual(0.5f, chance, "Ranged allies should not contribute to surround bonus");
            
            Object.DestroyImmediate(rangedAlly.gameObject);
        }

        [Test]
        public void HitChance_RangedBaseCalculation_IsCorrect()
        {
            // Archer (RSKL 60) vs Target (RDEF 10) at Dist 2
            // Score = (60 - 10) - (2 * 2) = 50 - 4 = 46%
            Unit archer = CreateUnit("Archer", 1, 50, 0);
            archer.Stats["RSKL"] = 60;
            archer.Stats["RRNG"] = 5;
            archer.Stats["MRNG"] = 0;

            Unit rDefTarget = CreateUnit("Target", 2, 50, 0);
            rDefTarget.Stats["RDEF"] = 10;

            Hex hAttacker = SetupHex(0, 0, 0, archer);
            Hex hTarget = SetupHex(2, 0, 0, rDefTarget); // Dist 2
            
            float chance = ruleset.HitChance(archer, rDefTarget);
            Assert.AreEqual(0.46f, chance, "Ranged hit chance should be (60-10) - (2*2) = 0.46");
            
            Object.DestroyImmediate(archer.gameObject);
            Object.DestroyImmediate(rDefTarget.gameObject);
        }

        [Test]
        public void HitChance_RangedHighGround_AddsBonus()
        {
            // Archer (RSKL 60) vs Target (RDEF 10) at Dist 2, High Ground
            // Score = (60 - 10) + 10 - (2 * 2) = 50 + 10 - 4 = 56%
            Unit archer = CreateUnit("Archer", 1, 50, 0);
            archer.Stats["RSKL"] = 60;
            archer.Stats["RRNG"] = 5;
            archer.Stats["MRNG"] = 0;

            Unit rDefTarget = CreateUnit("Target", 2, 50, 0);
            rDefTarget.Stats["RDEF"] = 10;

            Hex hAttacker = SetupHex(0, 0, 1.0f, archer); // High ground
            Hex hTarget = SetupHex(2, 0, 0.0f, rDefTarget);  // Low ground
            
            float chance = ruleset.HitChance(archer, rDefTarget);
            Assert.AreEqual(0.56f, chance, "Ranged hit chance should include elevation bonus");
            
            Object.DestroyImmediate(archer.gameObject);
            Object.DestroyImmediate(rDefTarget.gameObject);
        }

        [Test]
        public void HitChance_RangedCover_AppliesReduction()
        {
            // Archer (RSKL 60) vs Target (RDEF 0) at Dist 3
            // Base Chance = 60 - (3 * 2) = 54%
            // Cover exists at Dist 2 (Adjacent to target)
            // Effective Chance = 54% * (1.0 - 0.75) = 54% * 0.25 = 13.5%
            Unit archer = CreateUnit("Archer", 1, 50, 0);
            archer.Stats["RSKL"] = 60;
            archer.Stats["RRNG"] = 5;
            archer.Stats["MRNG"] = 0;

            Unit blocker = CreateUnit("Blocker", 2, 50, 0);

            Hex hAttacker = SetupHex(0, 0, 0, archer);
            Hex hBlocker = SetupHex(2, 0, 0, blocker); // Adjacent to target
            Hex hTarget = SetupHex(3, 0, 0, target);

            ruleset.coverMissChance = 0.75f;
            ruleset.rangedDistancePenalty = 2f;

            float chance = ruleset.HitChance(archer, target);
            // 0.54 * 0.25 = 0.135
            Assert.AreEqual(0.135f, chance, 0.001f, "Cover should apply a flat 75% reduction to the base hit chance");
            
            Object.DestroyImmediate(archer.gameObject);
            Object.DestroyImmediate(blocker.gameObject);
        }

        [Test]
        public void HitChance_Clamping_Works()
        {
            Unit god = CreateUnit("God", 1, 200, 100);     // 200 MSKL, 100 MDEF
            Unit peasant = CreateUnit("Peasant", 2, 10, 0); // 10 MSKL, 0 MDEF

            Hex hGod = SetupHex(0, 0, 0, god);
            Hex hPeasant = SetupHex(1, 0, 0, peasant);

            Assert.AreEqual(200, god.GetStat("MSKL"), "God MSKL should be 200");
            Assert.AreEqual(100, god.GetStat("MDEF"), "God MDEF should be 100");
            Assert.AreEqual(10, peasant.GetStat("MSKL"), "Peasant MSKL should be 10");

            // God attacks Peasant: (200 - 0) = 200 => Clamped to 1.0
            Assert.AreEqual(1.0f, ruleset.HitChance(god, peasant), "Hit chance should be clamped to 1.0");
            
            // Peasant attacks God: (10 - 100) = -90 => Clamped to 0.0
            Assert.AreEqual(0.0f, ruleset.HitChance(peasant, god), "Hit chance should be clamped to 0.0");
            
            Object.DestroyImmediate(god.gameObject);
            Object.DestroyImmediate(peasant.gameObject);
        }
    }
}
