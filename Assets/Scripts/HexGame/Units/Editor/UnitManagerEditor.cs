using UnityEditor;
using UnityEngine;
using HexGame.Units;
using System.IO;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(UnitManager))]
    public class UnitManagerEditor : UnityEditor.Editor
    {
        private string[] availableLayouts;
        private int selectedLayoutIndex = 0;

        private void OnEnable()
        {
            RefreshLayoutList();
        }

        private void RefreshLayoutList()
        {
            string dir = Path.Combine(Application.dataPath, "Data/UnitLayouts");
            if (!Directory.Exists(dir))
            {
                availableLayouts = new string[0];
                return;
            }

            availableLayouts = Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileName)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            UnitManager manager = (UnitManager)target;

            // --- Setup Section ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Unit Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unitVisualizationPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeUnitSet"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // --- Persistence Section ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            
            string defaultDir = Path.Combine(Application.dataPath, "Data/UnitLayouts");

            EditorGUILayout.BeginHorizontal();
            if (availableLayouts != null && availableLayouts.Length > 0)
            {
                if (selectedLayoutIndex >= availableLayouts.Length) selectedLayoutIndex = 0;
                selectedLayoutIndex = EditorGUILayout.Popup(selectedLayoutIndex, availableLayouts);
                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    manager.LoadUnits(Path.Combine(defaultDir, availableLayouts[selectedLayoutIndex]));
                }
            }
            else
            {
                EditorGUILayout.LabelField("No layouts found", EditorStyles.miniLabel);
            }
            
            if (GUILayout.Button("Ref.", GUILayout.Width(40))) RefreshLayoutList();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save New..."))
            {
                string path = EditorUtility.SaveFilePanel("Save Unit Layout", defaultDir, "units.json", "json");
                if (!string.IsNullOrEmpty(path)) 
                { 
                    manager.SaveUnits(path); 
                    RefreshLayoutList();
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("Import..."))
            {
                string path = EditorUtility.OpenFilePanel("Load Units", defaultDir, "json");
                if (!string.IsNullOrEmpty(path)) 
                {
                    manager.LoadUnits(path);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // --- Operations Section ---
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Relink Units to Grid", GUILayout.Height(25)))
            {
                manager.RelinkUnitsToGrid();
            }

            if (GUILayout.Button("Erase All Units", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Erase All Units", "Are you sure you want to remove all units?", "Yes", "No"))
                {
                    manager.EraseAllUnits();
                }
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
