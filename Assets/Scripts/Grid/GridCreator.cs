using UnityEngine;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor; 
#endif

namespace HexGame
{
    [ExecuteAlways]
    [RequireComponent(typeof(GridVisualizationManager))]
    public class GridCreator : MonoBehaviour, ISerializationCallbackReceiver
    {
        private GridVisualizationManager _gridManager;
        private GridVisualizationManager gridManager 
        {
            get 
            {
                if (_gridManager == null) _gridManager = GetComponent<GridVisualizationManager>();
                return _gridManager;
            }
        }

        [SerializeField] public int gridWidth = 10;
        [SerializeField] public int gridHeight = 10;

        [SerializeField] private float noiseScale = 0.1f;
        [SerializeField] private float elevationScale = 2.0f;
        [SerializeField] private Vector2 noiseOffset;

        [SerializeField] private float waterLevel = 0.4f;
        [SerializeField] private float mountainLevel = 0.8f;
        [SerializeField] private float forestLevel = 0.6f;
        [SerializeField] private float forestScale = 5.0f;

        // Internal persistence
        [SerializeField] [HideInInspector] private string serializedGridState;

        private void OnEnable()
        {
            // Restore grid from internal state if lost (e.g. after domain reload)
            if (gridManager != null && gridManager.Grid == null && !string.IsNullOrEmpty(serializedGridState))
            {
                RestoreGridFromState();
            }
        }

        public void OnBeforeSerialize()
        {
            // Save current grid state to internal string
            if (gridManager != null && gridManager.Grid != null)
            {
                GridSaveData saveData = new GridSaveData();
                saveData.width = gridManager.Grid.Width;
                saveData.height = gridManager.Grid.Height;

                foreach (HexData hexData in gridManager.Grid.GetAllHexes())
                {
                    HexSaveData hexSave = new HexSaveData
                    {
                        q = hexData.Q,
                        r = hexData.R,
                        elevation = hexData.Elevation,
                        terrainType = hexData.TerrainType
                    };
                    saveData.hexes.Add(hexSave);
                }

                serializedGridState = JsonUtility.ToJson(saveData);
            }
        }

        public void OnAfterDeserialize()
        {
            // No action needed here; restoration happens in OnEnable
        }

        private void RestoreGridFromState()
        {
            if (string.IsNullOrEmpty(serializedGridState)) return;

            GridSaveData loadedData = JsonUtility.FromJson<GridSaveData>(serializedGridState);
            if (loadedData == null) return;

            // Reconstruct the Grid object
            Grid grid = new Grid(loadedData.width, loadedData.height);
            
            foreach (HexSaveData hexSave in loadedData.hexes)
            {
                HexData hexData = new HexData(hexSave.q, hexSave.r);
                hexData.Elevation = hexSave.elevation;
                hexData.TerrainType = hexSave.terrainType;
                grid.AddHex(hexData);
            }

            // Clean up potentially existing visuals (e.g. from before recompile)
            ClearGrid();

            // Visualize
            gridManager.VisualizeGrid(grid);
            
            // Relink units
            UnitManager.Instance?.RelinkUnitsToGrid();
        }

        public void Initialize(GridVisualizationManager manager)
        {
            _gridManager = manager;
        }

        public void ClearGrid()
        {
            if (gridManager == null) return;

            #if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCompleteObjectUndo(this, "Clear Grid");
            #endif

            // Clear direct children (Hexes)
            for (int i = gridManager.transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(gridManager.transform.GetChild(i).gameObject);
                else DestroyImmediate(gridManager.transform.GetChild(i).gameObject);
            }

            if (gridManager.Grid != null) gridManager.Grid.Clear();
            gridManager.Grid = null;

            MarkDirty();
        }

        public void GenerateGrid()
        {
            if (gridManager == null)
            {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCompleteObjectUndo(this, "Generate Grid");
            #endif

            ClearGrid();

            Grid grid = new Grid(gridWidth, gridHeight);

            for (int r = 0; r < gridHeight; r++)
            {
                for (int q = 0; q < gridWidth; q++)
                {
                    // Create Hex Logic
                    // Calculate World Position first to get Noise - referencing Manager for Layout
                    Vector3 worldPosPreElevation = gridManager.HexToWorld(q, r);
                    
                    float xNoise = worldPosPreElevation.x * noiseScale + noiseOffset.x;
                    float zNoise = worldPosPreElevation.z * noiseScale + noiseOffset.y;
                    float noise = Mathf.PerlinNoise(xNoise, zNoise);
                    
                    TerrainType type = TerrainType.Plains;
                    float elevation = 0;

                    if (noise < waterLevel)
                    {
                        type = TerrainType.Water;
                        elevation = 0;
                    }
                    else if (noise > mountainLevel)
                    {
                        type = TerrainType.Mountains;
                        // Map noise range [mountainLevel, 1.0] to elevation
                        elevation = Mathf.Floor(noise * elevationScale);
                        // Ensure mountains are at least higher than plains
                        if (elevation <= 1) elevation = 2; 
                    }
                    else
                    {
                        // Land (Plains/Forest/Desert)
                        elevation = 1;
                        
                        // Vegetation noise
                        float fNoise = Mathf.PerlinNoise(xNoise * forestScale + 100f, zNoise * forestScale + 100f);
                        if (fNoise > forestLevel)
                        {
                            type = TerrainType.Forest;
                        }
                        else
                        {
                            type = TerrainType.Plains;
                        }
                    }
                    
                    // --- DATA LAYER ---
                    HexData hexData = new HexData(q, r);
                    hexData.Elevation = elevation;
                    hexData.TerrainType = type;
                    
                    grid.AddHex(hexData);
                }
            }

            // Pass the constructed data to the manager to display
            gridManager.VisualizeGrid(grid);

            // Relink units to new hexes
            UnitManager.Instance?.RelinkUnitsToGrid();

            MarkDirty();
        }

        public void SaveGrid(string path)
        {
            if (gridManager == null) return;

            // We save the Current Grid state from the Manager
            if (gridManager.Grid == null)
            {
                return;
            }

            GridSaveData saveData = new GridSaveData();
            saveData.width = gridManager.Grid.Width;
            saveData.height = gridManager.Grid.Height;

            foreach (HexData hexData in gridManager.Grid.GetAllHexes())
            {
                HexSaveData hexSave = new HexSaveData
                {
                    q = hexData.Q,
                    r = hexData.R,
                    elevation = hexData.Elevation,
                    terrainType = hexData.TerrainType
                };
                saveData.hexes.Add(hexSave);
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);
        }

        public void LoadGrid(string path)
        {
            if (gridManager == null) return;

            if (!File.Exists(path))
            {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCompleteObjectUndo(this, "Load Grid");
            #endif

            ClearGrid();

            string json = File.ReadAllText(path);
            GridSaveData loadedData = JsonUtility.FromJson<GridSaveData>(json);

            // Reconstruct the Grid object from data
            Grid grid = new Grid(loadedData.width, loadedData.height);
            
            foreach (HexSaveData hexSave in loadedData.hexes)
            {
                HexData hexData = new HexData(hexSave.q, hexSave.r);
                hexData.Elevation = hexSave.elevation;
                hexData.TerrainType = hexSave.terrainType;
                grid.AddHex(hexData);
            }

            // Update local settings to match loaded file (optional but good UI UX)
            gridWidth = loadedData.width;
            gridHeight = loadedData.height;

            // Visualize
            gridManager.VisualizeGrid(grid);

            // Relink units
            UnitManager.Instance?.RelinkUnitsToGrid();

            MarkDirty();
        }

        private void MarkDirty()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                if (this.gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(this.gameObject.scene);
                }
            }
            #endif
        }
    }
}