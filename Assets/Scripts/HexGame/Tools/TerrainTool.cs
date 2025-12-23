using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public class TerrainTool : BrushTool
    {
        [SerializeField] private TerrainType paintType = TerrainType.Plains;

        public override void HandleInput(Hex hoveredHex)
        {
            base.HandleInput(hoveredHex);
            if (!IsEnabled || hoveredHex == null) return;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Paint(hoveredHex);
            }
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