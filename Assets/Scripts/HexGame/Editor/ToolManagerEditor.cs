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
                selectedToolIndex = System.Array.IndexOf(toolNames, toolManager.ActiveTool.ToolName);
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
            toolNames = attachedTools.Select(t => t.ToolName).ToArray();

            // If no tool is active, ensure we can still show a reasonable state
            if (toolManager.ActiveTool == null && toolNames.Length > 0)
            {
                toolManager.SetActiveTool(attachedTools.FirstOrDefault());
                selectedToolIndex = 0;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // Draw serialized fields normally

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tool Selection", EditorStyles.boldLabel);

            // Ensure the tool list is up-to-date in case new tools were added/removed
            UpdateToolList(); 

            if (toolNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No ITool components found on this GameObject.", MessageType.Warning);
                return;
            }

            // Display current active tool
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Tool:", GUILayout.Width(80));
            GUI.enabled = false;
            EditorGUILayout.TextField(toolManager.ActiveTool != null ? toolManager.ActiveTool.ToolName : "None");
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Dropdown to select tool
            int newSelectedToolIndex = EditorGUILayout.Popup("Select Tool", selectedToolIndex, toolNames);
            if (newSelectedToolIndex != selectedToolIndex)
            {
                selectedToolIndex = newSelectedToolIndex;
                toolManager.SelectToolByName(toolNames[selectedToolIndex]);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Force Refresh Tool List"))
            {
                UpdateToolList();
            }

            // Ensure changes are saved
            if (GUI.changed)
            {
                EditorUtility.SetDirty(toolManager);
            }
        }
    }
}
