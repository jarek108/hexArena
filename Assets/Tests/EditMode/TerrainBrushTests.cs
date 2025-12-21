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
    public class TerrainBrushTests
    {
        private GameObject managerGO;
        private HexGridManager manager;
        private GridCreator creator;
        private TerrainBrush terrainBrush;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = new GameObject("ManagerGO");
            manager = managerGO.AddComponent<HexGridManager>();
            creator = managerGO.AddComponent<GridCreator>();
            terrainBrush = managerGO.AddComponent<TerrainBrush>();
            
            creator.Initialize(manager);

            creator.gridWidth = 5;
            creator.gridHeight = 5;
            creator.GenerateGrid();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerGO);
        }

        [Test]
        public void TerrainBrush_WhenEnabled_PaintsTerrainOnTargetHex()
        {
            // Arrange
            Hex targetHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            targetHex.Data.TerrainType = TerrainType.Water; // Set initial different type

            var brushSO = new SerializedObject(terrainBrush);
            brushSO.FindProperty("paintType").enumValueIndex = (int)TerrainType.Desert;
            brushSO.ApplyModifiedProperties();
            
            terrainBrush.OnActivate();

            // Act
            terrainBrush.Paint(targetHex);

            // Assert
            Assert.AreEqual(TerrainType.Desert, targetHex.Data.TerrainType, "Brush should have changed the hex terrain type to Desert.");
        }
    }
}