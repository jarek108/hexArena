using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using HexGame.Units;

namespace HexGame.Tools
{
    public class UnitPlacementTool : BrushTool
    {
        [Header("Unit Placement Settings")]
        [SerializeField] private UnitVisualization unitVisualizationPrefab;
        [SerializeField] private UnitSet activeUnitSet;
        [SerializeField] private int selectedUnitIndex = 0;

        public override void OnActivate()
        {
            base.OnActivate();
            // Force brush size to 1 for precise unit placement
            brushSize = 1;
        }

        public override void HandleInput(Hex hoveredHex)
        {
            base.HandleInput(hoveredHex);
            if (!IsEnabled || hoveredHex == null) return;

            // Use wasPressedThisFrame for discrete placement, not continuous painting
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceUnit(hoveredHex);
            }
        }

        private Transform GetUnitsContainer()
        {
            Transform container = transform.Find("Units");
            if (container == null)
            {
                GameObject go = new GameObject("Units");
                container = go.transform;
                container.SetParent(this.transform);
                container.localPosition = Vector3.zero;
                container.localRotation = Quaternion.identity;
                container.localScale = Vector3.one;
            }
            return container;
        }

        private void PlaceUnit(Hex targetHex)
        {
            if (targetHex == null || targetHex.Data == null) return;

            if (activeUnitSet == null || activeUnitSet.units == null || activeUnitSet.units.Count == 0)
            {
                Debug.LogWarning("UnitPlacementTool: No UnitSet assigned or set is empty.");
                return;
            }

            if (selectedUnitIndex < 0 || selectedUnitIndex >= activeUnitSet.units.Count)
            {
                Debug.LogWarning("UnitPlacementTool: Invalid unit index selected.");
                return;
            }

            if (unitVisualizationPrefab == null)
            {
                Debug.LogWarning("UnitPlacementTool: No UnitVisualizationPrefab assigned.");
                return;
            }

            UnitType unitType = activeUnitSet.units[selectedUnitIndex];

            // Cleanup existing unit if any
            if (targetHex.Data.Unit != null)
            {
                if (Application.isPlaying) Destroy(targetHex.Data.Unit.gameObject);
                else DestroyImmediate(targetHex.Data.Unit.gameObject);
                
                targetHex.Data.Unit = null;
            }
            
            // 1. Instantiate visualization prefab first
            UnitVisualization vizInstance = Instantiate(unitVisualizationPrefab, GetUnitsContainer());
            vizInstance.gameObject.name = $"Unit_{unitType.Name}";

            // 2. Add Unit component on top
            Unit unitComponent = vizInstance.gameObject.AddComponent<Unit>();
            
            // 3. Initialize Unit with set and index
            unitComponent.Initialize(activeUnitSet, selectedUnitIndex);
            
            // 4. Set Hex
            unitComponent.SetHex(targetHex);
        }
    }
}
