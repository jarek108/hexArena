using UnityEngine;
using HexGame.Tools;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame
{
    public class ToolManager : MonoBehaviour
    {
        private List<ITool> tools = new List<ITool>();
        public ITool ActiveTool { get; private set; }
        private ITool lastOngoingTool;

        [SerializeField] public string activeToolName; // For inspector visibility

        // Dependencies
        private HexRaycaster hexRaycaster;
        private Hex lastHoveredHex;

        private void Awake()
        {
            Initialize();
            
            // Set PathfindingTool as the default active tool
            var pathfindingTool = tools.FirstOrDefault(t => t is PathfindingTool);
            if (pathfindingTool != null)
            {
                SetActiveTool(pathfindingTool);
            }
        }

        public void Initialize()
        {
            tools = GetComponents<ITool>().ToList();
            if (ActiveTool != null)
            {
                activeToolName = ActiveTool.GetType().Name;
            }
            
            if (hexRaycaster == null) hexRaycaster = FindFirstObjectByType<HexRaycaster>();
        }

        public void SetUpForTesting()
        {
            Initialize();
        }

        private void Update()
        {
            if (Mouse.current == null) return;
            if (ActiveTool == null) return;

            if (hexRaycaster == null) hexRaycaster = FindFirstObjectByType<HexRaycaster>();
            if (hexRaycaster == null) return;

            ManualUpdate(hexRaycaster.currentHex);
        }

        public void ManualUpdate(Hex hoveredHex)
        {
            if (ActiveTool == null) return;

            if (hoveredHex != lastHoveredHex)
            {
                (ActiveTool as IActiveTool)?.HandleHighlighting(lastHoveredHex, hoveredHex);
                lastHoveredHex = hoveredHex;
            }

            ActiveTool.HandleInput(hoveredHex);
        }

        public void SetActiveTool(ITool tool)
        {
            if (tool == null) return;

            if (!tool.CheckRequirements(out string reason))
            {
                Debug.LogWarning($"ToolManager: Cannot activate tool '{tool.GetType().Name}'. Reason: {reason}");
                return;
            }

            if (tool is IToggleTool)
            {
                tool.OnActivate();
                return;
            }

            if (ActiveTool != null)
            {
                if (lastHoveredHex != null)
                {
                    (ActiveTool as IActiveTool)?.HandleHighlighting(lastHoveredHex, null);
                }
                
                ActiveTool.OnDeactivate();
            }
            
            ActiveTool = tool;
            lastOngoingTool = tool;
            lastHoveredHex = null;

            if (ActiveTool != null)
            {
                ActiveTool.OnActivate();
                activeToolName = ActiveTool.GetType().Name;
            }
            else
            {
                activeToolName = "None";
            }
        }

        public void SelectToolByName(string toolName)
        {
            var toolToActivate = tools.FirstOrDefault(t => t.GetType().Name == toolName);
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
            return tools.Select(t => t.GetType().Name);
        }
    }
}