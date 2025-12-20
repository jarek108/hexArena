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

        [Header("Selection Rim Settings")]
        [SerializeField] public HexGridManager.RimSettings highlightRimSettings = new HexGridManager.RimSettings { color = Color.yellow, width = 0.2f, pulsation = 5f };
        [SerializeField] public HexGridManager.RimSettings selectionRimSettings = new HexGridManager.RimSettings { color = Color.red, width = 0.2f, pulsation = 2f };

        public Hex SelectedHex { get; private set; }
        public Hex HighlightedHex { get; private set; }

        public void Initialize(HexGridManager manager)
        {
            _gridManager = manager;
        }

        public void HighlightHex(Hex hex)
        {
            if (gridManager == null) return;

            // Visual Priority: Selection > Highlight
            if (hex == SelectedHex) return; 

            // Reset old highlight if it changed
            if (HighlightedHex != null && HighlightedHex != hex)
            {
                // Only reset if it's not the selected hex (though logic above handles that, safety first)
                if (HighlightedHex != SelectedHex)
                {
                    gridManager.ResetHexToDefault(HighlightedHex);
                }
            }

            HighlightedHex = hex;
            if (hex != null)
            {
                gridManager.SetHexRim(hex, highlightRimSettings);
            }
        }

        public void DeselectHex(Hex hex)
        {
            if (gridManager == null) return;

            if (hex == SelectedHex)
            {
                SelectedHex = null;
                gridManager.ResetHexToDefault(hex);
            }
        }

        public void SelectHex(Hex hex)
        {
            if (gridManager == null) return;

            // If a different hex was previously selected, reset its visuals.
            if (SelectedHex != null && SelectedHex != hex)
            {
                gridManager.ResetHexToDefault(SelectedHex);
            }
            
            SelectedHex = hex; 

            if(SelectedHex != null)
            {
                gridManager.SetHexRim(hex, selectionRimSettings);
            }
        }

        public void ResetHex(Hex hex)
        {
            if (gridManager == null) return;

            if (hex == null) return;

            if (hex == HighlightedHex)
            {
                HighlightedHex = null;
            }

            // Re-apply the correct visual state without changing the logical selection.
            if (hex == SelectedHex)
            {
                gridManager.SetHexRim(hex, selectionRimSettings);
            }
            else
            {
                gridManager.ResetHexToDefault(hex);
            }
        }
        
        // Helper to force refresh visuals if settings change
        public void RefreshVisuals()
        {
            if (gridManager == null) return;

            if (SelectedHex != null) gridManager.SetHexRim(SelectedHex, selectionRimSettings);
            if (HighlightedHex != null && HighlightedHex != SelectedHex) gridManager.SetHexRim(HighlightedHex, highlightRimSettings);
        }
    }
}