using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace HexGame.Units.Editor
{
    [CustomEditor(typeof(UnitSet))]
    public class UnitSetEditor : UnityEditor.Editor
    {
        private Vector2 scrollPos;
        private UnitSchema pendingSchemaSelection;

        public override void OnInspectorGUI()
        {
            UnitSet set = (UnitSet)target;
            serializedObject.Update();

            string currentPath = AssetDatabase.GetAssetPath(set);
            bool isJsonLinked = !string.IsNullOrEmpty(currentPath) && currentPath.EndsWith(".json");

            // --- JSON Actions ---
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load JSON"))
            {
                string path = EditorUtility.OpenFilePanel("Load Set JSON", "Assets/Data/Sets", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    set.FromJson(json);
                    EditorUtility.SetDirty(set);
                }
            }
            if (GUILayout.Button("Save JSON"))
            {
                string path = EditorUtility.SaveFilePanel("Save Set JSON", "Assets/Data/Sets", set.name, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, set.ToJson());
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            // --- Header / Configuration ---
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            set.setName = EditorGUILayout.TextField("Set Name", set.setName);

            // Schema Selection
            string newSchemaId = EditorGUILayout.TextField("Schema ID Ref", set.schemaId);
            if (newSchemaId != set.schemaId)
            {
                set.schemaId = newSchemaId;
                set.schema = null; // Force reload by ID
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            // --- Unit List Editor ---
            UnitEditorUI.DrawUnitSetEditor(set, ref scrollPos);
            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(set);
                if (isJsonLinked)
                {
                    File.WriteAllText(currentPath, set.ToJson());
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
