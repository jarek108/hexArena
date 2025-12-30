using UnityEngine;
using System.Collections.Generic;
using HexGame.Units;
using System.Linq;
using System.IO;

namespace HexGame
{
    [ExecuteAlways]
    public class UnitManager : MonoBehaviour
    {
        public static UnitManager Instance { get; private set; }

        public UnitVisualization unitVisualizationPrefab;
        public string activeUnitSetPath = "";
        [HideInInspector] public string lastLayoutPath = "";

        private UnitSet _activeSet;
        public UnitSet ActiveUnitSet 
        {
            get
            {
                if (_activeSet == null) LoadActiveSet();
                return _activeSet;
            }
            set => _activeSet = value;
        }

        private void OnEnable()
        {
            Instance = this;
            if (!string.IsNullOrEmpty(activeUnitSetPath))
            {
                LoadActiveSet();
            }
        }

        public void LoadActiveSet()
        {
            if (string.IsNullOrEmpty(activeUnitSetPath) || !System.IO.File.Exists(activeUnitSetPath))
            {
                Debug.LogWarning($"[UnitManager] Active set path is invalid: {activeUnitSetPath}");
                return;
            }

            string json = System.IO.File.ReadAllText(activeUnitSetPath);
            _activeSet = ScriptableObject.CreateInstance<UnitSet>();
            _activeSet.FromJson(json);
            
            // Note: If the JSON refers to a schema asset that was deleted, it will be null.
            // We might need to manually link it if we know where it is.
            if (_activeSet.schema == null)
            {
                // Try to find a schema with the same name in Data/Schemas
                // This is a bit hacky but helps with the transition
                string schemaDir = "Assets/Data/Schemas";
                if (Directory.Exists(schemaDir))
                {
                    // This is just a fallback for the transition
                }
            }

            Debug.Log($"[UnitManager] Loaded active set: {_activeSet.setName} from {activeUnitSetPath}");
        }

        private void Start()
        {
            RelinkUnitsToGrid();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void SpawnUnit(int index, int teamId, Hex targetHex)
        {
            SpawnUnit(ActiveUnitSet, index, teamId, targetHex, unitVisualizationPrefab);
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

            Unit unitComponent = vizInstance.gameObject.AddComponent<Unit>();
            unitComponent.Initialize(index, teamId);
            unitComponent.SetHex(targetHex);
        }

        public void EraseUnits(List<HexData> hexes)
        {
            foreach (var hexData in hexes)
            {
                if (hexData.Units.Count > 0)
                {
                    // Create a copy because we are about to destroy them
                    var unitsToDestroy = new List<Unit>(hexData.Units);
                    foreach (var u in unitsToDestroy)
                    {
                        if (u != null)
                        {
                            if (Application.isPlaying) Destroy(u.gameObject);
                            else DestroyImmediate(u.gameObject);
                        }
                    }
                    hexData.Unit = null; // Clears the list
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
        }

        public void SaveUnits(string path)
        {
            UnitSaveBatch batch = new UnitSaveBatch();
            batch.unitSetId = ActiveUnitSet != null ? ActiveUnitSet.setName : "";

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
            lastLayoutPath = path;
        }

        public void LoadUnits(string path)
        {
            LoadUnits(path, ActiveUnitSet, unitVisualizationPrefab);
        }

        public void LoadUnits(string path, UnitSet fallbackSet, UnitVisualization visualizationPrefab)
        {
            if (!System.IO.File.Exists(path))
            {
                return;
            }

            string json = System.IO.File.ReadAllText(path);
            UnitSaveBatch batch = JsonUtility.FromJson<UnitSaveBatch>(json);

            EraseAllUnits();

            // Load set from batch ID if available
            UnitSet set = null;
            if (!string.IsNullOrEmpty(batch.unitSetId))
            {
                set = ResolveSetById(batch.unitSetId);
                if (set != null)
                {
                    _activeSet = set;
                }
            }

            if (set == null)
            {
                set = fallbackSet;
            }

            if (set == null)
            {
                return;
            }

            var gridManager = FindFirstObjectByType<GridVisualizationManager>();
            if (gridManager == null || gridManager.Grid == null)
            {
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
                        SpawnUnit(set, data.typeIndex, data.teamId, hexView, visualizationPrefab);
                    }
                }
            }
            lastLayoutPath = path;
            RelinkUnitsToGrid();
        }

        private UnitSet ResolveSetById(string id)
        {
            string folder = "Assets/Data/Sets";
            if (!Directory.Exists(folder)) return null;

            string[] files = Directory.GetFiles(folder, "*.json");
            foreach (var file in files)
            {
                string json = File.ReadAllText(file);
                var tempSet = ScriptableObject.CreateInstance<UnitSet>();
                tempSet.FromJson(json);
                if (tempSet.setName == id) 
                {
                    activeUnitSetPath = file.Replace("\\", "/");
                    return tempSet;
                }
            }
            return null;
        }
    }

    [System.Serializable]
    public class UnitSaveBatch
    {
        public string unitSetId;
        public List<UnitSaveData> units = new List<UnitSaveData>();
    }
}