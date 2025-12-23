using UnityEngine;

namespace HexGame.Tools
{
    [RequireComponent(typeof(ToolManager))]
    public abstract class BaseTool : MonoBehaviour, ITool, IHighlightingTool
    {
        public bool IsEnabled { get; protected set; }

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
            if (!IsEnabled || hoveredHex == null) return;
        }

        public abstract void HandleHighlighting(Hex oldHex, Hex newHex);
    }
}
