
using NUnit.Framework;
using UnityEngine;
using System.IO;

namespace HexGame.Tests
{
    [TestFixture]
    public class GridSaveLoadTests
    {
        private HexGridManager gridManager;
        private GridPersistence gridPersistence;
        private GameObject gridManagerGO;
        private string testSavePath;

        [SetUp]
        public void SetUp()
        {
            gridManagerGO = new GameObject("GridManager");
            gridManager = gridManagerGO.AddComponent<HexGridManager>();
            gridPersistence = gridManagerGO.AddComponent<GridPersistence>(); // Add the new component
            testSavePath = Path.Combine(Application.temporaryCachePath, "testGrid.json");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridManagerGO);
            if (File.Exists(testSavePath))
            {
                File.Delete(testSavePath);
            }
        }

        [Test]
        public void SaveAndLoad_GridDimensions_ArePreserved()
        {
            // Arrange
            gridManager.gridWidth = 15;
            gridManager.gridHeight = 12;
            gridManager.GenerateGrid();

            // Act
            gridPersistence.SaveGrid(gridManager, testSavePath);
            gridManager.ClearGrid(); 
            gridPersistence.LoadGrid(gridManager, testSavePath);

            // Assert
            Assert.IsNotNull(gridManager.Grid, "Grid should not be null after loading.");
            Assert.AreEqual(15, gridManager.Grid.Width, "Grid width should be preserved.");
            Assert.AreEqual(12, gridManager.Grid.Height, "Grid height should be preserved.");
        }

        [Test]
        public void SaveAndLoad_HexData_IsCorrectlyRestored()
        {
            // Arrange
            gridManager.GenerateGrid();
            HexData originalHexData = gridManager.Grid.GetHexAt(3, 4);
            originalHexData.Elevation = 5;
            originalHexData.TerrainType = TerrainType.Desert;

            // Act
            gridPersistence.SaveGrid(gridManager, testSavePath);
            gridManager.ClearGrid();
            gridPersistence.LoadGrid(gridManager, testSavePath);
            HexData loadedHexData = gridManager.Grid.GetHexAt(3, 4);

            // Assert
            Assert.IsNotNull(loadedHexData, "Loaded hex data should not be null.");
            Assert.AreEqual(5f, loadedHexData.Elevation, "Elevation was not restored correctly.");
            Assert.AreEqual(TerrainType.Desert, loadedHexData.TerrainType, "TerrainType was not restored correctly.");
        }
        
        [Test]
        public void Load_NonExistentFile_HandlesErrorGracefully()
        {
            // Arrange
            string nonExistentPath = Path.Combine(Application.temporaryCachePath, "nonexistent.json");

            // Act & Assert
            // We expect a log message, not an exception. The function should simply return without crashing.
            Assert.DoesNotThrow(() => gridPersistence.LoadGrid(gridManager, nonExistentPath), "Loading a non-existent file should not throw an exception.");
            
            // Optional: Check for a warning log
            // LogAssert.Expect(LogType.Warning, $"File not found at {nonExistentPath}. Aborting load.");
            // For now, we'll just ensure it doesn't crash.
        }
    }
}
