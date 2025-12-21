using UnityEngine;
using HexGame;
using HexGame.Tools;

namespace HexGame.Tests
{
    public static class TestHelper
    {
        public static GameObject CreateTestManager()
        {
            var go = new GameObject("TestManager");
            var gridManager = go.AddComponent<HexGridManager>();
            var creator = go.AddComponent<GridCreator>();
            var toolManager = go.AddComponent<ToolManager>();
            var selectionTool = go.AddComponent<SelectionTool>();
            var terrainTool = go.AddComponent<TerrainTool>();
            var elevationTool = go.AddComponent<ElevationTool>();
            var visualizer = go.AddComponent<HexStateVisualizer>();
            var raycaster = go.AddComponent<HexRaycaster>();
            var selectionManager = go.AddComponent<SelectionManager>();

            // Initialize all components in the correct order
            creator.Initialize(gridManager);
            toolManager.SetUpForTesting();
            selectionManager.Initialize(toolManager, raycaster);
            toolManager.SetActiveTool(selectionTool); // Set a default tool

            return go;
        }
    }
}
