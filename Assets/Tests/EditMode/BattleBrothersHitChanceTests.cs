using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private GameObject unitManagerGO;
        private UnitManager unitManager;
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

            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = ""; // Prevent loading real data
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, gameMaster);

            grid = new Grid(10, 10);
            manager.Grid = grid;

            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            gameMaster.ruleset = ruleset;
            
            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            var type = new UnitType { Name = "Unit" };
            unitSet.units = new List<UnitType> { type };

            attacker = CreateUnit("Attacker", 1, 60, 0); // Team 1, MSKL 60
            target = CreateUnit("Target", 2, 50, 10);   // Team 2, MDEF 10, RDEF 0
            ally = CreateUnit("Ally", 1, 50, 0);       // Team 1
        }

        [TearDown]
        public void TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            var instanceProp = typeof(GameMaster).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            instanceProp.SetValue(null, null);

            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(ruleset);
            if (attacker != null) Object.DestroyImmediate(attacker.gameObject);
            if (target != null) Object.DestroyImmediate(target.gameObject);
            if (ally != null) Object.DestroyImmediate(ally.gameObject);
        }

        private Unit CreateUnit(string name, int team, int mat, int mdf)
        {
            GameObject go = new GameObject(name);
            Unit u = go.AddComponent<Unit>();
            
            var customType = new UnitType { Name = name };
            customType.Stats = new List<UnitStatValue>
            {
                new UnitStatValue { id = "MAT", value = mat },
                new UnitStatValue { id = "MDF", value = mdf },
                new UnitStatValue { id = "RAT", value = 0 },
                new UnitStatValue { id = "RDF", value = 0 },
                new UnitStatValue { id = "RNG", value = 1 }
            };
            
            unitSet.units = new List<UnitType> { customType };
            u.Initialize(0, team);
            
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
            if (occupant != null) occupant.SetHex(h);
            return h;
        }

        private float GetTotalHitChance(Unit attacker, Unit target)
        {
            var hits = ruleset.GetPotentialHits(attacker, target);
            // Sum up probability ranges for the specific target on drawIndex 0
            return hits.Where(h => h.target == target && h.drawIndex == 0).Sum(h => h.max - h.min);
        }

        [Test]
        public void HitChance_BaseCalculation_IsCorrect()
        {
            SetupHex(0, 0, 0, attacker);
            SetupHex(1, 0, 0, target);
            Assert.AreEqual(0.5f, GetTotalHitChance(attacker, target), 0.001f);
        }

        [Test]
        public void HitChance_AttackerHighGround_AddsBonus()
        {
            ruleset.combat.meleeHighGroundBonus = 15f;
            Hex hAttacker = SetupHex(0, 0, 1.0f, null);
            Hex hTarget = SetupHex(1, 0, 0.0f, null);
            attacker.SetHex(hAttacker);
            target.SetHex(hTarget);
            Assert.AreEqual(0.65f, GetTotalHitChance(attacker, target), 0.001f);
        }

        [Test]
        public void HitChance_AttackerLowGround_AppliesPenalty()
        {
            ruleset.combat.meleeLowGroundPenalty = 20f;
            Hex hAttacker = SetupHex(0, 0, 0.0f, null);
            Hex hTarget = SetupHex(1, 0, 1.0f, null);
            attacker.SetHex(hAttacker);
            target.SetHex(hTarget);
            Assert.AreEqual(0.3f, GetTotalHitChance(attacker, target), 0.001f);
        }

        [Test]
        public void HitChance_SurroundBonus_AddsFivePerAlly()
        {
            ruleset.combat.surroundBonus = 7f;
            Hex hAttacker = SetupHex(0, 0, 0, null);
            Hex hTarget = SetupHex(1, 0, 0, null);
            Hex hAlly = SetupHex(2, -1, 0, null);
            attacker.SetHex(hAttacker);
            target.SetHex(hTarget);
            ally.SetHex(hAlly);
            Assert.AreEqual(0.57f, GetTotalHitChance(attacker, target), 0.001f);
        }

        [Test]
        public void HitChance_LongWeaponProximityPenalty_AppliesAtRangeOne()
        {
            attacker = CreateUnit("Polearm", 1, 60, 0);
            attacker.Stats["RNG"] = 2;
            ruleset.combat.longWeaponProximityPenalty = 15f;
            SetupHex(0, 0, 0, attacker);
            SetupHex(1, 0, 0, target);
            Assert.AreEqual(0.35f, GetTotalHitChance(attacker, target), 0.001f);
        }

        [Test]
        public void HitChance_RangedAlly_NoSurroundBonus()
        {
            attacker = CreateUnit("Melee", 1, 60, 0);
            Unit rangedAlly = CreateUnit("Archer", 1, 0, 0); // No MAT
            rangedAlly.Stats["RAT"] = 50;
            SetupHex(0, 0, 0, attacker);
            SetupHex(1, 0, 0, target);
            SetupHex(2, -1, 0, rangedAlly);
            Assert.AreEqual(0.5f, GetTotalHitChance(attacker, target), 0.001f);
            Object.DestroyImmediate(rangedAlly.gameObject);
        }

        [Test]
        public void HitChance_RangedBaseCalculation_IsCorrect()
        {
            Unit archer = CreateUnit("Archer", 1, 0, 0); // No MAT
            archer.Stats["RAT"] = 60; archer.Stats["RNG"] = 5;
            Unit rDefTarget = CreateUnit("Target", 2, 50, 0);
            rDefTarget.Stats["RDF"] = 10;
            SetupHex(0, 0, 0, archer);
            SetupHex(2, 0, 0, rDefTarget);
            Assert.AreEqual(0.46f, GetTotalHitChance(archer, rDefTarget), 0.001f);
            Object.DestroyImmediate(archer.gameObject);
            Object.DestroyImmediate(rDefTarget.gameObject);
        }

        [Test]
        public void HitChance_RangedHighGround_AddsBonus()
        {
            Unit archer = CreateUnit("Archer", 1, 0, 0); // No MAT
            archer.Stats["RAT"] = 60; archer.Stats["RNG"] = 5;
            Unit rDefTarget = CreateUnit("Target", 2, 50, 0);
            rDefTarget.Stats["RDF"] = 10;
            Hex hAttacker = SetupHex(0, 0, 1.0f, archer);
            Hex hTarget = SetupHex(2, 0, 0.0f, rDefTarget);
            Assert.AreEqual(0.56f, GetTotalHitChance(archer, rDefTarget), 0.001f);
            Object.DestroyImmediate(archer.gameObject);
            Object.DestroyImmediate(rDefTarget.gameObject);
        }

        [Test]
        public void HitChance_RangedCover_AppliesReduction()
        {
            Unit archer = CreateUnit("Archer", 1, 0, 0); // No MAT
            archer.Stats["RAT"] = 60; archer.Stats["RNG"] = 5;
            Unit blocker = CreateUnit("Blocker", 2, 50, 0);
            Hex hAttacker = SetupHex(0, 0, 0, archer);
            Hex hBlocker = SetupHex(2, 0, 0, blocker);
            Hex hTarget = SetupHex(3, 0, 0, target);
            ruleset.combat.coverMissChance = 0.75f;
            ruleset.combat.rangedDistancePenalty = 2f;
            Assert.AreEqual(0.135f, GetTotalHitChance(archer, target), 0.001f);
            Object.DestroyImmediate(archer.gameObject);
            Object.DestroyImmediate(blocker.gameObject);
        }

        [Test]
        public void HitChance_Clamping_Works()
        {
            Unit god = CreateUnit("God", 1, 200, 100);
            Unit peasant = CreateUnit("Peasant", 2, 10, 0);
            SetupHex(0, 0, 0, god);
            SetupHex(1, 0, 0, peasant);
            Assert.AreEqual(1.0f, GetTotalHitChance(god, peasant), 0.001f);
            Assert.AreEqual(0.0f, GetTotalHitChance(peasant, god), 0.001f);
            Object.DestroyImmediate(god.gameObject);
            Object.DestroyImmediate(peasant.gameObject);
        }
    }
}
