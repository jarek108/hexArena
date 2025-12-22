using UnityEditor;
using UnityEngine;
using HexGame.UI;

namespace HexGame.Editor
{
    [CustomEditor(typeof(IconManager))]
    public class IconManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            IconManager manager = (IconManager)target;

            serializedObject.Update();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("iconSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundBorderSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("padding"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hotkey Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeyColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotkeySize"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icons", EditorStyles.boldLabel);

            SerializedProperty iconsProp = serializedObject.FindProperty("icons");

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

            if (GUILayout.Button("Add Icon"))
            {
                iconsProp.InsertArrayElementAtIndex(iconsProp.arraySize);
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
