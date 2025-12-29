using UnityEditor;
using UnityEngine;
using System.IO;

namespace HexGame.Units.Editor
{
    [CustomEditor(typeof(UnitSchema))]
            public class UnitSchemaEditor : UnityEditor.Editor
        {
            private Vector2 scrollPos;
    
            public override void OnInspectorGUI()        {
            UnitSchema schema = (UnitSchema)target;
            serializedObject.Update();

            string currentPath = AssetDatabase.GetAssetPath(schema);
            bool isJsonLinked = !string.IsNullOrEmpty(currentPath) && currentPath.EndsWith(".json");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load JSON"))
            {
                string path = EditorUtility.OpenFilePanel("Load Schema JSON", "Assets/Data/Schemas", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    schema.FromJson(json);
                    EditorUtility.SetDirty(schema);
                }
            }
            if (GUILayout.Button("Save JSON"))
            {
                string path = EditorUtility.SaveFilePanel("Save Schema JSON", "Assets/Data/Schemas", schema.name, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, schema.ToJson());
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            UnitEditorUI.DrawSchemaEditor(schema, ref scrollPos);
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(schema);
                // If this is a transient instance from the Window, the Window handles saving.
                // But if this is an actual asset or we want to support SO->JSON sync:
                if (isJsonLinked)
                {
                    File.WriteAllText(currentPath, schema.ToJson());
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}