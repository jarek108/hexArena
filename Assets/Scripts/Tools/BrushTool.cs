using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public abstract class BrushTool : MonoBehaviour, IActiveTool
    {
        public enum ModifierKey { None, Ctrl, Shift, Alt }

        public bool IsEnabled { get; set; }

        public ModifierKey sizeControlKey = ModifierKey.Ctrl;
        [Range(1, 20)] public int brushSize = 1;
        public int maxBrushSize = 6;

        public virtual bool CheckRequirements(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void OnActivate()
        {
            IsEnabled = true;
        }

        public virtual void OnDeactivate()
        {
            IsEnabled = false;
        }

        public virtual void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled) return;

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f)
                {
                    if (IsModifierPressed())
                    {
                        // 1. Clear old highlight using current size
                        if (hoveredHex != null) HandleHighlighting(hoveredHex, null);

                        // 2. Change size
                        brushSize = Mathf.Clamp(brushSize + (scroll > 0 ? 1 : -1), 1, maxBrushSize);
                        
                        // 3. Re-apply highlight using new size
                        if (hoveredHex != null) HandleHighlighting(null, hoveredHex);
                    }
                }
            }
        }

        private bool IsModifierPressed()
        {
            if (sizeControlKey == ModifierKey.None) return true;
            if (Keyboard.current == null) return false;

            switch (sizeControlKey)
            {
                case ModifierKey.Ctrl:
                    return Keyboard.current.ctrlKey.isPressed;
                case ModifierKey.Shift:
                    return Keyboard.current.shiftKey.isPressed;
                case ModifierKey.Alt:
                    return Keyboard.current.altKey.isPressed;
                default:
                    return false;
            }
        }

        public virtual void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            // Simple highlighting logic
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return;

            if (oldHex != null)
            {
                var lastHighlightedHexes = manager.Grid.GetHexesInRange(oldHex.Data, brushSize - 1);
                foreach (var hData in lastHighlightedHexes)
                {
                    hData.RemoveState("Hovered");
                }
            }

            if (newHex != null)
            {
                var currentHighlightedHexes = manager.Grid.GetHexesInRange(newHex.Data, brushSize - 1);
                foreach (var hData in currentHighlightedHexes)
                {
                    hData.AddState("Hovered");
                }
            }
        }

        protected List<HexData> GetAffectedHexes(Hex center)
        {
            if (center == null) return new List<HexData>();
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return new List<HexData>();

            return manager.Grid.GetHexesInRange(center.Data, brushSize - 1);
        }
    }
}