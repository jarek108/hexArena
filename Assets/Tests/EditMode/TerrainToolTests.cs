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

        [Test]
        public void TerrainTool_BrushSize_CanBeModified()
        {
            terrainTool.OnActivate();
            
            var field = terrainTool.GetType().GetField("brushSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            int initialSize = (int)field.GetValue(terrainTool);
            field.SetValue(terrainTool, Mathf.Clamp(initialSize + 1, 1, 10));
            
            int newSize = (int)field.GetValue(terrainTool);
            Assert.AreNotEqual(initialSize, newSize, "Brush size should have changed.");
        }

        [Test]
        public void TerrainTool_Hover_ShowsPreview()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            targetHex.Data.TerrainType = TerrainType.Water;
            
            var brushSO = new SerializedObject(terrainTool);
            brushSO.FindProperty("paintType").enumValueIndex = (int)TerrainType.Desert;
            brushSO.ApplyModifiedProperties();
            
            terrainTool.OnActivate();

            // Act
            terrainTool.HandleHighlighting(null, targetHex);

            // Assert
            Assert.AreEqual(TerrainType.Desert, targetHex.TerrainType, "Hex View should show Desert preview.");
            Assert.AreEqual(TerrainType.Water, targetHex.Data.TerrainType, "Hex Data should still be Water.");
        }

        [Test]
        public void TerrainTool_MoveHover_ClearsOldPreview()
        {
            // Arrange
            Hex hex1 = manager.GetHexView(manager.Grid.GetHexAt(1, 1));
            Hex hex2 = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            hex1.Data.TerrainType = TerrainType.Plains;
            
            var brushSO = new SerializedObject(terrainTool);
            brushSO.FindProperty("paintType").enumValueIndex = (int)TerrainType.Desert;
            brushSO.ApplyModifiedProperties();
            
            terrainTool.OnActivate();

            // Act
            terrainTool.HandleHighlighting(null, hex1);
            Assert.AreEqual(TerrainType.Desert, hex1.TerrainType);
            
            terrainTool.HandleHighlighting(hex1, hex2);

            // Assert
            Assert.AreEqual(TerrainType.Plains, hex1.TerrainType, "Old hex should revert to its original terrain.");
            Assert.AreEqual(TerrainType.Desert, hex2.TerrainType, "New hex should show preview.");
        }

        [Test]
        public void TerrainTool_Deactivate_ClearsAllPreviews()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            targetHex.Data.TerrainType = TerrainType.Plains;
            
            // Set tool to Desert
            var paintTypeField = terrainTool.GetType().GetField("paintType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            paintTypeField.SetValue(terrainTool, TerrainType.Desert);

            terrainTool.OnActivate();
            
            // Act 1: Hover to show preview
            terrainTool.HandleHighlighting(null, targetHex);
            Assert.AreEqual(TerrainType.Desert, targetHex.TerrainType, "Preview should be visible before deactivation.");

            // Act 2: Deactivate
            terrainTool.OnDeactivate();

            // Assert
            Assert.AreEqual(TerrainType.Plains, targetHex.TerrainType, "Preview should be cleared on deactivation.");
        }
    }
}