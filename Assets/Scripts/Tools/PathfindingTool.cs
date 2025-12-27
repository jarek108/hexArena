using UnityEngine;
using System.Collections.Generic;
using HexGame.UI;
using UnityEngine.InputSystem;

namespace HexGame.Tools
{
    public class PathfindingTool : MonoBehaviour, IActiveTool
    {
        public bool IsEnabled { get; set; }

        public Hex SourceHex { get; private set; }
        public Hex TargetHex { get; private set; }

        [SerializeField] private float maxElevationChange = 1.0f;
        [SerializeField] private bool continuous = true;

        public virtual bool CheckRequirements(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void OnActivate()
        {
            IsEnabled = true;
        }

        public void OnDeactivate()
        {
            IsEnabled = false;
            ClearAll();
        }

        public void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled) return;

            // Prevent input if a unit is currently moving
            if (SourceHex != null && SourceHex.Unit != null && SourceHex.Unit.IsMoving) return;

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (hoveredHex != null) SetSource(hoveredHex);
                    else ClearAll();
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    if (hoveredHex != null) SetTarget(hoveredHex);
                    else ClearTarget();
                }
            }
        }

        public void SetSource(Hex hex)
        {
            if (SourceHex == hex)
            {
                ClearAll();
                return;
            }

            if (SourceHex != null)
            {
                SourceHex.Data.RemoveState("Selected");
            }

            SourceHex = hex;
            SourceHex.Data.AddState("Selected");

            // Changing source invalidates the current path/target
            ClearTarget();
        }

        public void SetTarget(Hex hex)
        {
            if (SourceHex == null || hex == SourceHex) return;

            if (TargetHex == hex)
            {
                ClearTarget();
                return;
            }

            // Pathfinding to target
            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Unit, SourceHex.Data, hex.Data);

            if (result.Success)
            {
                if (SourceHex.Unit != null)
                {
                    // Trigger sequential movement
                    SourceHex.Unit.MoveAlongPath(result.Path);
                    // Clear tool state as unit has moved
                    ClearAll();
                }
                else
                {
                    // Fallback visual highlight if no unit
                    if (TargetHex != null) TargetHex.Data.RemoveState("Target");
                    TargetHex = hex;
                    TargetHex.Data.AddState("Target");
                    CalculateAndShowPath(TargetHex);
                }
            }
        }

        private void ClearAll()
        {
            if (SourceHex != null)
            {
                SourceHex.Data.RemoveState("Selected");
                SourceHex = null;
            }
            ClearTarget();
        }

        private void ClearTarget()
        {
            if (TargetHex != null)
            {
                TargetHex.Data.RemoveState("Target");
                TargetHex = null;
            }
            ClearPathVisuals();
        }

        private void ClearPathVisuals()
        {
            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager != null && manager.Grid != null)
            {
                foreach (var hexData in manager.Grid.GetAllHexes())
                {
                    hexData.RemoveState("Path");
                }
            }
        }

        private void ClearPath() // Legacy helper for OnDeactivate
        {
            ClearAll();
        }

        public void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            if (oldHex != null)
            {
                oldHex.Data.RemoveState("Hovered");
            }
            if (newHex != null)
            {
                newHex.Data.AddState("Hovered");
                
                // Continuous pathfinding logic
                if (continuous && SourceHex != null && TargetHex == null && newHex != SourceHex)
                {
                    CalculateAndShowPath(newHex);
                }
            }
        }

        private void CalculateAndShowPath(Hex target)
        {
            if (SourceHex == null || target == null) return;

            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager == null || manager.Grid == null) return;

            ClearPathVisuals();

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Unit, SourceHex.Data, target.Data);

            if (result.Success)
            {
                foreach (var hexData in result.Path)
                {
                    if (hexData != SourceHex.Data && hexData != target.Data)
                    {
                        hexData.AddState("Path");
                    }
                }
            }
        }
    }
}
