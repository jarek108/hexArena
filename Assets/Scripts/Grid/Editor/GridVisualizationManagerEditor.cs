using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HexGame.Editor
{
    [CustomEditor(typeof(GridVisualizationManager))]
    public class GridVisualizationManagerEditor : UnityEditor.Editor
    {
        private static bool showLayout = true;
        private static bool showTerrain = true;
        private static bool showStates = true;

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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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
                // EditorGUILayout.HelpBox("'Default' is the fallback. Changes to 'Default' also update the shared material asset.", MessageType.Info);
                
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

                    HexState stateEnum = (HexState)stateProp.enumValueIndex;
                    bool isDefault = stateEnum == HexState.Default;

                    if (isDefault) GUI.backgroundColor = new Color(0.85f, 0.85f, 1f); 
                    EditorGUILayout.BeginHorizontal();
                    
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(stateProp, GUIContent.none, GUILayout.Width(80));
                    GUI.enabled = true;

                    if (isDefault) EditorGUILayout.LabelField("-", GUILayout.Width(30));
                    else EditorGUILayout.PropertyField(priorityProp, GUIContent.none, GUILayout.Width(30));

                    EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(45));
                    EditorGUILayout.PropertyField(pulsationProp, GUIContent.none, GUILayout.Width(50));
                    
                    if (isDefault) gridWidthProp.floatValue = EditorGUILayout.Slider(gridWidthProp.floatValue, 0f, 1f);
                    else widthProp.floatValue = EditorGUILayout.Slider(widthProp.floatValue, 0f, 1f);

                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Force Visual Refresh"))
            {
                ((GridVisualizationManager)target).RefreshVisuals();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                var manager = (GridVisualizationManager)target;
                manager.SyncMaterialWithDefault();
                manager.RefreshVisuals();
            }
        }
    }
}