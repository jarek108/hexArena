using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor; 

namespace HexGame
{
    [RequireComponent(typeof(HexGridManager))]
    public class GridCreator : MonoBehaviour
    {
        private HexGridManager _gridManager;
        private HexGridManager gridManager 
        {
            get 
            {
                if (_gridManager == null) _gridManager = GetComponent<HexGridManager>();
                return _gridManager;
            }
        }

        [Header("Grid Settings")]
        [SerializeField] public int gridWidth = 10;
        [SerializeField] public int gridHeight = 10;

        [Header("Generation Settings")]
        [SerializeField] private float noiseScale = 0.1f;
        [SerializeField] private float elevationScale = 2.0f;
        [SerializeField] private Vector2 noiseOffset;

        [Header("Terrain Generation")]
        [SerializeField] private float waterLevel = 0.4f;
        [SerializeField] private float mountainLevel = 0.8f;
        [SerializeField] private float forestLevel = 0.6f;
        [SerializeField] private float forestScale = 5.0f;

        public void Initialize(HexGridManager manager)
        {
            _gridManager = manager;
        }

        public void ClearGrid()
        {
            if (gridManager == null) return;

            int childCount = gridManager.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(gridManager.transform.GetChild(i).gameObject);
                else DestroyImmediate(gridManager.transform.GetChild(i).gameObject);
            }
            if (gridManager.Grid != null) gridManager.Grid.Clear();
            gridManager.Grid = null;
        }

        public void GenerateGrid()
        {
            if (gridManager == null)
            {
                Debug.LogError("GridCreator: HexGridManager component not found on the same GameObject!");
                return;
            }

            ClearGrid();

            HexGrid grid = new HexGrid(gridWidth, gridHeight);

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
        }

        public void SaveGrid(string path)
        {
            if (gridManager == null) return;

            // We save the Current Grid state from the Manager
            if (gridManager.Grid == null)
            {
                Debug.LogError("No grid to save!");
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

            ClearGrid();

            string json = File.ReadAllText(path);
            GridSaveData loadedData = JsonUtility.FromJson<GridSaveData>(json);

            // Reconstruct the HexGrid object from data
            HexGrid grid = new HexGrid(loadedData.width, loadedData.height);
            
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
        }
    }
}