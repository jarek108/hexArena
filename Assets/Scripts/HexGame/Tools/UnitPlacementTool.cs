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
        [SerializeField] private int selectedTeamId = 0;

        [Header("Ghost Visuals")]
        [SerializeField, Range(0f, 1f)] private float ghostTransparency = 0.5f;
        [SerializeField] private bool disableGhostShadows = true;

        private List<UnitVisualization> previewGhosts = new List<UnitVisualization>();
        private int lastGhostIndex = -1;
        private UnitVisualization lastGhostPrefab = null;

        public override void OnActivate()
        {
            base.OnActivate();
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
            if (Keyboard.current == null || activeUnitSet == null || activeUnitSet.units.Count == 0) return;

            int direction = 0;
            if (Keyboard.current.numpadPlusKey.wasPressedThisFrame || Keyboard.current.equalsKey.wasPressedThisFrame) direction = 1;
            else if (Keyboard.current.numpadMinusKey.wasPressedThisFrame || Keyboard.current.minusKey.wasPressedThisFrame) direction = -1;

            if (direction != 0)
            {
                int count = activeUnitSet.units.Count;
                selectedUnitIndex = (selectedUnitIndex + direction + count) % count;
                
                RefreshGhostPool();

                if (hoveredHex != null)
                {
                    HandleHighlighting(hoveredHex, hoveredHex);
                }
            }
        }

        private void EraseUnits(Hex centerHex)
        {
            var unitManager = FindFirstObjectByType<UnitManager>();
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
            if (unitVisualizationPrefab == null || activeUnitSet == null || selectedUnitIndex < 0 || selectedUnitIndex >= activeUnitSet.units.Count)
            {
                ClearGhostPool();
                return;
            }

            if (lastGhostIndex != selectedUnitIndex || lastGhostPrefab != unitVisualizationPrefab)
            {
                ClearGhostPool();
                lastGhostIndex = selectedUnitIndex;
                lastGhostPrefab = unitVisualizationPrefab;
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
            lastGhostIndex = -1;
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
            if (unitVisualizationPrefab == null) return;

            var unitManager = FindFirstObjectByType<UnitManager>();
            Transform container = unitManager != null ? unitManager.transform : transform;

            UnitVisualization ghost = Instantiate(unitVisualizationPrefab, container);
            ghost.gameObject.name = "UnitPlacement_PreviewGhost";
            ghost.gameObject.SetActive(false);
            
            if (activeUnitSet != null && selectedUnitIndex >= 0 && selectedUnitIndex < activeUnitSet.units.Count)
            {
                ghost.SetPreviewIdentity(activeUnitSet.units[selectedUnitIndex].Name);
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
            var unitManager = FindFirstObjectByType<UnitManager>();
            if (unitManager == null) return;

            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;

            foreach (var hexData in affectedHexes)
            {
                Hex targetHex = manager.GetHexView(hexData);
                unitManager.SpawnUnit(activeUnitSet, selectedUnitIndex, selectedTeamId, targetHex, unitVisualizationPrefab);
            }
        }
    }
}