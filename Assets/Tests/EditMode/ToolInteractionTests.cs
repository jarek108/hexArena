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
    public class ToolInteractionTests
    {
        private GameObject managerGO;
        private HexGridManager manager;
        private GridCreator creator;
        private ToolManager toolManager;
        private SelectionTool selectionTool;
        private TerrainTool terrainTool;
        private SelectionManager selectionManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            managerGO = TestHelper.CreateTestManager();
            manager = managerGO.GetComponent<HexGridManager>();
            creator = managerGO.GetComponent<GridCreator>();
            toolManager = managerGO.GetComponent<ToolManager>();
            selectionTool = managerGO.GetComponent<SelectionTool>();
            terrainTool = managerGO.GetComponent<TerrainTool>();
            selectionManager = managerGO.GetComponent<SelectionManager>();

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

        [UnityTest]
        public IEnumerator SelectionTool_WhenActive_HighlightsHoveredHex()
        {
            toolManager.SetActiveTool(selectionTool);
            Hex hexA = manager.GetHexView(manager.Grid.GetHexAt(1, 1));
            Hex hexB = manager.GetHexView(manager.Grid.GetHexAt(2, 2));
            
            selectionManager.ManualUpdate(hexA); 
            yield return null;

            Assert.IsTrue(hexA.Data.States.Contains(HexState.Hovered), "Hex A should be in Hovered state.");
            Assert.IsFalse(hexB.Data.States.Contains(HexState.Hovered), "Hex B should not be in Hovered state.");

            selectionManager.ManualUpdate(hexB);
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

            selectionManager.ManualUpdate(centerHex);
            yield return null;

            Assert.IsTrue(centerHex.Data.States.Contains(HexState.Hovered), "Center hex should be highlighted.");
            Assert.IsTrue(neighborHex.Data.States.Contains(HexState.Hovered), "Neighbor hex should be highlighted by brush size.");
            Assert.IsFalse(distantHex.Data.States.Contains(HexState.Hovered), "Distant hex should not be highlighted.");
        }
    }
}
