using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;

namespace HexGame.Tests
{
    public class HexComponentTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private GridCreator creator;
        private Hex testHex;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            creator = managerGO.GetComponent<GridCreator>();
            
            // Populate grid
            HexGrid grid = new HexGrid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);
            
            testHex = manager.GetHexView(manager.Grid.GetHexAt(0, 0));
            Assert.IsNotNull(testHex, "Test hex should not be null.");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(managerGO);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hex_Visuals_Update_On_Terrain_Change()
        {
            // Initial color for Plains (default)
            Color initialColor = manager.GetHexColor(testHex);

            // Change terrain type to Water (assuming Water is blue in mapping)
            testHex.TerrainType = TerrainType.Water;
            yield return null;
            
            Color actualColor = manager.GetHexColor(testHex);
            Assert.AreEqual(Color.blue, actualColor, "Hex color should update to new terrain type color.");
            Assert.AreNotEqual(initialColor, actualColor, "Hex color should change from initial color.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hex_Position_Updates_On_Elevation_Change()
        {
            // Get initial position
            Vector3 initialPosition = testHex.transform.position;
            float initialY = initialPosition.y;

            // Change elevation
            float newElevation = initialY + 5f;
            testHex.Elevation = newElevation;
            yield return null; // Allow OnValidate to run in editor

            // Get new position
            Vector3 finalPosition = testHex.transform.position;
            float finalY = finalPosition.y;

            Assert.AreEqual(newElevation, finalY, 0.001f, "Hex Y position should update to new elevation.");
            Assert.AreNotEqual(initialY, finalY, "Hex Y position should change from initial Y.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridVisualizationManager_InitializesHexBaseColorCorrectly()    {
            testHex.TerrainType = TerrainType.Plains;
            manager.RefreshVisuals(testHex);
            
            Color actualColor = manager.GetHexColor(testHex);
            Color expectedColor = manager.GetDefaultHexColor(testHex);
            Assert.AreEqual(expectedColor, actualColor, "Initial hex base color should match its terrain type.");
            yield return null;
        }
    }
}