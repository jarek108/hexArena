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
            Debug.Log("Pathfinding Tool Activated.");
        }

        public void OnDeactivate()
        {
            IsEnabled = false;
            ClearPath();
            Debug.Log("Pathfinding Tool Deactivated.");
        }

        public void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled || hoveredHex == null) return;

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    SelectHex(hoveredHex);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    ClearPath();
                }
            }
        }

        public void SelectHex(Hex hex)
        {
            if (SourceHex == null)
            {
                SourceHex = hex;
                SourceHex.Data.AddState(HexState.Selected);
                Debug.Log($"Source set to: {hex.Q}, {hex.R}");
            }
            else if (TargetHex == null && hex != SourceHex)
            {
                TargetHex = hex;
                TargetHex.Data.AddState(HexState.Target);
                Debug.Log($"Target set to: {hex.Q}, {hex.R}");
                CalculateAndShowPath();
            }
            else
            {
                bool clickedSource = (hex == SourceHex);
                ClearPath();
                
                if (!clickedSource)
                {
                    SourceHex = hex;
                    SourceHex.Data.AddState(HexState.Selected);
                    Debug.Log($"Source set to: {hex.Q}, {hex.R}");
                }
            }
        }

        private void ClearPath()
        {
            if (SourceHex != null)
            {
                SourceHex.Data.RemoveState(HexState.Selected);
                SourceHex = null;
            }
            if (TargetHex != null)
            {
                TargetHex.Data.RemoveState(HexState.Target);
                TargetHex = null;
            }

            // Clear all path states from the grid
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager != null && manager.Grid != null)
            {
                foreach (var hexData in manager.Grid.GetAllHexes())
                {
                    hexData.RemoveState(HexState.Path);
                }
            }
        }

        public void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            if (!IsEnabled) return;

            if (oldHex != null)
            {
                oldHex.Data.RemoveState(HexState.Hovered);
            }
            if (newHex != null)
            {
                newHex.Data.AddState(HexState.Hovered);
            }
        }

        private void CalculateAndShowPath()
        {
            if (SourceHex == null || TargetHex == null) return;

            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            if (manager == null || manager.Grid == null) return;

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Data, TargetHex.Data, maxElevationChange);

            if (result.Success)
            {
                foreach (var hexData in result.Path)
                {
                    // Don't overwrite source/target states visually if we want to keep them distinct
                    if (hexData != SourceHex.Data && hexData != TargetHex.Data)
                    {
                        hexData.AddState(HexState.Path);
                    }
                }
                Debug.Log($"Path found! Length: {result.Path.Count}");
            }
            else
            {
                Debug.LogWarning("No path found between selected hexes.");
            }
        }
    }
}
