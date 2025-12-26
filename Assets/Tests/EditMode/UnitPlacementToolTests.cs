using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using HexGame.Units;
using UnityEditor;
using System.Collections.Generic;

namespace HexGame.Tests
{
    [TestFixture]
    public class UnitPlacementToolTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private UnitPlacementTool unitTool;
        private GameObject unitManagerGO;
        private UnitManager unitManager;
        private UnitSet testSet;
        private UnitVisualization testViz;
        private GameObject testVizGO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            
            unitTool = managerGO.AddComponent<UnitPlacementTool>();
            
            unitManagerGO = new GameObject("UnitManager");
            unitManager = unitManagerGO.AddComponent<UnitManager>();
            
            Grid grid = new Grid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);

            // Create Test Data
            testSet = ScriptableObject.CreateInstance<UnitSet>();
            testSet.units = new List<UnitType>
            {
                new UnitType { Name = "TestUnit1" },
                new UnitType { Name = "TestUnit2" }
            };
            
            // Create dummy visualization prefab
            testVizGO = new GameObject("TestViz");
            testViz = testVizGO.AddComponent<SimpleUnitVisualization>();
           
            // Assign Setup to Manager
            unitManager.activeUnitSet = testSet;
            unitManager.unitVisualizationPrefab = testViz;

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
            if (unitManagerGO != null) Object.DestroyImmediate(unitManagerGO);
            Object.DestroyImmediate(testSet);
            if(testVizGO != null) Object.DestroyImmediate(testVizGO);
        }

        [Test]
        public void UnitPlacementTool_PlaceUnit_CreatesUnitOnHex()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            unitTool.OnActivate(); 
            
            var placeMethod = unitTool.GetType().GetMethod("PlaceUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            placeMethod.Invoke(unitTool, new object[] { targetHex });

            // Assert
            Assert.IsNotNull(targetHex.Data.Unit, "Hex should have a unit assigned.");
            Assert.AreEqual("TestUnit1", targetHex.Data.Unit.unitName);
        }
        
        [Test]
        public void UnitPlacementTool_BrushSize_PlacesMultipleUnits()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            
            // Set brush size to 2 (range 1) via reflection
            var brushField = unitTool.GetType().BaseType.GetField("brushSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            brushField.SetValue(unitTool, 2);
            
            unitTool.OnActivate();

            var placeMethod = unitTool.GetType().GetMethod("PlaceUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            placeMethod.Invoke(unitTool, new object[] { targetHex });

            // Assert
            int unitCount = unitManager.transform.childCount;
            Assert.AreEqual(7, unitCount, "Should place 7 units for brush size 2.");
        }

        [Test]
        public void UnitPlacementTool_EraseUnits_RemovesUnitsInBrush()
        {
            // Arrange: Place some units first
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            var brushField = unitTool.GetType().BaseType.GetField("brushSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            brushField.SetValue(unitTool, 2);
            
            unitTool.OnActivate();
            var placeMethod = unitTool.GetType().GetMethod("PlaceUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            placeMethod.Invoke(unitTool, new object[] { targetHex });

            Assert.AreEqual(7, unitManager.transform.childCount);

            // Act: Erase units
            var eraseMethod = unitTool.GetType().GetMethod("EraseUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eraseMethod.Invoke(unitTool, new object[] { targetHex });

            // Assert
            Assert.AreEqual(0, unitManager.transform.childCount, "All units in brush should be removed.");
        }

        [Test]
        public void UnitPlacementTool_Hover_MovesGhost()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(3, 3));
            unitTool.OnActivate();
            
            // Act
            unitTool.HandleHighlighting(null, targetHex);
            
            UnitVisualization ghost = null;
            foreach(Transform t in unitManager.transform) if(t.name == "UnitPlacement_PreviewGhost") ghost = t.GetComponent<UnitVisualization>();

            // Assert
            Assert.IsNotNull(ghost, "Ghost should be created on hover.");
            Vector3 expectedPos = targetHex.transform.position;
            expectedPos.y += ghost.yOffset;
            Assert.AreEqual(expectedPos, ghost.transform.position, "Ghost should move to hovered hex position.");
        }
    }
}