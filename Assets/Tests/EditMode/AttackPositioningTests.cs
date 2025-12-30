using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HexGame;

namespace HexGame.Tests
{
    public class AttackPositioningTests
    {
        private GameObject mockRoot;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        private Unit attacker;
        private Unit target;
        private Unit ally;

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
            gm.ruleset = ruleset;

            // Setup GridVisManager (needed for GetNeighbors etc if accessed via Instance)
            var gvmGo = new GameObject("MockGridVisManager");
            gvmGo.transform.SetParent(mockRoot.transform);
            var manager = gvmGo.AddComponent<GridVisualizationManager>();

            // Setup Grid
            grid = new Grid(10, 10);
            for (int q = 0; q < 10; q++)
                for (int r = 0; r < 10; r++)
                    grid.AddHex(new HexData(q, r));
            
            manager.VisualizeGrid(grid);

            attacker = CreateUnit(1, "Attacker");
            target = CreateUnit(2, "Target");
            ally = CreateUnit(1, "Ally");
        }

        [TearDown]
        public void Teardown()
        {
            if (mockRoot != null) Object.DestroyImmediate(mockRoot);
            Object.DestroyImmediate(ruleset);
        }

        private Unit CreateUnit(int teamId, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(mockRoot.transform);
            var unit = go.AddComponent<Unit>();
            unit.teamId = teamId;
            unit.Stats = new Dictionary<string, int>
            {
                { "MAT", 50 },
                { "RNG", 1 },
                { "HP", 100 }
            };
            return unit;
        }

        private void PlaceUnit(Unit unit, int q, int r)
        {
            Hex hexView = GridVisualizationManager.Instance.GetHex(q, r);
            unit.SetHex(hexView);
        }

        [Test]
        public void AttackPositioning_Paths_To_Nearest_Empty_Hex_In_Range()
        {
            // Scenario:
            // Target is at (5, 5)
            // Attacker is at (3, 5)
            // Hex (4, 5) is the direct path but is occupied by an Ally
            // Hex (4, 6) is empty and in range of Target
            
            PlaceUnit(target, 5, 5);
            PlaceUnit(ally, 4, 5);
            PlaceUnit(attacker, 3, 5);

            // 1. Get valid attack positions from Ruleset
            var attackPositions = ruleset.GetValidAttackPositions(attacker, target);
            
            // Check that ally's hex is NOT in the list
            Assert.IsFalse(attackPositions.Contains(grid.GetHexAt(4, 5)), "Occupied hex should not be a valid attack position.");
            Assert.IsTrue(attackPositions.Contains(grid.GetHexAt(4, 6)), "Empty adjacent hex should be a valid attack position.");

            // 2. Find path to any of these positions
            PathResult result = Pathfinder.FindPath(grid, attacker, attacker.CurrentHex.Data, attackPositions.ToArray());

            // 3. Assertions
            Assert.IsTrue(result.Success, "Should find a path to an attack position.");
            
            // The last hex in path should be an empty hex adjacent to target
            HexData landingHex = result.Path[result.Path.Count - 1];
            Assert.AreNotEqual(grid.GetHexAt(4, 5), landingHex, "Should not land on an ally.");
            Assert.AreEqual(1, HexMath.Distance(landingHex, grid.GetHexAt(5, 5)), "Should end adjacent to target.");
            
            // Verify path is longer/detour due to obstacle
            // Direct path to (4, 5) would be length 2. 
            // Path to (4, 6) or similar should be length 2 or 3 depending on hex geometry.
            Assert.GreaterOrEqual(result.Path.Count, 2);
        }

        [Test]
        public void AttackPositioning_Respects_Reach_Weapon_Range()
        {
            // Scenario: Pike user (Range 2)
            attacker.Stats["RNG"] = 2;
            
            PlaceUnit(target, 5, 5);
            // Block all adjacent hexes with allies
            var neighbors = grid.GetNeighbors(grid.GetHexAt(5, 5));
            foreach(var n in neighbors)
            {
                var blockingAlly = CreateUnit(1, "BlockingAlly");
                PlaceUnit(blockingAlly, n.Q, n.R);
            }

            PlaceUnit(attacker, 2, 5); // Some distance away

            var attackPositions = ruleset.GetValidAttackPositions(attacker, target);
            
            // Assert that none of the adjacent hexes (occupied) are targets
            foreach(var n in neighbors)
                Assert.IsFalse(attackPositions.Contains(n));

            // Should have valid hexes at distance 2
            Assert.IsNotEmpty(attackPositions, "Should find valid distance-2 attack positions.");

            PathResult result = Pathfinder.FindPath(grid, attacker, attacker.CurrentHex.Data, attackPositions.ToArray());
            Assert.IsTrue(result.Success);
            
            HexData landingHex = result.Path[result.Path.Count - 1];
            int distToTarget = HexMath.Distance(landingHex, grid.GetHexAt(5, 5));
            Assert.LessOrEqual(distToTarget, 2, "Should land within reach range.");
            Assert.Greater(distToTarget, 1, "Should land at distance 2 because distance 1 is fully blocked.");
        }
    }
}