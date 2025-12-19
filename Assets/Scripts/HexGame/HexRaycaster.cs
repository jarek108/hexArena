using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame
{
    public class HexRaycaster : MonoBehaviour
    {
        private HexGridManager gridManager;
        public Hex currentHex;

        private void Start()
        {
            gridManager = FindFirstObjectByType<HexGridManager>();
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                currentHex = gridManager.WorldToHex(hit.point);
            }
            else
            {
                currentHex = null;
            }
        }
    }
}