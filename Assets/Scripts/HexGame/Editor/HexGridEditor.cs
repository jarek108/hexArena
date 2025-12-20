using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(HexGridManager))]
    public class HexGridEditor : UnityEditor.Editor
    {
        private bool showRimSettings = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw everything except the rim settings
            DrawPropertiesExcluding(serializedObject, "m_Script", "defaultRimSettings", "highlightRimSettings", "selectionRimSettings");

            // Draw Rim Settings in a foldout
            showRimSettings = EditorGUILayout.Foldout(showRimSettings, "Rim Settings", true);
            if (showRimSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultRimSettings"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("highlightRimSettings"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("selectionRimSettings"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            HexGridManager gridManager = (HexGridManager)target;

            if (GUILayout.Button("Clear Grid"))
            {
                gridManager.ClearGrid();
            }
        }
    }
}