using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    [RequireComponent(typeof(ToolManager))]
    public class TerrainBrush : MonoBehaviour, ITool, IHighlightingTool
    {
        [SerializeField] private TerrainType paintType = TerrainType.Plains;
        [SerializeField] [Range(1, 10)] private int brushSize = 1;

        private List<HexData> lastHighlightedHexes = new List<HexData>();
        
        public string ToolName => "Terrain Brush";
        public bool IsEnabled { get; private set; }

        public void OnActivate()
        {
            IsEnabled = true;
        }

        public void OnDeactivate()
        {
            IsEnabled = false;
            ClearHighlights();
        }

        public void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled) return;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Paint(hoveredHex);
            }
        }

        public void Paint(Hex centerHex)
        {
            if (!IsEnabled || centerHex == null || centerHex.Data == null) return;
            
            var manager = FindFirstObjectByType<HexGridManager>();
            if (manager == null || manager.Grid == null) return;

            List<HexData> hexesInRadius = manager.Grid.GetHexesInRange(centerHex.Data, brushSize - 1);
            
            foreach (var hexData in hexesInRadius)
            {
                hexData.TerrainType = paintType;
            }
        }
        
        public void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            ClearHighlights();

            if (newHex != null && newHex.Data != null)
            {
                var manager = FindFirstObjectByType<HexGridManager>();
                if (manager == null || manager.Grid == null) return;

                lastHighlightedHexes = manager.Grid.GetHexesInRange(newHex.Data, brushSize - 1);
                foreach (var hexData in lastHighlightedHexes)
                {
                    hexData.AddState(HexState.Hovered);
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (var hexData in lastHighlightedHexes)
            {
                if(hexData != null) hexData.RemoveState(HexState.Hovered);
            }
            lastHighlightedHexes.Clear();
        }
    }
}
