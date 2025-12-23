using UnityEngine;
using HexGame;
using HexGame.Tools;

namespace HexGame.Tests
{
    public static class TestHelper
    {
        public static GameObject CreateTestManager()
        {
            var existing = Object.FindFirstObjectByType<GridVisualizationManager>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("TestGridManager");
            var manager = go.AddComponent<GridVisualizationManager>();
            
            #if UNITY_EDITOR
            manager.hexSurfaceMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSurfaceMaterial.mat");
            manager.hexMaterialSides = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSideMaterial.mat");
            #endif
            
            var creator = go.AddComponent<GridCreator>();
            creator.Initialize(manager);
            go.AddComponent<ToolManager>();
            go.AddComponent<SelectionTool>();
            go.AddComponent<TerrainTool>();
            go.AddComponent<ElevationTool>();
            manager.InitializeVisuals();
            manager.ClearCache();
            return go;
        }
    }
}
