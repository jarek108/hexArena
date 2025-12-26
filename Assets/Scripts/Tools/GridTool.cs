using UnityEngine;

namespace HexGame.Tools
{
    public class GridTool : MonoBehaviour, IToggleTool
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
            }
        }

        public void OnDeactivate() { }

        public void HandleInput(Hex hoveredHex) { }
    }
}
