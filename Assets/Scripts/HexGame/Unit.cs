using UnityEngine;

namespace HexGame
{
    public class Unit : MonoBehaviour
    {
        public Hex CurrentHex { get; private set; }
        
        public void SetHex(Hex hex)
        {
            if (CurrentHex != null)
            {
                CurrentHex.Unit = null;
            }
            
            CurrentHex = hex;
            
            if (CurrentHex != null)
            {
                if (CurrentHex.Unit != null && CurrentHex.Unit != this)
                {
                    Debug.LogWarning($"Hex {CurrentHex.name} is already occupied by {CurrentHex.Unit.name}. Overwriting.");
                }
                CurrentHex.Unit = this;
                UpdatePosition();
            }
        }
        
        private void UpdatePosition()
        {
            if (CurrentHex != null)
            {
                // Snap to hex position
                // Assuming hex pivot is at bottom or center? Hex mesh seems to be at y=elevation.
                // We should likely raycast or just place it at transform.position.
                Vector3 hexPos = CurrentHex.transform.position;
                transform.position = hexPos; 
            }
        }
    }
}
