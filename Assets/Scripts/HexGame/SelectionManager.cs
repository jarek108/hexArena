using UnityEngine;
using UnityEngine.InputSystem;
using HexGame.Tools;

namespace HexGame
{
    public class SelectionManager : MonoBehaviour
    {
        private HexRaycaster hexRaycaster;
        private ToolManager toolManager;
        private Hex lastHoveredHex;

        private void Awake()
        {
            Initialize(FindFirstObjectByType<ToolManager>(), FindFirstObjectByType<HexRaycaster>());
        }

        public void Initialize(ToolManager tm, HexRaycaster caster)
        {
            toolManager = tm;
            hexRaycaster = caster;
        }

        private void Update()
        {
            if (Mouse.current == null) return;
            
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            if (hexRaycaster == null) hexRaycaster = FindFirstObjectByType<HexRaycaster>();

            if (toolManager == null || hexRaycaster == null) return;
            
            Hex currentHoveredHex = hexRaycaster.currentHex;
            ManualUpdate(currentHoveredHex);
        }

        public void ManualUpdate(Hex hoveredHex)
        {
            if (toolManager == null || toolManager.ActiveTool == null) return;
            
            if (hoveredHex != lastHoveredHex)
            {
                (toolManager.ActiveTool as IHighlightingTool)?.HandleHighlighting(lastHoveredHex, hoveredHex);
                lastHoveredHex = hoveredHex;
            }

            toolManager.ActiveTool.HandleInput(hoveredHex);
        }
    }
}