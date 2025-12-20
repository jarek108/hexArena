using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GridPersistence))]
    public class GridPersistenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GridPersistence persistence = (GridPersistence)target;
            HexGridManager gridManager = persistence.GetComponent<HexGridManager>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Persistence Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Save Grid"))
            {
                string path = EditorUtility.SaveFilePanel("Save Grid", "", "grid.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    if (gridManager != null)
                    {
                        persistence.SaveGrid(gridManager, path);
                    }
                    else
                    {
                        Debug.LogError("HexGridManager component missing!");
                    }
                }
            }

            if (GUILayout.Button("Load Grid"))
            {
                string path = EditorUtility.OpenFilePanel("Load Grid", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    if (gridManager != null)
                    {
                        persistence.LoadGrid(gridManager, path);
                    }
                    else
                    {
                        Debug.LogError("HexGridManager component missing!");
                    }
                }
            }
        }
    }
}