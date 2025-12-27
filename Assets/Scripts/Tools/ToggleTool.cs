using UnityEngine;

namespace HexGame.Tools
{
    public abstract class ToggleTool : MonoBehaviour, IToggleTool
    {
        public bool isActive = true;

        public virtual bool CheckRequirements(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void OnActivate()
        {
            isActive = !isActive;
            OnToggle(isActive);
        }

        public abstract void OnToggle(bool newState);

        public virtual void OnDeactivate() { }
        public virtual void HandleInput(Hex hoveredHex) { }
    }
}
