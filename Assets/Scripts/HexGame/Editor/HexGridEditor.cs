using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(HexGridManager))]
    public class HexGridEditor : UnityEditor.Editor
    {
        private enum Tool { None, Elevation, Terrain }
        private Tool currentTool = Tool.None;
        private TerrainType selectedTerrain;
        private float elevationAmount = 0.1f;
        private float brushSize = 1f;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            HexGridManager gridManager = (HexGridManager)target;

            if (GUILayout.Button("Generate Grid"))
            {
                gridManager.GenerateGrid();
            }
            if (GUILayout.Button("Clear Grid"))
            {
                gridManager.ClearGrid();
            }

            EditorGUILayout.Space();
            
            // Toolbar for tool selection
            currentTool = (Tool)GUILayout.Toolbar((int)currentTool, new string[] { "None", "Elevation", "Terrain" });

            if (currentTool == Tool.Elevation)
            {
                elevationAmount = EditorGUILayout.FloatField("Elevation Step", elevationAmount);
                brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 1, 10);
            }
            else if (currentTool == Tool.Terrain)
            {
                selectedTerrain = (TerrainType)EditorGUILayout.EnumPopup("Terrain Type", selectedTerrain);
                brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 1, 10);
            }
        }

        private void OnSceneGUI()
        {
            HexGridManager gridManager = (HexGridManager)target;
            if (gridManager.Grid == null || currentTool == Tool.None)
            {
                return;
            }

            // Make sure scene view is interactive
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Hex hoveredHexView = gridManager.WorldToHex(hit.point);
                if (hoveredHexView == null || hoveredHexView.Data == null) return;

                HexData hoveredData = hoveredHexView.Data;

                // Draw a brush preview
                Handles.color = Color.cyan;
                Vector3 brushCenter = gridManager.HexToWorld(hoveredData.Q, hoveredData.R);
                brushCenter.y = hoveredData.Elevation; // Adjust brush height
                Handles.DrawWireDisc(brushCenter, Vector3.up, brushSize * gridManager.HexToWorld(1, 0).x * 0.5f);

                // Show Elevation Label
                if (currentTool == Tool.Elevation)
                {
                    Handles.Label(brushCenter + Vector3.up * 0.5f, $"Elev: {hoveredData.Elevation:F1}");
                }

                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0) // Left mouse button
                    {
                        Vector3Int centerCoords = new Vector3Int(hoveredData.Q, hoveredData.R, hoveredData.S);

                        for (int q = -(int)brushSize + 1; q < brushSize; q++)
                        {
                            for (int r = -(int)brushSize + 1; r < brushSize; r++)
                            {
                                Vector3Int targetCoords = centerCoords + new Vector3Int(q, r, -q - r);
                                
                                if (Hex.Distance(centerCoords, targetCoords) < brushSize)
                                {
                                    HexData data = gridManager.Grid.GetHexAt(targetCoords.x, targetCoords.y);
                                    if (data != null)
                                    {
                                        Hex view = gridManager.GetHexView(data);
                                        if (view != null)
                                        {
                                            if (currentTool == Tool.Elevation)
                                            {
                                                view.Elevation += elevationAmount * (e.shift ? -1 : 1);
                                            }
                                            else if (currentTool == Tool.Terrain)
                                            {
                                                view.TerrainType = selectedTerrain;
                                            }
                                            EditorUtility.SetDirty(view);
                                        }
                                    }
                                }
                            }
                        }
                        EditorUtility.SetDirty(target); // Mark manager dirty too
                        e.Use(); 
                    }
                }
            }
            SceneView.RepaintAll();
        }
    }
}