using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GridCreator))]
    public class GridCreatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GridCreator creator = (GridCreator)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Map Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Grid"))
            {
                creator.GenerateGrid();
            }

            if (GUILayout.Button("Clear Grid"))
            {
                creator.ClearGrid();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);

            if (GUILayout.Button("Save Grid"))
            {
                string path = EditorUtility.SaveFilePanel("Save Grid", "", "grid.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    creator.SaveGrid(path);
                }
            }

            if (GUILayout.Button("Load Grid"))
            {
                string path = EditorUtility.OpenFilePanel("Load Grid", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    creator.LoadGrid(path);
                }
            }
        }
    }
}