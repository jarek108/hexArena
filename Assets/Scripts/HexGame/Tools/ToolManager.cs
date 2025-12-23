using UnityEngine;
using HexGame.Tools;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame
{
    public class ToolManager : MonoBehaviour
    {
        private List<ITool> tools;
        public ITool ActiveTool { get; private set; }

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

            // Lazy load if missing (e.g. created after ToolManager)
            if (hexRaycaster == null) hexRaycaster = FindFirstObjectByType<HexRaycaster>();
            if (hexRaycaster == null) return;

            ManualUpdate(hexRaycaster.currentHex);
        }

        // Exposed for testing and manual overrides
        public void ManualUpdate(Hex hoveredHex)
        {
            if (ActiveTool == null) return;

            // Handle Highlighting transitions
            if (hoveredHex != lastHoveredHex)
            {
                (ActiveTool as IHighlightingTool)?.HandleHighlighting(lastHoveredHex, hoveredHex);
                lastHoveredHex = hoveredHex;
            }

            // Pass input to the active tool
            ActiveTool.HandleInput(hoveredHex);
        }

        public void SetActiveTool(ITool tool)
        {
            if (ActiveTool != null)
            {
                // Ensure we clean up highlights from the previous tool
                if (lastHoveredHex != null)
                {
                    (ActiveTool as IHighlightingTool)?.HandleHighlighting(lastHoveredHex, null);
                }
                
                ActiveTool.OnDeactivate();
            }
            
            ActiveTool = tool;
            lastHoveredHex = null; // Reset tracked hover for the new tool

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
