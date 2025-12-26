using UnityEngine;
using System.Collections.Generic;

namespace HexGame.Tools
{
    public abstract class BrushTool : MonoBehaviour, IActiveTool
    {
        public bool IsEnabled { get; set; }

        [SerializeField] [Range(1, 5)] protected int brushSize = 1;

        public virtual bool CheckRequirements(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void OnActivate()
        {
            IsEnabled = true;
        }

        public virtual void OnDeactivate()
        {
            IsEnabled = false;
        }

        public virtual void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled || hoveredHex == null) return;
        }

        public virtual void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            // Simple highlighting logic
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return;

            if (oldHex != null)
            {
                var lastHighlightedHexes = manager.Grid.GetHexesInRange(oldHex.Data, brushSize - 1);
                foreach (var hData in lastHighlightedHexes)
                {
                    hData.RemoveState(HexState.Hovered);
                }
            }

            if (newHex != null)
            {
                var currentHighlightedHexes = manager.Grid.GetHexesInRange(newHex.Data, brushSize - 1);
                foreach (var hData in currentHighlightedHexes)
                {
                    hData.AddState(HexState.Hovered);
                }
            }
        }

        protected List<HexData> GetAffectedHexes(Hex center)
        {
            if (center == null) return new List<HexData>();
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return new List<HexData>();

            return manager.Grid.GetHexesInRange(center.Data, brushSize - 1);
        }
    }
}