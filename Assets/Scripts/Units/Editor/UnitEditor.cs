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

            // 3. Current Stats (ReadOnly in this view)
            var currentStatsProp = serializedObject.FindProperty("currentStats");
            if (currentStatsProp != null && currentStatsProp.arraySize > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Live Stats (Current / Base)", EditorStyles.boldLabel);
                GUI.enabled = false;
                for (int i = 0; i < currentStatsProp.arraySize; i++)
                {
                    var statProp = currentStatsProp.GetArrayElementAtIndex(i);
                    string id = statProp.FindPropertyRelative("id").stringValue;
                    int val = statProp.FindPropertyRelative("value").intValue;
                    int baseVal = unit.GetBaseStat(id);

                    EditorGUILayout.TextField(id, $"{val} / {baseVal}");
                }
                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
