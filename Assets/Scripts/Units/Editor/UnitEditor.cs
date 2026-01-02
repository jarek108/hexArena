using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

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

            // Type Selection Dropdown using Global Set
            var set = unit.unitSet;
            if (set != null && set.units != null && set.units.Count > 0)
            {
                var names = set.units.Select((u, i) => $"[{i}] {u.Name}").ToArray();
                SerializedProperty typeIdProp = serializedObject.FindProperty("unitTypeId");
                
                int currentTypeIndex = set.units.FindIndex(u => u.id == typeIdProp.stringValue);
                if (currentTypeIndex == -1) currentTypeIndex = 0;

                int newTypeIndex = EditorGUILayout.Popup("Unit Type", currentTypeIndex, names);
                if (newTypeIndex != currentTypeIndex)
                {
                    typeIdProp.stringValue = set.units[newTypeIndex].id;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("UnitManager has no valid Active Unit Set.", MessageType.Warning);
            }

            // Read-only Type ID for reference
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unitTypeId"));
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
