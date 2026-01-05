using UnityEditor;
using UnityEngine;
using HexGame.UI;
using HexGame.Tools;
using System.Linq;
using System.Collections.Generic;

namespace HexGame.Editor
{
    [CustomEditor(typeof(IconManager))]
    public class IconManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            IconManager manager = (IconManager)target;
            SerializedProperty iconsProp = serializedObject.FindProperty("icons");

            serializedObject.Update();

            // Top Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate Tools"))
            {
                manager.PopulateTools();
            }
            if (GUILayout.Button("Add Icon"))
            {
                iconsProp.InsertArrayElementAtIndex(iconsProp.arraySize);
            }
            if (GUILayout.Button("Clear Icons"))
            {
                manager.icons.Clear();
                manager.ClearUIImmediate();
                EditorUtility.SetDirty(manager);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconFolder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconPadding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useGradient"));
            if (serializedObject.FindProperty("useGradient").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gradientColorBottom"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("padding"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hotkey Box Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeyBoxColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeyTextColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeyFontSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeyBoxSize"));
            
            EditorGUILayout.LabelField("Selection Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationSpeed"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icons", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < iconsProp.arraySize; i++)
            {
                SerializedProperty element = iconsProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = element.FindPropertyRelative("iconName");
                SerializedProperty spriteProp = element.FindPropertyRelative("iconSprite");
                SerializedProperty hotkeyProp = element.FindPropertyRelative("hotkey");
                SerializedProperty onClickProp = element.FindPropertyRelative("onClick");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(80));
                EditorGUILayout.PropertyField(spriteProp, GUIContent.none, GUILayout.Width(100));
                EditorGUILayout.LabelField("Key:", GUILayout.Width(30));
                EditorGUILayout.PropertyField(hotkeyProp, GUIContent.none, GUILayout.Width(40));
                
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    iconsProp.DeleteArrayElementAtIndex(i);
                    // Breaking here is crucial as the array size has changed
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break; 
                }
                EditorGUILayout.EndHorizontal();

                // Draw the UnityEvent on a new line
                EditorGUILayout.PropertyField(onClickProp);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (serializedObject.ApplyModifiedProperties() || EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                {
                    manager.ClearUIImmediate();
                    manager.RefreshUI();
                    EditorUtility.SetDirty(manager);
                }
            }
        }
    }
}
