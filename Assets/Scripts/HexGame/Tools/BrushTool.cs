using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Tools
{
    public abstract class BrushTool : BaseTool
    {
        [SerializeField] [Range(1, 10)] protected int brushSize = 1;
        protected List<HexData> lastHighlightedHexes = new List<HexData>();

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            ClearHighlights();
        }

        public override void HandleInput(Hex hoveredHex)
        {
            base.HandleInput(hoveredHex);
            if (!IsEnabled || hoveredHex == null) return;

            // Handle Brush Size adjustment via InputManager
            int scrollStep = InputManager.Instance.GetScrollStep();
            if (scrollStep != 0)
            {
                int oldSize = brushSize;
                brushSize = Mathf.Clamp(brushSize + scrollStep, 1, 10);

                if (oldSize != brushSize)
                {
                    // Refresh visuals for the new size
                    HandleHighlighting(hoveredHex, hoveredHex);
                }
            }
        }

        public override void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            ClearHighlights();

            if (newHex != null && newHex.Data != null)
            {
                var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
                if (manager == null || manager.Grid == null) return;

                lastHighlightedHexes = manager.Grid.GetHexesInRange(newHex.Data, brushSize - 1);
                foreach (var hexData in lastHighlightedHexes)
                {
                    hexData.AddState(HexState.Hovered);
                }
            }
        }

        protected virtual void ClearHighlights()
        {
            foreach (var hexData in lastHighlightedHexes)
            {
                if (hexData != null) hexData.RemoveState(HexState.Hovered);
            }
            lastHighlightedHexes.Clear();
        }

        protected List<HexData> GetAffectedHexes(Hex center)
        {
            if (center == null || center.Data == null) return new List<HexData>();

            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return new List<HexData>();

            return manager.Grid.GetHexesInRange(center.Data, brushSize - 1);
        }
    }
}
