using System.Collections.Generic;
using HexGame;
using HexGame.Units;
using NUnit.Framework;
using UnityEngine;

namespace HexGame.Tests
{
    [TestFixture]
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
        
        private GameObject hex1GO;
        private Hex hex1;
        private GameObject hex2GO;
        private Hex hex2;

        private GameObject unitGO;
        private Unit unit;
        private UnitSet unitSet;

        [SetUp]
        public void SetUp()
        {
            // 1. Setup Managers
            managerGO = new GameObject("GridVisualizationManager");
            manager = managerGO.AddComponent<GridVisualizationManager>();
            
            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = ""; // Prevent loading real data
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            gameMaster.ruleset = ruleset;

            // 2. Setup Grid
            grid = new Grid(10, 10);
            manager.Grid = grid; 
            
            // 3. Setup Hexes
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

            // 4. Setup Unit
            unitSet = ScriptableObject.CreateInstance<UnitSet>();
            unitManager.ActiveUnitSet = unitSet;

            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
        }

        [TearDown]
        public void TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(hex1GO);
            Object.DestroyImmediate(hex2GO);
            Object.DestroyImmediate(unitGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(unitSet);
        }

        private void SetupUnitWithRange(int range)
        {
            var type = new UnitType { Name = "TestUnit" };
            type.Stats = new List<UnitStatValue>
            {
                new UnitStatValue { id = "MAT", value = range > 0 ? 60 : 0 },
                new UnitStatValue { id = "RNG", value = range }
            };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(0, 1); // Team 1
        }

        [Test]
        public void OnEntry_WithMeleeRange_AddsZoC()
        {
            SetupUnitWithRange(1);
            
            // Act
            unit.SetHex(hex1);

            // Assert
            Assert.IsTrue(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "Neighbor should have Unit ZoC");
        }

        [Test]
        public void OnEntry_ZeroMeleeRange_NoZoC()
        {
            SetupUnitWithRange(0);

            // Act
            unit.SetHex(hex1);

            // Assert
            Assert.IsFalse(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "Neighbor should NOT have ZoC state");
        }

        [Test]
        public void OnEntry_HighElevationDifference_NoZoC()
        {
            SetupUnitWithRange(1);
            ruleset.maxElevationDelta = 1.0f;
            
            hex1.Data.Elevation = 0;
            hex2.Data.Elevation = 2.0f; // Too high

            // Act
            unit.SetHex(hex1);

            // Assert
            Assert.IsFalse(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "Unreachable neighbor should NOT have ZoC state");
        }

        [Test]
        public void OnDeparture_RemovesZoC()
        {
            SetupUnitWithRange(1);
            unit.SetHex(hex1);
            Assert.IsTrue(hex2.Data.States.Contains($"ZoC1_{unit.Id}"));

            // Act
            unit.SetHex(null); // Leave

            // Assert
            Assert.IsFalse(hex2.Data.States.Contains($"ZoC1_{unit.Id}"), "ZoC should be removed on departure");
        }

        [Test]
        public void GetMoveCost_EnemyOccupiedHex_ReturnsInfinity()
        {
            // Arrange
            // Simulate enemy unit with ID 999 on hex2
            hex2.Data.AddState("Occupied2_999"); // Team 2

            // Act
            float cost = ruleset.GetMoveCost(unit, hex1.Data, hex2.Data);

            // Assert
            Assert.AreEqual(float.PositiveInfinity, cost, "Movement into enemy occupied hex should be infinite cost.");
        }

        [Test]
        public void GetMoveCost_FriendlyOccupiedHex_ReturnsStandardCost()
        {
            // Arrange
            SetupUnitWithRange(1); // Team 1
            // Simulate friendly occupation on hex2
            hex2.Data.AddState("Occupied1_999"); // Team 1 (same as test unit)
            // Ruleset default costs: Plains=2, Uphill=1. Hex2 is at 0 elevation same as Hex1.
            // But verify elevation is 0
            hex1.Data.Elevation = 0;
            hex2.Data.Elevation = 0;
            ruleset.plainsCost = 2.0f;
            hex2.Data.TerrainType = TerrainType.Plains;

            // Act
            float cost = ruleset.GetMoveCost(unit, hex1.Data, hex2.Data);

            // Assert
            Assert.AreEqual(2.0f, cost, "Movement into friendly occupied hex should NOT be infinite.");
        }
    }
}
