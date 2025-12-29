using UnityEditor;
using UnityEngine;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(Unit))]
    [CanEditMultipleObjects]
    public class UnitEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Unit unit = (Unit)target;

            // 1. Configuration Section
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unitSetPath"));
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFilePanel("Select Unit Set JSON", "Assets/Data/Sets", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to project-relative if possible
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    serializedObject.FindProperty("unitSetPath").stringValue = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Type Selection Dropdown
            var set = unit.unitSet;
            if (set != null && set.units != null && set.units.Count > 0)
            {
                var names = set.units.Select((u, i) => $"[{i}] {u.Name}").ToArray();
                SerializedProperty typeIndexProp = serializedObject.FindProperty("typeIndex");
                
                int currentIndex = typeIndexProp.intValue;
                if (currentIndex < 0 || currentIndex >= names.Length) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Unit Type", currentIndex, names);
                if (newIndex != currentIndex)
                {
                    typeIndexProp.intValue = newIndex;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a valid UnitSet path to select a type.", MessageType.Info);
            }

            // Read-only Type Index for reference
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("typeIndex"));
            GUI.enabled = true;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("teamId"));

            // 2. Runtime Identity and Status
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            
            GUI.enabled = false;
            EditorGUILayout.IntField("Instance ID", unit.Id);
            EditorGUILayout.TextField("Unit Name", unit.UnitName);
            EditorGUILayout.ObjectField("Current Hex", unit.CurrentHex, typeof(Hex), true);
            GUI.enabled = true;

            // 3. Stats Dictionary (Manual Loop)
            if (unit.Stats != null && unit.Stats.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Calculated Stats", EditorStyles.boldLabel);
                GUI.enabled = false;
                foreach (var stat in unit.Stats)
                {
                    EditorGUILayout.IntField(stat.Key, stat.Value);
                }
                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
