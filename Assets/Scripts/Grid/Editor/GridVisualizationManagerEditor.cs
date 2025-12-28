using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GridVisualizationManager))]
    public class GridVisualizationManagerEditor : UnityEditor.Editor
    {
        private static bool showLayout = false;
        private static bool showTerrain = false;
        private static bool showStates = true;

        private string[] availableLayouts;
        private int selectedLayoutIndex = 0;

        private SerializedProperty hexSurfaceMaterialProp;
        private SerializedProperty hexMaterialSidesProp;
        private SerializedProperty colorPlainsProp;
        private SerializedProperty colorWaterProp;
        private SerializedProperty colorMountainsProp;
        private SerializedProperty colorForestProp;
        private SerializedProperty colorDesertProp;
        private SerializedProperty stateSettingsProp;
        private SerializedProperty showGridProp;
        private SerializedProperty gridWidthProp;
        private SerializedProperty hexSizeProp;
        private SerializedProperty isPointyTopProp;
        private SerializedProperty stateVisualsFileProp;

        private void OnEnable()
        {
            hexSurfaceMaterialProp = serializedObject.FindProperty("hexSurfaceMaterial");
            hexMaterialSidesProp = serializedObject.FindProperty("hexMaterialSides");
            colorPlainsProp = serializedObject.FindProperty("colorPlains");
            colorWaterProp = serializedObject.FindProperty("colorWater");
            colorMountainsProp = serializedObject.FindProperty("colorMountains");
            colorForestProp = serializedObject.FindProperty("colorForest");
            colorDesertProp = serializedObject.FindProperty("colorDesert");
            stateSettingsProp = serializedObject.FindProperty("stateSettings");
            showGridProp = serializedObject.FindProperty("showGrid");
            gridWidthProp = serializedObject.FindProperty("gridWidth");
            hexSizeProp = serializedObject.FindProperty("hexSize");
            isPointyTopProp = serializedObject.FindProperty("isPointyTop");
            stateVisualsFileProp = serializedObject.FindProperty("stateVisualsFile");

            RefreshLayoutList();
        }

        private void RefreshLayoutList()
        {
            string dir = Path.Combine(Application.dataPath, "Data/StateVisuals");
            if (!Directory.Exists(dir))
            {
                availableLayouts = new string[0];
                return;
            }

            availableLayouts = Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileName)
                .ToArray();

            // Match current file to index
            string current = stateVisualsFileProp.stringValue;
            selectedLayoutIndex = 0;
            for (int i = 0; i < availableLayouts.Length; i++)
            {
                if (availableLayouts[i] == current)
                {
                    selectedLayoutIndex = i;
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GridVisualizationManager manager = (GridVisualizationManager)target;

            EditorGUILayout.Space();

            // --- SECTION: PERSISTENCE (AT TOP) ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("State Layout Persistence", EditorStyles.boldLabel);
            
            string defaultDir = Path.Combine(Application.dataPath, "Data/StateVisuals");

            EditorGUILayout.BeginHorizontal();
            if (availableLayouts != null && availableLayouts.Length > 0)
            {
                if (selectedLayoutIndex >= availableLayouts.Length) selectedLayoutIndex = 0;
                int newIndex = EditorGUILayout.Popup(selectedLayoutIndex, availableLayouts);
                if (newIndex != selectedLayoutIndex)
                {
                    selectedLayoutIndex = newIndex;
                    stateVisualsFileProp.stringValue = availableLayouts[selectedLayoutIndex];
                }

                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    manager.LoadStateSettings();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No layouts found", EditorStyles.miniLabel);
            }
            
            if (GUILayout.Button("Ref.", GUILayout.Width(40))) RefreshLayoutList();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Current"))
            {
                manager.SaveStateSettings();
                RefreshLayoutList();
            }
            if (GUILayout.Button("Save New..."))
            {
                string path = EditorUtility.SaveFilePanel("Save State Layout", defaultDir, "new_states.json", "json");
                if (!string.IsNullOrEmpty(path)) 
                { 
                    manager.SaveStateSettings(path);
                    stateVisualsFileProp.stringValue = Path.GetFileName(path);
                    RefreshLayoutList();
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("Import..."))
            {
                string path = EditorUtility.OpenFilePanel("Load State Layout", defaultDir, "json");
                if (!string.IsNullOrEmpty(path)) 
                {
                    manager.LoadStateSettings(path);
                    stateVisualsFileProp.stringValue = Path.GetFileName(path);
                    RefreshLayoutList();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            
            // --- SECTION: LAYOUT & MATERIALS ---
            showLayout = EditorGUILayout.BeginFoldoutHeaderGroup(showLayout, "Layout & Materials");
            if (showLayout)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(isPointyTopProp);
                EditorGUILayout.PropertyField(hexSizeProp);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(hexSurfaceMaterialProp);
                EditorGUILayout.PropertyField(hexMaterialSidesProp);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // --- SECTION: TERRAIN COLORS ---
            showTerrain = EditorGUILayout.BeginFoldoutHeaderGroup(showTerrain, "Terrain Colors");
            if (showTerrain)
            {
                EditorGUILayout.PropertyField(colorPlainsProp, new GUIContent("Plains"));
                EditorGUILayout.PropertyField(colorWaterProp, new GUIContent("Water"));
                EditorGUILayout.PropertyField(colorMountainsProp, new GUIContent("Mountains"));
                EditorGUILayout.PropertyField(colorForestProp, new GUIContent("Forest"));
                EditorGUILayout.PropertyField(colorDesertProp, new GUIContent("Desert"));
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // --- SECTION: GRID & STATE VISUALS ---
            showStates = EditorGUILayout.BeginFoldoutHeaderGroup(showStates, "Grid & State Visuals");
            if (showStates)
            {
                EditorGUILayout.PropertyField(showGridProp, new GUIContent("Grid Visible"));

                // Header Row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("State", GUILayout.Width(80));
                EditorGUILayout.LabelField("Pri", GUILayout.Width(30));
                EditorGUILayout.LabelField("Color", GUILayout.Width(45));
                EditorGUILayout.LabelField("Pulse", GUILayout.Width(50));
                EditorGUILayout.LabelField("Width");
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < stateSettingsProp.arraySize; i++)
                {
                    SerializedProperty element = stateSettingsProp.GetArrayElementAtIndex(i);
                    SerializedProperty stateProp = element.FindPropertyRelative("state");
                    SerializedProperty priorityProp = element.FindPropertyRelative("priority");
                    SerializedProperty visualsProp = element.FindPropertyRelative("visuals");
                    
                    SerializedProperty colorProp = visualsProp.FindPropertyRelative("color");
                    SerializedProperty widthProp = visualsProp.FindPropertyRelative("width");
                    SerializedProperty pulsationProp = visualsProp.FindPropertyRelative("pulsation");

                    string stateName = stateProp.stringValue;
                    bool isDefault = stateName == "Default";

                    if (isDefault) GUI.backgroundColor = new Color(0.85f, 0.85f, 1f); 
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.PropertyField(stateProp, GUIContent.none, GUILayout.Width(80));

                    if (isDefault) EditorGUILayout.LabelField("-", GUILayout.Width(30));
                    else EditorGUILayout.PropertyField(priorityProp, GUIContent.none, GUILayout.Width(30));

                    EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(45));
                    EditorGUILayout.PropertyField(pulsationProp, GUIContent.none, GUILayout.Width(50));
                    
                    if (isDefault) gridWidthProp.floatValue = EditorGUILayout.Slider(gridWidthProp.floatValue, 0f, 1f);
                    else widthProp.floatValue = EditorGUILayout.Slider(widthProp.floatValue, 0f, 1f);

                    // Move Buttons
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(20)))
                    {
                        stateSettingsProp.MoveArrayElement(i, i - 1);
                        break;
                    }
                    GUI.enabled = i < stateSettingsProp.arraySize - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(20)))
                    {
                        stateSettingsProp.MoveArrayElement(i, i + 1);
                        break;
                    }
                    GUI.enabled = true;

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        stateSettingsProp.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.Space();

                if (GUILayout.Button("Add New State"))
                {
                    stateSettingsProp.InsertArrayElementAtIndex(stateSettingsProp.arraySize);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Force Visual Refresh", GUILayout.Height(30)))
            {
                manager.RefreshVisuals();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                if (!Application.isPlaying)
                {
                    manager.SyncMaterialWithDefault();
                    manager.RefreshVisuals();
                    EditorUtility.SetDirty(manager);
                }
            }
        }
    }
}
