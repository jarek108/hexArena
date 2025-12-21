using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;

namespace HexGame.Tests
{
    [TestFixture]
    public class GridSaveLoadTests
    {
        private HexGridManager gridManager;
        private GridCreator gridCreator;
        private GameObject gridManagerGO;
        private string testSavePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gridManagerGO = new GameObject("GridManager");
            gridManager = gridManagerGO.AddComponent<HexGridManager>();
            gridCreator = gridManagerGO.AddComponent<GridCreator>();
            gridCreator.Initialize(gridManager);
            testSavePath = Path.Combine(Application.temporaryCachePath, "testGrid.json");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(gridManagerGO);
            if (File.Exists(testSavePath))
            {
                File.Delete(testSavePath);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_GridDimensions_ArePreserved()
        {
            // Arrange
            gridCreator.gridWidth = 15;
            gridCreator.gridHeight = 12;
            gridCreator.GenerateGrid();

            // Act
            gridCreator.SaveGrid(testSavePath);
            gridCreator.ClearGrid(); 
            gridCreator.LoadGrid(testSavePath);

            // Assert
            Assert.IsNotNull(gridManager.Grid, "Grid should not be null after loading.");
            Assert.AreEqual(15, gridManager.Grid.Width, "Grid width should be preserved.");
            Assert.AreEqual(12, gridManager.Grid.Height, "Grid height should be preserved.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_HexData_IsCorrectlyRestored()
        {
            // Arrange
            gridCreator.GenerateGrid();
            HexData originalHexData = gridManager.Grid.GetHexAt(3, 4);
            originalHexData.Elevation = 5;
            originalHexData.TerrainType = TerrainType.Desert;

            // Act
            gridCreator.SaveGrid(testSavePath);
            gridCreator.ClearGrid();
            gridCreator.LoadGrid(testSavePath);
            HexData loadedHexData = gridManager.Grid.GetHexAt(3, 4);

            // Assert
            Assert.IsNotNull(loadedHexData, "Loaded hex data should not be null.");
            Assert.AreEqual(5f, loadedHexData.Elevation, "Elevation was not restored correctly.");
            Assert.AreEqual(TerrainType.Desert, loadedHexData.TerrainType, "TerrainType was not restored correctly.");
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Load_NonExistentFile_HandlesErrorGracefully()
        {
            // Arrange
            string nonExistentPath = Path.Combine(Application.temporaryCachePath, "nonexistent.json");

            // Act & Assert
            Assert.DoesNotThrow(() => gridCreator.LoadGrid(nonExistentPath), "Loading a non-existent file should not throw an exception.");
            yield return null;
        }
    }
}