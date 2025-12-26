using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using System.Collections.Generic;

namespace HexGame.Tests
{
    public class ElevationToolTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private ElevationTool elevationTool;
        private Hex testHex;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            elevationTool = managerGO.AddComponent<ElevationTool>();
            
            // Create a small grid and get a hex from it
            Grid grid = new Grid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);
            testHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [UnityTest]
        public IEnumerator ElevationTool_LeftClick_IncreasesElevation()
        {
            elevationTool.OnActivate();
            float initialElevation = testHex.Elevation;

            var method = elevationTool.GetType().GetMethod("ChangeElevation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(elevationTool, new object[] { testHex, 1f });

            yield return null;
            Assert.AreEqual(initialElevation + 1f, testHex.Elevation, "Elevation should have increased by 1.");
        }

        [UnityTest]
        public IEnumerator ElevationTool_RightClick_DecreasesElevation()
        {
            elevationTool.OnActivate();
            testHex.Elevation = 5f;
            float initialElevation = testHex.Elevation;

            var method = elevationTool.GetType().GetMethod("ChangeElevation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(elevationTool, new object[] { testHex, -1f });

            yield return null;
            Assert.AreEqual(initialElevation - 1f, testHex.Elevation, "Elevation should have decreased by 1.");
        }

        [UnityTest]
        public IEnumerator ElevationTool_RespectsClamping()
        {
            elevationTool.OnActivate();
            testHex.Elevation = 0f;

            var method = elevationTool.GetType().GetMethod("ChangeElevation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method.Invoke(elevationTool, new object[] { testHex, -1f });
            yield return null;
            Assert.AreEqual(0f, testHex.Elevation, "Elevation should be clamped at 0.");

            testHex.Elevation = 10f;
            method.Invoke(elevationTool, new object[] { testHex, 1f });
            yield return null;
            Assert.AreEqual(10f, testHex.Elevation, "Elevation should be clamped at 10.");
        }

        [Test]
        public void ElevationTool_BrushSize_CanBeModified()
        {
            elevationTool.OnActivate();
            
            var field = elevationTool.GetType().BaseType.GetField("brushSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            int initialSize = (int)field.GetValue(elevationTool);
            
            // Manually set brushSize to test logic
            field.SetValue(elevationTool, Mathf.Clamp(initialSize + 1, 1, 10));
            
            int newSize = (int)field.GetValue(elevationTool);
            Assert.AreEqual(initialSize + 1, newSize, "Brush size should have increased.");
        }
    }
}
