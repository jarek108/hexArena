using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using HexGame.Units;
using System.Linq;

namespace HexGame.Tools
{
    public class UnitPlacementTool : BrushTool
    {
        [Header("Selection Settings")]
        [SerializeField] private string selectedUnitId = "";
        [SerializeField] private int selectedTeamId = 0;

        public override bool CheckRequirements(out string reason)
        {
            if (UnitManager.Instance == null || UnitManager.Instance.ActiveUnitSet == null || UnitManager.Instance.ActiveUnitSet.units.Count == 0)
            {
                reason = "UnitManager has no active UnitSet. Assign a set before placing units.";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        [Header("Ghost Visuals")]
        [SerializeField, Range(0f, 1f)] private float ghostTransparency = 0.5f;
        [SerializeField] private bool disableGhostShadows = true;

        private List<UnitVisualization> previewGhosts = new List<UnitVisualization>();
        private string lastGhostId = "";
        private UnitVisualization lastGhostPrefab = null;

        public override void OnActivate()
        {
            base.OnActivate();
            
            // Ensure we have a valid selection
            if (string.IsNullOrEmpty(selectedUnitId))
            {
                var unitManager = UnitManager.Instance;
                if (unitManager != null && unitManager.ActiveUnitSet != null && unitManager.ActiveUnitSet.units.Count > 0)
                {
                    selectedUnitId = unitManager.ActiveUnitSet.units[0].id;
                }
            }

            RefreshGhostPool();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            ClearGhostPool();
        }

        public override void HandleInput(Hex hoveredHex)
        {
            base.HandleInput(hoveredHex);
            if (!IsEnabled) return;

            RotateUnitSelection(hoveredHex);

            RefreshGhostPool();

            if (hoveredHex == null)
            {
                SetGhostsActive(false);
                return;
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    PlaceUnits(hoveredHex);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    EraseUnits(hoveredHex);
                }
            }
        }

        private void RotateUnitSelection(Hex hoveredHex)
        {
            var unitManager = UnitManager.Instance;
            if (Keyboard.current == null || unitManager == null || unitManager.ActiveUnitSet == null || unitManager.ActiveUnitSet.units.Count == 0) return;

            int direction = 0;
            if (Keyboard.current.numpadPlusKey.wasPressedThisFrame || Keyboard.current.equalsKey.wasPressedThisFrame) direction = 1;
            else if (Keyboard.current.numpadMinusKey.wasPressedThisFrame || Keyboard.current.minusKey.wasPressedThisFrame) direction = -1;

            if (direction != 0)
            {
                var units = unitManager.ActiveUnitSet.units;
                int currentIndex = units.FindIndex(u => u.id == selectedUnitId);
                if (currentIndex == -1) currentIndex = 0;

                int count = units.Count;
                currentIndex = (currentIndex + direction + count) % count;
                selectedUnitId = units[currentIndex].id;
                
                RefreshGhostPool();

                if (hoveredHex != null)
                {
                    HandleHighlighting(hoveredHex, hoveredHex);
                }
            }
        }

        private void EraseUnits(Hex centerHex)
        {
            var unitManager = UnitManager.Instance;
            if (unitManager == null) return;

            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            unitManager.EraseUnits(affectedHexes);
        }

        public override void HandleHighlighting(Hex oldHex, Hex newHex)
        {
            base.HandleHighlighting(oldHex, newHex);
            if (!IsEnabled) return;

            if (newHex == null)
            {
                SetGhostsActive(false);
                return;
            }

            List<HexData> affectedHexes = GetAffectedHexes(newHex);
            
            while (previewGhosts.Count < affectedHexes.Count)
            {
                CreateNewGhostInstance();
            }

            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;
            for (int i = 0; i < previewGhosts.Count; i++)
            {
                if (i < affectedHexes.Count)
                {
                    previewGhosts[i].gameObject.SetActive(true);
                    Hex hexView = manager.GetHexView(affectedHexes[i]);
                    if (hexView != null)
                    {
                        Vector3 pos = hexView.transform.position;
                        pos.y += previewGhosts[i].yOffset;
                        previewGhosts[i].transform.position = pos;
                    }
                }
                else
                {
                    previewGhosts[i].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshGhostPool()
        {
            var unitManager = UnitManager.Instance;
            if (unitManager == null || unitManager.unitVisualizationPrefab == null || unitManager.ActiveUnitSet == null || string.IsNullOrEmpty(selectedUnitId))
            {
                ClearGhostPool();
                return;
            }

            if (lastGhostId != selectedUnitId || lastGhostPrefab != unitManager.unitVisualizationPrefab)
            {
                ClearGhostPool();
                lastGhostId = selectedUnitId;
                lastGhostPrefab = unitManager.unitVisualizationPrefab;
            }
        }

        private void ClearGhostPool()
        {
            foreach (var ghost in previewGhosts)
            {
                if (ghost != null)
                {
                    if (Application.isPlaying) Destroy(ghost.gameObject);
                    else DestroyImmediate(ghost.gameObject);
                }
            }
            previewGhosts.Clear();
            lastGhostId = "";
            lastGhostPrefab = null;
        }

        private void SetGhostsActive(bool active)
        {
            foreach (var ghost in previewGhosts)
            {
                if (ghost != null) ghost.gameObject.SetActive(active);
            }
        }

        private void CreateNewGhostInstance()
        {
            var unitManager = UnitManager.Instance;
            if (unitManager == null || unitManager.unitVisualizationPrefab == null) return;

            UnitVisualization ghost = Instantiate(unitManager.unitVisualizationPrefab, unitManager.transform);
            ghost.gameObject.name = "UnitPlacement_PreviewGhost";
            ghost.gameObject.SetActive(false);
            
            if (unitManager.ActiveUnitSet != null)
            {
                var unitType = unitManager.ActiveUnitSet.units.FirstOrDefault(u => u.id == selectedUnitId);
                if (unitType != null)
                {
                    ghost.SetPreviewIdentity(unitType.Name);
                }
            }

            ApplyGhostVisuals(ghost.gameObject);
            previewGhosts.Add(ghost);
        }

        private void ApplyGhostVisuals(GameObject ghostObj)
        {
            Renderer[] renderers = ghostObj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (disableGhostShadows)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                
                Color color = Color.white;
                if (r.sharedMaterial.HasProperty("_BaseColor"))
                {
                    color = mpb.GetColor("_BaseColor");
                    if (color.a == 0 && color.r == 0 && color.g == 0 && color.b == 0)
                        color = r.sharedMaterial.GetColor("_BaseColor");
                }

                color.a = ghostTransparency;
                mpb.SetColor("_BaseColor", color);
                r.SetPropertyBlock(mpb);
            }
        }

        private void PlaceUnits(Hex centerHex)
        {
            var unitManager = UnitManager.Instance;
            if (unitManager == null) return;

            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;

            foreach (var hexData in affectedHexes)
            {
                Hex targetHex = manager.GetHexView(hexData);
                unitManager.SpawnUnit(selectedUnitId, selectedTeamId, targetHex);
            }
        }
    }
}