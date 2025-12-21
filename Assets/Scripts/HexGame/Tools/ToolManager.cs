using UnityEngine;
using HexGame.Tools;
using System.Linq;
using System.Collections.Generic;

namespace HexGame
{
    public class ToolManager : MonoBehaviour
    {
        private List<ITool> tools;
        public ITool ActiveTool { get; private set; }

        [SerializeField] public string activeToolName; // For inspector visibility

        private void Awake()
        {
            Initialize();
            
            // Set SelectionTool as the default active tool
            var selectionTool = tools.FirstOrDefault(t => t is SelectionTool);
            if (selectionTool != null)
            {
                SetActiveTool(selectionTool);
            }
        }

        public void Initialize()
        {
            tools = GetComponents<ITool>().ToList();
            if (ActiveTool != null)
            {
                activeToolName = ActiveTool.ToolName;
            }
        }

        public void SetUpForTesting()
        {
            Initialize();
        }

        public void SetActiveTool(ITool tool)
        {
            if (ActiveTool != null)
            {
                ActiveTool.OnDeactivate();
            }
            
            ActiveTool = tool;

            if (ActiveTool != null)
            {
                ActiveTool.OnActivate();
                activeToolName = ActiveTool.ToolName;
            }
            else
            {
                activeToolName = "None";
            }
        }

        public void SelectToolByName(string toolName)
        {
            var toolToActivate = tools.FirstOrDefault(t => t.ToolName == toolName);
            if (toolToActivate != null)
            {
                SetActiveTool(toolToActivate);
            }
            else
            {
                Debug.LogWarning($"ToolManager: Tool with name '{toolName}' not found.");
            }
        }
        
        public IEnumerable<string> GetAvailableToolNames()
        {
            return tools.Select(t => t.ToolName);
        }
    }
}