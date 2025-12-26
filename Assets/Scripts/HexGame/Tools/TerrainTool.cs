using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public class TerrainTool : BrushTool
    {
        [SerializeField] private TerrainType paintType = TerrainType.Plains;
        private List<Hex> lastPreviewedViews = new List<Hex>();

        public override void OnDeactivate()
        {
            ClearPreview();
            base.OnDeactivate();
        }

        public override void HandleInput(Hex hoveredHex)
        {
            base.HandleInput(hoveredHex);
            if (!IsEnabled) return;

            RotatePaintType(hoveredHex);

            if (hoveredHex == null) return;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Paint(hoveredHex);
            }
        }

        private void RotatePaintType(Hex hoveredHex)
        {
            if (Keyboard.current == null) return;

            int direction = 0;
            if (Keyboard.current.numpadPlusKey.wasPressedThisFrame || Keyboard.current.equalsKey.wasPressedThisFrame) direction = 1;
            else if (Keyboard.current.numpadMinusKey.wasPressedThisFrame || Keyboard.current.minusKey.wasPressedThisFrame) direction = -1;

            if (direction != 0)
            {
                int count = System.Enum.GetValues(typeof(TerrainType)).Length;
                int current = (int)paintType;
                paintType = (TerrainType)((current + direction + count) % count);
                
                // Refresh preview if we are hovering something
                if (hoveredHex != null)
                {
                    HandleHighlighting(hoveredHex, hoveredHex);
                }
            }
        }

        public override void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            base.HandleHighlighting(oldHex, newHex);
            if (!IsEnabled) return;

            ClearPreview();

            if (newHex != null)
            {
                var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
                List<HexData> affectedData = GetAffectedHexes(newHex);
                foreach (var data in affectedData)
                {
                    Hex view = manager.GetHexView(data);
                    if (view != null)
                    {
                        view.SetPreviewTerrain(paintType);
                        lastPreviewedViews.Add(view);
                    }
                }
            }
        }

        private void ClearPreview()
        {
            foreach (var view in lastPreviewedViews)
            {
                if (view != null) view.SetPreviewTerrain(null);
            }
            lastPreviewedViews.Clear();
        }

        public void Paint(Hex centerHex)
        {
            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            
            foreach (var hexData in affectedHexes)
            {
                hexData.TerrainType = paintType;
            }
        }
    }
}