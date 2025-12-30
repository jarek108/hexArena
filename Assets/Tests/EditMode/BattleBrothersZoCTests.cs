using System.Collections.Generic;
using HexGame;
using HexGame.Units;
using NUnit.Framework;
using UnityEngine;

namespace HexGame.Tests
{
    [TestFixture]
    public class BattleBrothersZoCTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        
        private GameObject hexStartGO;
        private Hex hexStart;
        private GameObject hexEndGO;
        private Hex hexEnd;

        private GameObject unitGO;
        private Unit unit;
        private UnitSet unitSet;

        [SetUp]
        public void SetUp()
        {
            managerGO = new GameObject("GridVisualizationManager");
            manager = managerGO.AddComponent<GridVisualizationManager>();
            
            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = ""; // Prevent loading real data
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.plainsCost = 2.0f;
            ruleset.zocPenalty = 50.0f;
            gameMaster.ruleset = ruleset;

            grid = new Grid(10, 10);
            manager.Grid = grid;

            // Start Hex (0,0)
            hexStartGO = new GameObject("HexStart");
            hexStart = hexStartGO.AddComponent<Hex>();
            HexData data1 = new HexData(0, 0);
            data1.TerrainType = TerrainType.Plains;
            hexStart.AssignData(data1);
            grid.AddHex(data1);

            // End Hex (1, -1) - Neighbor
            hexEndGO = new GameObject("HexEnd");
            hexEnd = hexEndGO.AddComponent<Hex>();
            HexData data2 = new HexData(1, -1);
            data2.TerrainType = TerrainType.Plains;
            hexEnd.AssignData(data2);
            grid.AddHex(data2);

            unitSet = ScriptableObject.CreateInstance<UnitSet>();
            unitManager.ActiveUnitSet = unitSet;

            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            
            var type = new UnitType { Name = "TestUnit" };
            type.Stats = new List<UnitStatValue> { new UnitStatValue { id = "AP", value = 100 } };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(0, 0); // Team 0
        }

        [TearDown]
        public void TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(hexStartGO);
            Object.DestroyImmediate(hexEndGO);
            Object.DestroyImmediate(unitGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(unitSet);
        }

        [Test]
        public void GetMoveCost_NoZoC_ReturnsBaseCost()
        {
            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Cost should be base plains cost.");
        }

        [Test]
        public void GetMoveCost_EnemyZoC_AppliesPenalty()
        {
            // Enemy Team 1 ZoC
            hexEnd.Data.AddState("ZoC1_999");
            
            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(52.0f, cost, "Cost should be base (2) + penalty (50).");
        }

        [Test]
        public void VerifyMove_EnemyZoC_FailsWhenLowAP()
        {
            // Arrange
            hexEnd.Data.AddState("ZoC1_999");
            unit.Stats["CAP"] = 9; // Standard AP, less than 52

            // Act
            var result = ruleset.VerifyMove(unit, hexStart.Data, hexEnd.Data);

            // Assert
            Assert.IsFalse(result.isValid, "Move should be invalid due to high ZoC cost vs low AP.");
            Assert.IsTrue(result.reason.Contains("Not enough AP"), "Reason should mention AP.");
        }

        [Test]
        public void GetMoveCost_FriendlyZoC_NoPenalty()
        {
            // Friendly Team 0 ZoC
            hexEnd.Data.AddState("ZoC0_999");

            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Friendly ZoC should not penalize movement.");
        }

        [Test]
        public void GetMoveCost_NoUnit_IgnoresZoC()
        {
            // Enemy Team 1 ZoC
            hexEnd.Data.AddState("ZoC1_999");

            // Passing null as unit (Pathfinding without unit)
            float cost = ruleset.GetPathfindingMoveCost(null, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Null unit should ignore ZoC penalty.");
        }

        [Test]
        public void GetMoveCost_MeleeAttack_HighElevation_ReturnsInfinity()
        {
            // Arrange
            // Initialize unit with melee skill and range
            var type = new UnitType { Name = "MeleeUnit" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "MAT", value = 60 },
                new UnitStatValue { id = "RNG", value = 1 } 
            };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(0, 0); // Team 0

            // Add dummy unit to target hex to trigger attack logic in ruleset
            var targetGO = new GameObject("Target");
            var targetUnit = targetGO.AddComponent<Unit>();
            targetUnit.teamId = 1; // Enemy Team 1
            hexEnd.Data.Unit = targetUnit;

            ruleset.OnStartPathfinding(hexEnd.Data, unit);
            ruleset.maxElevationDelta = 1.0f;
            hexStart.Data.Elevation = 0;
            hexEnd.Data.Elevation = 2.0f; // Too high for melee

            // Act
            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);

            // Assert
            Assert.AreEqual(float.PositiveInfinity, cost, "Melee attack should fail if elevation delta is too high.");
            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void GetMoveCost_RangedAttack_HighElevation_ReturnsZero()
        {
            // Arrange
            // Initialize unit with ranged range
            var type = new UnitType { Name = "RangedUnit" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "RAT", value = 60 },
                new UnitStatValue { id = "RNG", value = 5 } 
            };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(0, 0); // Team 0

            // Add dummy unit to target hex
            var targetGO = new GameObject("Target");
            var targetUnit = targetGO.AddComponent<Unit>();
            targetUnit.teamId = 1; // Enemy Team 1
            hexEnd.Data.Unit = targetUnit;

            ruleset.OnStartPathfinding(hexEnd.Data, unit);
            ruleset.maxElevationDelta = 1.0f;
            hexStart.Data.Elevation = 0;
            hexEnd.Data.Elevation = 2.0f; 

            // Act
            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);

            // Assert
            Assert.AreEqual(0f, cost, "Ranged attack should bypass elevation check for the final target hex.");
            Object.DestroyImmediate(targetGO);
        }
    }
}