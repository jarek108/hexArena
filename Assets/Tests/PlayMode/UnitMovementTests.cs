using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Units;

namespace HexGame.Tests
{
    public class UnitMovementTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private GameObject gameMasterGO;
        private GameMaster gameMaster;
        private BattleBrothersRuleset ruleset;
        private Grid grid;
        private Unit unit;
        private UnitSet unitSet;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            typeof(GridVisualizationManager).GetProperty("Instance").SetValue(null, manager);

            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = "";
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            gameMasterGO = new GameObject("GameMaster");
            gameMaster = gameMasterGO.AddComponent<GameMaster>();
            ruleset = ScriptableObject.CreateInstance<BattleBrothersRuleset>();
            ruleset.transitionSpeed = 100f; // Fast for testing
            gameMaster.ruleset = ruleset;

            grid = new Grid(10, 10);
            manager.Grid = grid;

            unitSet = new UnitSet();
            unitManager.ActiveUnitSet = unitSet;
            var type = new UnitType { Name = "Test" };
            type.Stats = new List<UnitStatValue> { 
                new UnitStatValue { id = "AP", value = 100 },
                new UnitStatValue { id = "MAT", value = 60 },
                new UnitStatValue { id = "RNG", value = 1 } 
            };
            unitSet.units = new List<UnitType> { type };

            GameObject unitGO = new GameObject("Unit");
            unit = unitGO.AddComponent<Unit>();
            unitGO.AddComponent<SimpleUnitVisualization>();
            unit.Initialize(0, 0);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(gameMasterGO);
            Object.DestroyImmediate(ruleset);
            yield return null;
        }

        private Hex CreateHex(int q, int r)
        {
            HexData data = new HexData(q, r);
            grid.AddHex(data);
            
            GameObject go = new GameObject($"Hex_{q}_{r}");
            go.transform.SetParent(manager.transform);
            Hex hexView = go.AddComponent<Hex>();
            hexView.AssignData(data);
            
            return hexView;
        }

        [UnityTest]
        public IEnumerator UnitTraversal_UpdatesLogicalReferences()
        {
            // Arrange
            Hex startHex = CreateHex(0, 0);
            Hex targetHex = CreateHex(1, 0);
            unit.SetHex(startHex);

            Assert.AreEqual(unit, startHex.Data.Unit, "Start hex should initially have unit.");
            Assert.IsNull(targetHex.Data.Unit, "Target hex should initially be null.");

            List<HexData> path = new List<HexData> { startHex.Data, targetHex.Data };

            // Act
            unit.MoveAlongPath(path, 100f, 0f);
            
            // Wait for movement to complete
            float timeout = 2f;
            while (unit.IsMoving && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsNull(startHex.Data.Unit, "Start hex should be cleared after traversal.");
            Assert.AreEqual(unit, targetHex.Data.Unit, "Target hex should hold the unit reference after traversal.");
            Assert.IsTrue(targetHex.Data.States.Contains($"Occupied0_{unit.Id}"), "Target hex should have the Occupied state string.");
        }

        [UnityTest]
        public IEnumerator UnitTraversal_PassesThroughAlly()
        {
            // Arrange
            Hex hex1 = CreateHex(0, 0);
            Hex hex2 = CreateHex(1, 0); // Ally here
            Hex hex3 = CreateHex(2, 0); // Destination

            // Setup Main Unit
            unit.SetHex(hex1);

            // Setup Ally Unit
            GameObject allyGO = new GameObject("AllyUnit");
            Unit ally = allyGO.AddComponent<Unit>();
            allyGO.AddComponent<SimpleUnitVisualization>();
            ally.Initialize(0, 0); // Team 0 (Same as unit)
            ally.SetHex(hex2);

            // Verify setup
            Assert.AreEqual(1, hex2.Data.Units.Count, "Ally hex should have 1 unit.");
            Assert.AreEqual(ally, hex2.Data.Unit, "Ally hex should contain ally.");

            // Path: 0,0 -> 1,0 -> 2,0
            List<HexData> path = new List<HexData> { hex1.Data, hex2.Data, hex3.Data };

            // Act
            unit.MoveAlongPath(path, 100f, 0f);

            // Wait
            float timeout = 2f;
            while (unit.IsMoving && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsFalse(unit.IsMoving, "Unit should have finished moving.");
            Assert.AreEqual(hex3.Data, unit.CurrentHex.Data, "Unit should reach destination.");
            Assert.AreEqual(1, hex2.Data.Units.Count, "Ally hex should still have 1 unit (Ally).");
            Assert.AreEqual(ally, hex2.Data.Unit, "Ally hex should still contain ally.");
            Assert.IsFalse(hex2.Data.Units.Contains(unit), "Ally hex should NOT contain main unit after pass-through.");
            
            // Cleanup
            Object.Destroy(allyGO);
        }
    }
}
