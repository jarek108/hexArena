using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;
using System.Linq;

namespace HexGame
{
    [ExecuteAlways]
    public class UnitManager : MonoBehaviour
    {
        public static UnitManager Instance { get; private set; }

        [Header("Setup")]
        public UnitVisualization unitVisualizationPrefab;
        public UnitSet activeUnitSet;

        public UnitSet ActiveUnitSet => activeUnitSet;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void SpawnUnit(int index, int teamId, Hex targetHex)
        {
            SpawnUnit(activeUnitSet, index, teamId, targetHex, unitVisualizationPrefab);
        }

        public void SpawnUnit(UnitSet set, int index, int teamId, Hex targetHex, UnitVisualization prefab)
        {
            if (targetHex == null || targetHex.Data == null || set == null || prefab == null) return;

            // Cleanup existing unit if any
            if (targetHex.Data.Unit != null)
            {
                if (Application.isPlaying) Destroy(targetHex.Data.Unit.gameObject);
                else DestroyImmediate(targetHex.Data.Unit.gameObject);
                targetHex.Data.Unit = null;
            }

            if (index < 0 || index >= set.units.Count) return;

            UnitType type = set.units[index];
            UnitVisualization vizInstance = Instantiate(prefab, transform);
            vizInstance.gameObject.name = $"Unit_{type.Name}";

            Unit unitComponent = vizInstance.gameObject.AddComponent<Unit>();
            unitComponent.Initialize(set, index, teamId);
            unitComponent.SetHex(targetHex);
        }

        public void EraseUnits(List<HexData> hexes)
        {
            foreach (var hexData in hexes)
            {
                if (hexData.Unit != null)
                {
                    if (Application.isPlaying) Destroy(hexData.Unit.gameObject);
                    else DestroyImmediate(hexData.Unit.gameObject);
                    hexData.Unit = null;
                }
            }
        }

        public void EraseAllUnits()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.Contains("Ghost")) continue;

                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            
            var gridManager = FindFirstObjectByType<GridVisualizationManager>();
            if (gridManager != null && gridManager.Grid != null)
            {
                foreach (var hex in gridManager.Grid.GetAllHexes())
                {
                    hex.Unit = null;
                }
            }
        }

        public void RelinkUnitsToGrid()
        {
            var gridManager = FindFirstObjectByType<GridVisualizationManager>();
            if (gridManager == null || gridManager.Grid == null) return;

            Unit[] units = GetComponentsInChildren<Unit>();
            int relinkedCount = 0;
            foreach (var unit in units)
            {
                var data = unit.GetSaveData();
                HexData hexData = gridManager.Grid.GetHexAt(data.q, data.r);
                if (hexData != null)
                {
                    Hex hexView = gridManager.GetHexView(hexData);
                    if (hexView != null)
                    {
                        unit.SetHex(hexView);
                        relinkedCount++;
                    }
                }
            }
            Debug.Log($"Relinked {relinkedCount}/{units.Length} units to the grid.");
        }

        public void SaveUnits(string path)
        {
            UnitSaveBatch batch = new UnitSaveBatch();
            batch.unitSetName = activeUnitSet != null ? activeUnitSet.name : "";

            foreach (Transform child in transform)
            {
                if (child.name.Contains("Ghost")) continue;
                Unit unit = child.GetComponent<Unit>();
                if (unit != null)
                {
                    batch.units.Add(unit.GetSaveData());
                }
            }

            string json = JsonUtility.ToJson(batch, true);
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"Saved {batch.units.Count} units to {path}");
        }

        public void LoadUnits(string path)
        {
            LoadUnits(path, activeUnitSet, unitVisualizationPrefab);
        }

        public void LoadUnits(string path, UnitSet fallbackSet, UnitVisualization visualizationPrefab)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"LoadUnits: File not found at {path}");
                return;
            }

            string json = System.IO.File.ReadAllText(path);
            UnitSaveBatch batch = JsonUtility.FromJson<UnitSaveBatch>(json);

            EraseAllUnits();

            UnitSet set = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(batch.unitSetName))
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{batch.unitSetName} t:UnitSet");
                if (guids.Length > 0)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    set = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitSet>(assetPath);
                }
            }
#endif
            if (set == null)
            {
                set = fallbackSet;
            }

            if (set == null)
            {
                Debug.LogError("LoadUnits failed: No valid UnitSet found.");
                return;
            }

            var gridManager = FindFirstObjectByType<GridVisualizationManager>();
            if (gridManager == null || gridManager.Grid == null)
            {
                Debug.LogError("LoadUnits failed: GridManager not found.");
                return;
            }

            foreach (var data in batch.units)
            {
                HexData hexData = gridManager.Grid.GetHexAt(data.q, data.r);
                if (hexData != null)
                {
                    Hex hexView = gridManager.GetHexView(hexData);
                    if (hexView != null)
                    {
                        SpawnUnit(set, data.unitIndex, data.teamId, hexView, visualizationPrefab);
                    }
                }
            }
            Debug.Log($"Loaded {batch.units.Count} units.");
            RelinkUnitsToGrid();
        }
    }

    [System.Serializable]
    public class UnitSaveBatch
    {
        public string unitSetName;
        public List<UnitSaveData> units = new List<UnitSaveData>();
    }
}