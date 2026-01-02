using System.Collections.Generic;
using HexGame;
using HexGame.Units;
using NUnit.Framework;
using UnityEngine;

namespace HexGame.Tests
{
    [TestFixture]
    public class UnitStateCleanupTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private Grid grid;
        
        [SetUp]
        public void SetUp()
        {
            managerGO = new GameObject("GridVisualizationManager");
            manager = managerGO.AddComponent<GridVisualizationManager>();
            
            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            unitManager.activeUnitSetPath = "";
            typeof(UnitManager).GetProperty("Instance").SetValue(null, unitManager);

            grid = new Grid(10, 10);
            manager.Grid = grid;

            // Setup a few hexes
            for (int q = 0; q < 3; q++)
            {
                for (int r = 0; r < 3; r++)
                {
                    HexData data = new HexData(q, r);
                    grid.AddHex(data);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            typeof(UnitManager).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(unitManagerGO);
        }

        [Test]
        public void Unit_CleansUpStates_OnDestroy()
        {
            // Arrange
            GameObject unitGO = new GameObject("TestUnit");
            Unit unit = unitGO.AddComponent<Unit>();
            HexData hex = grid.GetHexAt(0, 0);
            
            unit.AddOwnedHexState(hex, "TestState");
            Assert.IsTrue(hex.States.Contains("TestState"), "State should be added to hex.");

            // Act
            Object.DestroyImmediate(unitGO);

            // Assert
            Assert.IsFalse(hex.States.Contains("TestState"), "State should be removed from hex on unit destruction.");
        }

        [Test]
        public void ClearOwnedHexStates_RemovesOnlySpecificStates()
        {
            // Arrange
            GameObject unitGO = new GameObject("TestUnit");
            Unit unit = unitGO.AddComponent<Unit>();
            HexData hex = grid.GetHexAt(0, 0);
            
            hex.AddState("ExternalState");
            unit.AddOwnedHexState(hex, "UnitState");

            // Act
            unit.ClearOwnedHexStates();

            // Assert
            Assert.IsFalse(hex.States.Contains("UnitState"), "Unit state should be removed.");
            Assert.IsTrue(hex.States.Contains("ExternalState"), "External state should remain.");
            Object.DestroyImmediate(unitGO);
        }

        [Test]
        public void AddOwnedHexState_TracksMultipleHexes()
        {
            // Arrange
            GameObject unitGO = new GameObject("TestUnit");
            Unit unit = unitGO.AddComponent<Unit>();
            HexData hex1 = grid.GetHexAt(0, 0);
            HexData hex2 = grid.GetHexAt(1, 1);
            
            unit.AddOwnedHexState(hex1, "State1");
            unit.AddOwnedHexState(hex2, "State2");

            // Act
            unit.ClearOwnedHexStates();

            // Assert
            Assert.IsFalse(hex1.States.Contains("State1"));
            Assert.IsFalse(hex2.States.Contains("State2"));
            Object.DestroyImmediate(unitGO);
        }
    }
}
