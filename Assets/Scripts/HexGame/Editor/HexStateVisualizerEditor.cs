using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HexGame.Editor
{
    [CustomEditor(typeof(HexStateVisualizer))]
    public class HexStateVisualizerEditor : UnityEditor.Editor
    {
        private SerializedProperty stateSettingsProp;
        private SerializedProperty showGridProp;
        private SerializedProperty gridWidthProp;

        private void OnEnable()
        {
            stateSettingsProp = serializedObject.FindProperty("stateSettings");
            showGridProp = serializedObject.FindProperty("showGrid");
            gridWidthProp = serializedObject.FindProperty("gridWidth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("'Default' is the fallback. Higher Priority overrides lower ones. Changes to 'Default' also update the shared material asset.", MessageType.Info);
            EditorGUILayout.Space();

            // Grid Visuals Row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(showGridProp, new GUIContent("Show Grid"));
            EditorGUILayout.PropertyField(gridWidthProp, new GUIContent("Grid Width"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State Visual Settings", EditorStyles.boldLabel);

            // Header Row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("State", GUILayout.Width(80));
            EditorGUILayout.LabelField("Pri", GUILayout.Width(30));
            EditorGUILayout.LabelField("Color", GUILayout.Width(45));
            EditorGUILayout.LabelField("Width", GUILayout.Width(150)); // More space for slider
            EditorGUILayout.LabelField("Pulse", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // Rows
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
                
                // State Name
                GUI.enabled = false;
                EditorGUILayout.PropertyField(stateProp, GUIContent.none, GUILayout.Width(80));
                GUI.enabled = true;

                // Priority
                if (isDefault)
                {
                    EditorGUILayout.LabelField("-", GUILayout.Width(30));
                }
                else
                {
                    EditorGUILayout.PropertyField(priorityProp, GUIContent.none, GUILayout.Width(30));
                }

                // Color
                EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(45));

                // Width - Forced Slider
                widthProp.floatValue = EditorGUILayout.Slider(widthProp.floatValue, 0f, 1f, GUILayout.Width(150));

                // Pulsation
                EditorGUILayout.PropertyField(pulsationProp, GUIContent.none, GUILayout.Width(50));

                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Force Visual Refresh"))
            {
                var visualizer = (HexStateVisualizer)target;
                var manager = visualizer.GetComponent<HexGridManager>();
                if (manager != null) manager.RefreshVisuals();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                var visualizer = (HexStateVisualizer)target;
                visualizer.SendMessage("SyncMaterialWithDefault", SendMessageOptions.DontRequireReceiver);
                
                var manager = visualizer.GetComponent<HexGridManager>();
                if (manager != null) manager.RefreshVisuals();
            }
        }
    }
}