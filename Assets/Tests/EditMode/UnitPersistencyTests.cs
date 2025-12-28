using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using HexGame.Units;
using System.Collections.Generic;
using System.IO;

namespace HexGame.Tests
{
    [TestFixture]
    public class UnitPersistencyTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private UnitPlacementTool unitTool;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private UnitSet testSet;
        private string testPath;
        private GameObject testVizGO;
        private UnitVisualization testViz;

        [SetUp]
        public void SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            unitTool = managerGO.AddComponent<UnitPlacementTool>();
            
            unitManagerGO = new GameObject("Units");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            
            Grid grid = new Grid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);

            testSet = ScriptableObject.CreateInstance<UnitSet>();
            testSet.name = "TestSet";
            testSet.units = new List<UnitType> { new UnitType { Name = "Unit0" } };

            testVizGO = new GameObject("TestVizPrefab");
            testViz = testVizGO.AddComponent<SimpleUnitVisualization>();

            // Setup Manager
            unitManager.activeUnitSet = testSet;
            unitManager.unitVisualizationPrefab = testViz;

            testPath = Path.Combine(Application.temporaryCachePath, "test_units.json");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
            if (unitManagerGO != null) Object.DestroyImmediate(unitManagerGO);
            if (testVizGO != null) Object.DestroyImmediate(testVizGO);
            Object.DestroyImmediate(testSet);
            if (File.Exists(testPath)) File.Delete(testPath);
        }

        [Test]
        public void SaveAndLoad_Units_AreCorrectlyRestored()
        {
            // Arrange: Place unit
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(1, 1));
            Assert.IsNotNull(targetHex, "Target hex view should exist.");

            unitManager.SpawnUnit(0, 0, targetHex);
            Assert.IsNotNull(targetHex.Data.Unit, "Unit should be assigned to hex data.");

            // Act: Save and Load
            unitManager.SaveUnits(testPath);
            Assert.IsTrue(File.Exists(testPath));

            // Clear manually to verify load
            unitManager.EraseAllUnits();
            Assert.IsNull(targetHex.Data.Unit, "Hex unit should be null after erase.");

            unitManager.LoadUnits(testPath);

            // Assert
            Assert.IsNotNull(targetHex.Data.Unit, "Unit should be restored after load.");
            Assert.AreEqual("Unit0", targetHex.Data.Unit.UnitName);
        }

        [Test]
        public void RegenerateGrid_Units_EndUpOnWrongPosition()
        {
            // 1. Place unit at (2, 2)
            HexData dataAt22 = manager.Grid.GetHexAt(2, 2);
            Hex hexAt22 = manager.GetHexView(dataAt22);
            unitManager.SpawnUnit(0, 0, hexAt22);
            
            Vector3 originalPos = hexAt22.transform.position;
            Assert.AreEqual(originalPos.x, hexAt22.Data.Unit.transform.position.x, 0.01f);

            // 2. Regenerate Grid
            GridCreator creator = managerGO.GetComponent<GridCreator>();
            creator.GenerateGrid(); // This clears hexes and rebuilds them

            // 3. Check position
            Unit unit = unitManager.GetComponentInChildren<Unit>();
            Assert.IsNotNull(unit, "Unit should still exist.");

            // Get new hex at (2,2)
            Hex newHexAt22 = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            Vector3 expectedPos = newHexAt22.transform.position;

            Assert.AreEqual(expectedPos.x, unit.transform.position.x, 0.01f, "Unit should be at its correct hex position after regeneration.");
        }
    }
}