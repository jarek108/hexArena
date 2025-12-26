using UnityEditor;
using UnityEngine;
using System.Linq;
using HexGame.Tools;

namespace HexGame.Editor
{
    [CustomEditor(typeof(ToolManager))]
    public class ToolManagerEditor : UnityEditor.Editor
    {
        private ToolManager toolManager;
        private string[] toolNames;
        private ITool[] attachedTools;

        private void OnEnable()
        {
            toolManager = (ToolManager)target;
            toolManager.Initialize(); 
            UpdateToolList();
        }

        private void UpdateToolList()
        {
            attachedTools = toolManager.GetComponents<ITool>();
            toolNames = attachedTools.Select(t => t.GetType().Name).ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateToolList(); 

            if (toolNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ITool components found on this GameObject.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Sync selected index with the current ActiveTool
            int selectedToolIndex = -1;
            if (toolManager.ActiveTool != null)
            {
                selectedToolIndex = System.Array.IndexOf(toolNames, toolManager.ActiveTool.GetType().Name);
            }

            EditorGUI.BeginChangeCheck();
            int newSelectedToolIndex = EditorGUILayout.Popup("Switch Tool", selectedToolIndex, toolNames);
            if (EditorGUI.EndChangeCheck())
            {
                toolManager.SetActiveTool(attachedTools[newSelectedToolIndex]);
                // If it was a toggle tool, ActiveTool won't change, 
                // so the index will naturally revert on the next layout pass.
            }
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Current Active Tool", EditorStyles.miniBoldLabel);
            GUI.enabled = false;
            EditorGUILayout.TextField(toolManager.ActiveTool != null ? toolManager.ActiveTool.GetType().Name : "None");
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Force Re-Initialize"))
            {
                toolManager.Initialize();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
