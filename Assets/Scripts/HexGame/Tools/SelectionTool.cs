using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public class SelectionTool : BaseTool
    {
        public Hex SelectedHex { get; private set; }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            // Clear selection and highlights when tool is deactivated
            if (SelectedHex != null)
            {
                SelectedHex.Data.RemoveState(HexState.Selected);
                SelectedHex = null;
            }
        }

        public override void HandleInput(Hex hoveredHex)
        {
            // Note: SelectionTool needs custom guard because it handles 'null' (void) clicks
            if (!IsEnabled) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (hoveredHex != null)
                {
                    if (SelectedHex == hoveredHex)
                    {
                        DeselectHex(hoveredHex);
                    }
                    else
                    {
                        SelectHex(hoveredHex);
                    }
                }
                else
                {
                    // Clicked on nothing (void), deselect current
                     if (SelectedHex != null)
                     {
                         DeselectHex(SelectedHex);
                     }
                }
            }
        }
        
        public override void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            if (oldHex != null && oldHex != SelectedHex)
            {
                oldHex.Data.RemoveState(HexState.Hovered);
            }
            if (newHex != null && newHex != SelectedHex)
            {
                newHex.Data.AddState(HexState.Hovered);
            }
        }

        public void DeselectHex(Hex hex)
        {
            if (!IsEnabled || hex == null) return;

            if (hex == SelectedHex)
            {
                SelectedHex = null;
                hex.Data.RemoveState(HexState.Selected);
            }
        }

        public void SelectHex(Hex hex)
        {
            if (!IsEnabled || hex == null) return;

            if (SelectedHex != null && SelectedHex != hex)
            {
                SelectedHex.Data.RemoveState(HexState.Selected);
            }
            
            SelectedHex = hex; 
            SelectedHex.Data.AddState(HexState.Selected);
            
            // Remove hovered state since it's now selected
            SelectedHex.Data.RemoveState(HexState.Hovered);
        }
    }
}
