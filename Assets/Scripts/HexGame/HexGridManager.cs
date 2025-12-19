using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace HexGame
{
    [ExecuteAlways]
    public class HexGridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;

        [Header("Generation Settings")]
        [SerializeField] private float noiseScale = 0.1f;
        [SerializeField] private float elevationScale = 2.0f;
        [SerializeField] private Vector2 noiseOffset;

        [Header("Terrain Generation")]
        [SerializeField] private float waterLevel = 0.4f;
        [SerializeField] private float mountainLevel = 0.8f;
        [SerializeField] private float forestLevel = 0.6f;
        [SerializeField] private float forestScale = 5.0f;

        [Header("Visual Settings")]
        [SerializeField] private Material hexSurfaceMaterial;
        [SerializeField] private Material hexMaterialSides;
        
        [System.Serializable]
        public struct RimSettings
        {
            public Color color;
            [Range(-1f, 1f)] public float width;
            public float pulsation;
        }

        [Header("Rim Settings")]
        [SerializeField] public RimSettings defaultRimSettings = new RimSettings { color = Color.black, width = -1.0f, pulsation = 0f };
        [SerializeField] public RimSettings highlightRimSettings = new RimSettings { color = Color.yellow, width = 0.2f, pulsation = 5f };
        [SerializeField] public RimSettings selectionRimSettings = new RimSettings { color = Color.red, width = 0.2f, pulsation = 2f };

        [System.Serializable]
        public struct TerrainColorMapping
        {
            public TerrainType terrainType;
            public Color color;
        }

        [SerializeField] private List<TerrainColorMapping> terrainTypeColors = new List<TerrainColorMapping>();

        private void OnValidate()
        {
            // Auto-load base materials if missing
            if (hexSurfaceMaterial == null) hexSurfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSurfaceMaterial.mat");
            if (hexMaterialSides == null) hexMaterialSides = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSideMaterial.mat");

            InitializeDefaultColors();
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            foreach (Transform child in transform)
            {
                Hex hex = child.GetComponent<Hex>();
                if (hex == null) continue;

                if (hex == SelectedHex)
                {
                    ApplyRimSettings(hex, selectionRimSettings);
                }
                else if (hex == HighlightedHex)
                {
                    ApplyRimSettings(hex, highlightRimSettings);
                }
                else
                {
                    ApplyRimSettings(hex, defaultRimSettings);
                }
            }
        }

        private void InitializeDefaultColors()
        {
            if (terrainTypeColors.Count == 0)
            {
                // Auto-populate with default colors
                foreach (TerrainType type in System.Enum.GetValues(typeof(TerrainType)))
                {
                    Color defaultColor = Color.magenta; // A distinct default for unassigned types
                    switch (type)
                    {
                        case TerrainType.Plains: defaultColor = Color.green; break;
                        case TerrainType.Water: defaultColor = Color.blue; break;
                        case TerrainType.Mountains: defaultColor = new Color(0.5f, 0.5f, 0.5f); break; // Grey
                        case TerrainType.Forest: defaultColor = new Color(0.1f, 0.4f, 0.1f); break; // Dark green
                        case TerrainType.Desert: defaultColor = new Color(0.8f, 0.8f, 0.4f); break; // Sandy yellow
                    }
                    terrainTypeColors.Add(new TerrainColorMapping { terrainType = type, color = defaultColor });
                }
            }
        }

        [Header("Layout Settings")]
        [SerializeField] private float hexSize = 1f;
        [SerializeField] private bool isPointyTop = true;

        public HexGrid Grid { get; private set; }
        public Hex SelectedHex { get; private set; }
        public Hex HighlightedHex { get; private set; }

        private Mesh hexMesh;

        private void OnEnable()
        {
            // Don't run this in play mode on enable
            if (Application.IsPlaying(this)) return;
            
            // Rebuild grid from existing children if possible
             if (Grid == null)
            {
                RebuildGridFromChildren();
            }
        }
        
        private void RebuildGridFromChildren()
        {
             Grid = new HexGrid(gridWidth, gridHeight);
             Hex[] childHexes = GetComponentsInChildren<Hex>();
             foreach(var hex in childHexes)
             {
                 // If HexData is missing (domain reload), reconstruct it from the Mono's serialized state
                 if (hex.Data == null)
                 {
                     // Use the serialized fields (Q, R, etc) which Unity preserved
                     HexData data = new HexData(hex.Q, hex.R);
                     data.Elevation = hex.Elevation;
                     data.TerrainType = hex.TerrainType;
                     hex.AssignData(data);
                 }
                 
                 Grid.AddHex(hex.Data);
                 // Ensure visuals are clean on rebuild (Play Mode start)
                 ResetHex(hex);
             }
        }

        public void GenerateGrid()
        {
            ClearGrid();

            Grid = new HexGrid(gridWidth, gridHeight);
            SelectedHex = null; // Reset selection on grid generation
            
            // Always create a fresh mesh to ensure validity
            hexMesh = CreateHexMesh();

            // Ensure we have materials
            if (hexSurfaceMaterial == null)
            {
                 // Try loading asset first
                 hexSurfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSurfaceMaterial.mat");
                 
                 if (hexSurfaceMaterial == null)
                 {
                     Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                     hexSurfaceMaterial = new Material(s);
                     hexSurfaceMaterial.name = "DefaultTop";
                     if (s.name.Contains("Universal")) hexSurfaceMaterial.SetColor("_BaseColor", Color.green);
                     else hexSurfaceMaterial.color = Color.green;
                 }
            }
            if (hexMaterialSides == null)
            {
                 // Try loading asset first
                 hexMaterialSides = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexSideMaterial.mat");

                 if (hexMaterialSides == null)
                 {
                     Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                     hexMaterialSides = new Material(s);
                     hexMaterialSides.name = "DefaultSides";
                     Color brown = new Color(0.4f, 0.2f, 0.1f);
                     if (s.name.Contains("Universal")) hexMaterialSides.SetColor("_BaseColor", brown);
                     else hexMaterialSides.color = brown;
                 }
            }

            for (int r = 0; r < gridHeight; r++)
            {
                for (int q = 0; q < gridWidth; q++)
                {
                    // Create Hex Logic
                    // Calculate World Position first to get Noise
                    Vector3 worldPosPreElevation = HexToWorld(q, r);
                    
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
                        // Or just use raw noise * scale
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
                    
                    Grid.AddHex(hexData);

                    // --- VIEW LAYER ---
                    Vector3 finalPos = new Vector3(worldPosPreElevation.x, elevation, worldPosPreElevation.z);

                    // Instantiate GO
                    GameObject hexGO = new GameObject($"Hex ({q}, {r})");
                    hexGO.transform.SetParent(this.transform);
                    hexGO.transform.position = finalPos;
                    
                    // Add Visual Components FIRST so Hex can access them
                    MeshFilter mf = hexGO.AddComponent<MeshFilter>();
                    mf.sharedMesh = hexMesh;
                    
                    MeshRenderer mr = hexGO.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = new Material[] { hexSurfaceMaterial, hexMaterialSides };
                    
                    MeshCollider mc = hexGO.AddComponent<MeshCollider>();
                    mc.sharedMesh = hexMesh;

                    // Add/Get Hex Component
                    Hex hex = hexGO.AddComponent<Hex>();
                    hex.AssignData(hexData); // Link View to Data
                    
                    // Set initial RIM properties (BaseColor is handled by Hex.TerrainType setter now)
                    MaterialPropertyBlock initialPropertyBlock = new MaterialPropertyBlock();
                    mr.GetPropertyBlock(initialPropertyBlock, 0); // Get the block potentially set by Hex.TerrainType
                    
                    // Explicitly set BaseColor to ensure it's correct even if property setter didn't trigger
                    initialPropertyBlock.SetColor("_BaseColor", GetDefaultHexColor(hex));
                    
                    // Initialize Rim to Default (Off)
                    initialPropertyBlock.SetColor("_RimColor", defaultRimSettings.color);
                    initialPropertyBlock.SetFloat("_RimWidth", defaultRimSettings.width);
                    initialPropertyBlock.SetFloat("_RimPulsationSpeed", defaultRimSettings.pulsation);

                    mr.SetPropertyBlock(initialPropertyBlock, 0);
                }
            }
        }
        
        private Mesh CreateHexMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "HexagonalMesh";

            float size = hexSize;
            float depth = 5f; // Skirt depth

            // Vertices count:
            // Top Face: 6 (Outer) + 1 (Center) = 7 vertices
            // Side Faces: 6 quads * 4 vertices = 24 vertices (Separate to ensure hard edges)
            // Total: 31 vertices
            
            // We need separate vertices for the top edge vs the side top edge to create a hard normal break.
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> topTriangles = new List<int>();
            List<int> sideTriangles = new List<int>();
            
            // --- Top Face (Submesh 0) ---
            // Center vertex at index 0
            vertices.Add(Vector3.zero); 
            // Center UV is 1 (representing distance from edge)
            uvs.Add(new Vector2(1f, 0f));
            
            // Top ring vertices (Indices 1-6)
            for (int i = 0; i < 6; i++)
            {
                float angle_deg = 60 * i + (isPointyTop ? 30 : 0);
                float angle_rad = Mathf.Deg2Rad * angle_deg;
                vertices.Add(new Vector3(size * Mathf.Cos(angle_rad), 0, size * Mathf.Sin(angle_rad)));
                
                // Edge UV is 0
                uvs.Add(new Vector2(0f, 0f));
            }

            // Top Triangles
            for (int i = 0; i < 6; i++)
            {
                topTriangles.Add(0); // Center
                topTriangles.Add((i == 5) ? 1 : i + 2); // Next
                topTriangles.Add(i + 1); // Current
            }

            // --- Side Faces (Submesh 1) ---
            // We generate independent quads for the sides to ensure sharp lighting at the top edge
            int sideStartIndex = vertices.Count;
            
            for (int i = 0; i < 6; i++)
            {
                float angle_deg = 60 * i + (isPointyTop ? 30 : 0);
                float angle_rad = Mathf.Deg2Rad * angle_deg;
                float x = size * Mathf.Cos(angle_rad);
                float z = size * Mathf.Sin(angle_rad);
                
                float next_angle_deg = 60 * ((i + 1) % 6) + (isPointyTop ? 30 : 0);
                float next_angle_rad = Mathf.Deg2Rad * next_angle_deg;
                float next_x = size * Mathf.Cos(next_angle_rad);
                float next_z = size * Mathf.Sin(next_angle_rad);

                // 4 Vertices for this side quad
                // Top-Current, Top-Next, Bottom-Current, Bottom-Next
                vertices.Add(new Vector3(x, 0, z)); // 0
                vertices.Add(new Vector3(next_x, 0, next_z)); // 1
                vertices.Add(new Vector3(x, -depth, z)); // 2
                vertices.Add(new Vector3(next_x, -depth, next_z)); // 3
                
                // Basic vertical UV mapping for sides
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                
                // 2 Triangles for the quad
                int currentBase = sideStartIndex + (i * 4);
                
                // Tri 1
                sideTriangles.Add(currentBase);     // Top-Current
                sideTriangles.Add(currentBase + 1); // Top-Next
                sideTriangles.Add(currentBase + 3); // Bottom-Next
                
                // Tri 2
                sideTriangles.Add(currentBase);     // Top-Current
                sideTriangles.Add(currentBase + 3); // Bottom-Next
                sideTriangles.Add(currentBase + 2); // Bottom-Current
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            
            // Define two submeshes
            mesh.subMeshCount = 2;
            mesh.SetTriangles(topTriangles, 0);
            mesh.SetTriangles(sideTriangles, 1);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public void ClearGrid()
        {
            // Destroy all child gameobjects (robust against domain reload)
            int childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
            
            if (Grid != null) Grid.Clear();
            Grid = null;
        }


        public Vector3 HexToWorld(Hex hex)
        {
            return HexToWorld(hex.Q, hex.R);
        }
        
        public Vector3 HexToWorld(int q, int r)
        {
             float x, z;
            if (isPointyTop)
            {
                x = hexSize * Mathf.Sqrt(3) * (q + r / 2f);
                z = hexSize * 3f / 2f * r;
            }
            else // Flat top
            {
                x = hexSize * 3f / 2f * q;
                z = hexSize * Mathf.Sqrt(3) * (r + q / 2f);
            }

            return new Vector3(x, 0, z);
        }

        public Hex GetHexView(HexData data)
        {
            if (data == null) return null;
            // In Approach 3, we usually have a 1:1 mapping.
            // For now, we find it among children. A Dictionary<HexData, Hex> would be faster.
            foreach (Transform child in transform)
            {
                Hex hex = child.GetComponent<Hex>();
                if (hex != null && hex.Data == data) return hex;
            }
            return null;
        }

        public Hex WorldToHex(Vector3 worldPos)
        {
            float q_float, r_float;
            if (isPointyTop)
            {
                q_float = (Mathf.Sqrt(3) / 3f * worldPos.x - 1f / 3f * worldPos.z) / hexSize;
                r_float = (2f / 3f * worldPos.z) / hexSize;
            }
            else // Flat top
            {
                q_float = (2f / 3f * worldPos.x) / hexSize;
                r_float = (-1f / 3f * worldPos.x + Mathf.Sqrt(3) / 3f * worldPos.z) / hexSize;
            }

            Vector3Int coords = RoundToHex(q_float, r_float);
            if(Grid == null) RebuildGridFromChildren();
            HexData data = Grid.GetHexAt(coords.x, coords.y);
            return GetHexView(data);
        }

        private Vector3Int RoundToHex(float q_float, float r_float)
        {
            float s_float = -q_float - r_float;

            int q = Mathf.RoundToInt(q_float);
            int r = Mathf.RoundToInt(r_float);
            int s = Mathf.RoundToInt(s_float);

            float q_diff = Mathf.Abs(q - q_float);
            float r_diff = Mathf.Abs(r - r_float);
            float s_diff = Mathf.Abs(s - s_float);

            if (q_diff > r_diff && q_diff > s_diff)
            {
                q = -r - s;
            }
            else if (r_diff > s_diff)
            {
                r = -q - s;
            }
            else
            {
                s = -q - r;
            }

            return new Vector3Int(q, r, s);
        }
        
        public void HighlightHex(Hex hex)
        {
            // Visual Priority: Selection > Highlight
            if (hex == SelectedHex) return; 

            // Reset old highlight if it changed
            if (HighlightedHex != null && HighlightedHex != hex)
            {
                ApplyRimSettings(HighlightedHex, defaultRimSettings);
            }

            HighlightedHex = hex;
            ApplyRimSettings(hex, highlightRimSettings);
        }

        public void DeselectHex(Hex hex)
        {
            if (hex == SelectedHex)
            {
                SelectedHex = null;
                ApplyRimSettings(hex, defaultRimSettings);
            }
        }

        public void SelectHex(Hex hex)
        {
            // If a different hex was previously selected, reset its visuals to default.
            if (SelectedHex != null && SelectedHex != hex)
            {
                ApplyRimSettings(SelectedHex, defaultRimSettings);
            }
            SelectedHex = hex; 

            // Don't apply selection visuals if the new hex is null (effectively a deselect all)
            if(SelectedHex != null)
            {
                ApplyRimSettings(hex, selectionRimSettings);
            }
        }

        public void ResetHex(Hex hex)
        {
            if (hex == null) return;

            if (hex == HighlightedHex)
            {
                HighlightedHex = null;
            }

            // Re-apply the correct visual state without changing the logical selection.
            RimSettings settingsToApply = (hex == SelectedHex) ? selectionRimSettings : defaultRimSettings;
            ApplyRimSettings(hex, settingsToApply);
        }

        private void ApplyRimSettings(Hex hex, RimSettings settings)
        {
            if (hex == null) return;
            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterials.Length > 0)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0); // Get existing block

                propertyBlock.SetColor("_RimColor", settings.color);
                propertyBlock.SetFloat("_RimWidth", settings.width);
                propertyBlock.SetFloat("_RimPulsationSpeed", settings.pulsation); // Corrected property name

                renderer.SetPropertyBlock(propertyBlock, 0);
            }
        }
        
        public void SetHexColor(Hex hex, Color color)
        {
            if (hex == null) return;
            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterials.Length > 0)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0);
                propertyBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(propertyBlock, 0);
            }
        }

        public Color GetDefaultHexColor(Hex hex)
        {
            if (terrainTypeColors.Count == 0)
            {
                InitializeDefaultColors();
            }

            // Look up color in the mapping list
            foreach (var mapping in terrainTypeColors)
            {
                if (mapping.terrainType == hex.TerrainType)
                {
                    return mapping.color;
                }
            }

            // Fallback to material color if not found
            if (hexSurfaceMaterial != null)
            {
                if (hexSurfaceMaterial.shader.name.Contains("Universal")) return hexSurfaceMaterial.GetColor("_BaseColor");
                else return hexSurfaceMaterial.color;
            }
            return Color.white; 
        }
        
        public Color GetDefaultSideColor()
        {
            if (hexMaterialSides != null)
            {
                if (hexMaterialSides.shader.name.Contains("Universal")) return hexMaterialSides.GetColor("_BaseColor");
                else return hexMaterialSides.color;
            }
            return Color.white; // Fallback
        }
        
        // Helper to get hex object - no longer needed as much since Hex IS the component, 
        // but kept for compatibility if needed or removed if unused. 
        // We will remove the dictionary lookup since the Hex component is on the object.
        public GameObject GetHexGameObject(Hex hex)
        {
            return hex != null ? hex.gameObject : null;
        }
    }
}