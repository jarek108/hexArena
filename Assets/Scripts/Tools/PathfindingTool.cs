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
            ClearPath();
        }

        public void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled) return;

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

            if (TargetHex != null)
            {
                TargetHex.Data.RemoveState("Target");
            }

            TargetHex = hex;
            TargetHex.Data.AddState("Target");

            CalculateAndShowPath();
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
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
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
            }
        }

        private void CalculateAndShowPath()
        {
            if (SourceHex == null || TargetHex == null) return;

            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return;

            ClearPathVisuals();

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Data, TargetHex.Data, maxElevationChange);

            if (result.Success)
            {
                foreach (var hexData in result.Path)
                {
                    if (hexData != SourceHex.Data && hexData != TargetHex.Data)
                    {
                        hexData.AddState("Path");
                    }
                }
            }
        }
    }
}
