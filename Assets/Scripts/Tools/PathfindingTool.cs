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

            // Auto-select active unit if turn flow is active and nothing is selected
            var activeUnit = GameMaster.Instance?.activeUnit;
            if (activeUnit != null && (SourceHex == null || SourceHex.Data.Unit != activeUnit))
            {
                if (activeUnit.CurrentHex != null)
                {
                    SetSource(activeUnit.CurrentHex);
                }
            }

            // Prevent input if a unit is currently moving
            if (SourceHex != null && SourceHex.Data.Unit != null && SourceHex.Data.Unit.IsMoving) 
            {
                var ruleset = GameMaster.Instance?.ruleset;
                if (ruleset != null) ruleset.OnClearPathfindingVisuals();
                return;
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (hoveredHex != null) 
                    {
                        // In turn flow, we can only click the already active unit (to toggle selection off/on)
                        // or click empty space to clear. But if activeUnit is forced, we shouldn't allow selecting others.
                        if (activeUnit != null)
                        {
                            if (hoveredHex.Data.Unit == activeUnit) SetSource(hoveredHex);
                            else if (hoveredHex.Data.Unit == null) ClearAll();
                        }
                        else
                        {
                            SetSource(hoveredHex);
                        }
                    }
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
            if (ruleset != null)
            {
                ruleset.OnUnitSelected(SourceHex.Data.Unit);
            }

            // Immediately show path visuals (zero-length path + AoA)
            ShowPath(GetPath(SourceHex), SourceHex);
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
            PathResult result = GetPath(hex);

            if (!result.Success)
            {
                return;
            }

            var ruleset = GameMaster.Instance?.ruleset;
            if (SourceHex.Data.Unit != null && ruleset != null)
            {
                Unit movingUnit = SourceHex.Data.Unit;

                // Deselect the old hex visually
                if (SourceHex != null)
                {
                    SourceHex.Data.RemoveState("Selected");
                    SourceHex = null;
                }
                ClearTarget();
                ruleset.OnClearPathfindingVisuals();

                // Let the ruleset handle how the unit follows the path
                ruleset.ExecutePath(movingUnit, result.Path, hex, () => 
                {
                    // On complete: re-select the unit at its new location
                    if (movingUnit != null && movingUnit.CurrentHex != null)
                    {
                        SetSource(movingUnit.CurrentHex);
                    }
                });
            }
            else if (SourceHex.Data.Unit == null)
            {
                // Fallback visual highlight if no unit
                if (TargetHex != null) TargetHex.Data.RemoveState("Target");
                TargetHex = hex;
                TargetHex.Data.AddState("Target");
                ShowPath(result, TargetHex);
            }
            
        }

        private void ClearAll()
        {
            if (SourceHex != null)
            {
                var ruleset = GameMaster.Instance?.ruleset;
                if (ruleset != null) ruleset.OnUnitDeselected(SourceHex.Data.Unit);

                SourceHex.Data.RemoveState("Selected");
                SourceHex = null;
            }
            ClearTarget();
            
            var rulesetVisuals = GameMaster.Instance?.ruleset;
            if (rulesetVisuals != null) rulesetVisuals.OnClearPathfindingVisuals();
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
                if (continuous && SourceHex != null && TargetHex == null)
                {
                    ShowPath(GetPath(newHex), newHex);
                }
            }
            else
            {
                var ruleset = GameMaster.Instance?.ruleset;
                if (ruleset != null) ruleset.OnClearPathfindingVisuals();
            }
        }

        private PathResult GetPath(Hex target)
        {
            if (SourceHex == null || target == null) return new PathResult { Success = false };

            var manager = GridVisualizationManager.Instance ?? FindFirstObjectByType<GridVisualizationManager>();
            if (manager == null || manager.Grid == null) return new PathResult { Success = false };

            var ruleset = GameMaster.Instance?.ruleset;

            if (SourceHex.Data.Unit != null && target.Data.Unit != null && target.Data.Unit.teamId != SourceHex.Data.Unit.teamId && ruleset != null)
            {
                var attackPositions = ruleset.GetValidAttackPositions(SourceHex.Data.Unit, target.Data.Unit);
                ruleset.OnStartPathfinding(attackPositions, SourceHex.Data.Unit);
                return Pathfinder.FindPath(manager.Grid, SourceHex.Data.Unit, SourceHex.Data, attackPositions.ToArray());
            }
            else
            {
                if (ruleset != null && SourceHex.Data.Unit != null) ruleset.OnStartPathfinding(target.Data, SourceHex.Data.Unit);
                return Pathfinder.FindPath(manager.Grid, SourceHex.Data.Unit, SourceHex.Data, target.Data);
            }
        }

        private void ShowPath(PathResult result, Hex target)
        {
            if (SourceHex == null || target == null || !result.Success) return;

            ClearPathVisuals();

            // Draw the entire path without truncation
            foreach (var hexData in result.Path)
            {
                if (hexData == SourceHex.Data) continue;

                // Only skip if this is the actual target hex and it has a unit
                if (hexData == target.Data && target.Data.Unit != null) continue;

                hexData.AddState("Path");
            }
        }
    }
}
