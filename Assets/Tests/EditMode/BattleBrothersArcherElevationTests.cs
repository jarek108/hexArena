using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Units;
using System.Collections.Generic;

namespace HexGame.Tests
{
    [TestFixture]
    public class BattleBrothersArcherElevationTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private BattleBrothersRuleset ruleset;
        private MovementModule movement;
        private CombatModule combat;
        private TacticalModule tactical;

        private GameObject attackerGO;
        private Unit attacker;
        private GameObject targetGO;
        private Unit target;

        [SetUp]
        public void SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            
            // Setup Ruleset
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            movement = ScriptableObject.CreateInstance<MovementModule>();
            combat = ScriptableObject.CreateInstance<CombatModule>();
            tactical = ScriptableObject.CreateInstance<TacticalModule>();
            
            ruleset.movement = movement;
            ruleset.combat = combat;
            ruleset.tactical = tactical;
            movement.maxElevationDelta = 1.0f; // Standard limit

            // Force Ruleset Instance (GameMaster might not be available)
            // Note: Ruleset logic usually queries GameMaster.Instance.ruleset, 
            // but some methods are called directly in tests.

            // Setup Grid
            Grid grid = new Grid(10, 10);
            for (int r = 0; r < 10; r++)
                for (int q = 0; q < 10; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);

            // Attacker (Archer)
            attackerGO = new GameObject("Archer");
            attacker = attackerGO.AddComponent<Unit>();
            // RNG 6, MAT 0 (Ranged)
            var archerType = new UnitType { id = "archer", Name = "Archer" };
            archerType.Stats.Add(new UnitStatValue { id = "RNG", value = 6 });
            archerType.Stats.Add(new UnitStatValue { id = "MAT", value = 0 });
            
            // Mock UnitSet for the Unit to find its type
            var unitSet = new UnitSet { setName = "TestSet" };
            unitSet.units.Add(archerType);
            
            // We need a way to link the unit to the type. 
            // In the real app, UnitManager handles this.
            // For now, let's manually initialize stats since it's a logic test.
            attacker.SetStat("RNG", 6);
            attacker.SetStat("MAT", 0);
            attacker.teamId = 0;

            // Target
            targetGO = new GameObject("Target");
            target = targetGO.AddComponent<Unit>();
            target.teamId = 1;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(attackerGO);
            Object.DestroyImmediate(targetGO);
            Object.DestroyImmediate(ruleset);
            Object.DestroyImmediate(movement);
            Object.DestroyImmediate(combat);
            Object.DestroyImmediate(tactical);
        }

        [Test]
        public void GetValidAttackPositions_Archer_ShouldIgnoreElevationLimit()
        {
            // Arrange
            HexData attackerHex = manager.Grid.GetHexAt(0, 0);
            HexData targetHex = manager.Grid.GetHexAt(2, 0); // Range 2
            
            attackerHex.Elevation = 0f;
            targetHex.Elevation = 10f; // Way above maxElevationDelta (1.0)

            attacker.SetHex(manager.GetHexView(attackerHex));
            target.SetHex(manager.GetHexView(targetHex));

            // Act
            List<HexData> validPositions = ruleset.GetValidAttackPositions(attacker, target);

            // Assert
            // IF THE BUG EXISTS: validPositions will NOT contain attackerHex (0,0) 
            // because the elevation delta (10) > maxElevationDelta (1).
            // We expect the bug to be confirmed if attackerHex is missing.
            
            bool canAttack = validPositions.Contains(attackerHex);
            
            Assert.IsTrue(canAttack, "Archer should be able to target unit at high elevation difference. (Bug: Elevation limit incorrectly applied to ranged units)");
        }
    }
}