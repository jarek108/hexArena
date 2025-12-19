using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame
{
    public class SelectionManager : MonoBehaviour
    {
        private HexRaycaster hexRaycaster;
        private HexGridManager gridManager;

        private Hex lastHoveredHex;
        private Hex selectedHex;

        private void Start()
        {
            hexRaycaster = FindFirstObjectByType<HexRaycaster>();
            gridManager = FindFirstObjectByType<HexGridManager>();
            selectedHex = null; // Ensure no hex is selected on start
        }

        private void Update()
        {
            if (Mouse.current == null) return;
            
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
                    gridManager.ResetHex(lastHoveredHex);
                }

                // Highlight the new hex
                if (currentHoveredHex != null)
                {
                    gridManager.HighlightHex(currentHoveredHex);
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
                    gridManager.DeselectHex(selectedHex);
                    selectedHex = null;
                }
                // Select a new hex
                else
                {
                    selectedHex = newSelectedHex;
                    gridManager.SelectHex(selectedHex);
                }
            }
        }
    }
}