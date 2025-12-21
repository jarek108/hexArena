using UnityEditor;
using UnityEngine;

namespace HexGame.Editor
{
    [CustomEditor(typeof(HexGridManager))]
    public class HexGridEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all properties except the ones we've moved or handled elsewhere
            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Use 'Grid Creator' to generate or clear the grid.\nUse 'Hex State Visualizer' to edit rim styles.", MessageType.Info);
        }
    }
}