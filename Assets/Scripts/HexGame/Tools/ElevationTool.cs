using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    [RequireComponent(typeof(ToolManager))]
    public class ElevationTool : MonoBehaviour, ITool, IHighlightingTool
    {
        [SerializeField] [Range(1, 10)] private int brushSize = 1;
        [SerializeField] private float minElevation = 0f;
        [SerializeField] private float maxElevation = 10f;

        private List<HexData> lastHighlightedHexes = new List<HexData>();
        
        public string ToolName => "Elevation Tool";
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
            if (!IsEnabled || hoveredHex == null) return;

            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ChangeElevation(hoveredHex, 1f);
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                ChangeElevation(hoveredHex, -1f);
            }
        }

        private void ChangeElevation(Hex centerHex, float delta)
        {
            if (centerHex == null || centerHex.Data == null) return;

            var manager = FindFirstObjectByType<HexGridManager>();
            if (manager == null || manager.Grid == null) return;

            List<HexData> hexesInRadius = manager.Grid.GetHexesInRange(centerHex.Data, brushSize - 1);
            
            foreach (var hexData in hexesInRadius)
            {
                hexData.Elevation = Mathf.Clamp(hexData.Elevation + delta, minElevation, maxElevation);
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
