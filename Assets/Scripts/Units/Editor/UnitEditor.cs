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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unitSet"));

            // Type Selection Dropdown
            if (unit.unitSet != null && unit.unitSet.units != null && unit.unitSet.units.Count > 0)
            {
                var names = unit.unitSet.units.Select((u, i) => $"[{i}] {u.Name}").ToArray();
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
                EditorGUILayout.HelpBox("Assign a UnitSet to select a type.", MessageType.Info);
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
