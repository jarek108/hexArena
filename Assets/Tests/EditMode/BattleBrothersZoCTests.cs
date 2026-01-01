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
            ruleset.movement = ScriptableObject.CreateInstance<MovementModule>();
            ruleset.combat = ScriptableObject.CreateInstance<CombatModule>();
            ruleset.tactical = ScriptableObject.CreateInstance<TacticalModule>();
            ruleset.movement.plainsCost = 2.0f;
            ruleset.movement.zocPenalty = 50.0f;
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

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            unit.Stats["HP"] = 100;
            unit.Stats["CAP"] = 100;
            unit.Stats["MFAT"] = 100;
            unit.Stats["CFAT"] = 0;
            
            var type = new UnitType { id = "test", Name = "TestUnit" };
            type.Stats = new List<UnitStatValue> { new UnitStatValue { id = "AP", value = 100 } };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize("test", 0); // Team 0
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
            var result = ruleset.TryMoveStep(unit, hexStart.Data, hexEnd.Data);

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
            var type = new UnitType { id = "melee", Name = "MeleeUnit" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "MAT", value = 60 },
                new UnitStatValue { id = "RNG", value = 1 } 
            };
            unitSet.units.Add(type);
            unit.Initialize("melee", 0); // Team 0

            // Add dummy unit to target hex to trigger attack logic in ruleset
            var targetGO = new GameObject("Target");
            var targetUnit = targetGO.AddComponent<Unit>();
            targetUnit.teamId = 1; // Enemy Team 1
            hexEnd.Data.Unit = targetUnit;

            ruleset.OnStartPathfinding(hexEnd.Data, unit);
            ruleset.movement.maxElevationDelta = 1.0f;
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
            var type = new UnitType { id = "ranged", Name = "RangedUnit" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "RAT", value = 60 },
                new UnitStatValue { id = "RNG", value = 5 } 
            };
            unitSet.units.Add(type);
            unit.Initialize("ranged", 0); // Team 0

            // Add dummy unit to target hex
            var targetGO = new GameObject("Target");
            var targetUnit = targetGO.AddComponent<Unit>();
            targetUnit.teamId = 1; // Enemy Team 1
            hexEnd.Data.Unit = targetUnit;

            ruleset.OnStartPathfinding(hexEnd.Data, unit);
            ruleset.movement.maxElevationDelta = 1.0f;
            hexStart.Data.Elevation = 0;
            hexEnd.Data.Elevation = 2.0f; 

            // Act
            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);

            // Assert
            Assert.AreEqual(0f, cost, "Ranged attack should bypass elevation check for the final target hex.");
            Object.DestroyImmediate(targetGO);
        }


        [Test]
        public void GetMoveCost_EnemyZoC_TeammatePresent_NoPenalty()
        {
            // Enemy Team 1 ZoC
            hexEnd.Data.AddState("ZoC1_999");
            // Friendly Teammate Occupied state (Team 0)
            hexEnd.Data.AddState("Occupied0_888");

            float cost = ruleset.GetPathfindingMoveCost(unit, hexStart.Data, hexEnd.Data);
            Assert.AreEqual(2.0f, cost, "Cost should be base plains cost (2) because teammate is present, ignoring enemy ZoC.");
        }


        [Test]
        public void FindPath_ChoosesPathThroughTeammate_InEnemyZoC()
        {
            // Setup a 3-hex line: Start(0,0) -> Mid(1,-1) -> End(1,-2)
            // Mid is the choke point with Enemy ZoC + Teammate
            
            // Create target hex (1, -2)
            var hexTargetGO = new GameObject("HexTarget");
            var hexTarget = hexTargetGO.AddComponent<Hex>();
            HexData targetData = new HexData(1, -2);
            targetData.TerrainType = TerrainType.Plains;
            hexTarget.AssignData(targetData);
            grid.AddHex(targetData);

            // Configure Mid Hex (hexEnd from SetUp is at 1, -1)
            hexEnd.Data.AddState("ZoC1_999"); // Enemy ZoC (Expensive!)
            hexEnd.Data.AddState("Occupied0_888"); // Teammate (Should negate ZoC)

            // Run Pathfinder
            PathResult result = Pathfinder.FindPath(grid, unit, hexStart.Data, targetData);

            // Assert
            Assert.IsTrue(result.Success, "Pathfinder should find a path.");
            Assert.AreEqual(4.0f, result.TotalCost, 0.1f, "Pathfinder should choose the cheap path through teammate (ignoring ZoC).");

            Object.DestroyImmediate(hexTargetGO);
        }


        [Test]
        public void TryMoveStep_LeavingEnemyZoC_TriggerHit_Interrupted()
        {
            // Arrange
            // 1. Setup Enemy Unit
            var enemyGO = new GameObject("Enemy");
            var enemy = enemyGO.AddComponent<Unit>();
            // Initialize with high MAT for guaranteed hit
            var type = new UnitType { id = "enemy", Name = "EnemyUnit" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "MAT", value = 100 },
                new UnitStatValue { id = "DMIN", value = 10 },
                new UnitStatValue { id = "DMAX", value = 10 } 
            };
            unitSet.units.Add(type);
            enemy.Initialize("enemy", 1); // Team 1
            enemy.Stats["MAT"] = 100;
            enemy.Stats["DMIN"] = 10;
            enemy.Stats["DMAX"] = 10;
            
            unit.SetHex(hexStart);

            // 2. Place Enemy on a neighbor hex
            // hexEnd is neighbor of hexStart (1,-1)
            enemy.SetHex(hexEnd); 
            
            // 3. Ensure hexStart has Enemy ZoC state
            hexStart.Data.AddState($"ZoC1_{enemy.Id}");
            
            // 4. Create destination hex (different from enemy hex)
            var hexDestGO = new GameObject("HexDest");
            var hexDest = hexDestGO.AddComponent<Hex>();
            HexData destData = new HexData(0, 1);
            destData.TerrainType = TerrainType.Plains;
            hexDest.AssignData(destData);
            grid.AddHex(destData);

            int startHP = unit.GetStat("HP");
            unit.Stats["CAP"] = 100;
            unit.Stats["MFAT"] = 100;
            unit.Stats["CFAT"] = 0;
            ruleset.ignoreAPs = true;
            ruleset.ignoreFatigue = true;

            // Act
            var result = ruleset.TryMoveStep(unit, hexStart.Data, destData);

            // Assert
            Assert.IsFalse(result.isValid, "Movement should be interrupted by ZoC hit.");
            Assert.IsTrue(result.reason.Contains("Attack of Opportunity"), "Reason should mention AoO.");
            Assert.Less(unit.GetStat("HP"), startHP, "Unit should have taken damage from the AoO.");

            Object.DestroyImmediate(enemyGO);
            Object.DestroyImmediate(hexDestGO);
        }
    }
}