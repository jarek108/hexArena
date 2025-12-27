using UnityEngine;

namespace HexGame.Tools
{
    public class GridTool : ToggleTool
    {
        private void Start()
        {
            var gridManager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (gridManager != null)
            {
                isActive = gridManager.showGrid;
            }
        }

        public override void OnToggle(bool newState)
        {
            var gridManager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (gridManager != null)
            {
                gridManager.ToggleShowGrid(); 
                isActive = gridManager.showGrid;
            }
        }
    }
}
