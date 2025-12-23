using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public class PathfindingTool : BaseTool
    {
        public Hex SourceHex { get; private set; }
        public Hex TargetHex { get; private set; }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            ClearAll();
        }

        public override void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled) return;

            if (Mouse.current == null) return;

            // Left Click: Set Source
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (hoveredHex != null)
                {
                    SetSource(hoveredHex);
                }
                else
                {
                    ClearAll();
                }
            }

            // Right Click: Set Target (requires Source)
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (hoveredHex != null && SourceHex != null)
                {
                    SetTarget(hoveredHex);
                }
            }
        }
        
        public override void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            // Always remove hover from old, regardless of selection state.
            // GridVisualizationManager priorities handle the actual visual visibility.
            if (oldHex != null)
            {
                oldHex.Data.RemoveState(HexState.Hovered);
            }
            
            if (newHex != null)
            {
                newHex.Data.AddState(HexState.Hovered);
            }
        }

        public void SetSource(Hex hex)
        {
            if (SourceHex != null)
            {
                SourceHex.Data.RemoveState(HexState.Selected);
            }

            // If we click the same hex, it's a toggle off
            if (SourceHex == hex)
            {
                ClearAll();
                return;
            }

            SourceHex = hex;
            // When selecting, remove hovered so it doesn't stay in the background
            SourceHex.Data.RemoveState(HexState.Hovered);
            SourceHex.Data.AddState(HexState.Selected);
            
            // Selection changes might invalidate current target
            ClearTarget();
        }

        public void SetTarget(Hex hex)
        {
            if (TargetHex != null)
            {
                TargetHex.Data.RemoveState(HexState.Target);
            }

            // Target cannot be the same as Source
            if (hex == SourceHex) return;

            TargetHex = hex;
            // When targeting, remove hovered so it doesn't stay in the background
            TargetHex.Data.RemoveState(HexState.Hovered);
            TargetHex.Data.AddState(HexState.Target);
        }

        private void ClearTarget()
        {
            if (TargetHex != null)
            {
                TargetHex.Data.RemoveState(HexState.Target);
                TargetHex = null;
            }
        }

        private void ClearAll()
        {
            if (SourceHex != null)
            {
                SourceHex.Data.RemoveState(HexState.Selected);
                SourceHex = null;
            }
            ClearTarget();
        }
    }
}
