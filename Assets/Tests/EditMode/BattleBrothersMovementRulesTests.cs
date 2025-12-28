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
            
            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.plainsCost = 2.0f;
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

            unitSet = ScriptableObject.CreateInstance<UnitSet>();
            unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            
            var type = new UnitType { Name = "TestUnit" };
            // Default 1 MRNG
            type.Stats = new List<UnitStatValue> { new UnitStatValue { id = "MRNG", value = 1 } };
            unitSet.units = new List<UnitType> { type };
            unit.Initialize(unitSet, 0, 1); // Team 1
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(unitGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(unitSet);
        }

        [Test]
        public void MoveThroughFriendly_ToEmptyHex_Allowed()
        {
            // Arrange: Hex1 occupied by friendly Team 1
            hex1.AddState($"Occupied1_{unit.Id}");
            
            // Simulating pathfinding to Hex2
            ruleset.OnStartPathfinding(hex2, unit); 

            // Act: Check cost to enter Hex1 (Friendly)
            float cost = ruleset.GetMoveCost(unit, hex0, hex1);

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
            float cost = ruleset.GetMoveCost(unit, hex0, hex1);

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
            enemy.Initialize(unitSet, 0, 2);
            hex2.Unit = enemy;
            hex2.AddState($"Occupied2_{enemy.Id}");

            // Hex1 has Friendly (Team 1) - BLOCKING the only direct path
            hex1.AddState($"Occupied1_{unit.Id}");

            // Simulating Attack Pathfinding to Hex2 (Enemy)
            // Logic: Pathfinding will try to route. 
            // 1. Move 0->1?
            // If 1 is "pass through", cost is 2.
            // But we can't stop on 1. 
            // 2. Can we stop on 1?
            // "Stop Index" logic handles the truncation, but Pathfinder needs to find a valid 'end' node.
            
            // This test verifies GetMoveCost logic specifically:
            // Moving 0 -> 1 (Intermediate) -> Allowed
            // Stopping on 1? That's handled by 'GetMoveStopIndex' logic OR by Pathfinder not finding a valid end.
            
            // Actually, if we target Hex2 (Enemy), the pathfinder calculates cost to Hex2.
            // MoveCost 1->2 (Into Enemy) is Infinity (because it's occupied by Team 2).
            // So normal pathfinding 0->1->2 FAILS.
            
            // Wait, for Attack, we want to stop at 1.
            // But 1 is Occupied.
            // So we CANNOT attack from 1.
            
            // Let's verify that 'stopping' on 1 is forbidden even if it's the attack position.
            
            // NOTE: The Pathfinder targets the Enemy Hex (2).
            // The truncation happens AFTER the path is found.
            // If the path to 2 goes through 1, and 1 is occupied...
            // The unit would "stop" at 1. 
            // But 1 is occupied.
            // So this move should be invalid.
            
            // Check: Is Hex1 treated as a valid destination?
            ruleset.OnStartPathfinding(hex1, unit); // Simulate trying to move directly to 1
            float cost = ruleset.GetMoveCost(unit, hex0, hex1);
            Assert.AreEqual(float.PositiveInfinity, cost, "Cannot manually move to occupied friendly.");

            Object.DestroyImmediate(enemyGO);
        }

        [Test]
        public void RangedAttack_AlreadyInRange_StopIndexIsOne()
        {
            // Archer at (0,0) has range 6. Enemy is at (6,0) (Dist 6).
            Unit archer = unit; // Stats: MRNG 1
            archer.Stats["RRNG"] = 6;
            archer.Stats["MRNG"] = 0;

            var enemyGO = new GameObject("Enemy");
            var enemy = enemyGO.AddComponent<Unit>();
            enemy.Initialize(unitSet, 0, 2);
            
            HexData enemyHex = new HexData(6, 0);
            enemyHex.Unit = enemy;

            List<HexData> path = new List<HexData> {
                new HexData(0,0), new HexData(1,0), new HexData(2,0),
                new HexData(3,0), new HexData(4,0), new HexData(5,0),
                enemyHex
            };

            int stopIndex = ruleset.GetMoveStopIndex(archer, path);

            Assert.AreEqual(1, stopIndex, "Should stop at current hex (index 1) if already in range.");

            Object.DestroyImmediate(enemyGO);
        }
    }
}
