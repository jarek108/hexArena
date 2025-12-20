using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor; 

namespace HexGame
{
    [RequireComponent(typeof(HexGridManager))]
    public class GridPersistence : MonoBehaviour
    {
        private HexGridManager gridManager;

        private void Awake()
        {
            gridManager = GetComponent<HexGridManager>();
        }

        public void SaveGrid(HexGridManager gm, string path)
        {
            GridSaveData saveData = new GridSaveData();
            saveData.width = gm.gridWidth;
            saveData.height = gm.gridHeight;

            foreach (HexData hexData in gm.Grid.GetAllHexes())
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
            Debug.Log($"Grid saved to {path}");
        }

        public void LoadGrid(HexGridManager gm, string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"File not found at {path}. Aborting load.");
                return;
            }

            string json = File.ReadAllText(path);
            GridSaveData loadedData = JsonUtility.FromJson<GridSaveData>(json);

            gm.ClearGrid(); // Clear the old grid via manager

            gm.gridWidth = loadedData.width;
            gm.gridHeight = loadedData.height;
            gm.Grid = new HexGrid(gm.gridWidth, gm.gridHeight); // Re-initialize grid directly on manager
            
            GenerateGridView(gm, loadedData);
        }
        
        private void GenerateGridView(HexGridManager gm, GridSaveData loadedData)
        {
            // Ensure hexMesh and materials are created by calling InitializeVisuals on the manager
            gm.InitializeVisuals(); 

            foreach (HexSaveData hexSave in loadedData.hexes)
            {
                HexData hexData = new HexData(hexSave.q, hexSave.r);
                hexData.Elevation = hexSave.elevation;
                hexData.TerrainType = hexSave.terrainType;
                gm.Grid.AddHex(hexData);

                Vector3 finalPos = gm.HexToWorld(hexData.Q, hexData.R);
                finalPos.y = hexData.Elevation;

                GameObject hexGO = new GameObject($"Hex ({hexData.Q}, {hexData.R})");
                hexGO.transform.SetParent(gm.transform);
                hexGO.transform.position = finalPos;

                MeshFilter mf = hexGO.AddComponent<MeshFilter>();
                mf.sharedMesh = gm.GetHexMesh();
                MeshRenderer mr = hexGO.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new Material[] { gm.hexSurfaceMaterial, gm.hexMaterialSides };
                MeshCollider mc = hexGO.AddComponent<MeshCollider>();
                mc.sharedMesh = gm.GetHexMesh();

                Hex hex = hexGO.AddComponent<Hex>();
                hex.AssignData(hexData);

                MaterialPropertyBlock initialPropertyBlock = new MaterialPropertyBlock();
                mr.GetPropertyBlock(initialPropertyBlock, 0);
                initialPropertyBlock.SetColor("_BaseColor", gm.GetDefaultHexColor(hex));
                initialPropertyBlock.SetColor("_RimColor", gm.defaultRimSettings.color);
                initialPropertyBlock.SetFloat("_RimWidth", gm.defaultRimSettings.width);
                initialPropertyBlock.SetFloat("_RimPulsationSpeed", gm.defaultRimSettings.pulsation);
                mr.SetPropertyBlock(initialPropertyBlock, 0);
            }
        }
    }
}