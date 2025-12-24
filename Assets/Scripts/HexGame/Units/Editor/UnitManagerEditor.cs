using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using HexGame;

namespace HexGame.Units.Editor
{
    [CustomEditor(typeof(UnitManager))]
    public class UnitManagerEditor : UnityEditor.Editor
    {
        private bool showUnits = true;

        public override void OnInspectorGUI()
        {
            UnitManager manager = (UnitManager)target;
            serializedObject.Update();

            // Settings
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeUnitSet"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultUnitVisualization"));
            GUILayout.Space(10);

            // Fetch Data from Set
            UnitSet set = manager.activeUnitSet;
            if (set == null || set.schema == null)
            {
                EditorGUILayout.HelpBox("Assign an Active Unit Set (with a valid Schema) to see unit stats.", MessageType.Warning);
            }

            var columns = (set != null && set.schema != null) ? set.schema.definitions : new List<UnitStatDefinition>();

            // --- Active Units Section ---
            EditorGUILayout.LabelField("Active Units", EditorStyles.boldLabel);
            
            if (manager.units == null) manager.units = new List<Unit>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unit Name", GUILayout.Width(100));
            EditorGUILayout.LabelField("Type", GUILayout.Width(80));
            
            foreach (var col in columns)
            {
                EditorGUILayout.LabelField(new GUIContent(col.id, col.name), GUILayout.Width(40));
            }
            
            EditorGUILayout.LabelField("Position", GUILayout.Width(80));
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            // Prepare Type Options for Dropdown
            List<string> typeNames = new List<string> { "None" };
            if (set != null)
            {
                foreach (var t in set.units) typeNames.Add(t.Name);
            }
            string[] typeOptions = typeNames.ToArray();

            // Draw Units
            for (int i = 0; i < manager.units.Count; i++)
            {
                Unit unit = manager.units[i];
                if (unit == null) continue;

                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(unit.name, GUILayout.Width(100));

                // Type Selector
                int currentIndex = 0;
                if (unit.UnitType != null && set != null)
                {
                    int foundIndex = set.units.IndexOf(unit.UnitType);
                    if (foundIndex != -1) currentIndex = foundIndex + 1;
                }

                int newIndex = EditorGUILayout.Popup(currentIndex, typeOptions, GUILayout.Width(80));
                if (newIndex != currentIndex && set != null)
                {
                    if (newIndex == 0) unit.Initialize(null);
                    else unit.Initialize(set.units[newIndex - 1]);
                    
                    EditorUtility.SetDirty(unit);
                }

                // Stats
                foreach (var col in columns)
                {
                    int val = unit.GetStat(col.id, 0);
                    EditorGUILayout.LabelField(val.ToString(), GUILayout.Width(40));
                }

                EditorGUILayout.LabelField(unit.CurrentHex != null ? unit.CurrentHex.name : "None", GUILayout.Width(80));

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    DestroyImmediate(unit.gameObject);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Add Unit"))
            {
                manager.CreateUnit();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}