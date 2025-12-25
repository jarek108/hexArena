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
        private UnitSet testSet;
        private UnitVisualization testViz;
        private GameObject testVizGO;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            
            unitTool = managerGO.AddComponent<UnitPlacementTool>();
            
            HexGrid grid = new HexGrid(5, 5);
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
           
            // Assign via reflection for private fields
            var toolType = unitTool.GetType();
            
            var setField = toolType.GetField("activeUnitSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setField.SetValue(unitTool, testSet);

            var vizField = toolType.GetField("unitVisualizationPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            vizField.SetValue(unitTool, testViz);

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
            Object.DestroyImmediate(testSet);
            if(testVizGO != null) Object.DestroyImmediate(testVizGO);
        }

        [Test]
        public void UnitPlacementTool_PlaceUnit_CreatesUnitOnHex()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            unitTool.OnActivate(); 
            
            var placeMethod = unitTool.GetType().GetMethod("PlaceUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            placeMethod.Invoke(unitTool, new object[] { targetHex });

            // Assert
            Assert.IsNotNull(targetHex.Data.Unit, "Hex should have a unit assigned.");
            Assert.AreEqual("TestUnit1", targetHex.Data.Unit.unitName);
            Assert.AreEqual(targetHex, targetHex.Data.Unit.CurrentHex);
            
            // Verify Unit component is on the same object as visualization (SimpleUnitVisualization)
            Assert.IsNotNull(targetHex.Data.Unit.GetComponent<SimpleUnitVisualization>());
        }
        
        [Test]
        public void UnitPlacementTool_PlaceUnit_ReplacesExisting()
        {
             // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            unitTool.OnActivate();
            
            // Place first unit
             var placeMethod = unitTool.GetType().GetMethod("PlaceUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            placeMethod.Invoke(unitTool, new object[] { targetHex });
            
            Unit firstUnit = targetHex.Data.Unit;
            Assert.IsNotNull(firstUnit);
            
            // Change selection to index 1
            var indexField = unitTool.GetType().GetField("selectedUnitIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            indexField.SetValue(unitTool, 1);
            
            // Act
            placeMethod.Invoke(unitTool, new object[] { targetHex });
            
            // Assert
            Assert.IsNotNull(targetHex.Data.Unit);
            Assert.AreNotEqual(firstUnit, targetHex.Data.Unit);
            Assert.AreEqual("TestUnit2", targetHex.Data.Unit.unitName);
            
            // Ensure first unit is destroyed (Unity Object null check)
            Assert.IsTrue(firstUnit == null);
        }

        [Test]
        public void UnitPlacementTool_PlaceUnit_ParentsToUnitsContainer()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
            unitTool.OnActivate();

            var placeMethod = unitTool.GetType().GetMethod("PlaceUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            placeMethod.Invoke(unitTool, new object[] { targetHex });

            // Assert
            Transform container = managerGO.transform.Find("Units");
            Assert.IsNotNull(container, "Units container should be created.");
            Assert.AreEqual(container, targetHex.Data.Unit.transform.parent, "Unit should be parented to the Units container.");
        }
    }
}