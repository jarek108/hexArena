using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Global multiplier for mouse scroll delta.")]
        public float scrollSensitivity = 0.1f;

        [Tooltip("The minimum delta required to trigger a scroll step.")]
        public float scrollThreshold = 0.01f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Returns the vertical scroll delta multiplied by the global sensitivity.
        /// </summary>
        public float GetScrollDelta()
        {
            if (Mouse.current == null) return 0f;
            return Mouse.current.scroll.y.ReadValue() * scrollSensitivity;
        }

        /// <summary>
        /// Returns 1 if scrolling up, -1 if scrolling down, or 0 if delta is below threshold.
        /// </summary>
        public int GetScrollStep()
        {
            float delta = GetScrollDelta();
            if (Mathf.Abs(delta) > scrollThreshold)
            {
                return delta > 0 ? 1 : -1;
            }
            return 0;
        }
    }
}