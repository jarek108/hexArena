using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using UnityEditor;

namespace HexGame.Tests
{
    [TestFixture]
    public class TerrainToolTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private TerrainTool terrainTool;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            terrainTool = managerGO.GetComponent<TerrainTool>();
            
            HexGrid grid = new HexGrid(5, 5);
            for (int r = 0; r < 5; r++)
                for (int q = 0; q < 5; q++)
                    grid.AddHex(new HexData(q, r));
            manager.VisualizeGrid(grid);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [Test]
        public void TerrainTool_WhenEnabled_PaintsTerrainOnTargetHex()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            targetHex.Data.TerrainType = TerrainType.Water; // Set initial different type

            var brushSO = new SerializedObject(terrainTool);
            brushSO.FindProperty("paintType").enumValueIndex = (int)TerrainType.Desert;
            brushSO.ApplyModifiedProperties();
            
            terrainTool.OnActivate();

            // Act
            terrainTool.Paint(targetHex);

            // Assert
            Assert.AreEqual(TerrainType.Desert, targetHex.Data.TerrainType, "Tool should have changed the hex terrain type to Desert.");
        }
    }
}