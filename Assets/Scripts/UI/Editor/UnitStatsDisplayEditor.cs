using UnityEditor;
using UnityEngine;
using HexGame.UI;

namespace HexGame.Editor
{
    [CustomEditor(typeof(UnitStatsDisplay))]
    [CanEditMultipleObjects]
    public class UnitStatsDisplayEditor : UnityEditor.Editor
    {
        private static bool showVisuals = true;
        private static bool showLayout = true;
        private static bool showRefs = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            DrawSelectionSection();
            
            EditorGUILayout.Space(5);
            DrawLayoutSection();

            EditorGUILayout.Space(5);
            DrawVisualsSection();

            EditorGUILayout.Space(5);
            DrawInternalRefsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSelectionSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Selection Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chooseUnitOn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("continuouslyVisible"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("keepShowingLastUnit"));
            EditorGUILayout.EndVertical();
        }

        private void DrawLayoutSection()
        {
            showLayout = EditorGUILayout.BeginFoldoutHeaderGroup(showLayout, "Layout & Background");
            if (showLayout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundSprite"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useGradient"));
                if (serializedObject.FindProperty("useGradient").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("gradientColorTop"), new GUIContent("Gradient Top"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("gradientColorBottom"), new GUIContent("Gradient Bottom"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Divider Line", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useDividerLine"), new GUIContent("Enabled"));
                if (serializedObject.FindProperty("useDividerLine").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dividerColor"), new GUIContent("Color"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dividerHeight"), new GUIContent("Height"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("panelPosition"));
                
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Spacing & Padding", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingY"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameToStatsSpacing"), new GUIContent("Name to Stats"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("statIdToValueSpacing"), new GUIContent("ID to Value Column"));
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("multilineUnitNames"));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawVisualsSection()
        {
            showVisuals = EditorGUILayout.BeginFoldoutHeaderGroup(showVisuals, "Typography & Colors");
            if (showVisuals)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Name Styling
                EditorGUILayout.LabelField("Unit Name", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameFont"), new GUIContent("Font Asset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameFontSize"), new GUIContent("Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameAlignment"), new GUIContent("Alignment"));
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);

                // Stats Styling
                EditorGUILayout.LabelField("Unit Stats", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("statsFont"), new GUIContent("Font Asset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("statsFontSize"), new GUIContent("Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("statsColor"), new GUIContent("Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("statsAlignment"), new GUIContent("Alignment"));
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawInternalRefsSection()
        {
            showRefs = EditorGUILayout.BeginFoldoutHeaderGroup(showRefs, "Internal References (Advanced)");
            if (showRefs)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("panel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unitNameText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unitStatsText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("displayedUnit"));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
