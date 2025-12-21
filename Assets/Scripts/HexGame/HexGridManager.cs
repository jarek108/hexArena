using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace HexGame
{
    [ExecuteAlways]
    public class HexGridManager : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] public Material hexSurfaceMaterial;
        [SerializeField] public Material hexMaterialSides;
        
        [System.Serializable]
        public struct RimSettings
        {
            public Color color;
            [Range(0f, 1f)] public float width;
            public float pulsation;
        }

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
            var visualizer = GetComponent<HexStateVisualizer>();
            if (visualizer == null) return;

            foreach (Transform child in transform)
            {
                Hex hex = child.GetComponent<Hex>();
                if (hex == null) continue;
                visualizer.RefreshVisuals(hex);
            }
        }

        private void InitializeDefaultColors()
        {
            if (terrainTypeColors.Count == 0)
            {
                foreach (TerrainType type in System.Enum.GetValues(typeof(TerrainType)))
                {
                    Color defaultColor = Color.magenta;
                    switch (type)
                    {
                        case TerrainType.Plains: defaultColor = Color.green; break;
                        case TerrainType.Water: defaultColor = Color.blue; break;
                        case TerrainType.Mountains: defaultColor = new Color(0.5f, 0.5f, 0.5f); break;
                        case TerrainType.Forest: defaultColor = new Color(0.1f, 0.4f, 0.1f); break;
                        case TerrainType.Desert: defaultColor = new Color(0.8f, 0.8f, 0.4f); break;
                    }
                    terrainTypeColors.Add(new TerrainColorMapping { terrainType = type, color = defaultColor });
                }
            }
        }

        [Header("Layout Settings")]
        [SerializeField] private float hexSize = 1f;
        [SerializeField] private bool isPointyTop = true;

        public HexGrid Grid { get; set; }

        private Mesh hexMesh;
        public Mesh GetHexMesh() { return hexMesh; }

        private void OnEnable()
        {
            if (Application.IsPlaying(this)) return;
            
            if (Grid == null)
            {
                RebuildGridFromChildren();
            }
        }
        
        private void RebuildGridFromChildren()
        {
             Hex[] childHexes = GetComponentsInChildren<Hex>();
             if (childHexes.Length == 0) return;

             int maxQ = 0;
             int maxR = 0;
             foreach(var hex in childHexes) {
                 if (hex.Q > maxQ) maxQ = hex.Q;
                 if (hex.R > maxR) maxR = hex.R;
             }
             Grid = new HexGrid(maxQ + 1, maxR + 1);

             foreach(var hex in childHexes)
             {
                 if (hex.Data == null)
                 {
                     HexData data = new HexData(hex.Q, hex.R);
                     data.Elevation = hex.Elevation;
                     data.TerrainType = hex.TerrainType;
                     hex.AssignData(data);
                 }
                 Grid.AddHex(hex.Data);
             }
             RefreshVisuals();
        }

        public void InitializeVisuals()
        {
            hexMesh = CreateHexMesh();

            if (hexSurfaceMaterial == null)
            {
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
        }

        public void VisualizeGrid(HexGrid newGrid)
        {
            Grid = newGrid;
            
            InitializeVisuals();

            // Instantiate GameObjects for every hex in the data
            foreach (HexData hexData in Grid.GetAllHexes())
            {
                Vector3 localPos = HexToWorld(hexData.Q, hexData.R);
                localPos.y = hexData.Elevation;

                GameObject hexGO = new GameObject($"Hex ({hexData.Q}, {hexData.R})");
                hexGO.transform.SetParent(this.transform);
                hexGO.transform.localPosition = localPos;
                
                MeshFilter mf = hexGO.AddComponent<MeshFilter>();
                mf.sharedMesh = hexMesh;
                MeshRenderer mr = hexGO.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new Material[] { hexSurfaceMaterial, hexMaterialSides };
                MeshCollider mc = hexGO.AddComponent<MeshCollider>();
                mc.sharedMesh = hexMesh;

                Hex hex = hexGO.AddComponent<Hex>();
                hex.AssignData(hexData); 
            }
            RefreshVisuals();
        }
        
        private Mesh CreateHexMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "HexagonalMesh";

            float size = hexSize;
            float depth = 5f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> topTriangles = new List<int>();
            List<int> sideTriangles = new List<int>();
            
            // Top Face
            vertices.Add(Vector3.zero); 
            uvs.Add(new Vector2(1f, 0f));
            
            for (int i = 0; i < 6; i++)
            {
                float angle_deg = 60 * i + (isPointyTop ? 30 : 0);
                float angle_rad = Mathf.Deg2Rad * angle_deg;
                vertices.Add(new Vector3(size * Mathf.Cos(angle_rad), 0, size * Mathf.Sin(angle_rad)));
                uvs.Add(new Vector2(0f, 0f));
            }

            for (int i = 0; i < 6; i++)
            {
                topTriangles.Add(0);
                topTriangles.Add((i == 5) ? 1 : i + 2);
                topTriangles.Add(i + 1);
            }

            // Side Faces
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

                vertices.Add(new Vector3(x, 0, z));
                vertices.Add(new Vector3(next_x, 0, next_z));
                vertices.Add(new Vector3(x, -depth, z));
                vertices.Add(new Vector3(next_x, -depth, next_z));
                
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                
                int currentBase = sideStartIndex + (i * 4);
                sideTriangles.Add(currentBase);
                sideTriangles.Add(currentBase + 1);
                sideTriangles.Add(currentBase + 3);
                sideTriangles.Add(currentBase);
                sideTriangles.Add(currentBase + 3);
                sideTriangles.Add(currentBase + 2);
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(topTriangles, 0);
            mesh.SetTriangles(sideTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public Vector3 HexToWorld(Hex hex) { return HexToWorld(hex.Q, hex.R); }
        
        public Vector3 HexToWorld(int q, int r)
        {
             float x, z;
            if (isPointyTop)
            {
                x = hexSize * Mathf.Sqrt(3) * (q + r / 2f);
                z = hexSize * 3f / 2f * r;
            }
            else
            {
                x = hexSize * 3f / 2f * q;
                z = hexSize * Mathf.Sqrt(3) * (r + q / 2f);
            }
            return new Vector3(x, 0, z);
        }

        public Hex GetHexView(HexData data)
        {
            if (data == null) return null;
            foreach (Transform child in transform)
            {
                Hex hex = child.GetComponent<Hex>();
                if (hex != null && hex.Data == data) return hex;
            }
            return null;
        }

        public Hex WorldToHex(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            float q_float, r_float;
            if (isPointyTop)
            {
                q_float = (Mathf.Sqrt(3) / 3f * localPos.x - 1f / 3f * localPos.z) / hexSize;
                r_float = (2f / 3f * localPos.z) / hexSize;
            }
            else
            {
                q_float = (2f / 3f * localPos.x) / hexSize;
                r_float = (-1f / 3f * localPos.x + Mathf.Sqrt(3) / 3f * localPos.z) / hexSize;
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
            if (q_diff > r_diff && q_diff > s_diff) q = -r - s;
            else if (r_diff > s_diff) r = -q - s;
            else s = -q - r;
            return new Vector3Int(q, r, s);
        }

        public void SetHexRim(Hex hex, RimSettings settings)
        {
            if (hex == null) return;
            Renderer renderer = hex.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterials.Length > 0)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0); 
                propertyBlock.SetColor("_RimColor", settings.color);
                propertyBlock.SetFloat("_RimWidth", settings.width);
                propertyBlock.SetFloat("_RimPulsationSpeed", settings.pulsation);
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
            if (terrainTypeColors.Count == 0) InitializeDefaultColors();
            foreach (var mapping in terrainTypeColors)
            {
                if (mapping.terrainType == hex.TerrainType) return mapping.color;
            }
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
            return Color.white; 
        }
    }
}