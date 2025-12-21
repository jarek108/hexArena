using UnityEngine;

namespace HexGame
{
    [RequireComponent(typeof(HexGridManager))]
    public class SelectionTool : MonoBehaviour
    {
        private HexGridManager _gridManager;
        private HexGridManager gridManager 
        {
            get 
            {
                if (_gridManager == null) _gridManager = GetComponent<HexGridManager>();
                return _gridManager;
            }
        }

        public Hex SelectedHex { get; private set; }
        public Hex HighlightedHex { get; private set; }

        public void Initialize(HexGridManager manager)
        {
            _gridManager = manager;
        }

        public void HighlightHex(Hex hex)
        {
            if (gridManager == null) return;

            // Visual Priority handled by HexStateVisualizer
            if (hex == SelectedHex) return; 

            // Reset old highlight if it changed
            if (HighlightedHex != null && HighlightedHex != hex)
            {
                HighlightedHex.Data.RemoveState(HexState.Hovered);
            }

            HighlightedHex = hex;
            if (hex != null)
            {
                hex.Data.AddState(HexState.Hovered);
            }
        }

        public void DeselectHex(Hex hex)
        {
            if (gridManager == null) return;

            if (hex == SelectedHex)
            {
                SelectedHex = null;
                if (hex != null) hex.Data.RemoveState(HexState.Selected);
            }
        }

        public void SelectHex(Hex hex)
        {
            if (gridManager == null) return;

            // If a different hex was previously selected, remove its state
            if (SelectedHex != null && SelectedHex != hex)
            {
                SelectedHex.Data.RemoveState(HexState.Selected);
            }
            
            SelectedHex = hex; 

            if(SelectedHex != null)
            {
                SelectedHex.Data.AddState(HexState.Selected);
            }
        }

        public void ResetHex(Hex hex)
        {
            if (gridManager == null || hex == null) return;

            if (hex == HighlightedHex)
            {
                HighlightedHex = null;
                hex.Data.RemoveState(HexState.Hovered);
            }
            // Logic for keeping Selected state is handled by HexStateVisualizer's fallback
        }
    }
}