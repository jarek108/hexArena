using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    [RequireComponent(typeof(ToolManager))]
    public class SelectionTool : MonoBehaviour, ITool, IHighlightingTool
    {
        public string ToolName => "Selection";
        public bool IsEnabled { get; private set; }

        public Hex SelectedHex { get; private set; }

        public void OnActivate() => IsEnabled = true;
        public void OnDeactivate()
        {
            IsEnabled = false;
            // Clear selection and highlights when tool is deactivated
            if (SelectedHex != null)
            {
                SelectedHex.Data.RemoveState(HexState.Selected);
                SelectedHex = null;
            }
        }

        public void HandleInput(Hex hoveredHex)
        {
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
        
        public void HandleHighlighting(Hex oldHex, Hex newHex)
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