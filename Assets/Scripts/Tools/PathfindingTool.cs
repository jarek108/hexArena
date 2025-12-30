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
        public List<PotentialHit> PotentialHits { get; private set; } = new List<PotentialHit>();

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
            if (ruleset != null)
            {
                ruleset.OnUnitSelected(SourceHex.Data.Unit);
            }

            // Immediately show path visuals (zero-length path + AoA)
            CalculateAndShowPath(SourceHex);
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
                ruleset.OnStartPathfinding(hex.Data, SourceHex.Data.Unit);
            }

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Data.Unit, SourceHex.Data, hex.Data);
            Debug.Log("path found");
            if (result.Success)
            {
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
                    CalculateAndShowPath(TargetHex);
                }
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
                if (continuous && SourceHex != null && TargetHex == null)
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
            PotentialHits.Clear();

            var ruleset = GameMaster.Instance?.ruleset;
            if (ruleset != null && SourceHex.Data.Unit != null)
            {
                ruleset.OnStartPathfinding(target.Data, SourceHex.Data.Unit);
            }

            PathResult result = Pathfinder.FindPath(manager.Grid, SourceHex.Data.Unit, SourceHex.Data, target.Data);

            if (ruleset != null && SourceHex.Data.Unit != null)
            {
                // Ruleset handles ghost drawing
                if (showGhost) ruleset.OnFinishPathfinding(SourceHex.Data.Unit, result.Path, result.Success);
                else ruleset.OnClearPathfindingVisuals();
            }

            if (result.Success)
            {
                // Truncate visualization if ruleset dictates
                int stopIndex = (ruleset != null && SourceHex.Data.Unit != null) ? 
                    ruleset.GetMoveStopIndex(SourceHex.Data.Unit, result.Path) : 
                    result.Path.Count;

                for (int i = 0; i < stopIndex; i++)
                {
                    var hexData = result.Path[i];
                    if (hexData == SourceHex.Data) continue;

                    // Only skip if this is the actual target hex and it has a unit
                    if (hexData == target.Data && target.Data.Unit != null) continue;

                    hexData.AddState("Path");
                }

                // If targeting an enemy, show hit chance preview
                if (SourceHex.Data.Unit != null && target.Data.Unit != null && target.Data.Unit.teamId != SourceHex.Data.Unit.teamId && ruleset != null && result.Path != null && result.Path.Count > 0)
                {
                    HexData stopHexData = result.Path[stopIndex - 1];
                    PotentialHits = ruleset.GetPotentialHits(SourceHex.Data.Unit, target.Data.Unit, stopHexData) ?? new List<PotentialHit>();

                    // Condensed single-line log
                    if (PotentialHits.Count > 0)
                    {
                        string logMsg = "[Preview] ";
                        for (int i = 0; i < PotentialHits.Count; i++)
                        {
                            var h = PotentialHits[i];
                            logMsg += $"{h.logInfo}#{h.target.Id} ({(h.max - h.min):P0})";
                            if (i < PotentialHits.Count - 1) logMsg += ", ";
                        }
                        Debug.Log(logMsg);
                    }
                }
            }
        }
    }
}
