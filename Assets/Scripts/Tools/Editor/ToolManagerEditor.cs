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

        private UnityEditor.Editor activeToolEditor;
        private ITool lastActiveTool;

        private void OnEnable()
        {
            toolManager = (ToolManager)target;
            toolManager.Initialize(); 
            UpdateToolList();
        }

        private void OnDisable()
        {
            if (activeToolEditor != null)
            {
                DestroyImmediate(activeToolEditor);
            }
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

            if (toolNames == null || toolNames.Length == 0)
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
                if (newSelectedToolIndex >= 0 && newSelectedToolIndex < attachedTools.Length)
                {
                    toolManager.SetActiveTool(attachedTools[newSelectedToolIndex]);
                }
            }
            
            // Draw the active tool's own inspector
            if (toolManager.ActiveTool != null)
            {
                if (lastActiveTool != toolManager.ActiveTool)
                {
                    if (activeToolEditor != null) DestroyImmediate(activeToolEditor);
                    
                    UnityEngine.Object toolObj = toolManager.ActiveTool as UnityEngine.Object;
                    if (toolObj != null)
                    {
                        activeToolEditor = UnityEditor.Editor.CreateEditor(toolObj);
                    }
                    lastActiveTool = toolManager.ActiveTool;
                }

                if (activeToolEditor != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"{toolManager.ActiveTool.GetType().Name} Settings", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    activeToolEditor.OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                if (activeToolEditor != null) DestroyImmediate(activeToolEditor);
                activeToolEditor = null;
                lastActiveTool = null;
            }

            if (GUILayout.Button("Force Re-Initialize"))
            {
                toolManager.Initialize();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
