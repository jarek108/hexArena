using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HexGame;
using HexGame.Tools;
using UnityEditor;

namespace HexGame.Tests
{
    public class ToolInteractionTests
    {
        private GameObject managerGO;
        private GridVisualizationManager manager;
        private ToolManager toolManager;
        private PathfindingTool pathfindingTool;
        private TerrainTool terrainTool;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<GridVisualizationManager>();
            toolManager = managerGO.GetComponent<ToolManager>();
            pathfindingTool = managerGO.GetComponent<PathfindingTool>();
            terrainTool = managerGO.GetComponent<TerrainTool>();
            
            var caster = managerGO.AddComponent<HexRaycaster>();
            
            // Create a small grid
            Grid grid = new Grid(5, 5);
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

        [UnityTest]
        public IEnumerator PathfindingTool_WhenActive_HighlightsHoveredHex()
        {
            toolManager.SetActiveTool(pathfindingTool);
            Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(1, 1));
            Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            
            // Note: ManualUpdate calls HandleHighlighting, which doesn't check Input System
            toolManager.ManualUpdate(hexA); 
            yield return null;

            Assert.IsTrue(hexA.Data.States.Contains(HexState.Hovered), "Hex A should be in Hovered state.");
            Assert.IsFalse(hexB.Data.States.Contains(HexState.Hovered), "Hex B should not be in Hovered state.");

            toolManager.ManualUpdate(hexB);
            yield return null;

            Assert.IsFalse(hexA.Data.States.Contains(HexState.Hovered), "Hex A should no longer be in Hovered state.");
            Assert.IsTrue(hexB.Data.States.Contains(HexState.Hovered), "Hex B should now be in Hovered state.");
        }

        [UnityTest]
        public IEnumerator TerrainTool_WhenActive_HighlightsHoveredHexes()
        {
            toolManager.SetActiveTool(terrainTool);
            var brushSO = new SerializedObject(terrainTool);
            brushSO.FindProperty("brushSize").intValue = 2;
            brushSO.ApplyModifiedProperties();
            
            Hex centerHex = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            Hex neighborHex = manager.GetHexView(manager.Grid.GetHexAt(2, 3));
            Hex distantHex = manager.GetHexView(manager.Grid.GetHexAt(4, 4));

            toolManager.ManualUpdate(centerHex);
            yield return null;

            Assert.IsTrue(centerHex.Data.States.Contains(HexState.Hovered), "Center hex should be highlighted.");
            Assert.IsTrue(neighborHex.Data.States.Contains(HexState.Hovered), "Neighbor hex should be highlighted by brush size.");
            Assert.IsFalse(distantHex.Data.States.Contains(HexState.Hovered), "Distant hex should not be highlighted.");
        }
    }
}