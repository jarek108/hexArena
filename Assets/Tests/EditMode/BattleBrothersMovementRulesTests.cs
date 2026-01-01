using System.Collections.Generic;
using HexGame;
using HexGame.Units;
using NUnit.Framework;
using UnityEngine;

namespace HexGame.Tests
{
    [TestFixture]
    public class BattleBrothersMovementRulesTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        
        private GameObject unitGO;
        private Unit unit;
        private UnitSet unitSet;

        // Grid Layout: 0-1-2 (Linear)
        private HexData hex0, hex1, hex2;

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
            gameMaster.ruleset = ruleset;

            grid = new Grid(10, 10);
            manager.Grid = grid;

            // Setup 3 linear hexes
            hex0 = new HexData(0, 0) { TerrainType = TerrainType.Plains };
            hex1 = new HexData(1, 0) { TerrainType = TerrainType.Plains }; // Neighbor to 0
            hex2 = new HexData(2, 0) { TerrainType = TerrainType.Plains }; // Neighbor to 1
            
            grid.AddHex(hex0);
            grid.AddHex(hex1);
            grid.AddHex(hex2);

                    unitSet = new UnitSet();
                    unitManager.ActiveUnitSet = unitSet;
            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            
            var type = new UnitType { Name = "TestUnit" };
            // Default 1 MRNG
            type.Stats = new List<UnitStatValue> { new UnitStatValue { id = "MRNG", value = 1 } };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(0, 1); // Team 1
        }

        [TearDown]
        public void TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(unitGO);
            Object.DestroyImmediate(ruleset);
        }

        [Test]
        public void MoveThroughFriendly_ToEmptyHex_Allowed()
        {
            // Arrange: Hex1 occupied by friendly Team 1
            hex1.AddState($"Occupied1_{unit.Id}");
            
            // Simulating pathfinding to Hex2
            ruleset.OnStartPathfinding(hex2, unit); 

            // Act: Check cost to enter Hex1 (Friendly)
            float cost = ruleset.GetPathfindingMoveCost(unit, hex0, hex1);

            // Assert
            Assert.AreNotEqual(float.PositiveInfinity, cost, "Should be able to pass through friendly unit.");
            Assert.AreEqual(2.0f, cost, "Cost should be normal terrain cost.");
        }

        [Test]
        public void MoveToFriendly_OccupiedHex_Forbidden()
        {
            // Arrange: Hex1 occupied by friendly Team 1
            hex1.AddState($"Occupied1_{unit.Id}");

            // Simulating pathfinding TO Hex1
            ruleset.OnStartPathfinding(hex1, unit);

            // Act
            float cost = ruleset.GetPathfindingMoveCost(unit, hex0, hex1);

            // Assert
            Assert.AreEqual(float.PositiveInfinity, cost, "Should NOT be able to end move on friendly unit.");
        }

        [Test]
        public void MeleeAttack_SurroundedEnemy_FindsValidPosition()
        {
            // Arrange
            // Hex2 has Enemy (Team 2)
            var enemyGO = new GameObject("Enemy");
            var enemy = enemyGO.AddComponent<Unit>();
            enemy.Initialize(0, 2);
            hex2.Unit = enemy;
            hex2.AddState($"Occupied2_{enemy.Id}");

            // Hex1 has Friendly (Team 1) - BLOCKING the only direct path
            hex1.AddState($"Occupied1_{unit.Id}");
            hex1.Unit = unit; // Actually assign unit to trigger VerifyMove correctly

            // Check: Is Hex1 treated as a valid destination?
            ruleset.OnStartPathfinding(hex1, unit); // Simulate trying to move directly to 1
            float cost = ruleset.GetPathfindingMoveCost(unit, hex0, hex1);
            Assert.AreEqual(float.PositiveInfinity, cost, "Pathfinding destination cannot be occupied hex.");

            // VerifyMove should also fail
            var result = ruleset.TryMoveStep(unit, hex0, hex1);
            Assert.IsFalse(result.isValid, "VerifyMove should reject stopping on occupied hex.");

            Object.DestroyImmediate(enemyGO);
        }

        [Test]
        public void RangedAttack_AlreadyInRange_StopIndexIsOne()
        {
            // Archer at (0,0) has range 6. Enemy is at (6,0) (Dist 6).
            Unit archer = unit; // Stats: MAT 0
            archer.Stats["RAT"] = 60;
            archer.Stats["RNG"] = 6;
            archer.Stats["MAT"] = 0;

            var enemyGO = new GameObject("Enemy");
            var enemy = enemyGO.AddComponent<Unit>();
            enemy.Initialize(0, 2);
            
            HexData enemyHex = new HexData(6, 0);
            enemyHex.Unit = enemy;

            List<HexData> path = new List<HexData> {
                new HexData(0,0), new HexData(1,0), new HexData(2,0),
                new HexData(3,0), new HexData(4,0), new HexData(5,0),
                enemyHex
            };

            // Must set target so Ruleset knows we are attacking this enemy
            ruleset.OnStartPathfinding(enemyHex, archer);

            int stopIndex = ruleset.GetMoveStopIndex(archer, path);

            Assert.AreEqual(1, stopIndex, "Should stop at current hex (index 1) if already in range.");

            Object.DestroyImmediate(enemyGO);
        }
    }
}
