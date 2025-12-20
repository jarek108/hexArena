using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame
{
    public class SelectionManager : MonoBehaviour
    {
        private HexRaycaster hexRaycaster;
        private SelectionTool selectionTool;

        private Hex lastHoveredHex;
        private Hex selectedHex;

        private void Start()
        {
            hexRaycaster = FindFirstObjectByType<HexRaycaster>();
            selectionTool = FindFirstObjectByType<SelectionTool>();
            
            if (selectionTool == null)
            {
                // Auto-add if missing (though it should be set up in scene)
                var gridManager = FindFirstObjectByType<HexGridManager>();
                if (gridManager != null)
                {
                    selectionTool = gridManager.gameObject.AddComponent<SelectionTool>();
                }
            }
            
            selectedHex = null; 
        }

        private void Update()
        {
            if (Mouse.current == null || selectionTool == null) return;
            
            HandleHighlighting();
            HandleSelection();
        }

        private void HandleHighlighting()
        {
            Hex currentHoveredHex = hexRaycaster.currentHex;
            if (currentHoveredHex != lastHoveredHex)
            {
                // Reset the hex we just left
                if (lastHoveredHex != null)
                {
                    selectionTool.ResetHex(lastHoveredHex);
                }

                // Highlight the new hex
                if (currentHoveredHex != null)
                {
                    selectionTool.HighlightHex(currentHoveredHex);
                }

                lastHoveredHex = currentHoveredHex;
            }
        }

        private void HandleSelection()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Hex newSelectedHex = hexRaycaster.currentHex;

                // Deselect if clicking the same hex or empty space
                if (newSelectedHex == selectedHex)
                {
                    if (selectedHex != null)
                    {
                        selectionTool.DeselectHex(selectedHex);
                    }
                    selectedHex = null;
                }
                // Select a new hex
                else
                {
                    selectedHex = newSelectedHex;
                    selectionTool.SelectHex(selectedHex);
                }
            }
        }
    }
}