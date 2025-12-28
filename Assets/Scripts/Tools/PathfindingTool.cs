using UnityEngine;
using System.Collections.Generic;
using HexGame.UI;
using UnityEngine.InputSystem;
using HexGame.Units;

namespace HexGame.Tools
{
    public class PathfindingTool : MonoBehaviour, IActiveTool
    {
        public bool IsEnabled { get; set; }

        public Hex SourceHex { get; private set; }
        public Hex TargetHex { get; private set; }

        [SerializeField] private bool continuous = true;
        [SerializeField] private bool showGhost = true;

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
            
            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null) ruleset.OnClearPathfindingVisuals();
        }

        public void HandleInput(Hex hoveredHex)
        {
            if (!IsEnabled) return;

            // Prevent input if a unit is currently moving
            if (SourceHex != null && SourceHex.Unit != null && SourceHex.Unit.IsMoving) 
            {
                var ruleset = GameMaster.Instance?.ruleset;
                if (ruleset != null) ruleset.OnClearPathfindingVisuals();
                return;
            }

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
            
            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null) ruleset.OnClearPathfindingVisuals();
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
            
            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null)
            {
                ruleset.OnStartPathfinding(hex.Data, SourceHex.Unit);
            }

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Unit, SourceHex.Data, hex.Data);

            if (result.Success)
            {
                if (SourceHex.Unit != null && ruleset != null)
                {
                    // Let the ruleset handle how the unit follows the path
                    ruleset.ExecutePath(SourceHex.Unit, result.Path, hex);
                    
                    // Clear tool state as unit has moved
                    ClearAll();
                    ruleset.OnClearPathfindingVisuals();
                }
                else if (SourceHex.Unit == null)
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
            
            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null) ruleset.OnClearPathfindingVisuals();
        }

        private void ClearTarget()
        {
            if (TargetHex != null)
            {
                TargetHex.Data.RemoveState("Target");
                TargetHex = null;
            }
            ClearPathVisuals();
            
            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null) ruleset.OnClearPathfindingVisuals();
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
            else
            {
                var ruleset = GameMaster.Instance?.ruleset;
                if (ruleset != null) ruleset.OnClearPathfindingVisuals();
            }
        }

        private void CalculateAndShowPath(Hex target)
        {
            if (SourceHex == null || target == null) return;

            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager == null || manager.Grid == null) return;

            ClearPathVisuals();

            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null)
            {
                ruleset.OnStartPathfinding(target.Data, SourceHex.Unit);
            }

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Unit, SourceHex.Data, target.Data);

            if (ruleset != null)
            {
                // Ruleset handles ghost drawing
                if (showGhost) ruleset.OnFinishPathfinding(SourceHex.Unit, result.Path, result.Success);
                else ruleset.OnClearPathfindingVisuals();
            }

            if (result.Success)
            {
                // Truncate visualization if ruleset dictates
                int showCount = ruleset != null ? ruleset.GetMoveStopIndex(SourceHex.Unit, result.Path) : result.Path.Count;

                for (int i = 0; i < showCount; i++)
                {
                    var hexData = result.Path[i];
                    if (hexData != SourceHex.Data && (i < showCount - 1 || target.Unit == null))
                    {
                        hexData.AddState("Path");
                    }
                }
            }
        }
    }
}
