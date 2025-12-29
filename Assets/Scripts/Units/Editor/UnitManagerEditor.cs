using UnityEditor;
using UnityEngine;
using HexGame.Units;
using System.IO;
using System.Linq;
using HexGame.Units.Editor;

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
            
            // Unit Set Selection Dropdown
            string setsDir = "Assets/Data/Sets";
            if (!Directory.Exists(setsDir)) Directory.CreateDirectory(setsDir);
            
            string[] setFiles = Directory.GetFiles(setsDir, "*.json");
            string[] setNames = setFiles.Select(Path.GetFileName).ToArray();
            
            SerializedProperty pathProp = serializedObject.FindProperty("activeUnitSetPath");
            string currentPath = pathProp.stringValue;
            int currentIndex = -1;
            
            if (!string.IsNullOrEmpty(currentPath))
            {
                string currentFileName = Path.GetFileName(currentPath);
                currentIndex = System.Array.IndexOf(setNames, currentFileName);
            }

            EditorGUI.BeginChangeCheck();
            int newSetIndex = EditorGUILayout.Popup("Active Unit Set", currentIndex, setNames);
            if (EditorGUI.EndChangeCheck() && newSetIndex >= 0)
            {
                pathProp.stringValue = setFiles[newSetIndex].Replace("\\", "/");
                // Force reload if needed? Or let OnEnable/property handle it
            }

            if (GUILayout.Button("Open Unit Data Editor"))
            {
                UnitDataEditorWindow.Open();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // --- Persistence Section ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            
            string defaultDir = Path.Combine(Application.dataPath, "Data/UnitLayouts");

            // Row 1: Dropdown, Refresh
            EditorGUILayout.BeginHorizontal();
            if (availableLayouts != null && availableLayouts.Length > 0)
            {
                if (selectedLayoutIndex >= availableLayouts.Length) selectedLayoutIndex = 0;
                selectedLayoutIndex = EditorGUILayout.Popup(selectedLayoutIndex, availableLayouts);
            }
            else
            {
                EditorGUILayout.LabelField("No layouts found", EditorStyles.miniLabel);
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshLayoutList();
            EditorGUILayout.EndHorizontal();

            // Row 2: Save, Save As, Reload, Load...
            EditorGUILayout.BeginHorizontal();
            
            // Save
            GUI.enabled = !string.IsNullOrEmpty(manager.lastLayoutPath);
            if (GUILayout.Button("Save"))
            {
                manager.SaveUnits(manager.lastLayoutPath);
            }
            
            // Save As
            GUI.enabled = true;
            if (GUILayout.Button("Save As"))
            {
                string path = EditorUtility.SaveFilePanel("Save Unit Layout", defaultDir, "units.json", "json");
                if (!string.IsNullOrEmpty(path)) 
                { 
                    manager.SaveUnits(path); 
                    RefreshLayoutList();
                    GUIUtility.ExitGUI();
                }
            }

            // Reload (from dropdown)
            GUI.enabled = availableLayouts != null && availableLayouts.Length > 0;
            if (GUILayout.Button("Reload"))
            {
                manager.LoadUnits(Path.Combine(defaultDir, availableLayouts[selectedLayoutIndex]));
            }

            // Load (External)
            GUI.enabled = true;
            if (GUILayout.Button("Load..."))
            {
                string path = EditorUtility.OpenFilePanel("Load Units", defaultDir, "json");
                if (!string.IsNullOrEmpty(path)) 
                {
                    manager.LoadUnits(path);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(manager.lastLayoutPath))
            {
                EditorGUILayout.LabelField($"Active: {Path.GetFileName(manager.lastLayoutPath)}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            // Row 3: Relink and Erase (One row, no header)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Relink Units", GUILayout.Height(20)))
            {
                manager.RelinkUnitsToGrid();
            }

            if (GUILayout.Button("Erase All", GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Erase All Units", "Are you sure you want to remove all units?", "Yes", "No"))
                {
                    manager.EraseAllUnits();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
