using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public class ElevationTool : BrushTool
    {
        [SerializeField] private float minElevation = 0f;
        [SerializeField] private float maxElevation = 10f;

        public override void HandleInput(Hex hoveredHex)
        {
            base.HandleInput(hoveredHex);
            if (!IsEnabled || hoveredHex == null) return;

            if (Mouse.current == null) return;

            // Handle Elevation Changes
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
            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            
            foreach (var hexData in affectedHexes)
            {
                hexData.Elevation = Mathf.Clamp(hexData.Elevation + delta, minElevation, maxElevation);
            }
        }
    }
}
