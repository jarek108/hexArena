using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;
using HexGame.Units;

namespace HexGame.Tests
{
    public class BattleBrothersRulesetTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        private UnitSet unitSet;

        private GameObject hex1GO;
        private Hex hex1;
        private GameObject hex2GO;
        private Hex hex2;
        private GameObject unitGO;
        private Unit unit;

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

            // Setup Hexes
            hex1GO = new GameObject("Hex1");
            hex1 = hex1GO.AddComponent<Hex>();
            HexData data1 = new HexData(0, 0);
            hex1.AssignData(data1);
            grid.AddHex(data1);

            hex2GO = new GameObject("Hex2");
            hex2 = hex2GO.AddComponent<Hex>();
            HexData data2 = new HexData(1, -1);
            hex2.AssignData(data2);
            grid.AddHex(data2);

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;

            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            unit.Stats["HP"] = 100;
            unit.Stats["CAP"] = 100;
            unit.Stats["MFAT"] = 100;
            unit.Stats["CFAT"] = 0;
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
            Object.DestroyImmediate(hex1GO);
            Object.DestroyImmediate(hex2GO);
            Object.DestroyImmediate(unitGO);
            Object.DestroyImmediate(ruleset);
        }

        private void SetupUnitWithRange(int range)
        {
            var type = new UnitType { id = "test", Name = "TestUnit" };
            type.Stats = new List<UnitStatValue>
            {
                new UnitStatValue { id = "MAT", value = range > 0 ? 60 : 0 },
                new UnitStatValue { id = "RNG", value = range }
            };
            unitSet.units.Add(type);
            unit.Initialize("test", 1); // Team 1
        }

        [Test]
        public void GetMoveCost_ValidMove_ReturnsCorrectCost()
        {
            SetupUnitWithRange(1);
            unit.SetHex(hex1);
            ruleset.movement.plainsCost = 2.0f;
            float cost = ruleset.GetPathfindingMoveCost(unit, hex1.Data, hex2.Data);
            Assert.AreEqual(2.0f, cost);
        }

        [Test]
        public void OnEntry_WithMeleeRange_AddsZoC()
        {
            SetupUnitWithRange(1);
            unit.SetHex(hex1);
            Assert.IsTrue(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "Neighbor should have Unit ZoC");
        }

        [Test]
        public void OnEntry_ZeroMeleeRange_NoZoC()
        {
            SetupUnitWithRange(0);
            unit.SetHex(hex1);
            Assert.IsFalse(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "Neighbor should NOT have ZoC state");
        }

        [Test]
        public void OnEntry_HighElevationDifference_NoZoC()
        {
            SetupUnitWithRange(1);
            ruleset.movement.maxElevationDelta = 1.0f;
            hex1.Data.Elevation = 0;
            hex2.Data.Elevation = 2.0f; // Too high
            unit.SetHex(hex1);
            Assert.IsFalse(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "Unreachable neighbor should NOT have ZoC state");
        }

        [Test]
        public void OnDeparture_RemovesZoC()
        {
            SetupUnitWithRange(1);
            unit.SetHex(hex1);
            Assert.IsTrue(hex2.Data.States.Contains($"ZoC1_{unit.Id}"));
            unit.SetHex(null); // Leave
            Assert.IsFalse(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "ZoC should be removed on departure");
        }

        [Test]
        public void GetMoveCost_EnemyOccupiedHex_ReturnsInfinity()
        {
            SetupUnitWithRange(1);
            unit.SetHex(hex1);
            hex2.Data.AddState("Occupied2_999"); // Team 2
            float cost = ruleset.GetPathfindingMoveCost(unit, hex1.Data, hex2.Data);
            Assert.AreEqual(float.PositiveInfinity, cost, "Movement into enemy occupied hex should be infinite cost.");
        }

        [Test]
        public void GetMoveCost_FriendlyOccupiedHex_ReturnsStandardCost()
        {
            SetupUnitWithRange(1); // Team 1
            unit.SetHex(hex1);
            hex2.Data.AddState("Occupied1_999"); // Team 1
            hex1.Data.Elevation = 0;
            hex2.Data.Elevation = 0;
            ruleset.movement.plainsCost = 2.0f;
            hex2.Data.TerrainType = TerrainType.Plains;
            float cost = ruleset.GetPathfindingMoveCost(unit, hex1.Data, hex2.Data);
            Assert.AreEqual(2.0f, cost, "Movement into friendly occupied hex should NOT be infinite.");
        }

        [Test]
        public void VerifyMove_Respects_IgnoreAPs()
        {
            ruleset.ignoreAPs = true;
            ruleset.movement.plainsCost = 10f;
            unit.Stats["CAP"] = 0;
            var result = ruleset.TryMoveStep(unit, hex1.Data, hex2.Data);
            Assert.IsTrue(result.isValid, "Move should be valid because ignoreAPs is true.");
        }

        [Test]
        public void PerformMove_Respects_IgnoreAPs()
        {
            ruleset.ignoreAPs = true;
            ruleset.movement.plainsCost = 5f;
            unit.Stats["CAP"] = 10;
            ruleset.PerformMove(unit, hex1.Data, hex2.Data);
            Assert.AreEqual(10, unit.Stats["CAP"], "AP should NOT be deducted because ignoreAPs is true.");
        }

        [Test]
        public void VerifyMove_Respects_IgnoreFatigue()
        {
            ruleset.ignoreAPs = true;
            ruleset.ignoreFatigue = true;
            ruleset.movement.plainsCost = 10f;
            unit.Stats["CFAT"] = 100;
            unit.Stats["FAT"] = 100;
            var result = ruleset.TryMoveStep(unit, hex1.Data, hex2.Data);
            Assert.IsTrue(result.isValid, "Move should be valid because ignoreFatigue is true.");
        }

        [Test]
        public void PerformMove_Respects_IgnoreFatigue()
        {
            ruleset.ignoreFatigue = true;
            ruleset.movement.plainsCost = 5f;
            unit.Stats["CFAT"] = 0;
            ruleset.PerformMove(unit, hex1.Data, hex2.Data);
            Assert.AreEqual(0, unit.Stats["CFAT"], "Fatigue should NOT be added because ignoreFatigue is true.");
        }

        [Test]
        public void GetMoveStopIndex_Respects_IgnoreAPs()
        {
            ruleset.ignoreAPs = true;
            ruleset.movement.plainsCost = 10f;
            unit.Stats["CAP"] = 0;
            List<HexData> path = new List<HexData> { hex1.Data, hex2.Data };
            int stopIndex = ruleset.GetMoveStopIndex(unit, path);
            Assert.AreEqual(2, stopIndex, "Should NOT truncate path because ignoreAPs is true.");
        }
    }
}