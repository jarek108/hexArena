using UnityEngine;

namespace HexGame.Tools
{
    public class HexGridTool : MonoBehaviour, IToggleTool
    {
        public bool CheckRequirements(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void OnActivate()
        {
            var gridManager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (gridManager != null)
            {
                gridManager.ToggleShowGrid();
                Debug.Log("Grid Visibility Toggled.");
            }
        }

        public void OnDeactivate() { }

        public void HandleInput(Hex hoveredHex) { }
    }
}
