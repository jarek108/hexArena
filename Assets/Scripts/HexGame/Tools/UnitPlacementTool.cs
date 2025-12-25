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

        private void EraseUnits(Hex centerHex)
        {
            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            foreach (var hexData in affectedHexes)
            {
                if (hexData.Unit != null)
                {
                    if (Application.isPlaying) Destroy(hexData.Unit.gameObject);
                    else DestroyImmediate(hexData.Unit.gameObject);
                    hexData.Unit = null;
                }
            }
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

            UnitVisualization ghost = Instantiate(unitVisualizationPrefab, GetUnitsContainer());
            ghost.gameObject.name = "UnitPlacement_PreviewGhost";
            ghost.gameObject.SetActive(false);
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
                Color color = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                color.a = ghostTransparency;
                mpb.SetColor("_BaseColor", color);
                r.SetPropertyBlock(mpb);
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

        private void PlaceUnits(Hex centerHex)
        {
            List<HexData> affectedHexes = GetAffectedHexes(centerHex);
            var manager = FindFirstObjectByType<GridVisualizationManager>() ?? GridVisualizationManager.Instance;

            foreach (var hexData in affectedHexes)
            {
                Hex targetHex = manager.GetHexView(hexData);
                PlaceUnitAt(targetHex);
            }
        }

        private void PlaceUnitAt(Hex targetHex)
        {
            if (targetHex == null || targetHex.Data == null) return;
            if (activeUnitSet == null || selectedUnitIndex < 0 || selectedUnitIndex >= activeUnitSet.units.Count) return;
            if (unitVisualizationPrefab == null) return;

            UnitType unitType = activeUnitSet.units[selectedUnitIndex];

            if (targetHex.Data.Unit != null)
            {
                if (Application.isPlaying) Destroy(targetHex.Data.Unit.gameObject);
                else DestroyImmediate(targetHex.Data.Unit.gameObject);
                targetHex.Data.Unit = null;
            }
            
            UnitVisualization vizInstance = Instantiate(unitVisualizationPrefab, GetUnitsContainer());
            vizInstance.gameObject.name = $"Unit_{unitType.Name}";

            Unit unitComponent = vizInstance.gameObject.AddComponent<Unit>();
            unitComponent.Initialize(activeUnitSet, selectedUnitIndex);
            unitComponent.SetHex(targetHex);
        }
    }
}