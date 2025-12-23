using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame
{
    [ExecuteAlways]
    public class InputManager : MonoBehaviour
    {
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<InputManager>();
                }
                return _instance;
            }
        }

        [Header("Settings")]
        [Tooltip("Global multiplier for mouse scroll delta.")]
        public float scrollSensitivity = 0.1f;

        [Tooltip("The minimum delta required to trigger a scroll step.")]
        public float scrollThreshold = 0.01f;

        private void OnEnable()
        {
            if (_instance == null) _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
        }

        private void Awake()
        {
            if (Application.isPlaying && _instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
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
