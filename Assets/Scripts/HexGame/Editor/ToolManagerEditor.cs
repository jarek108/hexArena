using UnityEditor;
using UnityEngine;
using System.Linq;
using HexGame.Tools; // Ensure this is present for ITool and other tool scripts

namespace HexGame.Editor
{
    [CustomEditor(typeof(ToolManager))]
    public class ToolManagerEditor : UnityEditor.Editor
    {
        private ToolManager toolManager;
        private string[] toolNames;
        private int selectedToolIndex;

        private void OnEnable()
        {
            toolManager = (ToolManager)target;
            // Initialize the toolManager's internal list if it hasn't been already
            // This is important for editor-time reflection of tools
            toolManager.SetUpForTesting(); 
            UpdateToolList();
            
            // Set initial selected index based on active tool
            if (toolManager.ActiveTool != null)
            {
                selectedToolIndex = System.Array.IndexOf(toolNames, toolManager.ActiveTool.GetType().Name);
            }
            else
            {
                selectedToolIndex = 0; // "None" or first available
            }
        }

        private void UpdateToolList()
        {
            // Get all ITool components attached to the GameObject
            var attachedTools = toolManager.GetComponents<ITool>();
            toolNames = attachedTools.Select(t => t.GetType().Name).ToArray();

            // If no tool is active, ensure we can still show a reasonable state
            if (toolManager.ActiveTool == null && toolNames.Length > 0)
            {
                toolManager.SetActiveTool(attachedTools.FirstOrDefault());
                selectedToolIndex = 0;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Ensure the tool list is up-to-date in case new tools were added/removed
            UpdateToolList(); 

            if (toolNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ITool components found on this GameObject.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Dropdown to select tool and display current
            EditorGUILayout.BeginHorizontal();
            int newSelectedToolIndex = EditorGUILayout.Popup("Switch Tool", selectedToolIndex, toolNames);
            if (newSelectedToolIndex != selectedToolIndex)
            {
                selectedToolIndex = newSelectedToolIndex;
                toolManager.SelectToolByName(toolNames[selectedToolIndex]);
            }
            
            GUI.enabled = false;
            EditorGUILayout.TextField(toolManager.ActiveTool != null ? toolManager.ActiveTool.GetType().Name : "None", GUILayout.Width(100));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("Force Refresh Tool List"))
            {
                UpdateToolList();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(toolManager);
            }
        }
    }
}